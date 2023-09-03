using System.Text;

namespace Gubbins.Network;

public static class HttpExtensions
{
    #region HttpContext_EX

    public static HttpContext WithBody(this HttpContext request, string body) => request.WithBody(body, Encoding.UTF8);

    public static HttpContext WithContent(this HttpContext request, HttpContentType content)
    {
        return request.WithHeader("Content_Type", content);
    }

    public static HttpContext WithQueries(this HttpContext request, IReadOnlyDictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithQueries(this HttpContext request, IDictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithQueries(this HttpContext request, IEnumerable<(string Key, object Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithQueries(this HttpContext request, params (string Key, object Value)[] maps) => WithQueries(request, (IEnumerable<(string Key, object Value)>) maps);

    public static HttpContext WithHeaders(this HttpContext request, IReadOnlyDictionary<string, string> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithHeaders(this HttpContext request, IDictionary<string, string> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithHeaders(this HttpContext request, IEnumerable<(string Key, string Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    public static HttpContext WithHeaders(this HttpContext request, params (string Key, string Value)[] maps) => WithHeaders(request, (IEnumerable<(string Key, string Value)>) maps);

    #endregion
}