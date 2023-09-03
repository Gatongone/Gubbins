using System.Text;
using System.Text.RegularExpressions;
using Gubbins.Structure;
using Gubbins.Resources;

namespace Gubbins.Network;

public struct HttpContext : IDisposable
{
    private const string DEFAULT_HTTP_VERSION = "1.1";

    private string? m_Url;
    private volatile bool m_IsDisposed;
    private HttpResponse m_Response;
    private readonly HttpMessageCache m_MessageCache;

    public string? Body { get; private set; }
    public Version Version { get; private set; } = new(DEFAULT_HTTP_VERSION);
    public HttpMethod Method { get; private set; } = HttpMethod.Undefined;
    public Encoding Encoding { get; private set; } = Encoding.UTF8;
    public IReadOnlyDictionary<string, string> Headers =>  m_MessageCache.Headers;
    public IReadOnlyDictionary<string, object> Queries =>  m_MessageCache.Queries;
    public bool IsResponse => Method.Equals(HttpMethod.Undefined);
    public string? ContentType =>  m_MessageCache.Headers.TryGetValue("Content-Type", out var type) ? type : null;
    public bool IsDisposed => m_IsDisposed;

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

    public readonly void Dispose()
    {
        if (m_IsDisposed) return;
         m_MessageCache.Clear();
         InternalSingleton.InstanceOf<InternalObjectPool<HttpMessageCache>>().Recycle(m_MessageCache);
         m_IsDisposed = true;
    }

    public static HttpContext CreateFromString(string httpString)
    {
        var lines = httpString.Split('\n');

        string?[] requestLine = lines[0].Split(' ');
        var isResponse = requestLine[0].Contains("HTTP");

        var context = new HttpContext();

        // Parse response
        if (isResponse)
        {
            context.Version = new Version(Regex.Match(requestLine[0], @"(?<=/).+").ToString());
            context.m_Response = new HttpResponse(long.Parse(requestLine[1]), requestLine[2]);
        }
        // Parse request
        else
        {
            context.Method = requestLine[0];
            context.m_Url = requestLine[1];
            context.Version = new Version(Regex.Match(requestLine[2], @"(?<=/).+").ToString());

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

    public HttpContext WithUrl(string? url)
    {
        m_Url = url;
        return this;
    }

    public HttpContext WithPath(params string[] filePaths)
    {
        if (filePaths.Length <= 0)
            return this;
        if (m_Url.EndsWith("/"))
            m_Url += $"{string.Join("/", filePaths)}";
        else
            m_Url += $"/{string.Join("/", filePaths)}";
        return this;
    }

    public HttpContext WithQuery(string name, object value)
    {
        if (value != null)
             m_MessageCache.Queries.Add(name, value);
        return this;
    }

    public HttpContext WithHeader(string name, string value)
    {
         m_MessageCache.Headers.Add(name, value);
        return this;
    }

    public HttpContext WithMethod(HttpMethod method)
    {
        Method = method;
        return this;
    }

    public HttpContext WithBody(string? body, Encoding encoding)
    {
        Encoding = encoding;
        Body = body;
        return this;
    }

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
        public Dictionary<string, string> Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object> Queries= new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
        public static HttpResponse None = new HttpResponse(0, null);

        public HttpResponse(long code, string? message) => (Code, Message) = (code, message);

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