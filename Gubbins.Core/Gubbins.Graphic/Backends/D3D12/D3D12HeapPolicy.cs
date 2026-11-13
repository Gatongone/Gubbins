#if NET8_0_OR_GREATER
using Gubbins.Graphic;

namespace Gubbins.Graphic;

/// <summary>
/// Decides which D3D12 heap serves a <see cref="UnifiedMemoryUsage"/>, and what sharing kind that heap implies.
/// </summary>
/// <remarks>
/// <para>
/// D3D12 has no memory type table like Vulkan: an allocation is an <c>ID3D12Heap</c> of a fixed kind, and the kind decides
/// everything about who can reach it.
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="UnifiedMemoryUsage.General"/> gets a custom heap in the L0 pool with write-back page properties and buffers-only
/// access. L0 is the memory the CPU itself runs out of, so the same heap is CPU cached and device visible: on a unified
/// memory adapter it is the machine's own DRAM, and on a discrete one it is system memory the device reaches over PCIe.
/// Either way one allocation serves both processors and the CPU reads it at memory speed, which is the property that makes
/// <see cref="UnifiedMemoryKind.SharedCoherent"/> an honest claim.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>D3D12_HEAP_TYPE_UPLOAD</c> is CPU writable and GPU readable, and it is write-combined: streaming writes are fine, but
/// the CPU reads a handful of bytes per transaction, which turns an ordinary sequential read of a result into a crawl. It is
/// only used when the driver refuses the custom heap, because it is then the only mappable memory left; the reported kind
/// still says the pages are coherent, since a fence is all the visibility either kind needs.
/// </description>
/// </item>
/// <item>
/// <description>
/// <c>D3D12_HEAP_TYPE_DEFAULT</c> is device local and the CPU cannot map it, which is exactly
/// <see cref="UnifiedMemoryKind.Device"/>.
/// </description>
/// </item>
/// </list>
/// </remarks>
internal static class D3D12HeapPolicy
{
    /// <summary>
    /// Chooses the heap for a usage.
    /// </summary>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <returns>The heap parameters to create, and the kind the allocator should report for it.</returns>
    internal static D3D12HeapChoice Choose(UnifiedMemoryUsage usage)
        => usage == UnifiedMemoryUsage.Device ? DeviceHeap() : SharedHeap();

    /// <summary>
    /// Gets the heap to use when the driver refuses the one <see cref="Choose"/> asked for.
    /// </summary>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <returns>The standard heap that always exists.</returns>
    /// <remarks>
    /// Only <see cref="UnifiedMemoryUsage.General"/> has one: a custom heap is an option a driver may decline, while the
    /// device-local default heap is the reason D3D12 exists and cannot be missing.
    /// </remarks>
    internal static D3D12HeapChoice FallbackFor(UnifiedMemoryUsage usage) => UploadHeap();

    /// <summary>
    /// Builds the shared choice: a custom heap in system memory with cached CPU access.
    /// </summary>
    /// <returns>The choice.</returns>
    private static D3D12HeapChoice SharedHeap() => new(
        new D3D12HeapProperties
        {
            Type = D3D12Api.HEAP_TYPE_CUSTOM,
            CpuPageProperty = D3D12Api.CPU_PAGE_WRITE_BACK,
            MemoryPoolPreference = D3D12Api.MEMORY_POOL_L0,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        },
        UnifiedMemoryKind.SharedCoherent,
        D3D12Api.RESOURCE_STATE_COMMON,
        true,
        D3D12Api.HEAP_FLAG_ALLOW_ONLY_BUFFERS,
        D3D12Api.RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS,
        true);

    /// <summary>
    /// Builds the upload heap choice, which is the fallback when the adapter has no shared heap.
    /// </summary>
    /// <returns>The choice.</returns>
    private static D3D12HeapChoice UploadHeap() => new(
        new D3D12HeapProperties
        {
            // A standard heap type requires the page property and the memory pool to stay UNKNOWN; only a custom heap may
            // choose them.
            Type = D3D12Api.HEAP_TYPE_UPLOAD,
            CpuPageProperty = D3D12Api.CPU_PAGE_UNKNOWN,
            MemoryPoolPreference = D3D12Api.MEMORY_POOL_UNKNOWN,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        },
        UnifiedMemoryKind.SharedCoherent,
        D3D12Api.RESOURCE_STATE_GENERIC_READ,
        true);

    /// <summary>
    /// Builds the device-only choice.
    /// </summary>
    /// <returns>The choice.</returns>
    private static D3D12HeapChoice DeviceHeap() => new(
        new D3D12HeapProperties
        {
            Type = D3D12Api.HEAP_TYPE_DEFAULT,
            CpuPageProperty = D3D12Api.CPU_PAGE_UNKNOWN,
            MemoryPoolPreference = D3D12Api.MEMORY_POOL_UNKNOWN,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        },
        UnifiedMemoryKind.Device,
        D3D12Api.RESOURCE_STATE_COMMON,
        false,
        0,
        D3D12Api.RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS);
}

/// <summary>
/// The parameters of one heap, and what they mean to the allocator.
/// </summary>
internal readonly struct D3D12HeapChoice
{
    /// <summary>
    /// The heap properties to create the heap with.
    /// </summary>
    internal readonly D3D12HeapProperties Properties;

    /// <summary>
    /// How the memory is shared between the CPU and the device.
    /// </summary>
    internal readonly UnifiedMemoryKind Kind;

    /// <summary>
    /// The resource state a placed buffer has to be created in for this heap.
    /// </summary>
    internal readonly int InitialState;

    /// <summary>
    /// Whether the CPU can map the resource placed in this heap.
    /// </summary>
    internal readonly bool Mapped;

    /// <summary>
    /// The <c>D3D12_HEAP_FLAGS</c> to create the heap with. A standard heap type rejects
    /// <c>D3D12_HEAP_FLAG_ALLOW_ONLY_BUFFERS</c>, which only a custom heap accepts.
    /// </summary>
    internal readonly int HeapFlags;

    /// <summary>
    /// The <c>D3D12_RESOURCE_FLAGS</c> to create the placed buffer with. An upload heap rejects
    /// <c>D3D12_RESOURCE_FLAG_ALLOW_UNORDERED_ACCESS</c>, because the GPU cannot write to it.
    /// </summary>
    internal readonly int ResourceFlags;

    /// <summary>
    /// Whether the driver is allowed to decline this heap, so a standard heap has to answer instead.
    /// </summary>
    internal readonly bool Optional;

    /// <param name="properties">The heap properties.</param>
    /// <param name="kind">How the memory is shared.</param>
    /// <param name="initialState">The resource state to create the placed buffer in.</param>
    /// <param name="mapped">Whether the CPU can map the resource.</param>
    /// <param name="heapFlags">The heap flags.</param>
    /// <param name="resourceFlags">The resource flags.</param>
    /// <param name="optional">Whether the driver may decline this heap.</param>
    internal D3D12HeapChoice(D3D12HeapProperties properties, UnifiedMemoryKind kind, int initialState, bool mapped, int heapFlags = 0, int resourceFlags = 0, bool optional = false)
    {
        Properties = properties;
        Kind = kind;
        InitialState = initialState;
        Mapped = mapped;
        HeapFlags = heapFlags;
        ResourceFlags = resourceFlags;
        Optional = optional;
    }
}

#endif
