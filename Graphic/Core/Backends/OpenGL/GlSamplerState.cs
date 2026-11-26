namespace Gubbins.Graphic;

/// <summary>
/// A native OpenGL sampler object behind <see cref="GpuSamplerState"/>.
/// </summary>
internal sealed unsafe class GlSamplerState : IGpuSamplerState
{
    private const uint MinFilter = 0x2801;
    private const uint MagFilter = 0x2800;
    private const uint WrapS = 0x2802;
    private const uint WrapT = 0x2803;
    private const int Nearest = 0x2600;
    private const int Linear = 0x2601;
    private const int ClampToEdge = 0x812F;
    private const int Repeat = 0x2901;
    private const int MirroredRepeat = 0x8370;

    private readonly GlEntryPoints m_Api;
    private readonly uint m_Handle;
    private bool m_Disposed;

    /// <param name="api">The entry points of the current context.</param>
    /// <param name="sampler">The sampler description.</param>
    internal GlSamplerState(GlEntryPoints api, GpuSampler sampler)
    {
        m_Api = api;

        var filter = sampler.Filter == GpuTextureFilter.Linear ? Linear : Nearest;
        var address = sampler.Address switch
        {
            GpuAddressMode.Repeat => Repeat,
            GpuAddressMode.Mirror => MirroredRepeat,
            _ => ClampToEdge
        };

        var handle = 0u;
        api.GenSamplers(1, &handle);
        api.SamplerParameter(handle, MinFilter, filter);
        api.SamplerParameter(handle, MagFilter, filter);
        api.SamplerParameter(handle, WrapS, address);
        api.SamplerParameter(handle, WrapT, address);
        m_Handle = handle;
    }

    /// <summary>
    /// Gets the OpenGL sampler name.
    /// </summary>
    public uint Handle => m_Handle;

    /// <summary>
    /// Releases the sampler.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        var handle = m_Handle;
        m_Api.DeleteSamplers(1, &handle);
    }
}
