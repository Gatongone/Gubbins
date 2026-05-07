namespace Gubbins.Event;

/// <summary>
/// <inheritdoc cref="IEventHandler{TNotification}"/>
/// </summary>
public interface IEventHandler : IEventHandler<Enhance.Unit>;

/// <summary>
/// The event handler from a weak reference provider for handling notifications.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IEventHandler<in TNotification>
{
    /// <summary>
    /// Handle notification.
    /// </summary>
    /// <param name="notification">Notification of messages with data.</param>
    void Handle(TNotification notification);
}
