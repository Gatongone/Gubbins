/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-02:39:39
 * Github: https://github.com/Gatongone
 * Description: HttpContext extensions
 */

using System.Text;

namespace Gubbins.Network;

/// <summary>
/// Provides extension methods for the HttpContext class.
/// </summary>
public static class HttpExtensions
{
    /// <summary>
    /// Sets the body of the HttpContext with the specified content.
    /// </summary>
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="body">The body content to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithBody(this HttpContext request, string body) 
        => request.WithBody(body, Encoding.UTF8);

    /// <summary>
    /// Sets the content type of the HttpContext.
    /// </summary>
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="content">The content type to set.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithContent(this HttpContext request, HttpContentType content)
        => request.WithHeader("Content_Type", content);
    
    /// <summary>
    /// Sets the query parameters of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The dictionary containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithQueries(this HttpContext request, IReadOnlyDictionary<string, object> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The dictionary containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithQueries(this HttpContext request, IDictionary<string, object> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The collection of key-value pairs containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithQueries(this HttpContext request, IEnumerable<(string Key, object Value)> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The collection of key-value pairs containing the query parameters.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithQueries(this HttpContext request, params (string Key, object Value)[] maps) => WithQueries(request, (IEnumerable<(string Key, object Value)>) maps);
    
    /// <summary>
    /// Sets the headers of the HttpContext using the provided dictionary.
    /// </summary>
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The dictionary containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithHeaders(this HttpContext request, IReadOnlyDictionary<string, string> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The dictionary containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithHeaders(this HttpContext request, IDictionary<string, string> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The collection of key-value pairs containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithHeaders(this HttpContext request, IEnumerable<(string Key, string Value)> maps)
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
    /// <param name="request">The HttpContext instance.</param>
    /// <param name="maps">The collection of key-value pairs containing the headers.</param>
    /// <returns>The updated HttpContext instance.</returns>
    public static HttpContext WithHeaders(this HttpContext request, params (string Key, string Value)[] maps) => WithHeaders(request, (IEnumerable<(string Key, string Value)>) maps);
}