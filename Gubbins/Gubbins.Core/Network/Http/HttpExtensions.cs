using System.Text;

namespace Gubbins.Network;

/// <summary>
/// Provides extension methods for the HttpContext class.
/// </summary>
public static class HttpExtensions
{
    /// <param name="request">The HttpContext instance.</param>
    extension(HttpContext request)
    {
    /// <summary>
    /// Sets the body of the HttpContext with the specified content.
    /// </summary>
    /// <param name="body">The body content to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithBody(string body)
        => request.WithBody(body, Encoding.UTF8);

    /// <summary>
    /// Sets the content type of the HttpContext.
    /// </summary>
    /// <param name="content">The content type to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithContent(HttpContentType content)
        => request.WithHeader("Content_Type", content);

    /// <summary>
    /// Sets the query parameters of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="maps">The dictionary containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithQueries(IReadOnlyDictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the query parameters of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="maps">The dictionary containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithQueries(IDictionary<string, object> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the query parameters of the HttpContext using the provided collection of key-value pairs.
    /// </summary>
    /// <param name="maps">The collection of key-value pairs containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithQueries(IEnumerable<(string Key, object Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithQuery(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the query parameters of the HttpContext using the provided collection of key-value pairs.
    /// </summary>
    /// <param name="maps">The collection of key-value pairs containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithQueries(params (string Key, object Value)[] maps) => request.WithQueries((IEnumerable<(string Key, object Value)>) maps);

    /// <summary>
    /// Sets the headers of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="maps">The dictionary containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithHeaders(IReadOnlyDictionary<string, string> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the headers of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="maps">The dictionary containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithHeaders(IDictionary<string, string> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the headers of the HttpContext using the provided collection of key-value pairs.
    /// </summary>
    /// <param name="maps">The collection of key-value pairs containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithHeaders(IEnumerable<(string Key, string Value)> maps)
    {
        foreach (var kv in maps)
        {
            if (kv.Value != null)
                request.WithHeader(kv.Key, kv.Value);
        }

        return request;
    }

    /// <summary>
    /// Sets the headers of the HttpContext using the provided collection of key-value pairs.
    /// </summary>
    /// <param name="maps">The collection of key-value pairs containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public HttpContext WithHeaders(params (string Key, string Value)[] maps) => request.WithHeaders((IEnumerable<(string Key, string Value)>) maps);
    }
}