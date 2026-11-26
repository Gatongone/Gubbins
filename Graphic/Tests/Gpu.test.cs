namespace Gubbins.Graphic.Tests;

using Gubbins.Graphic.D3D12;

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
    public void MetalTexture_UploadsAndReadsBack()
    {
        if (!OperatingSystem.IsMacOS() || !MetalDevice.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Metal device.");
            return;
        }

        using (owner)
        using (var texture = owner.Provider.CreateTexture(4, 4, GpuTextureFormat.Rgba8Unorm))
        using (var sampler = owner.Provider.CreateSampler(new GpuSampler(GpuTextureFilter.Linear, GpuAddressMode.Repeat)))
        {
            var pixels = new byte[4 * 4 * 4];

            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = (byte)(index * 7);
            }

            texture.Upload(pixels, 4 * 4);

            var readback = new byte[pixels.Length];
            texture.Readback(readback, 4 * 4);

            Assert.That(readback, Is.EqualTo(pixels), "the hardware texture returns the pixels it was written with");
            Assert.That(texture.Width, Is.EqualTo(4), "the texture keeps its width");
            Assert.That(texture.Height, Is.EqualTo(4), "the texture keeps its height");
            Assert.That(texture.Format, Is.EqualTo(GpuTextureFormat.Rgba8Unorm), "the texture keeps its format");
        }
    }

    [Test]
    public unsafe void D3D12DescriptorHeap_Starts()
    {
        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        using (owner)
        {
            var description = new D3D12DescriptorHeapDesc { Type = 0, NumDescriptors = 1, Flags = 1, NodeMask = 0 };
            var result = D3D12Api.CreateDescriptorHeap(owner.Provider.Device, &description, out var heap);

            Assert.That(result >= 0 && heap != 0, Is.True, $"the descriptor heap was created ({result:X8})");

            var cpu = D3D12Api.GetCpuDescriptorHandle(heap);
            var gpu = D3D12Api.GetGpuDescriptorHandle(heap);

            TestContext.WriteLine($"heap={heap:X}, cpu={cpu.Pointer:X}, gpu={gpu.Pointer:X}");
            Assert.That(cpu.Pointer, Is.Not.Zero, "the CPU descriptor handle is set");

            D3D12Api.Release(heap);
        }
    }

    [Test]
    public void D3D12Texture_UploadsAndReadsBack()
    {
        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        using (owner)
        using (var texture = owner.Provider.CreateTexture(4, 4, GpuTextureFormat.Rgba8Unorm))
        {
            var pixels = new byte[4 * 4 * 4];

            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = (byte)(index * 5);
            }

            texture.Upload(pixels, 4 * 4);

            var readback = new byte[pixels.Length];
            texture.Readback(readback, 4 * 4);

            Assert.That(readback, Is.EqualTo(pixels), "the hardware texture returns the pixels it was written with");
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
