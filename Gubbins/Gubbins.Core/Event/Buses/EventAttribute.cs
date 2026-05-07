namespace Gubbins.Event;

[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute(Type busType) : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute<TEvent> : Attribute;