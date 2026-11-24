using System.ComponentModel;

namespace Gubbins.Graphic;

/// <summary>
/// What the central processor is allowed to do with a block.
/// </summary>
/// <remarks>
/// <para>
/// The two are a trade, and which side wins is a property of the machine rather than of this layer. Where the device has
/// memory of its own, a shared block is host memory the device reaches across the bus: the kernel runs at the speed of
/// that link and no copy is ever made. A block meant only for the device lives in that memory, where the same kernel
/// runs several times faster, and the central processor pays for a copy at every synchronization point instead.
/// </para>
/// <para>
/// Where both processors address one pool of memory, both kinds are that pool and the difference is small, so the
/// choice costs little either way. For a pass whose input changes every frame, the copy a <see cref="Sync"/> block pays
/// can cost more than the bandwidth a <see cref="Shared"/> one gives up; for a pass over data that stays resident, the
/// device block is the one that keeps the kernel fed.
/// </para>
/// </remarks>
public enum GpuAccess : byte
{
    /// <summary>
    /// The block is read and written by both processors (CPU/GPU), with no copy between them.
    /// </summary>
    Shared = 0,

    /// <summary>
    /// The block is only meant for the graphics device. The central processor can still read it, but only at a synchronization (<see cref="GpuMemory.Synchronize()"/>)
    /// point, and only through a copy the layer makes.
    /// </summary>
    Sync = 1
}

/// <summary>
/// A block allocated by <see cref="GpuMemory"/>: the memory the device works on, plus the memory the central processor
/// reads, which are the same block unless the machine cannot share them.
/// </summary>
/// <remarks>
/// The block is what a kernel binds, and what the layer copies back at a synchronization point. The surface a program
/// uses is <see cref="GpuBuffer{T}"/>, which is a block read as elements of a type; this type is hidden from completion
/// for that reason.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class GpuBlock
{
    /// <param name="storage">The block the device works on.</param>
    /// <param name="readable">The block the central processor reads.</param>
    /// <param name="access">What the central processor is allowed to do with it.</param>
    internal GpuBlock(UnifiedMemoryBlock storage, UnifiedMemoryBlock readable, GpuAccess access)
    {
        Storage = storage;
        Readable = readable;
        Access = access;
    }

    /// <summary>
    /// Gets the block the device works on.
    /// </summary>
    public UnifiedMemoryBlock Storage { get; }

    /// <summary>
    /// Gets the block the central processor reads, which is <see cref="Storage"/> when the machine shares the memory.
    /// </summary>
    public UnifiedMemoryBlock Readable { get; }

    /// <summary>
    /// Gets what the central processor is allowed to do with this block.
    /// </summary>
    public GpuAccess Access { get; }

    /// <summary>
    /// Gets the size of the block, in bytes.
    /// </summary>
    public nuint Length => Storage.Length;

    /// <summary>
    /// Gets a value indicating whether the central processor has a block of its own to read.
    /// </summary>
    public bool IsCpuReadable => Readable.Host != null;

    /// <summary>
    /// Gets the block's contents as the central processor sees them.
    /// </summary>
    /// <returns>The contents.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the block has no memory the central processor can read, or when the device wrote it after the last
    /// synchronization point.
    /// </exception>
    public Span<byte> CpuView()
    {
        if (Readable.Host == null)
        {
            throw new InvalidOperationException("This block has no memory the central processor can read.");
        }

        if (WrittenByGpu)
        {
            throw new InvalidOperationException("The device wrote this block after the last synchronization point.");
        }

        return new Span<byte>(Readable.Host, checked((int)Readable.Length));
    }

    /// <summary>
    /// Gets a value indicating whether the device wrote the block since the last synchronization point.
    /// </summary>
    internal bool WrittenByGpu { get; set; }

    /// <summary>
    /// Gets a value indicating whether the central processor wrote the block since the last synchronization point.
    /// </summary>
    internal bool WrittenByCpu { get; set; }
}

/// <summary>
/// The memory a device works on, presented as blocks whose contents are stable between synchronization points rather
/// than as blocks that belong to a particular kind of heap.
/// </summary>
/// <remarks>
/// <para>
/// This is the layer a caller uses when it does not want to know what a "shared heap" or a "device local heap" is: it
/// asks for a block, marks it when a processor wrote it, and reads it after a synchronization point. Which of the two
/// kinds of memory serves a block, whether the central processor can address the device's copy of it, and whether the
/// pages have to be published before the device reads them are all decided here, per machine.
/// </para>
/// <para>
/// On a unified memory machine a block is one allocation both processors address, so a synchronization point only waits.
/// On a discrete machine the same block is still one allocation the device reaches over the interconnect, which is slower
/// than the device's own memory but costs no copy; only a <see cref="GpuAccess.Sync"/> block, which lives in the
/// device's memory, has to be copied back before the central processor reads it.
/// </para>
/// <para>
/// A block's contents are only guaranteed at a synchronization point. Reading a block the device wrote before
/// <see cref="Synchronize"/> has returned is refused rather than answered with stale bytes.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed unsafe class GpuMemory : IDisposable
{
    private UnifiedAllocator m_Allocator;
    private readonly List<GpuBlock> m_Live = [];

    /// <summary>
    /// The number of blocks that were live at the last frame reset.
    /// </summary>
    private int m_FrameMark;
    private bool m_Disposed;

    /// <param name="provider">The provider to allocate through.</param>
    /// <param name="segmentSize">The size a segment grows by.</param>
    /// <exception cref="ArgumentException">Thrown when the provider is not valid.</exception>
    public GpuMemory(UnifiedProvider provider, nuint segmentSize)
    {
        m_Allocator = new UnifiedAllocator(provider, segmentSize);
    }

    /// <summary>
    /// Gets the allocator underneath.
    /// </summary>
    /// <remarks>
    /// This is internal on purpose: a caller that can reach it starts choosing heaps and maintenance again, which is the
    /// one thing this layer exists to hide. It is handed out by reference because it is a mutable structure that keeps
    /// its segment bookkeeping in itself.
    /// </remarks>
    internal ref UnifiedAllocator Allocator => ref m_Allocator;

    /// <summary>
    /// Allocates a block.
    /// </summary>
    /// <param name="length">The size to allocate, in bytes.</param>
    /// <param name="access">What the central processor is allowed to do with it.</param>
    /// <returns>The block.</returns>
    /// <exception cref="NotSupportedException">Thrown when the device cannot provide memory of the requested access.</exception>
    /// <exception cref="OutOfMemoryException">Thrown when the device is out of memory.</exception>
    public GpuBlock Allocate(nuint length, GpuAccess access)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        if (access == GpuAccess.Shared)
        {
            var shared = m_Allocator.AllocBlock(length, UnifiedMemoryUsage.General);

            if (shared.Host == null)
            {
                m_Allocator.TryFree(shared);
                throw new NotSupportedException("This device has no memory both processors can address.");
            }

            return Track(new GpuBlock(shared, shared, GpuAccess.Shared));
        }

        // A block meant only for the device is the device's own memory when the machine has it. When that memory is
        // still addressable by the central processor there is nothing to copy and no second block.
        var storage = m_Allocator.AllocBlock(length, UnifiedMemoryUsage.Device);

        if (storage.Host != null)
        {
            return Track(new GpuBlock(storage, storage, GpuAccess.Sync));
        }

        var readable = m_Allocator.AllocBlock(length, UnifiedMemoryUsage.General);

        if (readable.Host == null)
        {
            m_Allocator.TryFree(readable);
            m_Allocator.TryFree(storage);
            throw new NotSupportedException("This device has no memory the central processor can read.");
        }

        return Track(new GpuBlock(storage, readable, GpuAccess.Sync));
    }

    /// <summary>
    /// Writes data into a block starting at an offset.
    /// </summary>
    /// <param name="block">The block to write.</param>
    /// <param name="offset">The offset inside the block to start at.</param>
    /// <param name="data">The bytes to write.</param>
    /// <returns><see langword="true"/> if the data reached the block; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// This is the shape a component column takes: one block per chunk, one byte range per component, so the offset is the
    /// column's start plus the element's index times its size.
    /// </remarks>
    public bool Upload(GpuBlock block, nuint offset, ReadOnlySpan<byte> data)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        if (offset > block.Length || (nuint)data.Length > block.Length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "The data does not fit in the block at that offset.");
        }

        block.WrittenByCpu = true;

        if (block.Storage.Host != null)
        {
            data.CopyTo(new Span<byte>(block.Storage.Host + offset, data.Length));
            return true;
        }

        return m_Allocator.TryUpload(block.Storage, offset, data);
    }

    /// <summary>
    /// Writes data into a block from the central processor.
    /// </summary>
    /// <param name="block">The block to write.</param>
    /// <param name="data">The bytes to write.</param>
    /// <returns><see langword="true"/> if the data reached the block; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// A block the device cannot see is filled through a copy the layer stages and submits, which is retired at the next
    /// <see cref="Synchronize"/>. The write is recorded as the central processor's, so a machine with non-coherent pages
    /// publishes it there.
    /// </remarks>
    public bool Upload(GpuBlock block, ReadOnlySpan<byte> data){
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        if ((nuint)data.Length > block.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "The data does not fit in the block.");
        }

        block.WrittenByCpu = true;

        // The device can address this block itself, so the central processor can write it directly.
        if (block.Storage.Host != null)
        {
            data.CopyTo(new Span<byte>(block.Storage.Host, data.Length));
            return true;
        }

        return m_Allocator.TryUpload(block.Storage, data);
    }

    /// <summary>
    /// Releases a block.
    /// </summary>
    /// <param name="block">The block to release.</param>
    /// <returns><see langword="true"/> if the block was released; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// A <see cref="GpuAccess.Sync"/> block that is still waiting for a copy back is released without one: the block
    /// is gone, so there is nothing left to read.
    /// </remarks>
    public bool Release(GpuBlock block)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        var released = m_Allocator.TryFree(block.Storage);

        if (block.Readable.Host != block.Storage.Host)
        {
            released &= m_Allocator.TryFree(block.Readable);
        }

        m_Live.Remove(block);
        return released;
    }

    /// <summary>
    /// Records that the central processor wrote a block, so the device's view of it is published at the next
    /// synchronization point on machines whose pages are not coherent.
    /// </summary>
    /// <param name="block">The block that was written.</param>
    public void MarkWrittenByCpu(GpuBlock block)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        block.WrittenByCpu = true;
    }

    /// <summary>
    /// Records that the device wrote a block, so the central processor cannot read it until the next synchronization
    /// point, and so a block it cannot address is copied back there.
    /// </summary>
    /// <param name="block">The block that was written.</param>
    /// <remarks>
    /// The operation that wrote the block has to be complete when <see cref="Synchronize"/> is called: this layer owns
    /// the transfer queue, not the queue a dispatch ran on.
    /// </remarks>
    public void MarkWrittenByGpu(GpuBlock block)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block));
        }

        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        block.WrittenByGpu = true;
    }

    /// <summary>
    /// Makes the writes recorded since the last synchronization point visible to the other processor.
    /// </summary>
    /// <returns><see langword="true"/> if every write is now visible; otherwise, <see langword="false"/>.</returns>
    public bool Synchronize()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        var copies = false;

        for (var i = 0; i < m_Live.Count; i++)
        {
            var block = m_Live[i];

            // Publish a CPU write on a machine whose pages the device does not share coherently.
            if (block.WrittenByCpu && block.Storage.Host != null && !block.Storage.IsCoherent)
            {
                m_Allocator.Flush(block.Storage.Host, block.Storage.Length);
            }

            // Copy a device write back when the central processor has no view of it.
            if (block.WrittenByGpu && block.Storage.Host == null)
            {
                if (block.Readable.Host == null ||
                    !m_Allocator.TryCopyDeferred(block.Readable, 0, block.Storage, 0, block.Storage.Length))
                {
                    return false;
                }

                copies = true;
            }
        }

        if (copies && (!m_Allocator.SubmitCopies() || !m_Allocator.WaitForCopies()))
        {
            return false;
        }

        // Invalidate after the copies have run, so a device write lands in the central processor's view of the block.
        for (var i = 0; i < m_Live.Count; i++)
        {
            var block = m_Live[i];

            if (block.WrittenByGpu && block.Readable.Host != null && !block.Readable.IsCoherent)
            {
                m_Allocator.Invalidate(block.Readable.Host, block.Readable.Length);
            }

            block.WrittenByGpu = false;
            block.WrittenByCpu = false;
        }

        return true;
    }

    /// <summary>
    /// Releases every block allocated since the last frame reset.
    /// </summary>
    /// <returns>The number of blocks that were released.</returns>
    public int ResetFrame()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        var released = 0;

        for (var i = m_Live.Count - 1; i >= m_FrameMark; i--)
        {
            var block = m_Live[i];
            m_Allocator.TryFree(block.Storage);

            if (block.Readable.Host != block.Storage.Host)
            {
                m_Allocator.TryFree(block.Readable);
            }

            m_Live.RemoveAt(i);
            released++;
        }

        m_FrameMark = m_Live.Count;
        return released;
    }

    /// <summary>
    /// Releases the segments no live block occupies back to the device.
    /// </summary>
    /// <returns>The number of segments that were released.</returns>
    public int Trim()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuMemory));
        }

        return m_Allocator.Trim();
    }
    /// <summary>
    /// Releases every block this layer still owns.
    /// </summary>
    /// <remarks>
    /// The provider belongs to whoever created it, so it is not disposed here.
    /// </remarks>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        for (var i = 0; i < m_Live.Count; i++)
        {
            m_Allocator.TryFree(m_Live[i].Storage);

            if (m_Live[i].Readable.Host != m_Live[i].Storage.Host)
            {
                m_Allocator.TryFree(m_Live[i].Readable);
            }
        }

        m_Live.Clear();

        // Freeing a block returns its range to the allocator, not the memory to the device: the segments an allocator
        // acquired are released by its own handle, which is this one's.
        m_Allocator.GetAllocator().Dispose();
    }

    /// <summary>
    /// Adds a block to the list the synchronization point walks.
    /// </summary>
    /// <param name="block">The block to track.</param>
    /// <returns>The block.</returns>
    private GpuBlock Track(GpuBlock block)
    {
        m_Live.Add(block);
        return block;
    }
}