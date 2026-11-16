namespace Gubbins.Events;

/// <summary>
/// The clock and scheduling abstraction used by time-based operators.
/// </summary>
/// <remarks>
/// Implementations can wrap <see cref="System.Threading.Timer"/>, a Unity frame loop, a Godot timer or a
/// deterministic test clock. Time-based operators take this interface instead of calling the system clock
/// directly, so tests can advance time without sleeping.
/// </remarks>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Schedules <paramref name="callback"/> to run once after <paramref name="dueTime"/>.
    /// </summary>
    /// <param name="dueTime">The delay before the callback runs; zero or negative runs immediately.</param>
    /// <param name="callback">The callback to run.</param>
    /// <returns>A handle that cancels the scheduled callback when disposed.</returns>
    IDisposable Schedule(TimeSpan dueTime, Action callback);

    /// <summary>
    /// Schedules <paramref name="callback"/> to run repeatedly every <paramref name="period"/>.
    /// </summary>
    /// <param name="period">The interval between callbacks.</param>
    /// <param name="callback">The callback to run.</param>
    /// <returns>A handle that stops the periodic callback when disposed.</returns>
    IDisposable SchedulePeriodic(TimeSpan period, Action callback);
}

/// <summary>
/// The default <see cref="ITimeProvider"/> backed by <see cref="System.Threading.Timer"/>.
/// </summary>
public sealed class SystemTimeProvider : ITimeProvider
{
    /// <summary>
    /// The shared instance.
    /// </summary>
    public static readonly SystemTimeProvider Default = new();

    private SystemTimeProvider() { }

    /// <inheritdoc/>
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public IDisposable Schedule(TimeSpan dueTime, Action callback)
        => new TimerHandle(callback, dueTime, Timeout.InfiniteTimeSpan, true);

    /// <inheritdoc/>
    public IDisposable SchedulePeriodic(TimeSpan period, Action callback)
        => new TimerHandle(callback, period, period, false);

    private sealed class TimerHandle : IDisposable
    {
        private Timer? m_Timer;

        public TimerHandle(Action callback, TimeSpan dueTime, TimeSpan period, bool oneShot)
        {
            m_Timer = new Timer(
                _ =>
                {
                    if (oneShot) Dispose();
                    callback();
                },
                null,
                dueTime,
                period);
        }

        public void Dispose()
        {
            var timer = Interlocked.Exchange(ref m_Timer, null);
            timer?.Dispose();
        }
    }
}