using Gubbins.Context;

namespace Gubbins.Pipeline;

/// <summary>
/// When Gubbins.Generator was imported, it would generate an implementation of this interface for each method marked with the <see cref="EventAttribute"/>.
/// The generated implementation would be responsible for subscribing to the specified event bus and invoking the decorated method whenever the corresponding event is published.
/// </summary>
public interface IEventListener
{
    /// <summary>
    /// Listens for events on the specified event bus and registers the decorated method as a handler for those events.
    /// </summary>
    /// <param name="resolver">Resolver used to resolve dependencies required by the event listener, such as the event bus instance.</param>
    /// <param name="registry">Registry used to register the event listener's dependencies, if necessary.</param>
    void Listen(IDependenciesResolver resolver, IDependenciesRegistry registry);

    /// <summary>
    /// Unsubscribes the decorated method from the event bus, effectively stopping it from receiving events.
    /// This method is typically called when the event listener is being disposed or when it needs to stop listening for events for any reason.
    /// </summary>
    /// <param name="resolver">Resolver used to resolve dependencies required to unsubscribe the event listener, such as the event bus instance.</param>
    void Clear(IDependenciesResolver resolver);
}