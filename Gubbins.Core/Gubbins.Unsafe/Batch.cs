using System.Collections;
using System.Runtime.CompilerServices;

namespace Gubbins.Unsafe;

/// <summary>
/// Represents a logical sequence of elements stored across multiple contiguous snippets.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public readonly struct Batch<T>(List<Snippet<T>> data) : IEnumerable<T>
{
    /// <summary>
    /// Gets the span for the snippet at the specified segment index.
    /// </summary>
    /// <param name="index">The segment index.</param>
    public Span<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => data[index].Span;
    }

    /// <summary>
    /// Gets the total number of elements across all segments.
    /// </summary>
    public readonly int ElementCount = data.Sum(static x => x.Length);

    /// <summary>
    /// Gets the number of underlying segments.
    /// </summary>
    public readonly int SegmentCount = data.Count;

    /// <summary>
    /// Gets a reference to the element at the specified flattened element index.
    /// </summary>
    /// <param name="elementIndex">The zero-based index across all segments.</param>
    /// <returns>A reference to the requested element.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="elementIndex"/> does not map to an element in the batch.
    /// </exception>
    public ref T GetElement(int elementIndex)
    {
        for (var i = 0; i < data.Count; i++)
        {
            if (data[i].Length > elementIndex)
            {
                return ref data[i].Span[elementIndex];
            }

            elementIndex -= data[i].Length;
        }

        throw new ArgumentOutOfRangeException(nameof(elementIndex));
    }

    /// <summary>
    /// Returns an enumerator that iterates through all elements in the batch.
    /// </summary>
    public Enumerator GetEnumerator() => new(data);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerates elements across all snippets in the batch.
    /// </summary>
    public struct Enumerator(List<Snippet<T>> data) : IEnumerator<T>
    {
        private readonly List<Snippet<T>> m_Data = data;

        private int m_SnippetIndex = 0;
        private int m_ElementIndex = 0;

        /// <summary>
        /// Advances to the next element in the batch.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator advanced; otherwise <see langword="false"/>.</returns>
        /// <inheritdoc />
        public bool MoveNext()
        {
            if (m_Data[m_SnippetIndex].Length > m_ElementIndex++)
            {
                return true;
            }

            m_ElementIndex = 0;
            return ++m_SnippetIndex < m_Data.Count;
        }

        /// <summary>
        /// Resets the enumerator to the initial position.
        /// </summary>
        /// <inheritdoc />
        public void Reset()
        {
            m_SnippetIndex = 0;
            m_ElementIndex = 0;
        }

        /// <summary>
        /// Gets a reference to the current element.
        /// </summary>
        public ref T Current => ref m_Data[m_SnippetIndex].Span[m_ElementIndex];

        /// <inheritdoc />
        T IEnumerator<T>.Current => Current;

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <summary>
        /// Releases resources associated with the enumerator.
        /// </summary>
        /// <inheritdoc />
        public void Dispose() { }
    }
}