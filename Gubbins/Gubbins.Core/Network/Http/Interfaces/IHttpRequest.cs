namespace Gubbins.Network;

/// <summary>
/// Http request.
/// </summary>
public interface IHttpRequest : IDisposable
{
    /// <summary>
    /// Is the request is disposed.
    /// If true, it should be create a new instance.
    /// </summary>
    bool IsDisposed { get; }

    /// <summary>
    /// Send request async.
    /// </summary>
    /// <returns>Response task.</returns>
    Task<HttpResponse> SendAsync();
}