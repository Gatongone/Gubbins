using System.Runtime.CompilerServices;

namespace Gubbins.Graphic;

/// <summary>
/// The minimum size of the segments <see cref="UnifiedAllocator"/> acquires, per usage.
/// </summary>
/// <remarks>
/// <para>
/// There are two heaps worth sizing differently. A device-only heap wants few, large segments, because each one is a driver
/// allocation of its own (<c>vkAllocateMemory</c>, <c>CreateHeap</c>, an <c>MTLBuffer</c>) and the CPU never suballocates
/// inside it after the first frame. Shared memory, which the CPU allocates from and writes every frame, wants smaller ones,
/// so the waste at the end of a frame stays small.
/// </para>
/// <para>
/// The sizes are minimums, not caps: a single allocation larger than the configured size gets a segment of its own, exactly
/// as it would with a single segment size. A size of <see langword="0"/> means
/// <see cref="UnifiedAllocator.DEFAULT_SEGMENT_SIZE"/>.
/// </para>
/// </remarks>
public readonly struct UnifiedSegmentSizes
{
    /// <summary>
    /// The minimum segment size for <see cref="UnifiedMemoryUsage.General"/> blocks, which are the shared ones.
    /// </summary>
    public readonly nuint General;

    /// <summary>
    /// The minimum segment size for <see cref="UnifiedMemoryUsage.Device"/> blocks.
    /// </summary>
    public readonly nuint Device;

    /// <param name="general">The minimum segment size for <see cref="UnifiedMemoryUsage.General"/> blocks.</param>
    /// <param name="device">The minimum segment size for <see cref="UnifiedMemoryUsage.Device"/> blocks.</param>
    public UnifiedSegmentSizes(nuint general, nuint device)
    {
        General = general;
        Device = device;
    }

    /// <summary>
    /// Creates a policy that uses the same size for every usage.
    /// </summary>
    /// <param name="size">The segment size to use for every usage.</param>
    /// <returns>The uniform policy.</returns>
    public static UnifiedSegmentSizes Uniform(nuint size) => new(size, size);

    /// <summary>
    /// Gets the minimum segment size configured for a usage.
    /// </summary>
    /// <param name="usage">The usage to look up.</param>
    /// <returns>The configured size, which may be <see langword="0"/> when the usage was left to the default.</returns>
    public readonly nuint this[UnifiedMemoryUsage usage]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => usage == UnifiedMemoryUsage.Device ? Device : General;
    }

    /// <summary>
    /// Replaces every zero with a fallback size, which is how the allocator resolves
    /// "leave this usage at the default".
    /// </summary>
    /// <param name="fallback">The size to use for a usage whose configured size is zero.</param>
    /// <returns>The resolved policy.</returns>
    public readonly UnifiedSegmentSizes WithDefault(nuint fallback) => new(
        General == 0 ? fallback : General,
        Device == 0 ? fallback : Device);
}