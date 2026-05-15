namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute(Type busType) : Attribute;

[AttributeUsage(AttributeTargets.Method)]
public class EventAttribute<TEvent> : Attribute;
