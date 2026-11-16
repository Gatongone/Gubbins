namespace Gubbins.Events;

/// <summary>
/// When Gubbins.Generator was imported, it would generate an implementation of this interface for each method marked with the <see cref="EventAttribute"/>.
/// The generated implementation would be responsible for subscribing to the specified event bus and invoking the decorated method whenever the corresponding event is published.
/// </summary>
public interface IEventListener
{
    /// <summary>
    /// Listens for events on the specified event bus and registers the decorated method as a handler for those events.
    /// </summary>
    /// <param name="context">The factory used to resolve and register the event buses required by the listener.</param>
    void Listen(IEventFactory context);

    /// <summary>
    /// Unsubscribes the decorated method from the event bus, effectively stopping it from receiving events.
    /// This method is typically called when the event listener is being disposed or when it needs to stop listening for events for any reason.
    /// </summary>
    /// <param name="context">The factory used to resolve the event buses required to unsubscribe the listener.</param>
    void Clear(IEventFactory context);
}

