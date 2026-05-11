using System.Runtime.CompilerServices;
using Gubbins.Collection;

namespace Gubbins.Entities;

/// <summary>
/// Provides an in-memory implementation of an entity repository that manages entities using an archetype-based storage system.
/// Entities are organized by their component types into archetypes, with efficient storage and retrieval mechanisms.
/// </summary>
public class EntityRepository : IEntityQuery, IEntityCommand
{
    private readonly List<Archetype>           m_Archetypes     = [];
    private readonly ExpandArray<EntityRecord> m_Entities       = [];
    private readonly Queue<int>                m_RemovedIndexes = new();

    /// <summary>
    /// Gets a read-only collection of all archetypes currently managed by this repository.
    /// </summary>
    public IReadOnlyList<IArchetype> Archetypes => m_Archetypes;

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <inheritdoc />
    public bool Contains(int index) => index >= 0 && m_Entities.Count > index && m_Entities[index].Entity.Valid;

    /// <inheritdoc />
    public void Update<T>(int index, T component) where T : unmanaged
    {
        ref var record = ref m_Entities[index];
        if (!record.Entity.Valid)
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }
        record.Chunk.Set(record.IndexInChunk, component);
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Entity Insert(Span<byte> data, Span<Type> componentTypes)
    {
        var archetype = RequireArcheType(componentTypes);
        var chunk = archetype.GetChunk();
        var chunkInIndex = chunk.Add(data, componentTypes);
        return AddEntity(chunk, chunkInIndex);
    }

    /// <inheritdoc />
    public void InsertAll(int count, Span<byte> data, Span<Type> componentTypes)
    {
        var archetype = RequireArcheType(componentTypes);
        for (var i = 0; i < count; i++)
        {
            var chunk = archetype.GetChunk();
            var chunkInIndex = chunk.Add(data, componentTypes);
            AddEntity(chunk, chunkInIndex);
        }
    }

    /// <summary>
    /// Gets an existing archetype that matches the specified component types, or creates a new one if none exists.
    /// </summary>
    /// <param name="componentTypes">The span of component types that define the archetype structure.</param>
    /// <returns>An archetype instance that can store entities with the specified component types.</returns>
    private Archetype RequireArcheType(Span<Type> componentTypes)
    {
        Archetype archetype = null!;
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
    /// <returns>The newly created or reused entity with its index and validity status set appropriately.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Entity AddEntity(Chunk chunk, int chunkInIndex)
    {
        if (m_RemovedIndexes.TryDequeue(out var removedIndex))
        {
            ref var record = ref m_Entities[removedIndex];
            record.Entity.Valid = true;
            record.Chunk        = chunk;
            record.IndexInChunk = chunkInIndex;
            Count++;
            return record.Entity;
        }

        var entity = new Entity {Index = m_Entities.Count, Valid = true};
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

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Delete(int index)
    {
        if (index < 0 || index >= m_Entities.Count)
        {
            return false;
        }

        ref var record = ref m_Entities[index];
        if (!record.Entity.Valid)
        {
            return false;
        }

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
        record.Entity.Valid = false;
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

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public EntityRecord Get(int index) => m_Entities[index];
}