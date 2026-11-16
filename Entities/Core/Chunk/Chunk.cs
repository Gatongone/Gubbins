using System.Runtime.CompilerServices;
using Gubbins.Enhance;
using Gubbins.Unsafe;
using Type = System.Type;

namespace Gubbins.Entities;

/// <summary>
/// SoA (Structure of Arrays) implementation for entities and their components' storage.
/// </summary>
/// <remarks>
/// <para>
/// The motivation for using SoA primarily revolves around optimizing performance in scenarios involving large collections of data,
/// particularly when leveraging modern CPU architectures, and their features like SIMD instructions and caching.
/// </para>
/// <para>
/// The component buffer is allocated from the <see cref="Allocator"/> supplied by the owning repository, so allocation
/// policy (native heap, arena, virtual memory, …) is decided by the business layer rather than by the chunk.
/// </para>
/// </remarks>
internal sealed unsafe class Chunk : IDisposable
{
    private const string ADD_INPUT_TYPE_COUNT_MISMATCH_MESSAGE = "Input type count does not match chunk components.";
    private const string ADD_VALUE_LENGTH_MISMATCH_MESSAGE = "Value length does not match components' size.";
    private const string ADD_CHUNK_FULL_MESSAGE = "Chunk is full.";
    private const string ADD_ENTITY_TYPE_NOT_ALLOWED_MESSAGE = "Input types must not include Entity.";

    /// <summary>
    /// Default size of the chunk in bytes.
    /// </summary>
    internal static readonly int DefaultChunkSize = Device.CacheLineSize * 256;

    /// <summary>
    /// Whether the chunk is full.
    /// </summary>
    public bool IsFull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    /// <summary>
    /// Entity count.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private set;
    }

    /// <summary>
    /// Entity max count.
    /// </summary>
    public readonly int Capacity;

    /// <summary>
    /// Maximum component size per entity.
    /// </summary>
    private readonly int m_ComponentsSize;

    /// <summary>
    /// The allocator that owns <see cref="m_Data"/>.
    /// </summary>
    private readonly Allocator m_Allocator;

    /// <summary>
    /// The size, in bytes, of the component buffer.
    /// </summary>
    private readonly int m_ChunkSize;

    /// <summary>
    /// The alignment with which the component buffer was allocated.
    /// </summary>
    private readonly byte m_Alignment;

    /// <summary>
    /// SoA data.
    /// </summary>
    private byte* m_Data;

    /// <summary>
    /// The starting indexes of entities in <see cref="m_Data"/> by their components' memory layout.
    /// </summary>
    private readonly int[] m_Indexes;

    /// <summary>
    /// Size of every element in <see cref="m_Types"/>.
    /// </summary>
    private readonly int[] m_Sizes;

    /// <summary>
    /// Component and entity types.
    /// </summary>
    /// <remarks>
    /// First type must be <see cref="Entity"/>.
    /// </remarks>
    private readonly Type[] m_Types;

    /// <summary>
    /// Initializes a chunk with the default buffer size.
    /// </summary>
    /// <param name="types">Component and entity types.</param>
    /// <param name="allocator">The allocator that owns the component buffer.</param>
    public Chunk(Type[] types, Allocator allocator) : this(types, allocator, DefaultChunkSize) { }

    /// <summary>
    /// Initializes a chunk with the specified buffer size.
    /// </summary>
    /// <param name="types">Component and entity types.</param>
    /// <param name="allocator">The allocator that owns the component buffer.</param>
    /// <param name="chunkSizeBytes">The size, in bytes, of the component buffer.</param>
    public Chunk(Type[] types, Allocator allocator, int chunkSizeBytes)
    {
        if (chunkSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes));
        }

        if (types[0] != typeof(Entity))
        {
            types = [typeof(Entity), .. types];
        }

        IsFull        = false;
        m_Allocator   = allocator;
        m_ChunkSize   = chunkSizeBytes;
        m_Indexes     = new int[types.Length];
        m_Sizes       = new int[types.Length];
        m_Types       = types;

        var componentsSize = 0;
        var alignment      = 1;
        for (var index = 0; index < types.Length; index++)
        {
            var elementSize = (int)Native.GetStackSize(types[index]);
            m_Sizes[index] = elementSize;
            alignment      = Math.Max(alignment, Native.GetAlignment(elementSize));
            if (index > 0)
            {
                componentsSize += elementSize;
            }
        }

        m_ComponentsSize = componentsSize;
        m_Alignment      = (byte)alignment;
        m_Data           = (byte*)allocator.Alloc((nuint)chunkSizeBytes, m_Alignment);

        if (m_Data == null)
        {
            throw new OutOfMemoryException($"Unable to allocate a chunk buffer of {chunkSizeBytes} bytes.");
        }

        // Unmanaged memory is not zeroed; keep the same initialized state the managed array had.
        new Span<byte>(m_Data, chunkSizeBytes).Clear();

        Capacity = chunkSizeBytes / (componentsSize + sizeof(Entity));
        IsFull   = Capacity == 0;

        for (var index = 0; index < types.Length; index++)
        {
            m_Indexes[index] += index == 0 ? 0 : m_Indexes[index - 1] + Capacity * (int)Native.GetStackSize(types[index - 1]);
        }
    }

    /// <summary>
    /// Add entity to the chunk.
    /// </summary>
    /// <param name="value">Components' values.</param>
    /// <param name="types">Components' types</param>
    /// <returns>The entity index in the chunk.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The chunk is full, or the value length does not match components' size.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int Add(ReadOnlySpan<byte> value, ReadOnlySpan<Type> types)
    {
        if (types.Length != m_Types.Length - 1)
        {
            throw new InvalidOperationException(ADD_INPUT_TYPE_COUNT_MISMATCH_MESSAGE);
        }

        // Value can't match chunk layout.
        if (value.Length != m_ComponentsSize)
        {
            throw new ArgumentOutOfRangeException(nameof(value), ADD_VALUE_LENGTH_MISMATCH_MESSAGE);
        }

        if (IsFull) throw new ArgumentOutOfRangeException(nameof(value), ADD_CHUNK_FULL_MESSAGE);

        Span<int> typeIndexes = stackalloc int[types.Length];
        Span<bool> matched = stackalloc bool[m_Types.Length];

        // Match all types.
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            if (types[i] == typeof(Entity))
            {
                throw new InvalidOperationException(ADD_ENTITY_TYPE_NOT_ALLOWED_MESSAGE);
            }

            var found = false;
            for (var j = 1; j < m_Types.Length; j++)
            {
                if (m_Types[j] != types[i]) continue;

                if (matched[j])
                {
                    throw new InvalidOperationException($"Duplicate type '{types[i]}' in input types.");
                }

                found = true;
                matched[j] = true;
                typeIndexes[i] = j;
                break;
            }

            if (!found) throw new InvalidOperationException($"Type '{types[i]}' not found in chunk.");
        }

        var data = new Span<byte>(m_Data, m_ChunkSize);

        // Copy values using input order as source and chunk layout as destination.
        var sourceOffset = 0;
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            var typeIndex = typeIndexes[i];
            var size = m_Sizes[typeIndex];
            var targetOffset = m_Indexes[typeIndex] + Count * size;
            value.Slice(sourceOffset, size).CopyTo(data.Slice(targetOffset, size));
            sourceOffset += size;
        }

        var index = Count++;
        IsFull = Count >= Capacity;
        return index;
    }

    /// <summary>
    /// Remove entity from the chunk.
    /// </summary>
    /// <param name="index">Entity index in chunk.</param>
    /// <returns>True if entity was successfully removed, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Remove(int index)
    {
        return Remove(index, out _, out _);
    }

    /// <summary>
    /// Remove entity from the chunk and report the moved entity when compaction occurs.
    /// </summary>
    /// <param name="index">Entity index in chunk.</param>
    /// <param name="movedEntity">Entity moved from tail to <paramref name="index"/>; default when no move happened.</param>
    /// <param name="movedFromIndex">Original index of <paramref name="movedEntity"/>; -1 when no move happened.</param>
    /// <returns>True if entity was successfully removed, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Remove(int index, out Entity movedEntity, out int movedFromIndex)
    {
        movedEntity = default;
        movedFromIndex = -1;

        // Invalid index.
        if (index < 0 || index >= Count) return false;

        var lastIndex = Count - 1;
        var data = new Span<byte>(m_Data, m_ChunkSize);

        // If removing the last entity, just decrement the count.
        if (index == lastIndex)
        {
            Count--;
            IsFull = Count >= Capacity;
            return true;
        }

        // Move the last entity's data into the removed slot for all columns (Entity + components).
        for (var i = 0; i < m_Indexes.Length; i++)
        {
            var elementSize = m_Sizes[i];
            var from = m_Indexes[i] + lastIndex * elementSize;
            var to = m_Indexes[i] + index * elementSize;
            data.Slice(from, elementSize).CopyTo(data.Slice(to, elementSize));
        }

        movedEntity = Get<Entity>(index);
        movedFromIndex = lastIndex;

        Count--;
        IsFull = Count >= Capacity;
        return true;
    }

    /// <summary>
    /// Set component of the entity at the specified index.
    /// </summary>
    /// <param name="index">Entity index in chunk.</param>
    /// <param name="value">Component value.</param>
    /// <typeparam name="T">Component or Entity type.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set<T>(int index, T value)
    {
        ref var comp = ref Get<T>(index);
        comp = value;
    }

    /// <summary>
    /// GetRecord component reference of the specified type.
    /// </summary>
    /// <typeparam name="T">Component or Entity type.</typeparam>
    /// <param name="index">Entity index in chunk.</param>
    /// <returns>Component reference.</returns>
    /// <exception cref="ArgumentException">Throw when the type is not contained by this chunk.</exception>
    public ref T Get<T>(int index)
    {
        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");
        }

        var typeIndex = -1;
        var size = 0;
        for (var i = 0; i < m_Types.Length; i++)
        {
            if (m_Types[i] == typeof(T))
            {
                size = m_Sizes[i];
                typeIndex = m_Indexes[i];
                break;
            }
        }

        if (typeIndex == -1) throw new ArgumentException($"Unmatched type '{typeof(T)}' in the chunk.");
        return ref Native.AsRef<T>(m_Data + typeIndex + index * size);
    }

    /// <summary>
    /// Finds the column of the specified component type.
    /// </summary>
    /// <param name="type">Component or entity type.</param>
    /// <returns>The column index, or <c>-1</c> when the type is not contained by this chunk.</returns>
    /// <remarks>
    /// Every chunk of an archetype is constructed from the same type list, so the returned index is identical for
    /// all of them. Callers that iterate an archetype's chunks should resolve the index once and then use
    /// <see cref="GetAllAt{T}"/> instead of re-scanning the type list per chunk.
    /// </remarks>
    internal int IndexOfColumn(Type type)
    {
        for (var i = 0; i < m_Types.Length; i++)
        {
            if (m_Types[i] == type)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Gets the components of the specified type using a column index resolved by <see cref="IndexOfColumn"/>.
    /// </summary>
    /// <typeparam name="T">Component or entity type.</typeparam>
    /// <param name="columnIndex">The column index resolved for <typeparamref name="T"/>.</param>
    /// <returns>Component value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Snippet<T> GetAllAt<T>(int columnIndex) => new(m_Data + m_Indexes[columnIndex], Count);

    /// <summary>
    /// GetRecord all component references of the specified type.
    /// </summary>
    /// <typeparam name="T">Component or Entity type.</typeparam>
    /// <returns>Component value.</returns>
    /// <exception cref="ArgumentException">Throw when the type is not contained by this chunk.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snippet<T> GetAll<T>()
    {
        var columnIndex = IndexOfColumn(typeof(T));
        if (columnIndex == -1) throw new ArgumentException($"Unmatched type '{typeof(T)}' in the chunk.");
        return GetAllAt<T>(columnIndex);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_Data == null)
        {
            return;
        }

        m_Allocator.Free(m_Data, (nuint)m_ChunkSize, m_Alignment);
        m_Data = null;
    }
}
