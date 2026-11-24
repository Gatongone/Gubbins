namespace Gubbins.Entities;

/// <summary>
/// Commands for modifying entities in the repository.
/// </summary>
public interface IEntityCommand
{
    /// <summary>
    /// Gets the change version of the most recent write, or <c>0</c> when nothing has been written yet.
    /// </summary>
    /// <remarks>
    /// Every command that writes takes the next version from the same clock the readers use, so this value doubles as the
    /// query side's <see cref="IEntityQuery.Version"/> and a <see cref="ChangeCursor"/> can advance on either role.
    /// </remarks>
    long Version { get; }

    /// <summary>
    /// Inserts a new entity into the repository with the specified component data and types.
    /// </summary>
    /// <param name="data">A span containing the serialized component data for the entity.</param>
    /// <param name="componentTypes">A span containing the types of components associated with the entity.</param>
    /// <returns>The newly created Entity instance.</returns>
    Entity Insert(Components components);

    /// <summary>
    /// Deletes the entity at the specified index from the repository.
    /// </summary>
    /// <param name="index">The zero-based index of the entity to delete.</param>
    /// <returns>True if the entity was successfully deleted; otherwise, false.</returns>
    bool Delete(int index);

    /// <summary>
    /// Inserts multiple entities into the repository with the same component configuration.
    /// </summary>
    /// <param name="count">The number of entities to insert.</param>
    /// <param name="data">A span containing the serialized component data for all entities.</param>
    /// <param name="componentTypes">A span containing the types of components associated with the entities.</param>
    void InsertAll(int count, Components components);

    /// <summary>
    /// Deletes multiple entities from the repository at the specified indexes.
    /// </summary>
    /// <param name="indexes">A parameter array of spans containing the zero-based indexes of entities to delete.</param>
    /// <returns>The number of entities that were successfully deleted.</returns>
    int DeleteAll(params Span<int> indexes);

    /// <summary>
    /// Updates the component data of a specific entity for a given component type.
    /// </summary>
    /// <param name="index">The index of entity whose component data is to be updated.</param>
    /// <param name="component">The new component data to be set for the entity.</param>
    /// <typeparam name="T">The type of the component being updated, which must be an unmanaged type.</typeparam>
    void Update<T>(int index, T component) where T : unmanaged;

    /// <summary>
    /// Enables or disables the entity at the specified index.
    /// </summary>
    /// <param name="index">The index of the entity to change.</param>
    /// <param name="enabled">Whether the entity should be enabled.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it already was in that state.</returns>
    bool SetEnabled(int index, bool enabled);

    /// <summary>
    /// Enables or disables the entity if its version matches the currently stored version.
    /// </summary>
    /// <param name="entity">The entity to change.</param>
    /// <param name="enabled">Whether the entity should be enabled.</param>
    /// <returns><see langword="true"/> if the entity changed state; <see langword="false"/> if it already was in that state.</returns>
    bool SetEnabled(Entity entity, bool enabled);

    /// <summary>
    /// Determines whether the entity at the specified index is enabled.
    /// </summary>
    /// <param name="index">The index of the entity to test.</param>
    bool IsEnabled(int index);

    /// <summary>
    /// Determines whether the entity is enabled.
    /// </summary>
    /// <param name="entity">The entity to test.</param>
    bool IsEnabled(Entity entity);

    /// <summary>
    /// Adds a component to an existing entity, moving it into the archetype that carries the extra type.
    /// </summary>
    /// <param name="entity">The entity to change.</param>
    /// <param name="value">The component value to store.</param>
    /// <typeparam name="T">The component type to add, which must be an unmanaged type.</typeparam>
    /// <returns><see langword="true"/> if the component was added; <see langword="false"/> if the entity already held one.</returns>
    bool AddComponent<T>(Entity entity, T value) where T : unmanaged;

    /// <summary>
    /// Removes a component from an existing entity, moving it into the archetype without the type.
    /// </summary>
    /// <param name="entity">The entity to change.</param>
    /// <typeparam name="T">The component type to remove, which must be an unmanaged type.</typeparam>
    /// <returns><see langword="true"/> if the component was removed; <see langword="false"/> if the entity did not hold one.</returns>
    bool RemoveComponent<T>(Entity entity) where T : unmanaged;

    /// <summary>
    /// Filters the entities in the repository based on the specified component criteria and returns the writable view of
    /// the matching chunks.
    /// </summary>
    /// <param name="componentFilter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>The chunks that contain entities matching the specified component criteria.</returns>
    /// <remarks>
    /// Iterating this view is a write access: it hands out <see cref="Span{T}"/> segments and <c>ref</c> elements, and it
    /// stamps every chunk it visits as changed, so a change-filtered query of another system picks that chunk up. The
    /// read-only counterpart of this view is what <see cref="IEntityQuery.Query(ComponentFilter)"/> returns.
    /// </remarks>
    Chunks Command(ComponentFilter componentFilter);

    /// <summary>
    /// Filters the entities in the repository based on the specified component criteria and returns the writable view of
    /// the matching chunks.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>The chunks that contain entities matching the specified component criteria.</returns>
    /// <remarks>
    /// Iterating this view is a write access: it hands out <see cref="Span{T}"/> segments and <c>ref</c> elements, and it
    /// stamps every chunk it visits as changed, so a change-filtered query of another system picks that chunk up.
    /// </remarks>
    Chunks Command(ComponentTypesMatching filter);

    /// <summary>
    /// Filters the entities in the repository and returns the writable view of the chunks that also changed after
    /// <paramref name="changedSince"/>.
    /// </summary>
    /// <param name="componentFilter">A filter that specifies the component criteria for matching entities.</param>
    /// <param name="changedSince">The version the caller captured at the end of its previous run.</param>
    /// <returns>The matching chunks that were written after <paramref name="changedSince"/>.</returns>
    Chunks Command(ComponentFilter componentFilter, long changedSince);

    /// <summary>
    /// Filters the entities in the repository and returns the writable view of the chunks that also changed after
    /// <paramref name="changedSince"/>.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <param name="changedSince">The version the caller captured at the end of its previous run.</param>
    /// <returns>The matching chunks that were written after <paramref name="changedSince"/>.</returns>
    Chunks Command(ComponentTypesMatching filter, long changedSince);
}
