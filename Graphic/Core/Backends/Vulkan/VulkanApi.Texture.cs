using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The part of the Vulkan API that creates and fills a texture: an image, a view, a sampler and the transfer commands that
/// move texels between the image and a staging buffer.
/// </summary>
/// <remarks>
/// The layouts mirror the Vulkan headers on a 64-bit build, including every explicit padding slot the other halves of the
/// interop use. An image is created with one mip and one layer, because that is the only shape <see cref="GpuTexture2D"/>
/// has.
/// </remarks>
internal static unsafe partial class VulkanApi
{
    /// <summary>
    /// <c>VkStructureType</c> for <c>VkImageCreateInfo</c>.
    /// </summary>
    internal const uint IMAGE_CREATE_INFO_TYPE = 14;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkImageViewCreateInfo</c>.
    /// </summary>
    internal const uint IMAGE_VIEW_CREATE_INFO_TYPE = 15;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkSamplerCreateInfo</c>.
    /// </summary>
    internal const uint SAMPLER_CREATE_INFO_TYPE = 31;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkImageMemoryBarrier</c>.
    /// </summary>
    internal const uint IMAGE_MEMORY_BARRIER_TYPE = 45;

    /// <summary>
    /// <c>VK_IMAGE_TYPE_2D</c>.
    /// </summary>
    internal const int IMAGE_TYPE_2D = 1;

    /// <summary>
    /// <c>VK_IMAGE_TILING_OPTIMAL</c>, which is the tiling a transferred and sampled image wants.
    /// </summary>
    internal const int IMAGE_TILING_OPTIMAL = 0;

    /// <summary>
    /// <c>VK_IMAGE_USAGE_TRANSFER_SRC_BIT</c>.
    /// </summary>
    internal const uint IMAGE_USAGE_TRANSFER_SOURCE = 0x00000001;

    /// <summary>
    /// <c>VK_IMAGE_USAGE_TRANSFER_DST_BIT</c>.
    /// </summary>
    internal const uint IMAGE_USAGE_TRANSFER_DESTINATION = 0x00000002;

    /// <summary>
    /// <c>VK_IMAGE_USAGE_SAMPLED_BIT</c>.
    /// </summary>
    internal const uint IMAGE_USAGE_SAMPLED = 0x00000004;

    /// <summary>
    /// <c>VK_IMAGE_LAYOUT_UNDEFINED</c>.
    /// </summary>
    internal const int IMAGE_LAYOUT_UNDEFINED = 0;

    /// <summary>
    /// <c>VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL</c>, the layout a sampled image binding expects.
    /// </summary>
    internal const int IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL = 5;

    /// <summary>
    /// <c>VK_IMAGE_LAYOUT_TRANSFER_SRC_OPTIMAL</c>.
    /// </summary>
    internal const int IMAGE_LAYOUT_TRANSFER_SOURCE_OPTIMAL = 6;

    /// <summary>
    /// <c>VK_IMAGE_LAYOUT_TRANSFER_DST_OPTIMAL</c>.
    /// </summary>
    internal const int IMAGE_LAYOUT_TRANSFER_DESTINATION_OPTIMAL = 7;

    /// <summary>
    /// <c>VK_IMAGE_ASPECT_COLOR_BIT</c>.
    /// </summary>
    internal const uint IMAGE_ASPECT_COLOR = 0x00000001;

    /// <summary>
    /// <c>VK_IMAGE_VIEW_TYPE_2D</c>.
    /// </summary>
    internal const int IMAGE_VIEW_TYPE_2D = 1;

    /// <summary>
    /// <c>VK_COMPONENT_SWIZZLE_IDENTITY</c>.
    /// </summary>
    internal const uint COMPONENT_SWIZZLE_IDENTITY = 0;

    /// <summary>
    /// <c>VK_SAMPLE_COUNT_1_BIT</c>.
    /// </summary>
    internal const uint SAMPLE_COUNT_1 = 0x00000001;

    /// <summary>
    /// <c>VK_SHARING_MODE_EXCLUSIVE</c>.
    /// </summary>
    internal const int SHARING_MODE_EXCLUSIVE = 0;

    /// <summary>
    /// <c>VK_QUEUE_FAMILY_IGNORED</c>.
    /// </summary>
    internal const uint QUEUE_FAMILY_IGNORED = 0xFFFFFFFF;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_TOP_OF_PIPE_BIT</c>, for a barrier whose source has no earlier work to wait for.
    /// </summary>
    internal const uint PIPELINE_STAGE_TOP_OF_PIPE = 0x00000001;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_FRAGMENT_SHADER_BIT</c>.
    /// </summary>
    internal const uint PIPELINE_STAGE_FRAGMENT_SHADER = 0x00000080;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_ALL_COMMANDS_BIT</c>, which covers a read the layer cannot name yet.
    /// </summary>
    internal const uint PIPELINE_STAGE_ALL_COMMANDS = 0x00010000;

    /// <summary>
    /// <c>VK_FILTER_NEAREST</c>.
    /// </summary>
    internal const int FILTER_NEAREST = 0;

    /// <summary>
    /// <c>VK_FILTER_LINEAR</c>.
    /// </summary>
    internal const int FILTER_LINEAR = 1;

    /// <summary>
    /// <c>VK_SAMPLER_MIPMAP_MODE_NEAREST</c>.
    /// </summary>
    internal const int SAMPLER_MIPMAP_MODE_NEAREST = 0;

    /// <summary>
    /// <c>VK_SAMPLER_ADDRESS_MODE_REPEAT</c>.
    /// </summary>
    internal const int SAMPLER_ADDRESS_MODE_REPEAT = 0;

    /// <summary>
    /// <c>VK_SAMPLER_ADDRESS_MODE_MIRRORED_REPEAT</c>.
    /// </summary>
    internal const int SAMPLER_ADDRESS_MODE_MIRRORED_REPEAT = 1;

    /// <summary>
    /// <c>VK_SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE</c>.
    /// </summary>
    internal const int SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE = 2;

    /// <summary>
    /// <c>VK_COMPARE_OP_ALWAYS</c>, the comparison a non-comparison sampler reports.
    /// </summary>
    internal const int COMPARE_OP_ALWAYS = 7;

    /// <summary>
    /// <c>VK_BORDER_COLOR_FLOAT_TRANSPARENT_BLACK</c>.
    /// </summary>
    internal const int BORDER_COLOR_FLOAT_TRANSPARENT_BLACK = 0;

    /// <summary>
    /// <c>VK_FORMAT_FEATURE_SAMPLED_IMAGE_BIT</c>.
    /// </summary>
    internal const uint FORMAT_FEATURE_SAMPLED_IMAGE = 0x00000001;

    /// <summary>
    /// <c>VK_FORMAT_FEATURE_TRANSFER_SRC_BIT</c>.
    /// </summary>
    internal const uint FORMAT_FEATURE_TRANSFER_SOURCE = 0x00004000;

    /// <summary>
    /// <c>VK_FORMAT_FEATURE_TRANSFER_DST_BIT</c>.
    /// </summary>
    internal const uint FORMAT_FEATURE_TRANSFER_DESTINATION = 0x00008000;

    /// <summary>
    /// <c>VK_DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER</c>, the descriptor a sampled texture is bound as.
    /// </summary>
    internal const uint DESCRIPTOR_TYPE_COMBINED_IMAGE_SAMPLER = 1;

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkImageCreateInfo*, nint, nint*, int> CreateImage =
        (delegate* unmanaged[Cdecl]<nint, VkImageCreateInfo*, nint, nint*, int>)Export("vkCreateImage");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroyImage =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroyImage");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, VkMemoryRequirements*, void> GetImageMemoryRequirements =
        (delegate* unmanaged[Cdecl]<nint, nint, VkMemoryRequirements*, void>)Export("vkGetImageMemoryRequirements");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, ulong, int> BindImageMemory =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, ulong, int>)Export("vkBindImageMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkImageViewCreateInfo*, nint, nint*, int> CreateImageView =
        (delegate* unmanaged[Cdecl]<nint, VkImageViewCreateInfo*, nint, nint*, int>)Export("vkCreateImageView");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroyImageView =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroyImageView");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkSamplerCreateInfo*, nint, nint*, int> CreateSampler =
        (delegate* unmanaged[Cdecl]<nint, VkSamplerCreateInfo*, nint, nint*, int>)Export("vkCreateSampler");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroySampler =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroySampler");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, int, uint, VkBufferImageCopy*, void> CmdCopyBufferToImage =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, int, uint, VkBufferImageCopy*, void>)Export("vkCmdCopyBufferToImage");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, int, nint, uint, VkBufferImageCopy*, void> CmdCopyImageToBuffer =
        (delegate* unmanaged[Cdecl]<nint, nint, int, nint, uint, VkBufferImageCopy*, void>)Export("vkCmdCopyImageToBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, VkFormatProperties*, void> GetPhysicalDeviceFormatProperties =
        (delegate* unmanaged[Cdecl]<nint, uint, VkFormatProperties*, void>)Export("vkGetPhysicalDeviceFormatProperties");
}

/// <summary>
/// <c>VkImageCreateInfo</c>, 88 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public int ImageType;
    public uint Format;
    public VkExtent3D Extent;
    public uint MipLevels;
    public uint ArrayLayers;
    public uint Samples;
    public int Tiling;
    public uint Usage;
    public int SharingMode;
    public uint QueueFamilyIndexCount;
    public uint QueueFamilyIndexPadding;
    public nint QueueFamilyIndexes;
    public int InitialLayout;
    public uint InitialLayoutPadding;
}

/// <summary>
/// <c>VkComponentMapping</c>, 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkComponentMapping
{
    public uint Red;
    public uint Green;
    public uint Blue;
    public uint Alpha;
}

/// <summary>
/// <c>VkImageSubresourceRange</c>, 20 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageSubresourceRange
{
    public uint AspectMask;
    public uint BaseMipLevel;
    public uint LevelCount;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

/// <summary>
/// <c>VkImageViewCreateInfo</c>, 80 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageViewCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public nint Image;
    public int ViewType;
    public uint Format;
    public VkComponentMapping Components;
    public VkImageSubresourceRange SubresourceRange;
}

/// <summary>
/// <c>VkImageSubresourceLayers</c>, 16 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageSubresourceLayers
{
    public uint AspectMask;
    public uint MipLevel;
    public uint BaseArrayLayer;
    public uint LayerCount;
}

/// <summary>
/// <c>VkOffset3D</c>, 12 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkOffset3D
{
    public int X;
    public int Y;
    public int Z;
}

/// <summary>
/// <c>VkBufferImageCopy</c>, 56 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferImageCopy
{
    public ulong BufferOffset;
    public uint BufferRowLength;
    public uint BufferImageHeight;
    public VkImageSubresourceLayers ImageSubresource;
    public VkOffset3D ImageOffset;
    public VkExtent3D ImageExtent;
}

/// <summary>
/// <c>VkImageMemoryBarrier</c>, 72 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkImageMemoryBarrier
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint SourceAccessMask;
    public uint DestinationAccessMask;
    public int OldLayout;
    public int NewLayout;
    public uint SourceQueueFamilyIndex;
    public uint DestinationQueueFamilyIndex;
    public nint Image;
    public VkImageSubresourceRange SubresourceRange;
}

/// <summary>
/// <c>VkSamplerCreateInfo</c>, 80 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSamplerCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public int MagFilter;
    public int MinFilter;
    public int MipmapMode;
    public int AddressModeU;
    public int AddressModeV;
    public int AddressModeW;
    public float MipLodBias;
    public uint AnisotropyEnable;
    public float MaxAnisotropy;
    public uint CompareEnable;
    public int CompareOp;
    public float MinLod;
    public float MaxLod;
    public int BorderColor;
    public uint UnnormalizedCoordinates;
}

/// <summary>
/// <c>VkDescriptorImageInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
/// <remarks>
/// The layer hands this out for the kernel binding step, so the image layout travels with the view instead of being
/// guessed by the caller.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorImageInfo
{
    public nint Sampler;
    public nint ImageView;
    public int ImageLayout;
    public uint LayoutPadding;
}

/// <summary>
/// <c>VkFormatProperties</c>, 12 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkFormatProperties
{
    public uint LinearTilingFeatures;
    public uint OptimalTilingFeatures;
    public uint BufferFeatures;
}
