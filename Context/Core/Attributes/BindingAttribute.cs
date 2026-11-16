namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Class)]
public class BindingAttribute(params Type[] baseTypes) : Attribute;