using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Gubbins.Web;

public class HttpRequest
{
    private HttpMethod m_Method = HttpMethod.Get;
    private string m_Url;
    private readonly Dictionary<string, string> m_Headers = new();
    private readonly Dictionary<string, object> m_Query = new();
    private readonly Dictionary<string, object> m_Body = new();

    private HttpRequest(string url) => m_Url = url;
#if UNITY
    public async Task<string> SendAsync()
    {
        var url = m_Method != HttpMethod.Get ? m_Url : m_Url + "?" + Encoding.UTF8.GetString(UnityWebRequest.SerializeSimpleForm(m_Query.ToDictionary(kv => kv.Key, kv => kv.Value.ToString())));
        var uri = new Uri(url);
        var request = new UnityWebRequest(uri) {downloadHandler = new DownloadHandlerBuffer()};
        request.method = m_Method.ToString().ToUpper();

        foreach (var header in m_Headers)
        {
            request.SetRequestHeader(header.Key, header.Value);
        }

        if (m_Body.Count > 0 && m_Method != HttpMethod.Get)
        {
            var body = JsonConvert.SerializeObject(m_Body);
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        }

        return await request.SendWebRequest();
    }
#else
    public async Task<string> SendAsync()
    {
        var url = m_Query.Count <= 0
            ? m_Url
            : m_Url + "?" + Encoding.UTF8.GetString(
                UnityWebRequest.SerializeSimpleForm(
                    m_Query.ToDictionary(kv =>
                        kv.Key, kv => kv.Value.ToString())));

        var uri = new Uri(url);
        var request = (HttpWebRequest) WebRequest.Create(uri);
        request.Method = m_Method.ToString().ToUpper();
        Console.WriteLine(url);

        foreach (var header in m_Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        if (m_Body.Count > 0 && m_Method != HttpMethod.Get)
        {
            var body = JsonConvert.SerializeObject(m_Body);
            using var dataStream = new StreamWriter(request.GetRequestStream());
            await dataStream.WriteAsync(body);
            dataStream.Close();
        }

        var response = (HttpWebResponse) await request.GetResponseAsync();
        var encoding = response.ContentEncoding;
        if (string.IsNullOrEmpty(encoding) || encoding.Length < 1)
        {
            encoding = "UTF-8";
        }

        var stream = response.GetResponseStream();
        if (stream == null) return null;

        var reader = new StreamReader(stream, Encoding.GetEncoding(encoding));
        return await reader.ReadToEndAsync();
    }
#endif

    public async Task<T> SendAsync<T>()
    {
        var result = await SendAsync();
        return JsonConvert.DeserializeObject<T>(result);
    }

    public HttpRequest WithContent(HttpContentType contentType, string key = "Content_Type")
    {
        m_Headers.Add(key, contentType.content);
        return this;
    }

    public HttpRequest WithPath(params string[] filePaths)
    {
        if (filePaths.Length <= 0)
            return this;
        if (m_Url.EndsWith("/"))
            m_Url += $"{string.Join("/", filePaths)}";
        else
            m_Url += $"/{string.Join("/", filePaths)}";
        return this;
    }

    public HttpRequest WithQuery(string name, [CanBeNull] object value)
    {
        if (value != null)
            m_Query.Add(name, value);
        return this;
    }

    public HttpRequest WithBody(string name, object value)
    {
        m_Body.Add(name, value);
        return this;
    }

    public HttpRequest WithHeader(string name, string value)
    {
        m_Headers.Add(name, value);
        return this;
    }

    public HttpRequest WithMethod(HttpMethod method)
    {
        m_Method = method;
        return this;
    }

    internal static HttpRequest CreateRequest(string url) => new HttpRequest(url);
}

public static class HttpRequestExtensions
{
    public static HttpRequest WithQuery(this HttpRequest request, Dictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpRequest WithQuery(this HttpRequest request, (string Key, object Value)[] maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpRequest WithBody(this HttpRequest request, Dictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithBody(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpRequest WithBody(this HttpRequest request, IEnumerable<(string Key, object Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithBody(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpRequest WithHeader(this HttpRequest request, Dictionary<string, string> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpRequest WithHeader(this HttpRequest request, IEnumerable<(string Key, string Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }
}