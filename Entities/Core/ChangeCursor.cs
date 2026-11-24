namespace Gubbins.Entities;

/// <summary>
/// A caller's position in the repository's write timeline.
/// </summary>
/// <remarks>
/// <para>
/// A change-filtered query compares the chunks' write versions against a "since when" value, and that value belongs to
/// the caller rather than to the repository: two systems may run at different rates, skip a frame, or run twice in one.
/// A cursor holds that value, so a system keeps one field instead of threading a version through every call site.
/// </para>
/// <para>
/// The <c>Changed</c> extensions capture the cursor's version, run the change-filtered query against it, and advance the
/// cursor to the repository's current version. Writes take their version at the moment they happen, so a write that
/// lands after that point gets a newer one and the next call sees it: nothing has to be advanced by hand and no frame
/// boundary has to be announced.
/// </para>
/// <para>
/// A cursor observes each write at least once. A write the caller performs itself through the command-side
/// <c>Changed</c> is observed again by that same cursor, because its view takes its version when it is iterated, after
/// the cursor captured the current one; give the reader and the writer separate cursors to keep them independent.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// private readonly ChangeCursor m_Changed = new();
///
/// private void Tick(IEntityQuery query)
/// {
///     var chunks = query.Changed(m_Changed, new ComponentFilter().Include&lt;Position&gt;());
///     chunks.ForEach((in Entity entity, in Position position) => ...);
/// }
/// </code>
/// </example>
public sealed class ChangeCursor
{
    /// <summary>
    /// The version the caller has observed up to. Zero means "nothing observed yet".
    /// </summary>
    private long m_Version;

    /// <summary>
    /// Initializes a cursor that starts at the beginning of the timeline, so its first call sees everything that exists.
    /// </summary>
    public ChangeCursor() { }

    /// <summary>
    /// Initializes a cursor at an explicit point in the write timeline.
    /// </summary>
    /// <param name="version">The version the cursor starts after.</param>
    public ChangeCursor(long version) => m_Version = version;

    /// <summary>
    /// Gets the version the cursor has observed up to.
    /// </summary>
    public long Version => Volatile.Read(ref m_Version);

    /// <summary>
    /// Moves the cursor back to the beginning of the timeline, so the next <c>Changed</c> call yields every matching
    /// chunk again.
    /// </summary>
    public void Reset() => Volatile.Write(ref m_Version, 0);

    /// <summary>
    /// Records that everything up to <paramref name="version"/> has been observed.
    /// </summary>
    /// <param name="version">The version the cursor continues after.</param>
    public void Advance(long version) => Volatile.Write(ref m_Version, version);
}

/// <summary>
/// Change-filtered queries that track their own position with a <see cref="ChangeCursor"/>.
/// </summary>
/// <remarks>
/// The cursor overloads are the automated form of the <c>changedSince</c> parameter on
/// <see cref="IEntityQuery.Query(ComponentFilter, long)"/> and <see cref="IEntityCommand.Command(ComponentFilter, long)"/>:
/// same filtering, without the caller having to carry the version around.
/// </remarks>
public static class ChangeCursorExtensions
{
    extension(IEntityQuery query)
    {
        /// <summary>
        /// Returns a read-only view of the chunks that match <paramref name="filter"/> and were written since this
        /// cursor last ran, then advances the cursor.
        /// </summary>
        /// <param name="cursor">The caller's position in the write timeline.</param>
        /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
        /// <returns>The matching chunks written after the cursor's version.</returns>
        public ReadOnlyChunks Changed(ChangeCursor cursor, ComponentFilter filter)
        {
            var since = cursor.Version;
            var current = query.Version;
            var chunks = query.Query(filter, since);
            cursor.Advance(current);
            return chunks;
        }

        /// <inheritdoc cref="Changed(ChangeCursor, ComponentFilter)"/>
        public ReadOnlyChunks Changed(ChangeCursor cursor, ComponentTypesMatching filter)
        {
            var since = cursor.Version;
            var current = query.Version;
            var chunks = query.Query(filter, since);
            cursor.Advance(current);
            return chunks;
        }
    }

    extension(IEntityCommand command)
    {
        /// <summary>
        /// Returns a writable view of the chunks that match <paramref name="filter"/> and were written since this
        /// cursor last ran, then advances the cursor.
        /// </summary>
        /// <param name="cursor">The caller's position in the write timeline.</param>
        /// <param name="filter">A filter that specifies the component criteria for matching entities.</param>
        /// <returns>The matching chunks written after the cursor's version, as the command side's writable view.</returns>
        /// <remarks>
        /// Iterating the returned view declares write access, so its chunks are stamped as changed for the readers that
        /// run after it.
        /// </remarks>
        public Chunks Changed(ChangeCursor cursor, ComponentFilter filter)
        {
            var since = cursor.Version;
            var current = command.Version;
            var chunks = command.Command(filter, since);
            cursor.Advance(current);
            return chunks;
        }

        /// <inheritdoc cref="Changed(ChangeCursor, ComponentFilter)"/>
        public Chunks Changed(ChangeCursor cursor, ComponentTypesMatching filter)
        {
            var since = cursor.Version;
            var current = command.Version;
            var chunks = command.Command(filter, since);
            cursor.Advance(current);
            return chunks;
        }
    }
}
