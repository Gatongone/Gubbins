using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gubbins.Unsafe;

/// <summary>
/// Represents a pinned contiguous region of managed memory backed by an array and exposed as a span.
/// </summary>
/// <typeparam name="T">The element type of the snippet.</typeparam>
public readonly unsafe struct Snippet<T>
{
    /// <summary>
    /// The size in bytes of a single element of type <typeparamref name="T"/>. This is calculated using the Native.GetStackSize method
    /// and is used for calculating byte offsets when accessing elements within the pinned memory region.
    /// </summary>
    private static readonly int s_ElementSize = (int) Native.GetStackSize<T>();

    /// <summary>
    /// A GCHandle that holds a pinned reference to the backing array or memory region.
    /// This handle is used to ensure that the memory remains fixed in place while the snippet is in use,
    /// allowing for safe access to the underlying data without the risk of it being moved by the garbage collector.
    /// </summary>
    private readonly GCHandle m_Handle;

    /// <summary>
    /// The byte offset from the start of the pinned memory region to the beginning of the snippet.
    /// This is calculated based on the specified range and the size of the element type.
    /// </summary>
    private readonly int m_Start;

    /// <summary>
    /// The byte length of the snippet, which is determined by the number of elements in the specified range multiplied by the size of each element.
    /// </summary>
    private readonly int m_Length;

    /// <summary>
    /// The byte offset from the base address of the pinned object to the first element of the array.
    /// This is necessary because the actual data may not start at the base address of the object due to object headers or other metadata.
    /// </summary>
    private readonly int m_Offset;

    /// <summary>
    /// Gets the pinned region as a span.
    /// </summary>
    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (m_Length == 0) return new Span<T>();
            var ptr = (byte*) Native.GetAddress(m_Handle.Target!);
            return new Span<T>(ptr + m_Offset + m_Start, Length);
        }
    }

    /// <summary>
    /// Gets the number of <typeparamref name="T"/> elements in the snippet.
    /// </summary>
    public int Length => m_Length / s_ElementSize;

    /// <summary>
    /// Initializes a snippet covering the entire array.
    /// </summary>
    /// <param name="array">The source array.</param>
    /// <param name="handle">A pinned handle for <paramref name="array"/>.</param>
    public Snippet(T[] array, GCHandle handle) : this(array, new Range(0, array.Length), handle) { }

    /// <summary>
    /// Initializes a snippet covering the specified range within the array.
    /// </summary>
    /// <param name="array">The source array.</param>
    /// <param name="range">The range to expose.</param>
    /// <param name="handle">A pinned handle for <paramref name="array"/>.</param>
    public Snippet(T[] array, Range range, GCHandle handle)
    {
        m_Handle = handle;
        var size = (int) Native.GetStackSize(typeof(T));
        m_Start  = range.Start.Value * size;
        m_Length = (range.End.Value - range.Start.Value) * size;
        if (array.Length > 0)
        {
            m_Offset = (int) Native.GetFirstElementAddress(array) - (int) Native.GetAddress(array);
        }
    }

    /// <summary>
    /// Initializes a snippet using raw byte offsets within the pinned array storage.
    /// </summary>
    /// <param name="array">The source array.</param>
    /// <param name="start">The byte offset at which the snippet begins.</param>
    /// <param name="length">The byte length of the snippet.</param>
    /// <param name="handle">A pinned handle for <paramref name="array"/>.</param>
    public Snippet(T[] array, int start, int length, GCHandle handle)
    {
        m_Handle = handle;
        m_Start  = start;
        m_Length = length;
        if (array.Length > 0)
        {
            m_Offset = (int) Native.GetFirstElementAddress(array) - (int) Native.GetAddress(array);
        }
    }

    /// <summary>
    /// Initializes a snippet from an existing pinned memory region.
    /// </summary>
    /// <param name="handle">The pinned handle for the backing object.</param>
    /// <param name="start">The byte offset at which the snippet begins.</param>
    /// <param name="length">The byte length of the snippet.</param>
    /// <param name="offset">The byte offset from the object base to the first element.</param>
    private Snippet(GCHandle handle, int start, int length, int offset = 16)
    {
        m_Handle = handle;
        m_Start  = start;
        m_Length = length;
        m_Offset = offset;
    }

    /// <summary>
    /// Reinterprets the same pinned memory region as a snippet of another element type.
    /// </summary>
    /// <typeparam name="TTo">The target element type.</typeparam>
    /// <returns>A snippet over the same memory using <typeparamref name="TTo"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snippet<TTo> Cast<TTo>() => new(m_Handle, m_Start, m_Length, m_Offset);
}