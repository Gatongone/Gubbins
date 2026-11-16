using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// <inheritdoc cref="IEventSubscribable{TNotification}"/>
/// </summary>
public interface IEventSubscribable : IEventSubscribable<Unit>;

/// <summary>
/// Defines the contract for objects that can subscribe to events.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IEventSubscribable<TNotification>
{
    /// <summary>
    /// Add event handler to bus.
    /// </summary>
    /// <param name="handler">The event handler with notification.</param>
    void Subscribe(IEventHandler<TNotification> handler);

    /// <summary>
    /// Remove handler from bus.
    /// </summary>
    /// <param name="handler">The event handler with notification.</param>
    /// <returns>Is removed succeed.</returns>
    bool Unsubscribe(IEventHandler<TNotification> handler);

    /// <summary>
    /// Remove all handlers from bus.
    /// </summary>
    void Clear();
}
