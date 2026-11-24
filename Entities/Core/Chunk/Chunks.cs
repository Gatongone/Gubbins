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
public struct Chunks
{
    /// <summary>
    /// The <see cref="ChangedSince"/> value that carries no change filter.
    /// </summary>
    private const long NO_CHANGE_FILTER = long.MinValue;

    /// <summary>
    /// The archetypes that were searched. Kept by reference so the query does not allocate.
    /// </summary>
    private readonly List<Archetype> m_Archetypes;

    /// <summary>
    /// The allocator used for the unmanaged snippet arrays produced by <see cref="GetLayout{T}"/>.
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
    /// The repository write clock, advanced by the chunks this query iterates with write access.
    /// </summary>
    private readonly ChangeClock m_Clock;

    /// <summary>
    /// The change version a chunk must be newer than to be iterated, or <see cref="NO_CHANGE_FILTER"/>.
    /// </summary>
    private readonly long m_ChangedSince;

    /// <summary>
    /// Whether archetypes that carry <see cref="Disabled"/> are iterated.
    /// </summary>
    private readonly bool m_IncludeDisabled;

    /// <summary>
    /// Gets the number of chunks in this collection.
    /// </summary>
    /// <remarks>
    /// Chunks skipped by the query's filters are not counted.
    /// </remarks>
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
                if (!Matches(m_Archetypes[i]))
                {
                    continue;
                }

                var chunks = m_Archetypes[i].ChunkList;
                for (var j = 0; j < chunks.Count; j++)
                {
                    if (IsChanged(chunks[j]))
                    {
                        count++;
                    }
                }
            }

            return count;
        }
    }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="filter">The component filter to apply.</param>
    /// <param name="clock">The repository write clock, advanced by write-access iteration.</param>
    internal Chunks(List<Archetype> archetypes, Allocator allocator, ComponentFilter filter, ChangeClock clock)
        : this(archetypes, allocator, filter, null, clock, ContainsDisabled(filter.Includes)) { }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="matching">The callback used to select archetypes.</param>
    /// <param name="clock">The repository write clock, advanced by write-access iteration.</param>
    internal Chunks(List<Archetype> archetypes, Allocator allocator, ComponentTypesMatching matching, ChangeClock clock)
        : this(archetypes, allocator, default, matching, clock, false) { }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="filter">The component filter to apply.</param>
    /// <param name="matching">The callback used to select archetypes, when the query has one.</param>
    /// <param name="clock">The repository write clock, advanced by write-access iteration.</param>
    /// <param name="includeDisabled">Whether archetypes carrying <see cref="Disabled"/> are iterated.</param>
    private Chunks(List<Archetype> archetypes, Allocator allocator, ComponentFilter filter, ComponentTypesMatching? matching, ChangeClock clock, bool includeDisabled)
        : this(archetypes, allocator, filter, matching, clock, NO_CHANGE_FILTER, includeDisabled) { }

    /// <param name="archetypes">The repository's archetypes to query.</param>
    /// <param name="allocator">The allocator used for the component snippet arrays.</param>
    /// <param name="filter">The component filter to apply.</param>
    /// <param name="matching">The callback used to select archetypes, when the query has one.</param>
    /// <param name="clock">The repository write clock, advanced by write-access iteration.</param>
    /// <param name="changedSince">The change version a chunk must be newer than, or <see cref="NO_CHANGE_FILTER"/>.</param>
    /// <param name="includeDisabled">Whether archetypes carrying <see cref="Disabled"/> are iterated.</param>
    private Chunks(List<Archetype> archetypes, Allocator allocator, ComponentFilter filter, ComponentTypesMatching? matching, ChangeClock clock, long changedSince, bool includeDisabled)
    {
        m_Archetypes = archetypes;
        m_Allocator = allocator;
        m_Filter = filter;
        m_Matching = matching;
        m_Clock = clock;
        m_ChangedSince = changedSince;
        m_IncludeDisabled = includeDisabled;
    }

    /// <summary>
    /// Returns a query that only iterates chunks written after <paramref name="version"/>, which is the change filter
    /// that skips the chunks nothing touched.
    /// </summary>
    /// <param name="version">
    /// The version a chunk must be newer than. Prefer <see cref="ChangeCursor"/>, which captures and advances it for the
    /// caller; pass <see cref="EntityRepository.Version"/> when an explicit point in the write timeline is wanted.
    /// </param>
    /// <remarks>
    /// A chunk whose <see cref="Chunk.WrittenVersion"/> is not newer than <paramref name="version"/> is not iterated and
    /// not counted. Every write takes a fresh version from the repository's clock, so a value captured before a write
    /// always selects it, and one captured after the write never does twice.
    /// </remarks>
    public readonly Chunks ChangedSince(long version)
        => new(m_Archetypes, m_Allocator, m_Filter, m_Matching, m_Clock, version, m_IncludeDisabled);

    /// <summary>
    /// Returns a read-only view of this query.
    /// </summary>
    /// <remarks>
    /// The view shares the filters of this query but exposes only read access: it iterates through
    /// <see cref="ReadOnlyChunkLayout{T}"/> and the read-only iteration overloads, cannot write component data, and
    /// never marks a chunk as changed. Its iteration methods share their names with the write-side ones, so
    /// <c>ForEach</c> and <c>ForEachAsync</c> exist on both sides and the receiver selects the access.
    /// </remarks>
    public readonly ReadOnlyChunks AsReadOnly()
        => new(new Chunks(m_Archetypes, m_Allocator, m_Filter, m_Matching, m_Clock, m_ChangedSince, m_IncludeDisabled));

    /// <summary>
    /// Returns a query that also iterates disabled entities.
    /// </summary>
    /// <remarks>
    /// Queries skip archetypes carrying <see cref="Disabled"/> unless their filter includes it, so a disabled entity
    /// disappears from every query without moving out of the repository. Use this to see them anyway, for example to
    /// list, enable or delete them.
    /// </remarks>
    public readonly Chunks IncludeDisabled()
        => new(m_Archetypes, m_Allocator, m_Filter, m_Matching, m_Clock, m_ChangedSince, true);

    /// <summary>
    /// Gets the components of type <typeparamref name="T"/> across all chunks in this collection, organized into contiguous snippets for efficient access.
    /// </summary>
    /// <returns>A <see cref="Components{T}"/> instance that provides access to the components of type <typeparamref name="T"/> across all chunks in this collection.</returns>
    /// <remarks>
    /// This is a write access: every chunk whose snippet is handed out is stamped as changed at the query's version,
    /// which keeps it out of another system's <see cref="ChangedSince"/> result. For a view that cannot write and does
    /// not stamp, use <see cref="AsReadOnly"/>.
    /// </remarks>
    public ChunkLayout<T> GetLayout<T>() where T : unmanaged
    {
        var snippets = new Ulist<Snippet<T>>(Count, m_Allocator);
        var elementCount = Fill(ref snippets, true);

        // The element count is accumulated during the fill, so the batch does not need a second pass to compute it.
        return new ChunkLayout<T>(ref snippets, elementCount);
    }

    /// <summary>
    /// Gets the components of type <typeparamref name="T"/> as a read-only view, without declaring write access.
    /// </summary>
    /// <typeparam name="T">Component or entity type.</typeparam>
    /// <returns>A <see cref="ReadOnlyChunkLayout{T}"/> over the same snippets, exposed as read-only spans.</returns>
    /// <remarks>
    /// The read half behind <see cref="ReadOnlyChunks.GetLayout{T}"/>: the spans are <see cref="ReadOnlySpan{T}"/>
    /// and the element accessor returns a <c>ref readonly</c>, so the caller can neither mutate a chunk nor make it look
    /// changed.
    /// </remarks>
    internal ReadOnlyChunkLayout<T> GetReadOnlyLayout<T>() where T : unmanaged
    {
        var snippets = new Ulist<Snippet<T>>(Count, m_Allocator);
        var elementCount = Fill(ref snippets, false);
        return new ReadOnlyChunkLayout<T>(ref snippets, elementCount);
    }

    /// <summary>
    /// Appends one snippet per chunk that matches this query and the change filter to <paramref name="snippets"/>.
    /// </summary>
    /// <typeparam name="T">Component or entity type.</typeparam>
    /// <param name="snippets">The snippet list to fill.</param>
    /// <param name="writeAccess">Whether visiting a chunk stamps it as changed.</param>
    /// <returns>The total number of elements across the appended snippets.</returns>
    /// <remarks>
    /// A write access takes one version from the clock for the whole fill and stamps every chunk it visits with it. One
    /// version for the fill, rather than one per chunk, keeps a single iteration one point in the write timeline.
    /// </remarks>
    private int Fill<T>(ref Ulist<Snippet<T>> snippets, bool writeAccess) where T : unmanaged
    {
        var elementCount = 0;
        if (m_Archetypes == null)
        {
            return elementCount;
        }

        var version = writeAccess ? m_Clock.Next() : 0;

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
                var chunk = chunks[j];
                if (!IsChanged(chunk))
                {
                    continue;
                }

                if (writeAccess)
                {
                    chunk.MarkWritten(version);
                }

                var snippet = chunk.GetAllAt<T>(column);
                snippets.Add(snippet);
                elementCount += snippet.Length;
            }
        }

        return elementCount;
    }

    /// <summary>
    /// Determines whether <paramref name="archetype"/> matches this query.
    /// </summary>
    /// <param name="archetype">The archetype to test.</param>
    /// <returns><see langword="true"/> if the archetype matches; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// A disabled entity is stored in an archetype that carries <see cref="Disabled"/>, so skipping those archetypes is
    /// what makes a disabled entity invisible to a query. A filter that includes <see cref="Disabled"/> keeps them.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool Matches(Archetype archetype)
        => (m_IncludeDisabled || !archetype.ContainsType(typeof(Disabled)))
        && (m_Matching?.Invoke(archetype.Types) ?? archetype.MatchComponents(m_Filter));

    /// <summary>
    /// Determines whether <paramref name="chunk"/> is new enough for this query's change filter.
    /// </summary>
    /// <param name="chunk">The chunk to test.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool IsChanged(Chunk chunk) => m_ChangedSince == NO_CHANGE_FILTER || chunk.WrittenVersion > m_ChangedSince;

    /// <summary>
    /// Determines whether the specified component list contains <see cref="Disabled"/>.
    /// </summary>
    /// <param name="types">The component types of a filter.</param>
    private static bool ContainsDisabled(ReadOnlySpan<Type> types)
    {
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == typeof(Disabled))
            {
                return true;
            }
        }

        return false;
    }
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
public struct ChunkLayout<T> : IDisposable
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
    internal ChunkLayout(ref Ulist<Snippet<T>> blocks)
    {
        Batch = new Batch<T>(blocks);
        m_Blocks = blocks;
    }

    /// <param name="blocks">Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    /// <param name="elementCount">The total element count, already known by the caller that filled <paramref name="blocks"/>.</param>
    internal ChunkLayout(ref Ulist<Snippet<T>> blocks, int elementCount)
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