using System.Runtime.CompilerServices;
using Gubbins.Collections;
using Gubbins.Spawner;

namespace Gubbins.Entities;

/// <summary>
/// Provides an in-memory implementation of an entity repository that manages entities using an archetype-based storage system.
/// Elements are organized by their component types into archetypes, with efficient storage and retrieval mechanisms.
/// </summary>
public class EntityRepository : IEntityQuery, IEntityCommand
{
    private const uint INITIAL_ENTITY_VERSION = 1;

    private readonly List<Archetype>           m_Archetypes     = [];
    private readonly ExpandArray<EntityRecord> m_Entities       = [];
    private readonly Queue<int>                m_RemovedIndexes = new();

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public bool Contains(int index) => index >= 0 && m_Entities.Count > index && m_Entities[index].IndexInChunk >= 0;

    /// <inheritdoc />
    public bool Contains(Entity entity) => Contains(entity.Index) && m_Entities[entity.Index].Entity.Version == entity.Version;

    /// <inheritdoc />
    public Chunks Search(ComponentFilter filter)
    {
        var pool = Pool<List<Chunk>>.Default;
        var chunks = pool.Spawn();
        for (var i = 0; i < m_Archetypes.Count; i++)
        {
            var archetype = m_Archetypes[i];
            if (archetype.MatchComponents(filter))
            {
                chunks.AddRange(archetype.Chunks);
            }
        }

        return new Chunks(chunks, pool);
    }

    /// <inheritdoc />
    public Chunks Search(ComponentTypesMatching filter)
    {
        var pool = Pool<List<Chunk>>.Default;
        var chunks = pool.Spawn();
        for (var i = 0; i < m_Archetypes.Count; i++)
        {
            var archetype = m_Archetypes[i];
            if (filter.Invoke(archetype.Types))
            {
                chunks.AddRange(archetype.Chunks);
            }
        }

        return new Chunks(chunks, pool);
    }

    /// <inheritdoc />
    public void Update<T>(int index, T component) where T : unmanaged
    {
        if (!Contains(index))
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }

        ref var record = ref m_Entities[index];
        record.Chunk.Set(record.IndexInChunk, component);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Insert(Span<byte> data, Span<Type> componentTypes)
    {
        var archetype = RequireArcheType(componentTypes);
        var chunk = archetype.GetPartiallyFilledChunk();
        var chunkInIndex = chunk.Add(data, componentTypes);
        return AddEntity(chunk, chunkInIndex);
    }

    /// <inheritdoc />
    public void InsertAll(int count, Span<byte> data, Span<Type> componentTypes)
    {
        var archetype = RequireArcheType(componentTypes);
        for (var i = 0; i < count; i++)
        {
            var chunk = archetype.GetPartiallyFilledChunk();
            var chunkInIndex = chunk.Add(data, componentTypes);
            AddEntity(chunk, chunkInIndex);
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
    /// <param name="componentTypes">The span of component types that define the archetype structure.</param>
    /// <returns>An archetype instance that can store entities with the specified component types.</returns>
    private Archetype RequireArcheType(Span<Type> componentTypes)
    {
        Archetype? archetype = null;
        for (var i = 0; i < m_Archetypes.Count; i++)
        {
            if (!m_Archetypes[i].MatchComponents(componentTypes))
            {
                continue;
            }

            archetype = m_Archetypes[i];
        }

        if (archetype == null)
        {
            archetype = new Archetype(componentTypes.ToArray());
            m_Archetypes.Add(archetype);
        }

        return archetype;
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
            record.Entity       = new Entity {Index = removedIndex, Version = record.Entity.Version};
            record.Chunk        = chunk;
            record.IndexInChunk = chunkInIndex;
            chunk.Set(chunkInIndex, record.Entity);
            Count++;
            return record.Entity;
        }

        var entity = new Entity {Index = m_Entities.Count, Version = INITIAL_ENTITY_VERSION};
        m_Entities.Add(new EntityRecord
        {
            Entity       = entity,
            Chunk        = chunk,
            IndexInChunk = chunkInIndex
        });
        chunk.Set(chunkInIndex, entity);
        Count++;
        return entity;
    }
}