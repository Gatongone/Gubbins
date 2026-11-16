using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Unmanaged;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Represents a collection of chunks that match a specific query, providing access to the components of those chunks in an efficient manner.
/// </summary>
/// <remarks>
/// The query is evaluated lazily over the repository's archetypes: creating a <see cref="Chunks"/> value does not allocate
/// a chunk list. Consequently it reflects archetypes and chunks added after the search was performed.
/// </remarks>
public struct Chunks : IDisposable
{
    /// <summary>
    /// The archetypes that were searched. Kept by reference so the query does not allocate.
    /// </summary>
    private readonly List<Archetype> m_Archetypes;

    /// <summary>
    /// The allocator used for the unmanaged snippet arrays produced by <see cref="GetComponents{T}"/>.
    /// </summary>
    private readonly Allocator m_Allocator;

    /// <summary>
    /// The component filter used when the query was created from a <see cref="ComponentFilter"/>.
    /// </summary>
    private readonly ComponentFilter m_Filter;

    /// <summary>
    /// The matching predicate used when the query was created from a <see cref="ComponentTypesMatching"/> callback.
    /// </summary>
    private readonly ComponentTypesMatching? m_Matching;

    /// <summary>
    /// Gets the number of chunks in this collection.
    /// </summary>
    public int Count
    {
        get
        {
            if (m_Archetypes == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < m_Archetypes.Count; i++)
            {
                var archetype = m_Archetypes[i];
                if (Matches(archetype))
                {
                    count += archetype.Chunks.Count;
                }
            }

            return count;
        }
    }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="filter">The component filter to apply.</param>
    internal Chunks(List<Archetype> archetypes, Allocator allocator, ComponentFilter filter)
    {
        m_Archetypes = archetypes;
        m_Allocator = allocator;
        m_Filter = filter;
        m_Matching = null;
    }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="matching">The callback used to select archetypes.</param>
    internal Chunks(List<Archetype> archetypes, Allocator allocator, ComponentTypesMatching matching)
    {
        m_Archetypes = archetypes;
        m_Allocator = allocator;
        m_Filter = default;
        m_Matching = matching;
    }

    /// <summary>
    /// Gets the components of type <typeparamref name="T"/> across all chunks in this collection, organized into contiguous snippets for efficient access.
    /// </summary>
    /// <returns>A <see cref="Components{T}"/> instance that provides access to the components of type <typeparamref name="T"/> across all chunks in this collection.</returns>
    public Components<T> GetComponents<T>() where T : unmanaged
    {
        var snippets = new Ulist<Snippet<T>>(Count, m_Allocator);
        var elementCount = 0;
        if (m_Archetypes != null)
        {
            for (var i = 0; i < m_Archetypes.Count; i++)
            {
                var archetype = m_Archetypes[i];
                if (!Matches(archetype))
                {
                    continue;
                }

                // Concrete List<Chunk> rather than IReadOnlyList<Chunk>: the interface indexer dispatch dominates the
                // per-chunk cost of the walk, and this path must also build for netstandard2.1 (no CollectionsMarshal).
                var chunks = archetype.ChunkList;
                if (chunks.Count == 0)
                {
                    continue;
                }

                // Every chunk of an archetype shares the same column layout, so the index is resolved once instead
                // of re-scanning the type list per chunk.
                var column = archetype.ResolveColumn<T>();
                for (var j = 0; j < chunks.Count; j++)
                {
                    var snippet = chunks[j].GetAllAt<T>(column);
                    snippets.Add(snippet);
                    elementCount += snippet.Length;
                }
            }
        }

        // The element count is accumulated during the fill, so the batch does not need a second pass to compute it.
        return new Components<T>(ref snippets, elementCount);
    }

    /// <summary>
    /// Nothing to release: the query is a lazy view and the snippet arrays are owned by <see cref="Components{T}"/>.
    /// </summary>
    public void Dispose() { }

    /// <summary>
    /// Determines whether <paramref name="archetype"/> matches this query.
    /// </summary>
    /// <param name="archetype">The archetype to test.</param>
    /// <returns><see langword="true"/> if the archetype matches; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool Matches(Archetype archetype)
        => m_Matching?.Invoke(archetype.Types) ?? archetype.MatchComponents(m_Filter);
}

/// <summary>
/// An enumerable over the segments of <typeparamref name="T"/> as raw addresses.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="batch">The batch the segments come from.</param>
public readonly unsafe struct SegmentAddressEnumerable<T>(Batch<T> batch)
{
    /// <summary>
    /// Gets the number of segments.
    /// </summary>
    public int Count => batch.SegmentCount;

    /// <summary>
    /// Gets the address, element count and stride of a segment.
    /// </summary>
    /// <param name="index">The segment index.</param>
    public SegmentAddress this[int index]
    {
        get
        {
            var span = batch[index];

            return new SegmentAddress((byte*)Native.GetFirstElementAddress(span), span.Length, (int)Native.GetStackSize<T>());
        }
    }
}

/// <summary>
/// One contiguous segment of components: the address and shape a buffer binding takes.
/// </summary>
public readonly unsafe struct SegmentAddress
{
    /// <summary>
    /// The CPU address of the first element.
    /// </summary>
    public readonly byte* Data;

    /// <summary>
    /// The number of elements in the segment.
    /// </summary>
    public readonly int Count;

    /// <summary>
    /// The distance between two elements, in bytes.
    /// </summary>
    public readonly int Stride;

    /// <param name="data">The CPU address of the first element.</param>
    /// <param name="count">The number of elements.</param>
    /// <param name="stride">The distance between two elements.</param>
    internal SegmentAddress(byte* data, int count, int stride)
    {
        Data = data;
        Count = count;
        Stride = stride;
    }
}

/// <summary>
/// Span link to components of type <typeparamref name="T"/> across multiple chunks.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public struct Components<T> : IDisposable
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
    /// Gets an enumerable of all entities of type <typeparamref name="T"/> across the chunks, allowing iteration over each component instance.
    /// </summary>
    public readonly ElementsEnumerable Elements => new(Batch);

    /// <summary>
    /// Gets an enumerable of all contiguous snippets of type <typeparamref name="T"/> across the chunks, allowing iteration over each snippet as a span.
    /// </summary>
    public readonly SegmentsEnumerable Segments => new(Batch);

    /// <summary>
    /// Gets an enumerable of the same snippets as raw addresses, which is what a buffer binding takes: a device needs the
    /// address, the element count and the stride, not a span.
    /// </summary>
    public readonly SegmentAddressEnumerable<T> Addresses => new(Batch);

    /// <param name="blocks">Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    internal Components(ref Ulist<Snippet<T>> blocks)
    {
        Batch = new Batch<T>(blocks);
        m_Blocks = blocks;
    }

    /// <param name="blocks">Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    /// <param name="elementCount">The total element count, already known by the caller that filled <paramref name="blocks"/>.</param>
    internal Components(ref Ulist<Snippet<T>> blocks, int elementCount)
    {
        Batch = new Batch<T>(blocks, elementCount);
        m_Blocks = blocks;
    }

    /// <summary>
    /// Clears the list of snippets referenced by this query result.
    /// </summary>
    public void Dispose() => m_Blocks.Dispose();

    /// <summary>
    /// An enumerable that provides access to the contiguous snippets of type across the chunks, allowing iteration over each snippet as a span for efficient processing.
    /// </summary>
    public readonly struct SegmentsEnumerable(Batch<T> batch)
#if NET9_0_OR_GREATER
        : IEnumerable<Span<T>>
#endif
    {
        /// <summary>
        /// Gets the span for the snippet at the specified segment index.
        /// </summary>
        /// <param name="index">The segment index.</param>
        public Span<T> this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => batch[index];
        }

        /// <summary>
        /// Gets the number of underlying segments in the batch.
        /// </summary>
        public readonly int Count = batch.SegmentCount;

        public SegmentsEnumerator GetEnumerator() => new(batch);

#if NET9_0_OR_GREATER
        IEnumerator<Span<T>> IEnumerable<Span<T>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
#endif

        /// <summary>
        /// An enumerator that iterates over the contiguous snippets of type <typeparamref name="T"/> across the chunks, providing access to each snippet as a span for efficient processing.
        /// </summary>
        /// <param name="batch"></param>
        public struct SegmentsEnumerator(Batch<T> batch)
#if NET9_0_OR_GREATER
            : IEnumerator<Span<T>>
#endif
        {
            private int m_CurrentIndex;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => m_CurrentIndex = -1;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++m_CurrentIndex < batch.SegmentCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => m_CurrentIndex = -1;

#if NET9_0_OR_GREATER
            Span<T> IEnumerator<Span<T>>.Current => batch[m_CurrentIndex];

            object IEnumerator.Current => batch[m_CurrentIndex].ToArray();
#endif
        }
    }

    /// <summary>
    /// An enumerable that provides access to all entities of type <typeparamref name="T"/> across the chunks, allowing iteration over each component instance for efficient processing.
    /// </summary>
    /// <param name="batch"> A batch representing the components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    public struct ElementsEnumerable(Batch<T> batch) : IEnumerable<T>
    {
        /// <summary>
        /// Gets a reference to the component of type <typeparamref name="T"/> at the specified flattened element index across all chunks, allowing direct access to the component instance for efficient processing.
        /// </summary>
        /// <param name="index">The zero-based index across all chunks.</param>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Batch.GetElement(index);
        }

        /// <summary>
        /// Gets the total number of component instances of type <typeparamref name="T"/> across all chunks.
        /// </summary>
        public readonly int Count = batch.ElementCount;

        private Batch<T> m_Batch = batch;

        public Batch<T>.Enumerator GetEnumerator() => m_Batch.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}