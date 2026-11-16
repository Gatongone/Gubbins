using Gubbins.Enhance;

namespace Gubbins.Events;



/// <summary>
/// A source that adapts another source with a handler-level operator.
/// </summary>
/// <typeparam name="TSource">The notification type of the upstream source.</typeparam>
/// <typeparam name="TResult">The notification type of this source.</typeparam>
public class ReactiveEvent<TSource, TResult> : IEventSubscribable<TResult>
{
    /// <summary>
    /// The upstream source.
    /// </summary>
    private readonly IEventSubscribable<TSource> m_Source;

    /// <summary>
    /// Creates the upstream handler that wraps one downstream handler.
    /// </summary>
    private readonly Func<IEventHandler<TResult>, IEventHandler<TSource>> m_HandlerFactory;

    /// <summary>
    /// The active subscriptions.
    /// </summary>
    private readonly List<Entry> m_Entries = [];

    /// <summary>
    /// Whether every subscription completes immediately without touching the source.
    /// </summary>
    private readonly bool m_CompleteOnSubscribe;

    /// <summary>
    /// Creates a reactive source.
    /// </summary>
    /// <param name="source">The upstream source.</param>
    /// <param name="handlerFactory">The factory that builds the upstream handler for a downstream handler.</param>
    /// <param name="completeOnSubscribe">Whether subscriptions complete immediately.</param>
    internal ReactiveEvent(
        IEventSubscribable<TSource> source,
        Func<IEventHandler<TResult>, IEventHandler<TSource>> handlerFactory,
        bool completeOnSubscribe = false)
    {
        m_Source = source;
        m_HandlerFactory = handlerFactory;
        m_CompleteOnSubscribe = completeOnSubscribe;
    }

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        if (m_CompleteOnSubscribe)
        {
            handler.OnComplete(default);
            return;
        }

        var upstream = m_HandlerFactory(handler);
        m_Source.Subscribe(upstream);
        m_Entries.Add(new Entry(handler, upstream));
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TResult>? handler)
    {
        if (handler is null) return false;

        for (var index = m_Entries.Count - 1; index >= 0; index--)
        {
            if (!m_Entries[index].Handler.Equals(handler)) continue;

            var upstream = m_Entries[index].Upstream;
            m_Entries.RemoveAt(index);
            if (upstream is IEventFinalizer finalizer) finalizer.Finalize();
            m_Source.Unsubscribe(upstream);
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Entries.Count > 0)
        {
            var index = m_Entries.Count - 1;
            var upstream = m_Entries[index].Upstream;
            m_Entries.RemoveAt(index);
            if (upstream is IEventFinalizer finalizer) finalizer.Finalize();
            m_Source.Unsubscribe(upstream);
        }
    }

    /// <summary>
    /// One downstream handler and the upstream handler that feeds it.
    /// </summary>
    private readonly record struct Entry(IEventHandler<TResult> Handler, IEventHandler<TSource> Upstream);
}

/// <summary>
/// An event that replays its current value to every new subscriber.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public class LatestEvent<TNotification> : IEventSubscribable<TNotification>
{
    private readonly List<IEventHandler<TNotification>?> m_Handlers = [];
    private bool m_HasValue;
    private TNotification? m_Value;
    private bool m_Completed;
    private Result<TNotification, Exception> m_Result;

    /// <summary>
    /// Creates an empty latest-value event.
    /// </summary>
    public LatestEvent() { }

    /// <summary>
    /// Creates a latest-value event with an initial value.
    /// </summary>
    /// <param name="value">The current value.</param>
    public LatestEvent(TNotification value)
    {
        m_HasValue = true;
        m_Value = value;
    }

    /// <summary>
    /// Gets the current value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the event has no value.</exception>
    public TNotification Value => m_HasValue
        ? m_Value!
        : throw new InvalidOperationException("The event has no value.");

    /// <summary>
    /// Whether the event has a current value.
    /// </summary>
    protected bool HasValueCore => m_HasValue;

    /// <summary>
    /// The current value without the empty check.
    /// </summary>
    protected TNotification CurrentValue => m_Value!;

    /// <summary>
    /// Pushes a new value to the event and its subscribers.
    /// </summary>
    /// <param name="value">The new value.</param>
    public void OnNext(TNotification value)
    {
        if (m_Completed) return;

        m_HasValue = true;
        m_Value = value;

        for (var index = 0; index < m_Handlers.Count; index++)
        {
            m_Handlers[index]?.Handle(value);
        }
    }

    /// <summary>
    /// Completes the event and its subscribers.
    /// </summary>
    /// <param name="result">The terminal result.</param>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Completed) return;
        m_Completed = true;
        m_Result = result;

        for (var index = 0; index < m_Handlers.Count; index++)
        {
            var handler = m_Handlers[index];
            if (handler is null) continue;

            m_Handlers[index] = null;
            handler.OnComplete(result);
        }

        m_Handlers.Clear();
    }

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        if (m_HasValue) handler.Handle(m_Value!);

        if (m_Completed)
        {
            handler.OnComplete(m_Result);
            return;
        }

        m_Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        var index = m_Handlers.LastIndexOf(handler);
        if (index < 0) return false;

        m_Handlers[index] = null;
        m_Handlers.RemoveAll(static item => item is null);
        return true;
    }

    /// <inheritdoc/>
    public void Clear() => m_Handlers.Clear();
}

/// <summary>
/// A latest-value event that drops a value equal to the current one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public sealed class StateEvent<TNotification> : LatestEvent<TNotification>
{
    private readonly IEqualityComparer<TNotification> m_Comparer;

    /// <summary>
    /// Creates a reactive property.
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    public StateEvent(TNotification initialValue) : base(initialValue)
        => m_Comparer = EqualityComparer<TNotification>.Default;

    /// <summary>
    /// Creates a reactive property with a custom comparer.
    /// </summary>
    /// <param name="initialValue">The initial value.</param>
    /// <param name="comparer">The comparer used to detect equal values.</param>
    public StateEvent(TNotification initialValue, IEqualityComparer<TNotification> comparer) : base(initialValue)
    {
        if (comparer is null) throw new ArgumentNullException(nameof(comparer));

        m_Comparer = comparer;
    }

    /// <summary>
    /// Gets or sets the current value; setting an equal value does not notify.
    /// </summary>
    public new TNotification Value
    {
        get => base.Value;
        set
        {
            if (HasValueCore && m_Comparer.Equals(CurrentValue, value)) return;

            OnNext(value);
        }
    }
}

/// <summary>
/// An event that replays every buffered value to a new subscriber.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public class ReplayEvent<TNotification> : IEventSubscribable<TNotification>
{
    private readonly List<TNotification> m_Buffer = [];
    private readonly List<IEventHandler<TNotification>?> m_Handlers = [];
    private bool m_Completed;
    private Result<TNotification, Exception> m_Result;

    /// <summary>
    /// Pushes a value to the buffer and to the current subscribers.
    /// </summary>
    /// <param name="value">The value to push.</param>
    public void OnNext(TNotification value)
    {
        if (m_Completed) return;

        m_Buffer.Add(value);
        for (var index = 0; index < m_Handlers.Count; index++)
        {
            m_Handlers[index]?.Handle(value);
        }
    }

    /// <summary>
    /// Completes the event and its subscribers.
    /// </summary>
    /// <param name="result">The terminal result.</param>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Completed) return;
        m_Completed = true;
        m_Result = result;

        for (var index = 0; index < m_Handlers.Count; index++)
        {
            var handler = m_Handlers[index];
            if (handler is null) continue;

            m_Handlers[index] = null;
            handler.OnComplete(result);
        }

        m_Handlers.Clear();
    }

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        for (var index = 0; index < m_Buffer.Count; index++)
        {
            handler.Handle(m_Buffer[index]);
        }

        if (m_Completed)
        {
            handler.OnComplete(m_Result);
            return;
        }

        m_Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        var index = m_Handlers.LastIndexOf(handler);
        if (index < 0) return false;

        m_Handlers[index] = null;
        m_Handlers.RemoveAll(static item => item is null);
        return true;
    }

    /// <inheritdoc/>
    public void Clear() => m_Handlers.Clear();
}

/// <summary>
/// A subscribable event that can be connected to its source explicitly.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public interface IConnectableEvent<TNotification> : IEventSubscribable<TNotification>
{
    /// <summary>
    /// Subscribes to the source and starts forwarding.
    /// </summary>
    /// <returns>A handle that disconnects the source.</returns>
    IDisposable Connect();
}

/// <summary>
/// Multicasts a single source subscription to multiple subscribers.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public sealed class ConnectableEvent<TNotification> : IConnectableEvent<TNotification>
{
    private readonly IEventSubscribable<TNotification> m_Source;
    private readonly List<IEventHandler<TNotification>?> m_Handlers = [];
    private readonly ForwardHandler m_Forward;
    private IDisposable? m_Connection;
    private bool m_Completed;
    private Result<TNotification, Exception> m_Result;

    /// <summary>
    /// Creates a connectable event.
    /// </summary>
    /// <param name="source">The source to multicast.</param>
    public ConnectableEvent(IEventSubscribable<TNotification> source)
    {
        m_Source = source;
        m_Forward = new ForwardHandler(this);
    }

    /// <summary>
    /// Whether the source has completed.
    /// </summary>
    public bool IsCompleted => m_Completed;

    /// <inheritdoc/>
    public IDisposable Connect()
    {
        if (m_Completed || m_Connection is not null) return EmptyDisposable.Instance;

        m_Source.Subscribe(m_Forward);
        m_Connection = new Connection(this);
        return m_Connection;
    }

    /// <inheritdoc/>
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
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        var index = m_Handlers.LastIndexOf(handler);
        if (index < 0) return false;

        m_Handlers[index] = null;
        m_Handlers.RemoveAll(static item => item is null);
        return true;
    }

    /// <inheritdoc/>
    public void Clear() => m_Handlers.Clear();

    private void Disconnect()
    {
        if (m_Connection is null) return;

        m_Source.Unsubscribe(m_Forward);
        m_Connection = null;
    }

    private void OnNext(TNotification notification)
    {
        for (var index = 0; index < m_Handlers.Count; index++)
        {
            m_Handlers[index]?.Handle(notification);
        }
    }

    private void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Completed) return;
        m_Completed = true;
        m_Result = result;
        m_Connection = null;

        for (var index = 0; index < m_Handlers.Count; index++)
        {
            var handler = m_Handlers[index];
            if (handler is null) continue;

            m_Handlers[index] = null;
            handler.OnComplete(result);
        }

        m_Handlers.Clear();
    }

    private sealed class ForwardHandler(ConnectableEvent<TNotification> owner) : IEventHandler<TNotification>
    {
        public void Handle(TNotification notification) => owner.OnNext(notification);

        public void OnComplete(Result<TNotification, Exception> result) => owner.OnComplete(result);
    }

    private sealed class Connection(ConnectableEvent<TNotification> owner) : IDisposable
    {
        public void Dispose() => owner.Disconnect();
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static readonly EmptyDisposable Instance = new();

        public void Dispose() { }
    }
}

/// <summary>
/// Shares one source subscription among its subscribers and disconnects when the last one leaves.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
public sealed class SharedEvent<TNotification> : IEventSubscribable<TNotification>
{
    private readonly ConnectableEvent<TNotification> m_Connectable;
    private IDisposable? m_Connection;
    private int m_Count;

    /// <summary>
    /// Creates a shared event.
    /// </summary>
    /// <param name="source">The source to share.</param>
    public SharedEvent(IEventSubscribable<TNotification> source)
        => m_Connectable = new ConnectableEvent<TNotification>(source);

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        if (m_Connectable.IsCompleted)
        {
            m_Connectable.Subscribe(handler);
            return;
        }

        m_Connectable.Subscribe(handler);
        m_Count++;
        if (m_Count == 1) m_Connection = m_Connectable.Connect();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (!m_Connectable.Unsubscribe(handler)) return false;

        m_Count--;
        if (m_Count != 0) return true;

        m_Connection?.Dispose();
        m_Connection = null;
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        m_Connectable.Clear();
        m_Count = 0;
        m_Connection?.Dispose();
        m_Connection = null;
    }
}

/// <summary>
/// Concatenates two sources: the second one is subscribed after the first one completes successfully.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ConcatEvent<TNotification>(
    IEventSubscribable<TNotification> first,
    IEventSubscribable<TNotification> second) : IEventSubscribable<TNotification>
{
    private readonly List<ConcatBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new ConcatBridge<TNotification>(first, second, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class ConcatBridge<T>(
        IEventSubscribable<T> first,
        IEventSubscribable<T> second,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private bool m_Disposed;
        private bool m_OnSecond;

        public IEventHandler<T> Handler => handler;

        public void Start() => first.Subscribe(this);

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                m_Disposed = true;
                handler.OnComplete(error);
                return;
            }

            m_OnSecond = true;
            second.Subscribe(handler);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;

            if (m_OnSecond) second.Unsubscribe(handler);
            else first.Unsubscribe(this);
        }
    }
}

/// <summary>
/// Merges two sources into one, completing when both complete.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class MergeEvent<TNotification>(
    IEventSubscribable<TNotification> first,
    IEventSubscribable<TNotification> second) : IEventSubscribable<TNotification>
{
    private readonly List<MergeBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new MergeBridge<TNotification>(first, second, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class MergeBridge<T>(
        IEventSubscribable<T> first,
        IEventSubscribable<T> second,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private int m_Remaining = 2;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start()
        {
            first.Subscribe(this);
            second.Subscribe(this);
        }

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                m_Disposed = true;
                handler.OnComplete(error);
                return;
            }

            if (--m_Remaining != 0) return;

            m_Disposed = true;
            handler.OnComplete(default);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            first.Unsubscribe(this);
            second.Unsubscribe(this);
        }
    }
}

/// <summary>
/// Switches to the latest inner source emitted by an outer source of sources.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SwitchEvent<TNotification>(
    IEventSubscribable<IEventSubscribable<TNotification>> sources) : IEventSubscribable<TNotification>
{
    private readonly List<SwitchBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new SwitchBridge<TNotification>(sources, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class SwitchBridge<T> : IEventHandler<IEventSubscribable<T>>
    {
        private readonly IEventSubscribable<IEventSubscribable<T>> m_Sources;
        private readonly IEventHandler<T> m_Handler;
        private readonly InnerSink m_Inner;
        private IEventSubscribable<T>? m_Current;
        private bool m_Disposed;
        private bool m_OuterCompleted;

        public SwitchBridge(IEventSubscribable<IEventSubscribable<T>> sources, IEventHandler<T> handler)
        {
            m_Sources = sources;
            m_Handler = handler;
            m_Inner = new InnerSink(this);
        }

        public IEventHandler<T> Handler => m_Handler;

        public void Start() => m_Sources.Subscribe(this);

        public void Handle(IEventSubscribable<T> notification)
        {
            if (m_Disposed) return;

            if (m_Current is not null) m_Current.Unsubscribe(m_Inner);

            m_Current = notification;
            notification.Subscribe(m_Inner);
        }

        public void OnComplete(Result<IEventSubscribable<T>, Exception> result)
        {
            if (m_Disposed) return;

            m_OuterCompleted = true;
            if (!result.TryGetValue(out _, out var error))
            {
                m_Disposed = true;
                m_Handler.OnComplete(error);
                return;
            }

            if (m_Current is not null) return;

            m_Disposed = true;
            m_Handler.OnComplete(default);
        }

        internal void InnerCompleted(Result<T, Exception> result)
        {
            if (m_Disposed) return;

            if (!m_OuterCompleted && result.Valid)
            {
                m_Current = null;
                return;
            }

            m_Disposed = true;
            m_Current = null;
            m_Sources.Unsubscribe(this);
            m_Handler.OnComplete(result);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Sources.Unsubscribe(this);
            if (m_Current is not null) m_Current.Unsubscribe(m_Inner);
            m_Current = null;
        }

        private sealed class InnerSink(SwitchBridge<T> owner) : IEventHandler<T>
        {
            public void Handle(T notification) => owner.m_Handler.Handle(notification);

            public void OnComplete(Result<T, Exception> result) => owner.InnerCompleted(result);
        }
    }
}

/// <summary>
/// Switches to a fallback source when the source terminates with an error.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class CatchEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    Func<Exception, IEventSubscribable<TNotification>?> fallbackFactory) : IEventSubscribable<TNotification>
{
    private readonly List<CatchBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new CatchBridge<TNotification>(source, fallbackFactory, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class CatchBridge<T>(
        IEventSubscribable<T> source,
        Func<Exception, IEventSubscribable<T>?> fallbackFactory,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private IEventSubscribable<T>? m_Fallback;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start() => source.Subscribe(this);

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                var fallback = fallbackFactory(error);
                if (fallback is null)
                {
                    m_Disposed = true;
                    handler.OnComplete(default);
                    return;
                }

                m_Fallback = fallback;
                fallback.Subscribe(handler);
                return;
            }

            m_Disposed = true;
            handler.OnComplete(default);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;

            if (m_Fallback is not null) m_Fallback.Unsubscribe(handler);
            else source.Unsubscribe(this);
        }
    }
}

/// <summary>
/// Resubscribes the source on completion or on error.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ResubscribeEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    int count,
    bool repeat) : IEventSubscribable<TNotification>
{
    private readonly List<ResubscribeBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new ResubscribeBridge<TNotification>(source, handler, count, repeat);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class ResubscribeBridge<T>(
        IEventSubscribable<T> source,
        IEventHandler<T> handler,
        int remaining,
        bool repeat) : IEventHandler<T>
    {
        private int m_Remaining = remaining;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start() => source.Subscribe(this);

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                if (repeat)
                {
                    m_Disposed = true;
                    handler.OnComplete(error);
                    return;
                }

                if (m_Remaining-- > 0)
                {
                    source.Subscribe(this);
                    return;
                }

                m_Disposed = true;
                handler.OnComplete(error);
                return;
            }

            if (!repeat)
            {
                m_Disposed = true;
                handler.OnComplete(default);
                return;
            }

            if (m_Remaining-- > 0)
            {
                source.Subscribe(this);
                return;
            }

            m_Disposed = true;
            handler.OnComplete(default);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);
        }
    }
}

/// <summary>
/// Completes when either the source or the trigger emits.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TTrigger">The trigger notification type.</typeparam>
internal sealed class TakeUntilEvent<TNotification, TTrigger>(
    IEventSubscribable<TNotification> source,
    IEventSubscribable<TTrigger> trigger) : IEventSubscribable<TNotification>
{
    private readonly List<TakeUntilBridge<TNotification, TTrigger>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new TakeUntilBridge<TNotification, TTrigger>(source, trigger, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class TakeUntilBridge<T, TTrigger>(
        IEventSubscribable<T> source,
        IEventSubscribable<TTrigger> trigger,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private readonly TriggerSink m_TriggerSink = new(handler);
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start()
        {
            m_TriggerSink.Owner = this;
            trigger.Subscribe(m_TriggerSink);
            source.Subscribe(this);
        }

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result) => Finish(result);

        internal void OnTrigger() => Finish(default);

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);
            trigger.Unsubscribe(m_TriggerSink);
        }

        private void Finish(Result<T, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);
            trigger.Unsubscribe(m_TriggerSink);
            handler.OnComplete(result);
        }

        private sealed class TriggerSink(IEventHandler<T> handler) : IEventHandler<TTrigger>
        {
            public TakeUntilBridge<T, TTrigger> Owner { get; set; } = null!;

            public void Handle(TTrigger notification) => Owner.OnTrigger();

            public void OnComplete(Result<TTrigger, Exception> result) { }
        }
    }
}

/// <summary>
/// Completes the source when a cancellation token is cancelled.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class TakeUntilTokenEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    CancellationToken cancellationToken) : IEventSubscribable<TNotification>
{
    private readonly List<TokenBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new TokenBridge<TNotification>(source, cancellationToken, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class TokenBridge<T>(
        IEventSubscribable<T> source,
        CancellationToken cancellationToken,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private CancellationTokenRegistration m_Registration;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                m_Disposed = true;
                handler.OnComplete(default);
                return;
            }

            m_Registration = cancellationToken.Register(static state => ((TokenBridge<T>)state!).Finish(default), this);
            source.Subscribe(this);
        }

        public void Handle(T notification) => handler.Handle(notification);

        public void OnComplete(Result<T, Exception> result) => Finish(result);

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Registration.Dispose();
            source.Unsubscribe(this);
        }

        private void Finish(Result<T, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Registration.Dispose();
            source.Unsubscribe(this);
            handler.OnComplete(result);
        }
    }
}

/// <summary>
/// Pairs the notifications of two sources by index.
/// </summary>
/// <typeparam name="TFirst">The first notification type.</typeparam>
/// <typeparam name="TSecond">The second notification type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
internal sealed class ZipEvent<TFirst, TSecond, TResult>(
    IEventSubscribable<TFirst> first,
    IEventSubscribable<TSecond> second,
    Func<TFirst, TSecond, TResult> selector) : IEventSubscribable<TResult>
{
    private readonly List<ZipBridge<TFirst, TSecond, TResult>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new ZipBridge<TFirst, TSecond, TResult>(first, second, selector, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class ZipBridge<TFirst, TSecond, TResult> : IEventHandler<TFirst>
    {
        private readonly IEventSubscribable<TFirst> m_First;
        private readonly IEventSubscribable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, TResult> m_Selector;
        private readonly IEventHandler<TResult> m_Handler;
        private readonly Queue<TFirst> m_Left = new();
        private readonly Queue<TSecond> m_Right = new();
        private readonly RightSink m_RightSink;
        private bool m_FirstCompleted;
        private bool m_SecondCompleted;
        private bool m_Disposed;

        public ZipBridge(
            IEventSubscribable<TFirst> first,
            IEventSubscribable<TSecond> second,
            Func<TFirst, TSecond, TResult> selector,
            IEventHandler<TResult> handler)
        {
            m_First = first;
            m_Second = second;
            m_Selector = selector;
            m_Handler = handler;
            m_RightSink = new RightSink(this);
        }

        public IEventHandler<TResult> Handler => m_Handler;

        public void Start()
        {
            m_First.Subscribe(this);
            m_Second.Subscribe(m_RightSink);
        }

        public void Handle(TFirst notification)
        {
            m_Left.Enqueue(notification);
            Drain();
        }

        public void OnComplete(Result<TFirst, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_FirstCompleted = true;
            if (m_SecondCompleted) Finish(default);
        }

        internal void OnRight(TSecond notification)
        {
            m_Right.Enqueue(notification);
            Drain();
        }

        internal void OnRightComplete(Result<TSecond, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_SecondCompleted = true;
            if (m_FirstCompleted) Finish(default);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_RightSink);
        }

        private void Drain()
        {
            while (m_Left.Count > 0 && m_Right.Count > 0)
            {
                m_Handler.Handle(m_Selector(m_Left.Dequeue(), m_Right.Dequeue()));
            }
        }

        private void Finish(Result<TResult, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_RightSink);
            m_Handler.OnComplete(result);
        }

        private sealed class RightSink(ZipBridge<TFirst, TSecond, TResult> owner) : IEventHandler<TSecond>
        {
            public void Handle(TSecond notification) => owner.OnRight(notification);

            public void OnComplete(Result<TSecond, Exception> result) => owner.OnRightComplete(result);
        }
    }
}

/// <summary>
/// Emits the latest combination of two sources.
/// </summary>
/// <typeparam name="TFirst">The first notification type.</typeparam>
/// <typeparam name="TSecond">The second notification type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
internal sealed class CombineLatestEvent<TFirst, TSecond, TResult>(
    IEventSubscribable<TFirst> first,
    IEventSubscribable<TSecond> second,
    Func<TFirst, TSecond, TResult> selector) : IEventSubscribable<TResult>
{
    private readonly List<CombineLatestBridge<TFirst, TSecond, TResult>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new CombineLatestBridge<TFirst, TSecond, TResult>(first, second, selector, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class CombineLatestBridge<TFirst, TSecond, TResult> : IEventHandler<TFirst>
    {
        private readonly IEventSubscribable<TFirst> m_First;
        private readonly IEventSubscribable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, TResult> m_Selector;
        private readonly IEventHandler<TResult> m_Handler;
        private readonly RightSink m_RightSink;
        private TFirst? m_Left;
        private TSecond? m_Right;
        private bool m_HasLeft;
        private bool m_HasRight;
        private bool m_FirstCompleted;
        private bool m_SecondCompleted;
        private bool m_Disposed;

        public CombineLatestBridge(
            IEventSubscribable<TFirst> first,
            IEventSubscribable<TSecond> second,
            Func<TFirst, TSecond, TResult> selector,
            IEventHandler<TResult> handler)
        {
            m_First = first;
            m_Second = second;
            m_Selector = selector;
            m_Handler = handler;
            m_RightSink = new RightSink(this);
        }

        public IEventHandler<TResult> Handler => m_Handler;

        public void Start()
        {
            m_First.Subscribe(this);
            m_Second.Subscribe(m_RightSink);
        }

        public void Handle(TFirst notification)
        {
            m_Left = notification;
            m_HasLeft = true;
            Emit();
        }

        public void OnComplete(Result<TFirst, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_FirstCompleted = true;
            if (m_SecondCompleted) Finish(default);
        }

        internal void OnRight(TSecond notification)
        {
            m_Right = notification;
            m_HasRight = true;
            Emit();
        }

        internal void OnRightComplete(Result<TSecond, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_SecondCompleted = true;
            if (m_FirstCompleted) Finish(default);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_RightSink);
        }

        private void Emit()
        {
            if (!m_HasLeft || !m_HasRight) return;

            m_Handler.Handle(m_Selector(m_Left!, m_Right!));
        }

        private void Finish(Result<TResult, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_RightSink);
            m_Handler.OnComplete(result);
        }

        private sealed class RightSink(CombineLatestBridge<TFirst, TSecond, TResult> owner) : IEventHandler<TSecond>
        {
            public void Handle(TSecond notification) => owner.OnRight(notification);

            public void OnComplete(Result<TSecond, Exception> result) => owner.OnRightComplete(result);
        }
    }
}

/// <summary>
/// Skips notifications until a trigger fires.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TTrigger">The trigger notification type.</typeparam>
internal sealed class SkipUntilEvent<TNotification, TTrigger>(
    IEventSubscribable<TNotification> source,
    IEventSubscribable<TTrigger> trigger) : IEventSubscribable<TNotification>
{
    private readonly List<SkipUntilBridge<TNotification, TTrigger>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new SkipUntilBridge<TNotification, TTrigger>(source, trigger, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class SkipUntilBridge<T, TTrigger>(
        IEventSubscribable<T> source,
        IEventSubscribable<TTrigger> trigger,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private readonly TriggerSink m_TriggerSink = new();
        private bool m_Open;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start()
        {
            m_TriggerSink.Owner = this;
            trigger.Subscribe(m_TriggerSink);
            source.Subscribe(this);
        }

        public void Handle(T notification)
        {
            if (m_Open) handler.Handle(notification);
        }

        public void OnComplete(Result<T, Exception> result) => Finish(result);

        internal void Open()
        {
            if (m_Open || m_Disposed) return;

            m_Open = true;
            trigger.Unsubscribe(m_TriggerSink);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);
            trigger.Unsubscribe(m_TriggerSink);
        }

        private void Finish(Result<T, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);
            trigger.Unsubscribe(m_TriggerSink);
            handler.OnComplete(result);
        }

        private sealed class TriggerSink : IEventHandler<TTrigger>
        {
            public SkipUntilBridge<T, TTrigger> Owner { get; set; } = null!;

            public void Handle(TTrigger notification) => Owner.Open();

            public void OnComplete(Result<TTrigger, Exception> result) { }
        }
    }
}

/// <summary>
/// Skips notifications until a cancellation token is cancelled.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SkipUntilTokenEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    CancellationToken cancellationToken) : IEventSubscribable<TNotification>
{
    private readonly List<TokenBridge<TNotification>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new TokenBridge<TNotification>(source, cancellationToken, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class TokenBridge<T>(
        IEventSubscribable<T> source,
        CancellationToken cancellationToken,
        IEventHandler<T> handler) : IEventHandler<T>
    {
        private CancellationTokenRegistration m_Registration;
        private bool m_Open;
        private bool m_Disposed;

        public IEventHandler<T> Handler => handler;

        public void Start()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                m_Open = true;
            }
            else
            {
                m_Registration = cancellationToken.Register(static state => ((TokenBridge<T>)state!).Open(), this);
            }

            source.Subscribe(this);
        }

        public void Handle(T notification)
        {
            if (m_Open) handler.Handle(notification);
        }

        public void OnComplete(Result<T, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Registration.Dispose();
            source.Unsubscribe(this);
            handler.OnComplete(result);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Registration.Dispose();
            source.Unsubscribe(this);
        }

        private void Open() => m_Open = true;
    }
}

/// <summary>
/// Combines every main notification with the latest notification of another source.
/// </summary>
/// <typeparam name="TFirst">The main notification type.</typeparam>
/// <typeparam name="TSecond">The other notification type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
internal sealed class WithLatestFromEvent<TFirst, TSecond, TResult>(
    IEventSubscribable<TFirst> first,
    IEventSubscribable<TSecond> second,
    Func<TFirst, TSecond, TResult> selector) : IEventSubscribable<TResult>
{
    private readonly List<WithLatestFromBridge<TFirst, TSecond, TResult>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new WithLatestFromBridge<TFirst, TSecond, TResult>(first, second, selector, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class WithLatestFromBridge<TFirst, TSecond, TResult> : IEventHandler<TFirst>
    {
        private readonly IEventSubscribable<TFirst> m_First;
        private readonly IEventSubscribable<TSecond> m_Second;
        private readonly Func<TFirst, TSecond, TResult> m_Selector;
        private readonly IEventHandler<TResult> m_Handler;
        private readonly OtherSink m_OtherSink;
        private TSecond? m_Latest;
        private bool m_HasLatest;
        private bool m_Disposed;

        public WithLatestFromBridge(
            IEventSubscribable<TFirst> first,
            IEventSubscribable<TSecond> second,
            Func<TFirst, TSecond, TResult> selector,
            IEventHandler<TResult> handler)
        {
            m_First = first;
            m_Second = second;
            m_Selector = selector;
            m_Handler = handler;
            m_OtherSink = new OtherSink(this);
        }

        public IEventHandler<TResult> Handler => m_Handler;

        public void Start()
        {
            m_Second.Subscribe(m_OtherSink);
            m_First.Subscribe(this);
        }

        public void Handle(TFirst notification)
        {
            if (m_HasLatest) m_Handler.Handle(m_Selector(notification, m_Latest!));
        }

        public void OnComplete(Result<TFirst, Exception> result)
        {
            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            Finish(default);
        }

        internal void OnOther(TSecond notification)
        {
            m_Latest = notification;
            m_HasLatest = true;
        }

        internal void OnOtherComplete(Result<TSecond, Exception> result)
        {
            if (!result.TryGetValue(out _, out var error)) Finish(error);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_OtherSink);
        }

        private void Finish(Result<TResult, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_First.Unsubscribe(this);
            m_Second.Unsubscribe(m_OtherSink);
            m_Handler.OnComplete(result);
        }

        private sealed class OtherSink(WithLatestFromBridge<TFirst, TSecond, TResult> owner) : IEventHandler<TSecond>
        {
            public void Handle(TSecond notification) => owner.OnOther(notification);

            public void OnComplete(Result<TSecond, Exception> result) => owner.OnOtherComplete(result);
        }
    }
}

/// <summary>
/// Emits a value before forwarding the source.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class PrependEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    TNotification value) : IEventSubscribable<TNotification>
{
    private readonly List<IEventHandler<TNotification>?> m_Handlers = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        handler.Handle(value);
        source.Subscribe(handler);
        m_Handlers.Add(handler);
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        var index = m_Handlers.LastIndexOf(handler);
        if (index < 0) return false;

        m_Handlers[index] = null;
        m_Handlers.RemoveAll(static item => item is null);
        return source.Unsubscribe(handler);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        for (var index = 0; index < m_Handlers.Count; index++)
        {
            if (m_Handlers[index] is { } handler) source.Unsubscribe(handler);
        }

        m_Handlers.Clear();
    }
}

/// <summary>
/// Flattens the inner sources produced by a selector.
/// </summary>
/// <typeparam name="TSource">The notification type of the source.</typeparam>
/// <typeparam name="TResult">The notification type of the inner sources.</typeparam>
internal sealed class SelectManyEvent<TSource, TResult>(
    IEventSubscribable<TSource> source,
    Func<TSource, IEventSubscribable<TResult>> selector) : IEventSubscribable<TResult>
{
    private readonly List<SelectManyBridge<TSource, TResult>> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new SelectManyBridge<TSource, TResult>(source, selector, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TResult> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class SelectManyBridge<TSource, TResult>(
        IEventSubscribable<TSource> source,
        Func<TSource, IEventSubscribable<TResult>> selector,
        IEventHandler<TResult> handler) : IEventHandler<TSource>
    {
        private readonly List<InnerSink> m_Inners = [];
        private bool m_SourceCompleted;
        private bool m_Disposed;

        public IEventHandler<TResult> Handler => handler;

        public void Start() => source.Subscribe(this);

        public void Handle(TSource notification)
        {
            if (m_Disposed) return;

            var inner = selector(notification);
            var sink = new InnerSink(this, inner);
            m_Inners.Add(sink);
            inner.Subscribe(sink);
        }

        public void OnComplete(Result<TSource, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_SourceCompleted = true;
            if (m_Inners.Count == 0) Finish(default);
        }

        private void OnInner(InnerSink sink)
        {
            m_Inners.Remove(sink);
            if (!m_SourceCompleted || m_Inners.Count != 0) return;

            Finish(default);
        }

        private void OnInnerError(InnerSink sink, Exception error)
        {
            m_Inners.Remove(sink);
            Finish(error);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);

            for (var index = 0; index < m_Inners.Count; index++)
            {
                m_Inners[index].Dispose();
            }

            m_Inners.Clear();
        }

        private void Finish(Result<TResult, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            source.Unsubscribe(this);

            for (var index = 0; index < m_Inners.Count; index++)
            {
                m_Inners[index].Dispose();
            }

            m_Inners.Clear();
            handler.OnComplete(result);
        }

        private sealed class InnerSink(SelectManyBridge<TSource, TResult> owner, IEventSubscribable<TResult> source) : IEventHandler<TResult>
        {
            public void Handle(TResult notification) => owner.Handler.Handle(notification);

            public void OnComplete(Result<TResult, Exception> result)
            {
                if (!result.TryGetValue(out _, out var error))
                {
                    owner.OnInnerError(this, error);
                    return;
                }

                owner.OnInner(this);
            }

            public void Dispose() => source.Unsubscribe(this);
        }
    }
}
/// <summary>
/// Compares two sources element by element and emits the answer when both complete.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SequenceEqualEvent<TNotification>(
    IEventSubscribable<TNotification> first,
    IEventSubscribable<TNotification> second,
    IEqualityComparer<TNotification> comparer) : IEventSubscribable<bool>
{
    private readonly List<Bridge> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<bool> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new Bridge(this, first, second, comparer, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<bool> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class Bridge(
        SequenceEqualEvent<TNotification> owner,
        IEventSubscribable<TNotification> first,
        IEventSubscribable<TNotification> second,
        IEqualityComparer<TNotification> comparer,
        IEventHandler<bool> handler) : IEventHandler<TNotification>
    {
        private readonly List<TNotification> m_First = [];
        private readonly List<TNotification> m_Second = [];
        private readonly SecondSink m_SecondSink = new();
        private bool m_FirstCompleted;
        private bool m_SecondCompleted;
        private bool m_Disposed;

        public IEventHandler<bool> Handler => handler;

        public void Start()
        {
            m_SecondSink.Owner = this;
            second.Subscribe(m_SecondSink);
            first.Subscribe(this);
        }

        public void Handle(TNotification notification) => m_First.Add(notification);

        public void OnComplete(Result<TNotification, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_FirstCompleted = true;
            if (m_SecondCompleted) Compare();
        }

        internal void OnSecond(TNotification notification) => m_Second.Add(notification);

        internal void OnSecondComplete(Result<TNotification, Exception> result)
        {
            if (m_Disposed) return;

            if (!result.TryGetValue(out _, out var error))
            {
                Finish(error);
                return;
            }

            m_SecondCompleted = true;
            if (m_FirstCompleted) Compare();
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            first.Unsubscribe(this);
            second.Unsubscribe(m_SecondSink);
        }

        private void Compare()
        {
            var equal = m_First.Count == m_Second.Count;
            if (equal)
            {
                for (var index = 0; index < m_First.Count; index++)
                {
                    if (comparer.Equals(m_First[index], m_Second[index])) continue;

                    equal = false;
                    break;
                }
            }

            Finish(equal);
        }

        private void Finish(bool equal)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            first.Unsubscribe(this);
            second.Unsubscribe(m_SecondSink);
            handler.Handle(equal);
            handler.OnComplete(default);
        }

        private void Finish(Exception error)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            first.Unsubscribe(this);
            second.Unsubscribe(m_SecondSink);
            handler.OnComplete(error);
        }

        private sealed class SecondSink : IEventHandler<TNotification>
        {
            public Bridge Owner { get; set; } = null!;

            public void Handle(TNotification notification) => Owner.OnSecond(notification);

            public void OnComplete(Result<TNotification, Exception> result) => Owner.OnSecondComplete(result);
        }
    }
}
/// <summary>
/// Subscribes to the source after a delay.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DelaySubscriptionEvent<TNotification>(
    IEventSubscribable<TNotification> source,
    ITimeProvider timeProvider,
    TimeSpan dueTime) : IEventSubscribable<TNotification>
{
    private readonly List<Bridge> m_Bridges = [];

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) throw new ArgumentNullException(nameof(handler));

        var bridge = new Bridge(this, source, timeProvider, dueTime, handler);
        m_Bridges.Add(bridge);
        bridge.Start();
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<TNotification> handler)
    {
        if (handler is null) return false;

        for (var index = m_Bridges.Count - 1; index >= 0; index--)
        {
            if (!m_Bridges[index].Handler.Equals(handler)) continue;

            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        while (m_Bridges.Count > 0)
        {
            var index = m_Bridges.Count - 1;
            var bridge = m_Bridges[index];
            m_Bridges.RemoveAt(index);
            bridge.Dispose();
        }
    }

    private sealed class Bridge(
        DelaySubscriptionEvent<TNotification> owner,
        IEventSubscribable<TNotification> source,
        ITimeProvider timeProvider,
        TimeSpan dueTime,
        IEventHandler<TNotification> handler) : IEventHandler<TNotification>
    {
        private IDisposable? m_Schedule;
        private bool m_Subscribed;
        private bool m_Disposed;

        public IEventHandler<TNotification> Handler => handler;

        public void Start() => m_Schedule = timeProvider.Schedule(dueTime, Subscribe);

        public void Handle(TNotification notification) => handler.Handle(notification);

        public void OnComplete(Result<TNotification, Exception> result) => Finish(result);

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Schedule?.Dispose();
            m_Schedule = null;
            if (m_Subscribed) source.Unsubscribe(this);
        }

        private void Subscribe()
        {
            if (m_Disposed) return;

            m_Subscribed = true;
            source.Subscribe(this);
        }

        private void Finish(Result<TNotification, Exception> result)
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_Schedule?.Dispose();
            m_Schedule = null;
            source.Unsubscribe(this);
            handler.OnComplete(result);
        }
    }
}
