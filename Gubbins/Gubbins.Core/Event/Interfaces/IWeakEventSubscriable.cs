using Gubbins.Enhance;

namespace Gubbins.Event;

/// <summary>
/// <inheritdoc cref="IWeakEventSubscriable{TNotification}"/>
/// </summary>
public interface IWeakEventSubscriable : IWeakEventSubscriable<Unit>;

/// <summary>
/// Allows the event bus to have the behavior of holding a weak reference to the EventHandler when it is registered.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IWeakEventSubscriable<out TNotification>
{
    /// <summary>
    /// Subscribe handler by weak reference.
    /// </summary>
    /// <param name="handler">The event handler with notification.</param>
    void SubscribeWeakly(IWeakEventHandler<TNotification> handler);
}