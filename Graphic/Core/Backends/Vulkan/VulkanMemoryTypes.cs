using Gubbins.Graphic;

namespace Gubbins.Graphic;

/// <summary>
/// The managed view of a <c>VkPhysicalDeviceMemoryProperties</c>, so that heap selection is testable without a device.
/// </summary>
internal readonly struct VulkanMemoryTypeInfo
{
    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT</c> value.
    /// </summary>
    internal const uint DEVICE_LOCAL = 0x00000001;

    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_HOST_VISIBLE_BIT</c> value.
    /// </summary>
    internal const uint HOST_VISIBLE = 0x00000002;

    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_HOST_COHERENT_BIT</c> value.
    /// </summary>
    internal const uint HOST_COHERENT = 0x00000004;

    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_HOST_CACHED_BIT</c> value, which is what makes CPU reads fast.
    /// </summary>
    internal const uint HOST_CACHED = 0x00000008;

    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_LAZILY_ALLOCATED_BIT</c> value, which cannot be bound as a plain buffer.
    /// </summary>
    internal const uint LAZILY_ALLOCATED = 0x00000010;

    /// <summary>
    /// The <c>VK_MEMORY_PROPERTY_PROTECTED_BIT</c> value, which needs device support this bridge does not negotiate.
    /// </summary>
    internal const uint PROTECTED = 0x00000020;

    private readonly uint[] m_Flags;
    private readonly uint[] m_HeapIndexes;
    private readonly ulong[] m_HeapSizes;

    /// <param name="typeCount">The number of memory types.</param>
    /// <param name="flags">The property flags of each memory type.</param>
    /// <param name="heapIndexes">The heap of each memory type.</param>
    /// <param name="heapSizes">The size of each heap, in bytes.</param>
    internal VulkanMemoryTypeInfo(int typeCount, uint[] flags, uint[] heapIndexes, ulong[] heapSizes)
    {
        TypeCount = typeCount;
        m_Flags = flags;
        m_HeapIndexes = heapIndexes;
        m_HeapSizes = heapSizes;
    }

    /// <summary>
    /// Gets the number of memory types.
    /// </summary>
    internal int TypeCount { get; }

    /// <summary>
    /// Gets the property flags of a memory type.
    /// </summary>
    /// <param name="type">The memory type index.</param>
    /// <returns>The <c>VkMemoryPropertyFlags</c> of that type.</returns>
    internal uint Flags(int type) => m_Flags[type];

    /// <summary>
    /// Gets the heap of a memory type.
    /// </summary>
    /// <param name="type">The memory type index.</param>
    /// <returns>The heap index of that type.</returns>
    internal uint HeapIndex(int type) => m_HeapIndexes[type];

    /// <summary>
    /// Gets the size of a heap, in bytes.
    /// </summary>
    /// <param name="heap">The heap index.</param>
    /// <returns>The heap size in bytes, or <see langword="0"/> when the index is out of range.</returns>
    internal ulong HeapSize(int heap) => heap >= 0 && heap < m_HeapSizes.Length ? m_HeapSizes[heap] : 0;

    /// <summary>
    /// Builds the info from a flat list of type flags, which keeps the tests short and readable.
    /// </summary>
    /// <param name="flags">The flags of each memory type, in index order.</param>
    /// <returns>The info over those types, all in one heap.</returns>
    internal static VulkanMemoryTypeInfo FromFlags(params uint[] flags)
    {
        var heapIndexes = new uint[flags.Length];

        return new VulkanMemoryTypeInfo(flags.Length, flags, heapIndexes, [8UL * 1024 * 1024 * 1024]);
    }
}

/// <summary>
/// Chooses the Vulkan memory type that serves a <see cref="UnifiedMemoryUsage"/>, and reports the sharing kind that type
/// implies.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole "which heap" decision of the Vulkan bridge, kept separate from the API calls so it can be tested
/// against synthetic memory properties instead of against a device. The rules follow what a driver actually exposes on
/// the three shapes of machine this allocator cares about:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// A unified memory SoC exposes <c>DEVICE_LOCAL | HOST_VISIBLE | HOST_COHERENT</c> system memory, which is the ideal
/// <see cref="UnifiedMemoryUsage.General"/> heap: zero copies and no cache maintenance.
/// </description>
/// </item>
/// <item>
/// <description>
/// A discrete GPU exposes a large <c>DEVICE_LOCAL</c> heap with no host bits, host-visible system memory that may or may
/// not be <c>HOST_CACHED</c>, and often <c>DEVICE_LOCAL | HOST_VISIBLE</c> BAR memory. The BAR window costs no copies,
/// but it is fast for the device and slow for the CPU, which pays an interconnect round trip per access, so it is not what
/// the CPU works in: on such a machine <see cref="UnifiedMemoryUsage.General"/> is system memory the device reaches over
/// the interconnect, and the reported <see cref="UnifiedMemoryKind"/> says exactly that.
/// </description>
/// </item>
/// <item>
/// <description>
/// A write-combined type is one without <c>HOST_CACHED</c>: cheap for the CPU to write, ruinous for it to read. It is
/// accepted for <see cref="UnifiedMemoryUsage.General"/> only after every cached host-visible type, because the shared
/// heap is the one the CPU reads as well as writes.
/// </description>
/// </item>
/// </list>
/// <para>
/// Lazily allocated and protected types are never selected: the first cannot be bound as a plain buffer, the second needs
/// device support this bridge does not negotiate.
/// </para>
/// </remarks>
internal static class VulkanMemoryTypes
{
    /// <summary>
    /// Score of a memory type that cannot serve the usage at all.
    /// </summary>
    internal const int UNUSABLE = int.MinValue;

    /// <summary>
    /// Picks the best memory type for <paramref name="usage"/> among the types a buffer reports as compatible.
    /// </summary>
    /// <param name="info">The memory properties of the physical device.</param>
    /// <param name="typeBits">The <c>memoryTypeBits</c> from <c>vkGetBufferMemoryRequirements</c>.</param>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <returns>The memory type index, or <c>-1</c> when no compatible type can serve the usage.</returns>
    internal static int Select(in VulkanMemoryTypeInfo info, uint typeBits, UnifiedMemoryUsage usage)
    {
        var best = -1;
        var bestScore = UNUSABLE;

        for (var type = 0; type < info.TypeCount; type++)
        {
            if ((typeBits & (1u << type)) == 0)
            {
                continue;
            }

            var score = Score(info.Flags(type), usage);
            if (score > bestScore)
            {
                best = type;
                bestScore = score;
            }
        }

        return best;
    }

    /// <summary>
    /// Scores a memory type for a usage, so that the highest score wins.
    /// </summary>
    /// <param name="flags">The <c>VkMemoryPropertyFlags</c> of the type.</param>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <returns>The score, or the sentinel that means the type cannot serve the usage.</returns>
    internal static int Score(uint flags, UnifiedMemoryUsage usage)
    {
        if ((flags & VulkanMemoryTypeInfo.LAZILY_ALLOCATED) != 0 || (flags & VulkanMemoryTypeInfo.PROTECTED) != 0)
        {
            return UNUSABLE;
        }

        var deviceLocal = (flags & VulkanMemoryTypeInfo.DEVICE_LOCAL) != 0;
        var hostVisible = (flags & VulkanMemoryTypeInfo.HOST_VISIBLE) != 0;
        var coherent = (flags & VulkanMemoryTypeInfo.HOST_COHERENT) != 0;
        var cached = (flags & VulkanMemoryTypeInfo.HOST_CACHED) != 0;

        switch (usage)
        {
            case UnifiedMemoryUsage.General:
                if (!hostVisible)
                {
                    return UNUSABLE;
                }

                // The CPU both reads and writes this heap, so cached access comes first: an uncached type costs it an
                // interconnect round trip per access instead of a memory one. Coherence is next, because it decides
                // whether the allocator has to maintain caches, and device locality only adds a bonus: on a unified memory
                // machine it comes with the cached type anyway, and on a discrete one it is the BAR window, which is fast
                // for the device and slow for the CPU.
                return (cached ? 100 : 0) + (coherent ? 40 : 0) + (deviceLocal ? 20 : 0);

            case UnifiedMemoryUsage.Device:
                if (!deviceLocal)
                {
                    return UNUSABLE;
                }

                // A CPU-invisible type is the point, but a host-visible device-local type is still the best memory the
                // machine has, and the caller can simply map it.
                return hostVisible ? 50 : 100;

            default:
                return UNUSABLE;
        }
    }

    /// <summary>
    /// Reports how a memory type is shared between the CPU and the device.
    /// </summary>
    /// <param name="flags">The <c>VkMemoryPropertyFlags</c> of the type.</param>
    /// <returns>The sharing kind the type implies.</returns>
    internal static UnifiedMemoryKind KindOf(uint flags)
    {
        var deviceLocal = (flags & VulkanMemoryTypeInfo.DEVICE_LOCAL) != 0;
        var hostVisible = (flags & VulkanMemoryTypeInfo.HOST_VISIBLE) != 0;

        if (hostVisible)
        {
            return (flags & VulkanMemoryTypeInfo.HOST_COHERENT) != 0
                ? UnifiedMemoryKind.SharedCoherent
                : UnifiedMemoryKind.Shared;
        }

        // Memory the CPU cannot map is only useful when it is device local; anything else is a host-side fallback.
        return deviceLocal ? UnifiedMemoryKind.Device : UnifiedMemoryKind.Host;
    }
}

