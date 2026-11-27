
namespace Gubbins.Graphic;

/// <summary>
/// A native CUDA texture behind <see cref="GpuTexture2D"/>: the <c>CUarray</c> that holds the texels and the
/// <c>CUtexObject</c> that samples it.
/// </summary>
/// <remarks>
/// <para>
/// CUDA has no sampler object beside the texture: the filter and the address modes are fields of the
/// <see cref="CudaApi.CUDA_TEXTURE_DESC"/> the object is created with, so the sampler is stored in the texture object and
/// reported through <see cref="Sampler"/> and <see cref="Description"/>. That is also why a texture is created with its
/// sampler and the provider's sampler state only records the description: there is no native sampler to create until
/// there is a texture to attach the description to.
/// </para>
/// <para>
/// Samples use normalized coordinates, like the other backends' samplers. The flag is not optional: the driver only
/// honours the wrap and mirror address modes when coordinates are normalized, and silently clamps them otherwise.
/// </para>
/// <para>
/// The texels move through a pinned host buffer and <c>cuMemcpy2D</c>. The copy is synchronous and the context is drained
/// before it, so an upload is in the array when the call returns and a readback sees everything a kernel wrote. Creating
/// the texture needs the device's context to be current on the calling thread, which it is for the thread that created
/// the <see cref="CudaDevice"/>.
/// </para>
/// </remarks>
internal sealed unsafe class CudaTexture : IGpuTexture
{
    private readonly int m_Width;
    private readonly int m_Height;
    private readonly int m_BytesPerPixel;
    private readonly int m_ArrayFormat;
    private readonly uint m_Channels;
    private readonly int m_ViewFormat;
    private readonly bool m_ReadAsInteger;
    private nint m_Array;
    private nint m_TextureObject;
    private GpuSampler m_Sampler;
    private CudaApi.CUDA_TEXTURE_DESC m_Description;
    private bool m_Disposed;

    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="sampler">The filter and the address modes the texture object is created with.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the format is not one the layer maps.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the driver refuses the array or the texture object.</exception>
    internal CudaTexture(int width, int height, GpuTextureFormat format, GpuSampler sampler)
    {
        m_Width = width;
        m_Height = height;

        // CUDA has no BGRA channel order: the four bytes are stored and fetched in memory order, so Bgra8Unorm shares the
        // RGBA8 layout and a kernel sees blue in x and red in z. The bytes are deliberately not swapped, because a
        // readback has to return exactly what was uploaded.
        (m_ArrayFormat, m_Channels, m_ViewFormat, m_BytesPerPixel, m_ReadAsInteger) = format switch
        {
            GpuTextureFormat.Rgba8Unorm => (CudaApi.AD_FORMAT_UNSIGNED_INT8, 4u, CudaApi.RES_VIEW_FORMAT_UINT_4X8, 4, false),
            GpuTextureFormat.Bgra8Unorm => (CudaApi.AD_FORMAT_UNSIGNED_INT8, 4u, CudaApi.RES_VIEW_FORMAT_UINT_4X8, 4, false),
            GpuTextureFormat.R32Uint => (CudaApi.AD_FORMAT_UNSIGNED_INT32, 1u, CudaApi.RES_VIEW_FORMAT_UINT_1X32, 4, true),
            GpuTextureFormat.R32Float => (CudaApi.AD_FORMAT_FLOAT, 1u, CudaApi.RES_VIEW_FORMAT_FLOAT_1X32, 4, false),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "CUDA has no format for it.")
        };

        m_Sampler = sampler;
        m_Description = CreateTextureDescriptor(sampler, m_ReadAsInteger);

        var descriptor = new CudaApi.CUDA_ARRAY_DESCRIPTOR
        {
            Width = (nuint)width,
            Height = (nuint)height,
            Format = m_ArrayFormat,
            NumChannels = m_Channels
        };

        CudaApi.Check(CudaApi.cuArrayCreate(out var array, &descriptor), $"cuArrayCreate({width}x{height})");
        m_Array = array;

        try
        {
            // The descriptor is copied to the stack because a field of a managed object has no address to hand out.
            var description = m_Description;
            m_TextureObject = CreateTextureObject(m_Array, width, height, m_ViewFormat, &description);
        }
        catch
        {
            CudaApi.cuArrayDestroy(m_Array);
            m_Array = 0;
            throw;
        }
    }

    /// <summary>
    /// Gets the <c>CUarray</c>.
    /// </summary>
    internal nint Array => m_Array;

    /// <summary>
    /// Gets the <c>CUtexObject</c> a kernel binds when it samples this texture.
    /// </summary>
    internal nint TextureObject => m_TextureObject;

    /// <summary>
    /// Gets the <c>CUarray_format</c> of one channel.
    /// </summary>
    internal int NativeFormat => m_ArrayFormat;

    /// <summary>
    /// Gets the <c>CUresourceViewFormat</c> the texture object is created with.
    /// </summary>
    internal int ViewFormat => m_ViewFormat;

    /// <summary>
    /// Gets the number of bytes one pixel occupies.
    /// </summary>
    internal int BytesPerPixel => m_BytesPerPixel;

    /// <summary>
    /// Gets the sampler description the texture object was created with.
    /// </summary>
    internal GpuSampler Sampler => m_Sampler;

    /// <summary>
    /// Gets the native description the texture object was created with, which is where the sampler really lives.
    /// </summary>
    internal CudaApi.CUDA_TEXTURE_DESC Description => m_Description;

    /// <inheritdoc/>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        ThrowIfDisposed();

        var tight = m_Width * m_BytesPerPixel;
        Validate(pixels.Length, bytesPerRow, tight);

        var size = (nuint)((long)tight * m_Height);
        CudaApi.Check(CudaApi.cuMemHostAlloc(out var staging, size, 0), $"cuMemHostAlloc({size})");

        try
        {
            var destination = new Span<byte>((void*)staging, (int)size);

            for (var row = 0; row < m_Height; row++)
            {
                pixels.Slice(row * bytesPerRow, tight).CopyTo(destination.Slice(row * tight, tight));
            }

            // A kernel that sampled the old texels may still be reading the array; the copy must not race it.
            Synchronize();
            Copy(staging, tight, toArray: true);
        }
        finally
        {
            CudaApi.cuMemFreeHost(staging);
        }
    }

    /// <inheritdoc/>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        ThrowIfDisposed();

        var tight = m_Width * m_BytesPerPixel;
        Validate(pixels.Length, bytesPerRow, tight);

        var size = (nuint)((long)tight * m_Height);
        CudaApi.Check(CudaApi.cuMemHostAlloc(out var staging, size, 0), $"cuMemHostAlloc({size})");

        try
        {
            // A kernel that wrote the array may still be running, and its writes are not in the array until it completes.
            Synchronize();
            Copy(staging, tight, toArray: false);

            var source = new ReadOnlySpan<byte>((void*)staging, (int)size);

            for (var row = 0; row < m_Height; row++)
            {
                source.Slice(row * tight, tight).CopyTo(pixels.Slice(row * bytesPerRow, tight));
            }
        }
        finally
        {
            CudaApi.cuMemFreeHost(staging);
        }
    }

    /// <summary>
    /// Releases the texture object and then the array it samples.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // A dispatch may still be sampling the array, so the context is drained before anything is destroyed.
        CudaApi.cuCtxSynchronize();

        if (m_TextureObject != 0)
        {
            CudaApi.cuTexObjectDestroy(m_TextureObject);
            m_TextureObject = 0;
        }

        if (m_Array != 0)
        {
            CudaApi.cuArrayDestroy(m_Array);
            m_Array = 0;
        }
    }

    /// <summary>
    /// Builds the native texture description that carries the sampler.
    /// </summary>
    /// <param name="sampler">The filter and the address modes.</param>
    /// <param name="readAsInteger">Whether an integer array is read as elements instead of normalized floats.</param>
    /// <returns>The description the texture object is created with.</returns>
    private static CudaApi.CUDA_TEXTURE_DESC CreateTextureDescriptor(GpuSampler sampler, bool readAsInteger)
    {
        var address = sampler.Address switch
        {
            GpuAddressMode.Repeat => CudaApi.ADDRESS_MODE_WRAP,
            GpuAddressMode.Mirror => CudaApi.ADDRESS_MODE_MIRROR,
            _ => CudaApi.ADDRESS_MODE_CLAMP
        };

        var filter = sampler.Filter == GpuTextureFilter.Linear ? CudaApi.FILTER_MODE_LINEAR : CudaApi.FILTER_MODE_POINT;

        // The sampler lives in this one structure. Normalized coordinates are what every other backend's sampler implies,
        // and the driver ignores wrap and mirror address modes without them. Integer arrays are read as elements; floats
        // are never promoted, so only the integer format asks for the flag.
        var description = new CudaApi.CUDA_TEXTURE_DESC
        {
            FilterMode = filter,
            Flags = CudaApi.TRSF_NORMALIZED_COORDINATES | (readAsInteger ? CudaApi.TRSF_READ_AS_INTEGER : 0u),
            MaxAnisotropy = 1,
            MipmapFilterMode = CudaApi.FILTER_MODE_POINT,
            MipmapLevelBias = 0,
            MinMipmapLevelClamp = 0,
            MaxMipmapLevelClamp = 0
        };

        description.AddressMode[0] = address;
        description.AddressMode[1] = address;
        description.AddressMode[2] = address;

        return description;
    }

    /// <summary>
    /// Creates the texture object that samples an array through a view format.
    /// </summary>
    /// <param name="array">The <c>CUarray</c> to sample.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="viewFormat">The <c>CUresourceViewFormat</c> to read the array through.</param>
    /// <param name="description">The native description that carries the sampler.</param>
    /// <returns>The <c>CUtexObject</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the driver refuses the object.</exception>
    private static nint CreateTextureObject(nint array, int width, int height, int viewFormat, CudaApi.CUDA_TEXTURE_DESC* description)
    {
        var resource = new CudaApi.CUDA_RESOURCE_DESC
        {
            ResType = CudaApi.RESOURCE_TYPE_ARRAY,
            HArray = array
        };

        var view = new CudaApi.CUDA_RESOURCE_VIEW_DESC
        {
            Format = viewFormat,
            Width = (nuint)width,
            Height = (nuint)height,
            Depth = 1,
            FirstMipmapLevel = 0,
            LastMipmapLevel = 0,
            FirstLayer = 0,
            LastLayer = 0
        };

        var result = CudaApi.cuTexObjectCreate(out var textureObject, &resource, description, &view);

        if (result == CudaApi.ERROR_INVALID_VALUE)
        {
            // A view that names the array's own format is redundant, and some drivers reject every view descriptor
            // outright (the Windows 610.88 driver answers CUDA_ERROR_INVALID_VALUE for all of them), so the object falls
            // back to the array's native view. The array was created in exactly this view format, so both paths read the
            // bytes the same way.
            result = CudaApi.cuTexObjectCreate(out textureObject, &resource, description, null);
        }

        CudaApi.Check(result, "cuTexObjectCreate");

        return textureObject;
    }

    /// <summary>
    /// Copies one buffer into the array, or the array into one buffer, and waits for the copy.
    /// </summary>
    /// <param name="host">The pinned host buffer, which must hold a tight image.</param>
    /// <param name="pitch">The row pitch of the host buffer, which is the array's own row width.</param>
    /// <param name="toArray">Whether the copy goes from the buffer into the array.</param>
    private void Copy(nint host, int pitch, bool toArray)
    {
        var copy = new CudaApi.CUDA_MEMCPY2D
        {
            WidthInBytes = (nuint)pitch,
            Height = (nuint)m_Height
        };

        if (toArray)
        {
            copy.SourceMemoryType = CudaApi.MEMORYTYPE_HOST;
            copy.SourceHost = host;
            copy.SourcePitch = (nuint)pitch;
            copy.DestinationMemoryType = CudaApi.MEMORYTYPE_ARRAY;
            copy.DestinationArray = m_Array;
        }
        else
        {
            copy.SourceMemoryType = CudaApi.MEMORYTYPE_ARRAY;
            copy.SourceArray = m_Array;
            copy.DestinationMemoryType = CudaApi.MEMORYTYPE_HOST;
            copy.DestinationHost = host;
            copy.DestinationPitch = (nuint)pitch;
        }

        CudaApi.Check(CudaApi.cuMemcpy2D(&copy), "cuMemcpy2D");

        // The synchronous copy is finished on return, but a drain also covers any work the driver queued beside it, so the
        // host buffer is safe to read and the array is safe to sample.
        Synchronize();
    }

    /// <summary>
    /// Waits for every operation recorded on the device's context.
    /// </summary>
    private static void Synchronize() => CudaApi.Check(CudaApi.cuCtxSynchronize(), "cuCtxSynchronize");

    /// <summary>
    /// Refuses a caller's rows when they cannot describe the image.
    /// </summary>
    /// <param name="length">The number of bytes in the caller's span.</param>
    /// <param name="bytesPerRow">The number of bytes in one caller row.</param>
    /// <param name="tight">The number of bytes in one image row.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a row is narrower than the image row.</exception>
    /// <exception cref="ArgumentException">Thrown when the span cannot hold every row.</exception>
    private void Validate(int length, int bytesPerRow, int tight)
    {
        if (bytesPerRow < tight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bytesPerRow),
                bytesPerRow,
                $"A {m_Width} pixel row of this format is {tight} bytes.");
        }

        var required = (long)bytesPerRow * m_Height;

        if (length < required)
        {
            throw new ArgumentException(
                $"The span holds {length} bytes, but {m_Height} rows of {bytesPerRow} bytes need {required}.",
                nameof(bytesPerRow));
        }
    }

    /// <summary>
    /// Throws when the texture has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the texture has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(CudaTexture));
        }
    }
}
