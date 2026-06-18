namespace Gubbins.Events;

/// <summary>
/// Linkable event interface that supports both subscription and broadcasting of notifications with results.
/// </summary>
/// <typeparam name="TNotification">The type of notification that the event will handle.</typeparam>
/// <typeparam name="TResult">The type of result that the event will produce when broadcasting.</typeparam> 
public interface ILinkableEvent<TNotification, TResult> : ILinkableEventSubscriable<TNotification, TResult>, ILinkableEventBroadcastable<TNotification, TResult>;