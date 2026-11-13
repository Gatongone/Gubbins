using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Represents an archetype that groups entities with the same set of component types into chunks for efficient storage and processing.
/// </summary>
/// <param name="componentTypes">An array of component types that define this archetype.</param>
/// <param name="allocator">The allocator used to create chunk buffers.</param>
/// <param name="chunkSizeBytes">The size, in bytes, of each chunk buffer.</param>
internal class Archetype(ReadOnlySpan<Type> componentTypes, Allocator allocator, int chunkSizeBytes) : IDisposable
{
    private readonly Type[] m_ComponentTypes = [..componentTypes];
    private readonly List<Chunk> m_Chunks = [];
    private readonly ComponentsInfo m_ComponentsInfo = new([.. componentTypes]);
    public IReadOnlyList<Chunk> Chunks => m_Chunks;

    /// <summary>
    /// The chunk list as a concrete <see cref="List{T}"/>, so hot query paths can use
    /// <see cref="System.Runtime.InteropServices.CollectionsMarshal.AsSpan{T}"/> and avoid interface dispatch per
    /// chunk when they walk the archetype.
    /// </summary>
    internal List<Chunk> ChunkList => m_Chunks;
    public ReadOnlySpan<Type> Types => m_ComponentTypes.AsSpan();
#if NET9_0_OR_GREATER
    private readonly System.Collections.Frozen.FrozenSet<Type> m_FrozenSet = [.. componentTypes];
#else
    private readonly BloomFilter m_ComponentFilter = new(componentTypes);
#endif

    internal bool MatchComponents(ComponentFilter filter)
    {
#if !NET9_0_OR_GREATER
        // Quick check using Bloom filter to determine if the archetype may contain all included components and none of the excluded components.
        if (!m_ComponentFilter.MayContainsAll(filter.Includes) || m_ComponentFilter.MayContainsAny(filter.Excludes))
        {
            return false;
        }
#endif

        // Final check to ensure that the archetype contains all included components and none of the excluded components.
        return ContainsTypes(filter.Includes) && NotContainsTypes(filter.Excludes);
    }

#if NET9_0_OR_GREATER
    private bool ContainsTypes(ReadOnlySpan<Type> compTypes)
    {
        for (var i = 0; i < compTypes.Length; i++)
        {
            if (!m_FrozenSet.Contains(compTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool NotContainsTypes(ReadOnlySpan<Type> compTypes)
    {
        for (var i = 0; i < compTypes.Length; i++)
        {
            if (m_FrozenSet.Contains(compTypes[i]))
            {
                return false;
            }
        }

        return true;
    }
#else
    private bool ContainsTypes(ReadOnlySpan<Type> componentTypes)
    {
        for (var i = 0; i < componentTypes.Length; i++)
        {
            if (!m_ComponentsInfo.Types.Contains(componentTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    private bool NotContainsTypes(ReadOnlySpan<Type> componentTypes)
    {
        for (var i = 0; i < componentTypes.Length; i++)
        {
            if (m_ComponentsInfo.Types.Contains(componentTypes[i]))
            {
                return false;
            }
        }

        return true;
    }
#endif

    /// <summary>
    /// Determines whether this archetype matches the specified component types exactly.
    /// </summary>
    /// <param name="compTypes">A span of component types to match against this archetype.</param>
    /// <returns>True if the archetype contains exactly the same component types; otherwise, false.</returns>
    public bool MatchComponents(ReadOnlySpan<Type> compTypes)
    {
        if (compTypes.Length != m_ComponentsInfo.Types.Count)
        {
            return false;
        }
#if NET9_0_OR_GREATER
        for (var i = 0; i < compTypes.Length; i++)
        {
            if (!m_FrozenSet.Contains(compTypes[i]))
            {
                return false;
            }
        }
#else
        if (!m_ComponentFilter.MayContainsAll(compTypes))
        {
            return false;
        }

        for (var i = 0; i < compTypes.Length; i++)
        {
            if (!m_ComponentsInfo.Types.Contains(compTypes[i]))
            {
                return false;
            }
        }
#endif
        return true;
    }

    /// <summary>
    /// Resolves the column index of the specified component type for every chunk in this archetype.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <returns>The column index shared by all of this archetype's chunks.</returns>
    /// <exception cref="ArgumentException">Thrown when the archetype does not contain the type.</exception>
    /// <remarks>
    /// Chunks are always constructed from the same component type list, so the column index is identical for all
    /// of them. Resolving it once here keeps the per-chunk cost of a query down to a pointer addition instead of a
    /// type-list scan.
    /// </remarks>
    internal int ResolveColumn<T>()
    {
        var column = m_Chunks[0].IndexOfColumn(typeof(T));
        if (column == -1) throw new ArgumentException($"Unmatched type '{typeof(T)}' in the chunk.");
        return column;
    }

    /// <summary>
    /// Retrieves all components of the specified type from all chunks in this archetype.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="result">A list that will be populated with snippets containing the requested components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetComponents<T>(List<Snippet<T>> result)
    {
        if (m_Chunks.Count == 0)
        {
            return;
        }

        var column = ResolveColumn<T>();
        foreach (var chunk in m_Chunks)
        {
            result.Add(chunk.GetAllAt<T>(column));
        }
    }

    /// <summary>
    /// Gets an available chunk for storing entities, creating a new one if all existing chunks are full.
    /// </summary>
    /// <returns>A chunk that has available space for storing entities.</returns>
    public Chunk GetPartiallyFilledChunk()
    {
        Chunk targetChunk = default;
        var found = false;
        foreach (var chunk in m_Chunks)
        {
            if (chunk.IsFull) continue;
            targetChunk = chunk;
            found = true;
            break;
        }

        if (!found)
        {
            targetChunk = new Chunk(m_ComponentTypes, allocator, chunkSizeBytes);
            m_Chunks.Add(targetChunk);
        }

        return targetChunk;
    }

    /// <summary>
    /// Releases every chunk buffer owned by this archetype.
    /// </summary>
    public void Dispose()
    {
        for (var i = 0; i < m_Chunks.Count; i++)
        {
            m_Chunks[i].Dispose();
        }

        m_Chunks.Clear();
    }

    /// <summary>
    /// MayContains information about the component types in this archetype, including their hash code for efficient matching.
    /// </summary>
    private readonly struct ComponentsInfo
    {
        internal readonly HashSet<Type> Types;

        /// <summary>
        /// Initializes a new instance of the ComponentsInfo struct with the specified component types.
        /// </summary>
        /// <param name="types">A hash set of component types to store and generate a hash code for.</param>
        public ComponentsInfo(HashSet<Type> types) => Types = types;
    }
}

/// <summary>
/// Represents a Bloom filter, a space-efficient probabilistic data structure used to test whether an element is a member of a set.
/// It can quickly determine if an element is definitely not in the set or may be in the set, allowing for fast membership checks with
/// a controlled false positive rate. This implementation uses a bit array and multiple hash functions to manage the presence of types in the filter.
/// </summary>
internal class BloomFilter
{
    /// <summary>
    /// The bit array that represents the Bloom filter's internal state, where each bit indicates whether a particular hash position has been set to true
    /// (indicating the presence of a type) or false (indicating the absence of a type). The size of this bit array determines the capacity of the Bloom
    /// filter and affects its false positive rate.
    /// </summary>
    private readonly BitArray m_Bits;

    /// <summary>
    /// The size of the Bloom filter, which determines the number of bits in the bit array and thus the capacity of the filter.
    /// A larger size can reduce the false positive rate but will consume more memory.
    /// </summary>
    private readonly int m_Size;

    /// <param name="size">
    /// The size of the Bloom filter, which determines the number of bits in the bit array and thus the capacity of the filter.
    /// </param>
    /// <param name="types">
    /// Each type will be processed to compute its hash positions, and the corresponding bits in the bit array will be set to true, indicating that these types are likely present in the filter.
    /// </param>
    internal BloomFilter(int size = 256, params ReadOnlySpan<Type> types) : this(new BitArray(size, false), size, types) { }

    /// <param name="bits">
    /// The bit array that represents the Bloom filter's internal state, where each bit indicates whether a particular hash
    /// position has been set to true (indicating the presence of a type) or false (indicating the absence of a type).
    /// The size of this bit array determines the capacity of the Bloom filter and affects its false positive rate.
    /// </param>
    /// <param name="size">
    /// The size of the Bloom filter, which determines the number of bits in the bit array and thus the capacity of the filter.
    /// </param>
    /// <param name="types">
    /// Each type will be processed to compute its hash positions, and the corresponding bits in the bit array will be set to true, indicating that these types are likely present in the filter.
    /// </param>
    internal BloomFilter(BitArray bits, int size, params ReadOnlySpan<Type> types)
    {
        (m_Bits, m_Size) = (bits, size);
        for (var i = 0; i < types.Length; i++)
        {
            Add(types[i]);
        }
    }

    /// <param name="types">
    /// Each type will be processed to compute its hash positions, and the corresponding bits in the bit array will be set to true, indicating that these types are likely present in the filter.
    /// </param>
    internal BloomFilter(ReadOnlySpan<Type> types) : this(256, types)
    {
    }

    /// <summary>
    /// The hash code is computed by compressing the bit array into a 32-bit integer,
    /// where each bit in the resulting integer represents whether a corresponding bit in the Bloom filter's bit array is set to true.
    /// This provides a quick way to compare Bloom filters based on their contents, although it may lead to collisions
    /// (different Bloom filters producing the same hash code) due to the nature of Bloom filters and the compression process.
    /// </summary>
    public int CompressHash
    {
        get
        {
            // Compress the bit array into a 32-bit integer
            var code = 0;
            for (var i = 0; i < Math.Min(m_Bits.Length, 32); i++)
            {
                if (m_Bits.Get(i)) code |= 1 << i;
            }

            return code;
        }
    }

    /// <summary>
    /// Returns a new Bloom filter that includes the specified type. This method creates a new Bloom filter instance with the added type, allowing for method chaining to build complex filters.
    /// The method computes multiple hash positions for the given type and sets the corresponding bits in the bit array to true, indicating that the type is likely present in the filter.
    /// Note that Bloom filters can have false positives, meaning that they may indicate that a type is present when it is not, but they will never indicate that a type is absent if it is indeed present.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Type type)
    {
        var (h1, h2) = GetHashes(type);
        m_Bits[h1] = true;
        m_Bits[h2] = true;
    }

    /// <summary>
    /// Checks whether the Bloom filter might contain the specified type. This method computes multiple hash positions for the given type and checks if all corresponding bits in the bit array are set to true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MayContains(Type type)
    {
        var (h1, h2) = GetHashes(type);
        return m_Bits[h1] && m_Bits[h2];
    }

    /// <summary>
    /// Checks whether the Bloom filter might contain all of the specified types. This method iterates through each type, computes its hash positions, and checks if all corresponding bits in the bit array are set to true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MayContainsAll(params ReadOnlySpan<Type> types)
    {
        for (var i = 0; i < types.Length; i++)
        {
            if (!MayContains(types[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks whether the Bloom filter might contain any of the specified types. This method iterates through each type, computes its hash positions, and checks if any corresponding bits in the bit array are set to true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MayContainsAny(params ReadOnlySpan<Type> types)
    {
        for (var i = 0; i < types.Length; i++)
        {
            if (MayContains(types[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Simulating two different hash functions through high-low transformations
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int, int) GetHashes(Type type)
    {
        var hash = type.GetHashCode();
        var h1 = Math.Abs(hash) % m_Size;
        var h2 = Math.Abs((hash ^ (hash >> 16)) * 16777619) % m_Size;
        return (h1, h2);
    }

    /// <summary>
    /// Checks whether the Bloom filter is empty (i.e., contains no elements). This method iterates through the bit array to determine if any bits are set to true,
    /// indicating that at least one element has been added to the filter. If all bits are false, the filter is considered empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEmpty()
    {
        for (var index = 0; index < m_Bits.Count; index++)
        {
            var b = m_Bits[index];
            if (b) return false;
        }

        return true;
    }
}