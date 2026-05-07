namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Class)]
public class DependencyAttribute(string key = "") : Attribute
{
    public readonly string Key = key;
}