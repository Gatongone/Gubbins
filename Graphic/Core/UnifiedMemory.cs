using System.Runtime.InteropServices;
using Gubbins.Unsafe;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// Describes how a range of memory is shared between the CPU and a graphics device.
/// </summary>
/// <remarks>
/// <para>
/// The kind is a property of the memory the graphics API gave us, never a choice the allocator can make on its own:
/// it follows from the backend's allocation flags (a Vulkan memory type, a D3D12 heap, a Metal storage mode, a CUDA
/// allocation) and from the hardware topology (a SoC with unified memory versus a discrete GPU with its own VRAM).
/// </para>
/// <para>
/// Only <see cref="SharedCoherent"/>, <see cref="Shared"/> and <see cref="Device"/> remove the copy. <see cref="Shared"/>
/// still needs the explicit cache maintenance that <see cref="UnifiedProvider.Flush"/> and
/// <see cref="UnifiedProvider.Invalidate"/> expose, because the device reads the same pages through a cache the CPU does
/// not write through. <see cref="Device"/> memory is not CPU addressable at all, so it removes the copy only for the
/// device: the CPU fills it through <see cref="UnifiedProvider.Copy"/> from a mapped block. None of them removes the
/// synchronization: a CPU write still has to be ordered before the device reads that range.
/// </para>
/// </remarks>
public enum UnifiedMemoryKind : byte
{
    /// <summary>
    /// The memory is only visible to the CPU. The device reaches the data through an explicit copy, for example a
    /// staging upload or a readback. This is the honest answer on a discrete GPU and the fallback when no backend
    /// bridge is installed.
    /// </summary>
    Host = 0,

    /// <summary>
    /// The same physical pages are visible to both processors, but they are not coherent: a CPU write is only observed
    /// by the device after <see cref="UnifiedProvider.Flush"/>, and a device write is only observed by the CPU after
    /// <see cref="UnifiedProvider.Invalidate"/>.
    /// </summary>
    Shared = 1,

    /// <summary>
    /// The same physical pages are visible to both processors and coherent in both directions, so no flush or
    /// invalidate is required.
    /// </summary>
    SharedCoherent = 2,

    /// <summary>
    /// The memory belongs to the device and the CPU cannot map it, so blocks in it have no CPU address. The block is
    /// still usable: it has a device handle, an offset and a device address, and the CPU fills it with
    /// <see cref="UnifiedProvider.Copy"/> from a mapped block.
    /// </summary>
    /// <remarks>
    /// This is Metal's <c>MTLStorageModePrivate</c>, a D3D12 <c>DEFAULT</c> heap, and the VRAM carve-out of an APU. It is
    /// not a compromise but the right heap for data the CPU never touches: vertex and index data that never changes,
    /// textures, and render targets.
    /// </remarks>
    Device = 3
}

/// <summary>
/// Describes what the CPU does with a block, so the provider can place it in the memory type that suits that use.
/// </summary>
/// <remarks>
/// <para>
/// On a unified memory architecture the same DRAM backs both processors, yet the heaps are not interchangeable. Apple
/// silicon exposes coherent shared pages whose buffer size is capped by <c>MTLDevice.maxBufferLength</c> and whose
/// granule is 16 KiB, plus <c>MTLStorageModePrivate</c> memory the CPU cannot see. An AMD APU exposes system memory the
/// GPU reaches through the GART, a BIOS-sized VRAM carve-out (the UMA frame buffer) that the CPU usually cannot map at
/// all, and write-combined upload memory whose CPU writes are cheap while its CPU reads are not. Intel and Qualcomm
/// SoCs add their own variants of the same heaps.
/// </para>
/// <para>
/// A provider that owns more than one heap therefore needs more than one answer, and the answer is per allocation: that
/// is why <see cref="UnifiedProvider.Alloc"/> takes a usage and reports the kind of the segment it actually chose. A
/// single-heap provider, such as <see cref="HostUnifiedProvider"/>, ignores the usage and keeps reporting its one kind.
/// </para>
/// </remarks>
public enum UnifiedMemoryUsage : byte
{
    /// <summary>
    /// The CPU and the device both read and write the block, for example the component arrays of an archetype that the
    /// simulation mutates and the renderer reads. Prefer coherent, CPU-cached shared memory.
    /// </summary>
    General = 0,

    /// <summary>
    /// The CPU never touches the block and the device reads and writes it, for example static vertex data, a texture, or
    /// a render target. Prefer the fastest device-local memory even when it is not CPU addressable; the CPU fills the
    /// block with <see cref="UnifiedProvider.Copy"/> if it has to fill it at all.
    /// </summary>
    Device = 1
}

/// <summary>
/// Describes whether the backend is allowed to reclaim the contents of a segment under memory pressure.
/// </summary>
public enum UnifiedMemoryResidency : byte
{
    /// <summary>
    /// The backend must keep the contents: the pages cannot be discarded or evicted behind the caller's back.
    /// </summary>
    Resident = 0,

    /// <summary>
    /// The backend may discard the contents to relieve memory pressure, for example a cache that can be rebuilt. It is
    /// the Metal purgeable state, the D3D12 eviction path, and the equivalent of a reclaimable allocation inside a
    /// unified memory pool.
    /// </summary>
    Discardable = 1
}

/// <summary>
/// Describes one block of memory as the graphics backend needs it: the CPU address when the block is mapped, the device
/// handle or device address of the containing segment, and the offset of the block inside that segment.
/// </summary>
/// <remarks>
/// A backend usually binds buffers at offsets inside one large allocation instead of allocating one memory object per
/// block. <c>vkBindBufferMemory</c> takes a <c>(memory, offset)</c> pair, <c>CreatePlacedResource</c> takes a heap plus
/// an offset, <c>setVertexBuffer</c> takes a Metal buffer plus an offset, and CUDA takes a plain address. Computing
/// <see cref="DeviceAddressOfBlock"/> on the device side is enough for the last case.
/// </remarks>
public readonly unsafe struct UnifiedMemorySegment
{
    /// <summary>
    /// The CPU address of the first byte of the containing segment, or <see langword="null"/> when the segment is
    /// device only.
    /// </summary>
    public readonly byte* Host;

    /// <summary>
    /// The size of the containing segment, in bytes.
    /// </summary>
    public readonly nuint Size;

    /// <summary>
    /// The offset of the block inside the segment, in bytes.
    /// </summary>
    public readonly nuint Offset;

    /// <summary>
    /// The opaque device handle that owns the segment, or <see langword="null"/> when the backend only exposes an
    /// address. It is the value a bridge passes back to the graphics API, such as a <c>VkDeviceMemory</c>,
    /// <c>ID3D12Heap</c>, <c>MTLBuffer</c>, or <c>CUdeviceptr</c>.
    /// </summary>
    public readonly void* DeviceHandle;

    /// <summary>
    /// The device address of the first byte of the segment, or <see langword="0"/> when the backend has no
    /// buffer-device-address support.
    /// </summary>
    public readonly ulong DeviceAddress;

    /// <summary>
    /// How the segment is shared between the CPU and the device. On a machine with more than one heap this can differ
    /// between segments of the same allocator.
    /// </summary>
    public readonly UnifiedMemoryKind Kind;

    /// <param name="host">The CPU address of the first byte of the segment, or <see langword="null"/> for a device-only segment.</param>
    /// <param name="size">The size of the segment, in bytes.</param>
    /// <param name="offset">The offset of the block inside the segment, in bytes.</param>
    /// <param name="deviceHandle">The opaque device handle that owns the segment.</param>
    /// <param name="deviceAddress">The device address of the first byte of the segment.</param>
    /// <param name="kind">How the segment is shared between the CPU and the device.</param>
    public UnifiedMemorySegment(byte* host, nuint size, nuint offset, void* deviceHandle, ulong deviceAddress, UnifiedMemoryKind kind)
    {
        Host = host;
        Size = size;
        Offset = offset;
        DeviceHandle = deviceHandle;
        DeviceAddress = deviceAddress;
        Kind = kind;
    }

    /// <summary>
    /// Gets the CPU address of the block, or <see langword="null"/> when the segment is device only.
    /// </summary>
    public byte* Block => Host == null ? null : Host + Offset;

    /// <summary>
    /// Gets the number of bytes still available after <see cref="Offset"/>.
    /// </summary>
    public nuint Tail => Size - Offset;

    /// <summary>
    /// Gets the device address of the block, or <see langword="0"/> when the backend has no buffer-device-address
    /// support.
    /// </summary>
    public ulong DeviceAddressOfBlock => DeviceAddress == 0 ? 0 : DeviceAddress + Offset;

    /// <summary>
    /// Gets a value indicating whether the CPU can address the block.
    /// </summary>
    public bool IsMapped => Host != null;

    /// <summary>
    /// Gets a value indicating whether the CPU and the device can reach the same pages without copying.
    /// </summary>
    public bool IsShared => Kind is UnifiedMemoryKind.Shared or UnifiedMemoryKind.SharedCoherent;

    /// <summary>
    /// Gets a value indicating whether the CPU and the device share the pages without needing cache maintenance.
    /// </summary>
    public bool IsCoherent => Kind == UnifiedMemoryKind.SharedCoherent;
}

/// <summary>
/// Describes an allocated block of unified memory: its CPU address when it is mapped, its length, and everything a
/// backend needs to bind a device buffer to it.
/// </summary>
/// <remarks>
/// This is the value <see cref="UnifiedAllocator.AllocBlock"/> returns, and the value the provider's copy operation takes,
/// because a device-only block cannot be described by a pointer: it is addressed by its segment handle plus
/// <see cref="Offset"/>.
/// </remarks>
public readonly unsafe struct UnifiedMemoryBlock
{
    /// <summary>
    /// The opaque device handle that owns the segment the block lives in.
    /// </summary>
    public readonly void* DeviceHandle;

    /// <summary>
    /// The CPU address of the segment the block lives in, or <see langword="null"/> when the segment is device only.
    /// </summary>
    public readonly byte* SegmentHost;

    /// <summary>
    /// The CPU address of the block, or <see langword="null"/> when the block is not CPU addressable.
    /// </summary>
    public readonly byte* Host;

    /// <summary>
    /// The device address of the block, or <see langword="0"/> when the backend has no buffer-device-address support.
    /// </summary>
    public readonly ulong DeviceAddress;

    /// <summary>
    /// The offset of the block inside its segment, in bytes.
    /// </summary>
    public readonly nuint Offset;

    /// <summary>
    /// The size of the block, in bytes.
    /// </summary>
    public readonly nuint Length;

    /// <summary>
    /// The size of the segment the block lives in, in bytes.
    /// </summary>
    public readonly nuint SegmentSize;

    /// <summary>
    /// How the segment is shared between the CPU and the device.
    /// </summary>
    public readonly UnifiedMemoryKind Kind;

    /// <param name="deviceHandle">The opaque device handle that owns the segment.</param>
    /// <param name="segmentHost">The CPU address of the segment, or <see langword="null"/> for a device-only segment.</param>
    /// <param name="host">The CPU address of the block, or <see langword="null"/> when it is not CPU addressable.</param>
    /// <param name="deviceAddress">The device address of the block, or <see langword="0"/>.</param>
    /// <param name="offset">The offset of the block inside its segment, in bytes.</param>
    /// <param name="length">The size of the block, in bytes.</param>
    /// <param name="segmentSize">The size of the segment, in bytes.</param>
    /// <param name="kind">How the segment is shared between the CPU and the device.</param>
    public UnifiedMemoryBlock(void* deviceHandle, byte* segmentHost, byte* host, ulong deviceAddress, nuint offset, nuint length, nuint segmentSize, UnifiedMemoryKind kind)
    {
        DeviceHandle = deviceHandle;
        SegmentHost = segmentHost;
        Host = host;
        DeviceAddress = deviceAddress;
        Offset = offset;
        Length = length;
        SegmentSize = segmentSize;
        Kind = kind;
    }

    /// <summary>
    /// Gets a value indicating whether the CPU can address the block.
    /// </summary>
    public bool IsMapped => Host != null;

    /// <summary>
    /// Gets a value indicating whether the CPU and the device can reach the same pages without copying.
    /// </summary>
    public bool IsShared => Kind is UnifiedMemoryKind.Shared or UnifiedMemoryKind.SharedCoherent;

    /// <summary>
    /// Gets a value indicating whether the CPU and the device share the pages without needing cache maintenance.
    /// </summary>
    public bool IsCoherent => Kind == UnifiedMemoryKind.SharedCoherent;

    /// <summary>
    /// Gets a value indicating whether the block is device local, so filling it takes a copy on the device rather than a
    /// CPU write.
    /// </summary>
    public bool IsDeviceLocal => Kind == UnifiedMemoryKind.Device;
}

/// <summary>
/// The set of operations a graphics backend has to provide so that <see cref="UnifiedAllocator"/> can hand out memory
/// the device reads and writes directly.
/// </summary>
/// <remarks>
/// <para>
/// A plain heap allocator cannot make memory device visible on its own: the graphics API owns the pages, or it owns the
/// mapping of the pages into the device's address space. This vtable is the seam where that ownership lives, so it is
/// expressed with function pointers and an opaque context, like <see cref="Allocator"/>.
/// </para>
/// <para>
/// The pointer returned by <see cref="Alloc"/> is the CPU address, or <see langword="null"/> when the backend served the
/// request from device-only memory; the kind it reports says which case the caller got. A unified memory type (Vulkan
/// <c>DEVICE_LOCAL | HOST_VISIBLE</c>, D3D12 custom <c>L0</c> heap, Metal <c>MTLStorageModeShared</c>, CUDA mapped host
/// memory) yields the same physical pages, a copy-based backend returns <see cref="UnifiedMemoryKind.Host"/> and forces
/// the caller to upload explicitly, and a device-only heap returns <see cref="UnifiedMemoryKind.Device"/> and expects the
/// caller to fill the block with <see cref="Copy"/>.
/// </para>
/// <para>
/// The default is <see cref="Host"/>, and <see cref="UnifiedAllocator"/> degrades to an ordinary bump arena in that
/// case, so a caller can adopt the API before any backend bridge exists.
/// </para>
/// </remarks>
public readonly unsafe struct UnifiedProvider(void* context, UnifiedProvider.VTable methods)
{
    /// <summary>
    /// The opaque pointer passed as the first argument to every provider method, used to hold the provider's state.
    /// </summary>
    public readonly void* Context = context;

    /// <summary>
    /// The virtual table that implements the provider operations.
    /// </summary>
    public readonly VTable Methods = methods;

    /// <summary>
    /// Holds the managed function pointers that implement the provider operations.
    /// Every function receives <see cref="Context"/> as its first argument.
    /// </summary>
    public readonly struct VTable
    {
        /// <summary>
        /// Acquires a segment of <paramref name="size"/> bytes aligned to <paramref name="alignment"/> for
        /// <paramref name="usage"/>, and reports the device handle, the device address and the sharing kind of that
        /// segment. Returns the CPU address of the segment, or <see langword="null"/> when the segment is device only or
        /// the backend cannot serve the request.
        /// </summary>
        /// <remarks>
        /// The size is already a multiple of the alignment, so a backend that requires page-granular segments (Vulkan's
        /// <c>VK_EXT_external_memory_host</c> requires a page-aligned base address <em>and</em> size) can allocate
        /// exactly what it was asked for. A backend with several heaps chooses among them here, for example a
        /// coherent one the CPU can map for <see cref="UnifiedMemoryUsage.General"/> and a CPU-invisible one for
        /// <see cref="UnifiedMemoryUsage.Device"/>. A <see langword="null"/> return with
        /// <see cref="UnifiedMemoryKind.Device"/> reported is a success; a <see langword="null"/> return with any other
        /// kind is a failure.
        /// </remarks>
        public readonly delegate* managed<void*, nuint, nuint, UnifiedMemoryUsage, void**, ulong*, UnifiedMemoryKind*, void*> Alloc;

        /// <summary>
        /// Releases a segment previously returned by <see cref="Alloc"/>. A device-only segment has no CPU address, so
        /// the segment is identified by <paramref name="deviceHandle"/> in that case.
        /// </summary>
        public readonly delegate* managed<void*, void*, void*, nuint, void> Free;

        /// <summary>
        /// Makes CPU writes in the range visible to the device. No-op for coherent and device-only memory.
        /// </summary>
        public readonly delegate* managed<void*, void*, nuint, void> Flush;

        /// <summary>
        /// Makes device writes in the range visible to the CPU. No-op for coherent and device-only memory.
        /// </summary>
        public readonly delegate* managed<void*, void*, nuint, void> Invalidate;

        /// <summary>
        /// Copies <paramref name="size"/> bytes between two blocks, from <paramref name="sourceOffset"/> inside the
        /// source to <paramref name="destinationOffset"/> inside the destination. Returns <see langword="false"/> when the
        /// backend cannot express that copy.
        /// </summary>
        /// <remarks>
        /// This is the only way to fill or read a <see cref="UnifiedMemoryKind.Device"/> block, and the fast path between
        /// two mapped heaps that the CPU should not bounce through. When <paramref name="defer"/> is false the backend
        /// submits the copy and waits for it, so the copy has completed when the method returns. When it is true the
        /// backend only records the copy, and <see cref="Submit"/> and <see cref="Wait"/> drive it: that is how a frame's
        /// worth of uploads is recorded on the CPU thread and then submitted once.
        /// </remarks>
        public readonly delegate* managed<void*, UnifiedMemoryBlock*, nuint, UnifiedMemoryBlock*, nuint, nuint, bool, bool> Copy;

        /// <summary>
        /// Submits the copies recorded with <see cref="Copy"/> and <c>defer</c> set to <see langword="true"/>. Returns
        /// <see langword="false"/> when the backend has nothing to submit.
        /// </summary>
        public readonly delegate* managed<void*, bool> Submit;

        /// <summary>
        /// Blocks until the copies submitted by <see cref="Submit"/> have completed. Returns <see langword="false"/> when
        /// the backend has no fence to wait on.
        /// </summary>
        public readonly delegate* managed<void*, bool> Wait;

        /// <summary>
        /// Reports the alignment the backend requires for segment base addresses and sizes, in bytes.
        /// </summary>
        public readonly delegate* managed<void*, nuint> Alignment;

        /// <summary>
        /// Reports the largest segment the backend can create, in bytes, or <see langword="0"/> when it has no limit.
        /// </summary>
        /// <remarks>
        /// This is not a formality: Metal caps a single <c>MTLBuffer</c> at <c>MTLDevice.maxBufferLength</c>, which is a
        /// few hundred megabytes on several families, and Vulkan caps a single allocation at
        /// <c>VkPhysicalDeviceLimits.maxMemoryAllocationSize</c>.
        /// </remarks>
        public readonly delegate* managed<void*, nuint> MaxSegmentSize;

        /// <summary>
        /// Reports the working set the backend recommends, in bytes, or <see langword="0"/> when it has no opinion.
        /// </summary>
        /// <remarks>
        /// Metal answers <c>MTLDevice.recommendedMaxWorkingSetSize</c> here, which is the number a unified memory SoC
        /// wants respected: going past it means competing with the operating system for the same DRAM. Vulkan answers
        /// the relevant budget from <c>VK_EXT_memory_budget</c>.
        /// </remarks>
        public readonly delegate* managed<void*, nuint> WorkingSetLimit;

        /// <summary>
        /// Asks the backend to make a segment resident or discardable. Returns <see langword="false"/> when the backend
        /// cannot express that state.
        /// </summary>
        /// <remarks>
        /// The segment is identified by <paramref name="segmentHost"/> when it is mapped and by
        /// <paramref name="deviceHandle"/> when it is not, so a backend may use either one to look it up.
        /// </remarks>
        public readonly delegate* managed<void*, void*, void*, UnifiedMemoryResidency, bool> SetResidency;

        /// <summary>
        /// Initializes a new virtual table with the given provider method implementations.
        /// </summary>
        /// <param name="alloc">The function pointer that acquires a segment.</param>
        /// <param name="free">The function pointer that releases a segment.</param>
        /// <param name="flush">The function pointer that publishes CPU writes to the device.</param>
        /// <param name="invalidate">The function pointer that publishes device writes to the CPU.</param>
        /// <param name="copy">The function pointer that copies between two blocks, or records the copy when asked to defer.</param>
        /// <param name="submit">The function pointer that submits the recorded copies.</param>
        /// <param name="wait">The function pointer that waits for the submitted copies.</param>
        /// <param name="alignment">The function pointer that reports the required segment alignment.</param>
        /// <param name="maxSegmentSize">The function pointer that reports the largest segment the backend can create.</param>
        /// <param name="workingSetLimit">The function pointer that reports the recommended working set.</param>
        /// <param name="setResidency">The function pointer that makes a segment resident or discardable.</param>
        public VTable(
            delegate* managed<void*, nuint, nuint, UnifiedMemoryUsage, void**, ulong*, UnifiedMemoryKind*, void*> alloc,
            delegate* managed<void*, void*, void*, nuint, void> free,
            delegate* managed<void*, void*, nuint, void> flush,
            delegate* managed<void*, void*, nuint, void> invalidate,
            delegate* managed<void*, UnifiedMemoryBlock*, nuint, UnifiedMemoryBlock*, nuint, nuint, bool, bool> copy,
            delegate* managed<void*, bool> submit,
            delegate* managed<void*, bool> wait,
            delegate* managed<void*, nuint> alignment,
            delegate* managed<void*, nuint> maxSegmentSize,
            delegate* managed<void*, nuint> workingSetLimit,
            delegate* managed<void*, void*, void*, UnifiedMemoryResidency, bool> setResidency)
        {
            Alloc = alloc;
            Free = free;
            Flush = flush;
            Invalidate = invalidate;
            Copy = copy;
            Submit = submit;
            Wait = wait;
            Alignment = alignment;
            MaxSegmentSize = maxSegmentSize;
            WorkingSetLimit = workingSetLimit;
            SetResidency = setResidency;
        }
    }

    /// <summary>
    /// A provider that serves ordinary page-aligned host memory and reports <see cref="UnifiedMemoryKind.Host"/>.
    /// It is the fallback used when no backend bridge is installed.
    /// </summary>
    /// <remarks>
    /// The implementation lives in unmanaged storage rather than in a static field, so the context pointer it reports can
    /// never be invalidated by anything the runtime does with static blocks. One provider is enough for the process, so
    /// the block is deliberately never released.
    /// </remarks>
    public static readonly UnifiedProvider Host = CreateHost();

    /// <summary>
    /// Copies a default <see cref="HostUnifiedProvider"/> into unmanaged storage and returns the provider over it.
    /// </summary>
    /// <returns>The process-wide host provider.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when the unmanaged storage cannot be allocated.</exception>
    private static UnifiedProvider CreateHost()
    {
        var storage = (HostUnifiedProvider*)UnmanagedAllocator.Global.Alloc((nuint)sizeof(HostUnifiedProvider), Native.GetAlignment((int)sizeof(HostUnifiedProvider)));
        if (storage == null)
        {
            throw new OutOfMemoryException($"Unable to allocate {sizeof(HostUnifiedProvider)} bytes for the host unified provider.");
        }

        *storage = new HostUnifiedProvider();
        return storage->GetProvider();
    }

    /// <summary>
    /// Gets a value indicating whether this instance wraps a usable virtual table.
    /// A default-initialized <see cref="UnifiedProvider"/> has no vtable and reports a zero alignment with no segment
    /// size limit.
    /// </summary>
    public bool IsValid
    {
        get => Methods.Alloc != null;
    }

    /// <summary>
    /// Gets the alignment the provider requires for segment base addresses and sizes, in bytes.
    /// </summary>
    public nuint Alignment => Methods.Alignment != null ? Methods.Alignment(Context) : 0;

    /// <summary>
    /// Gets the largest segment the backend can create, in bytes, or <see langword="0"/> when it has no limit.
    /// </summary>
    public nuint MaxSegmentSize => Methods.MaxSegmentSize != null ? Methods.MaxSegmentSize(Context) : 0;

    /// <summary>
    /// Gets the working set the backend recommends, in bytes, or <see langword="0"/> when it has no opinion.
    /// </summary>
    public nuint WorkingSetLimit => Methods.WorkingSetLimit != null ? Methods.WorkingSetLimit(Context) : 0;

    /// <summary>
    /// Acquires a segment of <paramref name="size"/> bytes aligned to <paramref name="alignment"/> for
    /// <paramref name="usage"/>.
    /// </summary>
    /// <param name="size">The size of the segment in bytes, already a multiple of the alignment.</param>
    /// <param name="alignment">The required base alignment in bytes; a power of two.</param>
    /// <param name="usage">What the CPU does with the segment, which selects the heap on a multi-heap backend.</param>
    /// <param name="deviceHandle">When the method returns, the opaque device handle that owns the segment.</param>
    /// <param name="deviceAddress">When the method returns, the device address of the segment, or <see langword="0"/>.</param>
    /// <param name="kind">When the method returns, how the chosen segment is shared between the CPU and the device.</param>
    /// <returns>The CPU address of the segment, or <see langword="null"/> for a device-only segment.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider has no vtable.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when the backend cannot serve the request.</exception>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        if (Methods.Alloc == null)
        {
            throw new InvalidOperationException("The unified provider has no vtable.");
        }

        ulong address;
        void* handle;
        UnifiedMemoryKind segmentKind;
        var segment = Methods.Alloc(Context, size, alignment, usage, &handle, &address, &segmentKind);

        // A device-only segment legitimately has no CPU address; anything else without one is a failure.
        if (segment == null && segmentKind != UnifiedMemoryKind.Device)
        {
            throw new OutOfMemoryException($"The unified provider cannot serve a segment of {size} bytes for {usage} usage.");
        }

        deviceHandle = handle;
        deviceAddress = address;
        kind = segmentKind;
        return segment;
    }

    /// <summary>
    /// Releases a segment previously returned by <see cref="Alloc"/>.
    /// </summary>
    /// <param name="segment">The CPU address of the segment, or <see langword="null"/> for a device-only segment.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    /// <param name="deviceHandle">The device handle of the segment, which identifies a device-only segment.</param>
    public void Free(void* segment, nuint size, void* deviceHandle)
    {
        if (Methods.Free != null && (segment != null || deviceHandle != null))
        {
            Methods.Free(Context, segment, deviceHandle, size);
        }
    }

    /// <summary>
    /// Makes CPU writes in the range visible to the device.
    /// </summary>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    public void Flush(void* ptr, nuint size)
    {
        if (Methods.Flush != null && ptr != null)
        {
            Methods.Flush(Context, ptr, size);
        }
    }

    /// <summary>
    /// Makes device writes in the range visible to the CPU.
    /// </summary>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    public void Invalidate(void* ptr, nuint size)
    {
        if (Methods.Invalidate != null && ptr != null)
        {
            Methods.Invalidate(Context, ptr, size);
        }
    }

    /// <summary>
    /// Copies <paramref name="size"/> bytes between two blocks, from <paramref name="sourceOffset"/> inside the source to
    /// <paramref name="destinationOffset"/> inside the destination.
    /// </summary>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="defer">
    /// When <see langword="true"/>, the backend only records the copy and returns; <see cref="Submit"/> and
    /// <see cref="Wait"/> drive it. Otherwise the copy has completed when the method returns.
    /// </param>
    /// <returns><see langword="true"/> if the backend performed or recorded the copy; otherwise, <see langword="false"/>.</returns>
    public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer = false)
    {
        if (Methods.Copy == null)
        {
            return false;
        }

        // The vtable takes writable pointers, so the blocks are copied into locals that can be addressed.
        var destinationBlock = destination;
        var sourceBlock = source;
        return Methods.Copy(Context, &destinationBlock, destinationOffset, &sourceBlock, sourceOffset, size, defer);
    }

    /// <summary>
    /// Submits the copies recorded with <see cref="Copy"/> and <c>defer</c> set to <see langword="true"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the backend submitted work; otherwise, <see langword="false"/>.</returns>
    public bool Submit() => Methods.Submit != null && Methods.Submit(Context);

    /// <summary>
    /// Blocks until the copies submitted by <see cref="Submit"/> have completed.
    /// </summary>
    /// <returns><see langword="true"/> if the backend waited for work; otherwise, <see langword="false"/>.</returns>
    public bool Wait() => Methods.Wait != null && Methods.Wait(Context);

    /// <summary>
    /// Asks the backend to make a segment resident or discardable.
    /// </summary>
    /// <param name="segment">The segment to change.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend applied the state; otherwise, <see langword="false"/>.</returns>
    public bool SetResidency(in UnifiedMemorySegment segment, UnifiedMemoryResidency residency)
        => Methods.SetResidency != null && Methods.SetResidency(Context, segment.Host, segment.DeviceHandle, residency);

    /// <summary>
    /// Asks the backend to make the segment that owns <paramref name="block"/> resident or discardable.
    /// </summary>
    /// <param name="block">A block of the segment to change.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend applied the state; otherwise, <see langword="false"/>.</returns>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency)
        => Methods.SetResidency != null && Methods.SetResidency(Context, block.SegmentHost, block.DeviceHandle, residency);
}

/// <summary>
/// Implemented by a graphics backend to supply a <see cref="UnifiedProvider"/> that <see cref="UnifiedAllocator"/> can
/// allocate device-visible memory from.
/// </summary>
/// <remarks>
/// <para>
/// A backend bridge usually lives on a managed object that owns the graphics device, so the pointer it hands out as
/// <see cref="UnifiedProvider.Context"/> cannot be the object itself. The supported pattern is to keep a
/// <see cref="GCHandle"/> to the bridge and to use <see cref="GCHandle.ToIntPtr"/> as the context, which the static
/// vtable functions turn back into the object with <see cref="GCHandle.FromIntPtr"/>.
/// </para>
/// <para>
/// Implementations must keep <see cref="UnifiedProvider.Context"/> stable for as long as the provider is in use, must
/// report the alignment, the segment size limit and the working set the backend really has, must implement
/// <see cref="Copy"/> because it is the only way into device-only memory, and must treat <see cref="Flush"/> and
/// <see cref="Invalidate"/> as cheap no-ops for the heaps that are already coherent.
/// </para>
/// </remarks>
public unsafe interface IUnifiedProvider
{
    /// <summary>
    /// Gets the alignment the backend requires for segment base addresses and sizes, in bytes.
    /// </summary>
    nuint Alignment { get; }

    /// <summary>
    /// Gets the largest segment the backend can create, in bytes, or <see langword="0"/> when it has no limit.
    /// </summary>
    nuint MaxSegmentSize { get; }

    /// <summary>
    /// Gets the working set the backend recommends, in bytes, or <see langword="0"/> when it has no opinion.
    /// </summary>
    nuint WorkingSetLimit { get; }

    /// <summary>
    /// Acquires a segment of <paramref name="size"/> bytes aligned to <paramref name="alignment"/> for
    /// <paramref name="usage"/>.
    /// </summary>
    /// <param name="size">The size of the segment in bytes, already a multiple of the alignment.</param>
    /// <param name="alignment">The required base alignment in bytes; a power of two.</param>
    /// <param name="usage">What the CPU does with the segment, which selects the heap on a multi-heap backend.</param>
    /// <param name="deviceHandle">When the method returns, the opaque device handle that owns the segment.</param>
    /// <param name="deviceAddress">When the method returns, the device address of the segment, or <see langword="0"/>.</param>
    /// <param name="kind">When the method returns, how the chosen segment is shared between the CPU and the device.</param>
    /// <returns>The CPU address of the segment, or <see langword="null"/> for a device-only segment.</returns>
    void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind);

    /// <summary>
    /// Releases a segment previously returned by <see cref="Alloc"/>.
    /// </summary>
    /// <param name="segment">The CPU address of the segment, or <see langword="null"/> for a device-only segment.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    /// <param name="deviceHandle">The device handle of the segment, which identifies a device-only segment.</param>
    void Free(void* segment, nuint size, void* deviceHandle);

    /// <summary>
    /// Makes CPU writes in the range visible to the device.
    /// </summary>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    void Flush(void* ptr, nuint size);

    /// <summary>
    /// Makes device writes in the range visible to the CPU.
    /// </summary>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    void Invalidate(void* ptr, nuint size);

    /// <summary>
    /// Copies <paramref name="size"/> bytes between two blocks, from <paramref name="sourceOffset"/> inside the source to
    /// <paramref name="destinationOffset"/> inside the destination, optionally recording the copy for a later
    /// <see cref="Submit"/>.
    /// </summary>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="defer">Whether to only record the copy instead of completing it.</param>
    /// <returns><see langword="true"/> if the backend performed or recorded the copy; otherwise, <see langword="false"/>.</returns>
    bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer);

    /// <summary>
    /// Submits the copies recorded with <see cref="Copy"/> and <paramref name="defer"/> set.
    /// </summary>
    /// <returns><see langword="true"/> if the backend submitted work; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// A copy that does not defer submits the recorded batch as well, because a bridge owns one queue position at a time:
    /// a caller that mixes the two forms has to submit and wait for the batch before its first synchronous copy.
    /// </remarks>
    bool Submit();

    /// <summary>
    /// Blocks until the copies submitted by <see cref="Submit"/> have completed.
    /// </summary>
    /// <returns><see langword="true"/> if the backend waited for work; otherwise, <see langword="false"/>.</returns>
    bool Wait();

    /// <summary>
    /// Asks the backend to make the segment that owns <paramref name="block"/> resident or discardable.
    /// </summary>
    /// <param name="block">A block of the segment to change.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the backend applied the state; otherwise, <see langword="false"/>.</returns>
    bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency);

    /// <summary>
    /// Gets a <see cref="UnifiedProvider"/> whose context is a stable pointer to this implementation's state.
    /// </summary>
    /// <returns>A provider backed by this implementation.</returns>
    UnifiedProvider GetProvider();
}

/// <summary>
/// Serves page-aligned host memory that a backend bridge can import as device memory without copying.
/// </summary>
/// <remarks>
/// <para>
/// This provider owns nothing but the pages: it allocates them, keeps them alive, and reports the sharing kind it was
/// told to report. Establishing the sharing is the bridge's job, because only the graphics API knows whether it can
/// import host pages. The three backend paths that can do so are Metal's
/// <c>newBufferWithBytesNoCopy:length:options:deallocator:</c> on Apple silicon, Vulkan's
/// <c>VK_EXT_external_memory_host</c> import, and CUDA's <c>cudaHostAlloc</c> with <c>cudaHostAllocMapped</c> followed
/// by <c>cudaHostGetDevicePointer</c>.
/// </para>
/// <para>
/// Because the pages are plain host memory, blocks allocated from this provider stay usable when the import probe
/// fails: the provider is an honest fallback, not a fake device. Create it with
/// <see cref="UnifiedMemoryKind.Shared"/> or <see cref="UnifiedMemoryKind.SharedCoherent"/> only after the bridge has
/// verified that the import succeeds for the alignment this provider allocates with.
/// </para>
/// <para>
/// The provider has one heap, so <see cref="Alloc"/> ignores <see cref="UnifiedMemoryUsage"/> and reports its one kind
/// for every segment. A <see cref="UnifiedMemoryUsage.Device"/> request therefore comes back as ordinary host memory
/// with <see cref="UnifiedMemoryKind.Host"/>: the engine then has to upload it, which is exactly what the reported kind
/// says. A bridge that can offer a coherent shared heap for <see cref="UnifiedMemoryUsage.General"/> and a CPU-invisible
/// heap for <see cref="UnifiedMemoryUsage.Device"/> is more useful than this one and should implement
/// <see cref="IUnifiedProvider"/> directly.
/// </para>
/// </remarks>
internal unsafe struct HostUnifiedProvider : IUnifiedProvider
{
    /// <summary>
    /// Bytes reserved in front of every block to record the distance back to the block the OS returned, so an arbitrary
    /// power-of-two alignment costs one aligned move instead of one OS allocation per alignment.
    /// </summary>
    private const nuint HEADER_SIZE = 16;

    /// <summary>
    /// The largest value a <see cref="nuint"/> can hold. It is computed rather than read from <c>nuint.MaxValue</c>
    /// because that member does not exist on every target framework.
    /// </summary>
    private static readonly nuint maxSize = unchecked((nuint)0 - 1);

    /// <summary>
    /// Provider methods for vtable.
    /// </summary>
    private static readonly UnifiedProvider.VTable methods = new(&AllocThunk, &FreeThunk, &FlushThunk, &InvalidateThunk, &CopyThunk, &SubmitThunk, &WaitThunk, &AlignmentThunk, &MaxSegmentSizeThunk, &WorkingSetLimitThunk, &SetResidencyThunk);

    /// <summary>
    /// The largest segment the importing backend accepts, or <see langword="0"/> when it has no limit.
    /// </summary>
    private readonly nuint m_MaxSegmentSize;

    /// <summary>
    /// The working set the backend recommends, or <see langword="0"/> when it has no opinion.
    /// </summary>
    private readonly nuint m_WorkingSetLimit;

    /// <summary>
    /// The sharing kind the bridge established for these pages.
    /// </summary>
    private readonly UnifiedMemoryKind m_Kind;

    /// <summary>
    /// Initializes a provider over ordinary heap pages that are not shared with any device.
    /// </summary>
    public HostUnifiedProvider()
    {
        Alignment = 0;
        m_MaxSegmentSize = 0;
        m_WorkingSetLimit = 0;
        m_Kind = UnifiedMemoryKind.Host;
    }

    /// <param name="kind">How a bridge shares these pages with the device; <see cref="UnifiedMemoryKind.Host"/> when none does.</param>
    /// <param name="alignment">
    /// The alignment the device requires for an imported range, or <see langword="0"/> for the system page size. Use
    /// the backend's import requirement, which is never smaller than a page.
    /// </param>
    /// <param name="maxSegmentSize">
    /// The largest range the backend can import in one object, or <see langword="0"/> when it has no limit. Pass Metal's
    /// <c>maxBufferLength</c> or Vulkan's <c>maxMemoryAllocationSize</c> here.
    /// </param>
    /// <param name="workingSetLimit">
    /// The working set the backend recommends, or <see langword="0"/> when it has no opinion. Pass Metal's
    /// <c>recommendedMaxWorkingSetSize</c> here.
    /// </param>
    public HostUnifiedProvider(UnifiedMemoryKind kind, nuint alignment, nuint maxSegmentSize = 0, nuint workingSetLimit = 0)
    {
        Alignment = alignment;
        m_MaxSegmentSize = maxSegmentSize;
        m_WorkingSetLimit = workingSetLimit;
        m_Kind = kind;
    }

    /// <inheritdoc/>
    public readonly nuint Alignment => field == 0 ? (nuint)Environment.SystemPageSize : field;

    /// <inheritdoc/>
    public readonly nuint MaxSegmentSize => m_MaxSegmentSize;

    /// <inheritdoc/>
    public readonly nuint WorkingSetLimit => m_WorkingSetLimit;

    /// <inheritdoc/>
    public UnifiedProvider GetProvider()
    {
#if NET9_0_OR_GREATER
        return new UnifiedProvider(Native.AsPointer(ref this), methods);
#else
        fixed (HostUnifiedProvider* pThis = &this)
        {
            return new UnifiedProvider(pThis, methods);
        }
#endif
    }

    /// <inheritdoc/>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        var self = this;
        kind = m_Kind;
        return AllocateSegment(&self, size, alignment, out deviceHandle, out deviceAddress);
    }

    /// <inheritdoc/>
    public readonly void Free(void* ptr, nuint size, void* deviceHandle)
    {
        var self = this;
        FreeSegment(&self, ptr, size);
    }

    /// <inheritdoc/>
    public readonly void Flush(void* ptr, nuint size) { }

    /// <inheritdoc/>
    public readonly void Invalidate(void* ptr, nuint size) { }

    /// <inheritdoc/>
    public readonly bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        // Host pages are all mapped, so a plain memcpy is the whole story; a device-only block is out of reach.
        if (destination.Host == null || source.Host == null)
        {
            return false;
        }

        if (destinationOffset > destination.Length || size > destination.Length - destinationOffset)
        {
            return false;
        }

        if (sourceOffset > source.Length || size > source.Length - sourceOffset)
        {
            return false;
        }

        Buffer.MemoryCopy(source.Host + sourceOffset, destination.Host + destinationOffset, (long)size, (long)size);
        return true;
    }

    /// <inheritdoc/>
    public readonly bool Submit() => false;

    /// <inheritdoc/>
    public readonly bool Wait() => false;

    /// <inheritdoc/>
    public readonly bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Forwards a vtable call to the host segment allocation.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    /// <param name="alignment">The required base alignment in bytes.</param>
    /// <param name="usage">The usage of the segment, which this single-heap provider ignores.</param>
    /// <param name="deviceHandle">When the method returns, the device handle of the segment.</param>
    /// <param name="deviceAddress">When the method returns, the device address of the segment.</param>
    /// <param name="kind">When the method returns, the sharing kind of the segment.</param>
    /// <returns>The CPU address of the segment.</returns>
    private static void* AllocThunk(void* ctx, nuint size, nuint alignment, UnifiedMemoryUsage usage, void** deviceHandle, ulong* deviceAddress, UnifiedMemoryKind* kind)
    {
        var self = (HostUnifiedProvider*)ctx;
        var segment = AllocateSegment(self, size, alignment, out var handle, out var address);
        *deviceHandle = handle;
        *deviceAddress = address;
        *kind = self->m_Kind;
        return segment;
    }

    /// <summary>
    /// Forwards a vtable call to the host segment release.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="ptr">The CPU address of the segment.</param>
    /// <param name="deviceHandle">The device handle of the segment, which host pages never have.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    private static void FreeThunk(void* ctx, void* ptr, void* deviceHandle, nuint size) => FreeSegment((HostUnifiedProvider*)ctx, ptr, size);

    /// <summary>
    /// Does nothing: plain host pages have no device-side cache to publish CPU writes to.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    private static void FlushThunk(void* ctx, void* ptr, nuint size) { }

    /// <summary>
    /// Does nothing: plain host pages have no device-side cache to publish device writes from.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="ptr">The CPU address of the range.</param>
    /// <param name="size">The size of the range in bytes.</param>
    private static void InvalidateThunk(void* ctx, void* ptr, nuint size) { }

    /// <summary>
    /// Forwards a vtable copy to the provider instance.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="destination">The block to copy into.</param>
    /// <param name="destinationOffset">The offset inside the destination block, in bytes.</param>
    /// <param name="source">The block to copy from.</param>
    /// <param name="sourceOffset">The offset inside the source block, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <param name="defer">
    /// Ignored, because a host memcpy is already complete when it returns and there is no queue to record it in.
    /// </param>
    /// <returns><see langword="true"/> if the copy was performed; otherwise, <see langword="false"/>.</returns>
    private static bool CopyThunk(void* ctx, UnifiedMemoryBlock* destination, nuint destinationOffset, UnifiedMemoryBlock* source, nuint sourceOffset, nuint size, bool defer)
        => ((HostUnifiedProvider*)ctx)->Copy(*destination, destinationOffset, *source, sourceOffset, size, defer);

    /// <summary>
    /// Reports that host memory has no copy queue to submit.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    private static bool SubmitThunk(void* ctx) => false;

    /// <summary>
    /// Reports that host memory has no submitted work to wait for.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    private static bool WaitThunk(void* ctx) => false;

    /// <summary>
    /// Reports the alignment the provider allocates with.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <returns>The alignment in bytes.</returns>
    private static nuint AlignmentThunk(void* ctx) => ((HostUnifiedProvider*)ctx)->Alignment;

    /// <summary>
    /// Reports the largest segment the importing backend accepts.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <returns>The limit in bytes, or <see langword="0"/> when there is none.</returns>
    private static nuint MaxSegmentSizeThunk(void* ctx) => ((HostUnifiedProvider*)ctx)->m_MaxSegmentSize;

    /// <summary>
    /// Reports the working set the backend recommends.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <returns>The recommended working set in bytes, or <see langword="0"/> when there is no opinion.</returns>
    private static nuint WorkingSetLimitThunk(void* ctx) => ((HostUnifiedProvider*)ctx)->m_WorkingSetLimit;

    /// <summary>
    /// Reports that plain host pages have no residency state to change.
    /// </summary>
    /// <param name="ctx">The context pointer to the HostUnifiedProvider instance.</param>
    /// <param name="segmentHost">The CPU address of the segment.</param>
    /// <param name="deviceHandle">The device handle of the segment.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    private static bool SetResidencyThunk(void* ctx, void* segmentHost, void* deviceHandle, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Allocates host pages aligned to at least the provider's alignment, which is what an import needs.
    /// </summary>
    /// <param name="self">The provider instance.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    /// <param name="alignment">The required base alignment in bytes; a power of two.</param>
    /// <param name="deviceHandle">Always <see langword="null"/>: host pages carry no handle until a bridge imports them.</param>
    /// <param name="deviceAddress">Always <see langword="0"/>: an import is usually a handle-based binding.</param>
    /// <returns>The CPU address of the segment.</returns>
    /// <exception cref="OutOfMemoryException">Thrown when the pages cannot be allocated.</exception>
    private static void* AllocateSegment(HostUnifiedProvider* self, nuint size, nuint alignment, out void* deviceHandle, out ulong deviceAddress)
    {
        var align = (*self).Alignment;
        if (alignment > align)
        {
            align = alignment;
        }

        // Adding the padding and the header can overflow, and an overflowing request must fail rather than wrap.
        if (align > maxSize - size - HEADER_SIZE)
        {
            throw new OutOfMemoryException($"The unified provider cannot serve a segment of {size} bytes.");
        }

        var total = size + align + HEADER_SIZE;
        void* raw;

#if NET6_0_OR_GREATER
        raw = NativeMemory.Alloc(total);
#else
        raw = Marshal.AllocHGlobal((nint)total).ToPointer();
#endif

        if (raw == null)
        {
            throw new OutOfMemoryException($"The unified provider cannot allocate {total} bytes.");
        }

        var payload = (byte*)Native.AlignForward((nuint)raw + HEADER_SIZE, align);

        // The distance back to the OS block is stored immediately in front of the payload, which always lands inside the
        // allocation because the payload starts at least HeaderSize bytes after the block.
        ((nuint*)payload)[-1] = (nuint)payload - (nuint)raw;

        deviceHandle = null;
        deviceAddress = 0;
        return payload;
    }

    /// <summary>
    /// Releases the segment at <paramref name="ptr"/> back to the OS allocator it came from.
    /// </summary>
    /// <param name="self">The provider instance.</param>
    /// <param name="ptr">The CPU address of the segment.</param>
    /// <param name="size">The size of the segment in bytes.</param>
    private static void FreeSegment(HostUnifiedProvider* self, void* ptr, nuint size)
    {
        if (ptr == null)
        {
            return;
        }

        var raw = (byte*)ptr - ((nuint*)ptr)[-1];

#if NET6_0_OR_GREATER
        NativeMemory.Free(raw);
#else
        Marshal.FreeHGlobal((nint)raw);
#endif
    }
}