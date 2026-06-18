using Gubbins.Enhance;

namespace Gubbins.Events;

/// <inheritdoc cref="ILinkableEventBroadcastable{Unit, TResult}"/>
public interface ILinkableEventBroadcastable<out TResult> : ILinkableEventBroadcastable<Unit, TResult>;

/// <summary>
/// Linkable event broadcastable interface, which can be linked to other events and broadcasted sequentially.
/// The broadcasted result can be used as the input of the next event in the chain.
/// </summary>
/// <typeparam name="TNotification"> The type of the notification to be broadcasted.</typeparam>
/// <typeparam name="TResult"> The type of the result to be returned after broadcasting the notification.</typeparam>
public interface ILinkableEventBroadcastable<in TNotification, out TResult>
{
    /// <summary>
    /// Broadcast the notification and return the result.
    /// </summary>
    /// <param name="notification">The notification to be broadcasted.</param>
    /// <returns>The final result after broadcasting the notification.</returns>
    TResult Broadcast(TNotification notification);
}