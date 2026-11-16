namespace Gubbins.Graphic.Tests;

/// <summary>
/// The layer above the allocator, on the three shapes of machine the shadow device models: a coherent unified memory SoC,
/// a non-coherent one, and a machine where the device's memory is not addressable at all.
/// </summary>
/// <remarks>
/// These tests live in the allocator fixture because they drive the same shadow device, which is the only place a
/// provider can be configured to be any of the three without a device.
/// </remarks>
public sealed unsafe partial class UnifiedAllocatorTests
{
    [Test]
    public void GpuMemory_ServesACpuBlockWithNoCopyBetweenTheProcessors()
    {
        using var device = ShadowDevice.CreateSoC();
        using var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);

        var block = memory.Allocate(256, GpuAccess.Shared);
        var pattern = GpuPattern(256);

        pattern.CopyTo(block.CpuView());
        memory.MarkWrittenByCpu(block);

        Assert.That(memory.Synchronize(), Is.True);
        Assert.That(block.CpuView().ToArray(), Is.EqualTo(pattern));
        Assert.That(device.PendingCopies, Is.Zero, "a coherent shared block is the same memory on both sides");
    }

    [Test]
    public void GpuMemory_PublishesCpuWritesOnANonCoherentMachine()
    {
        // The shadow device models a non-coherent shared heap as two blocks: the CPU only writes the host one, so an
        // invalidate after the device wrote the block brings back whatever a flush had published and zeros otherwise.
        using var device = ShadowDevice.CreateNonCoherentSoC();
        using var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);

        var block = memory.Allocate(256, GpuAccess.Shared);
        var pattern = GpuPattern(256);

        pattern.CopyTo(block.CpuView());
        memory.MarkWrittenByCpu(block);
        Assert.That(memory.Synchronize(), Is.True);

        memory.MarkWrittenByGpu(block);
        Assert.That(memory.Synchronize(), Is.True);
        Assert.That(block.CpuView().ToArray(), Is.EqualTo(pattern), "the CPU write reached the device's view");
    }

    [Test]
    public void GpuMemory_RefusesADeviceWriteUntilTheSynchronizationPoint()
    {
        using var device = ShadowDevice.CreateSoC();
        using var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);

        var block = memory.Allocate(128, GpuAccess.Shared);

        Assert.That(TryView(memory, block), Is.True);

        memory.MarkWrittenByGpu(block);
        Assert.That(TryView(memory, block), Is.False, "the device wrote the block after the last synchronization point");

        Assert.That(memory.Synchronize(), Is.True);
        Assert.That(TryView(memory, block), Is.True);

        // Synchronizing again is harmless and does not start a copy.
        Assert.That(memory.Synchronize(), Is.True);
        Assert.That(device.PendingCopies, Is.Zero);
    }

    [Test]
    public void GpuMemory_CopiesADeviceBlockBackAtTheSynchronizationPoint()
    {
        using var device = ShadowDevice.CreateSoC();
        using var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);

        var block = memory.Allocate(256, GpuAccess.Sync);
        var pattern = GpuPattern(256);

        Assert.That(block.Storage.IsMapped, Is.False, "device only memory is not addressable by the CPU");
        Assert.That(block.IsCpuReadable, Is.True, "the layer still gives the CPU a block to read");

        // Fill the device's memory the way a device would: a copy out of a block the CPU can write.
        var staging = memory.Allocator.AllocBlock(256, UnifiedMemoryUsage.General);
        pattern.CopyTo(new Span<byte>(staging.Host, 256));
        Assert.That(memory.Allocator.TryCopy(block.Storage, 0, staging, 0, 256), Is.True);

        memory.MarkWrittenByGpu(block);
        Assert.That(TryView(memory, block), Is.False);

        Assert.That(memory.Synchronize(), Is.True);
        Assert.That(block.CpuView().ToArray(), Is.EqualTo(pattern));
        Assert.That(memory.Allocator.TryFree(staging), Is.True);
    }

    [Test]
    public void GpuMemory_ReleaseReturnsTheBlockToTheAllocator()
    {
        using var device = ShadowDevice.CreateSoC();
        using var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);

        var before = (ulong)memory.Allocator.GetUsed(UnifiedMemoryUsage.General);
        var block = memory.Allocate(256, GpuAccess.Shared);

        Assert.That((ulong)memory.Allocator.GetUsed(UnifiedMemoryUsage.General), Is.GreaterThan(before));
        Assert.That(memory.Release(block), Is.True);
        Assert.That((ulong)memory.Allocator.GetUsed(UnifiedMemoryUsage.General), Is.EqualTo(before));
    }

    [Test]
    public void GpuMemory_DisposeReturnsEveryBlockItStillOwns()
    {
        using var device = ShadowDevice.CreateSoC();
        var memory = new GpuMemory(device.GetProvider(), s_SegmentSize);
        var before = (ulong)memory.Allocator.GetUsed(UnifiedMemoryUsage.General);

        memory.Allocate(256, GpuAccess.Shared);
        memory.Allocate(256, GpuAccess.Shared);
        memory.Dispose();

        Assert.That((ulong)memory.Allocator.GetUsed(UnifiedMemoryUsage.General), Is.EqualTo(before));
    }

    /// <summary>
    /// Builds a byte pattern that a mistake in the copy paths cannot reproduce by accident.
    /// </summary>
    /// <param name="length">How many bytes to build.</param>
    /// <returns>The pattern.</returns>
    private static byte[] GpuPattern(int length)
    {
        var pattern = new byte[length];

        for (var i = 0; i < length; i++)
        {
            pattern[i] = (byte)((i * 7) + 1);
        }

        return pattern;
    }

    /// <summary>
    /// Reports whether the central processor may read a block, without letting the exception out of the helper.
    /// </summary>
    /// <param name="memory">The layer to ask.</param>
    /// <param name="block">The block to read.</param>
    /// <returns><see langword="true"/> if the block can be read.</returns>
    private static bool TryView(GpuMemory memory, GpuBlock block)
    {
        try
        {
            _ = block.CpuView().Length;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
