using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Gubbins.Network
{
    public class UnityHttpRequest : IHttpRequest
    {
        public bool IsDisposed { get; set; }
        private bool m_IsClosed;
        private HttpContext m_Context;
        private readonly UnityWebRequest m_Request;

        public UnityHttpRequest(HttpContext context)
        {
            m_Context = context;
            m_Request = new UnityWebRequest(context.Uri) {downloadHandler = new DownloadHandlerBuffer()};

            foreach (var header in context.Headers)
            {
                if (m_Request.GetRequestHeader(header.Key) == null)
                    m_Request.SetRequestHeader(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(context.Body))
            {
                if (string.IsNullOrEmpty(context.ContentType))
                    m_Request.uploadHandler.contentType = context.ContentType;
                m_Request.uploadHandler = new UploadHandlerRaw(context.Encoding.GetBytes(context.Body));
            }
        }

        public async Task<HttpResponse> SendAsync()
        {
            Assert.IsFalse(m_IsClosed, "The HTTP request is closed, please create a new instance.");
            var downloadHandler = await m_Request.SendWebRequest();
            var response = new HttpResponse(downloadHandler.data, m_Request.responseCode, m_Request.error);
            m_IsClosed = true;
            return response;
        }
        
        /// <summary>
        /// Dispose request.
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        private void Cleanup()
        {
            if (IsDisposed) return;
            m_Context.Dispose();
            m_Request.Dispose();
            m_IsClosed = true;
            IsDisposed = true;

        }
        ~UnityHttpRequest()
        {
            Cleanup();
        }

    }

    internal readonly struct UnityHttpResult : INotifyCompletion
    {
        private readonly UnityWebRequest m_Request;
        private readonly UnityWebRequestAsyncOperation m_Operation;
        internal bool IsCompleted => m_Operation.isDone;

        internal UnityHttpResult(UnityWebRequestAsyncOperation operation)
        {
            m_Request = operation.webRequest;
            m_Operation = operation;
        }

        internal DownloadHandler GetResult() => m_Request.downloadHandler;
        void INotifyCompletion.OnCompleted(Action continuation) => m_Operation.completed += _ => continuation.Invoke();
    }


    internal static class UnityWebRequestExtensions
    {
        internal static UnityHttpResult GetAwaiter(this UnityWebRequestAsyncOperation operation) => new(operation);
    }
}