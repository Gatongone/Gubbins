using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Gubbins.Network
{
    /// <summary>
    /// Implements an HTTP request using UnityWebRequest, supporting async and disposal patterns.
    /// </summary>
    public class UnityHttpRequest : IHttpRequest
    {
        /// <summary>
        /// Indicates whether the request has been disposed.
        /// </summary>
        public bool IsDisposed { get; set; }

        /// <summary>
        /// Indicates whether the request is closed and cannot be reused.
        /// </summary>
        private bool m_IsClosed;

        /// <summary>
        /// The HTTP context containing request parameters.
        /// </summary>
        private HttpContext m_Context;

        /// <summary>
        /// The underlying UnityWebRequest instance.
        /// </summary>
        private readonly UnityWebRequest m_Request;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityHttpRequest"/> class.
        /// </summary>
        /// <param name="context">The HTTP context for the request.</param>
        /// <param name="downloadHandler">Optional download handler.</param>
        /// <param name="uploadHandler">Optional upload handler.</param>
        /// <param name="certificateHandler">Optional certificate handler.</param>
        public UnityHttpRequest(
            HttpContext context,
            DownloadHandler downloadHandler = null,
            UploadHandler uploadHandler = null,
            CertificateHandler certificateHandler = null)
        {
            m_Context = context;
            m_Request = new UnityWebRequest(context.Uri) { downloadHandler = downloadHandler ?? new DownloadHandlerBuffer() };

            foreach (var header in context.Headers)
            {
                if (m_Request.GetRequestHeader(header.Key) == null)
                    m_Request.SetRequestHeader(header.Key, header.Value);
            }

            if (certificateHandler != null)
                m_Request.certificateHandler = certificateHandler;

            if (string.IsNullOrEmpty(context.Body) && uploadHandler == null)
                return;

            if (string.IsNullOrEmpty(context.ContentType))
                m_Request.uploadHandler.contentType = context.ContentType;

            m_Request.uploadHandler = uploadHandler ?? new UploadHandlerRaw(context.Encoding.GetBytes(context.Body));
        }

        /// <summary>
        /// Sends the HTTP request asynchronously and returns the response.
        /// </summary>
        /// <returns>The HTTP response.</returns>
        public async Task<HttpResponse> SendAsync()
        {
            Assert.IsFalse(m_IsClosed, "The HTTP request is closed, please create a new instance.");
            await m_Request.SendWebRequest();
            var response = new HttpResponse(m_Request.downloadHandler.data, m_Request.responseCode, m_Request.error);
            m_IsClosed = true;
            return response;
        }

        /// <summary>
        /// Disposes the request and releases resources.
        /// </summary>
        public void Dispose()
        {
            Cleanup();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up resources used by the request.
        /// </summary>
        private void Cleanup()
        {
            if (IsDisposed) return;
            m_Context.Dispose();
            m_Request.Dispose();
            m_IsClosed = true;
            IsDisposed = true;
        }

        /// <summary>
        /// Finalizer to ensure resources are cleaned up.
        /// </summary>
        ~UnityHttpRequest()
        {
            Cleanup();
        }

#if !UNITY_2023_2_OR_NEWER
        /// <summary>
        /// Awaiter struct for UnityWebRequest async operations (for older Unity versions).
        /// </summary>
        internal readonly struct Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            private readonly UnityWebRequestAsyncOperation m_Operation;

            /// <summary>
            /// Gets whether the operation is completed.
            /// </summary>
            internal bool IsCompleted => m_Operation.isDone;

            /// <summary>
            /// Initializes the awaiter.
            /// </summary>
            /// <param name="operation">The UnityWebRequest async operation.</param>
            internal Awaiter(UnityWebRequestAsyncOperation operation) => m_Operation = operation;

            /// <summary>
            /// Gets the result (no value returned).
            /// </summary>
            internal void GetResult() { }

            /// <summary>
            /// Registers a continuation to be invoked when the operation completes.
            /// </summary>
            void System.Runtime.CompilerServices.INotifyCompletion.OnCompleted(Action continuation)
                => m_Operation.completed += _ => continuation.Invoke();
        }
#endif
    }

#if !UNITY_2023_2_OR_NEWER
    /// <summary>
    /// Extension methods for UnityWebRequestAsyncOperation awaiter support (for older Unity versions).
    /// </summary>
    internal static class UnityWebRequestExtensions
    {
        /// <summary>
        /// Gets an awaiter for the UnityWebRequest async operation.
        /// </summary>
        /// <param name="operation">The UnityWebRequest async operation.</param>
        /// <returns>The awaiter struct.</returns>
        internal static UnityHttpRequest.Awaiter GetAwaiter(this UnityWebRequestAsyncOperation operation) => new(operation);
    }
#endif
}