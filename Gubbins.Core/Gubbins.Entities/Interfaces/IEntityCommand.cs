namespace Gubbins.Entities;

public interface IEntityCommand
{
    /// <summary>
    /// Inserts a new entity into the repository with the specified component data and types.
    /// </summary>
    /// <param name="data">A span containing the serialized component data for the entity.</param>
    /// <param name="componentTypes">A span containing the types of components associated with the entity.</param>
    /// <returns>The newly created Entity instance.</returns>
    Entity Insert(Span<byte> data, Span<Type> componentTypes);

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
    void InsertAll(int count, Span<byte> data, Span<Type> componentTypes);

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
}