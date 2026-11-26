namespace Gubbins.Graphic;

/// <summary>
/// A hardware texture the layer created, with the format and the extent the device sees.
/// </summary>
/// <remarks>
/// The resource belongs to the backend, which owns the native texture; this type is the handle a caller uploads through,
/// reads back from and, once the kernel binding is in place, binds as a sampled image.
/// </remarks>
public sealed class GpuTexture2D : IDisposable
{
    private readonly IGpuTexture m_Backend;
    private bool m_Disposed;

    /// <param name="backend">The native texture.</param>
    /// <param name="width">The number of pixels along x.</param>
    /// <param name="height">The number of pixels along y.</param>
    /// <param name="format">The pixel format.</param>
    internal GpuTexture2D(IGpuTexture backend, int width, int height, GpuTextureFormat format)
    {
        m_Backend = backend;
        Width = width;
        Height = height;
        Format = format;
    }

    /// <summary>
    /// Gets the number of pixels along x.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the number of pixels along y.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the pixel format.
    /// </summary>
    public GpuTextureFormat Format { get; }

    /// <summary>
    /// Gets the native texture, for a caller that binds it itself.
    /// </summary>
    internal IGpuTexture Backend => m_Disposed ? throw new ObjectDisposedException(nameof(GpuTexture2D)) : m_Backend;

    /// <summary>
    /// Writes pixels into the texture from the central processor.
    /// </summary>
    /// <param name="pixels">The pixels, in row major order.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuTexture2D));
        }

        m_Backend.Upload(pixels, bytesPerRow);
    }

    /// <summary>
    /// Reads the pixels back into the central processor.
    /// </summary>
    /// <param name="pixels">The destination, in row major order.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuTexture2D));
        }

        m_Backend.Readback(pixels, bytesPerRow);
    }

    /// <summary>
    /// Releases the native texture.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        m_Backend.Dispose();
    }
}

/// <summary>
/// The native texture behind <see cref="GpuTexture2D"/>.
/// </summary>
internal interface IGpuTexture : IDisposable
{
    /// <summary>
    /// Writes pixels into the native texture.
    /// </summary>
    /// <param name="pixels">The pixels.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow);

    /// <summary>
    /// Reads pixels out of the native texture.
    /// </summary>
    /// <param name="pixels">The destination.</param>
    /// <param name="bytesPerRow">The number of bytes in one row.</param>
    void Readback(Span<byte> pixels, int bytesPerRow);
}
