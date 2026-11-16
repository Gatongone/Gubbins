using System.Runtime.CompilerServices;
using Gubbins.Collections;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Provides an in-memory implementation of an entity repository that manages entities using an archetype-based storage system.
/// Elements are organized by their component types into archetypes, with efficient storage and retrieval mechanisms.
/// </summary>
/// <remarks>
/// The repository allocates all chunk storage from the <see cref="Allocator"/> passed to its constructor, leaving the
/// allocation policy (native heap, arena, virtual memory, …) to the business layer.
/// </remarks>
public class EntityRepository : IEntityQuery, IEntityCommand, IDisposable
{
    private const uint INITIAL_ENTITY_VERSION = 1;

    private readonly Allocator                 m_Allocator;
    private readonly int                       m_ChunkSize;
    private readonly List<Archetype>           m_Archetypes     = [];
    private readonly ExpandArray<EntityRecord> m_Entities       = [];
    private readonly Queue<int>                m_RemovedIndexes = new();

    /// <summary>
    /// Maps an interned component type array onto the archetype that already matches it, so a typed insert does an
    /// identity lookup instead of scanning every archetype in the repository.
    /// </summary>
    private readonly Dictionary<Type[], Archetype> m_ArchetypeCache = new(ReferenceComparer.Instance);

    /// <summary>
    /// Initializes a repository whose chunks use the default buffer size.
    /// </summary>
    /// <param name="allocator">The allocator used for chunk storage.</param>
    public EntityRepository(Allocator allocator) : this(allocator, Chunk.DefaultChunkSize) { }

    /// <summary>
    /// Initializes a repository whose chunks use the specified buffer size.
    /// </summary>
    /// <param name="allocator">The allocator used for chunk storage.</param>
    /// <param name="chunkSizeBytes">The size, in bytes, of each chunk buffer.</param>
    public EntityRepository(Allocator allocator, int chunkSizeBytes)
    {
        if (chunkSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes));
        }

        m_Allocator = allocator;
        m_ChunkSize = chunkSizeBytes;
    }

    /// <inheritdoc/>
    public int Count { get; private set; }

    /// <inheritdoc/>
    public bool Contains(int index) => index >= 0 && m_Entities.Count > index && m_Entities[index].IndexInChunk >= 0;

    /// <inheritdoc/>
    public bool Contains(Entity entity) => Contains(entity.Index) && m_Entities[entity.Index].Entity.Version == entity.Version;

    /// <inheritdoc/>
    public Chunks Search(ComponentFilter filter) => new(m_Archetypes, m_Allocator, filter);

    /// <inheritdoc/>
    public Chunks Search(ComponentTypesMatching filter) => new(m_Archetypes, m_Allocator, filter);

    /// <inheritdoc/>
    public void Update<T>(int index, T component) where T : unmanaged
    {
        if (!Contains(index))
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }

        ref var record = ref m_Entities[index];
        record.Chunk.Set(record.IndexInChunk, component);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Insert(Components components) => InsertRaw(components.TypeArray, components.Bytes, components.Types);

    internal Entity InsertRaw(Type[]? cacheKey, ReadOnlySpan<byte> data, ReadOnlySpan<Type> componentTypes)
    {
        var archetype = RequireArcheType(cacheKey, componentTypes);
        var chunk = archetype.GetPartiallyFilledChunk();
        var chunkInIndex = chunk.Add(data, componentTypes);
        return AddEntity(chunk, chunkInIndex);
    }

    /// <inheritdoc/>
    public void InsertAll(int count, Components components)
        => InsertRawAll(count, components.TypeArray, components.Bytes, components.Types);

    internal void InsertRawAll(int count, Type[]? cacheKey, ReadOnlySpan<byte> data, ReadOnlySpan<Type> componentTypes)
    {
        var archetype = RequireArcheType(cacheKey, componentTypes);
        for (var i = 0; i < count; i++)
        {
            var chunk = archetype.GetPartiallyFilledChunk();
            var chunkInIndex = chunk.Add(data, componentTypes);
            AddEntity(chunk, chunkInIndex);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Delete(int index)
    {
        if (!Contains(index))
        {
            return false;
        }

        ref var record = ref m_Entities[index];

        var indexInChunk = record.IndexInChunk;
        if (!record.Chunk.Remove(indexInChunk, out var movedEntity, out var movedFromIndex))
        {
            return false;
        }

        if (movedFromIndex >= 0)
        {
            ref var movedRecord = ref m_Entities[movedEntity.Index];
            movedRecord.IndexInChunk = indexInChunk;
        }

        m_RemovedIndexes.Enqueue(index);
        record.IndexInChunk = -1;
        record.Entity.Version++;
        Count--;
        return true;
    }

    /// <inheritdoc/>
    public int DeleteAll(params Span<int> indexes)
    {
        var removedCount = 0;
        for (var i = indexes.Length - 1; i >= 0; i--)
        {
            if (Delete(indexes[i]))
            {
                removedCount++;
            }
        }

        return removedCount;
    }

    /// <summary>
    /// Deletes the entity if its version matches the currently stored version.
    /// </summary>
    public bool Delete(Entity entity)
    {
        if (!Contains(entity.Index))
        {
            return false;
        }

        // Check version match
        if (m_Entities[entity.Index].Entity.Version != entity.Version)
        {
            return false;
        }

        return Delete(entity.Index);
    }

    /// <summary>
    /// Updates a single component of the entity if its version matches the currently stored version.
    /// </summary>
    public void Update<T1>(Entity entity, T1 component1) where T1 : unmanaged
    {
        if (!Contains(entity.Index) || m_Entities[entity.Index].Entity.Version != entity.Version)
        {
            throw new InvalidOperationException($"Entity handle is stale or invalid. Expected version {entity.Version} at index {entity.Index}, but found {(Contains(entity.Index) ? m_Entities[entity.Index].Entity.Version : "invalid")}.");
        }

        Update(entity.Index, component1);
    }

    /// <summary>
    /// Gets the entity record at the specified index, which contains information about the entity's handle, its storage chunk, and its index within that chunk.
    /// </summary>
    /// <param name="index">The zero-based index of the entity record to retrieve.</param>
    /// <returns>The <see cref="EntityRecord"/> at the specified index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal EntityRecord GetRecord(int index) => m_Entities[index];

    /// <summary>
    /// Gets an existing archetype that matches the specified component types, or creates a new one if none exists.
    /// </summary>
    /// <param name="cacheKey">
    /// The interned type array the payload carried, or <see langword="null"/> when the caller only has a span (ECB
    /// playback).
    /// </param>
    /// <param name="componentTypes">The span of component types that define the archetype structure.</param>
    /// <returns>An archetype instance that can store entities with the specified component types.</returns>
    private Archetype RequireArcheType(Type[]? cacheKey, ReadOnlySpan<Type> componentTypes)
    {
        if (cacheKey != null && m_ArchetypeCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        Archetype archetype = null!;
        for (var i = 0; i < m_Archetypes.Count; i++)
        {
            if (!m_Archetypes[i].MatchComponents(componentTypes))
            {
                continue;
            }

            archetype = m_Archetypes[i];
            break;
        }

        if (archetype == null)
        {
            EnsureDistinctComponentTypes(componentTypes);
            archetype = new Archetype(componentTypes, m_Allocator, m_ChunkSize);
            m_Archetypes.Add(archetype);
        }

        if (cacheKey != null)
        {
            m_ArchetypeCache[cacheKey] = archetype;
        }

        return archetype;
    }

    /// <summary>
    /// Rejects a payload that carries the same component type twice.
    /// </summary>
    /// <param name="componentTypes">The component types of the archetype being created.</param>
    /// <remarks>
    /// A chunk addresses its columns by type, so two components of one type cannot both be stored. Without this
    /// guard the payload first lands in an archetype of the same width and only fails later inside
    /// <c>Chunk.Add</c> with a message about the input types.
    /// </remarks>
    private static void EnsureDistinctComponentTypes(ReadOnlySpan<Type> componentTypes)
    {
        for (var i = 0; i < componentTypes.Length; i++)
        {
            for (var j = i + 1; j < componentTypes.Length; j++)
            {
                if (componentTypes[i] == componentTypes[j])
                {
                    throw new InvalidOperationException($"An entity cannot hold two components of the same type '{componentTypes[i]}'.");
                }
            }
        }
    }

    /// <summary>Compares interned type arrays by reference, which is the archetype cache's key semantics.</summary>
    private sealed class ReferenceComparer : IEqualityComparer<Type[]>
    {
        /// <summary>The shared instance.</summary>
        public static readonly ReferenceComparer Instance = new();

        /// <inheritdoc/>
        public bool Equals(Type[]? x, Type[]? y) => ReferenceEquals(x, y);

        /// <inheritdoc/>
        public int GetHashCode(Type[] obj) => RuntimeHelpers.GetHashCode(obj);
    }

    /// <summary>
    /// Releases all chunk storage owned by the repository.
    /// </summary>
    public void Dispose()
    {
        for (var i = 0; i < m_Archetypes.Count; i++)
        {
            m_Archetypes[i].Dispose();
        }

        m_Archetypes.Clear();
        m_ArchetypeCache.Clear();
        m_Entities.Clear();
        m_RemovedIndexes.Clear();
        Count = 0;
    }

    /// <summary>
    /// Adds a new entity to the repository by either reusing a previously removed entity slot or creating a new one.
    /// Updates the repository's entity count and establishes the relationship between the entity and its storage chunk.
    /// </summary>
    /// <param name="chunk">The chunk where the entity's component data is stored.</param>
    /// <param name="chunkInIndex">The index within the chunk where the entity's data is located.</param>
    /// <returns>The newly created or reused entity with its index and version set appropriately.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity AddEntity(Chunk chunk, int chunkInIndex)
    {
        if (m_RemovedIndexes.TryDequeue(out var removedIndex))
        {
            ref var record = ref m_Entities[removedIndex];
            record.Entity = record.Entity with { Index = removedIndex };
            record.Chunk = chunk;
            record.IndexInChunk = chunkInIndex;
            chunk.Set(chunkInIndex, record.Entity);
            Count++;
            return record.Entity;
        }

        var entity = new Entity { Index = m_Entities.Count, Version = INITIAL_ENTITY_VERSION };
        m_Entities.Add(new EntityRecord
        {
            Entity = entity,
            Chunk = chunk,
            IndexInChunk = chunkInIndex
        });
        chunk.Set(chunkInIndex, entity);
        Count++;
        return entity;
    }
}
