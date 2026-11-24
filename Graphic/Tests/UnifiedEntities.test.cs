using Gubbins.Entities;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// Covers the wiring that makes the allocator useful: entity storage handed to a repository, so archetype chunk data lives
/// in memory the device reads without a copy.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EntityRepository"/> allocates every chunk from the <see cref="Gubbins.Unsafe.Allocator"/> it is given, so
/// passing <see cref="UnifiedAllocator.GetAllocator"/> is the whole integration. This test asserts what that buys: the
/// buffer the ECS writes into is a block of a shared segment, and on a coherent heap the address the device reads is
/// literally the address the ECS writes.
/// </para>
/// <para>
/// The provider is the same <c>ShadowDevice</c> the allocator tests use, so no graphics device is needed; the same
/// assertions hold against the Vulkan harness, which is where the hardware side is proven.
/// </para>
/// </remarks>
public sealed unsafe partial class UnifiedAllocatorTests
{
    [Test]
    public void EntityChunks_LiveInUnifiedMemory()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize * 4);
        using var repository = new EntityRepository(impl.GetAllocator(), chunkSizeBytes: (int)(s_SegmentSize * 2));

        for (var i = 0; i < 64; i++)
        {
            repository.Insert(i + 1);
        }

        var chunks = repository.Command(new ComponentFilter().Include<int>());
        using var components = chunks.GetLayout<int>();

        Assert.That(components.Segments.Count, Is.GreaterThan(0), "the query must expose at least one chunk segment");

        var span = components.Segments[0];
        fixed (int* data = span)
        {
            Assert.That(impl.TryGetSegment(data, out var segment), Is.True, "chunk storage must come from the unified arena");
            Assert.That(segment.Kind, Is.EqualTo(UnifiedMemoryKind.SharedCoherent));
            Assert.That(segment.IsMapped, Is.True);

            // Coherent memory: the address the device reads is the address the ECS writes.
            Assert.That(segment.DeviceAddressOfBlock, Is.EqualTo((ulong)data));
            Assert.That(((int*)segment.DeviceAddressOfBlock)[0], Is.EqualTo(1));
            Assert.That(((int*)segment.DeviceAddressOfBlock)[7], Is.EqualTo(8));

            // The values the ECS reports and the values at the device address are the same bytes, not a copy of them.
            Assert.That(span[0], Is.EqualTo(1));
            Assert.That(span[7], Is.EqualTo(8));
        }

        Assert.That((ulong)impl.GetUsed(UnifiedMemoryUsage.General), Is.GreaterThan(0UL), "chunk storage must be accounted to the shared heap");
    }

    [Test]
    public void DisposingTheRepository_ReturnsTheChunkStorage()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize * 4);

        var repository = new EntityRepository(impl.GetAllocator(), chunkSizeBytes: (int)(s_SegmentSize * 2));
        repository.Insert(1);
        var usedWithChunk = (ulong)impl.GetUsed(UnifiedMemoryUsage.General);

        repository.Dispose();
        var usedAfterDispose = (ulong)impl.GetUsed(UnifiedMemoryUsage.General);

        Assert.That(usedWithChunk, Is.GreaterThan(0UL));
        Assert.That(usedAfterDispose, Is.LessThan(usedWithChunk), "disposing the repository must give the chunk storage back");
    }

    [Test]
    public void UnifiedChunks_KeepWorkingAfterAFlushAndInvalidateRoundTrip()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), s_SegmentSize * 4);
        using var repository = new EntityRepository(impl.GetAllocator(), chunkSizeBytes: (int)(s_SegmentSize * 2));

        for (var i = 0; i < 32; i++)
        {
            repository.Insert(i * 3);
        }

        var chunks = repository.Command(new ComponentFilter().Include<int>());
        using var components = chunks.GetLayout<int>();

        var span = components.Segments[0];
        fixed (int* data = span)
        {
            impl.Flush(data, (nuint)(span.Length * sizeof(int)));
            impl.Invalidate(data, (nuint)(span.Length * sizeof(int)));

            Assert.That(impl.TryGetKind(data, out var kind), Is.True);
            Assert.That(kind, Is.EqualTo(UnifiedMemoryKind.SharedCoherent), "a coherent heap needs no cache maintenance");
            Assert.That(span[0], Is.EqualTo(0));
            Assert.That(span[5], Is.EqualTo(15));
        }
    }
}
