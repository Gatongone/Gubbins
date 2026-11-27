using Gubbins.Graphic.D3D12;
using Gubbins.Graphic.Metal;

namespace Gubbins.Graphic;

/// <summary>
/// One command buffer that records several kernel dispatches and submits them to the device once.
/// </summary>
/// <remarks>
/// <para>
/// A single dispatch pays for a command buffer, a compute encoder, a commit and a wait. A batch pays for those once: every
/// dispatch is recorded into the same encoder, with the pipeline state switched between kernels, and <see cref="Commit"/>
/// is the only submission. On Metal that is one <c>MTLCommandBuffer</c> and one <c>MTLComputeCommandEncoder</c> for the
/// whole group; on D3D12 it is one command allocator, one compute command list and one fence; on Vulkan it is one command
/// pool with one primary command buffer and one fence.
/// </para>
/// <para>
/// A record is only a request: it returns <see langword="false"/> when the kernel is not on the same device and queue as
/// the buffer, when a block has no native buffer behind it, when a texture binding belongs to another backend, or when the
/// batch has already been submitted. Every kernel, block, texture and sampler recorded into the batch has to stay alive
/// until the commit has completed, because the encoder or command list binds their handles and runs after the call that
/// recorded them has returned. A D3D12 record also claims the states its barriers move the textures into, so a resource a
/// batch recorded must not be used by another submission until the batch has completed.
/// </para>
/// <para>
/// The D3D12 batch borrows the compute queue of the kernel that seeded it, exposed internally by
/// <c>D3D12Compute</c>, so the group is submitted on the queue that kernel already uses. A D3D12 command list is not bound
/// to the queue that executes it, so a kernel loaded on the same <c>ID3D12Device</c> can be recorded into the batch even
/// though it owns a queue of its own; a kernel of another device, or of another backend, is refused because the resources
/// it binds belong to a device the submission never reaches. The seeding kernel has to outlive the batch, exactly like
/// every other kernel recorded into it.
/// </para>
/// <para>
/// Metal, D3D12 and Vulkan are the backends with this implementation today. A Vulkan batch owns one <c>VkCommandPool</c>
/// and one primary <c>VkCommandBuffer</c>, begins it once, records every dispatch into it, ends it once, submits it once to
/// the kernel's queue and signals one fence that <see cref="Wait"/> waits for. The descriptor set of each record comes from
/// a pool the batch owns, because every record has to keep its own bindings until the batch has run; those pools are only
/// destroyed once the fence has passed. The sampled images are bound in the layout they already hold, since the upload
/// leaves them sampleable and the single dispatch path performs no transition either.
/// </para>
/// <para>
/// <see cref="GpuKernel.CreateCommandBuffer"/> returns <see langword="null"/> on the backends without this implementation,
/// and the constructor refuses their kernels, so a caller can feature detect instead of branching on a description string.
/// </para>
/// </remarks>
public sealed class GpuCommandBuffer : IDisposable
{
    private readonly MetalCompute? m_Metal;
    private readonly D3D12Compute? m_D3D12;
    private readonly VulkanCompute? m_Vulkan;
    private readonly nint m_Queue;
    private readonly nint m_Device;

    /// <summary>
    /// The combined descriptor heaps of the D3D12 records, released once the batch fence passes them.
    /// </summary>
    private readonly List<nint> m_PendingHeaps = [];

    /// <summary>
    /// The descriptor pools of the Vulkan records, released once the batch fence passes them.
    /// </summary>
    private readonly List<nint> m_VulkanDescriptorPools = [];

    private nint m_CommandBuffer;
    private nint m_Encoder;
    private nint m_Allocator;
    private nint m_List;
    private nint m_Fence;
    private ulong m_FenceValue;
    private nint m_VulkanCommandPool;
    private nint m_VulkanCommandBuffer;
    private nint m_VulkanFence;
    private nint m_VulkanDescriptorPool;
    private bool m_VulkanSubmitted;
    private bool m_VulkanFenceSignaled;
    private bool m_Committed;
    private bool m_Disposed;

    /// <summary>
    /// The number of records one Vulkan descriptor pool holds sets for before the batch creates another one.
    /// </summary>
    private const int VulkanDescriptorSetsPerPool = 16;

    /// <summary>
    /// Creates a command buffer on the queue of the device <paramref name="kernel"/> was loaded on.
    /// </summary>
    /// <param name="kernel">A kernel on the device the batch records for, which may or may not be recorded into it.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="kernel"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown when the kernel's backend has no batched command buffer.</exception>
    /// <exception cref="MetalException">Thrown when Metal refuses to create the command buffer.</exception>
    /// <exception cref="D3D12Exception">Thrown when D3D12 refuses to create the allocator, the list or the fence.</exception>
    /// <exception cref="VulkanException">Thrown when Vulkan refuses to create the command pool, the command buffer or the fence.</exception>
    public GpuCommandBuffer(GpuKernel kernel)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        if (kernel.Metal is { } metal)
        {
            if (!metal.BeginCommandBuffer(out var commandBuffer, out var encoder))
            {
                throw new MetalException("Metal refused to create a command buffer.");
            }

            m_Metal = metal;
            m_Queue = metal.Queue;
            m_CommandBuffer = commandBuffer;
            m_Encoder = encoder;
            return;
        }

        if (kernel.Vulkan is { } vulkan)
        {
            m_Vulkan = vulkan;
            m_Queue = vulkan.Queue;
            m_Device = vulkan.Device;

            try
            {
                CreateVulkanCommandBuffer(vulkan);
            }
            catch
            {
                ReleaseVulkanObjects();
                throw;
            }

            return;
        }

        if (kernel.D3D12 is { } d3d12)
        {
            m_D3D12 = d3d12;
            m_Queue = d3d12.Queue;
            m_Device = d3d12.Device;

            try
            {
                if (D3D12Api.CreateCommandAllocator(m_Device, D3D12Api.COMMAND_LIST_TYPE_COMPUTE, out m_Allocator) < 0 || m_Allocator == 0)
                {
                    throw new D3D12Exception("CreateCommandAllocator failed for the batched command buffer.");
                }

                // The list is handed back recording, and the batch keeps recording into it until Commit closes it once.
                if (D3D12Api.CreateCommandList(m_Device, m_Allocator, D3D12Api.COMMAND_LIST_TYPE_COMPUTE, out m_List) < 0 || m_List == 0)
                {
                    throw new D3D12Exception("CreateCommandList failed for the batched command buffer.");
                }

                if (D3D12Api.CreateFence(m_Device, out m_Fence) < 0 || m_Fence == 0)
                {
                    throw new D3D12Exception("CreateFence failed for the batched command buffer.");
                }
            }
            catch
            {
                D3D12Api.Release(m_Fence);
                D3D12Api.Release(m_List);
                D3D12Api.Release(m_Allocator);
                m_Fence = 0;
                m_List = 0;
                m_Allocator = 0;
                throw;
            }

            return;
        }

        throw new NotSupportedException("A batched command buffer is only implemented for Metal, D3D12 and Vulkan.");
    }

    /// <summary>
    /// Records one dispatch into the batch, without submitting it.
    /// </summary>
    /// <param name="kernel">
    /// The kernel to run. It has to be on the same Metal queue or Vulkan queue as this buffer, or on the same D3D12 device
    /// the batch submits for.
    /// </param>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="lengths">
    /// The block that holds one element count per buffer, for a kernel that reads <c>Span.Length</c>; <see langword="null"/>
    /// when the kernel does not.
    /// </param>
    /// <param name="parameters">
    /// The block that holds the structure the kernel takes as one value; <see langword="null"/> when the kernel takes none.
    /// </param>
    /// <param name="textures">
    /// The textures and samplers the kernel samples, in the order it declares them; <see langword="null"/> when it samples
    /// none.
    /// </param>
    /// <returns><see langword="true"/> if the dispatch was recorded.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="kernel"/>, <paramref name="source"/> or <paramref name="destination"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    public bool Record(GpuKernel kernel, GpuBlock source, GpuBlock destination, uint groups, GpuBlock? lengths = null, GpuBlock? parameters = null, IReadOnlyList<GpuTextureBinding>? textures = null)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuCommandBuffer));
        }

        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        if (source is null || destination is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(destination));
        }

        // A command buffer takes the encoders of its own queue only, and a submitted one has nothing left to record into.
        if (m_Committed || groups == 0)
        {
            return false;
        }

        if (m_Metal is not null)
        {
            if (m_Encoder == 0 || kernel.Metal is not { } metal || metal.Queue != m_Queue)
            {
                return false;
            }

            return kernel.TryRecord(m_Encoder, source, destination, groups, lengths, parameters, textures);
        }

        if (m_Vulkan is not null)
        {
            // A command pool belongs to a queue family, and a batch submits on the queue of the kernel that seeded it, so a
            // kernel that would run on another queue is refused even when it shares the device.
            if (m_VulkanCommandBuffer == 0 || kernel.Vulkan is not { IsReady: true } vulkan ||
                vulkan.Device != m_Device || vulkan.Queue != m_Queue || vulkan.QueueFamily != m_Vulkan.QueueFamily)
            {
                return false;
            }

            if (!TryCreateVulkanDescriptorSet(vulkan, out var descriptorSet))
            {
                return false;
            }

            return kernel.TryRecordVulkan(m_VulkanCommandBuffer, descriptorSet, source, destination, groups, lengths, parameters, textures);
        }

        if (m_D3D12 is not null)
        {
            // The batch submits on the queue of the kernel that seeded it, and a D3D12 command list is not bound to the
            // queue that executes it, so any kernel of the same device records into it. Another device would bind
            // resources the submission never reaches, which is the D3D12 form of a wrong queue.
            if (m_List == 0 || kernel.D3D12 is not { } d3d12 || d3d12.Device != m_Device)
            {
                return false;
            }

            if (!kernel.TryRecordD3D12(m_List, source, destination, groups, lengths, parameters, textures, out var srvHeap, out var samplerHeap))
            {
                return false;
            }

            // The list may still be reading the heaps after this call, so the batch owns them until its fence passes.
            if (srvHeap != 0)
            {
                m_PendingHeaps.Add(srvHeap);
            }

            if (samplerHeap != 0)
            {
                m_PendingHeaps.Add(samplerHeap);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Ends the encoding, submits the batch and, by default, waits for it to complete.
    /// </summary>
    /// <param name="wait">Whether to wait for the submission, which makes every destination readable.</param>
    /// <returns><see langword="true"/> if the batch was submitted; <see langword="false"/> when it already had been.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    public unsafe bool Commit(bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuCommandBuffer));
        }

        if (m_Committed)
        {
            return false;
        }

        if (m_Metal is not null)
        {
            if (m_Encoder != 0)
            {
                MetalApi.SendVoid(m_Encoder, "endEncoding");
                MetalApi.Release(m_Encoder);
                m_Encoder = 0;
            }

            MetalApi.SendVoid(m_CommandBuffer, "commit");
            m_Committed = true;

            return !wait || Wait();
        }

        if (m_Vulkan is not null)
        {
            m_Committed = true;

            if (m_VulkanCommandBuffer == 0 || VulkanApi.EndCommandBuffer(m_VulkanCommandBuffer) != VulkanApi.SUCCESS)
            {
                // Nothing reached the queue, so nothing can still be reading the descriptor sets.
                ReleaseVulkanDescriptorPools();
                return false;
            }

            if (!SubmitVulkan())
            {
                return false;
            }

            m_VulkanSubmitted = true;
            return !wait || Wait();
        }

        if (m_List != 0)
        {
            if (D3D12Api.CloseCommandList(m_List) < 0)
            {
                // The list never reached the queue, so nothing can still be reading its heaps.
                ReleaseDescriptorHeaps();
                m_Committed = true;
                return false;
            }

            D3D12Api.ExecuteCommandLists(m_Queue, m_List);
            m_FenceValue++;

            if (D3D12Api.Signal(m_Queue, m_Fence, m_FenceValue) < 0)
            {
                m_Committed = true;
                return false;
            }
        }

        m_Committed = true;

        return !wait || Wait();
    }

    /// <summary>
    /// Waits for a batch that was submitted without waiting.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the submission has completed; <see langword="false"/> when the batch has not been
    /// submitted yet.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    /// <remarks>Waiting a second time is not an error: the submission has already completed, so there is nothing to do.</remarks>
    public bool Wait()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuCommandBuffer));
        }

        if (!m_Committed)
        {
            return false;
        }

        if (m_Vulkan is not null)
        {
            // A commit that failed before the submission has nothing to wait for, and the fence is never signaled.
            if (!m_VulkanSubmitted)
            {
                return false;
            }

            if (!m_VulkanFenceSignaled)
            {
                if (!WaitForVulkanFence())
                {
                    return false;
                }

                m_VulkanFenceSignaled = true;
            }

            // The queue has passed the command buffer, so the descriptor sets it recorded can be released now.
            ReleaseVulkanDescriptorPools();
            return true;
        }

        if (m_D3D12 is not null)
        {
            if (m_FenceValue != 0 && !D3D12Api.WaitForFence(m_Fence, m_FenceValue))
            {
                return false;
            }

            // The queue has passed the list, so the descriptor heaps it recorded can be released now.
            ReleaseDescriptorHeaps();
            return true;
        }

        if (m_CommandBuffer != 0)
        {
            MetalApi.SendVoid(m_CommandBuffer, "waitUntilCompleted");
            MetalApi.Release(m_CommandBuffer);
            m_CommandBuffer = 0;
        }

        return true;
    }

    /// <summary>
    /// Submits whatever was recorded and waits for it, then releases the command buffer's native objects.
    /// </summary>
    /// <remarks>
    /// A batch that is disposed without being committed is still submitted, because discarding a frame's dispatches silently
    /// is never what the caller meant. The D3D12 descriptor heaps and the Vulkan descriptor pools are only released after
    /// the fence that covers them has passed.
    /// </remarks>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        try
        {
            if (!m_Committed)
            {
                Commit();
            }
            else
            {
                Wait();
            }
        }
        finally
        {
            m_Disposed = true;

            if (m_D3D12 is not null)
            {
                ReleaseDescriptorHeaps();
                D3D12Api.Release(m_Fence);
                D3D12Api.Release(m_List);
                D3D12Api.Release(m_Allocator);
                m_Fence = 0;
                m_List = 0;
                m_Allocator = 0;
            }

            if (m_Vulkan is not null)
            {
                ReleaseVulkanObjects();
            }
        }
    }

    /// <summary>
    /// Releases the combined descriptor heaps of the D3D12 records once the batch fence has passed them.
    /// </summary>
    private void ReleaseDescriptorHeaps()
    {
        foreach (var heap in m_PendingHeaps)
        {
            D3D12Api.Release(heap);
        }

        m_PendingHeaps.Clear();
    }

    /// <summary>
    /// Creates the command pool, the primary command buffer and the fence of a Vulkan batch, and begins recording.
    /// </summary>
    /// <param name="vulkan">The pipeline whose queue family and device the batch records for.</param>
    /// <exception cref="VulkanException">Thrown when the driver refuses one of the objects.</exception>
    private unsafe void CreateVulkanCommandBuffer(VulkanCompute vulkan)
    {
        var poolInfo = new VkCommandPoolCreateInfo
        {
            StructureType = VulkanApi.COMMAND_POOL_CREATE_INFO_TYPE,
            Flags = VulkanApi.COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER,
            QueueFamilyIndex = vulkan.QueueFamily
        };

        nint pool = 0;

        if (VulkanApi.CreateCommandPool(m_Device, &poolInfo, 0, &pool) != VulkanApi.SUCCESS || pool == 0)
        {
            throw new VulkanException("vkCreateCommandPool failed for the batched command buffer.");
        }

        m_VulkanCommandPool = pool;

        var allocateInfo = new VkCommandBufferAllocateInfo
        {
            StructureType = VulkanApi.COMMAND_BUFFER_ALLOCATE_INFO_TYPE,
            CommandPool = pool,
            Level = VulkanApi.COMMAND_BUFFER_LEVEL_PRIMARY,
            CommandBufferCount = 1
        };

        nint commandBuffer = 0;

        if (VulkanApi.AllocateCommandBuffers(m_Device, &allocateInfo, &commandBuffer) != VulkanApi.SUCCESS || commandBuffer == 0)
        {
            throw new VulkanException("vkAllocateCommandBuffers failed for the batched command buffer.");
        }

        m_VulkanCommandBuffer = commandBuffer;

        // The batch begins once and records every dispatch into the one command buffer until Commit ends it.
        var beginInfo = new VkCommandBufferBeginInfo { StructureType = VulkanApi.COMMAND_BUFFER_BEGIN_INFO_TYPE };

        if (VulkanApi.BeginCommandBuffer(commandBuffer, &beginInfo) != VulkanApi.SUCCESS)
        {
            throw new VulkanException("vkBeginCommandBuffer failed for the batched command buffer.");
        }

        var fenceInfo = new VkFenceCreateInfo { StructureType = VulkanApi.FENCE_CREATE_INFO_TYPE };
        nint fence = 0;

        if (VulkanApi.CreateFence(m_Device, &fenceInfo, 0, &fence) != VulkanApi.SUCCESS || fence == 0)
        {
            throw new VulkanException("vkCreateFence failed for the batched command buffer.");
        }

        m_VulkanFence = fence;
    }

    /// <summary>
    /// Submits the Vulkan batch's command buffer to the queue, with the one fence that signals its completion.
    /// </summary>
    /// <returns><see langword="true"/> if the submission was accepted.</returns>
    private unsafe bool SubmitVulkan()
    {
        var commandBuffer = m_VulkanCommandBuffer;
        var submitInfo = new VkSubmitInfo
        {
            StructureType = VulkanApi.SUBMIT_INFO_TYPE,
            CommandBufferCount = 1,
            CommandBuffers = (nint)(&commandBuffer)
        };

        return VulkanApi.QueueSubmit(m_Queue, 1, &submitInfo, m_VulkanFence) == VulkanApi.SUCCESS;
    }

    /// <summary>
    /// Waits for the Vulkan batch's fence, which the submission signals or never signals when it failed.
    /// </summary>
    /// <returns><see langword="true"/> if the fence has been signaled.</returns>
    private unsafe bool WaitForVulkanFence()
    {
        var fence = m_VulkanFence;
        return VulkanApi.WaitForFences(m_Device, 1, &fence, 1, VulkanApi.INFINITE_TIMEOUT) == VulkanApi.SUCCESS;
    }

    /// <summary>
    /// Allocates the descriptor set of one Vulkan record, growing the batch's pools when the current one is full.
    /// </summary>
    /// <param name="compute">The pipeline that is being recorded.</param>
    /// <param name="descriptorSet">When the method returns, the <c>VkDescriptorSet</c> the record writes.</param>
    /// <returns><see langword="true"/> if a set was allocated.</returns>
    /// <remarks>
    /// A set belongs to one record because the command buffer runs every dispatch after the recording calls returned, so a
    /// set shared by records would make them read the bindings of the last one. Each pool is sized for this pipeline's four
    /// storage buffers and sampled images; a pool that runs out is left alive for the records already using it, and the next
    /// record uses a new one.
    /// </remarks>
    private bool TryCreateVulkanDescriptorSet(VulkanCompute compute, out nint descriptorSet)
    {
        if (m_VulkanDescriptorPool != 0 && compute.AllocateRecordDescriptorSet(m_VulkanDescriptorPool, out descriptorSet))
        {
            return true;
        }

        if (!compute.CreateRecordDescriptorPool(VulkanDescriptorSetsPerPool, out var pool))
        {
            descriptorSet = 0;
            return false;
        }

        m_VulkanDescriptorPools.Add(pool);
        m_VulkanDescriptorPool = pool;

        return compute.AllocateRecordDescriptorSet(pool, out descriptorSet);
    }

    /// <summary>
    /// Releases the Vulkan descriptor pools of the records once the batch fence has passed them.
    /// </summary>
    private unsafe void ReleaseVulkanDescriptorPools()
    {
        foreach (var pool in m_VulkanDescriptorPools)
        {
            VulkanApi.DestroyDescriptorPool(m_Device, pool, 0);
        }

        m_VulkanDescriptorPools.Clear();
        m_VulkanDescriptorPool = 0;
    }

    /// <summary>
    /// Releases every native object a Vulkan batch owns, after the device has drained the submission.
    /// </summary>
    private unsafe void ReleaseVulkanObjects()
    {
        // A pool destroyed while a submission still reads its sets is invalid, so the device drains before anything goes.
        VulkanApi.DeviceWaitIdle(m_Device);
        ReleaseVulkanDescriptorPools();

        if (m_VulkanFence != 0)
        {
            VulkanApi.DestroyFence(m_Device, m_VulkanFence, 0);
        }

        if (m_VulkanCommandPool != 0)
        {
            // A failed Commit can leave the buffer recording; resetting the pool returns it to the initial state before the
            // pool and its buffer are destroyed.
            VulkanApi.ResetCommandPool(m_Device, m_VulkanCommandPool, 0);
            VulkanApi.DestroyCommandPool(m_Device, m_VulkanCommandPool, 0);
        }

        m_VulkanFence = 0;
        m_VulkanCommandPool = 0;
        m_VulkanCommandBuffer = 0;
        m_VulkanSubmitted = false;
        m_VulkanFenceSignaled = false;
    }
}
