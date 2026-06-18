using Gubbins.Enhance;

namespace Gubbins.Events;

/// <inheritdoc cref="ILinkableEventSubscriable{Unit, TResult}"/>
public interface ILinkableEventSubscriable<TResult> : ILinkableEventHandler<Unit, TResult>;

/// <summary>
/// An event that can be linked to other events, and the handlers of the linked events will be executed when the event is raised.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public interface ILinkableEventSubscriable<out TNotification, TResult>
{
    /// <summary>
    /// Append a handler to linked event list.
    /// </summary>
    /// <param name="handler">The handler to append.</param>
    void Subscribe(ILinkableEventHandler<TNotification, TResult> handler);

    /// <summary>
    /// Append a handler to linked event list.
    /// </summary>
    /// <param name="handler">The handler to append.</param>
    void Unsubscribe(ILinkableEventHandler<TNotification, TResult> handler);

    /// <summary>
    /// Remove all handlers from bus.
    /// </summary>
    void Clear();
}