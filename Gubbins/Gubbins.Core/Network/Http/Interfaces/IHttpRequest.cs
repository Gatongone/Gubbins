namespace Gubbins.Network;

public interface IHttpRequest
{
    bool IsClosed { get; }
    Task<HttpResponse> SendAsync();
}