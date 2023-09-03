/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/08/12-01:33:45
 * Github: https://github.com/Gatongone
 * Description: Http request interface.
 */

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