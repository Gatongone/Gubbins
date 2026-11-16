namespace Gubbins.Events;

/// <summary>
///  A mediator that transfers a message from a sender to a receiver.
/// </summary>
public interface IEvent : IEventSubscribable, IEventBroadcastable;

/// <summary>
///  A mediator that transfers a message from a sender to a receiver.
/// </summary>
/// <typeparam name="TNotification">Event parameters.</typeparam>
public interface IEvent<TNotification> : IEventSubscribable<TNotification>, IEventBroadcastable<TNotification>;
