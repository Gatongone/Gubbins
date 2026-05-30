using System;
using System.Collections.Generic;
using Gubbins.Enhance;
using UnityEngine;
using UnityEngine.Events;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides strongly-typed wrappers for Unity Application events.
    /// </summary>
    public static class ApplicationEvents
    {
        /// <summary>
        /// Defines the delegate to use to register for events in which the focus gained or lost.
        /// </summary>
        public class FocusChanged : IEventSubscriable<bool>
        {
            private readonly Dictionary<IEventHandler<bool>, Action<bool>> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<bool> handler)
            {
                Action<bool> wrap = handler.Handle;
                Application.focusChanged += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<bool> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.focusChanged -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.focusChanged -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// Unity raises this event when the Player application is quitting.
        /// </summary>
        public class Quitting : IEventSubscriable<Unit>
        {
            private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<Unit> handler)
            {
                Action wrap = () => handler.Handle(Unit.Instance);
                Application.quitting += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.quitting -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.quitting -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// This event is raised when an application running on Android, iOS, or the Universal Windows Platform (UWP) is activated using a deep link URL.
        /// </summary>
        public class DeepLinkActivated : IEventSubscriable<string>
        {
            private readonly Dictionary<IEventHandler<string>, Action<string>> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<string> handler)
            {
                Action<string> wrap = handler.Handle;
                Application.deepLinkActivated += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<string> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.deepLinkActivated -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.deepLinkActivated -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// Event that is fired if a log message is received.
        /// </summary>
        public class LogMessageReceived : IEventSubscriable<(string condition, string stackTrace, LogType type)>
        {
            private readonly Dictionary<IEventHandler<(string condition, string stackTrace, LogType type)>, Application.LogCallback> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                Application.LogCallback wrap = (condition, stackTrace, type) => handler.Handle((condition, stackTrace, type));
                Application.logMessageReceived += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.logMessageReceived -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.logMessageReceived -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// Event that is fired if a log message is received.
        /// </summary>
        public class LogMessageReceivedThreaded : IEventSubscriable<(string condition, string stackTrace, LogType type)>
        {
            private readonly Dictionary<IEventHandler<(string condition, string stackTrace, LogType type)>, Application.LogCallback> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                Application.LogCallback wrap = (condition, stackTrace, type) => handler.Handle((condition, stackTrace, type));
                Application.logMessageReceivedThreaded += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.logMessageReceivedThreaded -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.logMessageReceivedThreaded -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// The Application._lowMemory event occurs when your application receives a low-memory notification from the device it is running on.
        /// </summary>
        public class LowMemory : IEventSubscriable<Unit>
        {
            private readonly Dictionary<IEventHandler<Unit>, Application.LowMemoryCallback> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<Unit> handler)
            {
                Application.LowMemoryCallback wrap = () => handler.Handle(Unit.Instance);
                Application.lowMemory += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.lowMemory -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.lowMemory -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// An event that is invoked every frame, on all platforms, immediately before rendering.
        /// </summary>
        public class BeforeRender : IEventSubscriable<Unit>
        {
            private readonly Dictionary<IEventHandler<Unit>, UnityAction> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<Unit> handler)
            {
                UnityAction wrap = () => handler.Handle(Unit.Instance);
                Application.onBeforeRender += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.onBeforeRender -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.onBeforeRender -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// Unity raises this event when the Player is unloading.
        /// </summary>
        public class Unloading : IEventSubscriable
        {
            private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<Unit> handler)
            {
                Action wrap = () => handler.Handle(Unit.Instance);
                Application.unloading += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                Application.unloading -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    Application.unloading -= wrap;
                }

                m_Handlers.Clear();
            }
        }
    }
}