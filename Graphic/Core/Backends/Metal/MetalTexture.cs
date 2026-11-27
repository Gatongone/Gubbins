namespace Gubbins.Graphic;

/// <summary>
/// A native Metal texture behind <see cref="GpuTexture2D"/>.
/// </summary>
internal sealed unsafe class MetalTexture : IGpuTexture
{
    private readonly nint m_Handle;
    private readonly int m_Width;
    private readonly int m_Height;
    private readonly bool m_Owns;
    private bool m_Disposed;

    /// <param name="device">The <c>MTLDevice</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="usage">What the texture is created for.</param>
    /// <exception cref="MetalException">Thrown when Metal refuses the texture.</exception>
    internal MetalTexture(nint device, int width, int height, GpuTextureFormat format, GpuTextureUsage usage)
    {
        var flags = 0u;

        if ((usage & GpuTextureUsage.Sampled) != 0)
        {
            flags |= 1;
        }

        if ((usage & GpuTextureUsage.Storage) != 0)
        {
            flags |= 2;
        }

        if ((usage & GpuTextureUsage.RenderTarget) != 0)
        {
            flags |= 4;
        }

        var texture = MetalApi.NewTexture(device, (uint)format, (nuint)width, (nuint)height, flags);

        if (texture == 0)
        {
            throw new MetalException($"Metal refused a {width}x{height} texture of {format}.");
        }

        m_Handle = texture;
        m_Width = width;
        m_Height = height;
        m_Owns = true;
    }

    /// <summary>
    /// Wraps a texture the engine owns.
    /// </summary>
    /// <param name="texture">The <c>MTLTexture</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    internal MetalTexture(nint texture, int width, int height)
    {
        m_Handle = texture;
        m_Width = width;
        m_Height = height;
        m_Owns = false;
    }

    /// <summary>
    /// Gets the <c>MTLTexture</c>.
    /// </summary>
    public nint Handle => m_Handle;

    /// <inheritdoc/>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalTexture));
        }

        if (bytesPerRow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerRow), bytesPerRow, "A row has at least one byte.");
        }

        var region = new MetalRegion { Width = (nuint)m_Width, Height = (nuint)m_Height, Depth = 1 };

        fixed (byte* source = pixels)
        {
            MetalApi.ReplaceRegion(m_Handle, region, 0, source, (nuint)bytesPerRow);
        }
    }

    /// <inheritdoc/>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalTexture));
        }

        if (bytesPerRow <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bytesPerRow), bytesPerRow, "A row has at least one byte.");
        }

        var region = new MetalRegion { Width = (nuint)m_Width, Height = (nuint)m_Height, Depth = 1 };

        fixed (byte* destination = pixels)
        {
            MetalApi.GetBytes(m_Handle, destination, (nuint)bytesPerRow, region, 0);
        }
    }

    /// <summary>
    /// Releases the texture.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        if (m_Owns)
        {
            MetalApi.Release(m_Handle);
        }
    }
}
