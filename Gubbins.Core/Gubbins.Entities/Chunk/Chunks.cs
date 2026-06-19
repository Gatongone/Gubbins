using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Represents a collection of chunks that match a specific query, providing access to the components of those chunks in an efficient manner.
/// </summary>
public readonly struct Chunks : IDisposable
{
    /// <summary>
    /// The list of chunks that match the query, allowing access to the components stored within those chunks for processing.
    /// </summary>
    private readonly List<Chunk>       m_Chunks;
    /// <summary>
    /// The pool used to recycle the list of chunks when this instance is disposed, ensuring efficient memory management by reusing lists for future queries.
    /// </summary>
    private readonly Pool<List<Chunk>> m_Pool;

    /// <summary>
    /// Gets the number of chunks in this collection.
    /// </summary>
    public int Count => m_Chunks.Count;

    /// <param name="chunks"> The list of chunks that match the query, allowing access to the components stored within those chunks for processing.</param>
    /// <param name="pool"> The pool used to recycle the list of chunks when this instance is disposed, ensuring efficient memory management by reusing lists for future queries.</param>
    internal Chunks(List<Chunk> chunks, Pool<List<Chunk>> pool)
    {
        m_Chunks = chunks;
        m_Pool   = pool;
    }

    /// <summary>
    /// Gets the components of type <typeparamref name="T"/> across all chunks in this collection, organized into contiguous snippets for efficient access.
    /// </summary>
    /// <returns>A <see cref="Components{T}"/> instance that provides access to the components of type <typeparamref name="T"/> across all chunks in this collection.</returns>
    public Components<T> GetComponents<T>() where T : unmanaged
    {
        var pool = Pool<List<Snippet<T>>>.Default;
        var snippets = pool.Spawn();
        for (var i = 0; i < m_Chunks.Count; i++)
        {
            snippets.Add(m_Chunks[i].GetAll<T>());
        }

        return new Components<T>(snippets, pool);
    }

    /// <summary>
    /// Disposes of this instance by clearing the list of chunks and recycling it back to the pool for reuse, ensuring efficient memory management.
    /// </summary>
    public void Dispose()
    {
        m_Chunks.Clear();
        m_Pool.Recycle(m_Chunks);
    }
}

/// <summary>
/// Span link to components of type <typeparamref name="T"/> across multiple chunks.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
public readonly struct Components<T> : IDisposable
{
    /// <summary>
    /// A batch representing the components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.
    /// </summary>
    internal readonly Batch<T> Batch;

    /// <summary>
    /// Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.
    /// </summary>
    private readonly List<Snippet<T>> m_Blocks;

    /// <summary>
    /// The pool used to recycle the list of snippets when this instance is disposed.
    /// </summary>
    private readonly Pool<List<Snippet<T>>> m_Pool;

    /// <summary>
    /// Gets an enumerable of all entities of type <typeparamref name="T"/> across the chunks, allowing iteration over each component instance.
    /// </summary>
    public readonly ElementsEnumerable Elements => new(Batch);

    /// <summary>
    /// Gets an enumerable of all contiguous snippets of type <typeparamref name="T"/> across the chunks, allowing iteration over each snippet as a span.
    /// </summary>
    public readonly SegmentsEnumerable Segments => new(Batch);

    /// <param name="blocks">Components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    /// <param name="pool">The pool used to recycle the list of snippets when this instance is disposed.</param>
    internal Components(List<Snippet<T>> blocks, Pool<List<Snippet<T>>> pool)
    {
        Batch    = new Batch<T>(blocks);
        m_Blocks = blocks;
        m_Pool   = pool;
    }

    /// <summary>
    /// Disposes of this instance by clearing the list of snippets and recycling it back to the pool for reuse, ensuring efficient memory management.
    /// </summary>
    public void Dispose()
    {
        m_Blocks.Clear();
        m_Pool.Recycle(m_Blocks);
    }

    /// <summary>
    /// An enumerable that provides access to the contiguous snippets of type <typeparamref name="T"/> across the chunks, allowing iteration over each snippet as a span for efficient processing.
    /// </summary>
    /// <param name="batch"> A batch representing the components of type <typeparamref name="T"/> across multiple chunks, organized into contiguous snippets for efficient access.</param>
    public readonly ref struct SegmentsEnumerable(Batch<T> batch)
#if NET9_0_OR_GREATER
        : IEnumerable<Span<T>>
#endif
    {
        /// <summary>
        /// Gets the span for the snippet at the specified segment index.
        /// </summary>
        /// <param name="index">The segment index.</param>
        public Span<T> this[int index] => batch[index];

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
    public readonly struct ElementsEnumerable(Batch<T> batch) : IEnumerable<T>
    {
        /// <summary>
        /// Gets a reference to the component of type <typeparamref name="T"/> at the specified flattened element index across all chunks, allowing direct access to the component instance for efficient processing.
        /// </summary>
        /// <param name="index">The zero-based index across all chunks.</param>
        public ref T this[int index] => ref batch.GetElement(index);

        /// <summary>
        /// Gets the total number of component instances of type <typeparamref name="T"/> across all chunks.
        /// </summary>
        public readonly int Count = batch.ElementCount;

        public Batch<T>.Enumerator GetEnumerator() => batch.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}