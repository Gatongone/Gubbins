using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The subset of the Vulkan API this bridge needs, loaded by hand so the package has no native dependency of its own.
/// </summary>
/// <remarks>
/// <para>
/// The bridge never creates a <c>VkInstance</c>, a <c>VkDevice</c> or a queue: the engine that already owns the graphics
/// device hands those handles in, and the bridge only allocates memory and binds buffers inside it. That is the shape a
/// Godot <c>RenderingDevice</c> or a Unity native plugin can supply.
/// </para>
/// <para>
/// The loader is a static class with function pointers rather than forty <see cref="DllImportAttribute"/> declarations,
/// because the library name differs per operating system and because a missing symbol has to be a clear failure at load
/// time instead of an <see cref="EntryPointNotFoundException"/> deep inside a frame.
/// </para>
/// <para>
/// Every structure mirrors the Vulkan headers on a 64-bit build, including the <c>pNext</c> slot. Their sizes and field
/// offsets are asserted by <c>VulkanInteropTests</c>, because a wrong layout is answered by the driver with an error code
/// or with garbage rather than with a crash in this code.
/// </para>
/// </remarks>
internal static unsafe partial class VulkanApi
{
    /// <summary>
    /// <c>VkStructureType</c> for <c>VkBufferCreateInfo</c>.
    /// </summary>
    internal const uint BUFFER_CREATE_INFO_TYPE = 12;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkSubmitInfo</c>.
    /// </summary>
    internal const uint SUBMIT_INFO_TYPE = 4;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkMemoryAllocateInfo</c>.
    /// </summary>
    internal const uint MEMORY_ALLOCATE_INFO_TYPE = 5;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkMappedMemoryRange</c>.
    /// </summary>
    internal const uint MAPPED_MEMORY_RANGE_TYPE = 6;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkFenceCreateInfo</c>.
    /// </summary>
    internal const uint FENCE_CREATE_INFO_TYPE = 8;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkBufferDeviceAddressInfo</c>.
    /// </summary>
    internal const uint BUFFER_DEVICE_ADDRESS_INFO_TYPE = 1000244001;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkCommandPoolCreateInfo</c>.
    /// </summary>
    internal const uint COMMAND_POOL_CREATE_INFO_TYPE = 39;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkCommandBufferAllocateInfo</c>.
    /// </summary>
    internal const uint COMMAND_BUFFER_ALLOCATE_INFO_TYPE = 40;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkCommandBufferBeginInfo</c>.
    /// </summary>
    internal const uint COMMAND_BUFFER_BEGIN_INFO_TYPE = 42;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkMemoryBarrier</c>.
    /// </summary>
    internal const uint MEMORY_BARRIER_TYPE = 46;

    /// <summary>
    /// <c>VK_BUFFER_USAGE_TRANSFER_SRC_BIT</c>.
    /// </summary>
    internal const uint BUFFER_USAGE_TRANSFER_SOURCE = 0x00000001;

    /// <summary>
    /// <c>VK_BUFFER_USAGE_TRANSFER_DST_BIT</c>.
    /// </summary>
    internal const uint BUFFER_USAGE_TRANSFER_DESTINATION = 0x00000002;

    /// <summary>
    /// <c>VK_BUFFER_USAGE_SHADER_DEVICE_ADDRESS_BIT</c>.
    /// </summary>
    internal const uint BUFFER_USAGE_SHADER_DEVICE_ADDRESS = 0x00020000;

    /// <summary>
    /// Everything a shared block may legitimately be bound as, except the device address bit, which needs a device
    /// feature.
    /// </summary>
    internal const uint BUFFER_USAGE_SHARED =
        BUFFER_USAGE_TRANSFER_SOURCE |
        BUFFER_USAGE_TRANSFER_DESTINATION |
        0x00000010 | // UNIFORM_BUFFER
        0x00000020 | // STORAGE_BUFFER
        0x00000040 | // INDEX_BUFFER
        0x00000080 | // VERTEX_BUFFER
        0x00000100;  // INDIRECT_BUFFER

    /// <summary>
    /// <c>VK_COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER_BIT</c>.
    /// </summary>
    internal const uint COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER = 0x00000002;

    /// <summary>
    /// <c>VK_COMMAND_BUFFER_LEVEL_PRIMARY</c>.
    /// </summary>
    internal const int COMMAND_BUFFER_LEVEL_PRIMARY = 0;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_TRANSFER_BIT</c>.
    /// </summary>
    internal const uint PIPELINE_STAGE_TRANSFER = 0x00001000;

    /// <summary>
    /// <c>VK_PIPELINE_STAGE_HOST_BIT</c>.
    /// </summary>
    internal const uint PIPELINE_STAGE_HOST = 0x00004000;

    /// <summary>
    /// <c>VK_ACCESS_TRANSFER_WRITE_BIT</c>.
    /// </summary>
    internal const uint ACCESS_TRANSFER_WRITE = 0x00001000;

    /// <summary>
    /// <c>VK_ACCESS_TRANSFER_READ_BIT</c>.
    /// </summary>
    internal const uint ACCESS_TRANSFER_READ = 0x00000800;

    /// <summary>
    /// <c>VK_ACCESS_HOST_READ_BIT</c>.
    /// </summary>
    internal const uint ACCESS_HOST_READ = 0x00002000;

    /// <summary>
    /// <c>VK_ACCESS_HOST_WRITE_BIT</c>.
    /// </summary>
    internal const uint ACCESS_HOST_WRITE = 0x00004000;

    /// <summary>
    /// <c>VK_SUCCESS</c>.
    /// </summary>
    internal const int SUCCESS = 0;

    /// <summary>
    /// <c>VK_WHOLE_SIZE</c>.
    /// </summary>
    internal const ulong WHOLE_SIZE = ulong.MaxValue;

    /// <summary>
    /// A timeout that lets <c>vkWaitForFences</c> wait indefinitely.
    /// </summary>
    internal const ulong INFINITE_TIMEOUT = ulong.MaxValue;

    /// <summary>
    /// The byte offset of <c>VkPhysicalDeviceMemoryProperties::memoryTypes</c>, asserted by the layout test.
    /// </summary>
    internal const int MEMORY_TYPES_OFFSET = 4;

    /// <summary>
    /// The byte offset of <c>VkPhysicalDeviceMemoryProperties::memoryHeaps</c>, asserted by the layout test.
    /// </summary>
    internal const int MEMORY_HEAPS_OFFSET = 264;

    /// <summary>
    /// Holds the loader handle behind its own type.
    /// </summary>
    /// <remarks>
    /// This type exists so that the handle is initialized on first use instead of in the field order of a partial class:
    /// <see cref="VulkanApi"/> is split over several files, and the initializers of a partial type run in an order the
    /// language does not define, so a field that the entry point table depends on cannot be another field of the same type.
    /// </remarks>
    private static class Loader
    {
        /// <summary>
        /// The Vulkan loader handle.
        /// </summary>
        internal static readonly nint Handle = LoadLibrary();
    }


    internal static readonly delegate* unmanaged[Cdecl]<nint, VkBufferCreateInfo*, nint, nint*, int> CreateBuffer =
        (delegate* unmanaged[Cdecl]<nint, VkBufferCreateInfo*, nint, nint*, int>)Export("vkCreateBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroyBuffer =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroyBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, VkMemoryRequirements*, void> GetBufferMemoryRequirements =
        (delegate* unmanaged[Cdecl]<nint, nint, VkMemoryRequirements*, void>)Export("vkGetBufferMemoryRequirements");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkMemoryAllocateInfo*, nint, nint*, int> AllocateMemory =
        (delegate* unmanaged[Cdecl]<nint, VkMemoryAllocateInfo*, nint, nint*, int>)Export("vkAllocateMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> FreeMemory =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkFreeMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, ulong, ulong, uint, void**, int> MapMemory =
        (delegate* unmanaged[Cdecl]<nint, nint, ulong, ulong, uint, void**, int>)Export("vkMapMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, void> UnmapMemory =
        (delegate* unmanaged[Cdecl]<nint, nint, void>)Export("vkUnmapMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, ulong, int> BindBufferMemory =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, ulong, int>)Export("vkBindBufferMemory");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, VkMappedMemoryRange*, int> FlushMappedMemoryRanges =
        (delegate* unmanaged[Cdecl]<nint, uint, VkMappedMemoryRange*, int>)Export("vkFlushMappedMemoryRanges");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, VkMappedMemoryRange*, int> InvalidateMappedMemoryRanges =
        (delegate* unmanaged[Cdecl]<nint, uint, VkMappedMemoryRange*, int>)Export("vkInvalidateMappedMemoryRanges");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkCommandPoolCreateInfo*, nint, nint*, int> CreateCommandPool =
        (delegate* unmanaged[Cdecl]<nint, VkCommandPoolCreateInfo*, nint, nint*, int>)Export("vkCreateCommandPool");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroyCommandPool =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroyCommandPool");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkCommandBufferAllocateInfo*, nint*, int> AllocateCommandBuffers =
        (delegate* unmanaged[Cdecl]<nint, VkCommandBufferAllocateInfo*, nint*, int>)Export("vkAllocateCommandBuffers");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, uint, int> ResetCommandPool =
        (delegate* unmanaged[Cdecl]<nint, nint, uint, int>)Export("vkResetCommandPool");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkCommandBufferBeginInfo*, int> BeginCommandBuffer =
        (delegate* unmanaged[Cdecl]<nint, VkCommandBufferBeginInfo*, int>)Export("vkBeginCommandBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, int> EndCommandBuffer =
        (delegate* unmanaged[Cdecl]<nint, int>)Export("vkEndCommandBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, uint, VkBufferCopy*, void> CmdCopyBuffer =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, uint, VkBufferCopy*, void>)Export("vkCmdCopyBuffer");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, uint, uint, uint, VkMemoryBarrier*, uint, nint, uint, nint, void> CmdPipelineBarrier =
        (delegate* unmanaged[Cdecl]<nint, uint, uint, uint, uint, VkMemoryBarrier*, uint, nint, uint, nint, void>)Export("vkCmdPipelineBarrier");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, VkSubmitInfo*, nint, int> QueueSubmit =
        (delegate* unmanaged[Cdecl]<nint, uint, VkSubmitInfo*, nint, int>)Export("vkQueueSubmit");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkFenceCreateInfo*, nint, nint*, int> CreateFence =
        (delegate* unmanaged[Cdecl]<nint, VkFenceCreateInfo*, nint, nint*, int>)Export("vkCreateFence");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, nint, void> DestroyFence =
        (delegate* unmanaged[Cdecl]<nint, nint, nint, void>)Export("vkDestroyFence");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, nint*, uint, ulong, int> WaitForFences =
        (delegate* unmanaged[Cdecl]<nint, uint, nint*, uint, ulong, int>)Export("vkWaitForFences");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, nint*, int> ResetFences =
        (delegate* unmanaged[Cdecl]<nint, uint, nint*, int>)Export("vkResetFences");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkBufferDeviceAddressInfo*, ulong> GetBufferDeviceAddress =
        (delegate* unmanaged[Cdecl]<nint, VkBufferDeviceAddressInfo*, ulong>)Export("vkGetBufferDeviceAddress");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceMemoryProperties*, void> GetPhysicalDeviceMemoryProperties =
        (delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceMemoryProperties*, void>)Export("vkGetPhysicalDeviceMemoryProperties");

    /// <summary>
    /// Reads the version the loader supports, which is what decides whether the 1.2 features are core or missing.
    /// </summary>
    /// <returns>The loader's API version, or 1.0 on a loader too old to report one.</returns>
    internal static uint InstanceVersion()
    {
        if (!NativeInterop.TryGetExport(Loader.Handle, "vkEnumerateInstanceVersion", out var address))
        {
            return 1u << 22;
        }

        uint version;

        return ((delegate* unmanaged[Cdecl]<uint*, int>) address)(&version) == SUCCESS ? version : 1u << 22;
    }

    /// <summary>
    /// Finds the Vulkan loader, whose file name differs per operating system.
    /// </summary>
    /// <returns>The library handle.</returns>
    /// <exception cref="DllNotFoundException">Thrown when no Vulkan loader is installed.</exception>
    private static nint LoadLibrary()
    {
        var names = NativeInterop.IsWindows
            ? ["vulkan-1.dll", "vulkan-1"]
            : NativeInterop.IsMacOS
                ? new[] { "libvulkan.1.dylib", "libvulkan.dylib", "libMoltenVK.dylib" }
                : new[] { "libvulkan.so.1", "libvulkan.so" };

        foreach (var name in names)
        {
            if (NativeInterop.TryLoad(name, out var handle))
            {
                return handle;
            }
        }

        throw new DllNotFoundException(
            "No Vulkan loader was found. Install the Vulkan runtime, or ship the loader next to the application. " +
            $"Tried: {string.Join(", ", names)}.");
    }

    /// <summary>
    /// Resolves an exported entry point.
    /// </summary>
    /// <param name="name">The Vulkan function name.</param>
    /// <returns>The entry point address.</returns>
    /// <exception cref="EntryPointNotFoundException">Thrown when the loader does not export the function.</exception>
    private static nint Export(string name)
    {
        if (NativeInterop.TryGetExport(Loader.Handle, name, out var address))
        {
            return address;
        }

        throw new EntryPointNotFoundException($"The Vulkan loader does not export {name}.");
    }

    /// <summary>
    /// Copies the memory types out of a <c>VkPhysicalDeviceMemoryProperties</c> into managed arrays that the selection
    /// logic can inspect.
    /// </summary>
    /// <param name="properties">The queried properties.</param>
    /// <returns>The managed view of the memory types and heaps.</returns>
    internal static VulkanMemoryTypeInfo Read(VkPhysicalDeviceMemoryProperties* properties)
    {
        // The offsets are the ones the layout test asserts, and they are used explicitly because a fixed size buffer
        // cannot be addressed through a struct pointer without going through byte arithmetic anyway.
        var types = (VkMemoryType*)((byte*)properties + MEMORY_TYPES_OFFSET);
        var heaps = (VkMemoryHeap*)((byte*)properties + MEMORY_HEAPS_OFFSET);

        var count = (int)Math.Min(properties->MemoryTypeCount, 32u);
        var flags = new uint[count];
        var heapIndexes = new uint[count];

        for (var i = 0; i < count; i++)
        {
            flags[i] = types[i].PropertyFlags;
            heapIndexes[i] = types[i].HeapIndex;
        }

        var heapCount = (int)Math.Min(properties->MemoryHeapCount, 16u);
        var heapSizes = new ulong[heapCount];

        for (var i = 0; i < heapCount; i++)
        {
            heapSizes[i] = heaps[i].Size;
        }

        return new VulkanMemoryTypeInfo(count, flags, heapIndexes, heapSizes);
    }
}

/// <summary>
/// <c>VkBufferCreateInfo</c>, 56 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public ulong Size;
    public uint Usage;
    public int SharingMode;
    public uint QueueFamilyIndexCount;
    public uint QueueFamilyPadding;
    public nint QueueFamilyIndexes;
}

/// <summary>
/// <c>VkMemoryRequirements</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryRequirements
{
    public ulong Size;
    public ulong Alignment;
    public uint MemoryTypeBits;
    public uint Padding;
}

/// <summary>
/// <c>VkMemoryAllocateInfo</c>, 32 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryAllocateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public ulong AllocationSize;
    public uint MemoryTypeIndex;
    public uint IndexPadding;
}

/// <summary>
/// <c>VkMappedMemoryRange</c>, 40 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMappedMemoryRange
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint Memory;
    public ulong Offset;
    public ulong Size;
}

/// <summary>
/// <c>VkMemoryType</c>, 8 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryType
{
    public uint PropertyFlags;
    public uint HeapIndex;
}

/// <summary>
/// <c>VkMemoryHeap</c>, 16 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryHeap
{
    public ulong Size;
    public uint Flags;
    public uint Padding;
}

/// <summary>
/// <c>VkPhysicalDeviceMemoryProperties</c>, 520 bytes: a type count, 32 types, a heap count and 16 heaps.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkPhysicalDeviceMemoryProperties
{
    public uint MemoryTypeCount;
    public fixed byte MemoryTypes[32 * 8];
    public uint MemoryHeapCount;
    public fixed byte MemoryHeaps[16 * 16];
}

/// <summary>
/// <c>VkBufferCopy</c>, 24 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferCopy
{
    public ulong SourceOffset;
    public ulong DestinationOffset;
    public ulong Size;
}

/// <summary>
/// <c>VkMemoryBarrier</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryBarrier
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint SourceAccessMask;
    public uint DestinationAccessMask;
}

/// <summary>
/// <c>VkCommandPoolCreateInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandPoolCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint QueueFamilyIndex;
}

/// <summary>
/// <c>VkCommandBufferAllocateInfo</c>, 32 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandBufferAllocateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint CommandPool;
    public int Level;
    public uint CommandBufferCount;
}

/// <summary>
/// <c>VkCommandBufferBeginInfo</c>, 32 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkCommandBufferBeginInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public nint InheritanceInfo;
}

/// <summary>
/// <c>VkSubmitInfo</c>, 72 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkSubmitInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint WaitSemaphoreCount;
    public uint WaitSemaphorePadding;
    public nint WaitSemaphores;
    public nint WaitDestinationStageMasks;
    public uint CommandBufferCount;
    public uint CommandBufferPadding;
    public nint CommandBuffers;
    public uint SignalSemaphoreCount;
    public uint SignalSemaphorePadding;
    public nint SignalSemaphores;
}

/// <summary>
/// <c>VkFenceCreateInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkFenceCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
}

/// <summary>
/// <c>VkBufferDeviceAddressInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkBufferDeviceAddressInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint Buffer;
}

