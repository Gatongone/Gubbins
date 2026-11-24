
namespace Gubbins.Graphic;

/// <summary>
/// Runs one compute pipeline on a Vulkan device: a SPIR-V module bound to two storage buffers, dispatched on a command
/// buffer of its own and waited for with a fence.
/// </summary>
/// <remarks>
/// The device and the queue belong to the bridge; the module, the layouts, the pool, the command pool and the fence are
/// this object's and are released when it is disposed. Every native handle is created through a local and copied into the
/// field afterwards, because a field cannot be addressed directly.
/// </remarks>
internal sealed unsafe class VulkanCompute : IDisposable
{
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private nint m_Module;
    private nint m_SetLayout;
    private nint m_PipelineLayout;
    private nint m_Pipeline;
    private nint m_DescriptorPool;
    private nint m_DescriptorSet;
    private nint m_CommandPool;
    private nint m_CommandBuffer;
    private nint m_Fence;
    private bool m_Disposed;

    /// <summary>
    /// The size of the buffer that carries the lengths of the inputs.
    /// </summary>
    private const ulong LengthsSize = 16;

    /// <param name="device">The <c>VkDevice</c> to run on.</param>
    /// <param name="queue">The queue to submit to.</param>
    /// <param name="queueFamily">The family of that queue.</param>
    internal VulkanCompute(nint device, nint queue, uint queueFamily)
    {
        m_Device = device;
        m_Queue = queue;

        var poolInfo = new VkCommandPoolCreateInfo
        {
            StructureType = VulkanApi.COMMAND_POOL_CREATE_INFO_TYPE,
            Flags = VulkanApi.COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER,
            QueueFamilyIndex = queueFamily
        };

        nint pool = 0;

        if (VulkanApi.CreateCommandPool(device, &poolInfo, 0, &pool) != VulkanApi.SUCCESS || pool == 0)
        {
            throw new VulkanException("vkCreateCommandPool failed for the compute pool.");
        }

        m_CommandPool = pool;

        var allocateInfo = new VkCommandBufferAllocateInfo
        {
            StructureType = VulkanApi.COMMAND_BUFFER_ALLOCATE_INFO_TYPE,
            CommandPool = pool,
            Level = VulkanApi.COMMAND_BUFFER_LEVEL_PRIMARY,
            CommandBufferCount = 1
        };

        nint buffer = 0;

        if (VulkanApi.AllocateCommandBuffers(device, &allocateInfo, &buffer) != VulkanApi.SUCCESS || buffer == 0)
        {
            throw new VulkanException("vkAllocateCommandBuffers failed for the compute buffer.");
        }

        m_CommandBuffer = buffer;

        var fenceInfo = new VkFenceCreateInfo { StructureType = VulkanApi.FENCE_CREATE_INFO_TYPE };
        nint fence = 0;

        if (VulkanApi.CreateFence(device, &fenceInfo, 0, &fence) != VulkanApi.SUCCESS || fence == 0)
        {
            throw new VulkanException("vkCreateFence failed for the compute fence.");
        }

        m_Fence = fence;
    }

    /// <summary>
    /// Loads a pipeline from SPIR-V.
    /// </summary>
    /// <param name="bytecode">The compiled shader.</param>
    /// <returns><see langword="true"/> if the pipeline was created.</returns>
    internal bool SetPipeline(scoped ReadOnlySpan<byte> bytecode)
    {
        if (m_Disposed || bytecode.Length == 0)
        {
            return false;
        }

        if (!CreateLayouts())
        {
            return false;
        }

        fixed (byte* code = bytecode)
        {
            var moduleInfo = new VkShaderModuleCreateInfo
            {
                StructureType = VulkanApi.SHADER_MODULE_CREATE_INFO_TYPE,
                CodeSize = (nuint)bytecode.Length,
                Code = (nint)code
            };

            nint module = 0;

            if (VulkanApi.CreateShaderModule(m_Device, &moduleInfo, 0, &module) != VulkanApi.SUCCESS || module == 0)
            {
                return false;
            }

            m_Module = module;
        }

        var entry = ReadEntryPoint(bytecode);

        if (entry == 0)
        {
            return false;
        }

        var stage = new VkPipelineShaderStageCreateInfo
        {
            StructureType = VulkanApi.PIPELINE_SHADER_STAGE_CREATE_INFO_TYPE,
            Stage = VulkanApi.SHADER_STAGE_COMPUTE,
            Module = m_Module,
            Name = entry
        };

        var creation = new VkComputePipelineCreateInfo
        {
            StructureType = VulkanApi.COMPUTE_PIPELINE_CREATE_INFO_TYPE,
            Stage = stage,
            Layout = m_PipelineLayout
        };

        nint pipeline = 0;

        if (VulkanApi.CreateComputePipelines(m_Device, 0, 1, &creation, 0, &pipeline) != VulkanApi.SUCCESS || pipeline == 0)
        {
            Gubbins.Graphic.NativeInterop.Free((void*)entry);
            return false;
        }

        Gubbins.Graphic.NativeInterop.Free((void*)entry);

        m_Pipeline = pipeline;
        return true;
    }

    /// <summary>
    /// Runs the loaded pipeline over two buffers.
    /// </summary>
    /// <param name="sourceBuffer">The <c>VkBuffer</c> the shader reads.</param>
    /// <param name="sourceOffset">The offset of the input inside it.</param>
    /// <param name="sourceSize">The size of the input.</param>
    /// <param name="destinationBuffer">The <c>VkBuffer</c> the shader writes.</param>
    /// <param name="destinationOffset">The offset of the output inside it.</param>
    /// <param name="destinationSize">The size of the output.</param>
    /// <param name="parametersBuffer">The <c>VkBuffer</c> that carries the structure the kernel takes, or zero when it takes none.</param>
    /// <param name="parametersOffset">The offset of that structure inside it.</param>
    /// <param name="parametersSize">The size of that structure.</param>
    /// <param name="lengthsBuffer">The <c>VkBuffer</c> that carries the lengths, or zero to reuse the input.</param>
    /// <param name="lengthsOffset">The offset of the lengths inside it.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    internal bool Dispatch(nint sourceBuffer, ulong sourceOffset, ulong sourceSize, nint destinationBuffer, ulong destinationOffset, ulong destinationSize, nint parametersBuffer, ulong parametersOffset, ulong parametersSize, nint lengthsBuffer, ulong lengthsOffset, uint groups)
    {
        if (m_Disposed || m_Pipeline == 0 || sourceBuffer == 0 || destinationBuffer == 0 || groups == 0)
        {
            return false;
        }

        if (!Bind(sourceBuffer, sourceOffset, sourceSize, destinationBuffer, destinationOffset, destinationSize, parametersBuffer, parametersOffset, parametersSize, lengthsBuffer, lengthsOffset))
        {
            return false;
        }

        var beginInfo = new VkCommandBufferBeginInfo { StructureType = VulkanApi.COMMAND_BUFFER_BEGIN_INFO_TYPE };

        if (VulkanApi.BeginCommandBuffer(m_CommandBuffer, &beginInfo) != VulkanApi.SUCCESS)
        {
            return false;
        }

        VulkanApi.CmdBindPipeline(m_CommandBuffer, VulkanApi.PIPELINE_BIND_POINT_COMPUTE, m_Pipeline);

        var set = m_DescriptorSet;
        VulkanApi.CmdBindDescriptorSets(m_CommandBuffer, VulkanApi.PIPELINE_BIND_POINT_COMPUTE, m_PipelineLayout, 0, 1, &set, 0, null);
        VulkanApi.CmdDispatch(m_CommandBuffer, groups, 1, 1);

        if (VulkanApi.EndCommandBuffer(m_CommandBuffer) != VulkanApi.SUCCESS)
        {
            return false;
        }

        var commandBuffer = m_CommandBuffer;
        var submitInfo = new VkSubmitInfo
        {
            StructureType = VulkanApi.SUBMIT_INFO_TYPE,
            CommandBufferCount = 1,
            CommandBuffers = (nint)(&commandBuffer)
        };

        if (VulkanApi.QueueSubmit(m_Queue, 1, &submitInfo, m_Fence) != VulkanApi.SUCCESS)
        {
            return false;
        }

        var fence = m_Fence;

        if (VulkanApi.WaitForFences(m_Device, 1, &fence, 1, ulong.MaxValue) != VulkanApi.SUCCESS)
        {
            return false;
        }

        VulkanApi.ResetFences(m_Device, 1, &fence);
        return true;
    }

    /// <summary>
    /// Builds the layouts, the pool and the descriptor set, once.
    /// </summary>
    /// <returns><see langword="true"/> if they all exist.</returns>
    private bool CreateLayouts()
    {
        if (m_DescriptorSet != 0)
        {
            return true;
        }

        var bindings = stackalloc VkDescriptorSetLayoutBinding[4];

        for (var index = 0; index < 4; index++)
        {
            bindings[index] = new VkDescriptorSetLayoutBinding
            {
                Binding = (uint)index,
                DescriptorType = VulkanApi.DESCRIPTOR_TYPE_STORAGE_BUFFER,
                DescriptorCount = 1,
                StageFlags = VulkanApi.SHADER_STAGE_COMPUTE
            };
        }

        var setLayoutInfo = new VkDescriptorSetLayoutCreateInfo
        {
            StructureType = VulkanApi.DESCRIPTOR_SET_LAYOUT_CREATE_INFO_TYPE,
            BindingCount = 4,
            Bindings = (nint)bindings
        };

        nint setLayout = 0;

        if (VulkanApi.CreateDescriptorSetLayout(m_Device, &setLayoutInfo, 0, &setLayout) != VulkanApi.SUCCESS || setLayout == 0)
        {
            return false;
        }

        m_SetLayout = setLayout;

        var layoutInfo = new VkPipelineLayoutCreateInfo
        {
            StructureType = VulkanApi.PIPELINE_LAYOUT_CREATE_INFO_TYPE,
            SetLayoutCount = 1,
            SetLayouts = (nint)(&setLayout)
        };

        nint pipelineLayout = 0;

        if (VulkanApi.CreatePipelineLayout(m_Device, &layoutInfo, 0, &pipelineLayout) != VulkanApi.SUCCESS || pipelineLayout == 0)
        {
            return false;
        }

        m_PipelineLayout = pipelineLayout;

        var poolSize = new VkDescriptorPoolSize
        {
            Type = VulkanApi.DESCRIPTOR_TYPE_STORAGE_BUFFER,
            DescriptorCount = 4
        };

        var poolInfo = new VkDescriptorPoolCreateInfo
        {
            StructureType = VulkanApi.DESCRIPTOR_POOL_CREATE_INFO_TYPE,
            MaxSets = 1,
            PoolSizeCount = 1,
            PoolSizes = (nint)(&poolSize)
        };

        nint pool = 0;

        if (VulkanApi.CreateDescriptorPool(m_Device, &poolInfo, 0, &pool) != VulkanApi.SUCCESS || pool == 0)
        {
            return false;
        }

        m_DescriptorPool = pool;

        var allocateInfo = new VkDescriptorSetAllocateInfo
        {
            StructureType = VulkanApi.DESCRIPTOR_SET_ALLOCATE_INFO_TYPE,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            SetLayouts = (nint)(&setLayout)
        };

        nint descriptorSet = 0;

        if (VulkanApi.AllocateDescriptorSets(m_Device, &allocateInfo, &descriptorSet) != VulkanApi.SUCCESS || descriptorSet == 0)
        {
            return false;
        }

        m_DescriptorSet = descriptorSet;
        return true;
    }

    /// <summary>
    /// Points the storage buffer bindings at the blocks of a dispatch.
    /// </summary>
    /// <returns><see langword="true"/> if the set was written.</returns>
    private bool Bind(nint sourceBuffer, ulong sourceOffset, ulong sourceSize, nint destinationBuffer, ulong destinationOffset, ulong destinationSize, nint parametersBuffer, ulong parametersOffset, ulong parametersSize, nint lengthsBuffer, ulong lengthsOffset)
    {
        if (!CreateLayouts())
        {
            return false;
        }

        var buffers = stackalloc VkDescriptorBufferInfo[4];
        buffers[0] = new VkDescriptorBufferInfo { Buffer = sourceBuffer, Offset = sourceOffset, Range = sourceSize };
        buffers[1] = new VkDescriptorBufferInfo { Buffer = destinationBuffer, Offset = destinationOffset, Range = destinationSize };

        // The block of the structure a kernel takes comes after the buffers, which is where the translation binds it, and
        // the counts are bound after it when it is there. A binding the kernel does not use is still written, so that the
        // set is complete.
        buffers[2] = parametersBuffer != 0
            ? new VkDescriptorBufferInfo { Buffer = parametersBuffer, Offset = parametersOffset, Range = parametersSize }
            : lengthsBuffer == 0
                ? new VkDescriptorBufferInfo { Buffer = sourceBuffer, Offset = sourceOffset, Range = sourceSize }
                : new VkDescriptorBufferInfo { Buffer = lengthsBuffer, Offset = lengthsOffset, Range = LengthsSize };

        buffers[3] = parametersBuffer != 0 && lengthsBuffer != 0
            ? new VkDescriptorBufferInfo { Buffer = lengthsBuffer, Offset = lengthsOffset, Range = LengthsSize }
            : new VkDescriptorBufferInfo { Buffer = sourceBuffer, Offset = sourceOffset, Range = sourceSize };

        var writes = stackalloc VkWriteDescriptorSet[4];

        for (var index = 0; index < 4; index++)
        {
            writes[index] = new VkWriteDescriptorSet
            {
                StructureType = VulkanApi.WRITE_DESCRIPTOR_SET_TYPE,
                DescriptorSet = m_DescriptorSet,
                DestinationBinding = (uint)index,
                DescriptorCount = 1,
                DescriptorType = VulkanApi.DESCRIPTOR_TYPE_STORAGE_BUFFER,
                BufferInfo = (nint)(&buffers[index])
            };
        }

        VulkanApi.UpdateDescriptorSets(m_Device, 4, writes, 0, 0);
        return true;
    }

    /// <summary>
    /// Releases the pipeline, the layouts, the pool, the command pool and the fence.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        VulkanApi.DeviceWaitIdle(m_Device);

        if (m_Pipeline != 0)
        {
            VulkanApi.DestroyPipeline(m_Device, m_Pipeline, 0);
        }

        if (m_DescriptorPool != 0)
        {
            VulkanApi.DestroyDescriptorPool(m_Device, m_DescriptorPool, 0);
        }

        if (m_PipelineLayout != 0)
        {
            VulkanApi.DestroyPipelineLayout(m_Device, m_PipelineLayout, 0);
        }

        if (m_SetLayout != 0)
        {
            VulkanApi.DestroyDescriptorSetLayout(m_Device, m_SetLayout, 0);
        }

        if (m_Module != 0)
        {
            VulkanApi.DestroyShaderModule(m_Device, m_Module, 0);
        }

        VulkanApi.DestroyFence(m_Device, m_Fence, 0);
        VulkanApi.DestroyCommandPool(m_Device, m_CommandPool, 0);
    }



    /// <summary>
    /// Reads the entry point name a SPIR-V module declares.
    /// </summary>
    /// <param name="bytecode">The module.</param>
    /// <returns>The name as a NUL terminated string the caller frees, or <see langword="0"/> when the module has no entry point.</returns>
    /// <remarks>
    /// The name comes out of the module instead of being assumed: what the compiler calls an entry point is its decision,
    /// and a pipeline created with the wrong name fails with an error that says nothing about the name.
    /// </remarks>
    private static nint ReadEntryPoint(scoped ReadOnlySpan<byte> bytecode)
    {
        const int HeaderWords = 5;
        const int EntryPointOpcode = 15;

        if (bytecode.Length < HeaderWords * sizeof(uint))
        {
            return 0;
        }

        var words = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, uint>(bytecode);

        for (var index = HeaderWords; index < words.Length;)
        {
            var instruction = words[index];
            var length = (int)(instruction >> 16);

            if (length == 0)
            {
                break;
            }

            if ((instruction & 0xFFFF) == EntryPointOpcode && length > 3)
            {
                // The name is the remainder of the instruction after the execution model and the function, and it ends at
                // the first zero byte; what follows it is the list of interface variables.
                var characters = new List<byte>();
                var terminated = false;

                for (var word = index + 3; word < index + length && !terminated; word++)
                {
                    for (var shift = 0; shift < 32; shift += 8)
                    {
                        var character = (byte)(words[word] >> shift);

                        if (character == 0)
                        {
                            terminated = true;
                            break;
                        }

                        characters.Add(character);
                    }
                }

                if (characters.Count == 0)
                {
                    return 0;
                }

                var name = (byte*)Gubbins.Graphic.NativeInterop.AllocZeroed((nuint)characters.Count + 1);

                for (var character = 0; character < characters.Count; character++)
                {
                    name[character] = characters[character];
                }

                return (nint)name;
            }

            index += length;
        }

        return 0;
    }
}

