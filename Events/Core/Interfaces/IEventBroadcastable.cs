using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// <inheritdoc cref="IEventBroadcastable{TNotification}"/>
/// </summary>
public interface IEventBroadcastable : IEventBroadcastable<Unit>;

/// <summary>
/// Defines the behavior of an object that can broadcast events to multiple subscribers.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IEventBroadcastable<TNotification>
{
    /// <summary>
    /// Execute all handler's handle method in bus with notification.
    /// </summary>
    /// <param name="notification">Notification of messages with data.</param>
    void Broadcast(TNotification notification);

    /// <summary>
    /// Terminates the bus and notifies every handler once.
    /// </summary>
    /// <param name="result">The terminal result: a successful value or the terminal error.</param>
    void OnComplete(Result<TNotification, Exception> result);
}
