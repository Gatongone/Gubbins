/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-01:44:01
 * Github: https://github.com/Gatongone
 * Description: Http methods.
 */

namespace Gubbins.Network;

/// <summary>
/// Http Methods.
/// </summary>
public readonly struct HttpMethod : IEquatable<HttpMethod>
{
    private readonly string m_Method;

    public HttpMethod(string? method) => m_Method = method.ToUpper();
    public static implicit operator string(HttpMethod method) => method.m_Method;
    public static implicit operator HttpMethod(string? method) => new(method);
    public static HttpMethod Get => new HttpMethod("GET");
    public static HttpMethod Post => new HttpMethod("POST");
    public static HttpMethod Put => new HttpMethod("Put");
    public static HttpMethod Delete => new HttpMethod("DELETE");
    public static HttpMethod Head => new HttpMethod("HEAD");
    public static HttpMethod Options => new HttpMethod("OPTIONS");
    public static HttpMethod Trace => new HttpMethod("TRACE");
    public static HttpMethod Patch => new HttpMethod("PATCH");
    public static HttpMethod Undefined => new HttpMethod("UNDEFINED");

    public bool Equals(HttpMethod other) => m_Method == other.m_Method;
    public override bool Equals(object? obj) => obj is HttpMethod other && Equals(other);
    public override int GetHashCode() => m_Method.GetHashCode();
    public override string ToString() => m_Method;
}
