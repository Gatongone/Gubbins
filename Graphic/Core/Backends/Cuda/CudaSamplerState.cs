
namespace Gubbins.Graphic;

/// <summary>
/// The sampler state of the CUDA backend: the filter and address modes a texture object samples with.
/// </summary>
/// <remarks>
/// <para>
/// CUDA has no sampler object beside the texture. <c>cuTexObjectCreate</c> takes one
/// <see cref="CudaApi.CUDA_TEXTURE_DESC"/> that holds the filter and the address modes, and a <c>tex</c> instruction
/// reads through the description of the object it names, so this type only records what the texture object was created
/// from. A dispatch never passes it to the kernel.
/// </para>
/// <para>
/// That is why <see cref="CudaTexture"/> is created with its sampler: a texture and the description it samples through
/// are one native object, and <see cref="CudaUnifiedProvider.CreateSampler"/> exists so the layer's sampler surface has a
/// value on this backend, not because the driver has a sampler to create.
/// </para>
/// </remarks>
internal sealed class CudaSamplerState : IGpuSamplerState
{
    /// <param name="sampler">The filter and the address modes the state describes.</param>
    internal CudaSamplerState(GpuSampler sampler) => Sampler = sampler;

    /// <summary>
    /// Gets the filter and the address modes this state was created with.
    /// </summary>
    internal GpuSampler Sampler { get; }

    /// <summary>
    /// Releases the state, which owns no native resource.
    /// </summary>
    public void Dispose() { }
}
