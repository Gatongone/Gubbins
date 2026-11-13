using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// Builds the function-pointer table of a <see cref="UnifiedProvider"/> from a managed
/// <see cref="IUnifiedProvider"/>, so that a bridge implements the interface and nothing else.
/// </summary>
/// <remarks>
/// <para>
/// The table is static per provider type: it is built once, and every instance of that bridge points at it. The context
/// is a <see cref="GCHandle"/> to the bridge, which stays valid when the garbage collector moves it; the bridge keeps the
/// handle so it can release it on disposal.
/// </para>
/// <para>
/// The allocator stores the provider as a value, so the table cannot be a reference to this class: a
/// <c>delegate*</c> has no pointer to it, which is what lets a copied <see cref="UnifiedAllocator"/> keep working.
/// </para>
/// </remarks>
/// <typeparam name="T">The bridge type.</typeparam>
internal static unsafe class ProviderAdapter<T> where T : class, IUnifiedProvider
{
    /// <summary>
    /// The provider methods of <typeparamref name="T"/>, built once.
    /// </summary>
    private static readonly UnifiedProvider.VTable s_Methods = new(
        &Alloc,
        &Free,
        &Flush,
        &Invalidate,
        &Copy,
        &Submit,
        &Wait,
        &AlignmentOf,
        &MaxSegmentSizeOf,
        &WorkingSetLimitOf,
        &SetResidency);

    /// <summary>
    /// Builds the provider of a bridge instance.
    /// </summary>
    /// <param name="provider">The bridge to expose.</param>
    /// <param name="handle">The handle that keeps it alive, allocated here the first time the bridge is exposed.</param>
    /// <returns>The provider the allocator calls through.</returns>
    internal static UnifiedProvider Create(T provider, ref GCHandle handle)
    {
        if (!handle.IsAllocated)
        {
            handle = GCHandle.Alloc(provider);
        }

        return new UnifiedProvider((void*)GCHandle.ToIntPtr(handle), s_Methods);
    }

    /// <summary>
    /// Recovers the bridge from the provider's context pointer.
    /// </summary>
    /// <param name="context">The provider context, which holds the bridge's <see cref="GCHandle"/>.</param>
    /// <returns>The bridge instance.</returns>
    private static T Resolve(void* context) => (T)GCHandle.FromIntPtr((nint)context).Target!;

    /// <summary>
    /// Allocates a segment through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="size">The size of the segment.</param>
    /// <param name="alignment">The required base alignment.</param>
    /// <param name="usage">What the CPU does with the segment.</param>
    /// <param name="deviceHandle">When the method returns, the device handle of the segment.</param>
    /// <param name="deviceAddress">When the method returns, the device address of the segment.</param>
    /// <param name="kind">When the method returns, how the segment is shared.</param>
    /// <returns>The CPU address of the segment, or <see langword="null"/> when it has none.</returns>
    private static void* Alloc(void* context, nuint size, nuint alignment, UnifiedMemoryUsage usage, void** deviceHandle, ulong* deviceAddress, UnifiedMemoryKind* kind)
    {
        var result = Resolve(context).Alloc(size, alignment, usage, out var handle, out var address, out var segmentKind);

        *deviceHandle = handle;
        *deviceAddress = address;
        *kind = segmentKind;
        return result;
    }

    /// <summary>
    /// Releases a segment through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="segment">The CPU address of the segment, or <see langword="null"/> when it has none.</param>
    /// <param name="deviceHandle">The device handle of the segment.</param>
    /// <param name="size">The size of the segment.</param>
    private static void Free(void* context, void* segment, void* deviceHandle, nuint size)
        => Resolve(context).Free(segment, size, deviceHandle);

    /// <summary>
    /// Publishes CPU writes through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="ptr">The range to publish.</param>
    /// <param name="size">The size of the range.</param>
    private static void Flush(void* context, void* ptr, nuint size) => Resolve(context).Flush(ptr, size);

    /// <summary>
    /// Invalidates CPU caches through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="ptr">The range to invalidate.</param>
    /// <param name="size">The size of the range.</param>
    private static void Invalidate(void* context, void* ptr, nuint size) => Resolve(context).Invalidate(ptr, size);

    /// <summary>
    /// Copies between blocks through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="destination">The destination block.</param>
    /// <param name="destinationOffset">The offset inside it.</param>
    /// <param name="source">The source block.</param>
    /// <param name="sourceOffset">The offset inside it.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="defer">Whether the copy is left for a later submit.</param>
    /// <returns><see langword="true"/> if the copy was recorded or completed.</returns>
    private static bool Copy(void* context, UnifiedMemoryBlock* destination, nuint destinationOffset, UnifiedMemoryBlock* source, nuint sourceOffset, nuint size, bool defer)
        => Resolve(context).Copy(*destination, destinationOffset, *source, sourceOffset, size, defer);

    /// <summary>
    /// Submits the recorded copies through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns><see langword="true"/> if the copies were submitted.</returns>
    private static bool Submit(void* context) => Resolve(context).Submit();

    /// <summary>
    /// Waits for the submitted copies through the bridge.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns><see langword="true"/> if the copies completed.</returns>
    private static bool Wait(void* context) => Resolve(context).Wait();

    /// <summary>
    /// Reports the bridge's segment alignment.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns>The alignment in bytes.</returns>
    private static nuint AlignmentOf(void* context) => Resolve(context).Alignment;

    /// <summary>
    /// Reports the bridge's largest segment.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns>The largest segment in bytes.</returns>
    private static nuint MaxSegmentSizeOf(void* context) => Resolve(context).MaxSegmentSize;

    /// <summary>
    /// Reports the bridge's working set.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <returns>The working set in bytes.</returns>
    private static nuint WorkingSetLimitOf(void* context) => Resolve(context).WorkingSetLimit;

    /// <summary>
    /// Applies a residency request to the segment a context pointer belongs to.
    /// </summary>
    /// <param name="context">The provider context.</param>
    /// <param name="segmentHost">The CPU address of the segment, or <see langword="null"/> when it has none.</param>
    /// <param name="deviceHandle">The device handle that owns the segment.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend accepted the request.</returns>
    private static bool SetResidency(void* context, void* segmentHost, void* deviceHandle, UnifiedMemoryResidency residency)
    {
        // A residency request names a segment, which is the part of a block the backend keeps its own bookkeeping for, so
        // the block is built with the segment and nothing else.
        var segment = new UnifiedMemoryBlock(deviceHandle, (byte*)segmentHost, null, 0, 0, 0, 0, default);

        return Resolve(context).SetResidency(segment, residency);
    }
}
