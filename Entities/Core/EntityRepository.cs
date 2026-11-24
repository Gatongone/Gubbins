using System.Runtime.CompilerServices;
using Gubbins.Collections;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Provides an in-memory implementation of an entity repository that manages entities using an archetype-based storage system.
/// Elements are organized by their component types into archetypes, with efficient storage and retrieval mechanisms.
/// </summary>
/// <remarks>
/// <para>
/// The repository allocates all chunk storage from the <see cref="Allocator"/> passed to its constructor, leaving the
/// allocation policy (native heap, arena, virtual memory, …) to the business layer.
/// </para>
/// <para>
/// It is both roles of the command/query split: <see cref="IEntityQuery.Query(ComponentFilter)"/> answers with the
/// read-only <see cref="ReadOnlyChunks"/>, and <see cref="IEntityCommand.Command(ComponentFilter)"/> with the writable
/// <see cref="Chunks"/>. A reader should depend on <see cref="IEntityQuery"/> and can then only query; a writer depends
/// on <see cref="IEntityCommand"/> and can mutate structure or component data.
/// </para>
/// </remarks>
public class EntityRepository : IEntityQuery, IEntityCommand, IDisposable
{
    private const uint INITIAL_ENTITY_VERSION = 1;

    /// <summary>
    /// The largest transition payload that is built on the stack instead of the heap.
    /// </summary>
    private const int MAX_STACK_PAYLOAD = 512;

    /// <summary>
    /// The write clock every mutation takes a value from.
    /// </summary>
    private readonly ChangeClock m_Clock = new();

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

    /// <summary>
    /// Gets the change version of the most recent write, or <c>0</c> when nothing has been written yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every write operation takes the next version before it stamps the chunks it touched, so a chunk written at
    /// <c>N</c> carries <c>N</c> and a filter with a lower change version sees it. Nothing has to be advanced by hand:
    /// the clock moves on every write, which is why a reader that captured an older value can never miss a write that
    /// happened after it looked.
    /// </para>
    /// <para>
    /// Prefer <see cref="ChangeCursor"/> over threading this value through call sites; it captures and advances the
    /// version for you. Read the property directly when a caller needs an explicit point in the write timeline, such as
    /// "everything written since the level loaded".
    /// </para>
    /// </remarks>
    public long Version => m_Clock.Current;

    /// <inheritdoc/>
    public bool Contains(int index) => index >= 0 && m_Entities.Count > index && m_Entities[index].IndexInChunk >= 0;

    /// <inheritdoc/>
    public bool Contains(Entity entity) => Contains(entity.Index) && m_Entities[entity.Index].Entity.Version == entity.Version;

    /// <summary>
    /// Advances the change clock and returns the new value.
    /// </summary>
    /// <returns>The version the following writes are compared against.</returns>
    /// <remarks>
    /// Writes advance the clock themselves, so a caller only needs this to mark an explicit point in the write timeline
    /// — the moment a scene finished loading, for example, so that everything written afterwards can be selected with
    /// <see cref="Chunks.ChangedSince"/>.
    /// </remarks>
    public long NextVersion() => m_Clock.Next();

    /// <summary>
    /// Gets the writable view of the chunks that match <paramref name="filter"/>, which is the command side of the
    /// repository.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>The matching chunks, as the command side's writable view.</returns>
    /// <remarks>
    /// This implements <see cref="IEntityCommand.Command(ComponentFilter)"/>. Iterating the view is a write access: it
    /// hands out <see cref="Span{T}"/> segments and <c>ref</c> elements, and it stamps every chunk it visits as changed,
    /// so another system's change-filtered query picks that chunk up. Readers use <see cref="Query(ComponentFilter)"/>
    /// instead, directly or through <see cref="IEntityQuery"/>.
    /// </remarks>
    public Chunks Command(ComponentFilter filter) => new(m_Archetypes, m_Allocator, filter, m_Clock);

    /// <inheritdoc cref="Command(ComponentFilter)"/>
    public Chunks Command(ComponentTypesMatching filter) => new(m_Archetypes, m_Allocator, filter, m_Clock);

    /// <inheritdoc cref="Command(ComponentFilter)"/>
    public Chunks Command(ComponentFilter filter, long changedSince) => Command(filter).ChangedSince(changedSince);

    /// <inheritdoc cref="Command(ComponentFilter)"/>
    public Chunks Command(ComponentTypesMatching filter, long changedSince) => Command(filter).ChangedSince(changedSince);

    /// <summary>
    /// Gets the read-only view of the chunks that match <paramref name="filter"/>, which is the query side of the
    /// repository.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>The matching chunks, as the query side's read-only view.</returns>
    /// <remarks>
    /// This implements <see cref="IEntityQuery.Query(ComponentFilter)"/>. The view hands out
    /// <see cref="ReadOnlySpan{T}"/> segments and <c>ref readonly</c> elements, and it never marks a chunk as changed.
    /// </remarks>
    public ReadOnlyChunks Query(ComponentFilter filter) => Command(filter).AsReadOnly();

    /// <inheritdoc cref="Query(ComponentFilter)"/>
    public ReadOnlyChunks Query(ComponentTypesMatching filter) => Command(filter).AsReadOnly();

    /// <inheritdoc cref="Query(ComponentFilter)"/>
    public ReadOnlyChunks Query(ComponentFilter filter, long changedSince) => Command(filter, changedSince).AsReadOnly();

    /// <inheritdoc cref="Query(ComponentFilter)"/>
    public ReadOnlyChunks Query(ComponentTypesMatching filter, long changedSince) => Command(filter, changedSince).AsReadOnly();

    /// <inheritdoc/>
    public void Update<T>(int index, T component) where T : unmanaged
    {
        if (!Contains(index))
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }

        ref var record = ref m_Entities[index];
        record.Chunk.Set(record.IndexInChunk, component);
        record.Chunk.MarkWritten(m_Clock.Next());
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
        record.Chunk.MarkWritten(m_Clock.Next());
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
    /// Enables or disables the entity at the specified index.
    /// </summary>
    /// <param name="index">The index of the entity to change.</param>
    /// <param name="enabled">Whether the entity should be enabled.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it already was in that state.</returns>
    /// <exception cref="InvalidOperationException">No entity exists at <paramref name="index"/>.</exception>
    /// <remarks>
    /// Disabling moves the entity into the archetype that carries <see cref="Disabled"/>, which is what hides it from
    /// queries; enabling moves it back. The handle keeps its index and version, so it stays valid across the change.
    /// </remarks>
    public bool SetEnabled(int index, bool enabled)
    {
        if (!Contains(index))
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }

        return SetDisabled(index, !enabled);
    }

    /// <summary>
    /// Enables or disables the entity if its version matches the currently stored version.
    /// </summary>
    /// <param name="entity">The entity to change.</param>
    /// <param name="enabled">Whether the entity should be enabled.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it already was in that state.</returns>
    /// <exception cref="InvalidOperationException">The handle is stale or no longer refers to a live entity.</exception>
    public bool SetEnabled(Entity entity, bool enabled)
    {
        RequireAlive(entity);
        return SetDisabled(entity.Index, !enabled);
    }

    /// <summary>
    /// Enables the entity if its version matches the currently stored version.
    /// </summary>
    /// <param name="entity">The entity to enable.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it was already enabled.</returns>
    public bool Enable(Entity entity) => SetEnabled(entity, true);

    /// <summary>
    /// Disables the entity if its version matches the currently stored version.
    /// </summary>
    /// <param name="entity">The entity to disable.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it was already disabled.</returns>
    public bool Disable(Entity entity) => SetEnabled(entity, false);

    /// <summary>
    /// Determines whether the entity at the specified index is enabled.
    /// </summary>
    /// <param name="index">The index of the entity to test.</param>
    /// <exception cref="InvalidOperationException">No entity exists at <paramref name="index"/>.</exception>
    public bool IsEnabled(int index)
    {
        if (!Contains(index))
        {
            throw new InvalidOperationException($"Entity with index {index} is not valid.");
        }

        return !m_Entities[index].Chunk.ContainsType(typeof(Disabled));
    }

    /// <summary>
    /// Determines whether the entity is enabled.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    public bool IsEnabled(Entity entity)
    {
        RequireAlive(entity);
        return !m_Entities[entity.Index].Chunk.ContainsType(typeof(Disabled));
    }

    /// <summary>
    /// Adds a component to an existing entity, moving it into the archetype that carries the extra type.
    /// </summary>
    /// <typeparam name="T">The component type to add.</typeparam>
    /// <param name="entity">The entity to change.</param>
    /// <param name="value">The component value to store.</param>
    /// <returns><see langword="true"/> if the component was added; <see langword="false"/> if the entity already held one.</returns>
    /// <exception cref="InvalidOperationException">The handle is stale or no longer refers to a live entity.</exception>
    /// <remarks>
    /// The entity keeps its index and version, so its handle survives the move, but it changes chunk: it may end up in
    /// a different chunk of the target archetype, and the source chunk compacts.
    /// </remarks>
    public bool AddComponent<T>(Entity entity, T value) where T : unmanaged
    {
        RequireAlive(entity);

        ref var record = ref m_Entities[entity.Index];
        var sourceTypes = record.Chunk.ComponentTypes;
        if (record.Chunk.ContainsType(typeof(T)))
        {
            return false;
        }

        var types = new Type[sourceTypes.Length + 1];
        sourceTypes.CopyTo(types);
        types[^1] = typeof(T);

        var target = RequireArcheType(null, types);
        var components = Components.Create(in value);
        return MoveToArchetype(entity.Index, target, in components);
    }

    /// <summary>
    /// Removes a component from an existing entity, moving it into the archetype without the type.
    /// </summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity to change.</param>
    /// <returns><see langword="true"/> if the component was removed; <see langword="false"/> if the entity did not hold one.</returns>
    /// <exception cref="InvalidOperationException">The handle is stale or no longer refers to a live entity.</exception>
    public bool RemoveComponent<T>(Entity entity) where T : unmanaged
    {
        RequireAlive(entity);

        ref var record = ref m_Entities[entity.Index];
        if (!record.Chunk.ContainsType(typeof(T)))
        {
            return false;
        }

        var target = RequireArcheType(null, WithType(record.Chunk.ComponentTypes, typeof(T), false));
        var none = default(Components);
        return MoveToArchetype(entity.Index, target, in none);
    }

    /// <summary>
    /// Throws unless <paramref name="entity"/> is a live handle whose version matches the stored one.
    /// </summary>
    /// <param name="entity">The handle to check.</param>
    private void RequireAlive(Entity entity)
    {
        if (!Contains(entity.Index) || m_Entities[entity.Index].Entity.Version != entity.Version)
        {
            throw new InvalidOperationException($"Entity handle is stale or invalid. Expected version {entity.Version} at index {entity.Index}, but found {(Contains(entity.Index) ? m_Entities[entity.Index].Entity.Version : "invalid")}.");
        }
    }

    /// <summary>
    /// Adds or removes <see cref="Disabled"/> on the entity at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The index of the entity to change.</param>
    /// <param name="disabled">Whether the entity should carry the tag.</param>
    /// <returns><see langword="true"/> if the entity changed state.</returns>
    private bool SetDisabled(int index, bool disabled)
    {
        ref var record = ref m_Entities[index];
        if (record.Chunk.ContainsType(typeof(Disabled)) == disabled)
        {
            return false;
        }

        var target = RequireArcheType(null, WithType(record.Chunk.ComponentTypes, typeof(Disabled), disabled));
        if (!disabled)
        {
            var none = default(Components);
            return MoveToArchetype(index, target, in none);
        }

        var tag = Components.Create(default(Disabled));
        return MoveToArchetype(index, target, in tag);
    }

    /// <summary>
    /// Moves the entity at <paramref name="index"/> into <paramref name="target"/>, carrying its components over.
    /// </summary>
    /// <param name="index">The index of the entity to move.</param>
    /// <param name="target">The archetype to move the entity into.</param>
    /// <param name="added">
    /// The values of the components the target holds but the entity does not, laid out in
    /// <see cref="Components.Types"/> order. Pass <see langword="default"/> when the target only drops types.
    /// </param>
    /// <returns><see langword="true"/> once the entity lives in the target archetype.</returns>
    /// <remarks>
    /// <para>
    /// This is the implementation behind enable/disable and add/remove component: an archetype is a fixed set of
    /// component types, so changing that set is a move between chunks. The entity handle is preserved, which is what
    /// makes the change invisible to code that holds a handle rather than a record.
    /// </para>
    /// <para>
    /// The target chunk is filled before the source row is released, so a failed add leaves the entity where it was.
    /// </para>
    /// </remarks>
    private bool MoveToArchetype(int index, Archetype target, in Components added)
    {
        ref var record = ref m_Entities[index];
        var source = record.Chunk;
        var sourceIndex = record.IndexInChunk;
        var entity = record.Entity;
        var targetTypes = target.Types;

        var size = target.ComponentSize;
        Span<byte> payload = size <= MAX_STACK_PAYLOAD ? stackalloc byte[MAX_STACK_PAYLOAD] : new byte[size];
        payload = payload.Slice(0, size);
        payload.Clear();

        // Fill the components the entity does not hold yet. CopyRow leaves these slots alone, because the source chunk
        // has no column for them.
        var addedTypes = added.Types;
        var addedBytes = added.Bytes;
        var offset = 0;
        for (var i = 0; i < addedTypes.Length; i++)
        {
            var elementSize = (int)Native.GetStackSize(addedTypes[i]);
            var slot = OffsetOf(targetTypes, addedTypes[i]);
            if (slot < 0)
            {
                throw new InvalidOperationException($"Type '{addedTypes[i]}' is not a component of the target archetype.");
            }

            addedBytes.Slice(offset, elementSize).CopyTo(payload.Slice(slot, elementSize));
            offset += elementSize;
        }

        source.CopyRow(sourceIndex, targetTypes, payload);

        // One version for the whole transition: both chunks are part of the same write.
        var version = m_Clock.Next();

        var chunk = target.GetPartiallyFilledChunk();
        var chunkIndex = chunk.Add(payload, targetTypes);
        chunk.Set(chunkIndex, entity);
        chunk.MarkWritten(version);

        source.Remove(sourceIndex, out var movedEntity, out var movedFromIndex);
        source.MarkWritten(version);

        // The source chunk compacts a row from its tail into the slot the entity left behind.
        if (movedFromIndex >= 0)
        {
            m_Entities[movedEntity.Index].IndexInChunk = sourceIndex;
        }

        record.Chunk = chunk;
        record.IndexInChunk = chunkIndex;
        return true;
    }

    /// <summary>
    /// Builds the component type list of an archetype that gains or loses one type.
    /// </summary>
    /// <param name="componentTypes">The component types of the current archetype.</param>
    /// <param name="type">The type to add or remove.</param>
    /// <param name="add">Whether <paramref name="type"/> is added or removed.</param>
    private static Type[] WithType(ReadOnlySpan<Type> componentTypes, Type type, bool add)
    {
        var types = new Type[add ? componentTypes.Length + 1 : componentTypes.Length - 1];
        var count = 0;
        for (var i = 0; i < componentTypes.Length; i++)
        {
            if (!add && componentTypes[i] == type)
            {
                continue;
            }

            types[count++] = componentTypes[i];
        }

        if (add)
        {
            types[count] = type;
        }

        return types;
    }

    /// <summary>
    /// Finds the byte offset of a component type inside an archetype's component layout.
    /// </summary>
    /// <param name="types">The component types, in layout order.</param>
    /// <param name="type">The type to locate.</param>
    /// <returns>The offset in bytes, or <c>-1</c> when the type is absent.</returns>
    private static int OffsetOf(ReadOnlySpan<Type> types, Type type)
    {
        var offset = 0;
        for (var i = 0; i < types.Length; i++)
        {
            if (types[i] == type)
            {
                return offset;
            }

            offset += (int)Native.GetStackSize(types[i]);
        }

        return -1;
    }

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
            chunk.MarkWritten(m_Clock.Next());
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
        chunk.MarkWritten(m_Clock.Next());
        Count++;
        return entity;
    }
}
