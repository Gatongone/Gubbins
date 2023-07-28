using UnityEngine.Networking;

namespace Gubbins.Web;

public static class UnityWebRequestAsyncOperationExtension
{
    public static HttpResult GetAwaiter(this UnityWebRequestAsyncOperation operation) => new HttpResult(operation);
}
