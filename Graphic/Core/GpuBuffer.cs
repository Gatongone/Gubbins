using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// A block of memory the device works on, with the element count and the access the caller asked for.
/// </summary>
/// <remarks>
/// <para>
/// The type is the typed face of <see cref="GpuBlock"/>: the byte length is the element count times the size of the
/// element, and the central processor reads the block as a span of those elements instead of as bytes it casts itself.
/// It is the shape the generated entry point of a kernel takes, so a dispatch does not pass a raw size or a block the
/// caller built by hand.
/// </para>
/// <para>
/// A buffer is disposed when the program is done with it, which returns the block to the memory it came from. Reading it
/// after the device wrote it is refused until <see cref="Gpu.Synchronize"/> has returned, which is what keeps a stale
/// value from being read as if it were the result.
/// </para>
/// </remarks>
public abstract class GpuBuffer : IDisposable
{
    private readonly GpuMemory m_Memory;
    private GpuBlock? m_Block;
    private bool m_Disposed;

    /// <param name="memory">The memory the block is allocated from.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="elementSize">The size of one element, in bytes.</param>
    /// <param name="access">Whether the central processor writes the block directly.</param>
    private protected GpuBuffer(GpuMemory memory, int count, int elementSize, GpuAccess access)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "A buffer holds a non negative number of elements.");
        }

        if (elementSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elementSize), elementSize, "An element takes at least one byte.");
        }

        Length = count;
        ElementSize = elementSize;
        m_Memory = memory;
        m_Block = memory.Allocate((nuint) ((long) count * elementSize), access);
    }

    /// <summary>
    /// Gets the number of elements the buffer holds.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the size of one element, in bytes.
    /// </summary>
    internal int ElementSize { get; }

    /// <summary>
    /// Gets the number of bytes the buffer holds.
    /// </summary>
    public int ByteLength => checked(Length * ElementSize);

    /// <summary>
    /// Gets what the central processor is allowed to do with the buffer.
    /// </summary>
    public GpuAccess Access => Block.Access;

    /// <summary>
    /// Gets the block the device works on, for the kernel that dispatches over it.
    /// </summary>
    internal GpuBlock Block => m_Block ?? throw new ObjectDisposedException(GetType().Name);

    /// <summary>
    /// Gets the memory the buffer was allocated from.
    /// </summary>
    internal GpuMemory Memory => m_Memory;

    /// <summary>
    /// Records that the central processor wrote the buffer, so the device's view is published at the next
    /// synchronization on a machine whose pages are not coherent.
    /// </summary>
    public void MarkWrittenByCpu() => m_Memory.MarkWrittenByCpu(Block);

    /// <summary>
    /// Records that the device wrote the buffer, so the central processor cannot read it until the next synchronization.
    /// </summary>
    public void MarkWrittenByGpu() => m_Memory.MarkWrittenByGpu(Block);

    /// <summary>
    /// Writes bytes into the buffer, which is what a typed buffer narrows to its own elements.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    private protected void Upload(ReadOnlySpan<byte> data) => m_Memory.Upload(Block, data);

    /// <summary>
    /// Releases the block.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        if (m_Block is { } block)
        {
            m_Memory.Release(block);
            m_Block = null;
        }
    }
}

/// <summary>
/// A buffer read as elements of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The element type, which is an unmanaged value.</typeparam>
public sealed class GpuBuffer<T> : GpuBuffer where T : unmanaged
{
    /// <param name="memory">The memory the block is allocated from.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="access">Whether the central processor writes the block directly.</param>
    internal GpuBuffer(GpuMemory memory, int count, GpuAccess access) : base(memory, count, Size, access)
    {
    }

    /// <summary>
    /// Gets the size of one element, in bytes.
    /// </summary>
    private static unsafe int Size => sizeof(T);

    /// <summary>
    /// Gets the contents as the central processor reads them.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the device wrote the buffer after the last synchronization point, or when the central processor has no
    /// view of it.
    /// </exception>
    public Span<T> CpuSpan => MemoryMarshal.Cast<byte, T>(Block.CpuView()).Slice(0, Length);

    /// <summary>
    /// Writes elements into the buffer from the central processor.
    /// </summary>
    /// <param name="values">The elements to write.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the values do not fit.</exception>
    public void Upload(ReadOnlySpan<T> values)
    {
        if (values.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(values), values.Length, "The values do not fit in the buffer.");
        }

        Upload(MemoryMarshal.AsBytes(values));
    }

    /// <summary>
    /// Copies the contents into an array, which is the shape a test or a caller that outlives the buffer keeps.
    /// </summary>
    /// <returns>The elements.</returns>
    public T[] ToArray() => CpuSpan.ToArray();
}
