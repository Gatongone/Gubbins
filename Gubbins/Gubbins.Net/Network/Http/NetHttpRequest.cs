using System.Diagnostics;
using System.Net;

namespace Gubbins.Network;

public class NetHttpRequest : IHttpRequest
{
    public bool IsClosed { get; set; }
#if NET6_0_OR_GREATER
    private readonly HttpClient m_Request;
    private readonly HttpRequestMessage m_Message;

    public NetHttpRequest(HttpContext context, Uri host = null)
    {
        var uri = context.Uri;
        Debug.Assert(uri != null);

        if (host != null && host != uri)
        {
            context.WithConnection(HttpConnectionType.KeepAlive, true);

            var proxy = new WebProxy(host);
            var handler = new HttpClientHandler {Proxy = proxy, UseProxy = true};
            m_Request = new HttpClient(handler);
        }
        else
            m_Request = new HttpClient();

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
        if(IsClosed) throw new InvalidOperationException("The HTTP request is closed, please create a new instance.");
        var responseMsg = await m_Request.SendAsync(m_Message);
        var response = new HttpResponse(await responseMsg.Content.ReadAsByteArrayAsync(), (long) responseMsg.StatusCode);
        IsClosed = true;
        return response;
    }
#else
    private readonly HttpWebRequest m_Request;

    public NetHttpRequest(HttpContext context, Uri host = null)
    {
        m_Request = (HttpWebRequest) WebRequest.Create(context.Uri);
        if (host != null && host != context.Uri)
        {
            m_Request.Proxy = new WebProxy(host);
        }
        
        m_Request.Method = context.Method;
        
        // Add headers
        foreach (var header in context.Headers)
        {
            m_Request.Headers.Add(header.Key, header.Value);
        }

        // Add body
        if (!string.IsNullOrEmpty(context.Body))
        {
            using var dataStream = new StreamWriter(m_Request.GetRequestStream(), context.Encoding);
            dataStream.WriteAsync(context.Body);
            dataStream.Close();
        }
        context.Dispose();       
    }

    public async Task<HttpResponse> SendAsync()
    {
        if(IsClosed) throw new InvalidOperationException("The HTTP request is closed, please create a new instance.");
        var response = (HttpWebResponse) await m_Request.GetResponseAsync();
        var stream = response.GetResponseStream();
        Debug.Assert(stream != null);
        IsClosed = true;
        return new HttpResponse(stream, (long) response.StatusCode);
    }
#endif
}