using System.Runtime.CompilerServices;

namespace Gubbins.Unsafe;

/// <summary>
/// Represents a contiguous region of unmanaged memory exposed as a span.
/// </summary>
/// <remarks>
/// The snippet is a view only; it does not own the memory. The owner (typically an allocator-backed type such as a chunk)
/// must keep the underlying memory alive for as long as the snippet is in use.
/// </remarks>
/// <typeparam name="T">The element type of the snippet.</typeparam>
public readonly unsafe struct Snippet<T>
{
    /// <summary>
    /// The size in bytes of a single element of type <typeparamref name="T"/>.
    /// </summary>
    private static readonly int elementSize = (int)Native.GetStackSize<T>();

    /// <summary>
    /// The address of the first element.
    /// </summary>
    private readonly void* m_Pointer;

    /// <summary>
    /// The number of <typeparamref name="T"/> elements in the region.
    /// </summary>
    private readonly int m_Length;

    /// <summary>
    /// Initializes a snippet over a region of unmanaged memory.
    /// </summary>
    /// <param name="pointer">The address of the first element.</param>
    /// <param name="length">The number of elements of type <typeparamref name="T"/>.</param>
    public Snippet(void* pointer, int length)
    {
        m_Pointer = pointer;
        m_Length  = length;
    }

    /// <summary>
    /// Gets the region as a span.
    /// </summary>
    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Length == 0 ? Span<T>.Empty : new Span<T>(m_Pointer, m_Length);
    }

    /// <summary>
    /// Gets the number of <typeparamref name="T"/> elements in the snippet.
    /// </summary>
    public int Length => m_Length;

    /// <summary>
    /// Gets a reference to the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Native.AsRef<T>((byte*)m_Pointer + (nint)index * elementSize);
    }

    /// <summary>
    /// Reinterprets the same memory region as a snippet of another element type.
    /// </summary>
    /// <typeparam name="TTo">The target element type.</typeparam>
    /// <returns>A snippet over the same memory using <typeparamref name="TTo"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snippet<TTo> Cast<TTo>()
    {
        var byteLength = m_Length * elementSize;
        return new Snippet<TTo>(m_Pointer, byteLength / Snippet<TTo>.elementSize);
    }
}
