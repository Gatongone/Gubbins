namespace Gubbins.Entities;

/// <summary>
/// The read side of the repository: queries, and nothing that mutates.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Query"/> on this interface yields a <see cref="ReadOnlyChunks"/> view, so a system that depends on
/// <see cref="IEntityQuery"/> cannot write component data, cannot mark a chunk as changed, and cannot reach a
/// <see cref="Chunks"/> at all. The counter design lives in <see cref="IEntityCommand"/>, whose <c>Query</c> overloads
/// yield that writable view; the two interfaces are complementary roles of one repository, not two implementations.
/// </para>
/// <para>
/// This is what makes command/query separation hold at the dependency level: a system states the role it needs, and the
/// compiler enforces it.
/// </para>
/// </remarks>
public interface IEntityQuery
{
    /// <summary>
    /// Gets the total number of entities in the repository.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the change version of the most recent write, or <c>0</c> when nothing has been written yet.
    /// </summary>
    /// <remarks>
    /// Writes advance it themselves, so a caller that only wants "what changed since I last looked" uses
    /// <see cref="ChangeCursor"/> and never touches this value. Read it directly to mark an explicit point in the write
    /// timeline, such as "everything written since the level loaded".
    /// </remarks>
    long Version { get; }

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
    /// Filters the entities in the repository based on the specified component criteria and returns a read-only view of
    /// the matching chunks.
    /// </summary>
    /// <param name="componentFilter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>A read-only view of the chunks that contain entities matching the specified component criteria.</returns>
    ReadOnlyChunks Query(ComponentFilter componentFilter);

    /// <summary>
    /// Filters the entities in the repository based on the specified component criteria and returns a read-only view of
    /// the matching chunks.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <returns>A read-only view of the chunks that contain entities matching the specified component criteria.</returns>
    ReadOnlyChunks Query(ComponentTypesMatching filter);

    /// <summary>
    /// Filters the entities in the repository and returns a read-only view of the chunks that also changed after
    /// <paramref name="changedSince"/>.
    /// </summary>
    /// <param name="componentFilter">A filter that specifies the component criteria for matching entities.</param>
    /// <param name="changedSince">The version the caller captured at the end of its previous run.</param>
    /// <returns>A read-only view of the matching chunks that were written after <paramref name="changedSince"/>.</returns>
    ReadOnlyChunks Query(ComponentFilter componentFilter, long changedSince);

    /// <summary>
    /// Filters the entities in the repository and returns a read-only view of the chunks that also changed after
    /// <paramref name="changedSince"/>.
    /// </summary>
    /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
    /// <param name="changedSince">The version the caller captured at the end of its previous run.</param>
    /// <returns>A read-only view of the matching chunks that were written after <paramref name="changedSince"/>.</returns>
    ReadOnlyChunks Query(ComponentTypesMatching filter, long changedSince);
}

public delegate bool ComponentTypesMatching(ReadOnlySpan<Type> componentTypes);
