using Gubbins.Graphic.D3D12;
using Gubbins.Graphic.Metal;

namespace Gubbins.Graphic;

/// <summary>
/// One command buffer that groups several kernel dispatches: Metal, D3D12 and Vulkan record them and submit the group once,
/// while OpenGL and CUDA run each record immediately as a synchronous fallback.
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
/// Two dispatches in one batch are only ordered against each other when <see cref="Barrier"/> is called between them: the
/// barrier publishes the writes of the records before it to the reads and writes of the records after it, which is what
/// lets a record consume the output of the previous record. Metal needs no such call and records nothing for it, because
/// the driver tracks the hazards of an encoder's dispatches itself.
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
/// Metal, D3D12 and Vulkan queue the records and submit them once. A Vulkan batch owns one <c>VkCommandPool</c> and one
/// primary <c>VkCommandBuffer</c>, begins it once, records every dispatch into it, ends it once, submits it once to the
/// kernel's queue and signals one fence that <see cref="Wait"/> waits for. The descriptor set of each record comes from a
/// pool the batch owns, because every record has to keep its own bindings until the batch has run; those pools are only
/// destroyed once the fence has passed. The sampled images are bound in the layout they already hold, since the upload
/// leaves them sampleable and the single dispatch path performs no transition either.
/// </para>
/// <para>
/// OpenGL and CUDA are a synchronous fallback, not a queued batch. Neither backend has a submission this layer can record
/// into and submit once, so <see cref="Record"/> runs the dispatch immediately through the kernel's single-dispatch path
/// and waits for it, then returns that dispatch's result. A caller gets the command buffer shape on every backend, but a
/// synchronous command buffer buys no batching: it costs exactly one dispatch, one completion wait and one synchronization
/// per record, the same as calling <see cref="GpuKernel.TryDispatch(GpuBlock, GpuBlock, uint, GpuBlock?, GpuBlock?, bool)"/>
/// in a loop. Because every record has already completed, <see cref="Barrier"/> has nothing to order and returns
/// <see langword="true"/>, <see cref="Commit"/> and <see cref="Wait"/> have nothing to submit and succeed, and
/// <see cref="Dispose"/> releases nothing. The blocks, textures and samplers of a synchronous record only have to stay
/// alive for the call, unlike the queued backends whose resources must outlive the commit.
/// </para>
/// <para>
/// <see cref="GpuKernel.CreateCommandBuffer"/> returns a command buffer for every backend a kernel can be loaded on, so a
/// caller does not need to branch on a description string to use the batched shape.
/// </para>
/// </remarks>
public sealed class GpuCommandBuffer : IDisposable
{
    private readonly MetalCompute? m_Metal;
    private readonly D3D12Compute? m_D3D12;
    private readonly VulkanCompute? m_Vulkan;

    /// <summary>
    /// The kernel that seeded a synchronous fallback, which is what tells <see cref="Record"/> that every dispatch runs
    /// immediately instead of being recorded for a later submission.
    /// </summary>
    private readonly GpuKernel? m_Synchronous;

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
    /// <exception cref="NotSupportedException">Thrown when the kernel has no backend this layer can dispatch.</exception>
    /// <exception cref="MetalException">Thrown when Metal refuses to create the command buffer.</exception>
    /// <exception cref="D3D12Exception">Thrown when D3D12 refuses to create the allocator, the list or the fence.</exception>
    /// <exception cref="VulkanException">Thrown when Vulkan refuses to create the command pool, the command buffer or the fence.</exception>
    /// <remarks>
    /// A Metal, D3D12 or Vulkan kernel gets a queued batch. An OpenGL or CUDA kernel gets the synchronous fallback, which
    /// allocates nothing and runs every record as it is made.
    /// </remarks>
    public GpuCommandBuffer(GpuKernel kernel)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        // OpenGL and CUDA cannot record into a submission this layer owns and submits once, so their command buffer is the
        // synchronous fallback: Record dispatches and waits immediately, and this object owns no native state.
        if (kernel.OpenGL is not null || kernel.Cuda is not null)
        {
            m_Synchronous = kernel;
            return;
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

        throw new NotSupportedException("The kernel has no backend a command buffer can dispatch.");
    }

    /// <summary>
    /// Records one dispatch into the batch, without submitting it. On the synchronous fallback the dispatch runs and is
    /// waited for before this method returns.
    /// </summary>
    /// <param name="kernel">
    /// The kernel to run. It has to be on the same Metal queue or Vulkan queue as this buffer, on the same D3D12 device the
    /// batch submits for, or on the same synchronous backend (OpenGL or CUDA) the buffer was created for.
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
    /// <returns>
    /// <see langword="true"/> if the dispatch was recorded, or ran and completed on a synchronous backend.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="kernel"/>, <paramref name="source"/> or <paramref name="destination"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    /// <remarks>
    /// A queued record only asks for the dispatch: it returns before the kernel runs, and the blocks, textures and samplers
    /// it binds have to stay alive until the commit has completed. A synchronous record is the opposite: the dispatch has
    /// already run and completed by the time it returns, and its resources only had to be alive for the call.
    /// </remarks>
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

        // The synchronous fallback has no encoder to fill: the record runs the dispatch now and returns its result.
        if (m_Synchronous is not null)
        {
            return RecordSynchronously(kernel, source, destination, groups, lengths, parameters, textures);
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
    /// Runs one record of the synchronous fallback immediately and waits for it.
    /// </summary>
    /// <param name="kernel">The kernel to run, which has to be on the fallback's own backend.</param>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="textures">The textures and samplers to bind, or <see langword="null"/> when the kernel samples none.</param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    /// <remarks>
    /// This is the single-dispatch path, not a recording: the textures go through the multi-texture dispatch of the backend
    /// and everything else goes through the backend-neutral one, with <c>wait</c> set because a record of a synchronous
    /// fallback has completed by the time it returns. A kernel of the other synchronous backend is refused, exactly like a
    /// kernel of another queue or device is refused by a queued batch.
    /// </remarks>
    private bool RecordSynchronously(GpuKernel kernel, GpuBlock source, GpuBlock destination, uint groups, GpuBlock? lengths, GpuBlock? parameters, IReadOnlyList<GpuTextureBinding>? textures)
    {
        var hasTextures = textures is { Count: > 0 };

        if (m_Synchronous!.OpenGL is not null)
        {
            if (kernel.OpenGL is null)
            {
                return false;
            }

            if (!hasTextures)
            {
                return kernel.TryDispatch(source, destination, groups, lengths, parameters, wait: true);
            }

            return kernel.TryDispatchOpenGlTextures(source, destination, lengths, parameters, textures as GpuTextureBinding[] ?? [.. textures!], groups, wait: true);
        }

        if (m_Synchronous.Cuda is not null)
        {
            if (kernel.Cuda is null)
            {
                return false;
            }

            if (!hasTextures)
            {
                return kernel.TryDispatch(source, destination, groups, lengths, parameters, wait: true);
            }

            return kernel.TryDispatchCudaTextures(source, destination, lengths, parameters, textures as GpuTextureBinding[] ?? [.. textures!], groups, wait: true);
        }

        return false;
    }

    /// <summary>
    /// Records a barrier that makes the writes of the records so far visible to the records recorded after it.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the barrier was recorded; <see langword="false"/> when the batch has already been
    /// submitted, because a closed command list or encoder accepts nothing more.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    /// <remarks>
    /// <para>
    /// A batch is one submission, not one dispatch: nothing orders the writes of a record against the reads of the record
    /// after it unless a barrier is recorded between them. Call this after the record that produces a block and before the
    /// record that consumes it, which is the batched equivalent of two separate submissions with a wait in between.
    /// </para>
    /// <para>
    /// On Metal the call is a no-op that returns <see langword="true"/>, because the driver tracks the hazard between the
    /// dispatches of one encoder. On D3D12 it records a global unordered access barrier, which orders every unordered
    /// access write before it against every unordered access read and write after it. On Vulkan it records a
    /// compute-to-compute memory barrier with shader-write as the source access mask and shader-read plus shader-write as
    /// the destination one.
    /// </para>
    /// <para>
    /// On the OpenGL and CUDA synchronous fallback the call is a no-op that returns <see langword="true"/>: every record
    /// has already run and been waited for, so the writes of one record are visible to the next record without a barrier.
    /// </para>
    /// </remarks>
    public unsafe bool Barrier()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuCommandBuffer));
        }

        if (m_Committed)
        {
            return false;
        }

        // A synchronous fallback completed every record as it was made, so there is no hazard left to order.
        if (m_Synchronous is not null)
        {
            return true;
        }

        // Metal tracks read-after-write hazards inside one compute encoder, so the record and this call record nothing.
        if (m_Metal is not null)
        {
            return m_Encoder != 0;
        }

        if (m_Vulkan is not null)
        {
            if (m_VulkanCommandBuffer == 0)
            {
                return false;
            }

            // Every record of a Vulkan batch is a compute dispatch, so the dependency is compute stage to compute stage: the
            // shader writes of the records before the barrier are published to the shader reads and writes after it.
            var barrier = new VkMemoryBarrier
            {
                StructureType = VulkanApi.MEMORY_BARRIER_TYPE,
                SourceAccessMask = VulkanApi.ACCESS_SHADER_WRITE,
                DestinationAccessMask = VulkanApi.ACCESS_SHADER_READ | VulkanApi.ACCESS_SHADER_WRITE
            };

            VulkanApi.CmdPipelineBarrier(
                m_VulkanCommandBuffer,
                VulkanApi.PIPELINE_STAGE_COMPUTE_SHADER,
                VulkanApi.PIPELINE_STAGE_COMPUTE_SHADER,
                0,
                1,
                &barrier,
                0,
                0,
                0,
                0);

            return true;
        }

        if (m_D3D12 is not null)
        {
            if (m_List == 0)
            {
                return false;
            }

            // A UAV barrier with no resource is the global form: it orders the unordered access writes the list has already
            // recorded against the unordered access reads and writes that come after it, whichever resource they address.
            var barrier = new D3D12ResourceBarrier
            {
                Type = D3D12Api.RESOURCE_BARRIER_TYPE_UAV
            };

            D3D12Api.ResourceBarrier(m_List, &barrier);
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
    /// <remarks>
    /// On the OpenGL and CUDA synchronous fallback there is nothing to end or submit, because every record already ran and
    /// completed: the call only marks the buffer committed, succeeds on the first call and returns <see langword="false"/>
    /// on the next ones, like the queued backends.
    /// </remarks>
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

        // The synchronous fallback submitted each record when it was made, so the commit has nothing left to submit.
        if (m_Synchronous is not null)
        {
            m_Committed = true;
            return true;
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
    /// submitted yet. The synchronous fallback has no submission, so it always returns <see langword="true"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the buffer was disposed.</exception>
    /// <remarks>
    /// Waiting a second time is not an error: the submission has already completed, so there is nothing to do. On the
    /// OpenGL and CUDA synchronous fallback there is never anything to wait for, so the call succeeds even before the
    /// buffer is committed.
    /// </remarks>
    public bool Wait()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuCommandBuffer));
        }

        // Every record of a synchronous fallback already ran and completed, whether or not the buffer was committed.
        if (m_Synchronous is not null)
        {
            return true;
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
    /// the fence that covers them has passed. The OpenGL and CUDA synchronous fallback owns no native objects and every
    /// record already ran and completed, so disposing it only marks it disposed.
    /// </remarks>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        // A synchronous fallback has no uncommitted work and no native state: every record already ran and completed.
        if (m_Synchronous is not null)
        {
            m_Disposed = true;
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
