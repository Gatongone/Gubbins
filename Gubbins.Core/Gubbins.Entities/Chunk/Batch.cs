using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Unmanaged;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Represents a logical sequence of elements stored across multiple contiguous snippets.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public struct Batch<T> : IEnumerable<T>
{
    /// <summary>
    /// Gets the span for the snippet at the specified segment index.
    /// </summary>
    /// <param name="index">The segment index.</param>
    public Span<T> this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Data[index].Span;
    }

    /// <summary>
    /// Gets the total number of elements across all segments.
    /// </summary>
    public readonly int ElementCount;

    /// <summary>
    /// Gets the number of underlying segments.
    /// </summary>
    public readonly int SegmentCount;

    private Ulist<Snippet<T>> m_Data;

    /// <param name="data">The snippets to expose as a batch.</param>
    public Batch(Ulist<Snippet<T>> data) : this(data, GetElementCount(data)) { }

    /// <param name="data">The snippets to expose as a batch.</param>
    /// <param name="elementCount">
    /// The total element count, when the caller already accumulated it while filling <paramref name="data"/>.
    /// Passing it skips a full pass over the snippets.
    /// </param>
    internal Batch(Ulist<Snippet<T>> data, int elementCount)
    {
        m_Data       = data;
        ElementCount = elementCount;
        SegmentCount = data.Count;
    }

    /// <summary>
    /// Counts the elements without going through LINQ, which would box the list enumerator.
    /// </summary>
    /// <param name="data">The snippets to count.</param>
    /// <returns>The total number of elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetElementCount(Ulist<Snippet<T>> data)
    {
        var count = 0;
        for (var i = 0; i < data.Count; i++)
        {
            count += data[i].Length;
        }

        return count;
    }

    /// <summary>
    /// Gets a reference to the element at the specified flattened element index.
    /// </summary>
    /// <param name="elementIndex">The zero-based index across all segments.</param>
    /// <returns>A reference to the requested element.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="elementIndex"/> does not map to an element in the batch.
    /// </exception>
    public readonly ref T GetElement(int elementIndex)
    {
        for (var i = 0; i < m_Data.Count; i++)
        {
            var snippet = m_Data[i];
            if (snippet.Length > elementIndex)
            {
                // Use the snippet indexer instead of materializing a Span for every lookup.
                return ref snippet[elementIndex];
            }

            elementIndex -= snippet.Length;
        }

        throw new ArgumentOutOfRangeException(nameof(elementIndex));
    }

    /// <summary>
    /// Returns an enumerator that iterates through all elements in the batch.
    /// </summary>
    public Enumerator GetEnumerator() => new(ref m_Data);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerates elements across all snippets in the batch.
    /// </summary>
    public struct Enumerator(ref Ulist<Snippet<T>> data) : IEnumerator<T>
    {
        private readonly Ulist<Snippet<T>> m_Data = data;

        /// <summary>
        /// The snippet currently being enumerated. Cached so <see cref="MoveNext"/> and <see cref="Current"/>
        /// do not have to index the list (and materialize a span) for every element.
        /// </summary>
        private Snippet<T> m_Snippet;

        private int m_SnippetLength;
        private int m_SnippetIndex = 0;
        private int m_ElementIndex = -1;

        /// <summary>
        /// Advances to the next element in the batch.
        /// </summary>
        /// <returns><see langword="true"/> if the enumerator advanced; otherwise <see langword="false"/>.</returns>
        /// <inheritdoc />
        public bool MoveNext()
        {
            while (true)
            {
                if (++m_ElementIndex < m_SnippetLength)
                {
                    return true;
                }

                if (m_SnippetIndex >= m_Data.Count)
                {
                    return false;
                }

                m_Snippet       = m_Data[m_SnippetIndex];
                m_SnippetLength = m_Snippet.Length;
                m_SnippetIndex++;
                m_ElementIndex = -1;
            }
        }

        /// <summary>
        /// Resets the enumerator to the initial position.
        /// </summary>
        /// <inheritdoc />
        public void Reset()
        {
            m_Snippet       = default;
            m_SnippetLength = 0;
            m_SnippetIndex  = 0;
            m_ElementIndex  = -1;
        }

        /// <summary>
        /// Gets a reference to the current element.
        /// </summary>
        public ref T Current => ref m_Snippet[m_ElementIndex];

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