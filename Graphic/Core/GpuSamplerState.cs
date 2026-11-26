namespace Gubbins.Graphic;

/// <summary>
/// A sampler state the layer created.
/// </summary>
public sealed class GpuSamplerState : IDisposable
{
    private readonly IGpuSamplerState m_Backend;
    private bool m_Disposed;

    /// <param name="backend">The native sampler state.</param>
    internal GpuSamplerState(IGpuSamplerState backend) => m_Backend = backend;

    /// <summary>
    /// Gets the native sampler state, for a caller that binds it itself.
    /// </summary>
    internal IGpuSamplerState Backend => m_Disposed ? throw new ObjectDisposedException(nameof(GpuSamplerState)) : m_Backend;

    /// <summary>
    /// Releases the native sampler state.
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
/// The native sampler state behind <see cref="GpuSamplerState"/>.
/// </summary>
internal interface IGpuSamplerState : IDisposable
{
}
