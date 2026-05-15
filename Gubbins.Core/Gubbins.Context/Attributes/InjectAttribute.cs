namespace Gubbins.Context;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
    public string? Key;

    public InjectAttribute(string key) => Key = key;
    public InjectAttribute() => Key = null;
}