namespace Gubbins.Events;

/// <summary>
/// The handler wrapper that create a weak reference for the input handler.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
internal sealed class WeakEventHandler<TNotification> : IWeakEventHandler<TNotification>
{
    /// <summary>
    /// Weak reference for the input handler.
    /// </summary>
    private readonly WeakReference<IEventHandler<TNotification>> m_WeakHandler;

    /// <summary>
    /// Is the handler still alive.
    /// </summary>
    public bool IsAlive => m_WeakHandler.TryGetTarget(out _);

    /// <summary>
    /// <inheritdoc cref="WeakEventHandler{TNotification}"/>
    /// </summary>
    /// <param name="handler">The handler that would be created a weak reference.</param>
    internal WeakEventHandler(IEventHandler<TNotification> handler) => m_WeakHandler = new WeakReference<IEventHandler<TNotification>>(handler);

    /// <inheritdoc/>
    public bool TryHandle(TNotification notification)
    {
        // Check alive.
        if (!m_WeakHandler.TryGetTarget(out var handler)) return false;
        // Alive, then handle it.
        handler.Handle(notification);
        return true;
    }
}