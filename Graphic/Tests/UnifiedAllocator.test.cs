using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Graphic.Tests;

[TestFixture]
public sealed unsafe partial class UnifiedAllocatorTests
{
    /// <summary>
    /// The segment size used by the tests, small enough that a few allocations fill a segment.
    /// </summary>
    /// <remarks>
    /// A segment is rounded up to the alignment of the provider it is acquired from, which the host provider reports as the
    /// system page size: 4 KiB on one machine and 16 KiB on another. The size the tests ask for is a multiple of that, so
    /// that what the allocator acquires is what the expectations here are written against.
    /// </remarks>
    private static readonly nuint s_SegmentSize = (nuint)Math.Max(4096, (nuint)Environment.SystemPageSize);

    /// <summary>
    /// The alignment the emulated backend requires of segment base addresses and sizes.
    /// </summary>
    /// <remarks>
    /// Every backend of this layer reports a small power of two, and 256 is the largest of them. The host's page size must
    /// not stand in for it: a segment is rounded up to this alignment, so a page size that differs between platforms turns
    /// every size these tests assert into a property of the host rather than of the allocator.
    /// </remarks>
    private const nuint SEGMENT_ALIGNMENT = 256;

    /// <summary>
    /// Emulates the memory of a graphics backend so the sharing semantics can be asserted without a GPU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The emulation is shaped like a unified memory SoC rather than like a single heap, because that is where the
    /// interesting cases are: <see cref="UnifiedMemoryUsage.General"/> can land in a coherent heap whose CPU and device
    /// view is the same block (Apple silicon shared storage, an AMD APU coherent system memory type), while
    /// <see cref="UnifiedMemoryUsage.General"/> and <see cref="UnifiedMemoryUsage.General"/> can land in a non-coherent
    /// heap modeled by a second block that only cache maintenance can bring in sync (the write-combined upload heap of an
    /// APU, or the non-coherent <c>HOST_VISIBLE</c> Vulkan memory type of most ARM SoCs), and
    /// <see cref="UnifiedMemoryUsage.Device"/> lands in a block the CPU cannot address at all and is filled by a
    /// device-side copy, the way <c>MTLStorageModePrivate</c> memory is.
    /// </para>
    /// <para>
    /// The class also documents the pattern a real bridge uses: keep a <see cref="GCHandle"/> to the bridge and use its
    /// pointer as the <see cref="UnifiedProvider.Context"/>, because a managed object's address is not stable.
    /// </para>
    /// </remarks>
    private sealed class ShadowDevice : IUnifiedProvider, IDisposable
    {
        /// <summary>
        /// Provider methods for vtable.
        /// </summary>
        private static readonly UnifiedProvider.VTable methods = new(
            &AllocThunk,
            &FreeThunk,
            &FlushThunk,
            &InvalidateThunk,
            &CopyThunk,
            &SubmitThunk,
            &WaitThunk,
            &AlignmentThunk,
            &MaxSegmentSizeThunk,
            &WorkingSetLimitThunk,
            &SetResidencyThunk);

        /// <summary>
        /// The sharing kind of the heap that serves each usage.
        /// </summary>
        private readonly Dictionary<UnifiedMemoryUsage, UnifiedMemoryKind> m_Heaps;

        /// <summary>
        /// The device's view of every live segment: the CPU address (null when device only), the device block, the
        /// segment size and its kind.
        /// </summary>
        private readonly List<(nuint Host, nuint Device, nuint Size, UnifiedMemoryKind Kind)> m_Segments = [];

        /// <summary>
        /// The copies the bridge recorded but has not submitted yet.
        /// </summary>
        private readonly List<(nuint Destination, nuint Source, nuint Size)> m_Recorded = [];

        /// <summary>
        /// The copies the bridge submitted but has not completed yet.
        /// </summary>
        private readonly List<(nuint Destination, nuint Source, nuint Size)> m_Submitted = [];

        /// <summary>
        /// The handle that roots this instance and supplies the provider's context pointer.
        /// </summary>
        private GCHandle m_Self;

        /// <param name="heaps">The sharing kind of the heap that serves each usage.</param>
        /// <param name="maxSegmentSize">The largest segment the emulated device accepts, or <see langword="0"/> for no limit.</param>
        /// <param name="workingSetLimit">The working set the emulated device recommends, or <see langword="0"/> for no opinion.</param>
        public ShadowDevice(Dictionary<UnifiedMemoryUsage, UnifiedMemoryKind> heaps, nuint maxSegmentSize = 0, nuint workingSetLimit = 0)
        {
            m_Heaps = heaps;
            MaxSegmentSize = maxSegmentSize;
            WorkingSetLimit = workingSetLimit;
            Alignment = SEGMENT_ALIGNMENT;
        }

        /// <inheritdoc/>
        public nuint Alignment { get; }

        /// <inheritdoc/>
        public nuint MaxSegmentSize { get; }

        /// <inheritdoc/>
        public nuint WorkingSetLimit { get; }

        /// <summary>
        /// Gets the last residency the bridge was asked for, or <see langword="null"/> when it was never asked.
        /// </summary>
        public UnifiedMemoryResidency? LastResidency { get; private set; }

        /// <summary>
        /// Gets the number of live segments, including device-only ones.
        /// </summary>
        public int LiveSegments => m_Segments.Count;

        /// <summary>
        /// Creates a device that exposes a coherent read-write heap, non-coherent upload and readback heaps, and a
        /// CPU-invisible device heap, which is the common shape of a unified memory SoC.
        /// </summary>
        /// <param name="maxSegmentSize">The largest segment the emulated device accepts, or <see langword="0"/> for no limit.</param>
        /// <param name="workingSetLimit">The working set the emulated device recommends, or <see langword="0"/> for no opinion.</param>
        /// <returns>The emulated device.</returns>
        public static ShadowDevice CreateSoC(nuint maxSegmentSize = 0, nuint workingSetLimit = 0) => new(new Dictionary<UnifiedMemoryUsage, UnifiedMemoryKind>
        {
            [UnifiedMemoryUsage.General] = UnifiedMemoryKind.SharedCoherent,
            [UnifiedMemoryUsage.Device] = UnifiedMemoryKind.Device
        }, maxSegmentSize, workingSetLimit);

        /// <summary>
        /// Creates a device whose shared heap is <em>not</em> coherent, which is the shape most ARM SoCs expose: the pages are
        /// shared but the CPU has to publish and invalidate ranges, so the cache maintenance paths keep their coverage.
        /// </summary>
        /// <param name="maxSegmentSize">The largest segment the emulated device accepts, or <see langword="0"/> for no limit.</param>
        /// <param name="workingSetLimit">The working set the emulated device recommends, or <see langword="0"/> for no opinion.</param>
        /// <returns>The emulated device.</returns>
        public static ShadowDevice CreateNonCoherentSoC(nuint maxSegmentSize = 0, nuint workingSetLimit = 0) => new(new Dictionary<UnifiedMemoryUsage, UnifiedMemoryKind>
        {
            [UnifiedMemoryUsage.General] = UnifiedMemoryKind.Shared,
            [UnifiedMemoryUsage.Device] = UnifiedMemoryKind.Device
        }, maxSegmentSize, workingSetLimit);

        /// <inheritdoc/>
        public UnifiedProvider GetProvider()
        {
            if (!m_Self.IsAllocated)
            {
                m_Self = GCHandle.Alloc(this);
            }

            return new UnifiedProvider((void*)GCHandle.ToIntPtr(m_Self), methods);
        }

        /// <inheritdoc/>
        public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
        {
            kind = m_Heaps.TryGetValue(usage, out var configured) ? configured : UnifiedMemoryKind.Host;

            // A backend limit is a hard failure, not a clamp: the caller has to ask for something smaller.
            if (MaxSegmentSize != 0 && size > MaxSegmentSize)
            {
                throw new OutOfMemoryException($"The emulated device cannot create a segment of {size} bytes.");
            }

            // Device-only memory has no CPU mapping and owns no host block at all.
            if (kind == UnifiedMemoryKind.Device)
            {
                var deviceOnly = AllocateAligned(size, alignment);
                m_Segments.Add((0, (nuint)deviceOnly, size, kind));
                deviceHandle = deviceOnly;
                deviceAddress = (ulong)deviceOnly;
                return null;
            }

            var host = AllocateAligned(size, alignment);

            // Coherent memory is a single block: the device sees exactly the pages the CPU writes. Everything else gets
            // its own block, which only a copy or cache maintenance can bring in sync.
            var device = kind == UnifiedMemoryKind.SharedCoherent ? host : AllocateAligned(size, alignment);

            m_Segments.Add(((nuint)host, (nuint)device, size, kind));
            deviceHandle = device;
            deviceAddress = (ulong)device;
            return host;
        }

        /// <inheritdoc/>
        public void Free(void* segment, nuint size, void* deviceHandle)
        {
            for (var i = 0; i < m_Segments.Count; i++)
            {
                var entry = m_Segments[i];

                // A device-only segment is identified by its handle because it has no CPU address.
                var matches = segment != null ? entry.Host == (nuint)segment : entry.Device == (nuint)deviceHandle;
                if (!matches)
                {
                    continue;
                }

                m_Segments.RemoveAt(i);

                if (entry.Host != 0)
                {
                    FreeAligned(entry.Host);
                }

                if (entry.Device != entry.Host && entry.Device != 0)
                {
                    FreeAligned(entry.Device);
                }

                return;
            }
        }

        /// <inheritdoc/>
        public void Flush(void* ptr, nuint size)
        {
            var (host, device, kind) = Find(ptr);

            if (kind == UnifiedMemoryKind.SharedCoherent)
            {
                return;
            }

            var offset = (byte*)ptr - (byte*)host;
            Buffer.MemoryCopy(ptr, (byte*)device + offset, (long)size, (long)size);
        }

        /// <inheritdoc/>
        public void Invalidate(void* ptr, nuint size)
        {
            var (host, device, kind) = Find(ptr);

            if (kind == UnifiedMemoryKind.SharedCoherent)
            {
                return;
            }

            var offset = (byte*)ptr - (byte*)host;
            Buffer.MemoryCopy((byte*)device + offset, ptr, (long)size, (long)size);
        }

        /// <inheritdoc/>
        public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
        {
            if (destination.DeviceAddress == 0 || source.DeviceAddress == 0)
            {
                return false;
            }

            // The emulated blit engine reads and writes device memory, which is exactly what makes it able to fill a
            // block the CPU cannot address.
            var destinationPointer = (byte*)destination.DeviceAddress + destinationOffset;
            var sourcePointer = (byte*)source.DeviceAddress + sourceOffset;

            if (defer)
            {
                m_Recorded.Add(((nuint)destinationPointer, (nuint)sourcePointer, size));
                return true;
            }

            Buffer.MemoryCopy(sourcePointer, destinationPointer, (long)size, (long)size);
            return true;
        }

        /// <inheritdoc/>
        public bool Submit()
        {
            if (m_Recorded.Count == 0)
            {
                return false;
            }

            m_Submitted.AddRange(m_Recorded);
            m_Recorded.Clear();
            return true;
        }

        /// <inheritdoc/>
        public bool Wait()
        {
            foreach (var (destination, source, size) in m_Submitted)
            {
                Buffer.MemoryCopy((byte*)source, (byte*)destination, (long)size, (long)size);
            }

            var waited = m_Submitted.Count > 0;
            m_Submitted.Clear();
            return waited;
        }

        /// <summary>
        /// Gets the number of copies that were recorded but not submitted yet, which is what makes a deferred copy
        /// observably deferred.
        /// </summary>
        public int PendingCopies => m_Recorded.Count;

        /// <inheritdoc/>
        public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency)
        {
            LastResidency = residency;
            return true;
        }

        /// <summary>
        /// Releases every segment and the handle that roots this instance.
        /// </summary>
        public void Dispose()
        {
            foreach (var entry in m_Segments)
            {
                if (entry.Host != 0)
                {
                    FreeAligned(entry.Host);
                }

                if (entry.Device != entry.Host && entry.Device != 0)
                {
                    FreeAligned(entry.Device);
                }
            }

            m_Segments.Clear();

            if (m_Self.IsAllocated)
            {
                m_Self.Free();
            }
        }

        /// <summary>
        /// Finds the segment that contains <paramref name="ptr"/>.
        /// </summary>
        /// <param name="ptr">A CPU address inside a live segment.</param>
        /// <returns>The CPU address, the device address and the kind of that segment.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the address is not inside a live segment.</exception>
        private (nuint Host, nuint Device, UnifiedMemoryKind Kind) Find(void* ptr)
        {
            foreach (var entry in m_Segments)
            {
                if (entry.Host == 0)
                {
                    continue;
                }

                var start = entry.Host;
                var address = (nuint)ptr;

                if (address >= start && address - start < entry.Size)
                {
                    return (entry.Host, entry.Device, entry.Kind);
                }
            }

            throw new InvalidOperationException($"The address {(ulong)ptr} is not inside a live segment.");
        }

        /// <summary>
        /// Allocates a page-aligned block with the OS allocator, recording the distance back to the allocated block in
        /// the bytes just in front of the payload.
        /// </summary>
        /// <param name="size">The size of the payload in bytes.</param>
        /// <param name="alignment">The required payload alignment in bytes.</param>
        /// <returns>The aligned payload address.</returns>
        /// <exception cref="OutOfMemoryException">Thrown when the block cannot be allocated.</exception>
        private static byte* AllocateAligned(nuint size, nuint alignment)
        {
            var align = alignment < (nuint)Environment.SystemPageSize ? (nuint)Environment.SystemPageSize : alignment;
            var total = size + align + (nuint)sizeof(nuint);
            var raw = (byte*)UnmanagedAllocator.Global.Alloc(total, 8);

            if (raw == null)
            {
                throw new OutOfMemoryException($"Cannot allocate {total} bytes.");
            }

            var payload = (byte*)Native.AlignForward((nuint)raw + (nuint)sizeof(nuint), align);
            ((nuint*)payload)[-1] = (nuint)payload - (nuint)raw;
            return payload;
        }

        /// <summary>
        /// Releases a block returned by <see cref="AllocateAligned"/> by recovering the address the OS allocator returned
        /// from the header just in front of the payload.
        /// </summary>
        /// <param name="payload">The payload address of a block returned by <see cref="AllocateAligned"/>.</param>
        private static void FreeAligned(nuint payload)
        {
            var ptr = (byte*)payload;
            var raw = ptr - ((nuint*)ptr)[-1];
            UnmanagedAllocator.Global.Free(raw, 0, 8);
        }

        /// <summary>
        /// Recovers the bridge instance from the provider's context pointer.
        /// </summary>
        /// <param name="ctx">The provider context, which holds the bridge's <see cref="GCHandle"/>.</param>
        /// <returns>The bridge instance.</returns>
        private static ShadowDevice Resolve(void* ctx) => (ShadowDevice)GCHandle.FromIntPtr((IntPtr)ctx).Target!;

        private static void* AllocThunk(void* ctx, nuint size, nuint alignment, UnifiedMemoryUsage usage, void** deviceHandle, ulong* deviceAddress, UnifiedMemoryKind* kind)
        {
            var result = Resolve(ctx).Alloc(size, alignment, usage, out var handle, out var address, out var segmentKind);
            *deviceHandle = handle;
            *deviceAddress = address;
            *kind = segmentKind;
            return result;
        }

        private static void FreeThunk(void* ctx, void* ptr, void* deviceHandle, nuint size) => Resolve(ctx).Free(ptr, size, deviceHandle);

        private static void FlushThunk(void* ctx, void* ptr, nuint size) => Resolve(ctx).Flush(ptr, size);

        private static void InvalidateThunk(void* ctx, void* ptr, nuint size) => Resolve(ctx).Invalidate(ptr, size);

        private static bool CopyThunk(void* ctx, UnifiedMemoryBlock* destination, nuint destinationOffset, UnifiedMemoryBlock* source, nuint sourceOffset, nuint size, bool defer)
            => Resolve(ctx).Copy(*destination, destinationOffset, *source, sourceOffset, size, defer);

        private static bool SubmitThunk(void* ctx) => Resolve(ctx).Submit();

        private static bool WaitThunk(void* ctx) => Resolve(ctx).Wait();

        private static nuint AlignmentThunk(void* ctx) => Resolve(ctx).Alignment;

        private static nuint MaxSegmentSizeThunk(void* ctx) => Resolve(ctx).MaxSegmentSize;

        private static nuint WorkingSetLimitThunk(void* ctx) => Resolve(ctx).WorkingSetLimit;

        private static bool SetResidencyThunk(void* ctx, void* segmentHost, void* deviceHandle, UnifiedMemoryResidency residency)
            => Resolve(ctx).SetResidency(default, residency);
    }

    /// <summary>
    /// Asserts that <paramref name="actual"/> equals <paramref name="expected"/>.
    /// </summary>
    /// <remarks>
    /// The native sized integer is compared as a <see cref="ulong"/> because NUnit's numeric constraints do not
    /// understand <see cref="UIntPtr"/>, which is how a <c>nuint</c> is boxed.
    /// </remarks>
    private static void AssertBytes(nuint actual, ulong expected, string? message = null) => Assert.That((ulong)actual, Is.EqualTo(expected), message);

    /// <summary>
    /// Builds a deterministic byte pattern that is different at every offset.
    /// </summary>
    /// <param name="length">The number of bytes.</param>
    /// <returns>The pattern.</returns>
    private static byte[] Pattern(int length)
    {
        var pattern = new byte[length];

        for (var index = 0; index < length; index++)
        {
            pattern[index] = (byte)((index * 31) ^ 0x5A);
        }

        return pattern;
    }

    private static void AssertAligned(void* pointer, nuint alignment)
    {
        Assert.That((ulong)((nuint)pointer & (alignment - 1)), Is.Zero, $"The pointer {(ulong)pointer} is not aligned to {alignment} bytes.");
    }

    [Test]
    public void HostProvider_Alloc_ReturnsAlignedHostBlocks()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(100, 1);

        Assert.That((ulong)pointer, Is.Not.Zero);
        AssertAligned(pointer, UnifiedAllocator.MINIMUM_ALIGNMENT);
        Assert.That(impl.TryGetKind(pointer, out var kind), Is.True);
        Assert.That(kind, Is.EqualTo(UnifiedMemoryKind.Host));

        for (var i = 0; i < 100; i++)
        {
            pointer[i] = (byte)i;
        }

        Assert.That(pointer[99], Is.EqualTo(99));
    }

    [Test]
    public void HostProvider_TryGetSegment_ReportsTheHostSegment()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(100, 1);

        Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);
        Assert.That((ulong)segment.Block, Is.EqualTo((ulong)pointer));
        AssertBytes(segment.Offset, 0);
        AssertBytes(segment.Size, s_SegmentSize);
        Assert.That(segment.Kind, Is.EqualTo(UnifiedMemoryKind.Host));
        Assert.That(segment.IsMapped, Is.True);
        Assert.That(segment.IsShared, Is.False);
        Assert.That(segment.IsCoherent, Is.False);
        Assert.That(segment.DeviceAddress, Is.Zero);
        Assert.That(segment.DeviceAddressOfBlock, Is.Zero);
        Assert.That((ulong)segment.Host, Is.Not.Zero);
    }

    [Test]
    public void TryGetSegment_ForeignPointer_ReturnsFalse()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();
        allocator.Alloc(100, 1);

        var foreign = stackalloc byte[16];

        Assert.That(impl.TryGetSegment(foreign, out _), Is.False);
        Assert.That(impl.TryGetSegment(null, out _), Is.False);
        Assert.That(impl.TryGetKind(foreign, out _), Is.False);
        Assert.That(impl.TryGetBlock(foreign, 16, out _), Is.False);
    }

    [Test]
    public void DefaultAllocator_IsNotUsable()
    {
        var empty = default(UnifiedAllocator);
        var foreign = stackalloc byte[16];

        Assert.That(empty.IsValid, Is.False);
        Assert.That(empty.TryGetSegment(foreign, out _), Is.False);
        Assert.That(empty.TryGetKind(foreign, out var kind), Is.False);
        Assert.That(kind, Is.EqualTo(UnifiedMemoryKind.Host));
    }

    [Test]
    public void Alloc_FillsTheSegmentBeforeAcquiringAnother()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        // A quarter of a segment each, so four of them fill it exactly and the fifth has to ask for another one.
        var block = s_SegmentSize / 4;
        var expected = 0UL;

        for (var i = 0; i < 4; i++)
        {
            var pointer = (byte*)allocator.Alloc(block, 1);
            Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);
            AssertBytes(segment.Offset, expected, $"Block {i} must follow the previous one.");
            Assert.That(impl.SegmentCount, Is.EqualTo(1));
            expected = (ulong)(segment.Offset + block);
        }

        allocator.Alloc(block, 1);

        Assert.That(impl.SegmentCount, Is.EqualTo(2));
        AssertBytes(impl.Capacity, (ulong)s_SegmentSize * 2);

        // Four blocks fill the first segment, and the fifth starts the second one.
        AssertBytes(impl.Used, s_SegmentSize + block);
    }

    [Test]
    public void Alloc_LargerThanTheSegment_GetsASegmentOfItsOwn()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(s_SegmentSize * 2, 1);

        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);
        AssertBytes(segment.Offset, 0);
        Assert.That((ulong)segment.Size, Is.GreaterThanOrEqualTo((ulong)(s_SegmentSize * 2)));
        Assert.That((ulong)(segment.Size % impl.ProviderAlignment), Is.Zero);
    }

    [Test]
    public void Alloc_ZeroLength_ReturnsAPointerWithoutConsumingTheArena()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(0, 1);

        Assert.That((ulong)pointer, Is.Not.Zero);
        AssertAligned(pointer, UnifiedAllocator.MINIMUM_ALIGNMENT);
        AssertBytes(impl.Used, 0);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
    }

    [Test]
    public void Free_ParksAnOlderBlockAndTheNextAllocationReusesIt()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = (ulong)allocator.Alloc(100, 1);
        var second = (ulong)allocator.Alloc(100, 1);

        // The first block is not the most recent one, so the cursor cannot move back over it. Its range is parked on the
        // free list instead, which is enough for the next allocation of that size to land on the very same address.
        allocator.Free((void*)first, 100, 1);
        AssertBytes(impl.Used, 100);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), UnifiedAllocator.MINIMUM_ALIGNMENT);

        var again = (ulong)allocator.Alloc(100, 1);
        Assert.That(again, Is.EqualTo(first), "The parked range is reused before the cursor moves.");
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        AssertBytes(impl.Used, (ulong)UnifiedAllocator.MINIMUM_ALIGNMENT + 100);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);

        // Releasing the blocks in reverse order reaches the cursor, which then pulls the whole arena back.
        allocator.Free((void*)second, 100, 1);
        allocator.Free((void*)again, 100, 1);
        AssertBytes(impl.Used, 0);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);
    }

    [Test]
    public void Free_JoinsNeighbouringRangesSoALargerBlockFits()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        // Two adjacent 256 byte blocks, with a third one keeping the cursor where it is.
        var first = (ulong)allocator.Alloc(256, 1);
        var second = (ulong)allocator.Alloc(256, 1);
        allocator.Alloc(256, 1);

        allocator.Free((void*)first, 256, 1);
        allocator.Free((void*)second, 256, 1);

        // Neither range could serve 512 bytes on its own; joined, they can, and the block starts where the first one did.
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 2 * UnifiedAllocator.MINIMUM_ALIGNMENT);

        var joined = (ulong)allocator.Alloc(512, 1);
        Assert.That(joined, Is.EqualTo(first));
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);
    }

    [Test]
    public void FreeList_PicksTheTightestRangeThatFits()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        // Three parked ranges of different sizes, kept apart by live blocks so that releasing them cannot join them into
        // one big range: a small one, then a large one, then one that is exactly the size the next request asks for.
        var small = (ulong)allocator.Alloc(256, 1);
        allocator.Alloc(256, 1);
        var large = (ulong)allocator.Alloc(2048, 1);
        allocator.Alloc(256, 1);
        var medium = (ulong)allocator.Alloc(1024, 1);
        allocator.Alloc(256, 1);

        allocator.Free((void*)small, 256, 1);
        allocator.Free((void*)large, 2048, 1);
        allocator.Free((void*)medium, 1024, 1);

        // A first-fit list would spend the 2048 byte range on a 1024 byte request and leave nothing for the block that
        // needs the big range; the tightest fit takes the 1024 byte one instead.
        var reused = (ulong)allocator.Alloc(1024, 1);
        Assert.That(reused, Is.EqualTo(medium));

        // The big range is still there for the big request, which is what best fit is for.
        var big = (ulong)allocator.Alloc(2048, 1);
        Assert.That(big, Is.EqualTo(large));

        Assert.That(impl.SegmentCount, Is.EqualTo(1));
    }

    [Test]
    public void Resize_GrowsABlockIntoTheRangeBehindIt()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = (byte*)allocator.Alloc(256, 1);
        var second = allocator.Alloc(256, 1);
        allocator.Alloc(256, 1);

        new Span<byte>(first, 256).Fill(0x27);

        // The block that follows is released, so the first block can grow over it without moving.
        allocator.Free(second, 256, 1);

        Assert.That(allocator.Resize(first, 256, 512, 1), Is.True, "the released range behind the block serves the growth");
        Assert.That(first[0], Is.EqualTo(0x27), "growing in place must not touch the block's own bytes");
        Assert.That(first[255], Is.EqualTo(0x27));
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);

        // Growing past the released range is refused, because the block after it is live.
        Assert.That(allocator.Resize(first, 512, 768, 1), Is.False);

        // A block whose neighbour is live cannot grow at all.
        var third = (byte*)allocator.Alloc(256, 1);
        Assert.That(allocator.Resize(first, 512, 768, 1), Is.False);
        Assert.That((ulong)third, Is.Not.EqualTo((ulong)first));
    }

    [Test]
    public void TryGetLargestFree_ReportsTheRoomTheArenaAlreadyHas()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        Assert.That(impl.TryGetLargestFree(UnifiedMemoryUsage.General, out var emptyLargest), Is.False);
        AssertBytes(emptyLargest, 0, "an arena with no segment has nothing to offer");

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(1024, UnifiedMemoryUsage.General);

        // Room after the cursor only.
        Assert.That(impl.TryGetLargestFree(UnifiedMemoryUsage.General, out var cursorRoom), Is.True);
        AssertBytes(cursorRoom, unchecked((ulong)(s_SegmentSize - (1024 + UnifiedAllocator.MINIMUM_ALIGNMENT))));

        // The parked range and the cursor room compete for the answer.
        Assert.That(impl.TryFree(first), Is.True);
        Assert.That(impl.TryGetLargestFree(UnifiedMemoryUsage.General, out var withParked), Is.True);
        AssertBytes(withParked, Math.Max(256UL, (ulong)(s_SegmentSize - (1024 + UnifiedAllocator.MINIMUM_ALIGNMENT))));

        // A usage with no segment has nothing, and this arena never asked for device memory.
        Assert.That(impl.TryGetLargestFree(UnifiedMemoryUsage.Device, out _), Is.False);

        Assert.That(second.Offset, Is.EqualTo(UnifiedAllocator.MINIMUM_ALIGNMENT));
    }

    [Test]
    public void Free_ChurnKeepsTheArenaFromGrowing()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        // One round warms the arena up: it acquires its first segment, and everything after that has to be served from
        // the ranges already released, or a per-frame allocator would grow without bound.
        void Round()
        {
            var blocks = stackalloc void*[4];

            for (var index = 0; index < 4; index++)
            {
                blocks[index] = allocator.Alloc(512, 1);
                AssertAligned((byte*)blocks[index], UnifiedAllocator.MINIMUM_ALIGNMENT);
            }

            for (var index = 0; index < 4; index++)
            {
                allocator.Free(blocks[index], 512, 1);
            }
        }

        Round();

        var capacity = impl.Capacity;
        var segments = impl.SegmentCount;

        for (var round = 0; round < 400; round++)
        {
            Round();
        }

        Assert.That(impl.SegmentCount, Is.EqualTo(segments), "Churn must not acquire another segment.");
        AssertBytes(impl.Capacity, capacity);
        AssertBytes(impl.Used, 0);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);
    }

    [Test]
    public void Free_KeepsTheLiveBlocksOfTheSegmentIntact()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = (byte*)allocator.Alloc(256, 1);
        var middle = (byte*)allocator.Alloc(256, 1);
        var last = (byte*)allocator.Alloc(256, 1);

        first[0] = 0x11;
        last[0] = 0x33;

        allocator.Free(middle, 256, 1);

        // The reused range is the one that was released, so the neighbours keep both their address and their bytes.
        var reused = (byte*)allocator.Alloc(256, 1);
        Assert.That((ulong)reused, Is.EqualTo((ulong)middle));
        Assert.That(first[0], Is.EqualTo(0x11));
        Assert.That(last[0], Is.EqualTo(0x33));
    }

    [Test]
    public void Resize_GrowsTheMostRecentBlockInPlace()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = allocator.Alloc(100, 1);
        var second = allocator.Alloc(100, 1);

        Assert.That(allocator.Resize(first, 100, 200, 1), Is.False, "An older block must not move.");
        Assert.That(allocator.Resize(second, 100, 200, 1), Is.True);
        AssertBytes(impl.Used, (ulong)UnifiedAllocator.MINIMUM_ALIGNMENT + 200);

        Assert.That(allocator.Resize(second, 200, 8, 1), Is.True);
        AssertBytes(impl.Used, (ulong)UnifiedAllocator.MINIMUM_ALIGNMENT + 8);
    }

    [Test]
    public void Resize_BeyondTheSegment_Fails()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = allocator.Alloc(100, 1);

        Assert.That(allocator.Resize(pointer, 100, s_SegmentSize * 2, 1), Is.False);
        AssertBytes(impl.Used, 100);
    }

    [Test]
    public void Remap_ShrinksInPlace_AndGrowsOnlyTheMostRecentBlock()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = allocator.Alloc(100, 1);
        var second = allocator.Alloc(100, 1);

        Assert.That((ulong)allocator.Remap(second, 100, 64, 1), Is.EqualTo((ulong)second));
        Assert.That((ulong)allocator.Remap(first, 100, 200, 1), Is.Zero, "A block the device may be bound to cannot move.");

        // Shrinking keeps the block's recorded length, so a later grow is expressed with the original length.
        Assert.That((ulong)allocator.Remap(second, 100, 512, 1), Is.EqualTo((ulong)second));
    }

    [Test]
    public void Reset_RewindsTheSegmentsWithoutReleasingThem()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = (ulong)allocator.Alloc(1000, 1);
        allocator.Alloc(s_SegmentSize, 1);

        var segments = impl.SegmentCount;
        var capacity = impl.Capacity;

        Assert.That(segments, Is.EqualTo(2));

        allocator.Reset();

        AssertBytes(impl.Used, 0);
        Assert.That(impl.SegmentCount, Is.EqualTo(segments), "A reset keeps the segments, so device bindings stay valid.");
        Assert.That(impl.Capacity, Is.EqualTo(capacity));

        // The rewound arena refills the segment it already owns, so the block keeps the address the device was bound to.
        Assert.That((ulong)allocator.Alloc(1000, 1), Is.EqualTo(first));
    }

    [Test]
    public void Trim_ReleasesTheSegmentsThatHoldNothingLive()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        // Half a segment plus the alignment step does not fit twice, so the second block forces a second segment.
        var block = s_SegmentSize / 2 + UnifiedAllocator.MINIMUM_ALIGNMENT;
        var first = impl.AllocBlock(block, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(block, UnifiedMemoryUsage.General);

        Assert.That(impl.SegmentCount, Is.EqualTo(2));
        Assert.That((nint)second.SegmentHost, Is.Not.EqualTo((nint)first.SegmentHost), "the blocks are in different segments");

        new Span<byte>(second.Host, (int)second.Length).Fill(0x5A);

        // Nothing is free yet, so the sweep must not take anything away.
        Assert.That(impl.Trim(), Is.Zero);
        Assert.That(impl.SegmentCount, Is.EqualTo(2));

        Assert.That(impl.TryFree(second), Is.True);
        Assert.That(impl.TryFree(first), Is.True);

        // Both segments are empty now, so both go back to the backend.
        Assert.That(impl.Trim(), Is.EqualTo(2));
        Assert.That(impl.SegmentCount, Is.Zero);
        AssertBytes(impl.Capacity, 0);

        // The allocator stays usable and hands out a fresh segment.
        var again = impl.AllocBlock(1024, UnifiedMemoryUsage.General);
        Assert.That((nint)again.Host, Is.Not.Zero);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
    }

    [Test]
    public void Trim_KeepsASegmentThatStillHoldsABlock()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = impl.AllocBlock(1000, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(1000, UnifiedMemoryUsage.General);

        new Span<byte>(first.Host, (int)first.Length).Fill(0x11);
        new Span<byte>(second.Host, (int)second.Length).Fill(0x22);

        Assert.That(impl.TryFree(first), Is.True);

        // The parked range alone does not make the segment empty, so it is kept and the live block keeps its bytes.
        Assert.That(impl.Trim(), Is.Zero);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        Assert.That(((byte*)second.Host)[0], Is.EqualTo(0x22));
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 1024);
    }

    [Test]
    public void Trim_OfOneUsageLeavesTheOthersAlone()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var general = impl.AllocBlock(512, UnifiedMemoryUsage.General);
        var deviceBlock = impl.AllocBlock(512, UnifiedMemoryUsage.Device);

        Assert.That(impl.SegmentCount, Is.EqualTo(2));

        Assert.That(impl.TryFree(general), Is.True);
        Assert.That(impl.TryFree(deviceBlock), Is.True);

        // Only the empty shared segment goes back when that usage is named; the device one is left alone.
        Assert.That(impl.Trim(UnifiedMemoryUsage.General), Is.EqualTo(1));
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.Device), (ulong)s_SegmentSize);

        // The device segment is still there, and the sweep for every usage takes it.
        Assert.That(impl.Trim(), Is.EqualTo(1));
        Assert.That(impl.SegmentCount, Is.Zero);
    }

    [Test]
    public void Trim_OfAnEmptyAllocatorDoesNothing()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        Assert.That(impl.Trim(), Is.Zero);
        Assert.That(impl.SegmentCount, Is.Zero);
    }

    [Test]
    public void Dispose_ReleasesTheSegments()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        var allocator = impl.GetAllocator();
        allocator.Alloc(1000, 1);
        allocator.Alloc(s_SegmentSize, 1);

        Assert.That(impl.SegmentCount, Is.EqualTo(2));

        allocator.Dispose();

        Assert.That(impl.SegmentCount, Is.Zero);
        AssertBytes(impl.Capacity, 0);
        AssertBytes(impl.Used, 0);
    }

    [Test]
    public void CoherentHeap_HostAndDeviceShareTheSamePages()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var pointer = (byte*)allocator.Alloc(64, 1);

        Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);
        Assert.That(segment.Kind, Is.EqualTo(UnifiedMemoryKind.SharedCoherent));
        Assert.That(segment.IsShared, Is.True);
        Assert.That(segment.IsCoherent, Is.True);
        Assert.That(device.LiveSegments, Is.EqualTo(1), "Coherent memory is one block, not two copies.");

        var deviceBlock = (byte*)segment.DeviceAddressOfBlock;
        Assert.That((ulong)deviceBlock, Is.EqualTo((ulong)pointer), "Coherent memory is one block, not two copies.");
        Assert.That(segment.DeviceAddressOfBlock - segment.DeviceAddress, Is.EqualTo((ulong)segment.Offset));

        // No flush is needed: the device reads the pages the CPU just wrote.
        pointer[0] = 0xAB;
        pointer[1] = 0xCD;

        Assert.That(deviceBlock[0], Is.EqualTo(0xAB));
        Assert.That(deviceBlock[1], Is.EqualTo(0xCD));
    }

    [Test]
    public void Usage_SelectsTheHeapAndTheCacheMaintenanceItNeeds()
    {
        // The non-coherent shape: the pages are shared, but the CPU has to publish its writes and invalidate the device's.
        using var device = ShadowDevice.CreateNonCoherentSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var shared = (byte*)impl.Alloc(64, UnifiedMemoryUsage.General);
        var deviceOnly = impl.AllocBlock(64, UnifiedMemoryUsage.Device);

        Assert.That(impl.TryGetSegment(shared, out var sharedSegment), Is.True);
        Assert.That(impl.SegmentCount, Is.EqualTo(2));

        // one allocator, two heaps: the shared pages the CPU addresses, and the device-local heap it cannot.
        Assert.That(sharedSegment.Kind, Is.EqualTo(UnifiedMemoryKind.Shared));
        Assert.That(deviceOnly.Kind, Is.EqualTo(UnifiedMemoryKind.Device));
        Assert.That(deviceOnly.IsMapped, Is.False, "the device heap has no CPU address at all");

        // A shared block needs a flush before the device sees it.
        var sharedDevice = (byte*)sharedSegment.DeviceAddressOfBlock;
        new Span<byte>(sharedDevice, 64).Clear();

        shared[0] = 0x22;
        Assert.That(sharedDevice[0], Is.Zero, "A shared heap that is not coherent needs a flush.");

        impl.Flush(shared, 64);
        Assert.That(sharedDevice[0], Is.EqualTo(0x22));
    }

    [Test]
    public void FlushPublishesCpuWritesToTheDevice()
    {
        using var device = ShadowDevice.CreateNonCoherentSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var pointer = (byte*)impl.Alloc(64, UnifiedMemoryUsage.General);

        Assert.That(impl.TryGetKind(pointer, out var kind), Is.True);
        Assert.That(kind, Is.EqualTo(UnifiedMemoryKind.Shared));

        Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);
        var deviceBlock = (byte*)segment.DeviceAddressOfBlock;
        new Span<byte>(deviceBlock, 64).Clear();

        pointer[0] = 0x11;
        Assert.That(deviceBlock[0], Is.Zero, "Without a flush the device still sees stale data.");

        impl.Flush(pointer, 64);

        Assert.That(deviceBlock[0], Is.EqualTo(0x11));
    }

    [Test]
    public void InvalidatePublishesDeviceWritesToTheCpu()
    {
        using var device = ShadowDevice.CreateNonCoherentSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var pointer = (byte*)impl.Alloc(64, UnifiedMemoryUsage.General);
        Assert.That(impl.TryGetSegment(pointer, out var segment), Is.True);

        var deviceBlock = (byte*)segment.DeviceAddressOfBlock;
        pointer[0] = 0;

        deviceBlock[0] = 0x77;
        Assert.That(pointer[0], Is.Zero, "Without an invalidate the CPU still sees stale data.");

        impl.Invalidate(pointer, 1);

        Assert.That(pointer[0], Is.EqualTo(0x77));
    }

    [Test]
    public void FlushAndInvalidate_AcceptTypedSpans()
    {
        using var device = ShadowDevice.CreateNonCoherentSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var values = impl.Alloc<int>((nuint)16, UnifiedMemoryUsage.General);
        Assert.That(impl.TryGetSegment((byte*)Native.AsPointer(values), out var segment), Is.True);

        var deviceBlock = (byte*)segment.DeviceAddressOfBlock;
        new Span<int>(deviceBlock, 16).Clear();

        values[3] = 42;
        Assert.That(((int*)deviceBlock)[3], Is.Zero);

        impl.Flush(values);
        Assert.That(((int*)deviceBlock)[3], Is.EqualTo(42));

        ((int*)deviceBlock)[3] = 7;
        impl.Invalidate(values);
        Assert.That(values[3], Is.EqualTo(7));
    }

    [Test]
    public void DeviceUsage_ReturnsAnUnmappedBlock()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var block = impl.AllocBlock(1024, UnifiedMemoryUsage.Device);

        Assert.That(block.IsMapped, Is.False, "Device-only memory has no CPU address.");
        Assert.That(block.IsDeviceLocal, Is.True);
        Assert.That(block.IsShared, Is.False);
        AssertBytes(block.Length, 1024);
        Assert.That(block.DeviceAddress, Is.Not.Zero, "The block still has a device address.");
        Assert.That((ulong)block.DeviceHandle, Is.Not.Zero, "A device-only block is addressed by its segment handle.");
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.Device), (ulong)s_SegmentSize);
    }

    [Test]
    public void Alloc_ForDeviceUsage_ThrowsBecauseThereIsNoPointer()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        Assert.Throws<InvalidOperationException>(() => impl.Alloc(1024, UnifiedMemoryUsage.Device));
    }

    [Test]
    public void Copy_FillsADeviceBlockFromAStagingBlock()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        // Stage the data in an upload heap, then let the device copy it into memory the CPU cannot address.
        var staging = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        Assert.That(staging.IsMapped, Is.True);

        for (var i = 0; i < 256; i++)
        {
            staging.Host[i] = (byte)(i ^ 0x5A);
        }

        impl.Flush(staging.Host, staging.Length);

        var destination = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        Assert.That(impl.TryCopy(destination, staging), Is.True);

        // Read it back through another heap: the copy back lands in the readback heap's device view, so the CPU needs an
        // invalidate before it sees the bytes.
        var readback = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        Assert.That(impl.TryCopy(readback, destination), Is.True);
        impl.Invalidate(readback.Host, readback.Length);

        for (var i = 0; i < 256; i++)
        {
            if (readback.Host[i] != (byte)(i ^ 0x5A))
            {
                Assert.Fail($"Byte {i} did not survive the round trip: {(int)readback.Host[i]}.");
            }
        }
    }

    [Test]
    public void Copy_RejectsRangesOutsideTheBlocks()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var source = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        var destination = impl.AllocBlock(64, UnifiedMemoryUsage.Device);

        Assert.That(impl.TryCopy(destination, 0, source, 0, 128), Is.False);
        Assert.That(impl.TryCopy(destination, 32, source, 32, 64), Is.False);
        Assert.That(impl.TryCopy(destination, 0, source, 0, 64), Is.True);
    }

    [Test]
    public void Copy_WorksBetweenMappedBlocksOnTheHostProvider()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        var source = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        var destination = impl.AllocBlock(64, UnifiedMemoryUsage.General);

        for (var i = 0; i < 64; i++)
        {
            source.Host[i] = (byte)i;
        }

        Assert.That(impl.TryCopy(destination, source), Is.True);
        Assert.That(destination.Host[63], Is.EqualTo(63));
    }

    [Test]
    public void DeferredCopy_CompletesOnlyAfterSubmitAndWait()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var staging = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        var destination = impl.AllocBlock(64, UnifiedMemoryUsage.Device);

        for (var i = 0; i < 64; i++)
        {
            staging.Host[i] = (byte)(i + 1);
        }

        impl.Flush(staging.Host, staging.Length);

        // Zero the destination first, so "the copy has not landed yet" is observable instead of depending on whatever the
        // OS allocator happened to leave in the device block.
        var zero = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        new Span<byte>(zero.Host, (int)zero.Length).Clear();
        impl.Flush(zero.Host, zero.Length);
        Assert.That(impl.TryCopy(destination, zero), Is.True);

        // Recording is not completion: nothing has touched the destination yet.
        Assert.That(impl.TryCopyDeferred(destination, 0, staging, 0, 64), Is.True);
        Assert.That(device.PendingCopies, Is.EqualTo(1));

        var readback = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        Assert.That(impl.TryCopy(readback, destination), Is.True);
        impl.Invalidate(readback.Host, readback.Length);
        Assert.That(readback.Host[0], Is.Zero, "The deferred copy must not have landed yet.");

        // Submitting starts the work; waiting is what makes it visible.
        Assert.That(impl.SubmitCopies(), Is.True);
        Assert.That(impl.WaitForCopies(), Is.True);

        Assert.That(impl.TryCopy(readback, destination), Is.True);
        impl.Invalidate(readback.Host, readback.Length);
        Assert.That(readback.Host[0], Is.EqualTo(1));
        Assert.That(readback.Host[63], Is.EqualTo(64));
    }

    [Test]
    public void TryFree_ReclaimsADeviceBlockAndReusesItsAddress()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        var second = impl.AllocBlock(256, UnifiedMemoryUsage.Device);

        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 512);

        // An older device block is reclaimable as well: nothing moves, its range is parked on the free list, which is the
        // only way to release device-only memory short of a reset.
        Assert.That(impl.TryFree(first), Is.True);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 256);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.Device), 256);

        // The parked range comes back at the same offset and the same device address, so a buffer binding rebuilt from it
        // is the one the counter above already knows.
        var third = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        Assert.That(third.Offset, Is.EqualTo(first.Offset));
        Assert.That(third.DeviceAddress, Is.EqualTo(first.DeviceAddress));
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 512);

        // Releasing what is left of the segment pulls the cursor back to where it started.
        Assert.That(impl.TryFree(second), Is.True);
        Assert.That(impl.TryFree(third), Is.True);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 0);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.Device), 0);
    }

    [Test]
    public void TryFreeWhenComplete_KeepsTheRangeReservedUntilTheFence()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFreeWhenComplete(first, 7), Is.True);
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(1));

        // The range is still reserved: it is not free, and it is not handed out again either.
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 512);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);

        var other = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        Assert.That(other.Offset, Is.EqualTo(second.Offset + second.Length), "a queued release must not be reused yet");

        // The device has not reached the value the block waits for, so nothing is handed back.
        Assert.That(impl.ReleaseCompleted(6), Is.Zero);
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(1));
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 768);

        // Reaching it releases the range, and the next allocation of that size lands on it.
        Assert.That(impl.ReleaseCompleted(7), Is.EqualTo(1));
        Assert.That(impl.PendingReleaseCount, Is.Zero);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 512);

        var reused = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        Assert.That(reused.Offset, Is.EqualTo(first.Offset));

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void ReleaseCompleted_ReleasesOnlyTheRangesTheFenceReached()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFreeWhenComplete(first, 3), Is.True);
        Assert.That(impl.TryFreeWhenComplete(second, 5), Is.True);
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(2));

        // One value at a time, so the queue is drained in the order the device passes the values.
        Assert.That(impl.ReleaseCompleted(3), Is.EqualTo(1));
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(1));
        Assert.That(impl.ReleaseCompleted(4), Is.Zero);
        Assert.That(impl.ReleaseCompleted(5), Is.EqualTo(1));
        Assert.That(impl.PendingReleaseCount, Is.Zero);

        // Both ranges are parked now, and the two blocks are adjacent, so the pair can serve a 512 byte block again.
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 512);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryFreeWhenComplete_RejectsAForeignBlock()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        var mine = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        var other = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        var foreign = other.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFreeWhenComplete(foreign, 1), Is.False);
        Assert.That(impl.PendingReleaseCount, Is.Zero);

        // The pointer overload takes the same decision.
        Assert.That(impl.TryFreeWhenComplete((void*)0x10, 256, 1), Is.False);
        Assert.That(impl.TryFreeWhenComplete(mine.Host, 8192, 1), Is.False, "a block that runs past the cursor is not a block.");

        impl.GetAllocator().Dispose();
        other.GetAllocator().Dispose();
    }

    [Test]
    public void Reset_DropsTheQueuedReleases()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        impl.AllocBlock(256, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFreeWhenComplete(second, 9), Is.True);
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(1));

        // A reset rewinds the whole arena, so waiting for a fence beyond it would keep memory reserved for nothing.
        impl.GetAllocator().Reset();

        Assert.That(impl.PendingReleaseCount, Is.Zero);
        AssertBytes(impl.Used, 0);
        Assert.That(impl.SegmentCount, Is.EqualTo(1), "the segments themselves are kept");

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryFreeWhenComplete_ReleasesADeviceRangeBehindItsFence()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        impl.AllocBlock(256, UnifiedMemoryUsage.Device);

        Assert.That(impl.TryFreeWhenComplete(first, 1), Is.True);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 512);

        Assert.That(impl.ReleaseCompleted(1), Is.EqualTo(1));

        var reused = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        Assert.That(reused.Offset, Is.EqualTo(first.Offset));
        Assert.That(reused.DeviceAddress, Is.EqualTo(first.DeviceAddress));

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryFree_RefusesADoubleRelease()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);

        var first = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        var second = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFree(first), Is.True);
        Assert.That(impl.TryFree(first), Is.False, "A block cannot be released twice.");
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 256);

        // The arena is still consistent: the parked range is served exactly once and the live block is untouched.
        var reused = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(reused.Offset, Is.EqualTo(first.Offset));
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);
        Assert.That(second.Offset, Is.Not.EqualTo(reused.Offset));

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryFree_RejectsABlockOfAnotherAllocator()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);
        impl.AllocBlock(64, UnifiedMemoryUsage.Device);

        var other = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        var foreign = other.AllocBlock(64, UnifiedMemoryUsage.General);

        Assert.That(impl.TryFree(foreign), Is.False);
    }

    [Test]
    public void TryUpload_FillsADeviceBlockThroughStaging()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var destination = impl.AllocBlock(512, UnifiedMemoryUsage.Device);
        var payload = Pattern(512);

        Assert.That(impl.TryUpload(destination, payload), Is.True);

        // The staging range went back to the arena as soon as the copy completed. It was the newest block of its segment,
        // so the cursor moved back over it instead of it being parked: the whole segment is free again.
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 0);
        AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);

        var readback = impl.AllocBlock(512, UnifiedMemoryUsage.General);
        Assert.That(impl.TryCopy(readback, destination), Is.True);
        impl.Invalidate(readback.Host, readback.Length);
        Assert.That(new ReadOnlySpan<byte>(readback.Host, 512).SequenceEqual(payload), Is.True);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryUpload_WritesOnlyTheBytesItWasGiven()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var destination = impl.AllocBlock(1024, UnifiedMemoryUsage.Device);
        var full = Pattern(1024);
        Assert.That(impl.TryUpload(destination, full), Is.True);

        // Overwrite the middle of the block, then check the bytes around it kept their first content.
        var patch = Pattern(128);
        Assert.That(impl.TryUpload(destination, 256, patch), Is.True);

        var readback = impl.AllocBlock(1024, UnifiedMemoryUsage.General);
        Assert.That(impl.TryCopy(readback, destination), Is.True);
        impl.Invalidate(readback.Host, readback.Length);

        var bytes = new ReadOnlySpan<byte>(readback.Host, 1024);

        Assert.That(bytes.Slice(256, 128).SequenceEqual(patch), Is.True, "the patch landed at its offset");
        Assert.That(bytes.Slice(0, 256).SequenceEqual(full.AsSpan(0, 256)), Is.True, "the bytes in front kept their content");
        Assert.That(bytes.Slice(384, 640).SequenceEqual(full.AsSpan(384, 640)), Is.True, "the bytes behind kept their content");

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryUploadAndReadback_RejectRangesOutsideTheBlock()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var block = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        var payload = Pattern(256);

        Assert.That(impl.TryUpload(block, 128, payload), Is.False, "the payload does not fit after the offset");
        Assert.That(impl.TryUpload(block, 512, payload), Is.False, "the offset is outside the block");

        var data = new byte[256];
        Assert.That(impl.TryReadback(data, block, 128), Is.False);
        Assert.That(impl.TryReadback(data, block, 512), Is.False);

        // Empty requests are valid and touch nothing.
        Assert.That(impl.TryUpload(block, ReadOnlySpan<byte>.Empty), Is.True);
        Assert.That(impl.TryReadback(Span<byte>.Empty, block), Is.True);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryReadback_ReadsBackWhatTryUploadWrote()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var destination = impl.AllocBlock(1024, UnifiedMemoryUsage.Device);
        var payload = Pattern(1024);
        Assert.That(impl.TryUpload(destination, payload), Is.True);

        var whole = new byte[1024];
        Assert.That(impl.TryReadback(whole, destination), Is.True);
        Assert.That(whole.AsSpan().SequenceEqual(payload), Is.True);

        var slice = new byte[256];
        Assert.That(impl.TryReadback(slice, destination, 512), Is.True);
        Assert.That(slice.AsSpan().SequenceEqual(payload.AsSpan(512, 256)), Is.True);

        // The readback range went back to the arena as well.
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 0);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void TryCopyDeferred_RejectsRangesOutsideTheBlocks()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var source = impl.AllocBlock(64, UnifiedMemoryUsage.General);
        var destination = impl.AllocBlock(64, UnifiedMemoryUsage.Device);

        Assert.That(impl.TryCopyDeferred(destination, 0, source, 0, 128), Is.False);
        Assert.That(impl.TryCopyDeferred(destination, 0, source, 32, 64), Is.False);
        Assert.That(device.PendingCopies, Is.Zero);
    }

    [Test]
    public void SetResidency_IsForwardedToTheProvider()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var block = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        Assert.That(impl.TrySetResidency(block, UnifiedMemoryResidency.Discardable), Is.True);
        Assert.That(device.LastResidency, Is.EqualTo(UnifiedMemoryResidency.Discardable));

        var host = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        var hostBlock = host.AllocBlock(64, UnifiedMemoryUsage.General);
        Assert.That(host.TrySetResidency(hostBlock, UnifiedMemoryResidency.Discardable), Is.False, "Host pages have no residency state.");
    }

    [Test]
    public void WorkingSetLimit_IsReportedByTheAllocator()
    {
        using var device = ShadowDevice.CreateSoC(workingSetLimit: 256 * 1024 * 1024);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        Assert.That((ulong)impl.WorkingSetLimit, Is.EqualTo(256UL * 1024 * 1024));
        Assert.That((ulong)new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize).WorkingSetLimit, Is.Zero);
    }

    [Test]
    public void UsageStatistics_SplitCapacityByUsage()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        impl.AllocBlock(1024, UnifiedMemoryUsage.General);
        impl.AllocBlock(512, UnifiedMemoryUsage.General);
        impl.AllocBlock(512, UnifiedMemoryUsage.Device);

        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 1536);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 512);
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.General), (ulong)s_SegmentSize);
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.Device), (ulong)s_SegmentSize);

        // The two heaps are separate segments even though they share one allocator.
        Assert.That(impl.SegmentCount, Is.EqualTo(2));
        AssertBytes(impl.Used, 2048);
    }

    [Test]
    public void Budget_FallsBackToTheBackendsWorkingSet()
    {
        // The host provider has no opinion on a working set, so nothing caps the arena unless the caller says so.
        Assert.That((ulong)new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize).Budget, Is.Zero);

        var capped = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize, 64 * 1024);
        Assert.That((ulong)capped.Budget, Is.EqualTo(64 * 1024));

        // A backend that reports a working set is the ceiling the arena enforces by default.
        using var device = ShadowDevice.CreateSoC(workingSetLimit: 2 * s_SegmentSize);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);
        Assert.That((ulong)impl.Budget, Is.EqualTo((ulong)(2 * s_SegmentSize)));
    }

    [Test]
    public void Budget_RefusesToGrowPastTheCeiling()
    {
        using var device = ShadowDevice.CreateSoC(workingSetLimit: s_SegmentSize);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        // Half a segment plus the alignment step, so two of them cannot share one segment on any machine.
        var block = s_SegmentSize / 2 + UnifiedAllocator.MINIMUM_ALIGNMENT;
        var first = impl.AllocBlock(block, UnifiedMemoryUsage.General);

        // A second block does not fit in the one segment the budget allows, so the arena refuses instead of acquiring one.
        Assert.Throws<OutOfMemoryException>(() => impl.AllocBlock(block, UnifiedMemoryUsage.General));
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
        AssertBytes(impl.Capacity, s_SegmentSize);

        // The refusal is about growing, not about using: the block that is there still works, and releasing it makes the
        // room available again.
        Assert.That((ulong)first.Length, Is.EqualTo((ulong)block));
        Assert.That(impl.TryFree(first), Is.True);

        var again = impl.AllocBlock(block, UnifiedMemoryUsage.General);
        Assert.That((ulong)again.Offset, Is.Zero);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
    }

    [Test]
    public void Budget_HandsBackTheEmptySegmentsBeforeRefusing()
    {
        // A block of one segment minus its alignment padding asks for exactly one segment.
        var blockSize = s_SegmentSize - UnifiedAllocator.MINIMUM_ALIGNMENT;

        using var device = ShadowDevice.CreateSoC(workingSetLimit: 2 * s_SegmentSize);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        // One general segment and one device segment: the arena is exactly at its ceiling.
        var general = impl.AllocBlock(blockSize, UnifiedMemoryUsage.General);
        var deviceBlock = impl.AllocBlock(blockSize, UnifiedMemoryUsage.Device);

        Assert.That(impl.SegmentCount, Is.EqualTo(2));
        AssertBytes(impl.Capacity, 2 * s_SegmentSize);

        // Releasing the device block leaves its segment empty but still acquired, so the capacity does not move yet.
        Assert.That(impl.TryFree(deviceBlock), Is.True);
        AssertBytes(impl.Capacity, 2 * s_SegmentSize);

        // The next general block needs a third segment. The empty device segment goes back on the way, which is what keeps
        // the allocation inside the ceiling.
        var second = impl.AllocBlock(blockSize, UnifiedMemoryUsage.General);

        Assert.That((ulong)second.Offset, Is.Zero, "the block landed in a fresh general segment");
        Assert.That(impl.SegmentCount, Is.EqualTo(2));
        AssertBytes(impl.Capacity, 2 * s_SegmentSize);
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.Device), 0, "the empty device segment went back to the backend");
        Assert.That(impl.TryFree(general), Is.True);
    }

    [Test]
    public void Budget_CountsEveryUsageTogether()
    {
        var blockSize = s_SegmentSize - UnifiedAllocator.MINIMUM_ALIGNMENT;

        // The ceiling is on the arena, not on one usage: the heaps are different resources but they come from the same
        // device, so an allocator that spends its budget on device memory has nothing left for a staging segment.
        using var device = ShadowDevice.CreateSoC(workingSetLimit: 2 * s_SegmentSize);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        impl.AllocBlock(blockSize, UnifiedMemoryUsage.General);
        impl.AllocBlock(blockSize, UnifiedMemoryUsage.Device);

        Assert.Throws<OutOfMemoryException>(() => impl.AllocBlock(blockSize, UnifiedMemoryUsage.General));
        Assert.Throws<OutOfMemoryException>(() => impl.AllocBlock(blockSize, UnifiedMemoryUsage.Device));
        Assert.That(impl.SegmentCount, Is.EqualTo(2));

        // A request that already has room in an existing segment is served, because it does not grow the arena.
        var small = impl.AllocBlock(256, UnifiedMemoryUsage.General);
        Assert.That((ulong)small.Offset, Is.EqualTo((ulong)blockSize));
        Assert.That(impl.SegmentCount, Is.EqualTo(2));
    }

    [Test]
    public void Acquire_EnforcesTheBackendSegmentLimit()
    {
        using var device = ShadowDevice.CreateSoC(maxSegmentSize: s_SegmentSize);
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);
        using var allocator = impl.GetAllocator();

        Assert.That(impl.MaxSegmentSize, Is.EqualTo(s_SegmentSize));

        // The request plus its alignment padding no longer fits in one segment, so it is rejected rather than clamped.
        Assert.Throws<OutOfMemoryException>(() => allocator.Alloc(s_SegmentSize, 1));

        // A request that fits still works, and the allocator stays usable after the rejection.
        var pointer = (byte*)impl.Alloc(1024, UnifiedMemoryUsage.General);
        Assert.That((ulong)pointer, Is.Not.Zero);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));
    }

    [Test]
    public void AllocatorHandle_KeepsAStableContext()
    {
        using var handle = AllocatorHandle.Create(new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize));

        var pointer = (byte*)handle.Allocator.Alloc(128, 1);

        Assert.That((ulong)pointer, Is.Not.Zero);
        AssertAligned(pointer, UnifiedAllocator.MINIMUM_ALIGNMENT);

        pointer[127] = 0x5A;
        Assert.That(pointer[127], Is.EqualTo(0x5A));
    }

    [Test]
    public void Allocator_CastReachesTheArenaBehindIt()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        using var allocator = impl.GetAllocator();

        var first = allocator.Alloc(512, 1);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 512);

        // The handle every collection holds can reach the arena it wraps, which is how a caller that needs the arena's own
        // bookkeeping gets at it without keeping a second reference to the struct.
        ref var arena = ref allocator.Cast<UnifiedAllocator>();

        AssertBytes(arena.GetUsed(UnifiedMemoryUsage.General), 512);
        Assert.That((ulong)arena.Budget, Is.Zero);

        // A release through the arena is visible to the handle, and the range is reused by the next allocation.
        var block = arena.TryGetBlock(first, 512, out var described);
        Assert.That(block, Is.True);
        Assert.That(arena.TryFree(described), Is.True);

        var again = allocator.Alloc(512, 1);
        Assert.That((ulong)again, Is.EqualTo((ulong)first));
    }

    [Test]
    public void GetAllocator_WithUsage_ServesThatHeap()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        using var shared = impl.GetAllocator(UnifiedMemoryUsage.General);

        var first = (byte*)shared.Alloc(256, 1);
        var second = (byte*)shared.Alloc(512, 1);

        Assert.That((ulong)first, Is.Not.Zero);
        Assert.That((ulong)second, Is.Not.Zero);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 768);
        Assert.That(impl.SegmentCount, Is.EqualTo(1));

        // The kind is the one the shared heap reports, not something the allocator made up.
        Assert.That(impl.TryGetBlock(first, 256, out var sharedBlock), Is.True);
        Assert.That(sharedBlock.Kind, Is.EqualTo(impl.AllocBlock(256, UnifiedMemoryUsage.General).Kind));
        Assert.That(sharedBlock.IsCoherent, Is.True);

        // A block handed out by it is released like any other, and the range is reused.
        var used = impl.GetUsed(UnifiedMemoryUsage.General);
        shared.Free(second, 512, 1);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), (ulong)(used - 512));

        var again = (byte*)shared.Alloc(512, 1);
        Assert.That((ulong)again, Is.EqualTo((ulong)second));
    }

    [Test]
    public void Reset_OfOneUsage_RewindsOnlyThatHeap()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        var deviceBlock = impl.AllocBlock(1024, UnifiedMemoryUsage.Device);
        impl.AllocBlock(1024, UnifiedMemoryUsage.General);

        var pending = impl.AllocBlock(512, UnifiedMemoryUsage.General);
        Assert.That(impl.TryFreeWhenComplete(pending, 3), Is.True);
        Assert.That(impl.PendingReleaseCount, Is.EqualTo(1));

        var rewound = impl.Reset(UnifiedMemoryUsage.General);

        Assert.That(rewound, Is.EqualTo(1), "only the one upload segment was rewound");
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 0, "the reused heap is empty again");
        Assert.That(impl.SegmentCount, Is.EqualTo(2), "the upload segment itself is kept");

        // The device block is exactly what a per-frame upload reset must not disturb.
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 1024);

        // A release that was waiting for the rewound heap goes with it, so it cannot park a range that no longer exists.
        Assert.That(impl.PendingReleaseCount, Is.Zero);

        // The upload heap still works and hands the space out again from the start of the segment.
        var again = impl.AllocBlock(1024, UnifiedMemoryUsage.General);
        Assert.That((ulong)again.Offset, Is.Zero);
        Assert.That(deviceBlock.Length, Is.EqualTo((nuint)1024));

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void Frames_ReuseTheUploadHeapWithoutGrowingTheArena()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        // The resources a frame reads: one device block that lives for the whole run, and one readback block.
        var persistent = impl.AllocBlock(256, UnifiedMemoryUsage.Device);
        var readback = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        void Frame(int seed)
        {
            // The frame's staging: written by the CPU, copied into the device block, then handed back at the end of the
            // frame instead of being released block by block.
            var staging = impl.AllocBlock(256, UnifiedMemoryUsage.General);
            new Span<byte>(staging.Host, 256).Fill((byte)seed);
            impl.Flush(staging.Host, staging.Length);

            Assert.That(impl.TryCopy(persistent, staging), Is.True, $"frame {seed}: the copy into the device block failed");

            // A short-lived device block as well, so the device heap churns too.
            var ephemeral = impl.AllocBlock(128, UnifiedMemoryUsage.Device);
            Assert.That(impl.TryCopy(ephemeral, staging), Is.True);
            Assert.That(impl.TryFree(ephemeral), Is.True);

            Assert.That(impl.TryCopy(readback, persistent), Is.True);
            impl.Invalidate(readback.Host, readback.Length);

            var bytes = new ReadOnlySpan<byte>(readback.Host, 256);
            Assert.That(bytes[0], Is.EqualTo((byte)seed), $"frame {seed}: the frame's bytes reached the device block");
            Assert.That(bytes[255], Is.EqualTo((byte)seed));

            // What a per-frame arena does at the end of a frame: hand the staging heap back, and nothing else.
            Assert.That(impl.Reset(UnifiedMemoryUsage.General), Is.EqualTo(1));
            AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 0);
        }

        Frame(1);

        var segments = impl.SegmentCount;
        var capacity = impl.Capacity;

        for (var frame = 2; frame <= 64; frame++)
        {
            Frame(frame);
        }

        Assert.That(impl.SegmentCount, Is.EqualTo(segments), "a frame must not acquire another segment");
        AssertBytes(impl.Capacity, (ulong)capacity, "a frame must not grow the arena");

        // The persistent block kept its identity through 64 frames of churn in the heaps around it.
        Assert.That((ulong)persistent.Offset, Is.Zero);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 0);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.Device), 256);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void Reset_OfAUnexpectedUsageDoesNothing()
    {
        var impl = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
        var block = impl.AllocBlock(256, UnifiedMemoryUsage.General);

        Assert.That(impl.Reset(UnifiedMemoryUsage.Device), Is.Zero);
        AssertBytes(impl.GetUsed(UnifiedMemoryUsage.General), 256);
        Assert.That((ulong)block.Offset, Is.Zero);

        impl.GetAllocator().Dispose();
    }

    [Test]
    public void GetAllocator_WithDeviceUsage_Throws()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize);

        // Device memory has no CPU address, so an Allocator - whose contract is a pointer - cannot serve it.
        Assert.Throws<ArgumentOutOfRangeException>(() => impl.GetAllocator(UnifiedMemoryUsage.Device));
    }

    [Test]
    public void AllocatorHandle_KeepsTheArenaAliveAfterTheOriginalStructIsGone()
    {
        // The arena lives in a scope that ends before the allocator is used, which is the shape every caller that stores the
        // allocator in a field has: the handle owns a copy in unmanaged storage, so the context stays valid.
        static AllocatorHandle CreateArena(out UnifiedAllocator arena)
        {
            arena = new UnifiedAllocator(UnifiedProvider.Host, s_SegmentSize);
            return AllocatorHandle.Create(arena);
        }

        using var handle = CreateArena(out _);
        using var allocator = handle.Allocator;

        var pointer = (byte*)allocator.Alloc(1024, 1);
        Assert.That((ulong)pointer, Is.Not.Zero);
        AssertAligned(pointer, UnifiedAllocator.MINIMUM_ALIGNMENT);

        pointer[0] = 0x5A;
        pointer[1023] = 0x27;
        Assert.That(pointer[0], Is.EqualTo(0x5A));
        Assert.That(pointer[1023], Is.EqualTo(0x27));

        // The arena behind the handle is the copy, and it is the one the allocator has been writing through.
        AssertBytes(handle.Allocator.Cast<UnifiedAllocator>().GetUsed(UnifiedMemoryUsage.General), 1024);

        // A reset through the handle rewinds the arena without releasing the segments.
        handle.Reset();
        AssertBytes(handle.Allocator.Cast<UnifiedAllocator>().GetUsed(UnifiedMemoryUsage.General), 0);
        Assert.That(handle.Allocator.Cast<UnifiedAllocator>().SegmentCount, Is.EqualTo(1));

        var reused = (byte*)allocator.Alloc(1024, 1);
        Assert.That((ulong)reused, Is.EqualTo((ulong)pointer));
    }

    [Test]
    public void Constructor_RejectsAProviderWithoutVTable()
    {
        Assert.Throws<ArgumentException>(() => new UnifiedAllocator(default, s_SegmentSize));
    }
}