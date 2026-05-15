using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Events;

public class Event : Event<Unit>, IEvent;

public class Event<TNotification> : IEvent<TNotification>
{
    /// <summary>
    /// Handler collections.
    /// </summary>
    private readonly List<IEventHandler<TNotification>> m_Handlers = [];

    /// <summary>
    /// Weak handler collections.
    /// </summary>
    private readonly List<IWeakEventHandler<TNotification>> m_WeakHandlers = [];

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_Handlers.Clear();
        m_WeakHandlers.Clear();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (null! == handler) return false;

        //  Dispatch all normal handlers.
        for (var index = m_Handlers.Count - 1; index >= 0; index--)
        {
            if (!m_Handlers[index].Equals(handler)) continue;
            m_Handlers.RemoveAt(index);
            return true;
        }

        //  Dispatch all weak handlers.
        for (var index = m_WeakHandlers.Count - 1; index >= 0; index--)
        {
            var weakTarget = m_WeakHandlers[index];
            // Check the target is alive.
            if (weakTarget.IsAlive)
            {
                if (weakTarget.Equals(handler)) continue;
                m_WeakHandlers.RemoveAt(index);
                return true;
            }

            // The reference is dead.
            m_WeakHandlers.RemoveAt(index);
        }

        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subscribe(IEventHandler<TNotification> handler) => m_Handlers.Add(handler);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubscribeWeakly(IWeakEventHandler<TNotification> handler) => m_WeakHandlers.Add(handler);

    /// <inheritdoc/>
    public void Broadcast(TNotification notification)
    {
        // Dispatch all normal handlers.
        foreach (var handler in m_Handlers)
        {
            handler.Handle(notification);
        }

        // Dispatch all weak handlers.
        foreach (var handler in m_WeakHandlers)
        {
            handler.TryHandle(notification);
        }

        // Clear all dead weak handlers.
        m_WeakHandlers.RemoveAll(static handler => !handler.IsAlive);
    }
}