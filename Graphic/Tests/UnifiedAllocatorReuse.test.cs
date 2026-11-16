namespace Gubbins.Graphic.Tests;

/// <summary>
/// Exercises the free list of <see cref="UnifiedAllocator"/>: the part of the arena that decides whether a steady churn of
/// allocations grows without bound or stays inside the segments the backend already handed out.
/// </summary>
/// <remarks>
/// The invariant every test here leans on is that reusing a released range may never disturb a live block. A free list
/// that gets its joins wrong allocates one block on top of another, which no single allocation test can see: only a check
/// that keeps looking at every live block while the arena churns does.
/// </remarks>
public sealed unsafe partial class UnifiedAllocatorTests
{
    /// <summary>
    /// Runs a deterministic mix of allocations and releases against a shadow model of the live blocks, checking after
    /// every step that no block was handed out on top of another and that every live block still holds its bytes.
    /// </summary>
    /// <remarks>
    /// The sizes and the release order are pseudo-random but reproducible, because a free list that only survives the
    /// orderings a human wrote down is not worth much. Each block is filled with a byte that identifies it, so a range
    /// reused over a live block shows up as the wrong byte rather than as a silent corruption.
    /// </remarks>
    [Test]
    public void FreeList_SurvivesARandomChurnWithoutOverlappingLiveBlocks()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), 16 * 1024);
        var live = new List<LiveBlock>(64);
        var random = new Random(20260101);
        var marker = 0;

        try
        {
            for (var step = 0; step < 600; step++)
            {
                // Release a random block about as often as the arena gains one, so the free list is used heavily while
                // the live set keeps moving.
                if (live.Count > 0 && random.Next(100) < 45)
                {
                    var index = random.Next(live.Count);
                    var released = live[index];
                    live.RemoveAt(index);

                    AssertLiveBytes(released, "a block must keep its bytes until it is released");
                    Assert.That(impl.TryFree(released.Block), Is.True);
                    continue;
                }

                var length = (nuint)random.Next(16, 4096);
                var allocated = impl.AllocBlock(length, UnifiedMemoryUsage.General);
                marker = (marker % 251) + 1;

                // Separate assertions rather than one multiple-assert block: a report that names the failing property is
                // worth more than one that fires three constraints at once.
                Assert.That(
                    (ulong)(allocated.Offset % UnifiedAllocator.MINIMUM_ALIGNMENT),
                    Is.Zero,
                    $"every block starts on an aligned unit (offset {allocated.Offset}, length {length})");

                Assert.That(
                    (ulong)(allocated.Offset + length),
                    Is.LessThanOrEqualTo((ulong)allocated.SegmentSize),
                    $"a block stays inside its segment (offset {allocated.Offset}, length {length}, segment {allocated.SegmentSize})");

                Assert.That(allocated.IsMapped, Is.True, "the shadow device maps the general blocks it serves");

                new Span<byte>(allocated.Host, (int)length).Fill((byte)marker);
                live.Add(new LiveBlock(allocated, (byte)marker));

                AssertNoOverlap(live);
                AssertEveryLiveBlockIntact(live);
            }

            // Releasing everything has to give the arena back in full: every range either reaches the cursor and pulls it,
            // or is joined onto the range that does.
            foreach (var released in live.ToArray())
            {
                AssertLiveBytes(released, "a block must keep its bytes until it is released");
                Assert.That(impl.TryFree(released.Block), Is.True);
            }

            AssertBytes(impl.Used, 0);
            AssertBytes(impl.GetFree(UnifiedMemoryUsage.General), 0);
        }
        finally
        {
            impl.GetAllocator().Dispose();
        }
    }

    /// <summary>
    /// Checks that every byte of a live block still holds the marker it was filled with.
    /// </summary>
    /// <param name="live">The live block to check.</param>
    /// <param name="message">The assertion message to use.</param>
    private static void AssertLiveBytes(LiveBlock live, string message)
    {
        var bytes = new ReadOnlySpan<byte>(live.Block.Host, (int)live.Block.Length);

        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] == live.Marker)
            {
                continue;
            }

            Assert.Fail(
                $"{message}: the block at offset {live.Block.Offset} of segment 0x{(nint)live.Block.SegmentHost:X} holds " +
                $"0x{bytes[index]:X2} at {index} instead of 0x{live.Marker:X2}.");

            return;
        }
    }

    /// <summary>
    /// Checks every live block of the churn.
    /// </summary>
    /// <param name="live">The live blocks.</param>
    private static void AssertEveryLiveBlockIntact(List<LiveBlock> live)
    {
        foreach (var block in live)
        {
            AssertLiveBytes(block, "a live block must keep its bytes while the arena churns");
        }
    }

    /// <summary>
    /// Checks that no two live blocks claim the same range of the same segment.
    /// </summary>
    /// <param name="live">The live blocks.</param>
    private static void AssertNoOverlap(List<LiveBlock> live)
    {
        for (var index = 0; index < live.Count; index++)
        {
            var block = live[index];

            for (var other = index + 1; other < live.Count; other++)
            {
                var candidate = live[other];

                if (candidate.Block.SegmentHost != block.Block.SegmentHost || candidate.Block.DeviceHandle != block.Block.DeviceHandle)
                {
                    continue;
                }

                var blockEnd = block.Block.Offset + block.Block.Length;
                var candidateEnd = candidate.Block.Offset + candidate.Block.Length;

                Assert.That(
                    blockEnd <= candidate.Block.Offset || candidateEnd <= block.Block.Offset,
                    Is.True,
                    $"two live blocks overlap: [{block.Block.Offset}, {blockEnd}) and [{candidate.Block.Offset}, {candidateEnd})");
            }
        }
    }

    /// <summary>
    /// A block of the churn: the description the allocator returned and the marker written into every one of its bytes.
    /// </summary>
    /// <param name="Block">The block the allocator returned.</param>
    /// <param name="Marker">The byte every byte of the block holds.</param>
    private readonly struct LiveBlock(UnifiedMemoryBlock block, byte marker)
    {
        /// <summary>
        /// The block the allocator returned.
        /// </summary>
        public readonly UnifiedMemoryBlock Block = block;

        /// <summary>
        /// The byte every byte of the block holds.
        /// </summary>
        public readonly byte Marker = marker;
    }
}
