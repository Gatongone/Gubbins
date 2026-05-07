using Gubbins.Enhance;

namespace Gubbins.Event;

/// <inheritdoc cref="IWeakEventHandler{TNotification}"/>
public interface IWeakEventHandler : IWeakEventHandler<Unit>;

/// <summary>
/// The event handler from a weak reference provider for handling notifications.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IWeakEventHandler<in TNotification>
{
    /// <summary>
    /// Is the handler reference alive.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Try handle notification if the event provider still alive.
    /// </summary>
    /// <param name="notification">Notification of messages with data.</param>
    /// <returns>Whether the event provider is alive.</returns>
    bool TryHandle(TNotification notification);
}