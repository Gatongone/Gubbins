namespace Gubbins.Event;

/// <summary>
/// <inheritdoc cref="IEventBroadcastable{TNotification}"/>
/// </summary>
public interface IEventBroadcastable : IEventBroadcastable<Enhance.Unit>;

/// <summary>
/// Defines the behavior of an object that can broadcast events to multiple subscribers.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IEventBroadcastable<in TNotification>
{
    /// <summary>
    /// Execute all handler's handle method in bus with notification.
    /// </summary>
    /// <param name="notification">Notification of messages with data.</param>
    void Broadcast(TNotification notification);
}