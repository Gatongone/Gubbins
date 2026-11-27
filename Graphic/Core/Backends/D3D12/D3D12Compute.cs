
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
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nint m_Allocator;
    private readonly nint m_List;
    private readonly nint m_Fence;
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
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    internal bool Dispatch(nint sourceResource, ulong sourceAddress, nint destinationResource, ulong destinationAddress, ulong parametersAddress, ulong lengthsAddress, nint textureResource, int textureState, nint textureHeap, D3D12GpuDescriptorHandle srv, nint samplerHeap, D3D12GpuDescriptorHandle sampler, uint groups, bool wait)
    {
        if (m_Disposed || m_Pipeline == 0 || sourceResource == 0 || destinationResource == 0 || groups == 0)
        {
            return false;
        }

        if (m_Pending && !WaitPending())
        {
            return false;
        }

        if (D3D12Api.ResetCommandAllocator(m_Allocator) < 0 || D3D12Api.ResetCommandList(m_List, m_Allocator) < 0)
        {
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

        if (textureResource != 0 && textureState != D3D12Api.RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE)
        {
            var texture = new D3D12ResourceBarrier
            {
                Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
                Resource = textureResource,
                Subresource = unchecked((uint)D3D12Api.RESOURCE_BARRIER_ALL_SUBRESOURCES),
                StateBefore = textureState,
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

        if (textureHeap != 0 && samplerHeap != 0)
        {
            var heaps = stackalloc nint[2];
            heaps[0] = textureHeap;
            heaps[1] = samplerHeap;
            D3D12Api.SetDescriptorHeaps(m_List, 2, heaps);
            D3D12Api.SetComputeRootDescriptorTable(m_List, 4, srv);
            D3D12Api.SetComputeRootDescriptorTable(m_List, 5, sampler);
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
            return false;
        }

        D3D12Api.ExecuteCommandLists(m_Queue, m_List);

        m_FenceValue++;

        if (D3D12Api.Signal(m_Queue, m_Fence, m_FenceValue) < 0)
        {
            return false;
        }

        if (!wait)
        {
            m_Pending = true;
            return true;
        }

        return D3D12Api.WaitForFence(m_Fence, m_FenceValue);
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
        return true;
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
            NumDescriptors = 1,
            BaseShaderRegister = 0,
            RegisterSpace = 0,
            OffsetInDescriptorsFromTableStart = 0
        };

        ranges[1] = new D3D12DescriptorRange
        {
            RangeType = D3D12Api.DESCRIPTOR_RANGE_TYPE_SAMPLER,
            NumDescriptors = 1,
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

