namespace Gubbins.Entities;

public interface IEntityQuery
{
    /// <summary>
    /// Gets the total number of entities in the repository.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets a read-only list of all archetypes available in the repository.
    /// </summary>
    IReadOnlyList<IArchetype> Archetypes { get; }

    /// <summary>
    /// Determines whether an entity exists at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index to check for entity existence.</param>
    /// <returns>True if an entity exists at the specified index; otherwise, false.</returns>
    bool Contains(int index);

    /// <summary>
    /// Retrieves the entity record at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the entity to retrieve.</param>
    /// <returns>The EntityRecord at the specified index.</returns>
    EntityRecord Get(int index);
}