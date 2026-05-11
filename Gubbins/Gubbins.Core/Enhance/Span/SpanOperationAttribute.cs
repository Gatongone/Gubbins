namespace Gubbins.Enhance;

/// <summary>
/// Marks a struct component and declares which span operation interfaces should be generated.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class SpanOperationAttribute : Attribute
{
    public SpanOperationAttribute(params Type[] operations) { }

    // Backward-compatible overload for existing call sites.
    public SpanOperationAttribute(Type type, string member, string method, string overwrite)
    {
        Member    = member;
        Method    = method;
        Overwrite = overwrite;
    }

    // Optional: bind listed operations to a specific component member.
    public string Member { get; set; } = string.Empty;

    // Optional: rename a specific generated extension method for listed operations.
    public string Method { get; set; } = string.Empty;
    public string Overwrite { get; set; } = string.Empty;
}
