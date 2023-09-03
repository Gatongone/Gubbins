/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-21:23:42
 * Github: https://github.com/Gatongone
 * Description: Http info context.
 */

using System.Text;
using System.Text.RegularExpressions;
using Gubbins.Structure;
using Gubbins.Resources;

namespace Gubbins.Network;

/// <summary>
/// Context that encapsulates an HTTP context, including request or response information.
/// </summary>
public struct HttpContext : IDisposable
{
    private const string DEFAULT_HTTP_VERSION = "1.1";

    private string? m_Url;
    private volatile bool m_IsDisposed;
    private HttpResponse m_Response;
    private readonly HttpMessageCache m_MessageCache;
    
    /// <summary>
    ///  Gets or sets the body of the HTTP context.
    /// </summary>
    public string? Body { get; private set; }
    
    /// <summary>
    ///  Gets or sets the version of the HTTP context.
    /// </summary>
    public Version Version { get; private set; } = new(DEFAULT_HTTP_VERSION);
    
    /// <summary>
    /// Gets or sets the HTTP method of the HTTP context.
    /// </summary>
    public HttpMethod Method { get; private set; } = HttpMethod.Undefined;
    
    /// <summary>
    /// Gets or sets the encoding of the HTTP context.
    /// </summary>
    public Encoding Encoding { get; private set; } = Encoding.UTF8;
    
    /// <summary>
    ///  Gets or sets the encoding of the HTTP context.
    /// </summary>
    public IReadOnlyDictionary<string, string> Headers =>  m_MessageCache.Headers;
    
    /// <summary>
    /// Gets the read-only dictionary of queries in the HTTP context.
    /// </summary>
    public IReadOnlyDictionary<string, object> Queries =>  m_MessageCache.Queries;
    
    /// <summary>
    ///  Gets a value indicating whether the HTTP context represents a response.
    /// </summary>
    public bool IsResponse => Method.Equals(HttpMethod.Undefined);
    
    /// <summary>
    /// Gets the content type of the HTTP context.
    /// </summary>
    public string? ContentType =>  m_MessageCache.Headers.TryGetValue("Content-Type", out var type) ? type : null;
    
    /// <summary>
    /// Is the HTTP Context is disposed, if true, the queries and headers will be cleared.
    /// </summary>
    public bool IsDisposed => m_IsDisposed;
    
    /// <summary>
    /// Gets the URI of the HTTP context.
    /// </summary>
    public Uri? Uri
    {
        get
        {
            var url = m_MessageCache.Queries.Count <= 0
                ? m_Url
                : m_Url + "?" + string.Join("&",  m_MessageCache.Queries.Select(kv => $"{kv.Key}={kv.Value}"));
            return url != null ? new Uri(url) : null;
        }
    }

    public HttpContext()
    {
        m_MessageCache = InternalSingleton.InstanceOf<InternalObjectPool<HttpMessageCache>>().Spawn();
    }

    public void Dispose()
    {
        if (m_IsDisposed) return;
         m_MessageCache.Clear();
         InternalSingleton.InstanceOf<InternalObjectPool<HttpMessageCache>>().Recycle(m_MessageCache);
         m_IsDisposed = true;
    }
    
    /// <summary>
    /// Creates an HttpContext instance from the provided HTTP string.
    /// </summary>
    /// <param name="httpString">The HTTP string to parse.</param>
    /// <returns>An HttpContext instance representing the parsed HTTP string.</returns>
    public static HttpContext CreateFromString(string httpString)
    {
        var lines = httpString.Split('\n');

        string[] requestLine = lines[0].Split(' ');
        var isResponse = requestLine[0].Contains("HTTP");

        var context = new HttpContext();

        // Parse response
        if (isResponse)
        {
            context.Version = new Version(Regex.Match(requestLine.ElementAtOrDefault(0) ?? string.Empty, @"(?<=/).+").ToString());
            context.m_Response = new HttpResponse(long.Parse(requestLine.ElementAtOrDefault(1) ?? string.Empty), requestLine[2]);
        }
        // Parse request
        else
        {
            context.Method = requestLine.ElementAtOrDefault(0);
            context.m_Url = requestLine.ElementAtOrDefault(1);
            context.Version = new Version(Regex.Match(requestLine.ElementAtOrDefault(2) ?? string.Empty, @"(?<=/).+").ToString());

            // Parse queries
            var urlParts = context.m_Url.Split('?');
            if (urlParts.Length > 1)
            {
                var query = urlParts[1];
                foreach (var param in query.Split('&'))
                {
                    var kv = param.Split('=');
                    context. m_MessageCache.Queries[kv[0]] = kv[1];
                }
            }
        }

        // Parse headers
        for (var i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                break;

            var header = Regex.Match(lines[i], @"(.+(?=: )): (.+(?=\s))");
            context. m_MessageCache.Headers[header.Groups[1].Value] = header.Groups[2].Value;
        }

        // Parse body
        context.Body = string.Join("\n", lines.Skip(lines.Length - 1));

        // Set the url from headers if empty
        if (string.IsNullOrEmpty(context.m_Url) && context.Headers.TryGetValue("host", out var url))
        {
            context.m_Url = url;
        }

        return context;
    }

    public HttpContext WithResponse(long httpCode, string? message)
    {
        m_Response = new HttpResponse(httpCode, message);
        return this;
    }

    public HttpContext WithVersion(Version version)
    {
        Version = version;
        return this;
    }

     /// <summary>
    /// Sets the URL of the HttpContext.
    /// </summary>
    /// <param name="url">The URL to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithUrl(string? url)
    {
        m_Url = url;
        return this;
    }
    
    /// <summary>
    /// Sets the path of the HttpContext.
    /// </summary>
    /// <param name="filePaths">The file paths to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithPath(params string[] filePaths)
    {
        if (filePaths.Length <= 0)
            return this;
        m_Url ??= "/";
        if (m_Url.EndsWith("/"))
            m_Url += $"{string.Join("/", filePaths)}";
        else
            m_Url += $"/{string.Join("/", filePaths)}";
        return this;
    }
    
    /// <summary>
    /// Sets the query of the HttpContext.
    /// </summary>
    /// <param name="name">The name of the query.</param>
    /// <param name="value">The value of the query.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithQuery(string name, object value)
    {
        if (value != null)
             m_MessageCache.Queries.Add(name, value);
        return this;
    }

    /// <summary>
    /// Sets the header of the HttpContext.
    /// </summary>
    /// <param name="name">The name of the header.</param>
    /// <param name="value">The value of the header.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithHeader(string name, string value)
    {
         m_MessageCache.Headers.Add(name, value);
        return this;
    }

    /// <summary>
    /// Sets the HTTP method of the HttpContext.
    /// </summary>
    /// <param name="method">The HTTP method to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithMethod(HttpMethod method)
    {
        Method = method;
        return this;
    }

    /// <summary>
    /// Sets the body and encoding of the HttpContext.
    /// </summary>
    /// <param name="body">The body to set.</param>
    /// <param name="encoding">The encoding to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithBody(string? body, Encoding encoding)
    {
        Encoding = encoding;
        Body = body;
        return this;
    }

    /// <summary>
    /// Sets the connection type of the HttpContext.
    /// </summary>
    /// <param name="connectionType">The connection type to set.</param>
    /// <param name="isProxy">A flag indicating whether the connection is a proxy connection.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithConnection(HttpConnectionType connectionType, bool isProxy = false)
    {
        if (isProxy)
        {
             m_MessageCache.Headers.Remove("Connection");
             m_MessageCache.Headers.Add("Proxy-Connection", connectionType);
        }
        else
        {
             m_MessageCache.Headers.Remove("Proxy-Connection");
             m_MessageCache.Headers.Add("Connection", connectionType);
        }

        return this;
    }
    
    /// <summary>
    /// Returns a string representation of the HttpContext.
    /// </summary>
    /// <returns>A string representation of the HttpContext.</returns>
    public override string ToString()
    {
        var body = Body;
        var path =  m_MessageCache.Headers.ContainsKey("Proxy-Connection") ||  m_MessageCache.Headers.ContainsKey("proxy-connection") ? m_Url : IsResponse ? string.Empty : " / ";
        var queryInfo =  m_MessageCache.Queries.Count <= 0 ? path : $" /?{string.Join("&",  m_MessageCache.Queries.Select(x => $"{x.Key}={x.Value}"))} ";
        var hostInfo = string.IsNullOrEmpty(m_Url) ? string.Empty : $"Host: {Uri.Host}\n";
        var headerInfo = string.Join("\n",  m_MessageCache.Headers.Select(x => $"{x.Key}: {x.Value}"));
        var methodInfo = IsResponse ? string.Empty : Method.ToString();
        var bodyInfo = string.IsNullOrEmpty(body) ? string.Empty : $"\n{body}";
        var responseInfo = m_Response.Equals(HttpResponse.None) ? string.Empty : $" {m_Response.Code} {m_Response.Message}";
        return $"{methodInfo}{queryInfo}HTTP/{Version}{responseInfo}\n{hostInfo}{headerInfo}{bodyInfo}";
    }

    private class HttpMessageCache
    {
        public readonly Dictionary<string, string> Headers = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, object> Queries= new(StringComparer.OrdinalIgnoreCase);

        public void Clear()
        {
            Headers.Clear();
            Queries.Clear();
        }
    }

    private readonly struct HttpResponse : IEquatable<HttpResponse>
    {
        public readonly long Code;
        public readonly string Message;
        public static readonly HttpResponse None = new(0, string.Empty);

        public HttpResponse(long code, string message) => (Code, Message) = (code, message);

        public bool Equals(HttpResponse other) => Code == other.Code && Message == other.Message;

        public override bool Equals(object? obj) => obj is HttpResponse other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Code.GetHashCode() * 397) ^ Message.GetHashCode();
            }
        }
    }
}