#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using Gubbins.Graphic;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic.Vulkan;

/// <summary>
/// The smallest Vulkan device a bridge can run on: one instance, one physical device, one logical device with one queue
/// that can transfer, and the extensions that make a unified memory machine measurable.
/// </summary>
/// <remarks>
/// <para>
/// An engine that already has a device should hand its handles to <see cref="VulkanUnifiedProvider"/> instead. This type
/// is for the case where nothing above this library speaks Vulkan: a caller asks for a device, gets one that works, and
/// never has to name an API.
/// </para>
/// <para>
/// The bridge is disposed first and the device last. The bridge records its copies on a command pool of its own, so the
/// only thing that has to outlive it is the queue it submits to.
/// </para>
/// </remarks>
internal sealed unsafe class VulkanDevice : IDisposable
{
    private readonly nint m_Instance;
    private readonly nint m_PhysicalDevice;
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly uint m_QueueFamily;
    private readonly uint m_QueueFlags;
    private readonly bool m_BufferDeviceAddress;
    private readonly nuint m_WorkingSetLimit;
    private bool m_Disposed;

    private VulkanDevice(nint instance, nint physicalDevice, nint device, nint queue, uint queueFamily, uint queueFlags, bool bufferDeviceAddress, nuint workingSetLimit)
    {
        m_Instance = instance;
        m_PhysicalDevice = physicalDevice;
        m_Device = device;
        m_Queue = queue;
        m_QueueFamily = queueFamily;
        m_QueueFlags = queueFlags;
        m_BufferDeviceAddress = bufferDeviceAddress;
        m_WorkingSetLimit = workingSetLimit;
        Provider = new VulkanUnifiedProvider(
            physicalDevice,
            device,
            queue,
            queueFamily,
            bufferDeviceAddress: bufferDeviceAddress,
            workingSetLimit: workingSetLimit);
    }

    /// <summary>
    /// Gets the bridge that allocates on this device.
    /// </summary>
    public VulkanUnifiedProvider Provider { get; }

    /// <summary>
    /// Gets the <c>VkDevice</c> handle, for a caller that needs it for its own resources.
    /// </summary>
    public nint Handle => m_Device;

    /// <summary>
    /// Gets the <c>VkPhysicalDevice</c> the bridge reports its memory types from.
    /// </summary>
    public nint PhysicalDevice => m_PhysicalDevice;

    /// <summary>
    /// Gets the <c>VkQueue</c> the bridge submits its copies to.
    /// </summary>
    public nint Queue => m_Queue;

    /// <summary>
    /// Gets the family of <see cref="Queue"/>.
    /// </summary>
    public uint QueueFamily => m_QueueFamily;

    /// <summary>
    /// Gets the <c>VkQueueFlagBits</c> of the chosen family, which says whether it can also dispatch compute.
    /// </summary>
    public uint QueueFlags => m_QueueFlags;

    /// <summary>
    /// Gets a value indicating whether the device was created with buffer device addresses enabled.
    /// </summary>
    public bool BufferDeviceAddress => m_BufferDeviceAddress;

    /// <summary>
    /// Gets the budget the driver reports for its heaps, or <see langword="0"/> when the extension is not available.
    /// </summary>
    public nuint WorkingSetLimit => m_WorkingSetLimit;

    /// <summary>
    /// Creates a device on the first physical device the loader enumerates.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(out VulkanDevice? created) => TryCreate(out created, out _);

    /// <summary>
    /// Creates a device on the first physical device the loader enumerates.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <param name="failure">When the method returns, why creation failed, or <see langword="null"/> when it did not.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This asks for Vulkan 1.1 without optional instance extensions, because a bridge that only allocates and copies does
    /// not need a surface, a swapchain or a graphics pipeline.
    /// </remarks>
    public static bool TryCreate(out VulkanDevice? created, out string? failure)
    {
        created = null;
        failure = null;

        var applicationName = (byte*)NativeMemory.AllocZeroed(64);
        var engineName = (byte*)NativeMemory.AllocZeroed(64);
        nint instance = 0;
        nint device = 0;
        nint extensionName = 0;

        try
        {
            WriteAscii(applicationName, "Gubbins.Graphic");
            WriteAscii(engineName, "Gubbins.Graphic.Vulkan");

            var applicationInfo = new VkApplicationInfo
            {
                StructureType = VulkanApi.APPLICATION_INFO_TYPE,
                ApplicationName = (nint)applicationName,
                EngineName = (nint)engineName,
                ApplicationVersion = 1,
                EngineVersion = 1,
                ApiVersion = VulkanApi.API_VERSION11
            };

            var instanceInfo = new VkInstanceCreateInfo
            {
                StructureType = VulkanApi.INSTANCE_CREATE_INFO_TYPE,
                ApplicationInfo = (nint)(&applicationInfo)
            };

            var instanceResult = VulkanApi.CreateInstance(&instanceInfo, 0, &instance);
            if (instanceResult != VulkanApi.SUCCESS || instance == 0)
            {
                failure = $"vkCreateInstance failed with {instanceResult}.";
                return false;
            }

            var physicalDevice = FirstPhysicalDevice(instance);
            if (physicalDevice == 0)
            {
                failure = "No Vulkan device was found.";
                return false;
            }

            var family = ChooseQueueFamily(physicalDevice, out var queueFlags);
            if (family < 0)
            {
                failure = "The physical device exposes no queue family that can transfer.";
                return false;
            }

            var priority = (float*)NativeMemory.AllocZeroed((nuint)sizeof(float));
            *priority = 1.0f;

            var queueInfo = new VkDeviceQueueCreateInfo
            {
                StructureType = VulkanApi.DEVICE_QUEUE_CREATE_INFO_TYPE,
                QueueFamilyIndex = (uint)family,
                QueueCount = 1,
                QueuePriorities = (nint)priority
            };

            // A buffer device address has to be enabled at device creation and allocated with the matching flag, so it is
            // only turned on when the driver reports support.
            VkPhysicalDeviceBufferDeviceAddressFeatures deviceAddressFeatures = default;
            deviceAddressFeatures.StructureType = VulkanApi.BUFFER_DEVICE_ADDRESS_FEATURES_TYPE;

            var features = new VkPhysicalDeviceFeatures2
            {
                StructureType = VulkanApi.FEATURES2_TYPE,
                Next = (nint)(&deviceAddressFeatures)
            };

            VulkanApi.GetPhysicalDeviceFeatures2(physicalDevice, &features);

            var supportsDeviceAddress = deviceAddressFeatures.BufferDeviceAddress != 0;
            if (supportsDeviceAddress)
            {
                deviceAddressFeatures.BufferDeviceAddress = 1;
            }

            // The budget extension turns the working set from "not reported" into a real number, which is what a unified
            // memory machine wants sized against.
            var enabledExtensions = stackalloc nint[1];
            var enabledExtensionCount = TryFindExtension(physicalDevice, VulkanApi.MEMORY_BUDGET_EXTENSION, enabledExtensions, 1, out extensionName);

            var deviceInfo = new VkDeviceCreateInfo
            {
                StructureType = VulkanApi.DEVICE_CREATE_INFO_TYPE,
                QueueCreateInfoCount = 1,
                QueueCreateInfos = (nint)(&queueInfo),
                Next = supportsDeviceAddress ? (nint)(&deviceAddressFeatures) : 0,
                EnabledExtensionCount = (uint)enabledExtensionCount,
                EnabledExtensionNames = enabledExtensionCount == 0 ? 0 : (nint)enabledExtensions
            };

            var deviceResult = VulkanApi.CreateDevice(physicalDevice, &deviceInfo, 0, &device);
            NativeMemory.Free(priority);

            if (deviceResult != VulkanApi.SUCCESS || device == 0)
            {
                failure = $"vkCreateDevice failed with {deviceResult}.";
                device = 0;
                return false;
            }

            nint queue;
            VulkanApi.GetDeviceQueue(device, (uint)family, 0, &queue);

            var workingSetLimit = enabledExtensionCount != 0 &&
                VulkanUnifiedProvider.TryQueryWorkingSetLimit(physicalDevice, out var reported)
                ? reported
                : 0;

            created = new VulkanDevice(instance, physicalDevice, device, queue, (uint)family, queueFlags, supportsDeviceAddress, workingSetLimit);
            instance = 0;
            device = 0;
            return true;
        }
        finally
        {
            NativeMemory.Free(applicationName);
            NativeMemory.Free(engineName);

            if (extensionName != 0)
            {
                NativeMemory.Free((void*)extensionName);
            }

            if (device != 0)
            {
                VulkanApi.DestroyDevice(device, 0);
            }

            if (instance != 0)
            {
                VulkanApi.DestroyInstance(instance, 0);
            }
        }
    }

    /// <summary>
    /// Releases the bridge, then the device and the instance it was created from.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        Provider.Dispose();

        if (m_Device != 0)
        {
            // A submitted copy may still be reading a segment, so the device has to be drained before it is destroyed.
            VulkanApi.DeviceWaitIdle(m_Device);
            VulkanApi.DestroyDevice(m_Device, 0);
        }

        if (m_Instance != 0)
        {
            VulkanApi.DestroyInstance(m_Instance, 0);
        }
    }

    /// <summary>
    /// Takes the first physical device the loader reports.
    /// </summary>
    /// <param name="instance">The instance to enumerate.</param>
    /// <returns>The physical device, or <see langword="0"/> when there is none.</returns>
    private static nint FirstPhysicalDevice(nint instance)
    {
        uint count = 0;
        if (VulkanApi.EnumeratePhysicalDevices(instance, &count, null) != VulkanApi.SUCCESS || count == 0)
        {
            return 0;
        }

        var devices = (nint*)NativeMemory.AllocZeroed(count * (nuint)sizeof(nint));
        var result = VulkanApi.EnumeratePhysicalDevices(instance, &count, devices);
        var first = result == VulkanApi.SUCCESS && count != 0 ? devices[0] : 0;
        NativeMemory.Free(devices);
        return first;
    }

    /// <summary>
    /// Chooses a queue family that can transfer, preferring one that can also dispatch.
    /// </summary>
    /// <param name="physicalDevice">The device to ask.</param>
    /// <param name="flags">When the method returns, the flags of the chosen family.</param>
    /// <returns>The family index, or <c>-1</c> when the device has none that can transfer.</returns>
    private static int ChooseQueueFamily(nint physicalDevice, out uint flags)
    {
        flags = 0;

        uint count = 0;
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, null);
        if (count == 0)
        {
            return -1;
        }

        var families = (VkQueueFamilyProperties*)NativeMemory.AllocZeroed(count * (nuint)sizeof(VkQueueFamilyProperties));
        VulkanApi.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &count, families);

        var chosen = -1;

        for (var i = 0; i < count; i++)
        {
            var candidate = families[i].QueueFlags;

            if ((candidate & VulkanApi.QUEUE_TRANSFER) == 0)
            {
                continue;
            }

            // A queue that can also dispatch is what a compute caller wants, so it wins over a transfer-only family.
            if (chosen < 0 || (candidate & VulkanApi.QUEUE_COMPUTE) != 0)
            {
                chosen = i;
                flags = candidate;

                if ((candidate & VulkanApi.QUEUE_COMPUTE) != 0)
                {
                    break;
                }
            }
        }

        NativeMemory.Free(families);
        return chosen;
    }

    /// <summary>
    /// Looks up a device extension by name and copies its name into <paramref name="names"/> when it is supported.
    /// </summary>
    /// <param name="physicalDevice">The physical device to enumerate.</param>
    /// <param name="wanted">The extension name to look for.</param>
    /// <param name="names">Where the matching extension name is written.</param>
    /// <param name="capacity">How many names <paramref name="names"/> can hold.</param>
    /// <param name="owned">When the method returns, the buffer holding the copied name, to be freed by the caller.</param>
    /// <returns>The number of names written: one when the extension is supported, zero otherwise.</returns>
    private static int TryFindExtension(nint physicalDevice, string wanted, nint* names, int capacity, out nint owned)
    {
        owned = 0;

        uint count = 0;
        if (VulkanApi.EnumerateDeviceExtensionProperties(physicalDevice, 0, &count, null) != VulkanApi.SUCCESS || count == 0)
        {
            return 0;
        }

        var properties = (VkExtensionProperties*)NativeMemory.AllocZeroed(count * (nuint)sizeof(VkExtensionProperties));
        var result = VulkanApi.EnumerateDeviceExtensionProperties(physicalDevice, 0, &count, properties);
        if (result != VulkanApi.SUCCESS)
        {
            NativeMemory.Free(properties);
            return 0;
        }

        var found = false;

        for (var i = 0; i < count && !found; i++)
        {
            var match = true;

            for (var c = 0; c < wanted.Length; c++)
            {
                if (properties[i].ExtensionName[c] != (byte)wanted[c])
                {
                    match = false;
                    break;
                }
            }

            found = match;
        }

        NativeMemory.Free(properties);

        if (!found || capacity == 0)
        {
            return 0;
        }

        var storage = (byte*)NativeMemory.AllocZeroed(256);

        for (var c = 0; c < wanted.Length; c++)
        {
            storage[c] = (byte)wanted[c];
        }

        names[0] = (nint)storage;
        owned = (nint)storage;
        return 1;
    }

    /// <summary>
    /// Writes an ASCII string into a zeroed buffer.
    /// </summary>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="value">The string to write.</param>
    private static void WriteAscii(byte* destination, string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            destination[i] = (byte)value[i];
        }

        destination[value.Length] = 0;
    }
}

#endif
