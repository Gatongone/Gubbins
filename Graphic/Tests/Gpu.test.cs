namespace Gubbins.Graphic.Tests;

using System.Runtime.InteropServices;
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
    public unsafe void D3D12Texture_NullSrv()
    {
        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        using (owner)
        using (var texture = owner.Provider.CreateTexture(4, 4, GpuTextureFormat.Rgba8Unorm))
        {
            texture.Upload(new byte[4 * 4 * 4], 4 * 4);

            var heapDescription = new D3D12DescriptorHeapDesc { Type = 0, NumDescriptors = 1, Flags = 1, NodeMask = 0 };
            Assert.That(D3D12Api.CreateDescriptorHeap(owner.Provider.Device, &heapDescription, out var heap) >= 0, Is.True, "the SRV heap was created");

            var resource = ((D3D12Texture)texture.Backend).Resource;
            D3D12Api.CreateShaderResourceView(owner.Provider.Device, resource, null, D3D12Api.GetCpuDescriptorHandle(heap));

            var probeProperties = new D3D12HeapProperties { Type = 2, CreationNodeMask = 1, VisibleNodeMask = 1 };
            var probe = new D3D12HeapDesc { SizeInBytes = 65536, Properties = probeProperties, Alignment = 0, Flags = 0 };
            var probeResult = D3D12Api.CreateHeap(owner.Provider.Device, &probe, out var probeHeap);

            Assert.That(probeResult >= 0 && probeHeap != 0, Is.True, $"the device still creates heaps after a null SRV ({probeResult:X8})");

            D3D12Api.Release(probeHeap);
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

    [Test]
    public void VulkanTexture_UploadsAndReadsBack()
    {
        if (!TryCreateVulkan(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Vulkan device.");
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

            var backend = (VulkanTexture)texture.Backend;
            var samplerBackend = (VulkanSamplerState)sampler.Backend;
            var descriptor = backend.GetDescriptorInfo(samplerBackend);

            Assert.That(backend.Image, Is.Not.Zero, "the texture owns a VkImage");
            Assert.That(backend.ImageView, Is.Not.Zero, "the texture owns a VkImageView");
            Assert.That(backend.NativeFormat, Is.EqualTo(37u), "the texture uses the Vulkan format for RGBA8");
            Assert.That(backend.Layout, Is.EqualTo(VulkanApi.IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL), "the texture is left sampleable");
            Assert.That(samplerBackend.Sampler, Is.Not.Zero, "the sampler owns a VkSampler");
            Assert.That(descriptor.Sampler, Is.EqualTo(samplerBackend.Sampler), "the descriptor names the sampler");
            Assert.That(descriptor.ImageView, Is.EqualTo(backend.ImageView), "the descriptor names the image view");
            Assert.That(descriptor.ImageLayout, Is.EqualTo(VulkanApi.IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL), "the descriptor names the sampleable layout");
        }
    }

    [Test]
    public void VulkanTexture_UploadsRowsWithAPaddedPitch()
    {
        if (!TryCreateVulkan(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Vulkan device.");
            return;
        }

        const int Width = 3;
        const int Height = 2;
        const int BytesPerRow = 16;

        using (owner)
        using (var texture = owner.Provider.CreateTexture(Width, Height, GpuTextureFormat.Rgba8Unorm))
        {
            var pixels = new byte[BytesPerRow * Height];

            for (var index = 0; index < pixels.Length; index++)
            {
                pixels[index] = (byte)(index * 3);
            }

            texture.Upload(pixels, BytesPerRow);

            var readback = new byte[pixels.Length];
            texture.Readback(readback, BytesPerRow);

            for (var row = 0; row < Height; row++)
            {
                var expected = pixels.AsSpan(row * BytesPerRow, Width * 4).ToArray();
                var actual = readback.AsSpan(row * BytesPerRow, Width * 4).ToArray();

                Assert.That(actual, Is.EqualTo(expected), $"row {row} survives a row pitch wider than the image row");
            }
        }
    }

    [Test]
    public void VulkanSampler_CreatesEveryDescription()
    {
        if (!TryCreateVulkan(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Vulkan device.");
            return;
        }

        using (owner)
        {
            foreach (var filter in new[] { GpuTextureFilter.Nearest, GpuTextureFilter.Linear })
            {
                foreach (var address in new[] { GpuAddressMode.Clamp, GpuAddressMode.Repeat, GpuAddressMode.Mirror })
                {
                    using var sampler = owner.Provider.CreateSampler(new GpuSampler(filter, address));
                    var backend = (VulkanSamplerState)sampler.Backend;

                    Assert.That(backend.Sampler, Is.Not.Zero, $"a {filter} {address} sampler was created");
                    Assert.That(backend.Description.Filter, Is.EqualTo(filter), "the sampler keeps its filter");
                    Assert.That(backend.Description.Address, Is.EqualTo(address), "the sampler keeps its address mode");
                }
            }
        }
    }

    /// <summary>
    /// Creates the Vulkan device the texture tests run on, or reports that this machine has none.
    /// </summary>
    /// <param name="owner">When the method returns, the device, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a device was created.</returns>
    private static bool TryCreateVulkan(out VulkanDevice? owner)
    {
        owner = null;

        // RuntimeInformation is the portable form: the netstandard target of this project has no OperatingSystem.IsWindows.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            return VulkanDevice.TryCreate(out owner) && owner is not null;
        }
        catch (Exception)
        {
            // A machine without the loader reports it by refusing to load, which is the device not being there.
            owner = null;
            return false;
        }
    }
}
