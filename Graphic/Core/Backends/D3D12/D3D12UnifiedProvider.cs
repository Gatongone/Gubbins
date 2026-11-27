using System.Runtime.InteropServices;
using Gubbins.Graphic;
using Gubbins.Graphic.D3D12;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// A D3D12 implementation of <see cref="IUnifiedProvider"/>: one <c>ID3D12Heap</c> plus one placed buffer resource per
/// segment, mapped when the heap allows it, with a copy queue for the transfers a device-only heap needs.
/// </summary>
/// <remarks>
/// <para>
/// The bridge borrows the device, the copy queue and the command allocator from the engine, exactly as the Vulkan bridge
/// borrows a <c>VkDevice</c>. The engine owns teardown: dispose the bridge before the device.
/// </para>
/// <para>
/// A segment is a heap with a single buffer placed at offset zero, so a block is a <c>(resource, offset)</c> pair, which is
/// what <c>CopyBufferRegion</c> and a shader resource view both take. D3D12 has no cache maintenance API: a CPU write is
/// published by the fence and the command list that follows it, so <see cref="Flush"/> and <see cref="Invalidate"/> are
/// no-ops, which the reported <see cref="UnifiedMemoryKind.SharedCoherent"/> tells the allocator.
/// </para>
/// </remarks>
public sealed unsafe class D3D12UnifiedProvider : IUnifiedProvider, IDisposable
{
    /// <summary>
    /// A copy that has been recorded but not submitted yet.
    /// </summary>
    private struct CopyOp
    {
        public nint Source;
        public nint Destination;
        public ulong SourceOffset;
        public ulong DestinationOffset;
        public ulong Size;
    }

    /// <summary>
    /// The bookkeeping of one segment: the heap, the resource placed in it and the mapping when it has one.
    /// </summary>
    private struct Arena
    {
        public Arena* Next;
        public nint Heap;
        public nint Resource;
        public byte* Host;
        public nuint Size;
        public nuint AllocationSize;
        public UnifiedMemoryKind Kind;
        public bool Owned;
    }

    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nint m_Allocator;
    private readonly nint m_CommandList;
    private readonly nint m_Fence;
    private readonly bool m_UnifiedMemory;
    private readonly nuint m_Alignment;
    private readonly nuint m_WorkingSetLimit;

    /// <summary>
    /// The largest segment the adapter can back, which is the local video memory budget it reports.
    /// </summary>
    private readonly nuint m_MaxSegmentSize;

    private readonly List<CopyOp> m_Pending = [];

    private Arena* m_First;
    private GCHandle m_Self;
    private ulong m_FenceValue;
    private bool m_Disposed;

    /// <summary>
    /// The number of segments the driver forced onto the fallback heap.
    /// </summary>
    private int m_FallbackHeapCount;

    /// <param name="device">The <c>ID3D12Device</c> the engine owns.</param>
    /// <param name="queue">A queue created with <c>D3D12_COMMAND_LIST_TYPE_COPY</c>.</param>
    /// <param name="commandAllocator">An allocator created with <c>D3D12_COMMAND_LIST_TYPE_COPY</c>.</param>
    /// <param name="workingSetLimit">The working set the engine wants reported, or <see langword="0"/>.</param>
    /// <exception cref="ArgumentException">Thrown when a required pointer is missing.</exception>
    /// <exception cref="D3D12Exception">Thrown when the bridge cannot create its command list or fence.</exception>
    public D3D12UnifiedProvider(nint device, nint queue, nint commandAllocator, nuint workingSetLimit = 0)
    {
        if (device == 0)
        {
            throw new ArgumentException("An ID3D12Device pointer is required.", nameof(device));
        }

        if (queue == 0)
        {
            throw new ArgumentException("An ID3D12CommandQueue pointer is required.", nameof(queue));
        }

        if (commandAllocator == 0)
        {
            throw new ArgumentException("An ID3D12CommandAllocator pointer is required.", nameof(commandAllocator));
        }

        m_Device = device;
        m_Queue = queue;
        m_Allocator = commandAllocator;

        // DXGI reports the video memory budget per segment. Their sum is the working set the caller should stay inside.
        // The local segment is what a segment can be made of and still stay resident, so it is also the largest segment:
        // a heap is allowed to be bigger than the local budget, but nothing would ever keep it in video memory.
        if (D3D12Api.TryQueryBudgets(device, out var localBudget, out var nonLocalBudget))
        {
            var total = localBudget + nonLocalBudget;
            m_WorkingSetLimit = workingSetLimit == 0 ? (nuint)total : workingSetLimit;
            m_MaxSegmentSize = (nuint)localBudget;
        }
        else
        {
            m_WorkingSetLimit = workingSetLimit;
            m_MaxSegmentSize = 0;
        }

        // A placed resource has to sit at the placement alignment, so a segment is a whole number of those.
        m_Alignment = D3D12Api.RESOURCE_PLACEMENT_ALIGNMENT;

        var architecture = new D3D12FeatureDataArchitecture();
        if (D3D12Api.CheckFeatureSupport(device, D3D12Api.FEATURE_ARCHITECTURE, &architecture, (uint)sizeof(D3D12FeatureDataArchitecture)) < 0)
        {
            architecture.UnifiedMemory = 0;
        }

        m_UnifiedMemory = architecture.UnifiedMemory != 0;

        var listResult = D3D12Api.CreateCommandList(device, commandAllocator, D3D12Api.COMMAND_LIST_TYPE_COPY, out var commandList);
        if (listResult < 0 || commandList == 0)
        {
            throw new D3D12Exception("CreateCommandList failed.", listResult);
        }

        // CreateCommandList hands back a list that is already recording, and an allocator cannot be reset while a list
        // that recorded into it is still open, so the initial recording session is closed here.
        var closeResult = D3D12Api.CloseCommandList(commandList);
        if (closeResult < 0)
        {
            throw new D3D12Exception("Closing the initial command list failed.", closeResult);
        }

        m_CommandList = commandList;

        var fenceResult = D3D12Api.CreateFence(device, out var fence);
        if (fenceResult < 0 || fence == 0)
        {
            throw new D3D12Exception("CreateFence failed.", fenceResult);
        }

        m_Fence = fence;
    }

    /// <summary>
    /// Gets a value indicating whether the adapter reported unified memory, so the memory shared with the CPU is the DRAM
    /// the CPU itself runs from rather than system memory the device reaches over PCIe.
    /// </summary>
    public bool IsUnifiedMemory => m_UnifiedMemory;

    /// <summary>
    /// Gets the number of segments the driver created on the write-combined upload heap, because it declined the cached
    /// heap <see cref="D3D12HeapPolicy.Choose"/> asked for.
    /// </summary>
    /// <remarks>
    /// Zero is the expected value. Any other value means the CPU reads those segments far slower than memory speed, and
    /// that the driver rather than the caller made that choice.
    /// </remarks>
    internal int FallbackHeapCount => m_FallbackHeapCount;

    /// <inheritdoc/>
    public nuint Alignment => m_Alignment;

    /// <inheritdoc/>
    /// <remarks>
    /// This is the local video memory budget the adapter reports: a segment bigger than that could be created, but nothing
    /// would keep it in video memory. D3D12 has no per-allocation limit to mirror and probing for one is destructive,
    /// because a request the adapter cannot back can remove the device, so the budget is the bound instead.
    /// <see langword="0"/> means the budgets could not be read.
    /// </remarks>
    public nuint MaxSegmentSize => m_MaxSegmentSize;

    /// <inheritdoc/>
    public nuint WorkingSetLimit => m_WorkingSetLimit;

    /// <summary>
    /// Gets the fence value the most recent <see cref="Submit"/> was tagged with.
    /// </summary>
    /// <remarks>
    /// Once <see cref="Wait"/> has returned <see langword="true"/> the queue has passed this value, which is what a caller
    /// passes to <c>UnifiedAllocator.TryFreeWhenComplete</c> to release the blocks the submitted copies read.
    /// </remarks>
    public ulong SubmittedFenceValue => m_FenceValue;

    /// <summary>
    /// Gets the number of live segments.
    /// </summary>
    internal int SegmentCount
    {
        get
        {
            var count = 0;

            for (var arena = m_First; arena != null; arena = arena->Next)
            {
                count++;
            }

            return count;
        }
    }

    /// <inheritdoc/>
    public UnifiedProvider GetProvider() => ProviderAdapter<D3D12UnifiedProvider>.Create(this, ref m_Self);

    /// <summary>
    /// Creates a two dimensional texture on the device.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <returns>The texture, to be disposed when it is no longer read.</returns>
    public GpuTexture2D CreateTexture(int width, int height, GpuTextureFormat format)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12UnifiedProvider));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "A texture is at least one pixel wide and high.");
        }

        return new GpuTexture2D(new D3D12Texture(m_Device, width, height, format), width, height, format);
    }

    /// <summary>
    /// Wraps a texture the engine owns.
    /// </summary>
    /// <remarks>
    /// The returned view releases its own copy queue and shader resource view when it is disposed, never the resource, which
    /// stays with the engine. It tracks the state it last put the resource in, so the caller passes the state the resource
    /// is in now and has to keep every view of the same resource in agreement about it.
    /// </remarks>
    /// <param name="resource">The <c>ID3D12Resource</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="state">The state the resource is in.</param>
    /// <param name="imported">When the method returns, the texture view.</param>
    /// <returns><see langword="true"/> when the texture was wrapped; otherwise, <see langword="false"/>.</returns>
    public bool TryImportTexture(nint resource, int width, int height, GpuTextureFormat format, int state, out GpuTexture2D? imported)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12UnifiedProvider));
        }

        imported = null;

        if (resource == 0 || width <= 0 || height <= 0 || state < 0 || !D3D12Texture.TryDescribeFormat(format, out _, out _))
        {
            return false;
        }

        imported = new GpuTexture2D(new D3D12Texture(m_Device, resource, width, height, format, state), width, height, format);
        return true;
    }

    /// <summary>
    /// Creates a sampler state on the device.
    /// </summary>
    /// <param name="sampler">The sampler description.</param>
    /// <returns>The state, to be disposed when it is no longer bound.</returns>
    public GpuSamplerState CreateSampler(GpuSampler sampler)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12UnifiedProvider));
        }

        return new GpuSamplerState(new D3D12SamplerState(m_Device, sampler));
    }

    /// <inheritdoc/>
    /// <exception cref="OutOfMemoryException">Thrown when the driver cannot create the heap.</exception>
    /// <exception cref="D3D12Exception">Thrown when the heap or the placed resource cannot be created for another reason.</exception>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12UnifiedProvider));
        }

        var choice = D3D12HeapPolicy.Choose(usage);
        kind = choice.Kind;

        var heapResult = CreateHeap(size, choice, out var heap);

        if ((heapResult < 0 || heap == 0) && choice.Optional)
        {
            // A driver may decline a custom heap. The upload heap is then the only mappable memory left: the segment still
            // works, but the CPU reads it a handful of bytes per transaction, so that is counted and can be reported
            // rather than silently paid for.
            choice = D3D12HeapPolicy.FallbackFor(usage);
            kind = choice.Kind;
            heapResult = CreateHeap(size, choice, out heap);
            m_FallbackHeapCount++;
        }

        if (heapResult < 0 || heap == 0)
        {
            ThrowAllocationFailure($"CreateHeap failed for {size} bytes of {usage} memory", heapResult);
        }

        var resourceDesc = new D3D12ResourceDesc
        {
            Dimension = D3D12Api.RESOURCE_DIMENSION_BUFFER,
            Alignment = 0,
            Width = size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = 0,                                            // DXGI_FORMAT_UNKNOWN, which is what a buffer requires
            SampleCount = 1,                                       // a buffer needs a sample count of one
            SampleQuality = 0,
            Layout = D3D12Api.TEXTURE_LAYOUT_ROW_MAJOR,
            Flags = choice.ResourceFlags
        };

        var resourceResult = D3D12Api.CreatePlacedResource(m_Device, heap, 0, &resourceDesc, choice.InitialState, out var resource);
        if (resourceResult < 0 || resource == 0)
        {
            D3D12Api.Release(heap);
            ThrowAllocationFailure($"CreatePlacedResource failed for {size} bytes of {usage} memory", resourceResult);
        }

        byte* host = null;
        if (choice.Mapped)
        {
            if (D3D12Api.Map(resource, null, out var mapped) < 0 || mapped == null)
            {
                D3D12Api.Release(resource);
                D3D12Api.Release(heap);
                throw new D3D12Exception($"Mapping a {size} byte {usage} segment failed.");
            }

            host = (byte*)mapped;
        }

        // A buffer's GPU virtual address is stable for its lifetime, so a block's address is the segment's plus its offset.
        var address = choice.Kind == UnifiedMemoryKind.Device ? D3D12Api.GetVirtualAddress(resource) : 0;

        var arena = (Arena*)NativeInterop.Alloc((nuint)sizeof(Arena));
        if (arena == null)
        {
            if (host != null)
            {
                D3D12Api.Unmap(resource, null);
            }

            D3D12Api.Release(resource);
            D3D12Api.Release(heap);
            throw new OutOfMemoryException($"Unable to allocate {sizeof(Arena)} bytes of bridge bookkeeping.");
        }

        *arena = default;
        arena->Heap = heap;
        arena->Resource = resource;
        arena->Host = host;
        arena->Size = size;
        arena->AllocationSize = size;
        arena->Kind = choice.Kind;
        arena->Owned = true;
        arena->Next = m_First;
        m_First = arena;

        deviceHandle = arena;
        deviceAddress = address;
        return host;
    }

    /// <inheritdoc/>
    public void Free(void* segment, nuint size, void* deviceHandle)
    {
        if (m_Disposed)
        {
            return;
        }

        var arena = (Arena*)deviceHandle;
        if (arena == null)
        {
            for (var current = m_First; current != null; current = current->Next)
            {
                if (current->Host == (byte*)segment)
                {
                    arena = current;
                    break;
                }
            }
        }

        if (arena == null)
        {
            return;
        }

        Unlink(arena);
        DestroyArena(arena);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Does nothing: D3D12 has no cache maintenance API, and the reported kind is
    /// <see cref="UnifiedMemoryKind.SharedCoherent"/>, so the allocator never asks.
    /// </remarks>
    public void Flush(void* ptr, nuint size) { }

    /// <inheritdoc/>
    /// <remarks>Does nothing, for the same reason as <see cref="Flush"/>.</remarks>
    public void Invalidate(void* ptr, nuint size) { }

    /// <inheritdoc/>
    public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        if (m_Disposed || size == 0)
        {
            return false;
        }

        var destinationArena = (Arena*)destination.DeviceHandle;
        var sourceArena = (Arena*)source.DeviceHandle;
        if (destinationArena == null || sourceArena == null)
        {
            return false;
        }

        m_Pending.Add(new CopyOp
        {
            Source = sourceArena->Resource,
            Destination = destinationArena->Resource,
            SourceOffset = (ulong)(source.Offset + sourceOffset),
            DestinationOffset = (ulong)(destination.Offset + destinationOffset),
            Size = size
        });

        return defer || (Submit() && Wait());
    }

    /// <inheritdoc/>
    /// <exception cref="D3D12Exception">Thrown when recording or executing the copies fails.</exception>
    public bool Submit()
    {
        if (m_Disposed || m_Pending.Count == 0)
        {
            return false;
        }

        var allocatorResult = D3D12Api.ResetCommandAllocator(m_Allocator);
        if (allocatorResult < 0)
        {
            throw new D3D12Exception("Resetting the command allocator failed.", allocatorResult);
        }

        var listResult = D3D12Api.ResetCommandList(m_CommandList, m_Allocator);
        if (listResult < 0)
        {
            throw new D3D12Exception("Resetting the command list failed.", listResult);
        }

        foreach (var copy in m_Pending)
        {
            D3D12Api.CopyBufferRegion(m_CommandList, copy.Destination, copy.DestinationOffset, copy.Source, copy.SourceOffset, copy.Size);
        }

        if (D3D12Api.CloseCommandList(m_CommandList) < 0)
        {
            throw new D3D12Exception("Closing the command list failed.");
        }

        D3D12Api.ExecuteCommandLists(m_Queue, m_CommandList);

        // The fence is what makes the copies observable: the queue signals it once the list has run.
        m_FenceValue++;
        if (D3D12Api.Signal(m_Queue, m_Fence, m_FenceValue) < 0)
        {
            throw new D3D12Exception("Signalling the copy fence failed.");
        }

        m_Pending.Clear();
        return true;
    }

    /// <inheritdoc/>
    public bool Wait()
    {
        if (m_Disposed || m_FenceValue == 0)
        {
            return false;
        }

        if (!D3D12Api.WaitForFence(m_Fence, m_FenceValue))
        {
            throw new D3D12Exception("Waiting for the copy fence failed.");
        }

        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// A segment is one heap with one buffer placed in it, and D3D12 evicts the buffer before the heap it was placed in,
    /// which is the reverse of a residency request, where the heap has to be back before the resource. Only the memory
    /// that backs the block is touched, so a discardable block loses its residency without disturbing the other blocks of
    /// the same segment. The contents survive either way: eviction is a hint about paging rather than a discard.
    /// </remarks>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency)
    {
        if (m_Disposed || !TryGetResource(block, out var resource, out _) || !TryGetHeap(block, out var heap, out _))
        {
            return false;
        }

        return ApplyResidency(resource, heap, residency);
    }

    /// <summary>
    /// Applies a residency request to the heap of a segment and to the buffer placed in it.
    /// </summary>
    /// <param name="resource">The <c>ID3D12Resource</c> placed in the heap.</param>
    /// <param name="heap">The <c>ID3D12Heap</c> that owns the memory.</param>
    /// <param name="residency">The requested residency.</param>
    /// <returns><see langword="true"/> if the driver accepted both steps; otherwise, <see langword="false"/>.</returns>
    private bool ApplyResidency(nint resource, nint heap, UnifiedMemoryResidency residency)
    {
        var objects = stackalloc nint[1];

        if (residency == UnifiedMemoryResidency.Discardable)
        {
            objects[0] = resource;

            if (D3D12Api.Evict(m_Device, objects, 1) < 0)
            {
                return false;
            }

            objects[0] = heap;
            return D3D12Api.Evict(m_Device, objects, 1) >= 0;
        }

        objects[0] = heap;

        if (D3D12Api.MakeResident(m_Device, objects, 1) < 0)
        {
            return false;
        }

        objects[0] = resource;
        return D3D12Api.MakeResident(m_Device, objects, 1) >= 0;
    }

    /// <summary>
    /// Gets the <c>ID3D12Resource</c> that spans the segment of <paramref name="block"/>, plus the block's offset.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="resource">When the method returns, the resource pointer.</param>
    /// <param name="offset">When the method returns, the offset to bind or copy at.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    public bool TryGetResource(in UnifiedMemoryBlock block, out nint resource, out ulong offset)
    {
        var arena = (Arena*)block.DeviceHandle;
        if (arena == null)
        {
            resource = 0;
            offset = 0;
            return false;
        }

        resource = arena->Resource;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Wraps a resource the engine owns as a block this layer can bind, without taking ownership of it.
    /// </summary>
    /// <param name="resource">The <c>ID3D12Resource</c>.</param>
    /// <param name="offset">The offset of the block inside the resource, in bytes.</param>
    /// <param name="length">The length of the block, in bytes.</param>
    /// <param name="host">The CPU mapping of the resource, or <see langword="0"/> when it is device only.</param>
    /// <param name="kind">How the resource is shared between the processors.</param>
    /// <param name="block">When the method returns, the block.</param>
    /// <returns><see langword="true"/> when the bookkeeping for the block was created.</returns>
    public bool TryImport(nint resource, nuint offset, nuint length, nint host, UnifiedMemoryKind kind, out UnifiedMemoryBlock block)
    {
        block = default;

        if (m_Disposed || resource == 0 || length == 0)
        {
            return false;
        }

        var arena = (Arena*)NativeInterop.AllocZeroed((nuint)sizeof(Arena));

        if (arena == null)
        {
            return false;
        }

        arena->Resource = resource;
        arena->Host = (byte*)host;
        arena->Size = length;
        arena->AllocationSize = length;
        arena->Kind = kind;
        arena->Owned = false;
        arena->Next = m_First;
        m_First = arena;

        block = new UnifiedMemoryBlock(
            arena,
            (byte*)host,
            host == 0 ? null : (byte*)host + (long)offset,
            0,
            offset,
            length,
            length,
            kind);

        return true;
    }

    /// <summary>
    /// Gets the <c>ID3D12Device</c> the bridge allocates on.
    /// </summary>
    internal nint Device => m_Device;

    /// <summary>
    /// Gets the <c>ID3D12CommandQueue</c> the bridge submits copies to.
    /// </summary>
    internal nint Queue => m_Queue;

    /// <summary>
    /// Gets the <c>ID3D12Heap</c> of the segment that owns <paramref name="block"/>, plus the block's offset.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="heap">When the method returns, the heap pointer.</param>
    /// <param name="offset">When the method returns, the offset to place at.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    public bool TryGetHeap(in UnifiedMemoryBlock block, out nint heap, out ulong offset)
    {
        var arena = (Arena*)block.DeviceHandle;
        if (arena == null)
        {
            heap = 0;
            offset = 0;
            return false;
        }

        heap = arena->Heap;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Releases every segment the bridge still owns, plus the command list and fence it created.
    /// </summary>
    /// <remarks>The <c>ID3D12Device</c> must still be alive; the bridge borrows it.</remarks>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // A submitted copy may still be reading a segment, so drain the queue before anything is released.
        if (m_FenceValue != 0 && D3D12Api.WaitForFence(m_Fence, m_FenceValue))
        {
            m_FenceValue = 0;
        }

        var arena = m_First;
        while (arena != null)
        {
            var next = arena->Next;
            DestroyArena(arena);
            arena = next;
        }

        m_First = null;

        D3D12Api.Release(m_Fence);
        D3D12Api.Release(m_CommandList);

        if (m_Self.IsAllocated)
        {
            m_Self.Free();
        }
    }

    /// <summary>
    /// Creates the heap of one segment.
    /// </summary>
    /// <param name="size">The size of the segment.</param>
    /// <param name="choice">The heap parameters to create it with.</param>
    /// <param name="heap">When the method returns, the heap pointer, or zero when the driver declined.</param>
    /// <returns>The <c>HRESULT</c> of the call.</returns>
    private int CreateHeap(nuint size, in D3D12HeapChoice choice, out nint heap)
    {
        var desc = new D3D12HeapDesc
        {
            SizeInBytes = size,
            Properties = choice.Properties,
            Alignment = 0,
            Flags = choice.HeapFlags
        };

        return D3D12Api.CreateHeap(m_Device, &desc, out heap);
    }

    /// <summary>
    /// Reports a failed allocation as <see cref="OutOfMemoryException"/> and everything else as a
    /// <see cref="D3D12Exception"/>.
    /// </summary>
    /// <param name="what">The call that failed.</param>
    /// <param name="hr">The HRESULT the call returned.</param>
    /// <exception cref="OutOfMemoryException">Thrown when the driver ran out of memory.</exception>
    /// <exception cref="D3D12Exception">Thrown for any other failure.</exception>
    private static void ThrowAllocationFailure(string what, int hr)
    {
        // Only E_OUTOFMEMORY is an allocation failure. A removed device is not: asking D3D12 for a heap bigger than the
        // adapter can ever back can remove the device, and after that every allocation fails, so the caller has to see the
        // difference between "this request was too big" and "the device is gone".
        if (hr == unchecked((int)0x8007000E))
        {
            throw new OutOfMemoryException($"{what} (HRESULT 0x{hr:X8}).");
        }

        if (hr == unchecked((int)0x887A0005) || hr == unchecked((int)0x887A0006))
        {
            throw new D3D12Exception(
                $"{what} and the device was removed; the adapter cannot back this request and every later call will fail",
                hr);
        }

        throw new D3D12Exception($"{what}.", hr);
    }

    /// <summary>
    /// Unlinks a segment from the list.
    /// </summary>
    /// <param name="arena">The segment to unlink.</param>
    private void Unlink(Arena* arena)
    {
        if (m_First == arena)
        {
            m_First = arena->Next;
            return;
        }

        for (var current = m_First; current != null; current = current->Next)
        {
            if (current->Next == arena)
            {
                current->Next = arena->Next;
                return;
            }
        }
    }

    /// <summary>
    /// Releases the D3D12 objects of a segment and its bookkeeping.
    /// </summary>
    /// <param name="arena">The segment to release.</param>
    private static void DestroyArena(Arena* arena)
    {
        if (!arena->Owned)
        {
            NativeInterop.Free(arena);
            return;
        }

        if (arena->Host != null)
        {
            D3D12Api.Unmap(arena->Resource, null);
        }

        D3D12Api.Release(arena->Resource);
        D3D12Api.Release(arena->Heap);
        NativeInterop.Free(arena);
    }

}

/// <summary>
/// Reports a failing D3D12 call with the HRESULT it returned.
/// </summary>
public sealed class D3D12Exception : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public D3D12Exception(string message) : base(message)
    {
    }

    /// <param name="message">The message that describes the failure.</param>
    /// <param name="hresult">The HRESULT value, or <see langword="0"/> when the failure has no code.</param>
    public D3D12Exception(string message, int hresult) : base($"{message} (HRESULT 0x{hresult:X8})")
    {
        HResult = hresult;
    }
}

