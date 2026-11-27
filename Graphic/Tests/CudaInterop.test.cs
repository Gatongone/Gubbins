using System.Runtime.InteropServices;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// Asserts the native layouts of the CUDA structures the texture path passes to the driver.
/// </summary>
/// <remarks>
/// A wrong size or offset is answered by the driver with an error code, a crash or silently wrong texels rather than a
/// managed exception, so the layouts are pinned here against the numbers <c>cuda.h</c> gives on a 64-bit build. The
/// numbers are the ones the header calls <c>CUDA_ARRAY_DESCRIPTOR_v2</c>, <c>CUDA_RESOURCE_DESC_v1</c>,
/// <c>CUDA_TEXTURE_DESC_v1</c>, <c>CUDA_RESOURCE_VIEW_DESC_v1</c> and <c>CUDA_MEMCPY2D_v2</c>.
/// </remarks>
public sealed unsafe class CudaInteropTests
{
    [Test]
    public void CudaTextureStructs_HaveTheHeaderSizes()
    {
        Assert.That(sizeof(CudaApi.CUDA_ARRAY_DESCRIPTOR), Is.EqualTo(24), "CUDA_ARRAY_DESCRIPTOR is 24 bytes");
        Assert.That(sizeof(CudaApi.CUDA_RESOURCE_DESC), Is.EqualTo(144), "CUDA_RESOURCE_DESC is 144 bytes");
        Assert.That(sizeof(CudaApi.CUDA_TEXTURE_DESC), Is.EqualTo(104), "CUDA_TEXTURE_DESC is 104 bytes");
        Assert.That(sizeof(CudaApi.CUDA_RESOURCE_VIEW_DESC), Is.EqualTo(112), "CUDA_RESOURCE_VIEW_DESC is 112 bytes");
        Assert.That(sizeof(CudaApi.CUDA_MEMCPY2D), Is.EqualTo(128), "CUDA_MEMCPY2D is 128 bytes");
    }

    [Test]
    public void CudaTextureStructs_HaveTheHeaderOffsets()
    {
        Assert.That(OffsetOf<CudaApi.CUDA_ARRAY_DESCRIPTOR>("Height"), Is.EqualTo(8), "the height follows the width");
        Assert.That(OffsetOf<CudaApi.CUDA_ARRAY_DESCRIPTOR>("Format"), Is.EqualTo(16), "the format follows the height");
        Assert.That(OffsetOf<CudaApi.CUDA_ARRAY_DESCRIPTOR>("NumChannels"), Is.EqualTo(20), "the channel count is last");

        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_DESC>("ResType"), Is.EqualTo(0), "the resource type is first");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_DESC>("HArray"), Is.EqualTo(8), "the array follows the padded union start");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_DESC>("Flags"), Is.EqualTo(136), "the flags follow the union");

        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("FilterMode"), Is.EqualTo(12), "the filter follows the three address modes");
        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("Flags"), Is.EqualTo(16), "the flags follow the filter");
        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("MaxAnisotropy"), Is.EqualTo(20), "the anisotropy follows the flags");
        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("MipmapFilterMode"), Is.EqualTo(24), "the mipmap filter follows the anisotropy");
        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("BorderColor"), Is.EqualTo(40), "the border colour follows the lod range");
        Assert.That(OffsetOf<CudaApi.CUDA_TEXTURE_DESC>("Reserved"), Is.EqualTo(56), "the reserved fields are last");

        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("Width"), Is.EqualTo(8), "the width follows the padded format");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("Height"), Is.EqualTo(16), "the height follows the width");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("Depth"), Is.EqualTo(24), "the depth follows the height");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("FirstMipmapLevel"), Is.EqualTo(32), "the mipmap range follows the depth");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("FirstLayer"), Is.EqualTo(40), "the layer range follows the mipmap range");
        Assert.That(OffsetOf<CudaApi.CUDA_RESOURCE_VIEW_DESC>("Reserved"), Is.EqualTo(48), "the reserved fields are last");

        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("SourceMemoryType"), Is.EqualTo(16), "the source kind follows the coordinates");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("SourceHost"), Is.EqualTo(24), "the source host pointer follows the padded kind");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("SourceArray"), Is.EqualTo(40), "the source array follows the device pointer");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("SourcePitch"), Is.EqualTo(48), "the source pitch follows the array");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("DestinationXInBytes"), Is.EqualTo(56), "the destination starts after the source");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("DestinationArray"), Is.EqualTo(96), "the destination array follows the device pointer");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("WidthInBytes"), Is.EqualTo(112), "the width follows the destination pitch");
        Assert.That(OffsetOf<CudaApi.CUDA_MEMCPY2D>("Height"), Is.EqualTo(120), "the height is last");
    }

    /// <summary>
    /// Reads the byte offset of a field, which is the number the driver reads.
    /// </summary>
    /// <typeparam name="T">The structure to inspect.</typeparam>
    /// <param name="field">The field name.</param>
    /// <returns>The offset in bytes.</returns>
    private static int OffsetOf<T>(string field) => (int)Marshal.OffsetOf(typeof(T), field);
}
