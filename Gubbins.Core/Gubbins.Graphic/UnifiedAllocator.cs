using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// Represents a bump allocator that serves allocations from segments a graphics backend shares between the CPU and the
/// device, so that the same pages are used for reading, uploading and reading back without a per-frame copy.
/// </summary>
/// <remarks>
/// <para>
/// The allocator itself cannot create shared memory: the graphics API owns the pages or their mapping, and only the
/// backend knows which memory types are CPU visible on this machine. <see cref="UnifiedAllocator"/> therefore delegates
/// segment acquisition to a <see cref="UnifiedProvider"/> and adds what the callers need on top: aligned bump
/// allocation with stable addresses, a mapping from a pointer to the segment that contains it
/// (<see cref="TryGetSegment"/>), the block description a backend binds a device buffer to
/// (<see cref="AllocBlock"/>), the copy that fills device-only memory (<see cref="TryCopy"/>), and the cache
/// maintenance that non-coherent shared memory requires (<see cref="Flush{T}(Span{T})"/> and
/// <see cref="Invalidate{T}(Span{T})"/>).
/// </para>
/// <para>
/// This is the zero-copy shape of a frame's worth of GPU data. The first frame allocates, the backend creates buffers
/// over the returned segments once, and every later frame writes into the same pages and flushes instead of allocating a
/// staging buffer and copying into it. What it does not remove is synchronization: a CPU write still has to be ordered
/// before the device reads the range, so the caller submits the buffer usage after the writes, not before.
/// </para>
/// <para>
/// On a unified memory architecture — Apple silicon, an AMD APU, an Intel or Qualcomm SoC, a Jetson module — the pages
/// are shared but the heaps are not interchangeable, so the kind belongs to the segment rather than to the allocator: one
/// heap both processors address, and one only the device addresses. <see cref="UnifiedMemoryUsage"/> is how the caller says
/// which of the two it wants; reading
/// <see cref="TryGetBlock"/> back says what it got, which decides whether a flush is needed
/// (<see cref="UnifiedMemoryBlock.IsCoherent"/>) and whether the block can be bound to a device buffer at all
/// (<see cref="UnifiedMemoryBlock.IsMapped"/> or <see cref="UnifiedMemoryBlock.IsDeviceLocal"/>). A discrete GPU answers
/// <see cref="UnifiedMemoryKind.Host"/> for everything, and the data then has to be copied to reach the device.
/// </para>
/// <para>
/// No block ever moves while the device may hold a binding to it, so the arena never compacts: a released range stays with
/// the segment that served it and the next allocation that fits reuses it, which is what keeps a steady churn of
/// allocations from growing the arena. Releases in allocation order also join up, so a segment a frame
/// filled and emptied comes back as one range.
/// </para>
/// <para>
/// A caller that cannot prove the device is finished with a block does not have to: <see cref="TryFreeWhenComplete"/>
/// queues the release behind a fence value and <see cref="ReleaseCompleted"/> hands the range back once that value is
/// reached, so the two ways to release are "the device is done" and "the device will tell me". Neither of them moves
/// anything, and a queued release keeps its range reserved until it is handed back.
/// </para>
/// <para>
/// <see cref="Reset"/> rewinds every segment to empty while keeping the segments themselves, so the backend's buffer
/// objects and mappings stay valid across frames. Only a resize or a remap is restricted to the most recent block of a
/// segment, because growing any other block in place would have to move it. <see cref="Trim"/> gives the segments that
/// hold nothing back to the backend, which is what an arena that grew during a spike uses to fall back. This allocator is
/// not thread-safe, which matches the usual rule that a frame's command recording happens on one thread.
/// </para>
/// </remarks>
public unsafe struct UnifiedAllocator : IAllocatable
{
    /// <summary>
    /// The segment size used when the caller does not choose one. Large enough to hold a frame's worth of entities
    /// without turning every buffer into its own device allocation.
    /// </summary>
    public const nuint DEFAULT_SEGMENT_SIZE = 4 * 1024 * 1024;

    /// <summary>
    /// The alignment every block gets, regardless of what the caller asks for.
    /// </summary>
    /// <remarks>
    /// Binding a buffer at an offset inside a shared segment requires the offset to satisfy the backend's offset
    /// alignment: a Metal encoder offset, a D3D12 constant buffer view offset, and Vulkan's
    /// <c>minStorageBufferOffsetAlignment</c> are all at most 256 bytes in practice, so this is the largest step that
    /// is safe everywhere.
    /// </remarks>
    public const nuint MINIMUM_ALIGNMENT = 256;

    /// <summary>
    /// The largest alignment that can be requested through the <see cref="Allocator"/> vtable, whose alignment argument
    /// is a byte.
    /// </summary>
    private const nuint MAXIMUM_ALIGNMENT = 128;

    /// <summary>
    /// The largest value a <see cref="nuint"/> can hold. It is computed rather than read from <c>nuint.MaxValue</c>
    /// because that member does not exist on every target framework.
    /// </summary>
    private static readonly nuint maxSize = unchecked((nuint)0 - 1);

    /// <summary>
    /// The alignment the free-list nodes are allocated with.
    /// </summary>
    private static readonly byte freeAlignment = Native.GetAlignment((int)sizeof(FreeNode));

    /// <summary>
    /// The alignment the deferred-release nodes are allocated with.
    /// </summary>
    private static readonly byte pendingAlignment = Native.GetAlignment((int)sizeof(PendingRelease));

    /// <summary>
    /// Allocator methods for vtable, serving <see cref="UnifiedMemoryUsage.General"/>.
    /// </summary>
    private static readonly Allocator.VTable methods = new(&Dispose, &Reset, &Alloc, &Resize, &Free, &Remap);

    /// <summary>
    /// The backend that acquires and maintains the segments.
    /// </summary>
    private readonly UnifiedProvider m_Provider;

    /// <summary>
    /// The minimum size of a segment acquired from <see cref="m_Provider"/>, per usage.
    /// </summary>
    private readonly UnifiedSegmentSizes m_SegmentSizes;

    /// <summary>
    /// The alignment every block gets, at least <see cref="MINIMUM_ALIGNMENT"/>.
    /// </summary>
    private readonly nuint m_Alignment;

    /// <summary>
    /// The oldest segment, or <see langword="null"/> before the first allocation.
    /// </summary>
    private Segment* m_First;

    /// <summary>
    /// The newest segment, or <see langword="null"/> before the first allocation. Segments are appended here, so the
    /// chain is ordered from the oldest segment to the newest one.
    /// </summary>
    private Segment* m_Last;

    /// <summary>
    /// The segment that served the most recent allocation. Its most recent block is the only one that can be freed,
    /// resized or remapped, because every other block may already be bound to a device buffer.
    /// </summary>
    private Segment* m_Current;

    /// <summary>
    /// The releases that are waiting for the device to pass a fence value, oldest first, or <see langword="null"/> when
    /// nothing is waiting.
    /// </summary>
    private PendingRelease* m_Pending;

    /// <summary>
    /// The total size the segments may reach, or <see langword="0"/> to use the working set the backend reports.
    /// </summary>
    private readonly nuint m_Budget;

    /// <param name="provider">The backend that acquires and maintains the segments.</param>
    /// <param name="segmentSize">
    /// The minimum size of a segment, or <see langword="0"/> for <see cref="DEFAULT_SEGMENT_SIZE"/>. The size is rounded
    /// up to the provider's alignment, and a single allocation larger than this gets a segment of its own size.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provider"/> has no vtable.</exception>
    public UnifiedAllocator(UnifiedProvider provider, nuint segmentSize = 0) : this()
    {
        if (!provider.IsValid)
        {
            throw new ArgumentException("The unified provider has no vtable.", nameof(provider));
        }

        m_Provider = provider;
        m_SegmentSizes = UnifiedSegmentSizes.Uniform(segmentSize == 0 ? DEFAULT_SEGMENT_SIZE : segmentSize);
        m_Alignment = MINIMUM_ALIGNMENT;
    }

    /// <summary>
    /// Initializes an allocator whose segments are sized per usage.
    /// </summary>
    /// <remarks>
    /// The heaps a unified memory machine exposes differ enough that one segment size cannot serve them all: a device-only
    /// heap wants few, large segments because every one of them is a driver allocation, while the shared pages the CPU
    /// writes every frame want smaller ones so a frame's waste stays small.
    /// small ones so a frame's waste stays small. Pass a size of <see langword="0"/> for a usage to fall back to
    /// <see cref="DEFAULT_SEGMENT_SIZE"/>.
    /// </remarks>
    /// <param name="provider">The backend that acquires and maintains the segments.</param>
    /// <param name="segmentSizes">The minimum segment size to use for each usage.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provider"/> has no vtable.</exception>
    public UnifiedAllocator(UnifiedProvider provider, in UnifiedSegmentSizes segmentSizes) : this()
    {
        if (!provider.IsValid)
        {
            throw new ArgumentException("The unified provider has no vtable.", nameof(provider));
        }

        m_Provider = provider;
        m_SegmentSizes = segmentSizes.WithDefault(DEFAULT_SEGMENT_SIZE);
        m_Alignment = MINIMUM_ALIGNMENT;
    }

    /// <summary>
    /// Initializes an allocator whose segments are sized per usage and whose total size is capped.
    /// </summary>
    /// <remarks>
    /// A budget below what the backend reports is how a caller keeps its own headroom: the allocator refuses to add a
    /// segment that would take the arena past it, after first handing back the segments that hold nothing, so the failure
    /// arrives at the allocation instead of as an out-of-memory from the driver later.
    /// </remarks>
    /// <param name="provider">The backend that acquires and maintains the segments.</param>
    /// <param name="segmentSizes">The minimum segment size to use for each usage.</param>
    /// <param name="budget">The total size the segments may reach, or <see langword="0"/> to use the backend's working set.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provider"/> has no vtable.</exception>
    public UnifiedAllocator(UnifiedProvider provider, in UnifiedSegmentSizes segmentSizes, nuint budget) : this()
    {
        if (!provider.IsValid)
        {
            throw new ArgumentException("The unified provider has no vtable.", nameof(provider));
        }

        m_Provider = provider;
        m_SegmentSizes = segmentSizes.WithDefault(DEFAULT_SEGMENT_SIZE);
        m_Alignment = MINIMUM_ALIGNMENT;
        m_Budget = budget;
    }

    /// <summary>
    /// Initializes an allocator with one segment size for every usage and a total size cap.
    /// </summary>
    /// <param name="provider">The backend that acquires and maintains the segments.</param>
    /// <param name="segmentSize">
    /// The minimum size of a segment, or <see langword="0"/> for <see cref="DEFAULT_SEGMENT_SIZE"/>.
    /// </param>
    /// <param name="budget">The total size the segments may reach, or <see langword="0"/> to use the backend's working set.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="provider"/> has no vtable.</exception>
    public UnifiedAllocator(UnifiedProvider provider, nuint segmentSize, nuint budget) : this()
    {
        if (!provider.IsValid)
        {
            throw new ArgumentException("The unified provider has no vtable.", nameof(provider));
        }

        m_Provider = provider;
        m_SegmentSizes = UnifiedSegmentSizes.Uniform(segmentSize == 0 ? DEFAULT_SEGMENT_SIZE : segmentSize);
        m_Alignment = MINIMUM_ALIGNMENT;
        m_Budget = budget;
    }

    /// <summary>
    /// Gets a value indicating whether this instance wraps a usable provider.
    /// A default-initialized <see cref="UnifiedAllocator"/> has no provider and must not be used to allocate memory.
    /// </summary>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Provider.IsValid;
    }

    /// <summary>
    /// Gets the backend that acquires and maintains the segments.
    /// </summary>
    public readonly UnifiedProvider Provider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Provider;
    }

    /// <summary>
    /// Gets the alignment the provider requires for segment base addresses and sizes, in bytes.
    /// </summary>
    public readonly nuint ProviderAlignment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Provider.Alignment;
    }

    /// <summary>
    /// Gets the largest segment the backend can create, in bytes, or <see langword="0"/> when it has no limit.
    /// </summary>
    public readonly nuint MaxSegmentSize
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Provider.MaxSegmentSize;
    }

    /// <summary>
    /// Gets the working set the backend recommends, in bytes, or <see langword="0"/> when it has no opinion.
    /// </summary>
    public readonly nuint WorkingSetLimit
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Provider.WorkingSetLimit;
    }

    /// <summary>
    /// Gets the total size the segments may reach, in bytes, or <see langword="0"/> when the arena may grow without a
    /// ceiling.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An allocator built without a budget of its own enforces the working set the backend reports, so on a graphics device
    /// the ceiling is the video memory budget the driver gives this process. A caller that wants to stay further inside
    /// that budget constructs the allocator with a smaller one.
    /// </para>
    /// <para>
    /// The ceiling is enforced when a segment is acquired, not when a block is allocated: an arena that is already over its
    /// budget because the budget was lowered afterwards still serves every request from the segments it has, and only
    /// refuses to add another one.
    /// </para>
    /// </remarks>
    public readonly nuint Budget
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Budget != 0 ? m_Budget : m_Provider.WorkingSetLimit;
    }

    /// <summary>
    /// Gets the alignment every block gets, in bytes.
    /// </summary>
    public readonly nuint Alignment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Alignment;
    }

    /// <summary>
    /// Gets the minimum size of the segments acquired for <paramref name="usage"/>, in bytes.
    /// </summary>
    /// <param name="usage">The usage to report.</param>
    /// <returns>The configured segment size, in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly nuint GetSegmentSize(UnifiedMemoryUsage usage) => m_SegmentSizes[usage];

    /// <summary>
    /// Gets the segment sizes this allocator uses, per usage.
    /// </summary>
    public readonly UnifiedSegmentSizes SegmentSizes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_SegmentSizes;
    }

    /// <summary>
    /// Gets the number of segments the allocator currently owns.
    /// </summary>
    public readonly int SegmentCount
    {
        get
        {
            var count = 0;

            for (var segment = m_First; segment != null; segment = segment->Next)
            {
                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the total size of the segments the allocator currently owns, in bytes.
    /// </summary>
    public readonly nuint Capacity
    {
        get
        {
            nuint capacity = 0;

            for (var segment = m_First; segment != null; segment = segment->Next)
            {
                capacity += segment->Size;
            }

            return capacity;
        }
    }

    /// <summary>
    /// Gets the number of bytes handed out since the last <see cref="Reset"/> and not handed back, in bytes.
    /// </summary>
    /// <remarks>
    /// A block that <see cref="TryFree"/> reclaimed is no longer counted, even though the segment still ends at the same
    /// place: the range waits with its segment and the next allocation of that size reuses it.
    /// </remarks>
    public readonly nuint Used
    {
        get
        {
            nuint used = 0;

            for (var segment = m_First; segment != null; segment = segment->Next)
            {
                used += segment->Used - segment->FreeBytes;
            }

            return used;
        }
    }

    /// <summary>
    /// Gets the total size of the segments acquired for <paramref name="usage"/>, in bytes.
    /// </summary>
    /// <remarks>
    /// The per-usage split is what makes a unified memory budget visible: the same total can be spent on the pages both
    /// processors address and on device-only memory, and on an SoC those are different resources even when they come from
    /// one DRAM.
    /// </remarks>
    /// <param name="usage">The usage to report.</param>
    /// <returns>The capacity of the matching segments, in bytes.</returns>
    public readonly nuint GetCapacity(UnifiedMemoryUsage usage)
    {
        nuint capacity = 0;

        for (var segment = m_First; segment != null; segment = segment->Next)
        {
            if (segment->Usage == usage)
            {
                capacity += segment->Size;
            }
        }

        return capacity;
    }

    /// <summary>
    /// Gets the number of bytes handed out from the segments acquired for <paramref name="usage"/> and not handed back, in
    /// bytes.
    /// </summary>
    /// <remarks>
    /// A range <see cref="TryFree"/> reclaimed is subtracted here but still counts against
    /// <see cref="GetCapacity(UnifiedMemoryUsage)"/> and still sits at the end of its segment, so the difference between the
    /// two is what the arena currently wastes on the padding between blocks.
    /// </remarks>
    /// <param name="usage">The usage to report.</param>
    /// <returns>The used bytes of the matching segments.</returns>
    public readonly nuint GetUsed(UnifiedMemoryUsage usage)
    {
        nuint used = 0;

        for (var segment = m_First; segment != null; segment = segment->Next)
        {
            if (segment->Usage == usage)
            {
                used += segment->Used - segment->FreeBytes;
            }
        }

        return used;
    }

    /// <summary>
    /// Gets the number of releases that are waiting for a fence value.
    /// </summary>
    /// <remarks>
    /// A waiting release still holds its range: <see cref="GetUsed(UnifiedMemoryUsage)"/> counts it and the next
    /// allocation cannot land on it until <see cref="ReleaseCompleted"/> hands it back.
    /// </remarks>
    public readonly int PendingReleaseCount
    {
        get
        {
            var count = 0;

            for (var node = m_Pending; node != null; node = node->Next)
            {
                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Releases <paramref name="block"/> once the device has passed <paramref name="fenceValue"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the release a caller uses while the device may still be reading the block: the frame submits its work,
    /// reads the value the submission was tagged with, and hands the block over with that value. The range stays
    /// reserved - it is not handed out by any later allocation and does not count as free - until
    /// <see cref="ReleaseCompleted"/> is called with a completed value that reaches it.
    /// </para>
    /// <para>
    /// Nothing here observes the device: the allocator has no queue and no fence of its own, so the value is whatever the
    /// caller's queue reports. Passing a value the device has not reached only delays the release, and passing one it
    /// passed long ago releases the range immediately, which is the caller's statement that it is safe.
    /// </para>
    /// </remarks>
    /// <param name="block">The block to release.</param>
    /// <param name="fenceValue">The value the submission that last used the block was tagged with.</param>
    /// <returns><see langword="true"/> if the release was queued; otherwise, <see langword="false"/>.</returns>
    public bool TryFreeWhenComplete(in UnifiedMemoryBlock block, ulong fenceValue)
    {
        var segment = FindByBlock(m_First, block);

        if (segment == null || block.Offset > segment->Used || block.Length > segment->Used - block.Offset)
        {
            return false;
        }

        fixed (UnifiedAllocator* pThis = &this)
        {
            return Enqueue(pThis, segment, block.Offset, block.Length, fenceValue);
        }
    }

    /// <summary>
    /// Releases the block at <paramref name="ptr"/> once the device has passed <paramref name="fenceValue"/>.
    /// </summary>
    /// <param name="ptr">A pointer previously returned by this allocator.</param>
    /// <param name="length">The size of the block in bytes.</param>
    /// <param name="fenceValue">The value the submission that last used the block was tagged with.</param>
    /// <returns><see langword="true"/> if the release was queued; otherwise, <see langword="false"/>.</returns>
    public bool TryFreeWhenComplete(void* ptr, nuint length, ulong fenceValue)
    {
        if (!TryFind(m_First, ptr, out var segment))
        {
            return false;
        }

        fixed (UnifiedAllocator* pThis = &this)
        {
            return Enqueue(pThis, segment, (nuint)ptr - (nuint)segment->Host, length, fenceValue);
        }
    }

    /// <summary>
    /// Hands back every range whose fence value the device has passed.
    /// </summary>
    /// <remarks>
    /// The caller decides when to call this, which is what keeps the policy in one place: once per frame after waiting for
    /// the frame that owns the oldest submission, or once per stage if the stages are fenced separately.
    /// </remarks>
    /// <param name="completedFenceValue">The highest fence value the device has completed.</param>
    /// <returns>The number of ranges that were released.</returns>
    public int ReleaseCompleted(ulong completedFenceValue)
    {
        var released = 0;
        PendingRelease* previous = null;
        var node = m_Pending;

        while (node != null)
        {
            var next = node->Next;

            if (node->FenceValue <= completedFenceValue)
            {
                var segment = node->Segment;
                var offset = node->Offset;
                var length = node->Length;

                if (previous == null)
                {
                    m_Pending = next;
                }
                else
                {
                    previous->Next = next;
                }

                UnmanagedAllocator.Global.Free(node, (nuint)sizeof(PendingRelease), pendingAlignment);

                if (FreeRange(segment, m_Alignment, offset, length))
                {
                    released++;
                }

                node = next;
                continue;
            }

            previous = node;
            node = next;
        }

        return released;
    }

    /// <summary>
    /// Finds the largest block that <paramref name="usage"/> could serve without acquiring another segment.
    /// </summary>
    /// <remarks>
    /// This is the question a caller asks before it decides whether a big upload fits or has to be split: the ranges a
    /// segment released and the room left at the end of it are both usable, and the answer is the larger of the two over
    /// the segments of that usage.
    /// </remarks>
    /// <param name="usage">The usage to report.</param>
    /// <param name="largest">When the method returns, the largest block that fits, in bytes.</param>
    /// <returns><see langword="true"/> if any room was found; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetLargestFree(UnifiedMemoryUsage usage, out nuint largest)
    {
        largest = 0;

        for (var segment = m_First; segment != null; segment = segment->Next)
        {
            if (segment->Usage != usage)
            {
                continue;
            }

            for (var node = segment->FreeHead; node != null; node = node->Next)
            {
                if (node->Size > largest)
                {
                    largest = node->Size;
                }
            }

            // The room after the cursor counts too, minus the padding the alignment of the next block needs.
            var cursor = AlignOffset(segment, m_Alignment);
            if (cursor <= segment->Size && segment->Size - cursor > largest)
            {
                largest = segment->Size - cursor;
            }
        }

        return largest != 0;
    }

    /// <summary>
    /// Gets the number of bytes that were released and are waiting to be reused, across the segments acquired for
    /// <paramref name="usage"/>.
    /// </summary>
    /// <remarks>
    /// This is the part of the arena a caller may still fill without the backend having to acquire another segment, so it
    /// is the number to watch when churn is suspected of growing the arena: it only drops when a released range is reused or
    /// when the release reaches the end of its segment and shortens it.
    /// </remarks>
    /// <param name="usage">The usage to report.</param>
    /// <returns>The released bytes of the matching segments.</returns>
    public readonly nuint GetFree(UnifiedMemoryUsage usage)
    {
        nuint free = 0;

        for (var segment = m_First; segment != null; segment = segment->Next)
        {
            if (segment->Usage == usage)
            {
                free += segment->FreeBytes;
            }
        }

        return free;
    }

    /// <summary>
    /// Rewinds the segments of <paramref name="usage"/> to empty while keeping them, which is the per-heap form of
    /// <see cref="Allocator.Reset"/>.
    /// </summary>
    /// <remarks>
    /// The allocator's own <c>Reset</c> rewinds every heap, because that is what the arena's handle does; a caller that
    /// recycles one heap per frame - the staging heap is the usual one - wants this instead, so the device blocks it still
    /// needs are untouched. The segments of the usage stay acquired, so the buffers bound to them stay valid, and the
    /// releases that were still waiting on a fence value for that usage are dropped with them.
    /// </remarks>
    /// <param name="usage">The heap to rewind.</param>
    /// <returns>The number of segments that were rewound.</returns>
    public int Reset(UnifiedMemoryUsage usage)
    {
        fixed (UnifiedAllocator* pThis = &this)
        {
            return ResetUsage(pThis, usage);
        }
    }

    /// <summary>
    /// Releases every segment that holds no live block, which is how an arena that grew during one spike gives the memory
    /// back to the backend instead of keeping it for the rest of the session.
    /// </summary>
    /// <remarks>
    /// Only segments whose blocks were all released are candidates: releasing one that still holds a block would pull
    /// memory out from under a device binding. Nothing moves, so every block that stays live keeps its address, its
    /// offset and its device address.
    /// </remarks>
    /// <returns>The number of segments that were released.</returns>
    public int Trim()
    {
        fixed (UnifiedAllocator* pThis = &this)
        {
            return Trim(pThis, UnifiedMemoryUsage.General, true);
        }
    }

    /// <summary>
    /// Releases every segment of <paramref name="usage"/> that holds no live block.
    /// </summary>
    /// <param name="usage">The usage whose empty segments to release.</param>
    /// <returns>The number of segments that were released.</returns>
    public int Trim(UnifiedMemoryUsage usage)
    {
        fixed (UnifiedAllocator* pThis = &this)
        {
            return Trim(pThis, usage, false);
        }
    }

    /// <summary>
    /// Gets the <see cref="Allocator"/> every other part of the engine allocates through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the surface callers normally see: ordinary collection and chunk code takes an <see cref="Allocator"/>,
    /// so it never mentions heaps, usages or device memory, and its allocations are served as
    /// <see cref="UnifiedMemoryUsage.General"/>. Everything on <see cref="UnifiedAllocator"/> that talks about blocks,
    /// copies or budgets is for the code that owns the graphics device, not for the code that allocates an array.
    /// </para>
    /// <para>
    /// The returned allocator carries a pointer to <see langword="this"/>, so the instance has to stay where it is for as
    /// long as the allocator is used: a <see cref="UnifiedAllocator"/> copied to another variable, returned by value, or
    /// held in a field that moves leaves the allocator pointing at the old copy. A caller that cannot promise that -
    /// which is most of them, since the arena usually outlives the frame that created it - should hand the arena to
    /// <see cref="AllocatorHandle.Create{T}(in T)"/>, which copies it into unmanaged storage and hands back an allocator
    /// whose context stays put.
    /// </para>
    /// <para>
    /// An <see cref="Allocator"/> can still reach the arena behind it with <see cref="Allocator.Cast{T}"/>, which is how a
    /// caller that needs <see cref="Trim"/>, <see cref="Budget"/> or <see cref="TryGetLargestFree"/> gets at them from the
    /// handle it already has.
    /// </para>
    /// </remarks>
    /// <returns>The allocator that serves the arena.</returns>
    public Allocator GetAllocator() => GetAllocator(UnifiedMemoryUsage.General);

    /// <summary>
    /// Gets an <see cref="Allocator"/> whose allocations are served from the heap of <paramref name="usage"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is how code that only knows how to allocate bytes reaches the shared heap, which is the whole point of the
    /// library: the pages an <see cref="Allocator"/> hands out are the pages the device reads and writes, without a staging
    /// copy. Freeing, resizing and remapping are the arena's own, so a block handed out by this allocator is released
    /// exactly like any other.
    /// </para>
    /// <para>
    /// Device-only memory cannot serve an <see cref="Allocator"/> at all, because its contract is a pointer the caller
    /// writes through; ask the arena for a
    /// <see cref="UnifiedMemoryBlock"/> instead and fill it with <see cref="TryUpload"/> or <see cref="TryCopy"/>.
    /// </para>
    /// <para>
    /// The lifetime rule is the same as for <see cref="GetAllocator()"/>: the allocator points at this instance, so use
    /// <see cref="AllocatorHandle.Create{T}(in T)"/> when the arena has to outlive the scope it was built in. Resetting it
    /// rewinds every heap, because that is what the arena's own handle does; a caller that recycles one heap per frame
    /// calls <see cref="Reset(UnifiedMemoryUsage)"/> instead.
    /// </para>
    /// </remarks>
    /// <param name="usage">The heap every allocation through the returned allocator is served from.</param>
    /// <returns>The allocator that serves that heap.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="usage"/> is <see cref="UnifiedMemoryUsage.Device"/>, which has no CPU address.
    /// </exception>
    public Allocator GetAllocator(UnifiedMemoryUsage usage)
    {
        var methods = usage switch
        {
            UnifiedMemoryUsage.General => UnifiedAllocator.methods,
            _ => throw new ArgumentOutOfRangeException(
                nameof(usage),
                usage,
                $"A {usage} block has no CPU address, so it cannot serve an {nameof(Allocator)}. Use {nameof(AllocBlock)} and fill it with {nameof(TryUpload)} instead.")
        };

#if NET9_0_OR_GREATER
        return new Allocator(Native.AsPointer(ref this), methods);
#else
        fixed (UnifiedAllocator* pThis = &this)
        {
            return new Allocator(pThis, methods);
        }
#endif
    }

    /// <summary>
    /// Allocates <paramref name="len"/> bytes for a block whose CPU access pattern is <paramref name="usage"/>.
    /// </summary>
    /// <remarks>
    /// Callers that only need ordinary read-write shared memory can use <see cref="GetAllocator"/> and the
    /// <see cref="Allocator"/> interface instead; those allocations are served as
    /// <see cref="UnifiedMemoryUsage.General"/>. This overload exists for the blocks whose usage decides the heap: a
    /// frame's instance data, for example, is filled by the CPU and only read by the device.
    /// </remarks>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <param name="usage">What the CPU does with the block, which selects the heap on a multi-heap backend.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the backend served the request from device-only memory, which has no CPU address. Use
    /// <see cref="AllocBlock"/> for that.
    /// </exception>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    public void* Alloc(nuint len, byte alignment, UnifiedMemoryUsage usage)
    {
        fixed (UnifiedAllocator* pThis = &this)
        {
            Allocate(pThis, len, alignment, usage, out var segment, out var offset);

            if (segment->Host == null)
            {
                throw new InvalidOperationException(
                    $"The backend served {usage} usage from device-only memory, which has no CPU address. Use {nameof(AllocBlock)} instead.");
            }

            return segment->Host + offset;
        }
    }

    /// <summary>
    /// Allocates <paramref name="len"/> bytes for a block whose CPU access pattern is <paramref name="usage"/>, with the
    /// largest block alignment the vtable can express.
    /// </summary>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="usage">What the CPU does with the block, which selects the heap on a multi-heap backend.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the block is not CPU addressable.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    public void* Alloc(nuint len, UnifiedMemoryUsage usage) => Alloc(len, (byte)MAXIMUM_ALIGNMENT, usage);

    /// <summary>
    /// Allocates a block for <paramref name="count"/> elements of type <typeparamref name="T"/> whose CPU access pattern
    /// is <paramref name="usage"/>.
    /// </summary>
    /// <param name="count">The number of elements to allocate.</param>
    /// <param name="usage">What the CPU does with the block, which selects the heap on a multi-heap backend.</param>
    /// <typeparam name="T">The type of the elements to allocate memory for.</typeparam>
    /// <returns>A <see cref="Span{T}"/> representing the allocated memory block.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> exceeds the maximum span length.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the block is not CPU addressable.</exception>
    public Span<T> Alloc<T>(nuint count, UnifiedMemoryUsage usage) where T : unmanaged
    {
        if (count > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        return new Span<T>(Alloc(count * (nuint)sizeof(T), (byte)MAXIMUM_ALIGNMENT, usage), (int)count);
    }

    /// <summary>
    /// Allocates a block of <paramref name="len"/> bytes and describes it the way a backend needs it, including the case
    /// where the backend served it from device-only memory that has no CPU address.
    /// </summary>
    /// <remarks>
    /// This is the allocation entry point for resources the CPU does not read and write every frame: a
    /// <see cref="UnifiedMemoryUsage.Device"/> block can come back with <see cref="UnifiedMemoryBlock.IsMapped"/> false,
    /// which is the desired outcome rather than a failure. Fill such a block with <see cref="TryCopy"/> from a mapped
    /// block, and bind it to a device buffer with its handle and offset.
    /// </remarks>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="usage">What the CPU does with the block, which selects the heap on a multi-heap backend.</param>
    /// <returns>The description of the allocated block.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    public UnifiedMemoryBlock AllocBlock(nuint len, UnifiedMemoryUsage usage)
    {
        fixed (UnifiedAllocator* pThis = &this)
        {
            Allocate(pThis, len, MAXIMUM_ALIGNMENT, usage, out var segment, out var offset);

            // A zero-length block still gets a valid address, so a device-only segment reports a null host rather than a
            // bogus one only when it really has no CPU mapping.
            var host = segment->Host == null ? null : segment->Host + offset;
            var segmentHost = segment->Host;

            return new UnifiedMemoryBlock(
                segment->DeviceHandle,
                segmentHost,
                host,
                segment->DeviceAddress == 0 ? 0 : segment->DeviceAddress + offset,
                offset,
                len,
                segment->Size,
                segment->Kind);
        }
    }

    /// <summary>
    /// Describes the segment that contains <paramref name="ptr"/> and the offset of the block inside it, which is what a
    /// backend needs to bind a buffer to the block.
    /// </summary>
    /// <param name="ptr">A pointer previously returned by this allocator.</param>
    /// <param name="segment">
    /// When the method returns, the description of the containing segment if <paramref name="ptr"/> belongs to this
    /// allocator; otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="ptr"/> belongs to this allocator; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetSegment(void* ptr, out UnifiedMemorySegment segment)
    {
        segment = default;

        if (!m_Provider.IsValid || ptr == null || !TryFind(m_First, ptr, out var found))
        {
            return false;
        }

        segment = new UnifiedMemorySegment(
            found->Host,
            found->Size,
            (nuint)ptr - (nuint)found->Host,
            found->DeviceHandle,
            found->DeviceAddress,
            found->Kind);
        return true;
    }

    /// <summary>
    /// Builds the description of the block that starts at <paramref name="ptr"/> and is <paramref name="length"/> bytes
    /// long, which is the value a backend binds a device buffer with.
    /// </summary>
    /// <param name="ptr">A pointer previously returned by this allocator.</param>
    /// <param name="length">The length of the block, in bytes.</param>
    /// <param name="block">
    /// When the method returns, the description of the block if <paramref name="ptr"/> belongs to this allocator;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="ptr"/> belongs to this allocator; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetBlock(void* ptr, nuint length, out UnifiedMemoryBlock block)
    {
        block = default;

        if (!TryGetSegment(ptr, out var segment))
        {
            return false;
        }

        block = new UnifiedMemoryBlock(
            segment.DeviceHandle,
            segment.Host,
            segment.Block,
            segment.DeviceAddressOfBlock,
            segment.Offset,
            length,
            segment.Size,
            segment.Kind);
        return true;
    }

    /// <summary>
    /// Reports how the segment that contains <paramref name="ptr"/> is shared, which decides whether the block needs a
    /// flush after the CPU writes it and an invalidate before the CPU reads it.
    /// </summary>
    /// <param name="ptr">A pointer previously returned by this allocator.</param>
    /// <param name="kind">
    /// When the method returns, how the containing segment is shared if <paramref name="ptr"/> belongs to this
    /// allocator; otherwise, <see cref="UnifiedMemoryKind.Host"/>.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="ptr"/> belongs to this allocator; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryGetKind(void* ptr, out UnifiedMemoryKind kind)
    {
        if (m_Provider.IsValid && ptr != null && TryFind(m_First, ptr, out var found))
        {
            kind = found->Kind;
            return true;
        }

        kind = UnifiedMemoryKind.Host;
        return false;
    }

    /// <summary>
    /// Makes CPU writes in the block visible to the device. No-op when the block's segment is coherent or not shared.
    /// </summary>
    /// <param name="ptr">The first byte of the block.</param>
    /// <param name="size">The size of the block in bytes.</param>
    public readonly void Flush(void* ptr, nuint size)
    {
        if (m_Provider.IsValid && TryFind(m_First, ptr, out var found) && found->Kind == UnifiedMemoryKind.Shared)
        {
            m_Provider.Flush(ptr, size);
        }
    }

    /// <summary>
    /// Makes device writes in the block visible to the CPU. No-op when the block's segment is coherent or not shared.
    /// </summary>
    /// <param name="ptr">The first byte of the block.</param>
    /// <param name="size">The size of the block in bytes.</param>
    public readonly void Invalidate(void* ptr, nuint size)
    {
        if (m_Provider.IsValid && TryFind(m_First, ptr, out var found) && found->Kind == UnifiedMemoryKind.Shared)
        {
            m_Provider.Invalidate(ptr, size);
        }
    }

    /// <summary>
    /// Makes CPU writes in <paramref name="span"/> visible to the device. No-op when the block's segment is coherent or
    /// not shared.
    /// </summary>
    /// <param name="span">The block written by the CPU.</param>
    /// <typeparam name="T">The type of the elements in the block.</typeparam>
    public readonly void Flush<T>(Span<T> span) where T : unmanaged
    {
        if (span.IsEmpty)
        {
            return;
        }

        fixed (T* ptr = span)
        {
            Flush(ptr, (nuint)span.Length * (nuint)sizeof(T));
        }
    }

    /// <summary>
    /// Makes device writes in <paramref name="span"/> visible to the CPU. No-op when the block's segment is coherent or
    /// not shared.
    /// </summary>
    /// <param name="span">The block written by the device.</param>
    /// <typeparam name="T">The type of the elements in the block.</typeparam>
    public readonly void Invalidate<T>(Span<T> span) where T : unmanaged
    {
        if (span.IsEmpty)
        {
            return;
        }

        fixed (T* ptr = span)
        {
            Invalidate(ptr, (nuint)span.Length * (nuint)sizeof(T));
        }
    }

    /// <summary>
    /// Copies <paramref name="size"/> bytes inside the allocator, from <paramref name="sourceOffset"/> inside the source
    /// block to <paramref name="destinationOffset"/> inside the destination block.
    /// </summary>
    /// <remarks>
    /// This is how a device-only block is filled: the backend copies from a mapped block on the device instead of the CPU
    /// writing the data again. The provider completes the copy before returning, so no additional synchronization is
    /// needed for a one-shot upload; a per-frame stream should write shared memory directly and skip the wait. A
    /// synchronous copy also submits whatever <see cref="TryCopyDeferred"/> recorded and has not been submitted yet,
    /// because a backend owns one queue position, so a deferred batch has to be submitted and waited first if it is meant
    /// to stay separate.
    /// </remarks>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns><see langword="true"/> if the backend performed the copy; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryCopy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size)
        => TryQueueCopy(destination, destinationOffset, source, sourceOffset, size, false);

    /// <summary>
    /// Validates a copy range and forwards it to the backend, synchronously or recorded.
    /// </summary>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="defer">Whether the backend should only record the copy.</param>
    /// <returns><see langword="true"/> if the backend performed or recorded the copy; otherwise, <see langword="false"/>.</returns>
    private readonly bool TryQueueCopy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        // Reject ranges that leave either block before the backend has to reason about them.
        if (destinationOffset > destination.Length || size > destination.Length - destinationOffset)
        {
            return false;
        }

        if (sourceOffset > source.Length || size > source.Length - sourceOffset)
        {
            return false;
        }

        return m_Provider.Copy(destination, destinationOffset, source, sourceOffset, size, defer);
    }

    /// <summary>
    /// Copies a whole block into another block, which is the usual way to fill a device-only block from a staging block
    /// of the same length.
    /// </summary>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="source">The block to copy from.</param>
    /// <returns><see langword="true"/> if the backend performed the copy; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryCopy(in UnifiedMemoryBlock destination, in UnifiedMemoryBlock source)
        => TryCopy(destination, 0, source, 0, source.Length < destination.Length ? source.Length : destination.Length);

    /// <summary>
    /// Copies <paramref name="data"/> into <paramref name="destination"/> through a staging block, which is the short way
    /// to fill a device-only block or a block the CPU cannot write.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The call is synchronous: it writes the bytes into a staging block taken from the shared heap, publishes them with
    /// <see cref="Flush"/>, copies them into the
    /// destination and waits for the copy before releasing the staging range back to the arena. A frame that fills many
    /// blocks should use <see cref="TryCopy"/> and <see cref="TryCopyDeferred"/> directly instead, so the copies are
    /// submitted together instead of waited for one at a time.
    /// </para>
    /// <para>
    /// Where the shared heap is the memory the device reads, this is a copy the device would have made anyway; on a
    /// machine without shared memory it is the copy that has to happen. Either way the caller does not have to know which
    /// of the two it is looking at.
    /// </para>
    /// </remarks>
    /// <param name="destination">The block to fill.</param>
    /// <param name="data">The bytes to copy into it.</param>
    /// <returns><see langword="true"/> if the bytes were copied; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when no staging block can be acquired.</exception>
    public bool TryUpload(in UnifiedMemoryBlock destination, ReadOnlySpan<byte> data)
        => TryUpload(destination, 0, data);

    /// <summary>
    /// Copies <paramref name="data"/> into <paramref name="destination"/> at <paramref name="destinationOffset"/>.
    /// </summary>
    /// <param name="destination">The block to fill.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="data">The bytes to copy into it.</param>
    /// <returns><see langword="true"/> if the bytes were copied; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when no staging block can be acquired.</exception>
    public bool TryUpload(in UnifiedMemoryBlock destination, nuint destinationOffset, ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            return true;
        }

        if (destinationOffset > destination.Length || (nuint)data.Length > destination.Length - destinationOffset)
        {
            return false;
        }

        var staging = AllocBlock((nuint)data.Length, UnifiedMemoryUsage.General);

        try
        {
            if (staging.Host == null)
            {
                return false;
            }

            data.CopyTo(new Span<byte>(staging.Host, data.Length));
            Flush(staging.Host, (nuint)data.Length);

            // A non-deferred copy is submitted and waited for by the backend, so the staging range is free to release by
            // the time the call returns.
            return TryCopy(destination, destinationOffset, staging, 0, (nuint)data.Length);
        }
        finally
        {
            TryFree(staging);
        }
    }

    /// <summary>
    /// Copies <paramref name="source"/> into <paramref name="data"/> through a readback block, which is the short way to
    /// read a block the CPU cannot address.
    /// </summary>
    /// <remarks>
    /// The call is synchronous, for the same reason <see cref="TryUpload(in UnifiedMemoryBlock, ReadOnlySpan{byte})"/> is:
    /// it copies into a block of the shared heap, waits for the copy, invalidates the
    /// range so the CPU sees the device's writes rather than a stale cache line, copies the bytes out and returns the
    /// readback range back to the arena.
    /// </remarks>
    /// <param name="data">The bytes to read into.</param>
    /// <param name="source">The block to read from.</param>
    /// <returns><see langword="true"/> if the bytes were read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when no readback block can be acquired.</exception>
    public bool TryReadback(Span<byte> data, in UnifiedMemoryBlock source)
        => TryReadback(data, source, 0);

    /// <summary>
    /// Copies <paramref name="source"/> at <paramref name="sourceOffset"/> into <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to read into.</param>
    /// <param name="source">The block to read from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <returns><see langword="true"/> if the bytes were read; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when no readback block can be acquired.</exception>
    public bool TryReadback(Span<byte> data, in UnifiedMemoryBlock source, nuint sourceOffset)
    {
        if (data.IsEmpty)
        {
            return true;
        }

        if (sourceOffset > source.Length || (nuint)data.Length > source.Length - sourceOffset)
        {
            return false;
        }

        var readback = AllocBlock((nuint)data.Length, UnifiedMemoryUsage.General);

        try
        {
            if (readback.Host == null)
            {
                return false;
            }

            if (!TryCopy(readback, 0, source, sourceOffset, (nuint)data.Length))
            {
                return false;
            }

            Invalidate(readback.Host, (nuint)data.Length);
            new ReadOnlySpan<byte>(readback.Host, data.Length).CopyTo(data);
            return true;
        }
        finally
        {
            TryFree(readback);
        }
    }

    /// <summary>
    /// Records a copy without waiting for it, so a frame's worth of uploads can be submitted in one go.
    /// </summary>
    /// <remarks>
    /// The destination content is undefined until <see cref="SubmitCopies"/> and <see cref="WaitForCopies"/> have
    /// completed, or until the device has otherwise been synchronized. Recording on the CPU thread and waiting once per
    /// frame is what keeps a device-only fill off the critical path; a backend that cannot record returns
    /// <see langword="false"/>, and the caller should fall back to
    /// <see cref="TryCopy(in UnifiedMemoryBlock, in UnifiedMemoryBlock)"/>.
    /// </remarks>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns><see langword="true"/> if the backend recorded the copy; otherwise, <see langword="false"/>.</returns>
    public readonly bool TryCopyDeferred(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size)
        => TryQueueCopy(destination, destinationOffset, source, sourceOffset, size, true);

    /// <summary>
    /// Submits the copies recorded by <see cref="TryCopyDeferred"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the backend submitted work; otherwise, <see langword="false"/>.</returns>
    public readonly bool SubmitCopies() => m_Provider.Submit();

    /// <summary>
    /// Blocks until the submitted copies have completed.
    /// </summary>
    /// <returns><see langword="true"/> if the backend waited for work; otherwise, <see langword="false"/>.</returns>
    public readonly bool WaitForCopies() => m_Provider.Wait();

    /// <summary>
    /// Reclaims a block in place, which is the only way to release a device-only block short of <see cref="Reset"/>.
    /// </summary>
    /// <remarks>
    /// The block does not have to be the most recent one: a range that is not at the end of its segment waits with that
    /// segment and the next allocation that fits reuses it, so a device block can be released short of
    /// <see cref="Reset"/> even while other blocks of the same segment are live. Reusing a range never moves a block, so
    /// the device bindings of the blocks that stay live are untouched; what the caller owes is that the device has
    /// finished with the block, because the arena has no way to know.
    /// </remarks>
    /// <param name="block">The block to reclaim.</param>
    /// <returns>
    /// <see langword="true"/> if the block belonged to this allocator and was reclaimed; otherwise, <see langword="false"/>,
    /// which also covers a block that was already released, because a range that overlaps an already released one is refused rather
    /// than counted twice.
    /// </returns>
    public readonly bool TryFree(in UnifiedMemoryBlock block)
    {
        var segment = FindByBlock(m_First, block);
        if (segment == null)
        {
            return false;
        }

        if (block.Offset > segment->Used || block.Length > segment->Used - block.Offset)
        {
            return false;
        }

        return FreeRange(segment, m_Alignment, block.Offset, block.Length);
    }

    /// <summary>
    /// Asks the backend to make the segment that contains <paramref name="ptr"/> resident or discardable.
    /// </summary>
    /// <param name="ptr">A pointer previously returned by this allocator.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend applied the state; otherwise, <see langword="false"/>.</returns>
    public readonly bool TrySetResidency(void* ptr, UnifiedMemoryResidency residency)
        => TryGetSegment(ptr, out var segment) && m_Provider.SetResidency(segment, residency);

    /// <summary>
    /// Asks the backend to make the segment that owns <paramref name="block"/> resident or discardable.
    /// </summary>
    /// <param name="block">A block of the segment to change.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend applied the state; otherwise, <see langword="false"/>.</returns>
    public readonly bool TrySetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency)
        => m_Provider.SetResidency(block, residency);

    /// <summary>
    /// Releases every segment (and therefore every device allocation) and returns the allocator to its initial state.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    private static void Dispose(void* ctx)
    {
        var self = (UnifiedAllocator*)ctx;
        var segment = self->m_First;

        ReleasePending(self);

        while (segment != null)
        {
            var next = segment->Next;
            ReleaseFreeList(segment);
            self->m_Provider.Free(segment->Host, segment->Size, segment->DeviceHandle);
            UnmanagedAllocator.Global.Free(segment, (nuint)sizeof(Segment), Native.GetAlignment((int)sizeof(Segment)));
            segment = next;
        }

        self->m_First = null;
        self->m_Last = null;
        self->m_Current = null;
    }

    /// <summary>
    /// Rewinds every segment to empty while keeping the segments allocated, so the backend's buffer objects and
    /// mappings stay valid for the next frame.
    /// </summary>
    /// <remarks>
    /// The released ranges are dropped as well: after a reset every segment is empty, so nothing is left waiting.
    /// </remarks>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    private static void Reset(void* ctx)
    {
        var self = (UnifiedAllocator*)ctx;

        ReleasePending(self);

        for (var segment = self->m_First; segment != null; segment = segment->Next)
        {
            ReleaseFreeList(segment);
            segment->Used = 0;
        }

        self->m_Current = self->m_First;
    }

    /// <summary>
    /// Serves the request with <see cref="UnifiedMemoryUsage.General"/> usage, so that generic code driving the
    /// allocator through its <see cref="Allocator"/> vtable gets ordinary read-write shared memory.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the block is not CPU addressable.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    private static void* Alloc(void* ctx, nuint len, byte alignment)
        => AllocatePointer((UnifiedAllocator*)ctx, len, alignment, UnifiedMemoryUsage.General);

    /// <summary>
    /// Serves an allocation that the vtable can hand out as a CPU pointer.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <param name="usage">What the CPU does with the block, which selects the heap.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the block is not CPU addressable.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    private static void* AllocatePointer(UnifiedAllocator* self, nuint len, byte alignment, UnifiedMemoryUsage usage)
    {
        Allocate(self, len, alignment, usage, out var segment, out var offset);

        // The vtable contract has no way to report an unmapped block, so it is an error here.
        if (segment->Host == null)
        {
            throw new InvalidOperationException($"The backend served {usage} memory that has no CPU address, so it cannot serve an {nameof(Allocator)}.");
        }

        return segment->Host + offset;
    }

    /// <summary>
    /// Serves <paramref name="len"/> bytes with at least the allocator's alignment from the first segment that has room
    /// and was acquired for <paramref name="usage"/>, because a block must not move once the device is bound to it.
    /// </summary>
    /// <remarks>
    /// Each segment is asked twice: first for a range that was released earlier and is parked on its free list, then for
    /// room after its cursor. A request that fits nowhere skips ahead: the segments it skipped keep their used length, and
    /// the skipped tail is only reclaimed by <see cref="Reset"/>, which is the trade a bump arena makes for stable
    /// addresses. A segment only serves the usage it was acquired for, because the usage is what selected its heap.
    /// </remarks>
    /// <param name="self">The allocator instance.</param>
    /// <param name="len">The length of the memory block to allocate in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (in bytes).</param>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <param name="segment">When the method returns, the segment that serves the block.</param>
    /// <param name="offset">When the method returns, the offset of the block inside the segment.</param>
    /// <exception cref="OutOfMemoryException">Thrown when no segment can be acquired for the request.</exception>
    private static void Allocate(UnifiedAllocator* self, nuint len, nuint alignment, UnifiedMemoryUsage usage, out Segment* segment, out nuint offset)
    {
        var align = alignment < self->m_Alignment ? self->m_Alignment : alignment;

        for (var candidate = self->m_First; candidate != null; candidate = candidate->Next)
        {
            if (candidate->Usage != usage)
            {
                continue;
            }

            // A parked range is reused before the cursor moves: the caller released it, so nothing is bound to it any
            // more, and reusing it keeps the arena from growing while every segment stays where the backend put it.
            if (TryServeFromFreeList(self, candidate, len, align, out var reused))
            {
                self->m_Current = candidate;
                segment = candidate;
                offset = reused;
                return;
            }

            var candidateOffset = AlignOffset(candidate, align);
            if (candidateOffset <= candidate->Size && len <= candidate->Size - candidateOffset)
            {
                // A zero-length request still returns a distinct, aligned address, but consuming no space keeps the
                // arena's bookkeeping unchanged for callers that probe the cursor.
                if (len != 0)
                {
                    candidate->Used = candidateOffset + len;
                }

                self->m_Current = candidate;
                segment = candidate;
                offset = candidateOffset;
                return;
            }
        }

        var created = Acquire(self, len, align, usage);
        created->Used = len;
        self->m_Current = created;
        segment = created;
        offset = 0;
    }

    /// <summary>
    /// Grows a block in place, so its address, and therefore any device binding to it, stays valid.
    /// </summary>
    /// <remarks>
    /// Two cases can succeed: the block is the newest one of its segment, in which case the cursor moves, or the range
    /// right behind the block was released, in which case the growth consumes it. Anything else has to be served by a new
    /// block, because moving the live one is exactly what the arena never does.
    /// </remarks>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be resized.</param>
    /// <param name="oldLen">The current size of the memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used, the block cannot move).</param>
    /// <returns><see langword="true"/> if the block was resized in place; otherwise, <see langword="false"/>.</returns>
    private static bool Resize(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        var self = (UnifiedAllocator*)ctx;

        if (TryGetActiveBlock(self, ptr, oldLen, out var active, out var offset))
        {
            var newUsed = offset + newLen;
            if (newUsed < offset || newUsed > active->Size)
            {
                return false;
            }

            active->Used = newUsed;
            return true;
        }

        // The block is not the newest one of its segment, so the cursor cannot move. It can still grow in place if the
        // range right behind it was released: the growth then eats into that range instead of moving the block.
        return TryGrowIntoFreeRange(self, ptr, oldLen, newLen);
    }

    /// <summary>
    /// Grows a block in place by consuming the released range that follows it.
    /// </summary>
    /// <remarks>
    /// This is what keeps a growing collection from being copied to another address when the arena happens to have room
    /// right behind it: the block's address is the one the device may already be bound to, so growing it is only possible
    /// at all because nothing that stays live moves.
    /// </remarks>
    /// <param name="self">The allocator instance.</param>
    /// <param name="ptr">The pointer to the block.</param>
    /// <param name="oldLen">The current size of the block in bytes.</param>
    /// <param name="newLen">The size the block should grow to, in bytes.</param>
    /// <returns><see langword="true"/> if the block was grown in place; otherwise, <see langword="false"/>.</returns>
    private static bool TryGrowIntoFreeRange(UnifiedAllocator* self, void* ptr, nuint oldLen, nuint newLen)
    {
        if (newLen <= oldLen || !TryFind(self->m_First, ptr, out var segment))
        {
            return false;
        }

        var start = (nuint)ptr - (nuint)segment->Host;
        if (start > segment->Used || oldLen > segment->Used - start)
        {
            return false;
        }

        var end = start + newLen;
        if (end < start)
        {
            return false;
        }

        // The range that follows the block has to be parked: a block that follows it is live, and growing over it would
        // corrupt it.
        FreeNode* following = null;

        for (var node = segment->FreeHead; node != null; node = node->Next)
        {
            if (node->Offset < start + oldLen)
            {
                continue;
            }

            if (node->Offset <= end)
            {
                following = node;
            }

            break;
        }

        if (following == null)
        {
            return false;
        }

        var limit = following->Offset + following->Size;
        if (end > limit)
        {
            return false;
        }

        // The consumed part is a whole number of aligned units, so the remainder stays a valid parked range.
        var consumed = AlignUp(end - following->Offset, self->m_Alignment);
        if (consumed == 0 || consumed > following->Size)
        {
            return false;
        }

        if (consumed == following->Size)
        {
            UnlinkFree(segment, following);
        }
        else
        {
            following->Offset += consumed;
            following->Size -= consumed;
            segment->FreeBytes -= consumed;
        }

        return true;
    }

    /// <summary>
    /// Releases a block that a caller obtained from the vtable, and parks its range so the next allocation of that size
    /// reuses it.
    /// </summary>
    /// <remarks>
    /// The range is rounded up to the arena alignment and clamped to the segment's cursor. A range that reaches the
    /// cursor pulls the cursor back, which shrinks the arena; any other range is parked on the segment's free list. The
    /// caller has to release a block it really allocated, exactly as with any arena: the allocator keeps no per-block
    /// header, so a wrong length is taken at face value.
    /// </remarks>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    /// <param name="ptr">The pointer to the memory block to be freed.</param>
    /// <param name="len">The size of the memory block to be freed in bytes.</param>
    /// <param name="alignment">The alignment requirement for the memory block (not used).</param>
    private static void Free(void* ctx, void* ptr, nuint len, byte alignment)
    {
        var self = (UnifiedAllocator*)ctx;

        if (!TryFind(self->m_First, ptr, out var segment))
        {
            return;
        }

        FreeRange(segment, self->m_Alignment, (nuint)ptr - (nuint)segment->Host, len);
    }

    /// <summary>
    /// Resizes the block in place, since a block the device may be bound to can never move.
    /// </summary>
    /// <param name="ctx">The context pointer to the UnifiedAllocator instance.</param>
    /// <param name="ptr">The pointer to the original memory block.</param>
    /// <param name="oldLen">The size of the original memory block in bytes.</param>
    /// <param name="newLen">The desired new size of the memory block in bytes.</param>
    /// <param name="alignment">The alignment requirement for the new memory block (not used).</param>
    /// <returns>The original pointer on success; otherwise, <see langword="null"/>.</returns>
    private static void* Remap(void* ctx, void* ptr, nuint oldLen, nuint newLen, byte alignment)
    {
        // Shrinking keeps the address unconditionally; growing requires the block to be the most recent one.
        if (newLen <= oldLen)
        {
            return ptr;
        }

        return Resize(ctx, ptr, oldLen, newLen, alignment) ? ptr : null;
    }

    /// <summary>
    /// Determines whether <paramref name="ptr"/> is the most recent allocation of the segment that served it, and
    /// reports that segment together with the block's offset inside it.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="ptr">The pointer to the memory block.</param>
    /// <param name="len">The size of the memory block in bytes.</param>
    /// <param name="segment">When the method returns, the segment that owns the block.</param>
    /// <param name="offset">When the method returns, the offset of the block inside the segment.</param>
    /// <returns><see langword="true"/> if the block can be reclaimed in place; otherwise, <see langword="false"/>.</returns>
    private static bool TryGetActiveBlock(UnifiedAllocator* self, void* ptr, nuint len, out Segment* segment, out nuint offset)
    {
        segment = self->m_Current;
        offset = 0;

        if (segment == null || ptr == null || segment->Host == null)
        {
            return false;
        }

        // Reject foreign pointers and blocks that are not the most recent allocation of the active segment.
        var start = (nuint)segment->Host;
        var address = (nuint)ptr;
        if (address < start)
        {
            return false;
        }

        offset = address - start;
        return offset <= segment->Used && len <= segment->Used - offset && offset + len == segment->Used;
    }

    /// <summary>
    /// Acquires a segment large enough for a block of <paramref name="len"/> bytes aligned to
    /// <paramref name="alignment"/> and appends it to the chain.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="len">The length of the block the segment must hold, in bytes.</param>
    /// <param name="alignment">The alignment of that block, in bytes.</param>
    /// <param name="usage">What the CPU does with the block, which the provider maps to a heap.</param>
    /// <returns>The new segment, with its cursor at the start of the block.</returns>
    /// <exception cref="OutOfMemoryException">
    /// Thrown when the segment or its bookkeeping cannot be allocated, or when the backend cannot create a segment that
    /// large.
    /// </exception>
    private static Segment* Acquire(UnifiedAllocator* self, nuint len, nuint alignment, UnifiedMemoryUsage usage)
    {
        // The base alignment covers the block's alignment, and the segment size is a whole number of aligned units,
        // which is the condition an imported host range has to satisfy.
        var baseAlignment = self->m_Provider.Alignment;
        if (baseAlignment < alignment)
        {
            baseAlignment = alignment;
        }

        if (baseAlignment == 0 || (baseAlignment & (baseAlignment - 1)) != 0)
        {
            throw new InvalidOperationException($"The unified provider reported an alignment of {baseAlignment} bytes, which is not a power of two.");
        }

        var size = self->m_SegmentSizes[usage];

        // A single block larger than the segment size gets a segment of its own, with room for the alignment padding.
        if (alignment > maxSize - len)
        {
            throw new OutOfMemoryException($"The unified allocator cannot serve an allocation of {len} bytes.");
        }

        var required = len + alignment;
        if (size < required)
        {
            size = required;
        }

        var rounded = AlignUp(size, baseAlignment);
        if (rounded < size)
        {
            throw new OutOfMemoryException($"The unified allocator cannot serve an allocation of {len} bytes.");
        }

        size = rounded;

        // The backend's own limit decides whether this can work at all: Metal caps a buffer at maxBufferLength, Vulkan
        // caps an allocation at maxMemoryAllocationSize, and both are well below the address space on some families.
        var limit = self->m_Provider.MaxSegmentSize;
        if (limit != 0 && size > limit)
        {
            throw new OutOfMemoryException($"The backend limits a unified segment to {limit} bytes, but the allocation needs {size} bytes.");
        }

        // The budget is a ceiling on what the arena may hold in total. The segments that hold nothing are handed back
        // first, because they are the part of the ceiling the arena is wasting; only what is still there afterwards can be
        // spent. The caller sees the refusal at the allocation that needed the room rather than as an out-of-memory from
        // the driver once the memory is really gone.
        var budget = self->m_Budget != 0 ? self->m_Budget : self->m_Provider.WorkingSetLimit;
        if (budget != 0 && !HasRoom(self, budget, size))
        {
            throw new OutOfMemoryException($"The unified allocator holds {TotalCapacity(self)} of its {budget} byte budget and cannot add a segment of {size} bytes.");
        }

        var host = self->m_Provider.Alloc(size, baseAlignment, usage, out var deviceHandle, out var deviceAddress, out var kind);

        var segment = (Segment*)UnmanagedAllocator.Global.Alloc((nuint)sizeof(Segment), Native.GetAlignment((int)sizeof(Segment)));
        if (segment == null)
        {
            self->m_Provider.Free(host, size, deviceHandle);
            throw new OutOfMemoryException($"The unified allocator cannot allocate {sizeof(Segment)} bytes of bookkeeping.");
        }

        *segment = default;
        segment->Host = (byte*)host;
        segment->Size = size;
        segment->DeviceHandle = deviceHandle;
        segment->DeviceAddress = deviceAddress;
        segment->Kind = kind;
        segment->Usage = usage;

        if (self->m_Last == null)
        {
            self->m_First = segment;
        }
        else
        {
            self->m_Last->Next = segment;
        }

        self->m_Last = segment;
        return segment;
    }

    /// <summary>
    /// Finds the segment that contains <paramref name="ptr"/>.
    /// </summary>
    /// <param name="first">The oldest segment of the chain to search.</param>
    /// <param name="ptr">The pointer to look for.</param>
    /// <param name="found">When the method returns, the segment that contains <paramref name="ptr"/>.</param>
    /// <returns><see langword="true"/> if a segment contains the pointer; otherwise, <see langword="false"/>.</returns>
    private static bool TryFind(Segment* first, void* ptr, out Segment* found)
    {
        var address = (nuint)ptr;

        for (var segment = first; segment != null; segment = segment->Next)
        {
            // A device-only segment has no CPU address, so no pointer can belong to it.
            if (segment->Host == null)
            {
                continue;
            }

            var start = (nuint)segment->Host;
            if (address >= start && address - start < segment->Size)
            {
                found = segment;
                return true;
            }
        }

        found = null;
        return false;
    }

    /// <summary>
    /// Finds the segment that owns <paramref name="block"/>.
    /// </summary>
    /// <param name="first">The oldest segment of the chain to search.</param>
    /// <param name="block">The block to locate.</param>
    /// <returns>The segment that owns the block, or <see langword="null"/> when this allocator does not own it.</returns>
    private static Segment* FindByBlock(Segment* first, in UnifiedMemoryBlock block)
    {
        for (var segment = first; segment != null; segment = segment->Next)
        {
            // A device-only segment has no CPU address, so its device handle is the only way to recognise it.
            if (block.DeviceHandle != null && segment->DeviceHandle == block.DeviceHandle)
            {
                return segment;
            }

            if (block.SegmentHost != null && segment->Host == block.SegmentHost)
            {
                return segment;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the offset of the first aligned address at or after the segment's cursor, measuring from the segment's
    /// base address.
    /// </summary>
    /// <param name="segment">The segment to allocate from.</param>
    /// <param name="alignment">A non-zero power of two.</param>
    /// <returns>The offset of the aligned address inside the segment.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint AlignOffset(Segment* segment, nuint alignment)
    {
        // A device-only segment has no CPU address to align against, so the provider's aligned base is assumed and the
        // cursor alone decides the offset.
        if (segment->Host == null)
        {
            var offset = AlignUp(segment->Used, alignment);
            return offset >= segment->Used && offset <= segment->Size ? offset : segment->Size;
        }

        var start = (nuint)segment->Host;
        var aligned = Native.AlignForward(start + segment->Used, alignment);

        // Alignment rounds up, so the result can only fall behind the cursor when the addition wrapped.
        return aligned < start ? segment->Size : aligned - start;
    }

    /// <summary>
    /// Rounds <paramref name="value"/> up to the next multiple of <paramref name="alignment"/>.
    /// </summary>
    /// <param name="value">The value to round up.</param>
    /// <param name="alignment">A non-zero power of two.</param>
    /// <returns>The rounded value, or <see langword="0"/> if the rounding would overflow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint AlignUp(nuint value, nuint alignment)
    {
        var mask = alignment - 1;

        // `~mask` is the largest value that can be rounded up without overflowing.
        return value > ~mask ? 0 : (value + mask) & ~mask;
    }

    /// <summary>
    /// Serves <paramref name="len"/> bytes out of a range parked on the segment's free list.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="segment">The segment to serve from.</param>
    /// <param name="len">The length of the block to serve, in bytes.</param>
    /// <param name="alignment">The alignment of the block, in bytes.</param>
    /// <param name="offset">When the method returns, the offset of the block inside the segment.</param>
    /// <returns><see langword="true"/> if a parked range was found and still has room; otherwise, <see langword="false"/>.</returns>
    private static bool TryServeFromFreeList(UnifiedAllocator* self, Segment* segment, nuint len, nuint alignment, out nuint offset)
    {
        offset = 0;

        // Every parked range starts on an aligned unit and is a whole number of them long, so it can only serve a
        // request that asks for no more alignment than the arena hands out itself.
        if (alignment > self->m_Alignment)
        {
            return false;
        }

        // A zero-length request takes the first address without consuming a range, exactly as the cursor path does.
        if (len == 0)
        {
            if (segment->FreeHead == null)
            {
                return false;
            }

            offset = segment->FreeHead->Offset;
            return true;
        }

        var wanted = AlignUp(len, self->m_Alignment);
        if (wanted == 0)
        {
            return false;
        }

        // Best fit: the tightest range that still holds the request. A large range left for a large request is what keeps a
        // mixed-size churn from fragmenting the segment into pieces nothing big can use again.
        FreeNode* best = null;

        for (var node = segment->FreeHead; node != null; node = node->Next)
        {
            if (node->Size < wanted)
            {
                continue;
            }

            if (best == null || node->Size < best->Size)
            {
                best = node;

                // An exact fit cannot be improved on, so the scan stops here.
                if (best->Size == wanted)
                {
                    break;
                }
            }
        }

        if (best == null)
        {
            return false;
        }

        offset = best->Offset;

        if (best->Size == wanted)
        {
            UnlinkFree(segment, best);
            return true;
        }

        // The remainder keeps both its offset and its length aligned, so the next block served from it is aligned too.
        best->Offset += wanted;
        best->Size -= wanted;
        segment->FreeBytes -= wanted;
        return true;
    }

    /// <summary>
    /// Sums the size of every segment the allocator holds.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <returns>The total capacity in bytes.</returns>
    private static nuint TotalCapacity(UnifiedAllocator* self)
    {
        nuint capacity = 0;

        for (var segment = self->m_First; segment != null; segment = segment->Next)
        {
            capacity += segment->Size;
        }

        return capacity;
    }

    /// <summary>
    /// Determines whether a segment of <paramref name="size"/> bytes fits in the budget, handing back the segments that
    /// hold nothing first.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="budget">The total size the segments may reach.</param>
    /// <param name="size">The size of the segment about to be acquired.</param>
    /// <returns><see langword="true"/> if the arena may grow by <paramref name="size"/> bytes; otherwise, <see langword="false"/>.</returns>
    private static bool HasRoom(UnifiedAllocator* self, nuint budget, nuint size)
    {
        var capacity = TotalCapacity(self);

        if (capacity <= budget && size <= budget - capacity)
        {
            return true;
        }

        // The arena is already inside the budget it can spend, or it is not, but the empty segments are pure waste either
        // way, so they go back before the refusal is decided.
        Trim(self, UnifiedMemoryUsage.General, true);

        capacity = TotalCapacity(self);
        return capacity <= budget && size <= budget - capacity;
    }

    /// <summary>
    /// Rewinds the segments of one usage and drops the releases that were waiting on them.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="usage">The heap to rewind.</param>
    /// <returns>The number of segments that were rewound.</returns>
    private static int ResetUsage(UnifiedAllocator* self, UnifiedMemoryUsage usage)
    {
        var rewound = 0;

        for (var segment = self->m_First; segment != null; segment = segment->Next)
        {
            if (segment->Usage != usage)
            {
                continue;
            }

            ReleaseFreeList(segment);
            segment->Used = 0;
            rewound++;
        }

        // A queued release whose range no longer exists would park garbage the next time it is collected.
        DropPending(self, usage);

        if (self->m_Current != null && self->m_Current->Usage == usage)
        {
            self->m_Current = null;
        }

        return rewound;
    }

    /// <summary>
    /// Drops the queued releases that belong to the segments of one usage.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="usage">The heap whose waiting releases to drop.</param>
    private static void DropPending(UnifiedAllocator* self, UnifiedMemoryUsage usage)
    {
        PendingRelease* previous = null;
        var pending = self->m_Pending;

        while (pending != null)
        {
            var next = pending->Next;

            if (pending->Segment != null && pending->Segment->Usage == usage)
            {
                if (previous == null)
                {
                    self->m_Pending = next;
                }
                else
                {
                    previous->Next = next;
                }

                UnmanagedAllocator.Global.Free(pending, (nuint)sizeof(PendingRelease), pendingAlignment);
                pending = next;
                continue;
            }

            previous = pending;
            pending = next;
        }
    }

    /// <summary>
    /// Releases the segments that hold no live block.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="usage">The usage to restrict the sweep to, which is ignored when <paramref name="everyUsage"/> is set.</param>
    /// <param name="everyUsage">Whether every usage is a candidate.</param>
    /// <returns>The number of segments that were released.</returns>
    private static int Trim(UnifiedAllocator* self, UnifiedMemoryUsage usage, bool everyUsage)
    {
        var released = 0;
        var segment = self->m_First;

        while (segment != null)
        {
            var next = segment->Next;

            if ((everyUsage || segment->Usage == usage) && segment->Used == segment->FreeBytes)
            {
                UnlinkSegment(self, segment);
                ReleaseSegment(self, segment);
                released++;
            }

            segment = next;
        }

        return released;
    }

    /// <summary>
    /// Queues a range to be released once the device has passed a fence value.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="segment">The segment that owns the range.</param>
    /// <param name="offset">The offset of the range inside the segment.</param>
    /// <param name="length">The length of the range in bytes.</param>
    /// <param name="fenceValue">The fence value the range waits for.</param>
    /// <returns><see langword="true"/> if the range was queued; otherwise, <see langword="false"/>.</returns>
    private static bool Enqueue(UnifiedAllocator* self, Segment* segment, nuint offset, nuint length, ulong fenceValue)
    {
        if (segment == null || offset > segment->Used || length > segment->Used - offset)
        {
            return false;
        }

        // An empty block has no range to hold, so there is nothing to wait for.
        if (length == 0)
        {
            return true;
        }

        var pending = (PendingRelease*)UnmanagedAllocator.Global.Alloc((nuint)sizeof(PendingRelease), pendingAlignment);
        if (pending == null)
        {
            return false;
        }

        pending->Next = null;
        pending->Segment = segment;
        pending->Offset = offset;
        pending->Length = length;
        pending->FenceValue = fenceValue;

        // The queue is kept in submission order so that a release sweep can stop at the first value the device has not
        // reached yet, which is the common case: one frame's worth of releases is pending at a time.
        if (self->m_Pending == null)
        {
            self->m_Pending = pending;
            return true;
        }

        var tail = self->m_Pending;
        while (tail->Next != null)
        {
            tail = tail->Next;
        }

        tail->Next = pending;
        return true;
    }

    /// <summary>
    /// Removes <paramref name="segment"/> from the chain and moves the allocator's cursors off it.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="segment">The segment to unlink.</param>
    private static void UnlinkSegment(UnifiedAllocator* self, Segment* segment)
    {
        if (self->m_First == segment)
        {
            self->m_First = segment->Next;
        }
        else
        {
            for (var previous = self->m_First; previous != null; previous = previous->Next)
            {
                if (previous->Next != segment)
                {
                    continue;
                }

                previous->Next = segment->Next;
                break;
            }
        }

        if (self->m_Last == segment)
        {
            // The chain runs from the oldest segment to the newest, so the newest one left is the last reachable one.
            self->m_Last = null;

            for (var candidate = self->m_First; candidate != null; candidate = candidate->Next)
            {
                self->m_Last = candidate;
            }
        }

        if (self->m_Current == segment)
        {
            self->m_Current = null;
        }
    }

    /// <summary>
    /// Returns one segment's memory to the backend and releases its bookkeeping.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    /// <param name="segment">The segment to release, which must already be unlinked.</param>
    private static void ReleaseSegment(UnifiedAllocator* self, Segment* segment)
    {
        ReleaseFreeList(segment);
        self->m_Provider.Free(segment->Host, segment->Size, segment->DeviceHandle);
        UnmanagedAllocator.Global.Free(segment, (nuint)sizeof(Segment), Native.GetAlignment((int)sizeof(Segment)));
    }

    /// <summary>
    /// Releases the range at <paramref name="offset"/> and hands it back to the arena.
    /// </summary>
    /// <param name="segment">The segment that owns the range.</param>
    /// <param name="alignment">The alignment the arena hands out.</param>
    /// <param name="offset">The offset of the range inside the segment.</param>
    /// <param name="length">The length of the range in bytes.</param>
    /// <returns><see langword="true"/> if the range was reclaimed; otherwise, <see langword="false"/>.</returns>
    private static bool FreeRange(Segment* segment, nuint alignment, nuint offset, nuint length)
    {
        if (segment == null || alignment == 0)
        {
            return false;
        }

        if (offset > segment->Used || length > segment->Used - offset)
        {
            return false;
        }

        // Releasing an empty block is a valid request that needs no bookkeeping.
        if (length == 0)
        {
            return true;
        }

        // The range is measured in whole aligned units, so that whatever is served from it later is aligned as well, and
        // the padding the alignment left behind is reclaimed with it.
        var size = AlignUp(length, alignment);
        if (size == 0 || size > segment->Used - offset)
        {
            size = segment->Used - offset;
        }

        // A range that reaches the cursor needs no bookkeeping at all: the cursor moves back over it, and every range the
        // moved cursor now reaches is joined to it.
        if (offset + size == segment->Used)
        {
            segment->Used = offset;
            PullTail(segment);
            return true;
        }

        return Park(segment, offset, size);
    }

    /// <summary>
    /// Moves the cursor back over every parked range that ends at it.
    /// </summary>
    /// <param name="segment">The segment whose cursor to pull back.</param>
    private static void PullTail(Segment* segment)
    {
        bool moved;

        do
        {
            moved = false;

            for (var node = segment->FreeHead; node != null; node = node->Next)
            {
                if (node->Offset + node->Size != segment->Used)
                {
                    continue;
                }

                segment->Used = node->Offset;
                UnlinkFree(segment, node);
                moved = true;
                break;
            }
        } while (moved);
    }

    /// <summary>
    /// Puts a range on the segment's free list, in offset order, joining it with any neighbour it touches.
    /// </summary>
    /// <remarks>
    /// Joining is what keeps the list short: a frame that releases its blocks in allocation order ends up with one range
    /// per segment rather than a list the length of the frame, which is also what lets the next frame allocate a block
    /// larger than any single released range.
    /// </remarks>
    /// <param name="segment">The segment that owns the range.</param>
    /// <param name="offset">The offset of the range inside the segment.</param>
    /// <param name="size">The size of the range in bytes.</param>
    /// <returns><see langword="true"/> if the range was recorded; otherwise, <see langword="false"/>.</returns>
    private static bool Park(Segment* segment, nuint offset, nuint size)
    {
        var end = offset + size;

        // The node that starts in front of this range, and the first one that starts after it.
        FreeNode* previous = null;
        var next = segment->FreeHead;

        while (next != null && next->Offset < offset)
        {
            previous = next;
            next = next->Next;
        }

        // A range that overlaps what is already parked was released twice, or was never a block this arena handed out.
        // Refusing it is what keeps a double release from turning into two blocks on top of each other later.
        if (previous != null && previous->Offset + previous->Size > offset)
        {
            return false;
        }

        if (next != null && next->Offset < end)
        {
            return false;
        }

        // Every range that starts at or inside this one is absorbed by it.
        while (next != null && next->Offset <= end)
        {
            var following = next->Next;
            var nextEnd = next->Offset + next->Size;

            if (nextEnd > end)
            {
                end = nextEnd;
            }

            UnlinkFree(segment, next);
            next = following;
        }

        // A range that ends exactly where this one starts belongs to this one now.
        if (previous != null && previous->Offset + previous->Size >= offset)
        {
            var previousEnd = previous->Offset + previous->Size;
            var mergedEnd = end > previousEnd ? end : previousEnd;

            segment->FreeBytes += mergedEnd - previousEnd;
            previous->Size = mergedEnd - previous->Offset;
            return true;
        }

        var node = (FreeNode*)UnmanagedAllocator.Global.Alloc((nuint)sizeof(FreeNode), freeAlignment);
        if (node == null)
        {
            // Without the bookkeeping the range is lost until the segment is reset, which is the same trade the arena
            // makes when a segment's own header cannot be allocated.
            return false;
        }

        node->Next = next;
        node->Offset = offset;
        node->Size = end - offset;

        if (previous == null)
        {
            segment->FreeHead = node;
        }
        else
        {
            previous->Next = node;
        }

        segment->FreeBytes += node->Size;
        return true;
    }

    /// <summary>
    /// Removes <paramref name="node"/> from the free list of <paramref name="segment"/>, returns its bookkeeping and
    /// subtracts its size from the segment's parked bytes.
    /// </summary>
    /// <param name="segment">The segment that owns the node.</param>
    /// <param name="node">The node to remove.</param>
    private static void UnlinkFree(Segment* segment, FreeNode* node)
    {
        FreeNode* previous = null;

        for (var candidate = segment->FreeHead; candidate != null; previous = candidate, candidate = candidate->Next)
        {
            if (candidate != node)
            {
                continue;
            }

            if (previous == null)
            {
                segment->FreeHead = candidate->Next;
            }
            else
            {
                previous->Next = candidate->Next;
            }

            segment->FreeBytes -= candidate->Size;
            UnmanagedAllocator.Global.Free(candidate, (nuint)sizeof(FreeNode), freeAlignment);
            return;
        }
    }

    /// <summary>
    /// Returns every node of the deferred-release queue and clears it, without releasing the ranges it describes.
    /// </summary>
    /// <param name="self">The allocator instance.</param>
    private static void ReleasePending(UnifiedAllocator* self)
    {
        var pending = self->m_Pending;

        while (pending != null)
        {
            var next = pending->Next;
            UnmanagedAllocator.Global.Free(pending, (nuint)sizeof(PendingRelease), pendingAlignment);
            pending = next;
        }

        self->m_Pending = null;
    }

    /// <summary>
    /// Returns every node of a segment's free list and clears it.
    /// </summary>
    /// <param name="segment">The segment to clear.</param>
    private static void ReleaseFreeList(Segment* segment)
    {
        var node = segment->FreeHead;

        while (node != null)
        {
            var next = node->Next;
            UnmanagedAllocator.Global.Free(node, (nuint)sizeof(FreeNode), freeAlignment);
            node = next;
        }

        segment->FreeHead = null;
        segment->FreeBytes = 0;
    }

    /// <summary>
    /// Header of a single segment. It is described with plain unmanaged fields so that it can be stored directly in
    /// unmanaged memory, which keeps the allocator itself unmanaged and therefore storable in a stable context.
    /// </summary>
    private struct Segment
    {
        /// <summary>
        /// The next, newer segment, or <see langword="null"/> for the newest segment.
        /// </summary>
        public Segment* Next;

        /// <summary>
        /// The CPU address of the first byte of the segment, or <see langword="null"/> for a device-only segment.
        /// </summary>
        public byte* Host;

        /// <summary>
        /// The size of the segment, in bytes.
        /// </summary>
        public nuint Size;

        /// <summary>
        /// The number of bytes handed out from the start of the segment, including alignment padding.
        /// </summary>
        public nuint Used;

        /// <summary>
        /// The oldest parked range of the segment, or <see langword="null"/> when nothing was released yet. The list is
        /// kept ordered by offset so that neighbours can be joined when a range is released between two others.
        /// </summary>
        public FreeNode* FreeHead;

        /// <summary>
        /// The number of bytes parked on <see cref="FreeHead"/>, which is what <see cref="GetUsed(UnifiedMemoryUsage)"/>
        /// subtracts from <see cref="Used"/>.
        /// </summary>
        public nuint FreeBytes;

        /// <summary>
        /// The opaque device handle that owns the segment, or <see langword="null"/> when the backend only exposes an
        /// address.
        /// </summary>
        public void* DeviceHandle;

        /// <summary>
        /// The device address of the first byte of the segment, or <see langword="0"/> when the backend has no
        /// buffer-device-address support.
        /// </summary>
        public ulong DeviceAddress;

        /// <summary>
        /// How the segment is shared between the CPU and the device, as reported by the provider when it was acquired.
        /// </summary>
        public UnifiedMemoryKind Kind;

        /// <summary>
        /// What the CPU does with the segment, which is why the provider chose its heap. A segment only serves blocks of
        /// the same usage.
        /// </summary>
        public UnifiedMemoryUsage Usage;
    }

    /// <summary>
    /// A block whose release is waiting for the device to pass a fence value.
    /// </summary>
    /// <remarks>
    /// The node is kept beside the arena for the same reason the free-list nodes are: neither the range it describes nor
    /// the segment header has a place to hold a queue pointer that survives a reset.
    /// </remarks>
    private struct PendingRelease
    {
        /// <summary>
        /// The next waiting release, which was queued after this one, or <see langword="null"/> for the last one.
        /// </summary>
        public PendingRelease* Next;

        /// <summary>
        /// The segment that owns the range.
        /// </summary>
        public Segment* Segment;

        /// <summary>
        /// The offset of the range inside its segment, in bytes.
        /// </summary>
        public nuint Offset;

        /// <summary>
        /// The length of the range in bytes.
        /// </summary>
        public nuint Length;

        /// <summary>
        /// The fence value the device has to pass before the range is handed back.
        /// </summary>
        public ulong FenceValue;
    }

    /// <summary>
    /// A range of a segment that was released and can be served again, in offset order inside the segment's list.
    /// </summary>
    /// <remarks>
    /// The node is allocated beside the segment rather than stored inside the range it describes: a device-only segment
    /// has no CPU address to write a header through, and reusing device memory is exactly where a per-frame allocator
    /// needs the free list most. One small allocation per released range is the price of that, and it is returned as soon
    /// as the range is served again or joined with a neighbour.
    /// </remarks>
    private struct FreeNode
    {
        /// <summary>
        /// The next parked range, which starts at a higher offset, or <see langword="null"/> for the last one.
        /// </summary>
        public FreeNode* Next;

        /// <summary>
        /// The offset of the range inside its segment, in bytes.
        /// </summary>
        public nuint Offset;

        /// <summary>
        /// The size of the range, in bytes, which is always a whole number of the arena's aligned units.
        /// </summary>
        public nuint Size;
    }
}