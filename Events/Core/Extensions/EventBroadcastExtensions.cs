using Gubbins.Enhance;
namespace Gubbins.Events;

/// <summary>
/// Extensions for <see cref="IEventBroadcastable{TNotification}"/>.
/// </summary>
public static class EventBroadcastExtensions
{
    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{Unit}.Broadcast(Unit)"/>
    /// </summary>
    public static void Broadcast(this IEventBroadcastable<Unit> publisher)
        => publisher.Broadcast(Unit.Instance);

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2>(this IEventBroadcastable<(TArg1, TArg2)> publisher, TArg1 arg1, TArg2 arg2)
        => publisher.Broadcast((arg1, arg2));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3>(this IEventBroadcastable<(TArg1, TArg2, TArg3)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        => publisher.Broadcast((arg1, arg2, arg3));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        => publisher.Broadcast((arg1, arg2, arg3, arg4));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15));

    /// <summary>
    /// <inheritdoc cref="IEventBroadcastable{TNotification}.Broadcast(TNotification)"/>
    /// </summary>
    public static void Broadcast<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(this IEventBroadcastable<(TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16)> publisher, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16)
        => publisher.Broadcast((arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16));
}