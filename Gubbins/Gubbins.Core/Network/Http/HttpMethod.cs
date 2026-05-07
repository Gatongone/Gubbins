namespace Gubbins.Network;

/// <summary>
/// Http Methods.
/// </summary>
public readonly struct HttpMethod(string method) : IEquatable<HttpMethod>
{
    private readonly string m_Method = method.ToUpper();

    public static implicit operator string(HttpMethod method) => method.m_Method;
    public static implicit operator HttpMethod(string method) => new(method);
    public static HttpMethod Get       => new("GET");
    public static HttpMethod Post      => new("POST");
    public static HttpMethod Put       => new("Put");
    public static HttpMethod Delete    => new("DELETE");
    public static HttpMethod Head      => new("HEAD");
    public static HttpMethod Options   => new("OPTIONS");
    public static HttpMethod Trace     => new("TRACE");
    public static HttpMethod Patch     => new("PATCH");
    public static HttpMethod Undefined => new("UNDEFINED");

    public bool Equals(HttpMethod other) => m_Method == other.m_Method;
    public override bool Equals(object? obj) => obj is HttpMethod other && Equals(other);
    public override int GetHashCode() => m_Method.GetHashCode();
    public override string ToString() => m_Method;
}