namespace Gubbins.Graphic.D3D12;

/// <summary>
/// One texture and the sampler state a D3D12 dispatch binds beside it.
/// </summary>
/// <remarks>
/// The texture and the sampler each own a one-descriptor heap, while the root signature expects tables of
/// <see cref="D3D12Compute.MaxTextures"/> descriptors. A dispatch copies the two descriptors of every binding into its own
/// combined heaps, so this carries the CPU handles those copies read from and the resource and state the transition
/// barriers need.
/// </remarks>
internal readonly struct D3D12TextureBinding
{
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">The sampler state to sample it through.</param>
    internal D3D12TextureBinding(D3D12Texture texture, D3D12SamplerState sampler)
    {
        Resource = texture.Resource;
        State = texture.State;
        Srv = D3D12Api.GetCpuDescriptorHandle(texture.SrvHeap);
        Sampler = D3D12Api.GetCpuDescriptorHandle(sampler.Heap);
    }

    /// <summary>
    /// Gets the <c>ID3D12Resource</c> of the texture, for the transition into the read state.
    /// </summary>
    internal nint Resource { get; }

    /// <summary>
    /// Gets the state the texture is in when the dispatch is recorded.
    /// </summary>
    internal int State { get; }

    /// <summary>
    /// Gets the CPU descriptor of the texture's shader resource view.
    /// </summary>
    internal D3D12CpuDescriptorHandle Srv { get; }

    /// <summary>
    /// Gets the CPU descriptor of the sampler.
    /// </summary>
    internal D3D12CpuDescriptorHandle Sampler { get; }
}
