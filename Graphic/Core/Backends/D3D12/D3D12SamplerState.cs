namespace Gubbins.Graphic.D3D12;

/// <summary>
/// A native D3D12 sampler state behind <see cref="GpuSamplerState"/>.
/// </summary>
internal sealed unsafe class D3D12SamplerState : IGpuSamplerState
{
    private readonly nint m_Heap;
    private readonly D3D12GpuDescriptorHandle m_Descriptor;
    private bool m_Disposed;

    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="sampler">The sampler description.</param>
    /// <exception cref="D3D12Exception">Thrown when the heap or the sampler cannot be created.</exception>
    internal D3D12SamplerState(nint device, GpuSampler sampler)
    {
        var heapDescription = new D3D12DescriptorHeapDesc { Type = 1, NumDescriptors = 1, Flags = 1, NodeMask = 0 };

        if (D3D12Api.CreateDescriptorHeap(device, &heapDescription, out m_Heap) < 0 || m_Heap == 0)
        {
            throw new D3D12Exception("CreateDescriptorHeap failed for the sampler state.");
        }

        var filter = sampler.Filter == GpuTextureFilter.Linear ? 0x15u : 0u;
        var address = sampler.Address switch
        {
            GpuAddressMode.Repeat => 1u,
            GpuAddressMode.Mirror => 2u,
            _ => 3u
        };

        var description = new D3D12SamplerDesc
        {
            Filter = filter,
            AddressU = address,
            AddressV = address,
            AddressW = address,
            MipLodBias = 0,
            MaxAnisotropy = 1,
            ComparisonFunc = 8,
            MinLod = 0,
            MaxLod = float.MaxValue
        };

        D3D12Api.CreateSampler(device, &description, D3D12Api.GetCpuDescriptorHandle(m_Heap));
        m_Descriptor = D3D12Api.GetGpuDescriptorHandle(m_Heap);
    }

    /// <summary>
    /// Gets the descriptor heap that holds the sampler.
    /// </summary>
    public nint Heap => m_Heap;

    /// <summary>
    /// Gets the GPU descriptor of the sampler.
    /// </summary>
    public D3D12GpuDescriptorHandle Descriptor => m_Descriptor;

    /// <summary>
    /// Releases the sampler heap.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        D3D12Api.Release(m_Heap);
    }
}
