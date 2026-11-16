using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// A handler that needs to run cleanup when its subscription is detached.
/// </summary>
internal interface IEventFinalizer
{
    /// <summary>
    /// Runs the cleanup once.
    /// </summary>
    void Finalize();
}

/// <summary>
/// Forwards only the notifications that satisfy a predicate.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class FilterHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Func<TNotification, bool> predicate) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (predicate(notification)) handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Projects every notification before forwarding it.
/// </summary>
/// <typeparam name="TSource">The notification type of the source.</typeparam>
/// <typeparam name="TResult">The notification type of the projection.</typeparam>
internal sealed class ProjectHandler<TSource, TResult>(
    IEventHandler<TResult> handler,
    Func<TSource, TResult> selector) : IEventHandler<TSource>
{
    /// <inheritdoc/>
    public void Handle(TSource notification) => handler.Handle(selector(notification));

    /// <inheritdoc/>
    public void OnComplete(Result<TSource, Exception> result) => handler.OnComplete(result.Map(selector));
}

/// <summary>
/// Forwards a fixed number of notifications, then detaches and completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class TakeHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler,
    int count) : IEventHandler<TNotification>
{
    /// <summary>
    /// The number of notifications still allowed.
    /// </summary>
    private int m_Remaining = count;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Remaining <= 0) return;

        handler.Handle(notification);
        if (--m_Remaining != 0) return;

        source.Unsubscribe(this);
        handler.OnComplete(default);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Forwards only the notifications that differ from the last forwarded one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DistinctHandler<TNotification>(
    IEventHandler<TNotification> handler,
    IEqualityComparer<TNotification> comparer) : IEventHandler<TNotification>
{
    /// <summary>
    /// Whether a notification was forwarded already.
    /// </summary>
    private bool m_HasValue;

    /// <summary>
    /// The last forwarded notification.
    /// </summary>
    private TNotification? m_Last;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_HasValue && comparer.Equals(m_Last!, notification)) return;

        m_HasValue = true;
        m_Last = notification;
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Forwards every notification and completion unchanged.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class IdentityHandler<TNotification>(IEventHandler<TNotification> handler) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Emits the last notification when the source completes, or fails when it is empty.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class LastHandler<TNotification>(IEventHandler<TNotification> handler) : IEventHandler<TNotification>
{
    /// <summary>
    /// Whether a notification was seen.
    /// </summary>
    private bool m_HasValue;

    /// <summary>
    /// The last notification.
    /// </summary>
    private TNotification? m_Last;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_HasValue = true;
        m_Last = notification;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (!m_HasValue)
        {
            handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
            return;
        }

        handler.Handle(m_Last!);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the first notification, then detaches and completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class FirstHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        handler.Handle(notification);
        source.Unsubscribe(this);
        handler.OnComplete(default);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
    }
}

/// <summary>
/// Folds every notification into one value and emits it when the source completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TResult">The aggregate type.</typeparam>
internal sealed class AggregateHandler<TNotification, TResult>(
    IEventHandler<TResult> handler,
    TResult seed,
    Func<TResult, TNotification, TResult> accumulate) : IEventHandler<TNotification>
{
    /// <summary>
    /// The aggregate so far.
    /// </summary>
    private TResult m_Aggregate = seed;

    /// <inheritdoc/>
    public void Handle(TNotification notification) => m_Aggregate = accumulate(m_Aggregate, notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(m_Aggregate);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the average of the notifications when the source completes, or fails when it is empty.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class AverageHandler<TNotification>(
    IEventHandler<double> handler,
    Func<TNotification, double> selector) : IEventHandler<TNotification>
{
    /// <summary>
    /// The sum so far.
    /// </summary>
    private double m_Sum;

    /// <summary>
    /// The number of notifications.
    /// </summary>
    private int m_Count;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_Sum += selector(notification);
        m_Count++;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (m_Count == 0)
        {
            handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
            return;
        }

        handler.Handle(m_Sum / m_Count);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the smallest or largest notification when the source completes, or fails when it is empty.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ExtremumHandler<TNotification>(
    IEventHandler<TNotification> handler,
    bool isMax) : IEventHandler<TNotification>
{
    /// <summary>
    /// The comparer used to order the notifications.
    /// </summary>
    private readonly Comparer<TNotification> m_Comparer = Comparer<TNotification>.Default;

    /// <summary>
    /// Whether a notification was seen.
    /// </summary>
    private bool m_HasValue;

    /// <summary>
    /// The current extremum.
    /// </summary>
    private TNotification? m_Value;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (!m_HasValue || (isMax ? m_Comparer.Compare(notification, m_Value!) > 0 : m_Comparer.Compare(notification, m_Value!) < 0))
        {
            m_HasValue = true;
            m_Value = notification;
        }
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (!m_HasValue)
        {
            handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
            return;
        }

        handler.Handle(m_Value!);
        handler.OnComplete(default);
    }
}

/// <summary>
/// <summary>
/// Skips the first notifications of the source.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SkipHandler<TNotification>(IEventHandler<TNotification> handler, int count) : IEventHandler<TNotification>
{
    private int m_Remaining = count;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Remaining > 0)
        {
            m_Remaining--;
            return;
        }

        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Skips notifications while a predicate holds.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SkipWhileHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Func<TNotification, bool> predicate) : IEventHandler<TNotification>
{
    private bool m_Open;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (!m_Open && predicate(notification)) return;

        m_Open = true;
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Forwards notifications while a predicate holds, then detaches and completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class TakeWhileHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler,
    Func<TNotification, bool> predicate) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (!predicate(notification))
        {
            source.Unsubscribe(this);
            handler.OnComplete(default);
            return;
        }

        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Emits the running aggregate of every notification.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ScanHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Func<TNotification, TNotification, TNotification> accumulate) : IEventHandler<TNotification>
{
    private TNotification? m_State;
    private bool m_HasState;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_State = m_HasState ? accumulate(m_State!, notification) : notification;
        m_HasState = true;
        handler.Handle(m_State);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Emits the running aggregate of every notification, starting from a seed.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ScanSeedHandler<TNotification>(
    IEventHandler<TNotification> handler,
    TNotification seed,
    Func<TNotification, TNotification, TNotification> accumulate) : IEventHandler<TNotification>
{
    private TNotification m_State = seed;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_State = accumulate(m_State, notification);
        handler.Handle(m_State);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Emits every notification together with the previous one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class PairwiseHandler<TNotification>(IEventHandler<(TNotification, TNotification)> handler) : IEventHandler<TNotification>
{
    private TNotification? m_Previous;
    private bool m_HasPrevious;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_HasPrevious) handler.Handle((m_Previous!, notification));

        m_Previous = notification;
        m_HasPrevious = true;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits notifications in fixed-size arrays.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class BufferHandler<TNotification>(
    IEventHandler<TNotification[]> handler,
    int count) : IEventHandler<TNotification>
{
    private readonly List<TNotification> m_Buffer = [];

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_Buffer.Add(notification);
        if (m_Buffer.Count != count) return;

        handler.Handle(m_Buffer.ToArray());
        m_Buffer.Clear();
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (m_Buffer.Count > 0)
        {
            handler.Handle(m_Buffer.ToArray());
            m_Buffer.Clear();
        }

        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits a default notification when the source completes without one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DefaultIfEmptyHandler<TNotification>(
    IEventHandler<TNotification> handler,
    TNotification defaultValue) : IEventHandler<TNotification>
{
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_HasValue = true;
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (!m_HasValue) handler.Handle(defaultValue);

        handler.OnComplete(default);
    }
}

/// <summary>
/// Suppresses every notification and forwards only the completion.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class IgnoreElementsHandler<TNotification>(IEventHandler<TNotification> handler) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification) { }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Casts every notification to another type.
/// </summary>
/// <typeparam name="TSource">The notification type of the source.</typeparam>
/// <typeparam name="TResult">The notification type of the cast.</typeparam>
internal sealed class CastHandler<TSource, TResult>(IEventHandler<TResult> handler) : IEventHandler<TSource>
{
    /// <inheritdoc/>
    public void Handle(TSource notification) => handler.Handle((TResult)(object)notification!);

    /// <inheritdoc/>
    public void OnComplete(Result<TSource, Exception> result)
        => handler.OnComplete(result.Map(static value => (TResult)(object)value!));
}

/// <summary>
/// Forwards only the notifications that are assignable to another type.
/// </summary>
/// <typeparam name="TSource">The notification type of the source.</typeparam>
/// <typeparam name="TResult">The notification type to filter for.</typeparam>
internal sealed class OfTypeHandler<TSource, TResult>(IEventHandler<TResult> handler) : IEventHandler<TSource>
{
    /// <inheritdoc/>
    public void Handle(TSource notification)
    {
        if (notification is TResult value) handler.Handle(value);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TSource, Exception> result)
        => handler.OnComplete(result.Map(static value => value is TResult casted ? casted : default!));
}

/// <summary>
/// Emits whether at least one notification satisfies a predicate.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class AnyHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<bool> handler,
    Func<TNotification, bool> predicate) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (!predicate(notification)) return;

        handler.Handle(true);
        source.Unsubscribe(this);
        handler.OnComplete(default);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(false);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits whether every notification satisfies a predicate.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class AllHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<bool> handler,
    Func<TNotification, bool> predicate) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (predicate(notification)) return;

        handler.Handle(false);
        source.Unsubscribe(this);
        handler.OnComplete(default);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(true);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Collects every notification and emits the list when the source completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ToListHandler<TNotification>(IEventHandler<IReadOnlyList<TNotification>> handler) : IEventHandler<TNotification>
{
    private readonly List<TNotification> m_Values = [];

    /// <inheritdoc/>
    public void Handle(TNotification notification) => m_Values.Add(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(m_Values);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Collects every notification and emits the array when the source completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ToArrayHandler<TNotification>(IEventHandler<TNotification[]> handler) : IEventHandler<TNotification>
{
    private readonly List<TNotification> m_Values = [];

    /// <inheritdoc/>
    public void Handle(TNotification notification) => m_Values.Add(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle([.. m_Values]);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Invokes an action when the wrapped handler completes or is detached.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class FinallyHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Action onFinally) : IEventHandler<TNotification>, IEventFinalizer
{
    private bool m_Invoked;

    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        Invoke();
        handler.OnComplete(result);
    }

    /// <inheritdoc/>
    public void Finalize() => Invoke();

    private void Invoke()
    {
        if (m_Invoked) return;

        m_Invoked = true;
        onFinally();
    }
}

internal sealed class TaskHandler<TNotification>(TaskCompletionSource<TNotification> completion) : IEventHandler<TNotification>
{
    private TNotification? m_Last;

    /// <inheritdoc/>
    public void Handle(TNotification notification) => m_Last = notification;

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            completion.TrySetException(error);
            return;
        }

        completion.TrySetResult(m_Last!);
    }
}

/// <summary>
/// Forwards only the notifications with a key that was not seen before.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class DistinctByHandler<TNotification, TKey>(
    IEventHandler<TNotification> handler,
    Func<TNotification, TKey> keySelector,
    IEqualityComparer<TKey> comparer) : IEventHandler<TNotification>
{
    private readonly HashSet<TKey> m_Seen = new(comparer);

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Seen.Add(keySelector(notification))) handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Buffers the last notifications and emits them on completion.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class TakeLastHandler<TNotification>(
    IEventHandler<TNotification> handler,
    int count) : IEventHandler<TNotification>
{
    private readonly Queue<TNotification> m_Buffer = new();

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (count == 0) return;

        m_Buffer.Enqueue(notification);
        if (m_Buffer.Count > count) m_Buffer.Dequeue();
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        while (m_Buffer.Count > 0) handler.Handle(m_Buffer.Dequeue());

        handler.OnComplete(default);
    }
}

/// <summary>
/// Drops the last notifications and forwards the rest immediately.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SkipLastHandler<TNotification>(
    IEventHandler<TNotification> handler,
    int count) : IEventHandler<TNotification>
{
    private readonly Queue<TNotification> m_Buffer = new();

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_Buffer.Enqueue(notification);
        if (m_Buffer.Count > count) handler.Handle(m_Buffer.Dequeue());
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Emits one extra value when the source completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class AppendHandler<TNotification>(
    IEventHandler<TNotification> handler,
    TNotification value) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(value);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Invokes a side effect before forwarding a notification.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DoHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Action<TNotification> onNext) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        onNext(notification);
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Invokes a side effect when the source completes.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DoOnCompleteHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Action<Result<TNotification, Exception>> onComplete) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        onComplete(result);
        handler.OnComplete(result);
    }
}

/// <summary>
/// Invokes a side effect when the subscription is detached.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DoOnDisposeHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Action onDispose) : IEventHandler<TNotification>, IEventFinalizer
{
    private bool m_Invoked;

    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);

    /// <inheritdoc/>
    public void Finalize()
    {
        if (m_Invoked) return;

        m_Invoked = true;
        onDispose();
    }
}

/// <summary>
/// Invokes a side effect when the source fails.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DoOnErrorHandler<TNotification>(
    IEventHandler<TNotification> handler,
    Action<Exception> onError) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error)) onError(error);

        handler.OnComplete(result);
    }
}

/// <summary>
/// Emits each notification together with its zero-based index.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class IndexHandler<TNotification>(
    IEventHandler<(int, TNotification)> handler) : IEventHandler<TNotification>
{
    private int m_Index;

    /// <inheritdoc/>
    public void Handle(TNotification notification) => handler.Handle((m_Index++, notification));

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the notification with the smallest or largest key on completion.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class ExtremumByHandler<TNotification, TKey>(
    IEventHandler<TNotification> handler,
    Func<TNotification, TKey> keySelector,
    bool isMax) : IEventHandler<TNotification>
{
    private readonly Comparer<TKey> m_Comparer = Comparer<TKey>.Default;
    private TNotification? m_Value;
    private TKey? m_Key;
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        var key = keySelector(notification);
        if (m_HasValue && (isMax ? m_Comparer.Compare(key, m_Key!) <= 0 : m_Comparer.Compare(key, m_Key!) >= 0)) return;

        m_Value = notification;
        m_Key = key;
        m_HasValue = true;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (!m_HasValue)
        {
            handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
            return;
        }

        handler.Handle(m_Value!);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the notification at a fixed index.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ElementAtHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler,
    int index,
    TNotification? defaultValue,
    bool useDefault) : IEventHandler<TNotification>
{
    private int m_Index = -1;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_Index++;
        if (m_Index != index) return;

        handler.Handle(notification);
        handler.OnComplete(default);
        source.Unsubscribe(this);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (useDefault)
        {
            handler.Handle(defaultValue!);
            handler.OnComplete(default);
            return;
        }

        handler.OnComplete(new InvalidOperationException("The source completed before the requested index."));
    }
}

/// <summary>
/// Emits the only notification or fails when there is not exactly one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SingleHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler,
    TNotification? defaultValue,
    bool useDefault) : IEventHandler<TNotification>
{
    private TNotification? m_Value;
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_HasValue)
        {
            source.Unsubscribe(this);
            handler.OnComplete(new InvalidOperationException("The source contains more than one notification."));
            return;
        }

        m_Value = notification;
        m_HasValue = true;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (!m_HasValue)
        {
            if (!useDefault)
            {
                handler.OnComplete(new InvalidOperationException("The source completed without any notification."));
                return;
            }

            handler.Handle(defaultValue!);
            handler.OnComplete(default);
            return;
        }

        handler.Handle(m_Value!);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the first notification on completion, or a default value.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class FirstOrDefaultHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    IEventHandler<TNotification> handler,
    TNotification defaultValue) : IEventHandler<TNotification>
{
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_HasValue) return;

        m_HasValue = true;
        handler.Handle(notification);
        handler.OnComplete(default);
        source.Unsubscribe(this);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        if (m_HasValue) return;

        handler.Handle(defaultValue);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Emits the last notification on completion, or a default value.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class LastOrDefaultHandler<TNotification>(
    IEventHandler<TNotification> handler,
    TNotification defaultValue) : IEventHandler<TNotification>
{
    private TNotification? m_Value;
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        m_Value = notification;
        m_HasValue = true;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(m_HasValue ? m_Value! : defaultValue);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Collects every notification into a set on completion.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ToHashSetHandler<TNotification>(
    IEventHandler<HashSet<TNotification>> handler,
    IEqualityComparer<TNotification> comparer) : IEventHandler<TNotification>
{
    private readonly HashSet<TNotification> m_Values = new(comparer);

    /// <inheritdoc/>
    public void Handle(TNotification notification) => m_Values.Add(notification);

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(m_Values);
        handler.OnComplete(default);
    }
}

/// <summary>
/// Runs a side effect for every notification and completes with the source.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ForEachHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    Action<TNotification> onNext,
    TaskCompletionSource<Unit> completion) : IEventHandler<TNotification>
{
    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        try
        {
            onNext(notification);
        }
        catch (Exception error)
        {
            source.Unsubscribe(this);
            completion.TrySetException(error);
        }
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            completion.TrySetException(error);
            return;
        }

        completion.TrySetResult(default);
    }
}

/// <summary>
/// Collects every notification into a dictionary on completion.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TElement">The element type.</typeparam>
internal sealed class ToDictionaryHandler<TNotification, TKey, TElement>(
    IEventHandler<Dictionary<TKey, TElement>> handler,
    Func<TNotification, TKey> keySelector,
    Func<TNotification, TElement> elementSelector,
    IEqualityComparer<TKey> comparer) : IEventHandler<TNotification>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TElement> m_Values = new(comparer);

    /// <inheritdoc/>
    public void Handle(TNotification notification)
        => m_Values.Add(keySelector(notification), elementSelector(notification));

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (!result.TryGetValue(out _, out var error))
        {
            handler.OnComplete(error);
            return;
        }

        handler.Handle(m_Values);
        handler.OnComplete(default);
    }
}
/// <summary>
/// Forwards only the notifications whose selected key differs from the previous one.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
/// <typeparam name="TKey">The key type.</typeparam>
internal sealed class DistinctUntilChangedByHandler<TNotification, TKey>(
    IEventHandler<TNotification> handler,
    Func<TNotification, TKey> keySelector,
    IEqualityComparer<TKey> comparer) : IEventHandler<TNotification>
{
    private TKey? m_Last;
    private bool m_HasValue;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        var key = keySelector(notification);
        if (m_HasValue && comparer.Equals(m_Last!, key)) return;

        m_Last = key;
        m_HasValue = true;
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}
/// <summary>
/// Forwards the first notification and suppresses the following ones inside a time window.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ThrottleFirstHandler<TNotification>(
    ITimeProvider timeProvider,
    TimeSpan window,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>
{
    private DateTimeOffset? m_NextAllowed;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        var now = timeProvider.UtcNow;
        if (m_NextAllowed is { } next && now < next) return;

        m_NextAllowed = now + window;
        handler.Handle(notification);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result) => handler.OnComplete(result);
}

/// <summary>
/// Delays every notification and the completion by a fixed duration.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DelayHandler<TNotification>(
    ITimeProvider timeProvider,
    TimeSpan dueTime,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>, IEventFinalizer
{
    private readonly List<IDisposable> m_Pending = [];

    /// <inheritdoc/>
    public void Handle(TNotification notification)
        => m_Pending.Add(timeProvider.Schedule(dueTime, () => handler.Handle(notification)));

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
        => m_Pending.Add(timeProvider.Schedule(dueTime, () => handler.OnComplete(result)));

    /// <inheritdoc/>
    public void Finalize()
    {
        for (var index = 0; index < m_Pending.Count; index++) m_Pending[index].Dispose();

        m_Pending.Clear();
    }
}
/// <summary>
/// Emits a notification only after the source stays quiet for a fixed duration.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class DebounceHandler<TNotification>(
    ITimeProvider timeProvider,
    TimeSpan dueTime,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>, IEventFinalizer
{
    private IDisposable? m_Timer;
    private TNotification? m_Value;
    private bool m_HasValue;
    private bool m_Disposed;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Disposed) return;

        m_Value = notification;
        m_HasValue = true;
        m_Timer?.Dispose();
        m_Timer = timeProvider.Schedule(dueTime, Emit);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Disposed) return;

        m_Timer?.Dispose();
        m_Timer = null;

        if (result.Valid && m_HasValue) Emit();

        m_Disposed = true;
        handler.OnComplete(result);
    }

    /// <inheritdoc/>
    public void Finalize()
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer?.Dispose();
        m_Timer = null;
    }

    private void Emit()
    {
        if (m_Disposed || !m_HasValue) return;

        m_HasValue = false;
        handler.Handle(m_Value!);
        m_Value = default;
    }
}

/// <summary>
/// Emits the latest notification on every sampling period.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class SampleHandler<TNotification> : IEventHandler<TNotification>, IEventFinalizer
{
    private readonly IEventHandler<TNotification> m_Handler;
    private readonly IDisposable m_Timer;
    private TNotification? m_Value;
    private bool m_HasValue;
    private bool m_Disposed;

    /// <summary>
    /// Creates the handler and starts the sampling timer.
    /// </summary>
    /// <param name="timeProvider">The clock used to sample.</param>
    /// <param name="period">The sampling period.</param>
    /// <param name="handler">The downstream handler.</param>
    public SampleHandler(ITimeProvider timeProvider, TimeSpan period, IEventHandler<TNotification> handler)
    {
        m_Handler = handler;
        m_Timer = timeProvider.SchedulePeriodic(period, Tick);
    }

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Disposed) return;

        m_Value = notification;
        m_HasValue = true;
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer.Dispose();

        if (result.Valid && m_HasValue) m_Handler.Handle(m_Value!);

        m_Handler.OnComplete(result);
    }

    /// <inheritdoc/>
    public void Finalize()
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer.Dispose();
    }

    private void Tick()
    {
        if (m_Disposed || !m_HasValue) return;

        m_HasValue = false;
        m_Handler.Handle(m_Value!);
        m_Value = default;
    }
}

/// <summary>
/// Fails when the source does not emit within the timeout period.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class TimeoutHandler<TNotification>(
    IEventSubscribable<TNotification> source,
    ITimeProvider timeProvider,
    TimeSpan dueTime,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>, IEventFinalizer
{
    private IDisposable? m_Timer;
    private bool m_Disposed;

    /// <summary>
    /// Starts the timeout timer.
    /// </summary>
    public void Start() => m_Timer = timeProvider.Schedule(dueTime, OnTimeout);

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Disposed) return;

        handler.Handle(notification);
        m_Timer?.Dispose();
        m_Timer = timeProvider.Schedule(dueTime, OnTimeout);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer?.Dispose();
        m_Timer = null;
        handler.OnComplete(result);
    }

    /// <inheritdoc/>
    public void Finalize()
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer?.Dispose();
        m_Timer = null;
    }

    private void OnTimeout()
    {
        if (m_Disposed) return;

        m_Disposed = true;
        source.Unsubscribe(this);
        handler.OnComplete(new TimeoutException("The source did not emit within the timeout period."));
    }
}
/// <summary>
/// Emits the latest notification at the end of each window.
/// </summary>
/// <typeparam name="TNotification">The notification type.</typeparam>
internal sealed class ThrottleLastHandler<TNotification>(
    ITimeProvider timeProvider,
    TimeSpan window,
    IEventHandler<TNotification> handler) : IEventHandler<TNotification>, IEventFinalizer
{
    private IDisposable? m_Timer;
    private TNotification? m_Value;
    private bool m_HasValue;
    private bool m_Disposed;

    /// <inheritdoc/>
    public void Handle(TNotification notification)
    {
        if (m_Disposed) return;

        m_Value = notification;
        m_HasValue = true;
        m_Timer ??= timeProvider.Schedule(window, Emit);
    }

    /// <inheritdoc/>
    public void OnComplete(Result<TNotification, Exception> result)
    {
        if (m_Disposed) return;

        m_Timer?.Dispose();
        m_Timer = null;

        if (result.Valid && m_HasValue) Emit();

        m_Disposed = true;
        handler.OnComplete(result);
    }

    /// <inheritdoc/>
    public void Finalize()
    {
        if (m_Disposed) return;

        m_Disposed = true;
        m_Timer?.Dispose();
        m_Timer = null;
    }

    private void Emit()
    {
        m_Timer = null;
        if (m_Disposed || !m_HasValue) return;

        m_HasValue = false;
        handler.Handle(m_Value!);
        m_Value = default;
    }
}
