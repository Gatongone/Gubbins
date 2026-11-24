namespace Gubbins.Entities;

/// <summary>
/// A read-only view over the chunks that match a query.
/// </summary>
/// <remarks>
/// <para>
/// Obtained from <see cref="Chunks.AsReadOnly"/>. It exposes the same query surface as <see cref="Chunks"/> but hands
/// out read-only data — <see cref="ReadOnlyChunkLayout{T}"/> instead of <see cref="Components{T}"/> — and never marks a
/// chunk as changed, so a reader can neither write component data nor make it look hot to a writer's
/// <see cref="Chunks.ChangedSince"/> filter.
/// </para>
/// <para>
/// The iteration methods share their names with the write-side ones (<c>ForEach</c>, <c>ForEachAsync</c>) because the
/// receiver type selects the access: an action taking <c>ref</c> components only binds to <see cref="Chunks"/>, and one
/// taking <c>in</c> components only binds to this type. Overloading the same receiver on the delegate type instead
/// would be ambiguous, because a lambda declared with <c>in</c> parameters also converts to the <c>ref</c> delegate.
/// </para>
/// </remarks>
public struct ReadOnlyChunks
{
    /// <summary>
    /// The write-side query this view reads through. It was created without write access, so every read through it
    /// leaves the change versions alone.
    /// </summary>
    private readonly Chunks m_Chunks;

    /// <param name="chunks">A query created without write access.</param>
    internal ReadOnlyChunks(Chunks chunks) => m_Chunks = chunks;

    /// <summary>
    /// Gets the number of chunks in this collection.
    /// </summary>
    /// <remarks>
    /// Chunks skipped by the view's filters are not counted.
    /// </remarks>
    public readonly int Count => m_Chunks.Count;

    /// <summary>
    /// Returns a view that only iterates chunks written after <paramref name="version"/>.
    /// </summary>
    /// <param name="version">
    /// The version a chunk must be newer than. Prefer <see cref="ChangeCursor"/>, which captures and advances it, or pass
    /// <see cref="EntityRepository.Version"/> for an explicit point in the write timeline.
    /// </param>
    /// <remarks>
    /// A read-only pass only iterates the chunks something wrote since the captured version, so its work is
    /// proportional to what changed rather than to what exists.
    /// </remarks>
    public readonly ReadOnlyChunks ChangedSince(long version) => new(m_Chunks.ChangedSince(version));

    /// <summary>
    /// Returns a view that also iterates disabled entities.
    /// </summary>
    /// <remarks>
    /// Queries skip archetypes carrying <see cref="Disabled"/> unless their filter includes it. Use this to read them
    /// anyway, for example to list them before enabling or deleting them.
    /// </remarks>
    public readonly ReadOnlyChunks IncludeDisabled() => new(m_Chunks.IncludeDisabled());

    /// <summary>
    /// Gets the components of type <typeparamref name="T"/> across all chunks in this view, as read-only spans.
    /// </summary>
    /// <typeparam name="T">Component or entity type.</typeparam>
    /// <returns>A <see cref="ReadOnlyChunkLayout{T}"/> over the matching snippets.</returns>
    /// <remarks>
    /// This is a read access: no chunk is stamped as changed, and the spans it hands out cannot be written.
    /// </remarks>
    public readonly ReadOnlyChunkLayout<T> GetLayout<T>() where T : unmanaged => m_Chunks.GetReadOnlyLayout<T>();

    /// <summary>
    /// Nothing to release: the view is a lazy view and the snippet arrays are owned by
    /// <see cref="ReadOnlyChunkLayout{T}"/>.
    /// </summary>
}
