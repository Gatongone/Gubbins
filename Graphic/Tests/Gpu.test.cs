namespace Gubbins.Graphic.Tests;

/// <summary>
/// The surface a program uses: a typed buffer allocated by the layer, and the synchronization that makes a device write
/// readable.
/// </summary>
/// <remarks>
/// These tests need a device but no kernel: they are the part of the API that hands out memory, which works on any
/// machine the layer can create a device on. The tests that run a kernel need the artifacts a build produces and are
/// skipped here when the machine has none.
/// </remarks>
public sealed class GpuTests
{
    [Test]
    public void GpuBuffer_UploadsAndReadsTheElements()
    {
        if (!Gpu.TryCreate(out var gpu) || gpu is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        using (gpu)
        {
            using var buffer = gpu.Upload(new uint[] { 1, 2, 3, 4 });

            Assert.That(buffer.Length, Is.EqualTo(4), "the buffer holds the elements it was written with");
            Assert.That(buffer.ByteLength, Is.EqualTo(4 * sizeof(uint)), "the size is the element count times the element");
            Assert.That(buffer.CpuSpan.ToArray(), Is.EqualTo(new uint[] { 1, 2, 3, 4 }), "the central processor reads what it wrote");
            Assert.That(buffer.Access, Is.EqualTo(GpuAccess.Shared), "a buffer the caller writes is shared by default");
        }
    }

    [Test]
    public void GpuBuffer_RefusesAReadAfterTheDeviceWroteIt()
    {
        if (!Gpu.TryCreate(out var gpu) || gpu is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        using (gpu)
        {
            using var buffer = gpu.Allocate<uint>(4);
            buffer.MarkWrittenByGpu();

            Assert.That(() => _ = buffer.CpuSpan, Throws.TypeOf<InvalidOperationException>(), "a device write is only readable at a synchronization point");

            gpu.Synchronize();

            Assert.That(buffer.CpuSpan.Length, Is.EqualTo(4), "the synchronization makes the buffer readable");
        }
    }

    [Test]
    public void GpuBuffer_AllocatesAFreshBlockForEveryCall()
    {
        if (!Gpu.TryCreate(out var gpu) || gpu is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        using (gpu)
        {
            using var first = gpu.Allocate<uint>(8);
            using var second = gpu.Allocate<uint>(8);

            first.Upload(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 });

            Assert.That(second.CpuSpan.ToArray(), Is.EqualTo(new uint[8]), "a second buffer is its own block");
            Assert.That(first.CpuSpan.ToArray(), Is.EqualTo(new uint[] { 1, 2, 3, 4, 5, 6, 7, 8 }), "the first buffer keeps what it was written with");
        }
    }
}
