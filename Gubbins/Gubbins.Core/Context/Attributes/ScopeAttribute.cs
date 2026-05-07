namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute(bool bake = false) : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public class MultitonAttribute(int bakeCount = 0) : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public class CustomAttribute(Type scopeControllerType, bool bake = false) : Attribute;