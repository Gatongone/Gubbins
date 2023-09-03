/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/17-04:39:52
 * Github: https://github.com/Gatongone
 * Description: Http connection type.
 */

namespace Gubbins.Network;

/// <summary>
/// Http connection type.
/// </summary>
public struct HttpConnectionType : IEquatable<HttpConnectionType>
{
    private readonly string m_Connection;

    public HttpConnectionType(string connectionType) => m_Connection = connectionType;
    public static implicit operator string(HttpConnectionType connectionType) => connectionType.m_Connection;
    public static implicit operator HttpConnectionType(string connectionType) => new(connectionType);
    public override string ToString() => m_Connection;
    public static HttpConnectionType KeepAlive => new("Keep-Alive");
    public static HttpConnectionType Close => new("Close");

    public bool Equals(HttpConnectionType other) => m_Connection == other.m_Connection;
    public override bool Equals(object? obj) => obj is HttpConnectionType other && Equals(other);
    public override int GetHashCode() => m_Connection.GetHashCode();
}