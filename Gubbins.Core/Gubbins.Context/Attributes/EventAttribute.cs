namespace Gubbins.Context;

/// <summary>
/// Indicates that the decorated method is an event handler that should be registered with the specified event bus.
/// </summary>
/// <param name="busType"></param>
[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute(Type busType) : Attribute;

/// <summary>
/// Indicates that the decorated method is an event handler that should be registered with the event bus corresponding to the specified event type.
/// </summary>
/// <typeparam name="TEvent">The type of event that the decorated method should handle. The event bus corresponding to this event type will be used to register the method as an event handler.</typeparam>
[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute<TEvent> : Attribute;