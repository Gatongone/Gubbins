namespace Gubbins.Entities;

/// <summary>
/// Querying entities in the repository based on their component composition and existence.
/// </summary>
public interface IEntityQuery
{
    /// <summary>
    /// Gets the total number of entities in the repository.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Determines whether an entity exists at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index to check for entity existence.</param>
    /// <returns>True if an entity exists at the specified index; otherwise, false.</returns>
    bool Contains(int index);

    /// <summary>
    /// Determines whether an entity exists.
    /// </summary>
    /// <param name="entity">The entity handle to check for existence.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    bool Contains(Entity entity);

    /// <summary>
    /// Filters the entities in the repository based on the specified component criteria and returns a collection of matching chunks.
    /// </summary>
    /// <param name="componentFilter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>A collection of chunks that contain entities matching the specified component criteria.</returns>
    Chunks Search(ComponentFilter componentFilter);

    /// <summary>
    /// Filters the entities in the repository based on the specified component criteria and returns a collection of matching chunks.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>A collection of chunks that contain entities matching the specified component criteria.</returns>
    Chunks Search(ComponentTypesMatching filter);
}

public delegate bool ComponentTypesMatching(ReadOnlySpan<Type> componentTypes);
