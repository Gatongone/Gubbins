using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// A linkable event that can be subscribed to and broadcasted with a result.
/// </summary>
/// <typeparam name="TResult">The type of the result.</typeparam>
public class LinkableEvent<TResult>(TResult defaultResult) : LinkableEvent<Unit, TResult>(defaultResult);

/// <summary>
/// A linkable event that can be subscribed to and broadcasted with a result.
/// </summary>
/// <param name="defaultResult">The default result to return when there are no handlers. and it would be first handler's previousResult if there are handlers.</param>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
public class LinkableEvent<TNotification, TResult>(TResult defaultResult) : ILinkableEventSubscriable<TNotification, TResult>, ILinkableEventBroadcastable<TNotification, TResult>, IEventBroadcastable<TNotification>
{
    public LinkableEvent() : this(default!) { }

    /// <summary>
    /// Handler collections.
    /// </summary>
    private readonly List<ILinkableEventHandler<TNotification, TResult>> m_Handlers = [];

    /// <inheritdoc/>
    public void Subscribe(ILinkableEventHandler<TNotification, TResult> handler) => m_Handlers.Add(handler);

    /// <inheritdoc/>
    public void Unsubscribe(ILinkableEventHandler<TNotification, TResult> handler) => m_Handlers.Remove(handler);

    /// <inheritdoc/>
    public void Clear() => m_Handlers.Clear();

    /// <summary>
    /// Broadcast the event to all handlers but never return result.
    /// </summary>
    void IEventBroadcastable<TNotification>.Broadcast(TNotification notification) => Broadcast(notification);

    /// <inheritdoc/>
    public TResult Broadcast(TNotification notification)
    {
        if (m_Handlers.Count == 0)
        {
            return defaultResult;
        }

        var previous = defaultResult;
        for (var i = 1; i < m_Handlers.Count; i++)
        {
            previous = m_Handlers[i].Handle(notification, defaultResult);
        }

        return previous;
    }
}