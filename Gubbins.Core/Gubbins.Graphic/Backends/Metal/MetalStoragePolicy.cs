#if NET8_0_OR_GREATER
namespace Gubbins.Graphic;

/// <summary>
/// Decides which Metal storage mode serves a <see cref="UnifiedMemoryUsage"/>, and what sharing kind that storage mode
/// implies.
/// </summary>
/// <remarks>
/// <para>
/// Metal has one memory model on every Mac: <c>MTLStorageModeShared</c> is memory both processors can address, and the
/// driver keeps it coherent, so nothing has to be flushed or invalidated. <c>MTLStorageModePrivate</c> is memory only the
/// GPU addresses, which is exactly <see cref="UnifiedMemoryKind.Device"/>.
/// </para>
/// <para>
/// <c>MTLStorageModeManaged</c> exists for the discrete Macs and is an optimisation: it trades the coherence the driver
/// maintains for explicit <c>didModifyRange:</c> and <c>synchronizeResource:</c> calls. Serving a coherent heap is both
/// simpler and always correct, so the bridge never asks for it.
/// </para>
/// <para>
/// There is nothing to choose beyond that: a shared block is one the CPU writes and reads, so it keeps the default cache
/// mode, and the write-combined mode is left to a caller that knows it only ever writes.
/// </para>
/// </remarks>
internal static class MetalStoragePolicy
{
    /// <summary>
    /// <c>MTLStorageModeShared</c>: both processors can address the memory, and the driver keeps it coherent.
    /// </summary>
    internal const int STORAGE_MODE_SHARED = 0;

    /// <summary>
    /// <c>MTLStorageModeManaged</c>: CPU and GPU both address it, and the CPU has to publish its writes explicitly.
    /// </summary>
    internal const int STORAGE_MODE_MANAGED = 1;

    /// <summary>
    /// <c>MTLStorageModePrivate</c>: only the GPU can address the memory.
    /// </summary>
    internal const int STORAGE_MODE_PRIVATE = 2;

    /// <summary>
    /// <c>MTLCPUCacheModeDefaultCache</c>.
    /// </summary>
    internal const int CPU_CACHE_MODE_DEFAULT = 0;

    /// <summary>
    /// <c>MTLCPUCacheModeWriteCombined</c>: best for memory the CPU writes and never reads.
    /// </summary>
    internal const int CPU_CACHE_MODE_WRITE_COMBINED = 1;

    /// <summary>
    /// Chooses the storage mode for a usage.
    /// </summary>
    /// <param name="usage">What the CPU does with the block.</param>
    /// <returns>The storage mode, the CPU cache mode and the sharing kind to report for it.</returns>
    internal static MetalStorageChoice Choose(UnifiedMemoryUsage usage) => usage switch
    {
        // Shared storage is the memory both processors address, and Metal keeps it coherent, so the CPU cache mode stays at
        // the default: the heap is written and read by the CPU, and write-combined memory is ruinous to read.
        UnifiedMemoryUsage.General => new MetalStorageChoice(STORAGE_MODE_SHARED, CPU_CACHE_MODE_DEFAULT, UnifiedMemoryKind.SharedCoherent),

        _ => new MetalStorageChoice(STORAGE_MODE_PRIVATE, CPU_CACHE_MODE_DEFAULT, UnifiedMemoryKind.Device)
    };
}

/// <summary>
/// What <see cref="MetalStoragePolicy"/> decided for one usage.
/// </summary>
/// <param name="StorageMode">The <c>MTLStorageMode</c> to create the buffer with.</param>
/// <param name="CpuCacheMode">The <c>MTLCPUCacheMode</c> to create the buffer with.</param>
/// <param name="Kind">How the memory is shared between the CPU and the device.</param>
internal readonly struct MetalStorageChoice(int storageMode, int cpuCacheMode, UnifiedMemoryKind kind)
{
    /// <summary>
    /// The <c>MTLStorageMode</c> to create the buffer with.
    /// </summary>
    internal readonly int StorageMode = storageMode;

    /// <summary>
    /// The <c>MTLCPUCacheMode</c> to create the buffer with.
    /// </summary>
    internal readonly int CpuCacheMode = cpuCacheMode;

    /// <summary>
    /// How the memory is shared between the CPU and the device.
    /// </summary>
    internal readonly UnifiedMemoryKind Kind = kind;

    /// <summary>
    /// Gets a value indicating whether the CPU can address the buffer this choice creates.
    /// </summary>
    internal bool IsMapped => StorageMode != MetalStoragePolicy.STORAGE_MODE_PRIVATE;
}

#endif
