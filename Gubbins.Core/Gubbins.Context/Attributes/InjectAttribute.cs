namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Parameter)]
public class InjectAttribute : Attribute
{
    public string? Key;

    public InjectAttribute(string key) => Key = key;
    public InjectAttribute() => Key = null;
}