using System.Runtime.InteropServices;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// Asserts the native layouts of the Vulkan structures the texture path passes to the driver.
/// </summary>
/// <remarks>
/// A wrong size or offset is answered by the driver with an error code, a crash or silently wrong texels rather than a
/// managed exception, so the layouts are pinned here against the numbers <c>vulkan_core.h</c> gives on a 64-bit build.
/// </remarks>
public sealed class VulkanInteropTests
{
    [Test]
    public unsafe void VulkanTextureStructs_HaveTheHeaderSizes()
    {
        Assert.That(sizeof(VkImageCreateInfo), Is.EqualTo(88), "VkImageCreateInfo is 88 bytes");
        Assert.That(sizeof(VkImageViewCreateInfo), Is.EqualTo(80), "VkImageViewCreateInfo is 80 bytes");
        Assert.That(sizeof(VkSamplerCreateInfo), Is.EqualTo(80), "VkSamplerCreateInfo is 80 bytes");
        Assert.That(sizeof(VkBufferImageCopy), Is.EqualTo(56), "VkBufferImageCopy is 56 bytes");
        Assert.That(sizeof(VkImageMemoryBarrier), Is.EqualTo(72), "VkImageMemoryBarrier is 72 bytes");
        Assert.That(sizeof(VkImageSubresourceRange), Is.EqualTo(20), "VkImageSubresourceRange is 20 bytes");
        Assert.That(sizeof(VkImageSubresourceLayers), Is.EqualTo(16), "VkImageSubresourceLayers is 16 bytes");
        Assert.That(sizeof(VkComponentMapping), Is.EqualTo(16), "VkComponentMapping is 16 bytes");
        Assert.That(sizeof(VkOffset3D), Is.EqualTo(12), "VkOffset3D is 12 bytes");
        Assert.That(sizeof(VkDescriptorImageInfo), Is.EqualTo(24), "VkDescriptorImageInfo is 24 bytes");
        Assert.That(sizeof(VkFormatProperties), Is.EqualTo(12), "VkFormatProperties is 12 bytes");
    }

    [Test]
    public void VulkanTextureStructs_HaveTheHeaderOffsets()
    {
        Assert.That(OffsetOf<VkImageCreateInfo>("Extent"), Is.EqualTo(28), "the image extent follows the format");
        Assert.That(OffsetOf<VkImageCreateInfo>("Usage"), Is.EqualTo(56), "the image usage follows the sample count");
        Assert.That(OffsetOf<VkImageCreateInfo>("InitialLayout"), Is.EqualTo(80), "the initial layout follows the queue families");

        Assert.That(OffsetOf<VkImageViewCreateInfo>("Image"), Is.EqualTo(24), "the view image follows the flags");
        Assert.That(OffsetOf<VkImageViewCreateInfo>("Format"), Is.EqualTo(36), "the view format follows the view type");
        Assert.That(OffsetOf<VkImageViewCreateInfo>("Components"), Is.EqualTo(40), "the swizzle follows the format");
        Assert.That(OffsetOf<VkImageViewCreateInfo>("SubresourceRange"), Is.EqualTo(56), "the range follows the swizzle");

        Assert.That(OffsetOf<VkSamplerCreateInfo>("MagFilter"), Is.EqualTo(20), "the filters follow the flags");
        Assert.That(OffsetOf<VkSamplerCreateInfo>("AddressModeU"), Is.EqualTo(32), "the address modes follow the mipmap mode");
        Assert.That(OffsetOf<VkSamplerCreateInfo>("MinLod"), Is.EqualTo(64), "the LOD range follows the compare state");
        Assert.That(OffsetOf<VkSamplerCreateInfo>("UnnormalizedCoordinates"), Is.EqualTo(76), "the coordinates flag is last");

        Assert.That(OffsetOf<VkBufferImageCopy>("ImageSubresource"), Is.EqualTo(16), "the subresource follows the row lengths");
        Assert.That(OffsetOf<VkBufferImageCopy>("ImageOffset"), Is.EqualTo(32), "the offset follows the subresource");
        Assert.That(OffsetOf<VkBufferImageCopy>("ImageExtent"), Is.EqualTo(44), "the extent follows the offset");

        Assert.That(OffsetOf<VkImageMemoryBarrier>("SourceAccessMask"), Is.EqualTo(16), "the source access follows pNext");
        Assert.That(OffsetOf<VkImageMemoryBarrier>("OldLayout"), Is.EqualTo(24), "the layouts follow the access masks");
        Assert.That(OffsetOf<VkImageMemoryBarrier>("Image"), Is.EqualTo(40), "the image follows the queue families");
        Assert.That(OffsetOf<VkImageMemoryBarrier>("SubresourceRange"), Is.EqualTo(48), "the range follows the image");

        Assert.That(OffsetOf<VkDescriptorImageInfo>("ImageLayout"), Is.EqualTo(16), "the layout follows the image view");
    }

    /// <summary>
    /// Reads the byte offset of a field, which is the number the driver reads.
    /// </summary>
    /// <typeparam name="T">The structure to inspect.</typeparam>
    /// <param name="field">The field name.</param>
    /// <returns>The offset in bytes.</returns>
    private static int OffsetOf<T>(string field) => (int)Marshal.OffsetOf(typeof(T), field);
}
