using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Extensions for <see cref="IEventSubscriable{TNotification}"/>.
/// </summary>
public static class LinkableEventSubscribeExtensions
{
    #region Subscribe

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{Unit, TResult}.Subscribe(ILinkableEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TResult>(this ILinkableEventSubscriable<Unit, TResult> subscriber, Func<TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification})"/>
    /// </summary>
    public static void Subscribe<TArg, TResult>(this ILinkableEventSubscriable<TArg, TResult> subscriber, Func<TArg, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2), TResult> subscriber, Func<TArg1, TArg2, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3), TResult> subscriber, Func<TArg1, TArg2, TArg3, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Subscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Subscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> delegation)
        => subscriber.Subscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(delegation));

    #endregion

    #region Unsubscribe

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{Unit, TResult}.Unsubscribe(ILinkableEventHandler{Unit, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TResult>(this ILinkableEventSubscriable<Unit, TResult> subscriber, Func<TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg, TResult>(this ILinkableEventSubscriable<TArg, TResult> subscriber, Func<TArg, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2), TResult> subscriber, Func<TArg1, TArg2, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3), TResult> subscriber, Func<TArg1, TArg2, TArg3, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(delegation));

    /// <summary>
    /// <inheritdoc cref="ILinkableEventSubscriable{TNotification, TResult}.Unsubscribe(ILinkableEventHandler{TNotification, TResult})"/>
    /// </summary>
    public static void Unsubscribe<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(this ILinkableEventSubscriable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14), TResult> subscriber, Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult, TResult> delegation)
        => subscriber.Unsubscribe(new LinkableEventHandler<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(delegation));

    #endregion
}