using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace Gubbins.Web;

public readonly struct HttpResult : INotifyCompletion
{
    private readonly UnityWebRequest m_Request;
    private readonly UnityWebRequestAsyncOperation m_Operation;
    public bool IsCompleted => m_Operation.isDone;

    public HttpResult(UnityWebRequestAsyncOperation operation)
    {
        m_Request = operation.webRequest;
        m_Operation = operation;
    }

    public string GetResult() => m_Request.downloadHandler?.text;

    public void OnCompleted(Action continuation) => m_Operation.completed += _ => continuation.Invoke();
}
