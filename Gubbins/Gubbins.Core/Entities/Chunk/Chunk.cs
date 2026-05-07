using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Enhance;

namespace Gubbins.Entities;

/// <summary>
/// SoA (Structure of Arrays) implementation for entities and their components' storage.
/// </summary>
/// <remarks>
/// The motivation for using SoA primarily revolves around optimizing performance in scenarios involving large collections of data,
/// particularly when leveraging modern CPU architectures, and their features like SIMD instructions and caching.
/// </remarks>
public sealed class Chunk : IDisposable
{
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
    public unsafe int Add(Span<byte> value, Span<Type> types)
    {
        // Value can't match chunk layout.
        if (value.Length != m_ComponentsSize)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value length does not match components' size.");
        }

        if (IsFull) throw new ArgumentOutOfRangeException(nameof(value), "Chunk is full.");

        Span<int> typeIndexes = stackalloc int[types.Length];

        // Match all types.
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            var found = false;
            for (var j = 0; j < m_Types.Length; j++)
            {
                if (m_Types[j] != types[i]) continue;
                found          = true;
                typeIndexes[i] = j;
                break;
            }
            if (!found) throw new InvalidOperationException($"Type '{types[i]}' not found in chunk.");
        }

        // Copy the value to the chunk.
        for (var i = 0; i < typeIndexes.Length; i++)
        {
            var typeIndex = typeIndexes[i];
            var data = m_Data.AsSpan()[(m_Indexes[typeIndex] + Count * m_Sizes[typeIndex])..];
            var range = m_Layout[typeIndex - 1];
            value[range].CopyTo(data);
        }

        var index = Count++;
        return index;
    }

    /// <summary>
    /// Remove entity from the chunk.
    /// </summary>
    /// <param name="index">Entity index in chunk.</param>
    /// <returns>True if entity was successfully removed, false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int index)
    {
        // Invalid index.
        if (index < 0 || index >= Count) return false;

        var data = m_Data.AsSpan();

        // If removing the last entity, just decrement the count.
        if (index == Count - 1)
        {
            Count--;
            return true;
        }

        // Else swap the component at the index with the last component and decrement the count.
        for (var i = 1; i < Count - 1; i++)
        {
            var j = m_Indexes[index];
            (data[j], data[j + Count]) = (data[j + Count], data[j]);
        }

        Count--;
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
    /// Get component reference of the specified type.
    /// </summary>
    /// <typeparam name="T">Component or Entity type.</typeparam>
    /// <param name="index">Entity index in chunk.</param>
    /// <returns>Component reference.</returns>
    /// <exception cref="ArgumentException">Throw when the type is not contained by this chunk.</exception>
    public unsafe ref T Get<T>(int index)
    {
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
    /// Get all component references of the specified type.
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