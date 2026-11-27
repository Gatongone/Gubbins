namespace Gubbins.Graphic;

/// <summary>
/// A texture and the sampler state a kernel samples it through.
/// </summary>
public readonly struct GpuTextureBinding
{
    /// <param name="texture">The texture.</param>
    /// <param name="sampler">The sampler state.</param>
    public GpuTextureBinding(GpuTexture2D texture, GpuSamplerState sampler)
    {
        Texture = texture;
        Sampler = sampler;
    }

    /// <summary>
    /// Gets the texture.
    /// </summary>
    public GpuTexture2D Texture { get; }

    /// <summary>
    /// Gets the sampler state.
    /// </summary>
    public GpuSamplerState Sampler { get; }
}
