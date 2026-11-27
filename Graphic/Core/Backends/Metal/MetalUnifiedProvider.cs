using System.Runtime.InteropServices;
using Gubbins.Graphic;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// A Metal implementation of <see cref="IUnifiedProvider"/>: one <c>MTLBuffer</c> per segment, created in the storage mode
/// <see cref="MetalStoragePolicy"/> picks for the usage, with a command queue for the blits a device-only buffer needs.
/// </summary>
/// <remarks>
/// <para>
/// The bridge borrows nothing: it creates the <c>MTLDevice</c> it is given a pointer to, or the system default one, and it
/// owns the command queue. Dispose it before the device disappears.
/// </para>
/// <para>
/// Metal keeps <c>MTLStorageModeShared</c> coherent on every Mac, so <see cref="Flush"/> and <see cref="Invalidate"/> are
/// no-ops and the bridge reports <see cref="UnifiedMemoryKind.SharedCoherent"/>. The storage mode that would need cache
/// maintenance, <c>MTLStorageModeManaged</c>, is only an optimisation on the discrete Macs and the bridge does not ask for
/// it.
/// </para>
/// <para>
/// A deferred copy is a blit recorded into one open command buffer: <see cref="Copy"/> with <c>defer</c> set records,
/// <see cref="Submit"/> ends the encoding and commits, and <see cref="Wait"/> waits for that submission. That is the shape
/// a frame wants - a hundred uploads recorded on the CPU thread and handed to the GPU once - and it only needs one
/// command buffer, which the bridge retains across the calls because Metal hands command buffers out autoreleased.
/// </para>
/// <para>
/// Two rules come out of that shape. There is one batch at a time: recording a deferred copy while an earlier submission is
/// still unwaited returns <see langword="false"/>, so the caller waits or falls back instead of the bridge queuing batches
/// behind its own back. And an immediate copy runs before any open batch, because the batch is only committed at
/// <see cref="Submit"/> - a caller that mixes the two has to submit and wait before it relies on the order.
/// </para>
/// </remarks>
public sealed unsafe class MetalUnifiedProvider : IUnifiedProvider, IDisposable
{
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nuint m_WorkingSetLimit;
    private readonly nuint m_MaxSegmentSize;
    private readonly bool m_UnifiedMemory;
    private readonly List<nint> m_Buffers = [];

    private nint m_Batch;
    private nint m_BatchEncoder;
    private nint m_Committed;

    private GCHandle m_Self;
    private readonly bool m_OwnsQueue;
    private bool m_Disposed;

    /// <param name="device">The <c>MTLDevice</c> to allocate from.</param>
    /// <param name="workingSetLimit">The working set the caller wants reported, or <see langword="0"/> to use the device's.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="device"/> is <see langword="0"/>.</exception>
    /// <exception cref="MetalException">Thrown when Metal refuses to create the command queue.</exception>
    public MetalUnifiedProvider(nint device, nuint workingSetLimit = 0)
        : this(device, 0, workingSetLimit)
    {
    }

    /// <param name="device">The <c>MTLDevice</c> to allocate from.</param>
    /// <param name="queue">The command queue the engine owns, or <see langword="0"/> to create one.</param>
    /// <param name="workingSetLimit">The working set the caller wants reported, or <see langword="0"/> to use the device's.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="device"/> is <see langword="0"/>.</exception>
    /// <exception cref="MetalException">Thrown when Metal refuses to create the command queue.</exception>
    public MetalUnifiedProvider(nint device, nint queue, nuint workingSetLimit = 0)
    {
        if (device == 0)
        {
            throw new ArgumentException("An MTLDevice pointer is required.", nameof(device));
        }

        m_Device = device;

        if (queue == 0)
        {
            queue = MetalApi.Send(device, "newCommandQueue");

            if (queue == 0)
            {
                throw new MetalException("Metal refused to create a command queue.");
            }

            m_OwnsQueue = true;
        }

        m_Queue = queue;

        // The device reports what it wants respected: the recommended working set is the number an application on a unified
        // memory Mac should stay inside, and the largest single buffer is the largest segment the arena may ask for. All
        // three arrived in macOS 10.15, so each one is asked for rather than assumed: a missing selector would abort the
        // process, and a machine that old still allocates correctly, it just has nothing to report.
        var recommended = MetalApi.RespondsToSelector(device, "recommendedMaxWorkingSetSize")
            ? (nuint)MetalApi.SendUInt64(device, "recommendedMaxWorkingSetSize")
            : 0;

        m_WorkingSetLimit = workingSetLimit == 0 ? recommended : workingSetLimit;

        m_MaxSegmentSize = MetalApi.RespondsToSelector(device, "maxBufferLength")
            ? (nuint)MetalApi.SendUInt64(device, "maxBufferLength")
            : 0;

        m_UnifiedMemory = MetalApi.RespondsToSelector(device, "hasUnifiedMemory") && MetalApi.SendBool(device, "hasUnifiedMemory");
        m_Self = GCHandle.Alloc(this, GCHandleType.Normal);
    }

    /// <summary>
    /// Gets a value indicating whether the Objective-C runtime and the Metal framework are both present, which is what a
    /// caller checks before it asks for a device.
    /// </summary>
    internal static bool IsAvailable => MetalApi.IsAvailable;

    /// <summary>
    /// Gets a value indicating whether the device reports unified memory, so the CPU and the GPU address the same pages.
    /// </summary>
    public bool IsUnifiedMemory => m_UnifiedMemory;

    /// <inheritdoc/>
    public nuint Alignment => MetalApi.SEGMENT_ALIGNMENT;

    /// <inheritdoc/>
    public nuint MaxSegmentSize => m_MaxSegmentSize;

    /// <inheritdoc/>
    public nuint WorkingSetLimit => m_WorkingSetLimit;

    /// <summary>
    /// Gets the command queue the bridge created, which a caller can borrow for its own compute or blit work so that it is
    /// ordered against the copies the bridge records.
    /// </summary>
    internal nint CommandQueue => m_Queue;

    /// <summary>
    /// Gets the number of segments the bridge currently holds.
    /// </summary>
    internal int SegmentCount => m_Buffers.Count;

    /// <summary>
    /// Gets the <c>MTLBuffer</c> that spans the segment of <paramref name="block"/>, plus the block's offset.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="buffer">When the method returns, the buffer pointer.</param>
    /// <param name="offset">When the method returns, the offset to bind or copy at.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge; otherwise, <see langword="false"/>.</returns>
    public bool TryGetBuffer(in UnifiedMemoryBlock block, out nint buffer, out ulong offset)
    {
        if (block.DeviceHandle == null)
        {
            buffer = 0;
            offset = 0;
            return false;
        }

        buffer = (nint)block.DeviceHandle;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Wraps a buffer the engine owns as a block this layer can bind, without taking ownership of it.
    /// </summary>
    /// <param name="buffer">The <c>MTLBuffer</c>.</param>
    /// <param name="offset">The offset of the block inside the buffer, in bytes.</param>
    /// <param name="length">The length of the block, in bytes.</param>
    /// <param name="block">When the method returns, the block.</param>
    /// <returns><see langword="true"/> when the buffer is CPU addressable and the range fits it.</returns>
    public bool TryImport(nint buffer, nuint offset, nuint length, out UnifiedMemoryBlock block)
    {
        block = default;

        if (m_Disposed || buffer == 0 || length == 0)
        {
            return false;
        }

        var bufferLength = (nuint)MetalApi.SendUInt64(buffer, "length");

        if (offset > bufferLength || length > bufferLength - offset)
        {
            return false;
        }

        var host = MetalApi.Send(buffer, "contents");

        if (host == 0)
        {
            return false;
        }

        block = new UnifiedMemoryBlock(
            (void*)buffer,
            (byte*)host,
            (byte*)host + (long)offset,
            0,
            offset,
            length,
            bufferLength,
            UnifiedMemoryKind.SharedCoherent);

        return true;
    }

    /// <summary>
    /// Gets the <c>MTLDevice</c> the bridge allocates from.
    /// </summary>
    internal nint Device => m_Device;

    /// <summary>
    /// Creates a two dimensional texture on the device.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="usage">What the texture is created for.</param>
    /// <returns>The texture, to be disposed when it is no longer read.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the extent is not positive.</exception>
    /// <exception cref="MetalException">Thrown when Metal refuses the texture.</exception>
    public GpuTexture2D CreateTexture(int width, int height, GpuTextureFormat format, GpuTextureUsage usage = GpuTextureUsage.Sampled)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalUnifiedProvider));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A texture is at least one pixel wide.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "A texture is at least one pixel high.");
        }

        return new GpuTexture2D(new MetalTexture(m_Device, width, height, format, usage), width, height, format);
    }

    /// <summary>
    /// Wraps a texture the engine owns.
    /// </summary>
    /// <param name="texture">The <c>MTLTexture</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="imported">When the method returns, the texture view.</param>
    /// <returns><see langword="true"/> when the texture was wrapped.</returns>
    public bool TryImportTexture(nint texture, int width, int height, GpuTextureFormat format, out GpuTexture2D? imported)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalUnifiedProvider));
        }

        if (texture == 0 || width <= 0 || height <= 0)
        {
            imported = null;
            return false;
        }

        imported = new GpuTexture2D(new MetalTexture(texture, width, height), width, height, format);
        return true;
    }

    /// <summary>
    /// Creates a sampler state on the device.
    /// </summary>
    /// <param name="sampler">The sampler description.</param>
    /// <returns>The state, to be disposed when it is no longer bound.</returns>
    /// <exception cref="MetalException">Thrown when Metal refuses the sampler state.</exception>
    public GpuSamplerState CreateSampler(GpuSampler sampler)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalUnifiedProvider));
        }

        var filter = (nuint)(sampler.Filter == GpuTextureFilter.Linear ? 1 : 0);
        var address = (nuint)sampler.Address;
        var state = MetalApi.NewSamplerState(m_Device, filter, filter, address);

        if (state == 0)
        {
            throw new MetalException("Metal refused the sampler state.");
        }

        return new GpuSamplerState(new MetalSamplerState(state));
    }

    /// <inheritdoc/>
    public UnifiedProvider GetProvider()
    {
        if (!m_Self.IsAllocated)
        {
            throw new ObjectDisposedException(nameof(MetalUnifiedProvider));
        }

        return ProviderAdapter<MetalUnifiedProvider>.Create(this, ref m_Self);
    }

    /// <inheritdoc/>
    /// <exception cref="OutOfMemoryException">Thrown when Metal cannot create a buffer of that size.</exception>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(MetalUnifiedProvider));
        }

        var choice = MetalStoragePolicy.Choose(usage);
        var options = choice.StorageMode == MetalStoragePolicy.STORAGE_MODE_PRIVATE
            ? MetalApi.RESOURCE_STORAGE_MODE_PRIVATE
            : MetalApi.RESOURCE_STORAGE_MODE_SHARED | choice.CpuCacheMode;

        var buffer = MetalApi.NewBuffer(m_Device, size, options);
        if (buffer == 0)
        {
            throw new OutOfMemoryException($"Metal could not create a buffer of {size} bytes.");
        }

        // A private buffer has no CPU address, which is exactly how the allocator learns the segment is device only. A
        // shared buffer that reports none means the storage mode assumption did not hold, which is worth saying plainly
        // rather than returning null and looking like a device-only segment.
        void* host = null;

        if (choice.IsMapped)
        {
            host = (void*)MetalApi.Send(buffer, "contents");

            if (host == null)
            {
                MetalApi.Release(buffer);
                throw new MetalException($"Metal created the buffer of {size} bytes in shared storage but reported no CPU address for it.");
            }
        }

        m_Buffers.Add(buffer);

        deviceHandle = (void*)buffer;
        deviceAddress = 0;
        kind = choice.Kind;
        return host;
    }

    /// <inheritdoc/>
    public void Free(void* ptr, nuint size, void* deviceHandle)
    {
        if (deviceHandle == null)
        {
            return;
        }

        var buffer = (nint)deviceHandle;
        m_Buffers.Remove(buffer);
        MetalApi.Release(buffer);
    }

    /// <inheritdoc/>
    /// <remarks>Nothing to do: <c>MTLStorageModeShared</c> is coherent on every Mac.</remarks>
    public void Flush(void* ptr, nuint size) { }

    /// <inheritdoc/>
    /// <remarks>Nothing to do: <c>MTLStorageModeShared</c> is coherent on every Mac.</remarks>
    public void Invalidate(void* ptr, nuint size) { }

    /// <inheritdoc/>
    /// <remarks>
    /// Metal's blit copies need the offsets and the size to be multiples of four bytes, so a copy that is not aligned that
    /// way is refused rather than rounded, because rounding either drops bytes or moves bytes that belong to the block
    /// behind the range. Blocks the arena hands out are aligned far beyond that, so this only bites a partial copy that
    /// slices a block at an odd offset.
    /// </remarks>
    public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        if (m_Disposed || size == 0)
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

        if ((destinationOffset % MetalApi.COPY_ALIGNMENT) != 0 || (sourceOffset % MetalApi.COPY_ALIGNMENT) != 0 || (size % MetalApi.COPY_ALIGNMENT) != 0)
        {
            return false;
        }

        var sourceBuffer = (nint)source.DeviceHandle;
        var destinationBuffer = (nint)destination.DeviceHandle;

        if (sourceBuffer == 0 || destinationBuffer == 0)
        {
            return false;
        }

        // The offsets the allocator passes are relative to the block, and a block is packed at an offset inside its segment,
        // so both have to be added before Metal sees them.
        var absoluteSource = source.Offset + sourceOffset;
        var absoluteDestination = destination.Offset + destinationOffset;

        return defer
            ? RecordDeferred(sourceBuffer, absoluteSource, destinationBuffer, absoluteDestination, size)
            : CopyNow(sourceBuffer, absoluteSource, destinationBuffer, absoluteDestination, size);
    }

    /// <inheritdoc/>
    /// <remarks>Ends the open encoding and commits it. Returns <see langword="false"/> when nothing was recorded.</remarks>
    public bool Submit()
    {
        if (m_Disposed || m_Batch == 0)
        {
            return false;
        }

        MetalApi.SendVoid(m_BatchEncoder, "endEncoding");
        MetalApi.Release(m_BatchEncoder);
        m_BatchEncoder = 0;

        MetalApi.SendVoid(m_Batch, "commit");

        // The command buffer stays owned by the bridge until the caller waits for it, so an unwaited submission cannot be
        // reclaimed by an autorelease pool behind the caller's back.
        m_Committed = m_Batch;
        m_Batch = 0;
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>Waits for the outstanding submission. Returns <see langword="false"/> when there is none.</remarks>
    public bool Wait()
    {
        if (m_Disposed || m_Committed == 0)
        {
            return false;
        }

        MetalApi.SendVoid(m_Committed, "waitUntilCompleted");
        MetalApi.Release(m_Committed);
        m_Committed = 0;
        return true;
    }

    /// <summary>
    /// Records a blit into the open command buffer, opening one when there is none.
    /// </summary>
    /// <param name="sourceBuffer">The source <c>MTLBuffer</c>.</param>
    /// <param name="sourceOffset">The offset inside the source, in bytes.</param>
    /// <param name="destinationBuffer">The destination <c>MTLBuffer</c>.</param>
    /// <param name="destinationOffset">The offset inside the destination, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns><see langword="true"/> if the blit was recorded; otherwise, <see langword="false"/>.</returns>
    private bool RecordDeferred(nint sourceBuffer, nuint sourceOffset, nint destinationBuffer, nuint destinationOffset, nuint size)
    {
        // One batch at a time: a caller that records before waiting for the previous submission would otherwise get its
        // copies reordered behind its back.
        if (m_Committed != 0)
        {
            return false;
        }

        if (m_Batch == 0)
        {
            var pool = MetalApi.PushAutoreleasePool();

            try
            {
                var commandBuffer = MetalApi.Send(m_Queue, "commandBuffer");
                if (commandBuffer == 0)
                {
                    return false;
                }

                // Both are autoreleased by Metal, so they are retained to outlive the pool this call is about to pop.
                MetalApi.Retain(commandBuffer);
                m_Batch = commandBuffer;

                var encoder = MetalApi.Send(commandBuffer, "blitCommandEncoder");
                if (encoder == 0)
                {
                    MetalApi.Release(commandBuffer);
                    m_Batch = 0;
                    return false;
                }

                MetalApi.Retain(encoder);
                m_BatchEncoder = encoder;
            }
            finally
            {
                MetalApi.PopAutoreleasePool(pool);
            }
        }

        MetalApi.CopyBuffer(m_BatchEncoder, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
        return true;
    }

    /// <summary>
    /// Records a blit, submits it on its own command buffer and waits for it, which is what a non-deferred copy promises.
    /// </summary>
    /// <param name="sourceBuffer">The source <c>MTLBuffer</c>.</param>
    /// <param name="sourceOffset">The offset inside the source, in bytes.</param>
    /// <param name="destinationBuffer">The destination <c>MTLBuffer</c>.</param>
    /// <param name="destinationOffset">The offset inside the destination, in bytes.</param>
    /// <param name="size">The number of bytes to copy.</param>
    /// <returns><see langword="true"/> if the bytes were copied; otherwise, <see langword="false"/>.</returns>
    private bool CopyNow(nint sourceBuffer, nuint sourceOffset, nint destinationBuffer, nuint destinationOffset, nuint size)
    {
        var pool = MetalApi.PushAutoreleasePool();

        try
        {
            var commandBuffer = MetalApi.Send(m_Queue, "commandBuffer");
            if (commandBuffer == 0)
            {
                return false;
            }

            var encoder = MetalApi.Send(commandBuffer, "blitCommandEncoder");
            if (encoder == 0)
            {
                return false;
            }

            MetalApi.CopyBuffer(encoder, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, size);
            MetalApi.SendVoid(encoder, "endEncoding");
            MetalApi.SendVoid(commandBuffer, "commit");

            // The contract says a non-deferred copy has completed when the call returns.
            MetalApi.SendVoid(commandBuffer, "waitUntilCompleted");
            return true;
        }
        finally
        {
            MetalApi.PopAutoreleasePool(pool);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Always <see langword="false"/>: Metal has no residency control. <c>setPurgeableState:</c> exists but describes
    /// whether the contents may be discarded, not whether the pages are resident, so claiming support would make a caller
    /// skip work it actually has to do.
    /// </remarks>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Releases every buffer the bridge still owns, plus the command queue it created.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // Whatever is still recorded is finished before the buffers it reads disappear.
        if (m_BatchEncoder != 0)
        {
            MetalApi.SendVoid(m_BatchEncoder, "endEncoding");
            MetalApi.Release(m_BatchEncoder);
            m_BatchEncoder = 0;
        }

        if (m_Batch != 0)
        {
            MetalApi.SendVoid(m_Batch, "commit");
            MetalApi.Release(m_Batch);
            m_Batch = 0;
        }

        if (m_Committed != 0)
        {
            MetalApi.SendVoid(m_Committed, "waitUntilCompleted");
            MetalApi.Release(m_Committed);
            m_Committed = 0;
        }

        foreach (var buffer in m_Buffers)
        {
            MetalApi.Release(buffer);
        }

        m_Buffers.Clear();

        if (m_OwnsQueue)
        {
            MetalApi.Release(m_Queue);
        }

        if (m_Self.IsAllocated)
        {
            m_Self.Free();
        }
    }

}

/// <summary>
/// Thrown when Metal refuses an operation the bridge cannot recover from.
/// </summary>
public sealed class MetalException : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public MetalException(string message) : base(message)
    {
    }

    /// <param name="message">The message that describes the failure.</param>
    /// <param name="innerException">The exception that caused it.</param>
    public MetalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

