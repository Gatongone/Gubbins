namespace Gubbins.Network;

public interface IHttpRequest : IDisposable
{
    bool IsDisposed { get; }
    Task<HttpResponse> SendAsync();
}