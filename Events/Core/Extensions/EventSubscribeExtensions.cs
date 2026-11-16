using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Extensions for <see cref="IEventSubscribable{TNotification}"/>.
/// </summary>
public static class EventSubscribeExtensions
{
    #region Subscribe

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{Unit})"/>
    /// </summary>
    public static void Subscribe(this IEventSubscribable<Unit> subscriber, Action delegation)
        => subscriber.Subscribe(new ActionEventHandler(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg>(this IEventSubscribable<TArg> subscriber, Action<TArg> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2>(this IEventSubscribable<(TArg1, TArg2)> subscriber, Action<TArg1, TArg2> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3>(this IEventSubscribable<(TArg1, TArg2, TArg3)> subscriber, Action<TArg1, TArg2, TArg3> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4)> subscriber, Action<TArg1, TArg2, TArg3, TArg4> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Subscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> delegation)
        => subscriber.Subscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(delegation));

    #endregion

    #region Unsubscribe

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{Unit})"/>
    /// </summary>
    public static void Unsubscribe(this IEventSubscribable<Unit> subscriber, Action delegation)
        => subscriber.Unsubscribe(new ActionEventHandler(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg>(this IEventSubscribable<TArg> subscriber, Action<TArg> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2>(this IEventSubscribable<(TArg1, TArg2)> subscriber, Action<TArg1, TArg2> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3>(this IEventSubscribable<(TArg1, TArg2, TArg3)> subscriber, Action<TArg1, TArg2, TArg3> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4)> subscriber, Action<TArg1, TArg2, TArg3, TArg4> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(delegation));

    /// <summary>
    /// <inheritdoc cref="IEventSubscribable{TNotification}.Unsubscribe(IEventHandler{TNotification})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(this IEventSubscribable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16)> subscriber, Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> delegation)
        => subscriber.Unsubscribe(new ActionEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(delegation));

    #endregion
}