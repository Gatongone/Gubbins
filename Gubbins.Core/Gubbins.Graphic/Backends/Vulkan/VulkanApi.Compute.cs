#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic.Vulkan;

/// <summary>
/// The part of the Vulkan API that only a harness needs: creating a compute pipeline, so a shader can be pointed at a
/// shared block on real hardware.
/// </summary>
/// <remarks>
/// A bridge has no business creating pipelines, which is why this half of the interop is separate from the allocator half.
/// It exists to close the last verification gap: every other harness check proves the copy engine reads the block, and this
/// one proves a shader does.
/// </remarks>
internal static unsafe partial class VulkanApi
{
    /// <summary>
    /// <c>VkStructureType</c> for <c>VkShaderModuleCreateInfo</c>.
    /// </summary>
    internal const uint SHADER_MODULE_CREATE_INFO_TYPE = 16;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkPipelineShaderStageCreateInfo</c>.
    /// </summary>
    internal const uint PIPELINE_SHADER_STAGE_CREATE_INFO_TYPE = 18;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkComputePipelineCreateInfo</c>.
    /// </summary>
    internal const uint COMPUTE_PIPELINE_CREATE_INFO_TYPE = 29;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkPipelineLayoutCreateInfo</c>.
    /// </summary>
    internal const uint PIPELINE_LAYOUT_CREATE_INFO_TYPE = 30;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkDescriptorSetLayoutCreateInfo</c>.
    /// </summary>
    internal const uint DESCRIPTOR_SET_LAYOUT_CREATE_INFO_TYPE = 32;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkDescriptorPoolCreateInfo</c>.
    /// </summary>
    internal const uint DESCRIPTOR_POOL_CREATE_INFO_TYPE = 33;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkDescriptorSetAllocateInfo</c>.
    /// </summary>
    internal const uint DESCRIPTOR_SET_ALLOCATE_INFO_TYPE = 34;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkWriteDescriptorSet</c>.
    /// </summary>
    internal const uint WRITE_DESCRIPTOR_SET_TYPE = 35;

    /// <summary>
    /// <c>VK_DESCRIPTOR_TYPE_STORAGE_BUFFER</c>.
    /// </summary>
    internal const uint DESCRIPTOR_TYPE_STORAGE_BUFFER = 7;

    /// <summary>
    /// <c>VK_SHADER_STAGE_COMPUTE_BIT</c>.
    /// </summary>
    internal const uint SHADER_STAGE_COMPUTE = 0x00000020;

    /// <summary>
    /// <c>VK_PIPELINE_BIND_POINT_COMPUTE</c>.
    /// </summary>
    internal const int PIPELINE_BIND_POINT_COMPUTE = 1;

    /// <summary>
    /// <c>VK_ACCESS_SHADER_READ_BIT</c>.
    /// </summary>
    internal const uint ACCESS_SHADER_READ = 0x00000020;

    /// <summary>
    /// <c>VK_ACCESS_SHADER_WRITE_BIT</c>.
    /// </summary>
    internal const uint ACCESS_SHADER_WRITE = 0x00000040;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_COMPUTE_SHADER_BIT</c>.
    /// </summary>
    internal const uint PIPELINE_STAGE_COMPUTE_SHADER = 0x00000800;

    internal static readonly delegate* unmanaged<nint, VkShaderModuleCreateInfo*, nint, nint*, int> CreateShaderModule =
        (delegate* unmanaged<nint, VkShaderModuleCreateInfo*, nint, nint*, int>)Export("vkCreateShaderModule");

    internal static readonly delegate* unmanaged<nint, nint, nint, void> DestroyShaderModule =
        (delegate* unmanaged<nint, nint, nint, void>)Export("vkDestroyShaderModule");

    internal static readonly delegate* unmanaged<nint, VkDescriptorSetLayoutCreateInfo*, nint, nint*, int> CreateDescriptorSetLayout =
        (delegate* unmanaged<nint, VkDescriptorSetLayoutCreateInfo*, nint, nint*, int>)Export("vkCreateDescriptorSetLayout");

    internal static readonly delegate* unmanaged<nint, nint, nint, void> DestroyDescriptorSetLayout =
        (delegate* unmanaged<nint, nint, nint, void>)Export("vkDestroyDescriptorSetLayout");

    internal static readonly delegate* unmanaged<nint, VkPipelineLayoutCreateInfo*, nint, nint*, int> CreatePipelineLayout =
        (delegate* unmanaged<nint, VkPipelineLayoutCreateInfo*, nint, nint*, int>)Export("vkCreatePipelineLayout");

    internal static readonly delegate* unmanaged<nint, nint, nint, void> DestroyPipelineLayout =
        (delegate* unmanaged<nint, nint, nint, void>)Export("vkDestroyPipelineLayout");

    internal static readonly delegate* unmanaged<nint, nint, uint, VkComputePipelineCreateInfo*, nint, nint*, int> CreateComputePipelines =
        (delegate* unmanaged<nint, nint, uint, VkComputePipelineCreateInfo*, nint, nint*, int>)Export("vkCreateComputePipelines");

    internal static readonly delegate* unmanaged<nint, nint, nint, void> DestroyPipeline =
        (delegate* unmanaged<nint, nint, nint, void>)Export("vkDestroyPipeline");

    internal static readonly delegate* unmanaged<nint, VkDescriptorPoolCreateInfo*, nint, nint*, int> CreateDescriptorPool =
        (delegate* unmanaged<nint, VkDescriptorPoolCreateInfo*, nint, nint*, int>)Export("vkCreateDescriptorPool");

    internal static readonly delegate* unmanaged<nint, nint, nint, void> DestroyDescriptorPool =
        (delegate* unmanaged<nint, nint, nint, void>)Export("vkDestroyDescriptorPool");

    internal static readonly delegate* unmanaged<nint, VkDescriptorSetAllocateInfo*, nint*, int> AllocateDescriptorSets =
        (delegate* unmanaged<nint, VkDescriptorSetAllocateInfo*, nint*, int>)Export("vkAllocateDescriptorSets");

    internal static readonly delegate* unmanaged<nint, uint, VkWriteDescriptorSet*, uint, nint, void> UpdateDescriptorSets =
        (delegate* unmanaged<nint, uint, VkWriteDescriptorSet*, uint, nint, void>)Export("vkUpdateDescriptorSets");

    internal static readonly delegate* unmanaged<nint, int, nint, void> CmdBindPipeline =
        (delegate* unmanaged<nint, int, nint, void>)Export("vkCmdBindPipeline");

    internal static readonly delegate* unmanaged<nint, int, nint, uint, uint, nint*, uint, uint*, void> CmdBindDescriptorSets =
        (delegate* unmanaged<nint, int, nint, uint, uint, nint*, uint, uint*, void>)Export("vkCmdBindDescriptorSets");

    internal static readonly delegate* unmanaged<nint, uint, uint, uint, void> CmdDispatch =
        (delegate* unmanaged<nint, uint, uint, uint, void>)Export("vkCmdDispatch");
}

/// <summary>
/// <c>VkShaderModuleCreateInfo</c>, 40 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkShaderModuleCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public nuint CodeSize;
    public nint Code;
}

/// <summary>
/// <c>VkPipelineShaderStageCreateInfo</c>, 48 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineShaderStageCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint Stage;
    public nint Module;
    public nint Name;
    public nint SpecializationInfo;
}

/// <summary>
/// <c>VkComputePipelineCreateInfo</c>, 96 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkComputePipelineCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public VkPipelineShaderStageCreateInfo Stage;
    public nint Layout;
    public nint BasePipelineHandle;
    public int BasePipelineIndex;
    public uint IndexPadding;
}

/// <summary>
/// <c>VkPipelineLayoutCreateInfo</c>, 48 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPipelineLayoutCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint SetLayoutCount;
    public nint SetLayouts;
    public uint PushConstantRangeCount;
    public uint PushConstantRangePadding;
    public nint PushConstantRanges;
}

/// <summary>
/// <c>VkDescriptorSetLayoutBinding</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetLayoutBinding
{
    public uint Binding;
    public uint DescriptorType;
    public uint DescriptorCount;
    public uint StageFlags;
    public nint ImmutableSamplers;
}

/// <summary>
/// <c>VkDescriptorSetLayoutCreateInfo</c>, 32 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetLayoutCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint BindingCount;
    public nint Bindings;
}

/// <summary>
/// <c>VkDescriptorPoolSize</c>, 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorPoolSize
{
    public uint Type;
    public uint DescriptorCount;
}

/// <summary>
/// <c>VkDescriptorPoolCreateInfo</c>, 40 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorPoolCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint MaxSets;
    public uint PoolSizeCount;
    public uint PoolSizePadding;
    public nint PoolSizes;
}

/// <summary>
/// <c>VkDescriptorSetAllocateInfo</c>, 40 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorSetAllocateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint DescriptorPool;
    public uint DescriptorSetCount;
    public uint DescriptorSetPadding;
    public nint SetLayouts;
}

/// <summary>
/// <c>VkDescriptorBufferInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDescriptorBufferInfo
{
    public nint Buffer;
    public ulong Offset;
    public ulong Range;
}

/// <summary>
/// <c>VkWriteDescriptorSet</c>, 64 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkWriteDescriptorSet
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint DescriptorSet;
    public uint DestinationBinding;
    public uint DestinationArrayElement;
    public uint DescriptorCount;
    public uint DescriptorType;
    public nint ImageInfo;
    public nint BufferInfo;
    public nint TexelBufferView;
}

#endif
