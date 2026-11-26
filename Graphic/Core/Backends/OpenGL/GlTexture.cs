namespace Gubbins.Graphic;

/// <summary>
/// A native OpenGL texture behind <see cref="GpuTexture2D"/>.
/// </summary>
internal sealed unsafe class GlTexture : IGpuTexture
{
    private const uint Texture2D = 0x0DE1;
    private const uint UnpackAlignment = 0x0CF5;
    private const uint PackAlignment = 0x0D05;

    private readonly GlEntryPoints m_Api;
    private readonly uint m_Handle;
    private readonly int m_Width;
    private readonly int m_Height;
    private readonly uint m_Format;
    private readonly uint m_Type;
    private readonly int m_BytesPerPixel;
    private bool m_Disposed;

    /// <param name="api">The entry points of the current context.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    internal GlTexture(GlEntryPoints api, int width, int height, GpuTextureFormat format)
    {
        m_Api = api;
        m_Width = width;
        m_Height = height;

        (var internalFormat, m_Format, m_Type, m_BytesPerPixel) = format switch
        {
            GpuTextureFormat.Rgba8Unorm => (0x8058, 0x1908u, 0x1401u, 4),
            GpuTextureFormat.Bgra8Unorm => (0x8058, 0x80E1u, 0x1401u, 4),
            GpuTextureFormat.R32Uint => (0x8236, 0x8D94u, 0x1405u, 4),
            GpuTextureFormat.R32Float => (0x822E, 0x1903u, 0x1406u, 4),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "The OpenGL backend has no format for it.")
        };

        var handle = 0u;
        api.GenTextures(1, &handle);
        api.BindTexture(Texture2D, handle);
        api.TexImage2D(Texture2D, 0, internalFormat, width, height, 0, m_Format, m_Type, null);
        api.BindTexture(Texture2D, 0);
        m_Handle = handle;
    }

    /// <summary>
    /// Gets the OpenGL texture name.
    /// </summary>
    public uint Handle => m_Handle;

    /// <inheritdoc/>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GlTexture));
        }

        m_Api.BindTexture(Texture2D, m_Handle);
        m_Api.PixelStore(UnpackAlignment, 1);

        fixed (byte* source = pixels)
        {
            m_Api.TexSubImage2D(Texture2D, 0, 0, 0, m_Width, m_Height, m_Format, m_Type, source);
        }

        m_Api.BindTexture(Texture2D, 0);
    }

    /// <inheritdoc/>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GlTexture));
        }

        m_Api.BindTexture(Texture2D, m_Handle);
        m_Api.PixelStore(PackAlignment, 1);

        fixed (byte* destination = pixels)
        {
            m_Api.GetTexImage(Texture2D, 0, m_Format, m_Type, destination);
        }

        m_Api.BindTexture(Texture2D, 0);
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
        var handle = m_Handle;
        m_Api.DeleteTextures(1, &handle);
    }
}
