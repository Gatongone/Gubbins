#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using Gubbins.Graphic;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic.Vulkan;

/// <summary>
/// A Vulkan implementation of <see cref="IUnifiedProvider"/>: it allocates <c>VkDeviceMemory</c> from the memory type that
/// suits each usage, binds one <c>VkBuffer</c> across the whole segment so blocks are addressable as buffer ranges, and
/// uses a transfer command buffer for the copies a device-only heap needs.
/// </summary>
/// <remarks>
/// <para>
/// The bridge does not create a device. The engine passes the <c>VkPhysicalDevice</c>, the <c>VkDevice</c> and a queue
/// that supports transfers, which is what a Godot <c>RenderingDevice</c> or a Unity native plugin already has. That also
/// means the caller owns device teardown: dispose this bridge before the device.
/// </para>
/// <para>
/// Each segment is one <c>VkDeviceMemory</c> plus one <c>VkBuffer</c> that spans it. Blocks are sub-ranges of that buffer,
/// which is how <c>vkCmdCopyBuffer</c> can fill a device-only block without the CPU ever touching it, and how a caller
/// binds a block with <c>(buffer, offset)</c>. Use <see cref="TryGetVulkanBuffer"/> and
/// <see cref="TryGetVulkanMemory"/> to get those handles back out of a block.
/// </para>
/// <para>
/// The memory-type selection and the structure layouts are unit tested; the provider itself needs a live device and is
/// exercised by the Vulkan harness. Run that harness on a new driver before trusting it: which types a vendor calls host
/// visible and coherent is the whole difference between a zero-copy arena and a staging copy on that machine.
/// </para>
/// </remarks>
public sealed unsafe class VulkanUnifiedProvider : IUnifiedProvider, IDisposable
{
    /// <summary>
    /// The fallback granularity for <c>vkFlushMappedMemoryRanges</c>, used when the caller does not pass the device's
    /// <c>nonCoherentAtomSize</c>. Rounding up is always correct, only slower.
    /// </summary>
    private const ulong DEFAULT_ATOM_SIZE = 4096;

    /// <summary>
    /// A pending copy, recorded from a <see cref="Copy"/> call and turned into a command when <see cref="Submit"/> runs.
    /// </summary>
    private struct CopyOp
    {
        public nint SourceBuffer;
        public nint DestinationBuffer;
        public ulong SourceOffset;
        public ulong DestinationOffset;
        public ulong Size;
    }

    /// <summary>
    /// The bookkeeping of one segment: the memory it owns, the buffer that spans it and the mapping when it has one.
    /// </summary>
    private struct Arena
    {
        /// <summary>
        /// The next, newer segment.
        /// </summary>
        internal Arena* Next;

        /// <summary>
        /// The <c>VkDeviceMemory</c> of the segment.
        /// </summary>
        public nint Memory;

        /// <summary>
        /// The <c>VkBuffer</c> that spans the segment.
        /// </summary>
        public nint Buffer;

        /// <summary>
        /// The persistent mapping of the segment, or <see langword="null"/> when it is not CPU visible.
        /// </summary>
        public byte* Host;

        /// <summary>
        /// The size the allocator asked for, in bytes.
        /// </summary>
        public nuint Size;

        /// <summary>
        /// The size that was really allocated, in bytes, which can be larger than <see cref="Size"/>.
        /// </summary>
        public nuint AllocationSize;

        /// <summary>
        /// The <c>VkMemoryPropertyFlags</c> of the memory type that was chosen.
        /// </summary>
        public uint MemoryFlags;

        /// <summary>
        /// The memory type index that was chosen, which is what tells a caller what the driver really offered.
        /// </summary>
        public uint MemoryTypeIndex;

        /// <summary>
        /// How the segment is shared between the CPU and the device.
        /// </summary>
        public UnifiedMemoryKind Kind;
    }

    private readonly nint m_PhysicalDevice;
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly uint m_QueueFamilyIndex;
    private readonly bool m_BufferDeviceAddress;
    private readonly nuint m_WorkingSetLimit;
    private readonly nuint m_Alignment;
    private readonly ulong m_AtomSize;
    private readonly VulkanMemoryTypeInfo m_MemoryTypes;

    /// <summary>
    /// The largest single heap, which is the largest segment the device can possibly serve.
    /// </summary>
    private readonly nuint m_MaxSegmentSize;

    private readonly List<CopyOp> m_Pending = [];

    private nint m_CommandPool;
    private bool m_OwnsCommandPool;
    private nint m_CommandBuffer;
    private nint m_Fence;
    private bool m_FencePending;
    private ulong m_FenceValue;

    private Arena* m_First;

    private GCHandle m_Self;

    private bool m_Disposed;

    /// <param name="physicalDevice">The <c>VkPhysicalDevice</c> the device was created from.</param>
    /// <param name="device">The <c>VkDevice</c> that owns every allocation this bridge makes.</param>
    /// <param name="queue">A queue that supports transfer operations.</param>
    /// <param name="queueFamilyIndex">The family of <paramref name="queue"/>.</param>
    /// <param name="commandPool">
    /// A command pool on <paramref name="queueFamilyIndex"/>, or <see langword="0"/> to let the bridge create and destroy
    /// its own.
    /// </param>
    /// <param name="bufferDeviceAddress">
    /// Whether the device was created with <c>bufferDeviceAddress</c> enabled, which lets blocks report a device address
    /// and lets the caller build buffers without a descriptor set.
    /// </param>
    /// <param name="workingSetLimit">
    /// The working set the caller wants reported, or <see langword="0"/>. Read it from <c>VK_EXT_memory_budget</c> when
    /// that extension is enabled.
    /// </param>
    /// <param name="alignment">
    /// The segment alignment the caller requires, or <see langword="0"/> for the system page size. Raise it to the
    /// device's <c>minImportedHostPointerAlignment</c> when the segments are going to be imported somewhere else.
    /// </param>
    /// <param name="atomSize">
    /// The device's <c>nonCoherentAtomSize</c>, or <see langword="0"/> for 4096. It is the granularity cache maintenance
    /// is rounded up to.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when a required handle is missing.</exception>
    /// <exception cref="VulkanException">Thrown when the Vulkan calls needed to set the bridge up fail.</exception>
    public VulkanUnifiedProvider(
        nint physicalDevice,
        nint device,
        nint queue,
        uint queueFamilyIndex,
        nint commandPool = 0,
        bool bufferDeviceAddress = false,
        nuint workingSetLimit = 0,
        nuint alignment = 0,
        ulong atomSize = 0)
    {
        if (physicalDevice == 0)
        {
            throw new ArgumentException("A VkPhysicalDevice handle is required.", nameof(physicalDevice));
        }

        if (device == 0)
        {
            throw new ArgumentException("A VkDevice handle is required.", nameof(device));
        }

        if (queue == 0)
        {
            throw new ArgumentException("A VkQueue handle is required.", nameof(queue));
        }

        m_PhysicalDevice = physicalDevice;
        m_Device = device;
        m_Queue = queue;
        m_QueueFamilyIndex = queueFamilyIndex;
        m_BufferDeviceAddress = bufferDeviceAddress;
        m_WorkingSetLimit = workingSetLimit;

        var page = (nuint)Environment.SystemPageSize;
        m_Alignment = alignment == 0 ? (page < 4096 ? 4096 : page) : alignment;
        m_AtomSize = atomSize == 0 ? DEFAULT_ATOM_SIZE : atomSize;

        VkPhysicalDeviceMemoryProperties properties;
        VulkanApi.GetPhysicalDeviceMemoryProperties(physicalDevice, &properties);
        m_MemoryTypes = VulkanApi.Read(&properties);
        if (m_MemoryTypes.TypeCount == 0)
        {
            throw new VulkanException("The physical device reported no memory types.");
        }

        // A single allocation must come from a single heap, so the largest heap is a sound upper bound. It is deliberately
        // permissive: the driver's own VkPhysicalDeviceLimits::maxMemoryAllocationSize is not mirrored, because a hand
        // written copy of that 400 byte structure would be a silent source of wrong limits, and a request that exceeds what
        // the driver really allows fails in vkAllocateMemory and surfaces as an out-of-memory exception.
        for (var type = 0; type < m_MemoryTypes.TypeCount; type++)
        {
            var heapSize = (nuint)m_MemoryTypes.HeapSize((int)m_MemoryTypes.HeapIndex(type));
            if (heapSize > m_MaxSegmentSize)
            {
                m_MaxSegmentSize = heapSize;
            }
        }

        if (commandPool != 0)
        {
            m_CommandPool = commandPool;
            m_OwnsCommandPool = false;
        }
        else
        {
            var poolInfo = new VkCommandPoolCreateInfo
            {
                StructureType = VulkanApi.COMMAND_POOL_CREATE_INFO_TYPE,
                Flags = VulkanApi.COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER,
                QueueFamilyIndex = queueFamilyIndex
            };

            nint pool;
            var result = VulkanApi.CreateCommandPool(device, &poolInfo, 0, &pool);
            if (result != VulkanApi.SUCCESS)
            {
                throw new VulkanException($"vkCreateCommandPool failed with {result}.", result);
            }

            m_CommandPool = pool;
            m_OwnsCommandPool = true;
        }

        // One primary command buffer is enough: the bridge records, submits and waits, so the buffer is free again
        // before the next submit.
        var allocateInfo = new VkCommandBufferAllocateInfo
        {
            StructureType = VulkanApi.COMMAND_BUFFER_ALLOCATE_INFO_TYPE,
            CommandPool = m_CommandPool,
            Level = VulkanApi.COMMAND_BUFFER_LEVEL_PRIMARY,
            CommandBufferCount = 1
        };

        nint commandBuffer;
        var allocateResult = VulkanApi.AllocateCommandBuffers(device, &allocateInfo, &commandBuffer);
        if (allocateResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkAllocateCommandBuffers failed with {allocateResult}.", allocateResult);
        }

        m_CommandBuffer = commandBuffer;

        var fenceInfo = new VkFenceCreateInfo { StructureType = VulkanApi.FENCE_CREATE_INFO_TYPE };
        nint fence;
        var fenceResult = VulkanApi.CreateFence(device, &fenceInfo, 0, &fence);
        if (fenceResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkCreateFence failed with {fenceResult}.", fenceResult);
        }

        m_Fence = fence;
    }

    /// <inheritdoc/>
    public nuint Alignment => m_Alignment;

    /// <inheritdoc/>
    /// <remarks>
    /// This is the largest heap the device reports, which is a sound upper bound because a single <c>vkAllocateMemory</c>
    /// cannot span two heaps. It is deliberately more permissive than
    /// <c>VkPhysicalDeviceLimits::maxMemoryAllocationSize</c>: mirroring that four hundred byte structure by hand would be a
    /// silent source of wrong limits, so a request above what the driver really allows is left to fail in
    /// <c>vkAllocateMemory</c>, which surfaces as an <see cref="OutOfMemoryException"/>.
    /// </remarks>
    public nuint MaxSegmentSize => m_MaxSegmentSize;

    /// <inheritdoc/>
    public nuint WorkingSetLimit => m_WorkingSetLimit;

    /// <summary>
    /// Gets a value indicating whether the device exposes memory that is device local, host visible, coherent and cached
    /// all at once, which is what makes the DRAM the CPU runs from the memory the device works in.
    /// </summary>
    public bool IsUnifiedMemory
    {
        get
        {
            const uint wanted = VulkanMemoryTypeInfo.DEVICE_LOCAL | VulkanMemoryTypeInfo.HOST_VISIBLE | VulkanMemoryTypeInfo.HOST_COHERENT | VulkanMemoryTypeInfo.HOST_CACHED;

            for (var type = 0; type < m_MemoryTypes.TypeCount; type++)
            {
                if ((m_MemoryTypes.Flags(type) & wanted) == wanted)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets the number of live segments.
    /// </summary>
    internal int SegmentCount
    {
        get
        {
            var count = 0;

            for (var arena = m_First; arena != null; arena = arena->Next)
            {
                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Queries the driver for the budget it reports per heap, which is the closest thing Vulkan has to a working set.
    /// </summary>
    /// <remarks>
    /// The caller has to enable <c>VK_EXT_memory_budget</c> at device creation and pass the result in as
    /// <c>workingSetLimit</c>; this helper only reads the numbers, because a bridge cannot tell whether an extension was
    /// enabled by the engine that created the device.
    /// </remarks>
    /// <param name="physicalDevice">The <c>VkPhysicalDevice</c> to query.</param>
    /// <param name="workingSetLimit">When the method returns, the sum of the heap budgets in bytes.</param>
    /// <returns><see langword="true"/> if the driver reported a budget; otherwise, <see langword="false"/>.</returns>
    internal static bool TryQueryWorkingSetLimit(nint physicalDevice, out nuint workingSetLimit)
    {
        VkPhysicalDeviceMemoryBudgetPropertiesExt budget = default;
        budget.StructureType = 1000237000; // VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_MEMORY_BUDGET_PROPERTIES_EXT

        var properties = new VkPhysicalDeviceMemoryProperties2
        {
            StructureType = VulkanApi.MEMORY_PROPERTIES2_TYPE,
            Next = (nint)(&budget)
        };

        VulkanApi.GetPhysicalDeviceMemoryProperties2(physicalDevice, &properties);

        ulong total = 0;
        for (var heap = 0; heap < 16; heap++)
        {
            total += budget.HeapBudget[heap];
        }

        workingSetLimit = (nuint)total;
        return total != 0;
    }

    /// <summary>
    /// Gets the number of memory types the physical device reports.
    /// </summary>
    /// <returns>The memory type count.</returns>
    internal int GetMemoryTypeCount() => m_MemoryTypes.TypeCount;

    /// <summary>
    /// Gets the <c>VkMemoryPropertyFlags</c> of a memory type, which is what the bridge selects on.
    /// </summary>
    /// <param name="type">The memory type index.</param>
    /// <returns>The property flags of that type.</returns>
    internal uint GetMemoryTypeFlags(int type) => m_MemoryTypes.Flags(type);

    /// <summary>
    /// Gets the size of the heap a memory type belongs to.
    /// </summary>
    /// <param name="type">The memory type index.</param>
    /// <returns>The heap size in bytes.</returns>
    internal ulong GetMemoryTypeHeapSize(int type) => m_MemoryTypes.HeapSize((int)m_MemoryTypes.HeapIndex(type));

    /// <inheritdoc/>
    public UnifiedProvider GetProvider() => ProviderAdapter<VulkanUnifiedProvider>.Create(this, ref m_Self);

    /// <inheritdoc/>
    /// <exception cref="VulkanException">Thrown when Vulkan refuses to create or bind the buffer, or to map the memory.</exception>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        ThrowIfDisposed();

        // The device address usage bit is only legal when the feature was enabled at device creation.
        var usageFlags = m_BufferDeviceAddress
            ? VulkanApi.BUFFER_USAGE_SHARED | VulkanApi.BUFFER_USAGE_SHADER_DEVICE_ADDRESS
            : VulkanApi.BUFFER_USAGE_SHARED;

        var bufferInfo = new VkBufferCreateInfo
        {
            StructureType = VulkanApi.BUFFER_CREATE_INFO_TYPE,
            Size = size,
            Usage = usageFlags,
            SharingMode = 0 // VK_SHARING_MODE_EXCLUSIVE
        };

        nint buffer;
        var bufferResult = VulkanApi.CreateBuffer(m_Device, &bufferInfo, 0, &buffer);
        if (bufferResult != VulkanApi.SUCCESS)
        {
            ThrowAllocationFailure($"vkCreateBuffer failed for a {size} byte segment", bufferResult);
        }

        VkMemoryRequirements requirements;
        VulkanApi.GetBufferMemoryRequirements(m_Device, buffer, &requirements);

        var memoryType = VulkanMemoryTypes.Select(m_MemoryTypes, requirements.MemoryTypeBits, usage);
        if (memoryType < 0)
        {
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException(
                $"No memory type can serve {usage} usage for a {size} byte buffer (compatible type bits 0x{requirements.MemoryTypeBits:X}).");
        }

        var flags = m_MemoryTypes.Flags(memoryType);
        kind = VulkanMemoryTypes.KindOf(flags);

        // Vulkan can require more memory than the buffer needs, for example for alignment; the tail is simply unused.
        var allocationSize = size > requirements.Size ? (ulong)size : requirements.Size;

        var allocateInfo = new VkMemoryAllocateInfo
        {
            StructureType = VulkanApi.MEMORY_ALLOCATE_INFO_TYPE,
            AllocationSize = allocationSize,
            MemoryTypeIndex = (uint)memoryType
        };

        // Calling vkGetBufferDeviceAddress on memory that was not allocated with this flag is undefined behaviour, so the
        // flag is part of enabling the feature rather than an optimisation.
        VkMemoryAllocateFlagsInfo allocateFlags = default;
        if (m_BufferDeviceAddress)
        {
            allocateFlags.StructureType = VulkanApi.MEMORY_ALLOCATE_FLAGS_INFO_TYPE;
            allocateFlags.Flags = VulkanApi.MEMORY_ALLOCATE_DEVICE_ADDRESS;
            allocateInfo.Next = (nint)(&allocateFlags);
        }

        nint memory;
        var allocateResult = VulkanApi.AllocateMemory(m_Device, &allocateInfo, 0, &memory);
        if (allocateResult != VulkanApi.SUCCESS)
        {
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            ThrowAllocationFailure($"vkAllocateMemory failed for {allocationSize} bytes from memory type {memoryType}", allocateResult);
        }

        var bindResult = VulkanApi.BindBufferMemory(m_Device, buffer, memory, 0);
        if (bindResult != VulkanApi.SUCCESS)
        {
            VulkanApi.FreeMemory(m_Device, memory, 0);
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException($"vkBindBufferMemory failed with {bindResult}.", bindResult);
        }

        // A device-only heap has no CPU address; everything else is mapped once and kept mapped, which is the whole point
        // of a persistent mapping.
        byte* host = null;
        if (kind != UnifiedMemoryKind.Device)
        {
            void* mapped;
            var mapResult = VulkanApi.MapMemory(m_Device, memory, 0, VulkanApi.WHOLE_SIZE, 0, &mapped);
            if (mapResult != VulkanApi.SUCCESS)
            {
                VulkanApi.FreeMemory(m_Device, memory, 0);
                VulkanApi.DestroyBuffer(m_Device, buffer, 0);
                throw new VulkanException($"vkMapMemory failed with {mapResult}.", mapResult);
            }

            host = (byte*)mapped;
        }

        var address = 0UL;
        if (m_BufferDeviceAddress)
        {
            var addressInfo = new VkBufferDeviceAddressInfo { StructureType = VulkanApi.BUFFER_DEVICE_ADDRESS_INFO_TYPE, Buffer = buffer };
            address = VulkanApi.GetBufferDeviceAddress(m_Device, &addressInfo);
        }

        var arena = (Arena*)NativeMemory.Alloc((nuint)sizeof(Arena));
        if (arena == null)
        {
            if (host != null)
            {
                VulkanApi.UnmapMemory(m_Device, memory);
            }

            VulkanApi.FreeMemory(m_Device, memory, 0);
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException($"Unable to allocate {sizeof(Arena)} bytes of bridge bookkeeping.");
        }

        *arena = default;
        arena->Memory = memory;
        arena->Buffer = buffer;
        arena->Host = host;
        arena->Size = size;
        arena->AllocationSize = (nuint)allocationSize;
        arena->MemoryFlags = flags;
        arena->MemoryTypeIndex = (uint)memoryType;
        arena->Kind = kind;
        arena->Next = m_First;
        m_First = arena;

        deviceHandle = arena;
        deviceAddress = address;
        return host;
    }

    /// <inheritdoc/>
    public void Free(void* segment, nuint size, void* deviceHandle)
    {
        if (m_Disposed)
        {
            return;
        }

        var arena = (Arena*)deviceHandle;
        if (arena == null)
        {
            for (var current = m_First; current != null; current = current->Next)
            {
                if (current->Host == (byte*)segment)
                {
                    arena = current;
                    break;
                }
            }
        }

        if (arena == null)
        {
            return;
        }

        Unlink(arena);
        DestroyArena(arena);
    }

    /// <inheritdoc/>
    public void Flush(void* ptr, nuint size)
    {
        if (m_Disposed || !TryResolveRange(ptr, size, out var memory, out var offset, out var range))
        {
            return;
        }

        var mappedRange = new VkMappedMemoryRange
        {
            StructureType = VulkanApi.MAPPED_MEMORY_RANGE_TYPE,
            Memory = memory,
            Offset = offset,
            Size = range
        };

        var result = VulkanApi.FlushMappedMemoryRanges(m_Device, 1, &mappedRange);
        if (result != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkFlushMappedMemoryRanges failed with {result}.", result);
        }
    }

    /// <inheritdoc/>
    public void Invalidate(void* ptr, nuint size)
    {
        if (m_Disposed || !TryResolveRange(ptr, size, out var memory, out var offset, out var range))
        {
            return;
        }

        var mappedRange = new VkMappedMemoryRange
        {
            StructureType = VulkanApi.MAPPED_MEMORY_RANGE_TYPE,
            Memory = memory,
            Offset = offset,
            Size = range
        };

        var result = VulkanApi.InvalidateMappedMemoryRanges(m_Device, 1, &mappedRange);
        if (result != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkInvalidateMappedMemoryRanges failed with {result}.", result);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The copy is recorded as a <c>vkCmdCopyBuffer</c> region, so both blocks must live in segments this bridge created.
    /// A deferred copy stays in a list until <see cref="Submit"/> turns the list into one command buffer.
    /// </remarks>
    public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        if (m_Disposed || size == 0)
        {
            return false;
        }

        var destinationArena = (Arena*)destination.DeviceHandle;
        var sourceArena = (Arena*)source.DeviceHandle;
        if (destinationArena == null || sourceArena == null)
        {
            return false;
        }

        m_Pending.Add(new CopyOp
        {
            SourceBuffer = sourceArena->Buffer,
            DestinationBuffer = destinationArena->Buffer,
            SourceOffset = (ulong)(source.Offset + sourceOffset),
            DestinationOffset = (ulong)(destination.Offset + destinationOffset),
            Size = size
        });

        if (defer)
        {
            return true;
        }

        return Submit() && Wait();
    }

    /// <inheritdoc/>
    /// <exception cref="VulkanException">Thrown when Vulkan refuses to record or submit the command buffer.</exception>
    public bool Submit()
    {
        if (m_Disposed || m_Pending.Count == 0)
        {
            return false;
        }

        var resetResult = VulkanApi.ResetCommandPool(m_Device, m_CommandPool, 0);
        if (resetResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkResetCommandPool failed with {resetResult}.", resetResult);
        }

        var beginInfo = new VkCommandBufferBeginInfo { StructureType = VulkanApi.COMMAND_BUFFER_BEGIN_INFO_TYPE };
        var beginResult = VulkanApi.BeginCommandBuffer(m_CommandBuffer, &beginInfo);
        if (beginResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkBeginCommandBuffer failed with {beginResult}.", beginResult);
        }

        foreach (var copy in m_Pending)
        {
            var region = new VkBufferCopy
            {
                SourceOffset = copy.SourceOffset,
                DestinationOffset = copy.DestinationOffset,
                Size = copy.Size
            };

            VulkanApi.CmdCopyBuffer(m_CommandBuffer, copy.SourceBuffer, copy.DestinationBuffer, 1, &region);
        }

        // The copies wrote through the transfer stage; publishing them to the host is what makes a following
        // vkInvalidateMappedMemoryRanges observe the bytes instead of a stale cache line.
        var barrier = new VkMemoryBarrier
        {
            StructureType = VulkanApi.MEMORY_BARRIER_TYPE,
            SourceAccessMask = VulkanApi.ACCESS_TRANSFER_WRITE,
            DestinationAccessMask = VulkanApi.ACCESS_HOST_READ | VulkanApi.ACCESS_HOST_WRITE
        };

        VulkanApi.CmdPipelineBarrier(
            m_CommandBuffer,
            VulkanApi.PIPELINE_STAGE_TRANSFER,
            VulkanApi.PIPELINE_STAGE_HOST,
            0,
            1,
            &barrier,
            0,
            0,
            0,
            0);

        var endResult = VulkanApi.EndCommandBuffer(m_CommandBuffer);
        if (endResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkEndCommandBuffer failed with {endResult}.", endResult);
        }

        var commandBuffer = m_CommandBuffer;
        var submitInfo = new VkSubmitInfo
        {
            StructureType = VulkanApi.SUBMIT_INFO_TYPE,
            CommandBufferCount = 1,
            CommandBuffers = (nint)(&commandBuffer)
        };

        var submitResult = VulkanApi.QueueSubmit(m_Queue, 1, &submitInfo, m_Fence);
        if (submitResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkQueueSubmit failed with {submitResult}.", submitResult);
        }

        m_FencePending = true;
        m_FenceValue++;
        m_Pending.Clear();
        return true;
    }

    /// <inheritdoc/>
    /// <exception cref="VulkanException">Thrown when the fence wait fails.</exception>
    public bool Wait()
    {
        if (m_Disposed || !m_FencePending)
        {
            return false;
        }

        nint fence = m_Fence;
        var waitResult = VulkanApi.WaitForFences(m_Device, 1, &fence, 1, VulkanApi.INFINITE_TIMEOUT);
        if (waitResult != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkWaitForFences failed with {waitResult}.", waitResult);
        }

        VulkanApi.ResetFences(m_Device, 1, &fence);
        m_FencePending = false;
        return true;
    }

    /// <summary>
    /// Gets the fence value the most recent <see cref="Submit"/> was tagged with.
    /// </summary>
    /// <remarks>
    /// Vulkan has one fence per submission here rather than a timeline, so the value is a counter this bridge keeps: once
    /// <see cref="Wait"/> has returned <see langword="true"/>, everything up to it has completed. That is the value a
    /// caller passes to <c>UnifiedAllocator.TryFreeWhenComplete</c>.
    /// </remarks>
    public ulong SubmittedFenceValue => m_FenceValue;

    /// <inheritdoc/>
    /// <remarks>
    /// Always <see langword="false"/>: residency is a driver or extension concern in Vulkan, and pretending otherwise
    /// would make a caller skip work it actually has to do.
    /// </remarks>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Gets the <c>VkBuffer</c> that spans the segment of <paramref name="block"/>, plus the block's offset inside it.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="buffer">When the method returns, the <c>VkBuffer</c> handle.</param>
    /// <param name="offset">When the method returns, the offset to bind or to copy at.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    public bool TryGetVulkanBuffer(in UnifiedMemoryBlock block, out nint buffer, out ulong offset)
    {
        var arena = (Arena*)block.DeviceHandle;
        if (arena == null)
        {
            buffer = 0;
            offset = 0;
            return false;
        }

        buffer = arena->Buffer;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Gets the memory type index of the segment that owns <paramref name="block"/>.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="type">When the method returns, the memory type index.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Pair it with <see cref="GetMemoryTypeFlags"/> to see what the driver actually offered for a usage, which is the
    /// difference between a zero-copy machine and one that has to reach its shared memory over the interconnect.
    /// </remarks>
    internal bool TryGetMemoryType(in UnifiedMemoryBlock block, out int type)
    {
        var arena = (Arena*)block.DeviceHandle;
        if (arena == null)
        {
            type = -1;
            return false;
        }

        type = (int)arena->MemoryTypeIndex;
        return true;
    }

    /// <summary>
    /// Gets the <c>VkDeviceMemory</c> of the segment that owns <paramref name="block"/>, plus the offset to bind at.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="memory">When the method returns, the <c>VkDeviceMemory</c> handle.</param>
    /// <param name="offset">When the method returns, the offset to bind at.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    public bool TryGetVulkanMemory(in UnifiedMemoryBlock block, out nint memory, out ulong offset)
    {
        var arena = (Arena*)block.DeviceHandle;
        if (arena == null)
        {
            memory = 0;
            offset = 0;
            return false;
        }

        memory = arena->Memory;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Releases every segment this bridge still owns and destroys the command pool and fence it created.
    /// </summary>
    /// <remarks>
    /// The <c>VkDevice</c> must still be alive. Disposing the bridge after the device is undefined behaviour, which is the
    /// usual ownership rule for a bridge that borrows a device.
    /// </remarks>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // A submitted copy may still be reading a segment, so the device has to be drained before anything is freed.
        if (m_FencePending)
        {
            VulkanApi.DeviceWaitIdle(m_Device);
            m_FencePending = false;
        }

        var arena = m_First;
        while (arena != null)
        {
            var next = arena->Next;
            DestroyArena(arena);
            arena = next;
        }

        m_First = null;

        if (m_Fence != 0)
        {
            VulkanApi.DestroyFence(m_Device, m_Fence, 0);
            m_Fence = 0;
        }

        if (m_CommandPool != 0 && m_OwnsCommandPool)
        {
            VulkanApi.DestroyCommandPool(m_Device, m_CommandPool, 0);
            m_CommandPool = 0;
        }

        if (m_Self.IsAllocated)
        {
            m_Self.Free();
        }
    }

    /// <summary>
    /// Maps a CPU range inside a mapped segment to the memory object and the cache maintenance range Vulkan wants.
    /// </summary>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    /// <param name="memory">When the method returns, the <c>VkDeviceMemory</c> of the segment.</param>
    /// <param name="offset">When the method returns, the offset of the range inside that memory, rounded down to the atom size.</param>
    /// <param name="range">When the method returns, the range size, rounded up to the atom size and clamped to the allocation.</param>
    /// <returns><see langword="true"/> if the range belongs to a mapped, non-coherent segment of this bridge.</returns>
    private bool TryResolveRange(void* ptr, nuint size, out nint memory, out ulong offset, out ulong range)
    {
        memory = 0;
        offset = 0;
        range = 0;

        if (ptr == null || size == 0)
        {
            return false;
        }

        var address = (nuint)ptr;

        for (var arena = m_First; arena != null; arena = arena->Next)
        {
            if (arena->Host == null || arena->Kind != UnifiedMemoryKind.Shared)
            {
                continue;
            }

            var start = (nuint)arena->Host;
            if (address < start || address - start >= (nuint)arena->AllocationSize)
            {
                continue;
            }

            var atom = m_AtomSize;
            var blockOffset = (ulong)(address - start);
            var rawEnd = blockOffset + size;
            var alignedOffset = blockOffset - (blockOffset % atom);
            var alignedEnd = rawEnd + ((atom - (rawEnd % atom)) % atom);
            var allocationEnd = (ulong)arena->AllocationSize;

            memory = arena->Memory;
            offset = alignedOffset;
            range = alignedEnd > allocationEnd ? allocationEnd - alignedOffset : alignedEnd - alignedOffset;
            return range != 0;
        }

        return false;
    }

    /// <summary>
    /// Unlinks a segment from the list.
    /// </summary>
    /// <param name="arena">The segment to unlink.</param>
    private void Unlink(Arena* arena)
    {
        if (m_First == arena)
        {
            m_First = arena->Next;
            return;
        }

        for (var current = m_First; current != null; current = current->Next)
        {
            if (current->Next == arena)
            {
                current->Next = arena->Next;
                return;
            }
        }
    }

    /// <summary>
    /// Releases the Vulkan objects of a segment and its bookkeeping.
    /// </summary>
    /// <param name="arena">The segment to release.</param>
    private void DestroyArena(Arena* arena)
    {
        if (arena->Host != null)
        {
            VulkanApi.UnmapMemory(m_Device, arena->Memory);
        }

        VulkanApi.DestroyBuffer(m_Device, arena->Buffer, 0);
        VulkanApi.FreeMemory(m_Device, arena->Memory, 0);
        NativeMemory.Free(arena);
    }

    /// <summary>
    /// Reports a failed allocation as <see cref="OutOfMemoryException"/>, which is what the allocator documents, and falls
    /// back to a <see cref="VulkanException"/> for the failures that are not about memory.
    /// </summary>
    /// <param name="what">The call that failed.</param>
    /// <param name="result">The <c>VkResult</c> the call returned.</param>
    /// <exception cref="OutOfMemoryException">Thrown when the driver ran out of host or device memory.</exception>
    /// <exception cref="VulkanException">Thrown for any other failure.</exception>
    private static void ThrowAllocationFailure(string what, int result)
    {
        // VK_ERROR_OUT_OF_HOST_MEMORY is -1 and VK_ERROR_OUT_OF_DEVICE_MEMORY is -2.
        if (result is -1 or -2)
        {
            throw new OutOfMemoryException($"{what} (VkResult {result}).");
        }

        throw new VulkanException($"{what}.", result);
    }

    /// <summary>
    /// Throws when the bridge has already been disposed, because its device handles may no longer be valid.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the bridge has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(m_Disposed, this);
    }

}

/// <summary>
/// Reports a failing Vulkan call with the result code it returned.
/// </summary>
public sealed class VulkanException : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public VulkanException(string message) : base(message)
    {
    }

    /// <param name="message">The message that describes the failure.</param>
    /// <param name="result">The <c>VkResult</c> value, or <see langword="0"/> when the failure has no code.</param>
    public VulkanException(string message, int result) : base($"{message} (VkResult {result})")
    {
        Result = result;
    }

    /// <summary>
    /// Gets the <c>VkResult</c> value the failing call returned.
    /// </summary>
    public int Result { get; }
}

#endif
