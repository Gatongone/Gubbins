using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Extensions for <see cref="IWeakEventSubscriable{TNotification}"/>.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("ReSharper", "UnusedType.Global")]
public static class EventSubscribeWeaklyExtensions
{
    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="eventHandler">The handler that would be referenced weakly by the event bus.</param>
    public static void SubscribeWeakly<TNotification>(this IWeakEventSubscriable<TNotification> eventBus, IEventHandler<TNotification> eventHandler)
        => eventBus.SubscribeWeakly(new WeakEventHandler<TNotification>(eventHandler));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly(this IWeakEventSubscriable<Unit> eventBus, IEquatable<DBNull> provider, Action action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly(this IWeakEventSubscriable<Unit> eventBus, Action action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T>(this IWeakEventSubscriable<T> eventBus, IEquatable<DBNull> provider, Action<T> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T>(this IWeakEventSubscriable<T> eventBus, Action<T> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2>(this IWeakEventSubscriable<(T1, T2)> eventBus, IEquatable<DBNull> provider, Action<T1, T2> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2>(this IWeakEventSubscriable<(T1, T2)> eventBus, Action<T1, T2> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3>(this IWeakEventSubscriable<(T1, T2, T3)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3>(this IWeakEventSubscriable<(T1, T2, T3)> eventBus, Action<T1, T2, T3> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4>(this IWeakEventSubscriable<(T1, T2, T3, T4)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4>(this IWeakEventSubscriable<(T1, T2, T3, T4)> eventBus, Action<T1, T2, T3, T4> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5)> eventBus, Action<T1, T2, T3, T4, T5> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6)> eventBus, Action<T1, T2, T3, T4, T5, T6> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="provider">The event provider which should contains the delegation that from instance method.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> eventBus, IEquatable<DBNull> provider, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(action, provider));

    /// <summary>
    /// Convert action delegate to ActionEventHandler, then register to event bus.
    /// </summary>
    /// <param name="eventBus">Event bus.</param>
    /// <param name="action">The delegate which convert to ActionEventHandler.</param>
    public static void SubscribeWeakly<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IWeakEventSubscriable<(T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15)> eventBus, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action)
        => eventBus.SubscribeWeakly(new WeakActionEventHandler<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(action, action.Target as IEquatable<DBNull> ?? throw new ArgumentException($"Delegate {nameof(action.Target)} is not a IEquatable<DBNull>")));
}