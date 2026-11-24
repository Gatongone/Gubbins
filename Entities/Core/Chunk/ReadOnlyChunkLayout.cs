using System.Runtime.CompilerServices;
using Gubbins.Unmanaged;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Read-only view over the components of type <typeparamref name="T"/> across the chunks of a query.
/// </summary>
/// <typeparam name="T">Component or entity type.</typeparam>
/// <remarks>
/// <para>
/// The reader's counterpart of <see cref="Components{T}"/>: it is obtained from
/// <see cref="Chunks.GetReadOnlyLayout{T}"/>, exposes <see cref="ReadOnlySpan{T}"/> segments and
/// <c>ref readonly</c> elements, and never marks a chunk as changed. A reader therefore cannot mutate component data,
/// and cannot make a chunk look hot to another system's <see cref="Chunks.ChangedSince"/> filter.
/// </para>
/// <para>
/// The component data belongs to the chunks, but the snippet list this value walks is unmanaged memory it owns itself,
/// so dispose it like the write-side counterpart.
/// </para>
/// </remarks>
public struct ReadOnlyChunkLayout<T> : IDisposable where T : unmanaged
{
    /// <summary>
    /// A batch representing the components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.
    /// </summary>
    internal readonly Batch<T> Batch;

    /// <summary>
    /// Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.
    /// </summary>
    private Ulist<Snippet<T>> m_Blocks;

    /// <summary>
    /// Gets the total number of components of type <typeparamref name="T"/> across all chunks.
    /// </summary>
    public readonly int Count => Batch.ElementCount;

    /// <summary>
    /// Gets the contiguous snippets of type <typeparamref name="T"/> as read-only spans, one per chunk.
    /// </summary>
    public readonly SegmentsEnumerable Segments => new(Batch);

    /// <summary>
    /// Gets every component of type <typeparamref name="T"/> as a read-only reference, flattened across the chunks.
    /// </summary>
    public readonly ElementsEnumerable Elements => new(Batch);

    /// <summary>
    /// Gets the same snippets as raw addresses, which is what a read-only buffer binding takes: a device needs the
    /// address, the element count and the stride, not a span.
    /// </summary>
    public readonly SegmentAddressEnumerable<T> Addresses => new(Batch);

    /// <param name="blocks">The snippet list the query filled.</param>
    /// <param name="elementCount">The total element count, already accumulated while the query filled the list.</param>
    internal ReadOnlyChunkLayout(ref Ulist<Snippet<T>> blocks, int elementCount)
    {
        Batch = new Batch<T>(blocks, elementCount);
        m_Blocks = blocks;
    }

    /// <summary>
    /// Releases the snippet list, which is unmanaged memory owned by this value. The component data itself stays owned
    /// by the chunks.
    /// </summary>
    public void Dispose() => m_Blocks.Dispose();

    /// <summary>
    /// An enumerable that provides the contiguous snippets of type <typeparamref name="T"/> as read-only spans.
    /// </summary>
    /// <param name="batch">A batch representing the components across multiple chunks.</param>
    public readonly struct SegmentsEnumerable(Batch<T> batch)
    {
        /// <summary>
        /// Gets the read-only span for the snippet at the specified segment index.
        /// </summary>
        /// <param name="index">The segment index.</param>
        public ReadOnlySpan<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => batch[index];
        }

        /// <summary>
        /// Gets the number of underlying segments.
        /// </summary>
        public readonly int Count = batch.SegmentCount;

        /// <summary>
        /// Gets an enumerator over the read-only spans.
        /// </summary>
        public SegmentsEnumerator GetEnumerator() => new(batch);

        /// <summary>
        /// Enumerates the read-only spans of the segments.
        /// </summary>
        /// <param name="batch">A batch representing the components across multiple chunks.</param>
        public struct SegmentsEnumerator(Batch<T> batch)
        {
            private int m_CurrentIndex;

            /// <summary>
            /// Releases the enumerator.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => m_CurrentIndex = -1;

            /// <summary>
            /// Advances to the next segment.
            /// </summary>
            /// <returns><see langword="true"/> if the enumerator advanced; otherwise <see langword="false"/>.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_CurrentIndex < batch.SegmentCount;

            /// <summary>
            /// Resets the enumerator to the initial position.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => m_CurrentIndex = -1;

            /// <summary>
            /// Gets the read-only span of the current segment.
            /// </summary>
            public ReadOnlySpan<T> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => batch[m_CurrentIndex];
            }
        }
    }

    /// <summary>
    /// An enumerable that provides every component of type <typeparamref name="T"/> as a read-only reference.
    /// </summary>
    /// <param name="batch">A batch representing the components across multiple chunks.</param>
    /// <remarks>
    /// Enumerate it with <c>foreach (ref readonly var value in elements)</c>. The enumerator is the write-side
    /// <see cref="Batch{T}.Enumerator"/>, whose <c>Current</c> is a <c>ref T</c>; the <c>ref readonly</c> binding is what
    /// keeps the loop from writing through it.
    /// </remarks>
    public readonly struct ElementsEnumerable(Batch<T> batch)
    {
        /// <summary>
        /// Gets a read-only reference to the component at the specified flattened element index.
        /// </summary>
        /// <param name="index">The zero-based index across all chunks.</param>
        public ref readonly T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref batch.GetElement(index);
        }

        /// <summary>
        /// Gets the total number of components of type <typeparamref name="T"/> across all chunks.
        /// </summary>
        public readonly int Count = batch.ElementCount;

        /// <summary>
        /// Gets the enumerator whose <c>Current</c> is a <c>ref T</c>.
        /// </summary>
        public Batch<T>.Enumerator GetEnumerator() => batch.GetEnumerator();
    }
}
