using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace Gubbins.Network;

public class NetHttpRequest : IHttpRequest
{
    public bool IsDisposed { get; private set; }
    private HttpResponse m_Response;
    private readonly HttpContext m_Context;

#if NET6_0_OR_GREATER
    private readonly HttpClient m_Request;
    private readonly HttpRequestMessage m_Message;

    public NetHttpRequest(HttpContext context, Uri host = null)
    {
        m_Context = context;
        var uri = context.Uri;
        Debug.Assert(uri != null);

        if (host != null && host != uri)
        {
            context.WithConnection(HttpConnectionType.KeepAlive, true);
            m_Request = HttpClientFactory.Spawn(host);
        }
        else
            m_Request = HttpClientFactory.Spawn();

        m_Message = new HttpRequestMessage(new System.Net.Http.HttpMethod(context.Method), uri)
        {
            Version = context.Version
        };

        // Add headers
        if (context.Headers.Count > 0)
        {
            foreach (var header in context.Headers)
            {
                m_Message.Headers.Add(header.Key, header.Value);
            }
        }

        // Add body
        if (!string.IsNullOrEmpty(context.Body))
            m_Message.Content = new StringContent(context.Body, context.Encoding);
    }

    public async Task<HttpResponse> SendAsync()
    {
        var responseMsg = await m_Request.SendAsync(m_Message);
        m_Response = new HttpResponse(await responseMsg.Content.ReadAsByteArrayAsync(), (long) responseMsg.StatusCode);
        return m_Response;
    }

    private static class HttpClientFactory
    {
        private static readonly HttpClient s_SharedClient = new();
        private static readonly ConcurrentDictionary<Uri, HttpClient> s_ClientMaps = new();

        public static HttpClient Spawn(Uri proxyHost = null)
        {
            if (proxyHost == null) return s_SharedClient;
            
            if (s_ClientMaps.TryGetValue(proxyHost, out var client))
                return client;
            var proxy = new WebProxy(proxyHost);
            var handler = new HttpClientHandler {Proxy = proxy, UseProxy = true};
            client = new HttpClient(handler);
            s_ClientMaps.TryAdd(proxyHost, client);
            return client;

        }
    }
#else
    private readonly HttpWebRequest m_Request;

    public NetHttpRequest(HttpContext context, Uri proxyHost = null)
    {
        m_Context = context;
        m_Request = (HttpWebRequest) WebRequest.Create(context.Uri);
        if (proxyHost != null && proxyHost != context.Uri)
        {
            m_Request.Proxy = new WebProxy(proxyHost);
        }
        
        m_Request.Method = context.Method;
        
        // Add headers
        foreach (var header in context.Headers)
        {
            m_Request.Headers.Add(header.Key, header.Value);
        }
        
        context.Dispose();       
    }

    public async Task<HttpResponse> SendAsync()
    {
        // Add body
        if (!string.IsNullOrEmpty(m_Context.Body))
        {
            using var dataStream = new StreamWriter(m_Request.GetRequestStream(), m_Context.Encoding);
            await dataStream.WriteAsync(m_Context.Body);
            dataStream.Close();
        }
        
        var response = (HttpWebResponse) await m_Request.GetResponseAsync();
        var stream = response.GetResponseStream();
        Debug.Assert(stream != null);
        m_Response = new HttpResponse(stream, (long) response.StatusCode);
        return m_Response;
    }
#endif

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (IsDisposed) return;
        if (disposing)
        {
#if NET6_0_OR_GREATER
            m_Message.Dispose();
#endif
        }
        m_Context.Dispose();
        m_Response.Dispose();
        IsDisposed = true;
    }

    ~NetHttpRequest()
    {
        Dispose(false);
    }
}