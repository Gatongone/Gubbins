namespace Gubbins.Entities;

/// <summary>
/// The repository's monotonic write clock.
/// </summary>
/// <remarks>
/// <para>
/// Every write takes the next value before it stamps the chunks it touched, so writes are totally ordered and a reader
/// that captured a value earlier can always tell whether something was written after it looked. Taking the value at the
/// moment of the write, rather than once per frame, is what lets the automation stay correct: a write that happens
/// after a reader has captured the current value still gets a strictly newer one, so the reader cannot miss it.
/// </para>
/// <para>
/// <see cref="Current"/> is read concurrently by queries; <see cref="Next"/> is the only mutator and is atomic.
/// </para>
/// </remarks>
internal sealed class ChangeClock
{
    /// <summary>
    /// The last value handed out. Zero is reserved for "nothing has been written yet", so every write is greater.
    /// </summary>
    private long m_Version;

    /// <summary>
    /// Gets the value of the most recent write, or <c>0</c> when nothing has been written yet.
    /// </summary>
    public long Current => Volatile.Read(ref m_Version);

    /// <summary>
    /// Takes the next value of the clock.
    /// </summary>
    /// <returns>The value the caller stamps the chunks it writes.</returns>
    public long Next() => Interlocked.Increment(ref m_Version);
}
