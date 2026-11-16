using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Events;

public class Event : Event<Unit>, IEvent;

public class Event<TNotification> : IEvent<TNotification>
{
    /// <summary>
    /// Handler collection. A null slot is a handler removed during a broadcast.
    /// </summary>
    private readonly List<IEventHandler<TNotification>?> m_Handlers = [];

    /// <summary>
    /// The number of broadcasts currently running.
    /// </summary>
    private int m_DispatchDepth;

    /// <summary>
    /// Whether the bus has terminated.
    /// </summary>
    private bool m_Completed;

    /// <summary>
    /// The terminal result, when the bus has terminated.
    /// </summary>
    private Result<TNotification, Exception> m_Result;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (m_DispatchDepth == 0)
        {
            m_Handlers.Clear();
            return;
        }

        for (var index = 0; index < m_Handlers.Count; index++)
        {
            m_Handlers[index] = null;
        }
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        var index = m_Handlers.LastIndexOf(handler);
        if (index < 0) return false;

        m_Handlers[index] = null;
        if (m_DispatchDepth == 0) Compact();
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        if (m_Completed)
        {
            handler.OnComplete(m_Result);
            return;
        }

        m_Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public void Broadcast(TNotification notification)
    {
        if (m_Completed) return;

        m_DispatchDepth++;
        try
        {
            var handlers = m_Handlers;
            var handlerCount = handlers.Count;
            for (var index = 0; index < handlerCount; index++)
            {
                var handler = handlers[index];
                if (handler is null) continue;

                handler.Handle(notification);
            }
        }
        finally
        {
            if (--m_DispatchDepth == 0) Compact();
        }
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Completed) return;
        m_Completed = true;
        m_Result = result;

        m_DispatchDepth++;
        try
        {
            var handlers = m_Handlers;
            var handlerCount = handlers.Count;
            for (var index = 0; index < handlerCount; index++)
            {
                var handler = handlers[index];
                if (handler is null) continue;

                handlers[index] = null;
                handler.OnComplete(result);
            }
        }
        finally
        {
            if (--m_DispatchDepth == 0) Compact();
        }
    }

    /// <summary>
    /// Removes the null slots left by handlers that were removed during a broadcast.
    /// </summary>
    private void Compact() => m_Handlers.RemoveAll(static handler => handler is null);
}