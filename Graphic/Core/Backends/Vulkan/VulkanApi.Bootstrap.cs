using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// The part of the Vulkan API that only a harness or an engine bootstrap needs: creating an instance and a device so the
/// bridge has something to allocate from.
/// </summary>
/// <remarks>
/// The bridge itself never creates these, which is why this half of the interop lives apart from the allocator-facing
/// half. It is here so a backend check can run with one command instead of needing a renderer.
/// </remarks>
internal static unsafe partial class VulkanApi
{
    /// <summary>
    /// <c>VkStructureType</c> for <c>VkApplicationInfo</c>.
    /// </summary>
    internal const uint APPLICATION_INFO_TYPE = 0;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkInstanceCreateInfo</c>.
    /// </summary>
    internal const uint INSTANCE_CREATE_INFO_TYPE = 1;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkDeviceQueueCreateInfo</c>.
    /// </summary>
    internal const uint DEVICE_QUEUE_CREATE_INFO_TYPE = 2;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkDeviceCreateInfo</c>.
    /// </summary>
    internal const uint DEVICE_CREATE_INFO_TYPE = 3;

    /// <summary>
    /// <c>VK_QUEUE_GRAPHICS_BIT</c>.
    /// </summary>
    internal const uint QUEUE_GRAPHICS = 0x00000001;

    /// <summary>
    /// <c>VK_QUEUE_COMPUTE_BIT</c>.
    /// </summary>
    internal const uint QUEUE_COMPUTE = 0x00000002;

    /// <summary>
    /// <c>VK_QUEUE_TRANSFER_BIT</c>.
    /// </summary>
    internal const uint QUEUE_TRANSFER = 0x00000004;

    /// <summary>
    /// <c>VK_API_VERSION_1_1</c>.
    /// </summary>
    internal const uint API_VERSION11 = (1u << 22) | (1u << 12);

    /// <summary>
    /// <c>VK_API_VERSION_1_2</c>, which is the version buffer device addresses are core in.
    /// </summary>
    internal const uint API_VERSION12 = (1u << 22) | (2u << 12);

    internal static readonly delegate* unmanaged[Cdecl]<VkInstanceCreateInfo*, nint, nint*, int> CreateInstance =
        (delegate* unmanaged[Cdecl]<VkInstanceCreateInfo*, nint, nint*, int>)Export("vkCreateInstance");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, void> DestroyInstance =
        (delegate* unmanaged[Cdecl]<nint, nint, void>)Export("vkDestroyInstance");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint*, nint*, int> EnumeratePhysicalDevices =
        (delegate* unmanaged[Cdecl]<nint, uint*, nint*, int>)Export("vkEnumeratePhysicalDevices");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint*, VkQueueFamilyProperties*, void> GetPhysicalDeviceQueueFamilyProperties =
        (delegate* unmanaged[Cdecl]<nint, uint*, VkQueueFamilyProperties*, void>)Export("vkGetPhysicalDeviceQueueFamilyProperties");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkDeviceCreateInfo*, nint, nint*, int> CreateDevice =
        (delegate* unmanaged[Cdecl]<nint, VkDeviceCreateInfo*, nint, nint*, int>)Export("vkCreateDevice");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, void> DestroyDevice =
        (delegate* unmanaged[Cdecl]<nint, nint, void>)Export("vkDestroyDevice");

    internal static readonly delegate* unmanaged[Cdecl]<nint, uint, uint, nint*, void> GetDeviceQueue =
        (delegate* unmanaged[Cdecl]<nint, uint, uint, nint*, void>)Export("vkGetDeviceQueue");

    internal static readonly delegate* unmanaged[Cdecl]<nint, int> DeviceWaitIdle =
        (delegate* unmanaged[Cdecl]<nint, int>)Export("vkDeviceWaitIdle");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceFeatures2*, void> GetPhysicalDeviceFeatures2 =
        (delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceFeatures2*, void>)Export("vkGetPhysicalDeviceFeatures2");

    internal static readonly delegate* unmanaged[Cdecl]<nint, nint, uint*, VkExtensionProperties*, int> EnumerateDeviceExtensionProperties =
        (delegate* unmanaged[Cdecl]<nint, nint, uint*, VkExtensionProperties*, int>)Export("vkEnumerateDeviceExtensionProperties");

    internal static readonly delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceMemoryProperties2*, void> GetPhysicalDeviceMemoryProperties2 =
        (delegate* unmanaged[Cdecl]<nint, VkPhysicalDeviceMemoryProperties2*, void>)Export("vkGetPhysicalDeviceMemoryProperties2");

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkPhysicalDeviceFeatures2</c>.
    /// </summary>
    internal const uint FEATURES2_TYPE = 1000059000;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkPhysicalDeviceMemoryProperties2</c>.
    /// </summary>
    internal const uint MEMORY_PROPERTIES2_TYPE = 1000059006;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkMemoryAllocateFlagsInfo</c>.
    /// </summary>
    internal const uint MEMORY_ALLOCATE_FLAGS_INFO_TYPE = 1000060000;

    /// <summary>
    /// <c>VkStructureType</c> for <c>VkPhysicalDeviceBufferDeviceAddressFeatures</c>.
    /// </summary>
    internal const uint BUFFER_DEVICE_ADDRESS_FEATURES_TYPE = 1000257000;

    /// <summary>
    /// <c>VK_MEMORY_ALLOCATE_DEVICE_ADDRESS_BIT</c>, which every allocation behind a device address must carry.
    /// </summary>
    internal const uint MEMORY_ALLOCATE_DEVICE_ADDRESS = 0x00000002;

    /// <summary>
    /// The name of the extension that reports a budget per heap.
    /// </summary>
    internal const string MEMORY_BUDGET_EXTENSION = "VK_EXT_memory_budget";
}

/// <summary>
/// <c>VkApplicationInfo</c>, 48 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkApplicationInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public nint ApplicationName;
    public uint ApplicationVersion;
    public uint ApplicationVersionPadding;
    public nint EngineName;
    public uint EngineVersion;
    public uint ApiVersion;
}

/// <summary>
/// <c>VkInstanceCreateInfo</c>, 64 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkInstanceCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint FlagsPadding;
    public nint ApplicationInfo;
    public uint EnabledLayerCount;
    public uint EnabledLayerPadding;
    public nint EnabledLayerNames;
    public uint EnabledExtensionCount;
    public uint EnabledExtensionPadding;
    public nint EnabledExtensionNames;
}

/// <summary>
/// <c>VkExtent3D</c>, 12 bytes.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkExtent3D
{
    public uint Width;
    public uint Height;
    public uint Depth;
}

/// <summary>
/// <c>VkQueueFamilyProperties</c>, 24 bytes: three 32-bit fields followed by a 12 byte extent.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkQueueFamilyProperties
{
    public uint QueueFlags;
    public uint QueueCount;
    public uint TimestampValidBits;
    public VkExtent3D MinimumImageTransferGranularity;
}

/// <summary>
/// <c>VkDeviceQueueCreateInfo</c>, 40 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDeviceQueueCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint QueueFamilyIndex;
    public uint QueueCount;
    public uint QueueCountPadding;
    public nint QueuePriorities;
}

/// <summary>
/// <c>VkDeviceCreateInfo</c>, 72 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkDeviceCreateInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint QueueCreateInfoCount;
    public nint QueueCreateInfos;
    public uint EnabledLayerCount;
    public uint EnabledLayerPadding;
    public nint EnabledLayerNames;
    public uint EnabledExtensionCount;
    public uint EnabledExtensionPadding;
    public nint EnabledExtensionNames;
    public nint EnabledFeatures;
}

/// <summary>
/// <c>VkMemoryAllocateFlagsInfo</c>, 24 bytes on a 64-bit build.
/// </summary>
/// <remarks>
/// A buffer that reports a device address only has a defined address when its memory was allocated with
/// <c>VK_MEMORY_ALLOCATE_DEVICE_ADDRESS_BIT</c>, which this structure is how you ask for.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
internal struct VkMemoryAllocateFlagsInfo
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint Flags;
    public uint DeviceMask;
}

/// <summary>
/// <c>VkPhysicalDeviceBufferDeviceAddressFeatures</c>, 32 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPhysicalDeviceBufferDeviceAddressFeatures
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public uint BufferDeviceAddress;
    public uint BufferDeviceAddressCaptureReplay;
    public uint BufferDeviceAddressMultiDevice;
    public uint Padding;
}

/// <summary>
/// <c>VkPhysicalDeviceFeatures</c>, 220 bytes: fifty-five feature bits the bridge never inspects.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkPhysicalDeviceFeatures
{
    public fixed uint Bits[55];
}

/// <summary>
/// <c>VkPhysicalDeviceFeatures2</c>, 240 bytes on a 64-bit build, used only as the head of a feature query chain.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPhysicalDeviceFeatures2
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public VkPhysicalDeviceFeatures Features;
}

/// <summary>
/// <c>VkExtensionProperties</c>, 260 bytes: a 256 byte name and a version.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkExtensionProperties
{
    public fixed byte ExtensionName[256];
    public uint SpecificationVersion;
}

/// <summary>
/// <c>VkPhysicalDeviceMemoryProperties2</c>, 536 bytes on a 64-bit build.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct VkPhysicalDeviceMemoryProperties2
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public VkPhysicalDeviceMemoryProperties MemoryProperties;
}

/// <summary>
/// <c>VkPhysicalDeviceMemoryBudgetPropertiesEXT</c>, 272 bytes on a 64-bit build: a budget and a usage per heap.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct VkPhysicalDeviceMemoryBudgetPropertiesExt
{
    public uint StructureType;
    public uint StructurePadding;
    public nint Next;
    public fixed ulong HeapBudget[16];
    public fixed ulong HeapUsage[16];
}
