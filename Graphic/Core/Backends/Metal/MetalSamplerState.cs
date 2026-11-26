namespace Gubbins.Graphic;

/// <summary>
/// A native Metal sampler state behind <see cref="GpuSamplerState"/>.
/// </summary>
internal sealed class MetalSamplerState : IGpuSamplerState
{
    private readonly nint m_Handle;
    private bool m_Disposed;

    /// <param name="handle">The <c>MTLSamplerState</c>.</param>
    internal MetalSamplerState(nint handle) => m_Handle = handle;

    /// <summary>
    /// Gets the <c>MTLSamplerState</c>.
    /// </summary>
    public nint Handle => m_Handle;

    /// <summary>
    /// Releases the sampler state.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        MetalApi.Release(m_Handle);
    }
}
