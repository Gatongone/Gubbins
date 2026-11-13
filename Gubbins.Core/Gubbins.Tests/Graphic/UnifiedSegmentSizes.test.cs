namespace Gubbins.Graphic.Tests;

/// <summary>
/// Covers the per-usage segment size policy: device-only memory and the shared pages want different segment sizes, because
/// one is a driver allocation the CPU never suballocates in and the other is memory the CPU fills every frame.
/// </summary>
public sealed unsafe partial class UnifiedAllocatorTests
{
    [Test]
    public void SegmentSizes_AreChosenPerUsage()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), new UnifiedSegmentSizes(4 * 1024, 32 * 1024));

        AssertBytes(impl.GetSegmentSize(UnifiedMemoryUsage.General), 4 * 1024);
        AssertBytes(impl.GetSegmentSize(UnifiedMemoryUsage.Device), 32 * 1024);

        var shared = impl.AllocBlock(1024, UnifiedMemoryUsage.General);

        Assert.That(impl.TryGetSegment(shared.Host, out var sharedSegment), Is.True);
        AssertBytes(sharedSegment.Size, 4 * 1024);

        // A device-only segment has no CPU address to look up, so its size shows up in the accounting instead.
        impl.AllocBlock(1024, UnifiedMemoryUsage.Device);
        AssertBytes(impl.GetCapacity(UnifiedMemoryUsage.Device), 32 * 1024);
        Assert.That(impl.SegmentCount, Is.EqualTo(2));
    }

    [Test]
    public void SegmentSizes_ZeroFallsBackToTheDefault()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), new UnifiedSegmentSizes(2048, 0));

        AssertBytes(impl.GetSegmentSize(UnifiedMemoryUsage.General), 2048);
        AssertBytes(impl.GetSegmentSize(UnifiedMemoryUsage.Device), UnifiedAllocator.DEFAULT_SEGMENT_SIZE);
    }

    [Test]
    public void SegmentSizes_AnAllocationLargerThanTheUsageSizeGetsItsOwnSegment()
    {
        using var device = ShadowDevice.CreateSoC();
        var impl = new UnifiedAllocator(device.GetProvider(), new UnifiedSegmentSizes(2048, 2048));

        // The configured size is a minimum, not a cap: a bigger block must still be served.
        var big = impl.AllocBlock(9000, UnifiedMemoryUsage.General);

        Assert.That(impl.TryGetSegment(big.Host, out var segment), Is.True);
        Assert.That((ulong)segment.Size, Is.GreaterThanOrEqualTo(9000UL));
        AssertBytes(segment.Offset, 0);

        // The uniform constructor keeps behaving like a one-size policy.
        var uniform = new UnifiedAllocator(device.GetProvider(), 1024);
        AssertBytes(uniform.GetSegmentSize(UnifiedMemoryUsage.General), 1024);
        AssertBytes(uniform.GetSegmentSize(UnifiedMemoryUsage.Device), 1024);
    }
}