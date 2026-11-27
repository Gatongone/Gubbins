
namespace Gubbins.Graphic.D3D12;

/// <summary>
/// Runs one compute pipeline on a D3D12 device: a root signature with four root descriptors, a pipeline state loaded from
/// the HLSL the system compiles, and a compute queue the bridge does not own.
/// </summary>
/// <remarks>
/// The root descriptors are the shape this layer binds: an unordered access view over the input block, one over the
/// output block, one over the block that carries the structure a kernel takes, and one over the block that carries the
/// lengths. All three are ordered access because the shader declares
/// them that way, so that one translation of a kernel can bind the same way on a backend that has a single address space
/// for resources. Resources are created in the common state and copied from the common state, so every dispatch starts and
/// ends by putting them back there.
/// </remarks>
internal sealed unsafe class D3D12Compute : IDisposable
{
    /// <summary>
    /// The largest number of texture and sampler pairs one dispatch binds.
    /// </summary>
    /// <remarks>
    /// The root signature tables are this wide, so the shader reads the texture at <c>t0</c> through the sampler at
    /// <c>s0</c> and the nth binding through <c>t(n-1)</c> and <c>s(n-1)</c>. A dispatch fills the slots it has bindings
    /// for and repeats the first binding over the rest.
    /// </remarks>
    internal const int MaxTextures = 8;

    private const int DescriptorHeapTypeSrv = 0;
    private const int DescriptorHeapTypeSampler = 1;
    private const int DescriptorHeapShaderVisible = 1;

    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nint m_Allocator;
    private readonly nint m_List;
    private readonly nint m_Fence;

    /// <summary>
    /// The combined descriptor heaps of dispatches recorded without waiting, released once the fence passes them.
    /// </summary>
    private readonly List<nint> m_PendingHeaps = [];

    private nint m_RootSignature;
    private nint m_Pipeline;
    private ulong m_FenceValue;
    private bool m_Pending;
    private bool m_Disposed;

    /// <param name="device">The <c>ID3D12Device</c> to run on.</param>
    /// <exception cref="D3D12Exception">Thrown when the queue, the allocator, the list or the fence cannot be created.</exception>
    internal D3D12Compute(nint device)
    {
        m_Device = device;

        var queueDescription = new D3D12CommandQueueDesc { Type = D3D12Api.COMMAND_LIST_TYPE_COMPUTE, NodeMask = 1 };
        if (D3D12Api.CreateCommandQueue(device, &queueDescription, out m_Queue) < 0 || m_Queue == 0)
        {
            throw new D3D12Exception("CreateCommandQueue failed for the compute queue.");
        }

        if (D3D12Api.CreateCommandAllocator(device, D3D12Api.COMMAND_LIST_TYPE_COMPUTE, out m_Allocator) < 0 || m_Allocator == 0)
        {
            throw new D3D12Exception("CreateCommandAllocator failed for the compute allocator.");
        }

        if (D3D12Api.CreateCommandList(device, m_Allocator, D3D12Api.COMMAND_LIST_TYPE_COMPUTE, out m_List) < 0 || m_List == 0)
        {
            throw new D3D12Exception("CreateCommandList failed for the compute list.");
        }

        // CreateCommandList hands back a recording list, and an allocator cannot be reset while a list recorded into it is
        // still open, so the initial session is closed here.
        if (D3D12Api.CloseCommandList(m_List) < 0)
        {
            throw new D3D12Exception("Closing the initial compute list failed.");
        }

        if (D3D12Api.CreateFence(device, out m_Fence) < 0 || m_Fence == 0)
        {
            throw new D3D12Exception("CreateFence failed for the compute fence.");
        }
    }

    /// <summary>
    /// Loads a pipeline state from bytecode, replacing the one that is already loaded.
    /// </summary>
    /// <param name="bytecode">The compiled shader.</param>
    /// <returns><see langword="true"/> if the pipeline state was created.</returns>
    internal bool SetPipeline(scoped ReadOnlySpan<byte> bytecode)
    {
        if (m_Disposed || bytecode.Length == 0)
        {
            return false;
        }

        if (m_Pipeline != 0)
        {
            D3D12Api.Release(m_Pipeline);
            m_Pipeline = 0;
        }

        if (m_RootSignature == 0 && !CreateRootSignature())
        {
            return false;
        }

        fixed (byte* code = bytecode)
        {
            var description = new D3D12ComputePipelineStateDesc
            {
                RootSignature = m_RootSignature,
                Shader = new D3D12ShaderBytecode { Data = code, Size = (nuint)bytecode.Length },
                NodeMask = 1,
                Flags = D3D12Api.PIPELINE_STATE_FLAG_NONE
            };

            var hr = D3D12Api.CreateComputePipelineState(m_Device, &description, out m_Pipeline);
            if (hr < 0 || m_Pipeline == 0)
            {
                m_Pipeline = 0;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Loads a pipeline state from the HLSL a kernel was translated to, which the system compiles here.
    /// </summary>
    /// <param name="source">The HLSL of the kernel.</param>
    /// <param name="function">The function to compile.</param>
    /// <returns><see langword="true"/> if the pipeline state was created.</returns>
    /// <exception cref="D3D12Exception">Thrown when the system has no compiler or it refuses the program.</exception>
    internal bool SetPipelineFromSource(string source, string function)
    {
        if (m_Disposed || string.IsNullOrEmpty(source))
        {
            return false;
        }

        return SetPipeline(D3D12Api.CompileShader(source, function));
    }

    /// <summary>
    /// Runs the loaded pipeline over two buffers.
    /// </summary>
    /// <param name="sourceResource">The <c>ID3D12Resource</c> the input lives in, for the transition barriers.</param>
    /// <param name="sourceAddress">The GPU address of the input, bound as an unordered access view.</param>
    /// <param name="destinationResource">The <c>ID3D12Resource</c> the output lives in.</param>
    /// <param name="destinationAddress">The GPU address of the output, bound as an unordered access view.</param>
    /// <param name="lengthsAddress">The GPU address of the lengths, or <see langword="0"/> when the kernel takes none.</param>
    /// <param name="groups">The number of thread groups to dispatch on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    internal bool Dispatch(nint sourceResource, ulong sourceAddress, nint destinationResource, ulong destinationAddress, ulong parametersAddress, ulong lengthsAddress, uint groups, bool wait)
        => Dispatch(sourceResource, sourceAddress, destinationResource, destinationAddress, parametersAddress, lengthsAddress, ReadOnlySpan<D3D12TextureBinding>.Empty, groups, wait);

    /// <summary>
    /// Runs the loaded pipeline over two buffers with up to <see cref="MaxTextures"/> textures bound to it.
    /// </summary>
    /// <remarks>
    /// The bindings are gathered into two shader-visible descriptor heaps of their own, one for the textures and one for
    /// the samplers, because the texture and the sampler each own a one-descriptor heap while the root signature tables
    /// are <see cref="MaxTextures"/> descriptors wide. The heaps live until the queue has finished with them: a dispatch
    /// that records without waiting keeps them on a pending list that <see cref="WaitPending"/> releases.
    /// </remarks>
    /// <param name="sourceResource">The <c>ID3D12Resource</c> the input lives in, for the transition barriers.</param>
    /// <param name="sourceAddress">The GPU address of the input, bound as an unordered access view.</param>
    /// <param name="destinationResource">The <c>ID3D12Resource</c> the output lives in.</param>
    /// <param name="destinationAddress">The GPU address of the output, bound as an unordered access view.</param>
    /// <param name="parametersAddress">The GPU address of the structure the kernel takes, or <see langword="0"/>.</param>
    /// <param name="lengthsAddress">The GPU address of the lengths, or <see langword="0"/> when the kernel takes none.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of thread groups to dispatch on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    internal bool Dispatch(nint sourceResource, ulong sourceAddress, nint destinationResource, ulong destinationAddress, ulong parametersAddress, ulong lengthsAddress, ReadOnlySpan<D3D12TextureBinding> bindings, uint groups, bool wait)
    {
        if (m_Disposed || m_Pipeline == 0 || sourceResource == 0 || destinationResource == 0 || groups == 0 || bindings.Length > MaxTextures)
        {
            return false;
        }

        if (m_Pending && !WaitPending())
        {
            return false;
        }

        nint srvHeap = 0;
        nint samplerHeap = 0;

        if (bindings.Length != 0 && !TryCreateDescriptorHeaps(bindings, out srvHeap, out samplerHeap))
        {
            return false;
        }

        if (D3D12Api.ResetCommandAllocator(m_Allocator) < 0 || D3D12Api.ResetCommandList(m_List, m_Allocator) < 0)
        {
            ReleaseDescriptorHeaps(srvHeap, samplerHeap);
            return false;
        }

        var source = new D3D12ResourceBarrier
        {
            Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
            Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
            StateBefore = D3D12Api.RESOURCE_STATE_COMMON,
            StateAfter = D3D12Api.RESOURCE_STATE_UNORDERED_ACCESS
        };

        var destination = new D3D12ResourceBarrier
        {
            Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
            Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
            StateBefore = D3D12Api.RESOURCE_STATE_COMMON,
            StateAfter = D3D12Api.RESOURCE_STATE_UNORDERED_ACCESS
        };

        source.Resource = sourceResource;
        destination.Resource = destinationResource;

        for (var index = 0; index < bindings.Length; index++)
        {
            var binding = bindings[index];

            if (binding.Resource == 0 || binding.State == D3D12Api.RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE)
            {
                continue;
            }

            // A kernel may sample one texture through two samplers, which binds it twice. The resource only transitions
            // once, because a second barrier from its old state would report a state it is no longer in.
            var duplicate = false;

            for (var earlier = 0; earlier < index; earlier++)
            {
                if (bindings[earlier].Resource == binding.Resource)
                {
                    duplicate = true;
                    break;
                }
            }

            if (duplicate)
            {
                continue;
            }

            var texture = new D3D12ResourceBarrier
            {
                Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
                Resource = binding.Resource,
                Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
                StateBefore = binding.State,
                StateAfter = D3D12Api.RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE
            };

            D3D12Api.ResourceBarrier(m_List, &texture);
        }

        D3D12Api.ResourceBarrier(m_List, &source);
        D3D12Api.SetComputeRootSignature(m_List, m_RootSignature);
        D3D12Api.SetPipelineState(m_List, m_Pipeline);
        D3D12Api.SetComputeRootUnorderedAccessView(m_List, 0, sourceAddress);
        D3D12Api.SetComputeRootUnorderedAccessView(m_List, 1, destinationAddress);

        // The block of the structure a kernel takes comes after the buffers, which is where the translation binds it, and
        // the counts are bound after it when it is there.
        if (parametersAddress != 0)
        {
            D3D12Api.SetComputeRootUnorderedAccessView(m_List, 2, parametersAddress);

            if (lengthsAddress != 0)
            {
                D3D12Api.SetComputeRootUnorderedAccessView(m_List, 3, lengthsAddress);
            }
        }
        else if (lengthsAddress != 0)
        {
            D3D12Api.SetComputeRootUnorderedAccessView(m_List, 2, lengthsAddress);
        }

        if (bindings.Length != 0)
        {
            var heaps = stackalloc nint[2];
            heaps[0] = srvHeap;
            heaps[1] = samplerHeap;
            D3D12Api.SetDescriptorHeaps(m_List, 2, heaps);
            D3D12Api.SetComputeRootDescriptorTable(m_List, 4, D3D12Api.GetGpuDescriptorHandle(srvHeap));
            D3D12Api.SetComputeRootDescriptorTable(m_List, 5, D3D12Api.GetGpuDescriptorHandle(samplerHeap));
        }

        D3D12Api.ResourceBarrier(m_List, &destination);
        D3D12Api.Dispatch(m_List, groups, 1, 1);

        // Both resources go back to the common state, which is where the bridge's copies expect to find them.
        var restoreSource = new D3D12ResourceBarrier
        {
            Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
            Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
            StateBefore = D3D12Api.RESOURCE_STATE_UNORDERED_ACCESS,
            StateAfter = D3D12Api.RESOURCE_STATE_COMMON
        };

        var restoreDestination = new D3D12ResourceBarrier
        {
            Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
            Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
            StateBefore = D3D12Api.RESOURCE_STATE_UNORDERED_ACCESS,
            StateAfter = D3D12Api.RESOURCE_STATE_COMMON
        };

        restoreSource.Resource = sourceResource;
        restoreDestination.Resource = destinationResource;

        D3D12Api.ResourceBarrier(m_List, &restoreSource);
        D3D12Api.ResourceBarrier(m_List, &restoreDestination);

        if (D3D12Api.CloseCommandList(m_List) < 0)
        {
            ReleaseDescriptorHeaps(srvHeap, samplerHeap);
            return false;
        }

        D3D12Api.ExecuteCommandLists(m_Queue, m_List);

        m_FenceValue++;

        if (D3D12Api.Signal(m_Queue, m_Fence, m_FenceValue) < 0)
        {
            ReleaseDescriptorHeaps(srvHeap, samplerHeap);
            return false;
        }

        if (!wait)
        {
            // The queue may still be reading the heaps, so they live until the fence passes the dispatch that recorded
            // them, which is what WaitPending waits for.
            if (srvHeap != 0)
            {
                m_PendingHeaps.Add(srvHeap);
            }

            if (samplerHeap != 0)
            {
                m_PendingHeaps.Add(samplerHeap);
            }

            m_Pending = true;
            return true;
        }

        var completed = D3D12Api.WaitForFence(m_Fence, m_FenceValue);
        ReleaseDescriptorHeaps(srvHeap, samplerHeap);
        return completed;
    }

    /// <summary>
    /// Waits for the dispatches that were recorded without waiting.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    internal bool WaitPending()
    {
        if (!m_Pending)
        {
            return true;
        }

        if (!D3D12Api.WaitForFence(m_Fence, m_FenceValue))
        {
            return false;
        }

        m_Pending = false;
        ReleasePendingHeaps();
        return true;
    }

    /// <summary>
    /// Creates the two combined descriptor heaps a dispatch binds and fills every slot of them.
    /// </summary>
    /// <param name="bindings">The textures and samplers to copy into the heaps.</param>
    /// <param name="srvHeap">When the method returns, the heap that holds the texture descriptors.</param>
    /// <param name="samplerHeap">When the method returns, the heap that holds the sampler descriptors.</param>
    /// <returns><see langword="true"/> if both heaps were created and filled.</returns>
    private bool TryCreateDescriptorHeaps(ReadOnlySpan<D3D12TextureBinding> bindings, out nint srvHeap, out nint samplerHeap)
    {
        srvHeap = 0;
        samplerHeap = 0;

        if (bindings.Length == 0)
        {
            return false;
        }

        var srvDescription = new D3D12DescriptorHeapDesc
        {
            Type = DescriptorHeapTypeSrv,
            NumDescriptors = MaxTextures,
            Flags = DescriptorHeapShaderVisible,
            NodeMask = 0
        };

        if (D3D12Api.CreateDescriptorHeap(m_Device, &srvDescription, out srvHeap) < 0 || srvHeap == 0)
        {
            srvHeap = 0;
            return false;
        }

        var samplerDescription = new D3D12DescriptorHeapDesc
        {
            Type = DescriptorHeapTypeSampler,
            NumDescriptors = MaxTextures,
            Flags = DescriptorHeapShaderVisible,
            NodeMask = 0
        };

        if (D3D12Api.CreateDescriptorHeap(m_Device, &samplerDescription, out samplerHeap) < 0 || samplerHeap == 0)
        {
            samplerHeap = 0;
            D3D12Api.Release(srvHeap);
            srvHeap = 0;
            return false;
        }

        var srvStart = D3D12Api.GetCpuDescriptorHandle(srvHeap);
        var samplerStart = D3D12Api.GetCpuDescriptorHandle(samplerHeap);
        var srvIncrement = D3D12Api.GetDescriptorHandleIncrementSize(m_Device, DescriptorHeapTypeSrv);
        var samplerIncrement = D3D12Api.GetDescriptorHandleIncrementSize(m_Device, DescriptorHeapTypeSampler);

        for (var index = 0; index < MaxTextures; index++)
        {
            // Every slot the dispatch does not bind is filled from the first binding. The shader reads only the slots it
            // declared, and D3D12 then sees a table with no uninitialized descriptor in it.
            var binding = index < bindings.Length ? bindings[index] : bindings[0];

            var srv = new D3D12CpuDescriptorHandle { Pointer = srvStart.Pointer + ((nuint)index * srvIncrement) };
            var sampler = new D3D12CpuDescriptorHandle { Pointer = samplerStart.Pointer + ((nuint)index * samplerIncrement) };

            D3D12Api.CopyDescriptorsSimple(m_Device, 1, srv, binding.Srv, DescriptorHeapTypeSrv);
            D3D12Api.CopyDescriptorsSimple(m_Device, 1, sampler, binding.Sampler, DescriptorHeapTypeSampler);
        }

        return true;
    }

    /// <summary>
    /// Releases the two heaps one dispatch created, either after its fence or after a failure before it was submitted.
    /// </summary>
    /// <param name="srvHeap">The texture heap, or <see langword="0"/>.</param>
    /// <param name="samplerHeap">The sampler heap, or <see langword="0"/>.</param>
    private static void ReleaseDescriptorHeaps(nint srvHeap, nint samplerHeap)
    {
        D3D12Api.Release(srvHeap);
        D3D12Api.Release(samplerHeap);
    }

    /// <summary>
    /// Releases the heaps of the dispatches that were recorded without waiting, once their fence has passed.
    /// </summary>
    private void ReleasePendingHeaps()
    {
        foreach (var heap in m_PendingHeaps)
        {
            D3D12Api.Release(heap);
        }

        m_PendingHeaps.Clear();
    }

    /// <summary>
    /// Builds the root signature the pipeline state needs.
    /// </summary>
    /// <returns><see langword="true"/> if the signature was created.</returns>
    private bool CreateRootSignature()
    {
        // A root descriptor on its own reports no size, which is why the block that carries the lengths is bound beside
        // the buffers rather than read from one of them.
        var parameters = stackalloc D3D12RootParameter[6];
        var ranges = (D3D12DescriptorRange*)NativeInterop.AllocZeroed((nuint)(sizeof(D3D12DescriptorRange) * 2));

        if (ranges == null)
        {
            return false;
        }

        for (var index = 0; index < 4; index++)
        {
            parameters[index] = new D3D12RootParameter
            {
                Type = D3D12Api.ROOT_PARAMETER_TYPE_UAV,
                ShaderRegister = (uint)index,
                RegisterSpace = 0,
                ShaderVisibility = D3D12Api.SHADER_VISIBILITY_ALL
            };
        }

        ranges[0] = new D3D12DescriptorRange
        {
            RangeType = D3D12Api.DESCRIPTOR_RANGE_TYPE_SRV,
            NumDescriptors = MaxTextures,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0
        };

        ranges[1] = new D3D12DescriptorRange
        {
            RangeType = D3D12Api.DESCRIPTOR_RANGE_TYPE_SAMPLER,
            NumDescriptors = MaxTextures,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0
        };

        parameters[4] = new D3D12RootParameter
        {
            Type = D3D12Api.ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            DescriptorTable = (IntPtr)ranges,
            NumDescriptorRanges = 1,
            ShaderVisibility = D3D12Api.SHADER_VISIBILITY_ALL
        };

        parameters[5] = new D3D12RootParameter
        {
            Type = D3D12Api.ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            DescriptorTable = (IntPtr)(ranges + 1),
            NumDescriptorRanges = 1,
            ShaderVisibility = D3D12Api.SHADER_VISIBILITY_ALL
        };

        var description = new D3D12RootSignatureDesc
        {
            ParameterCount = 6,
            Parameters = parameters,
            Flags = D3D12Api.ROOT_SIGNATURE_FLAGS_NONE
        };

        var serialize = D3D12Api.SerializeRootSignature(&description, out var blob, out var error);
        if (serialize < 0 || blob == 0)
        {
            if (error != 0)
            {
                D3D12Api.Release(error);
            }

            NativeInterop.Free(ranges);
            return false;
        }

        var result = D3D12Api.CreateRootSignature(m_Device, D3D12Api.BlobData(blob), D3D12Api.BlobSize(blob), out m_RootSignature);
        D3D12Api.Release(blob);
        NativeInterop.Free(ranges);

        if (error != 0)
        {
            D3D12Api.Release(error);
        }

        return result >= 0 && m_RootSignature != 0;
    }

    /// <summary>
    /// Releases the fence, the queue, the allocator, the list, the pipeline and the root signature.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        WaitPending();

        if (m_Pipeline != 0)
        {
            D3D12Api.Release(m_Pipeline);
        }

        if (m_RootSignature != 0)
        {
            D3D12Api.Release(m_RootSignature);
        }

        D3D12Api.Release(m_Fence);
        D3D12Api.Release(m_List);
        D3D12Api.Release(m_Allocator);
        D3D12Api.Release(m_Queue);
    }
}

