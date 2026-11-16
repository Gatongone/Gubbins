using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Creates or resolves the event buses used by generated event listeners.
/// </summary>
[Duck]
public interface IEventFactory
{
    /// <summary>
    /// Gets or creates the event bus for <typeparamref name="TEvent"/>.
    /// </summary>
    /// <typeparam name="TEvent">The concrete event bus type.</typeparam>
    /// <returns>The resolved or newly created event bus.</returns>
    TEvent RequireEvent<TEvent>() where TEvent : class, new();
}