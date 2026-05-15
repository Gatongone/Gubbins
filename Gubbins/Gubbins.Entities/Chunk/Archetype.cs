using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Defines the contract for an archetype that manages components and chunks in an entity-component system.
/// </summary>
public interface IArchetype
{
    /// <summary>
    /// Determines whether this archetype matches the specified component types exactly.
    /// </summary>
    /// <param name="componentTypes">A span of component types to match against this archetype.</param>
    /// <returns>True if the archetype contains exactly the same component types; otherwise, false.</returns>
    bool MatchComponents(Span<Type> componentTypes);

    /// <summary>
    /// Determines whether this archetype matches the specified component criteria using hash-based filtering.
    /// </summary>
    /// <param name="componentHash">A hash code representing the required components for optimization.</param>
    /// <param name="included">A span of component types that must be present in this archetype.</param>
    /// <param name="excluded">A span of component types that must not be present in this archetype.</param>
    /// <returns>True if the archetype contains all included components and none of the excluded components; otherwise, false.</returns>
    bool MatchComponents(int componentHash, Span<Type> included, Span<Type> excluded);

    /// <summary>
    /// Retrieves all components of the specified type from all chunks in this archetype.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="result">A list that will be populated with snippets containing the requested components.</param>
    void GetComponents<T>(List<Snippet<T>> result);
}

/// <summary>
/// Represents an archetype that groups entities with the same set of component types into chunks for efficient storage and processing.
/// </summary>
/// <param name="componentTypes">An array of component types that define this archetype.</param>
public sealed class Archetype(Type[] componentTypes) : IArchetype
{
    private readonly List<Chunk>    m_Chunks         = [];
    private readonly ComponentsInfo m_ComponentsInfo = new([..componentTypes]);
    private readonly Type[]         m_ComponentTypes = componentTypes.ToArray();

    /// <summary>
    /// Determines whether this archetype matches the specified component types exactly.
    /// </summary>
    /// <param name="componentTypes">A span of component types to match against this archetype.</param>
    /// <returns>True if the archetype contains exactly the same component types; otherwise, false.</returns>
    public bool MatchComponents(Span<Type> componentTypes)
    {
        if (componentTypes.Length != m_ComponentsInfo.Types.Count)
        {
            return false;
        }

        for (var i = 0; i < componentTypes.Length; i++)
        {
            if (!m_ComponentsInfo.Types.Contains(componentTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether this archetype matches the specified component criteria using hash-based filtering.
    /// </summary>
    /// <param name="componentHash">A hash code representing the required components for optimization.</param>
    /// <param name="included">A span of component types that must be present in this archetype.</param>
    /// <param name="excluded">A span of component types that must not be present in this archetype.</param>
    /// <returns>True if the archetype contains all included components and none of the excluded components; otherwise, false.</returns>
    public bool MatchComponents(int componentHash, Span<Type> included, Span<Type> excluded)
    {
        if (componentHash != (m_ComponentsInfo.HashCode & componentHash))
        {
            return false;
        }

        for (var index = 0; index < included.Length; index++)
        {
            var type = included[index];
            if (!m_ComponentsInfo.Types.Contains(type)) return false;
        }

        for (var index = 0; index < excluded.Length; index++)
        {
            var type = excluded[index];
            if (m_ComponentsInfo.Types.Contains(type)) return false;
        }

        return true;
    }

    /// <summary>
    /// Retrieves all components of the specified type from all chunks in this archetype.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="result">A list that will be populated with snippets containing the requested components.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetComponents<T>(List<Snippet<T>> result)
    {
        foreach (var chunk in m_Chunks)
        {
            var mem = chunk.GetAll<T>();
            result.Add(mem);
        }
    }

    /// <summary>
    /// Gets an available chunk for storing entities, creating a new one if all existing chunks are full.
    /// </summary>
    /// <returns>A chunk that has available space for storing entities.</returns>
    public Chunk GetChunk()
    {
        Chunk targetChunk = null!;
        foreach (var chunk in m_Chunks)
        {
            if (chunk.IsFull) continue;
            targetChunk = chunk;
            break;
        }

        if (targetChunk == null!)
        {
            targetChunk = new Chunk(m_ComponentTypes);
            m_Chunks.Add(targetChunk);
        }

        return targetChunk;
    }

    /// <summary>
    /// Contains information about the component types in this archetype, including their hash code for efficient matching.
    /// </summary>
    private readonly struct ComponentsInfo
    {
        internal readonly HashSet<Type> Types;
        internal readonly int           HashCode;

        /// <summary>
        /// Initializes a new instance of the ComponentsInfo struct with the specified component types.
        /// </summary>
        /// <param name="types">A hash set of component types to store and generate a hash code for.</param>
        public ComponentsInfo(HashSet<Type> types)
        {
            Types    = types;
            HashCode = types.Aggregate(0, (hash, type) => hash | type.GetHashCode());
        }
    }
}