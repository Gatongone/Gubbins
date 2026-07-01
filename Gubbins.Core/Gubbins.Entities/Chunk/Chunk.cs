using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// SoA (Structure of Arrays) implementation for entities and their components' storage.
/// </summary>
/// <remarks>
/// The motivation for using SoA primarily revolves around optimizing performance in scenarios involving large collections of data,
/// particularly when leveraging modern CPU architectures, and their features like SIMD instructions and caching.
/// </remarks>
internal sealed class Chunk : IDisposable
{
    private const string AddInputTypeCountMismatchMessage = "Input type count does not match chunk components.";
    private const string AddValueLengthMismatchMessage = "Value length does not match components' size.";
    private const string AddChunkFullMessage = "Chunk is full.";
    private const string AddEntityTypeNotAllowedMessage = "Input types must not include Entity.";

    /// <summary>
    /// Size of the chunk in bytes.
    /// </summary>
    private static readonly int s_MaxChunkSize = Device.CacheLineSize * 256;

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
    /// SoA data.
    /// </summary>
    private byte[] m_Data;

    /// <summary>
    /// The starting indexes of entities in <see cref="m_Data"/> by their components' memory layout.
    /// </summary>
    private readonly int[] m_Indexes;

    /// <summary>
    /// Size of every element in <see cref="m_Types"/>.
    /// </summary>
    private readonly int[] m_Sizes;

    /// <summary>
    /// Components' memory layout exclude entity itself.
    /// </summary>
    private readonly Range[] m_Layout;

    /// <summary>
    /// Component and entity types.
    /// </summary>
    /// <remarks>
    /// First type must be <see cref="Entity"/>.
    /// </remarks>
    private readonly Type[] m_Types;

    /// <summary>
    /// Handle for pinned memory.
    /// </summary>
    private GCHandle m_PinnedHandle;

    /// <param name="types">Component and entity types.</param>
    public unsafe Chunk(Type[] types)
    {
        if (types[0] != typeof(Entity))
        {
            types = new[] {typeof(Entity)}.Concat(types).ToArray();
        }

        IsFull    = false;
        m_Data    = new byte[s_MaxChunkSize];
        m_Indexes = new int[types.Length];
        m_Sizes   = new int[types.Length];
        m_Layout  = new Range[types.Length - 1];
        m_Types   = types;
        m_PinnedHandle = GCHandle.Alloc(m_Data, GCHandleType.Pinned);
        var componentsSize = 0;
        for (var index = 0; index < types.Length; index++)
        {
            m_Types[index] = types[index];
            var elementSize = (int) Native.GetStackSize(types[index]);
            m_Sizes[index] = elementSize;
            if (index > 0)
            {
                m_Layout[index - 1] =  index == 1 ? new Range(0, elementSize) : new Range(m_Layout[index - 2].End, m_Layout[index - 2].End.Value + elementSize);
                componentsSize      += elementSize;
            }
        }

        m_ComponentsSize = componentsSize;
        Capacity         = componentsSize == 0 ? 0 : s_MaxChunkSize / (componentsSize + sizeof(Entity));

        for (var index = 0; index < types.Length; index++)
        {
            m_Indexes[index] += index == 0 ? 0 : m_Indexes[index - 1] + Capacity * (int) Native.GetStackSize(types[index - 1]);
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
    internal unsafe int Add(Span<byte> value, Span<Type> types)
    {
        if (types.Length != m_Types.Length - 1)
        {
            throw new InvalidOperationException(AddInputTypeCountMismatchMessage);
        }

        // Value can't match chunk layout.
        if (value.Length != m_ComponentsSize)
        {
            throw new ArgumentOutOfRangeException(nameof(value), AddValueLengthMismatchMessage);
        }

        if (IsFull) throw new ArgumentOutOfRangeException(nameof(value), AddChunkFullMessage);

        Span<int> typeIndexes = stackalloc int[types.Length];
        Span<bool> matched = stackalloc bool[m_Types.Length];

        // Match all types.
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            if (types[i] == typeof(Entity))
            {
                throw new InvalidOperationException(AddEntityTypeNotAllowedMessage);
            }

            var found = false;
            for (var j = 1; j < m_Types.Length; j++)
            {
                if (m_Types[j] != types[i]) continue;

                if (matched[j])
                {
                    throw new InvalidOperationException($"Duplicate type '{types[i]}' in input types.");
                }

                found          = true;
                matched[j]     = true;
                typeIndexes[i] = j;
                break;
            }
            if (!found) throw new InvalidOperationException($"Type '{types[i]}' not found in chunk.");
        }

        // Copy values using input order as source and chunk layout as destination.
        var sourceOffset = 0;
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            var typeIndex = typeIndexes[i];
            var size = m_Sizes[typeIndex];
            var targetOffset = m_Indexes[typeIndex] + Count * size;
            value.Slice(sourceOffset, size).CopyTo(m_Data.AsSpan(targetOffset, size));
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
        var data = m_Data.AsSpan();

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
    public unsafe ref T Get<T>(int index)
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
        var monoValue = m_Data.AsSpan().Slice(typeIndex + index * size, size);
        return ref Native.AsRef<T>(Native.GetFirstElementAddress(monoValue));
    }

    /// <summary>
    /// GetRecord all component references of the specified type.
    /// </summary>
    /// <typeparam name="T">Component or Entity type.</typeparam>
    /// <returns>Component value.</returns>
    /// <exception cref="ArgumentException">Throw when the type is not contained by this chunk.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Snippet<T> GetAll<T>()
    {
        var comIndex = -1;
        for (var i = 0; i < m_Types.Length; i++)
        {
            if (m_Types[i] == typeof(T))
            {
                comIndex = m_Indexes[i];
                break;
            }
        }

        if (comIndex == -1) throw new ArgumentException($"Unmatched type '{typeof(T)}' in the chunk.");
        var snippet = new Snippet<byte>(m_Data, comIndex, Count * (int) Native.GetStackSize<T>(), m_PinnedHandle);
        var result = snippet.Cast<T>();
        return result;
    }

    /// <inheritdoc />
    public void Dispose() => m_PinnedHandle.Free();
}