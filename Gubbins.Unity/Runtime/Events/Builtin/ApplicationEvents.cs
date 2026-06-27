using System;
using System.Collections.Generic;
using Gubbins.Enhance;
using UnityEngine;
using UnityEngine.Events;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides strongly-typed wrapper for Unity Application events.
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
                Action<bool> action = handler.Handle;
                Application.focusChanged += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<bool> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.focusChanged -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.focusChanged -= action;
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
                Action action;
                if (handler is ActionEventHandler actionHandler)
                {
                    action = actionHandler.Invocation;
                }
                else
                {
                    action = () => handler.Handle(Unit.Instance);
                }

                Application.quitting += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.quitting -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.quitting -= action;
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
                Action<string> action = handler.Handle;
                Application.deepLinkActivated += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<string> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.deepLinkActivated -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.deepLinkActivated -= action;
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
                Application.LogCallback action;
                if (handler is ActionEventHandler<(string condition, string stackTrace, LogType type)> actionHandler)
                {
                    action = (condition, stackTrace, type) => actionHandler.Invocation((condition, stackTrace, type));
                }
                else
                {
                    action = (condition, stackTrace, type) => handler.Handle((condition, stackTrace, type));
                }

                Application.logMessageReceived += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.logMessageReceived -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.logMessageReceived -= action;
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
                Application.LogCallback action;
                if (handler is ActionEventHandler<string, string, LogType> actionHandler)
                {
                    action = actionHandler.Invocation.Invoke;
                }
                else
                {
                    action = (condition, stackTrace, type) => handler.Handle((condition, stackTrace, type));
                }

                Application.logMessageReceivedThreaded += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(string condition, string stackTrace, LogType type)> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.logMessageReceivedThreaded -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.logMessageReceivedThreaded -= action;
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
                Application.LowMemoryCallback action;
                if (handler is ActionEventHandler actionHandler)
                {
                    action = actionHandler.Invocation.Invoke;
                }
                else
                {
                    action = () => handler.Handle(Unit.Instance);
                }

                Application.lowMemory += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.lowMemory -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.lowMemory -= action;
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
                UnityAction action;
                if (handler is ActionEventHandler actionHandler)
                {
                    action = actionHandler.Invocation.Invoke;
                }
                else
                {
                    action = () => handler.Handle(Unit.Instance);
                }
                Application.onBeforeRender += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.onBeforeRender -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.onBeforeRender -= action;
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
                Action action;
                if (handler is ActionEventHandler actionHandler)
                {
                    action = actionHandler.Invocation;
                }
                else
                {
                    action = () => handler.Handle(Unit.Instance);
                }
                Application.unloading += action;
                m_Handlers.Add(handler, action);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Unit> handler)
            {
                if (!m_Handlers.Remove(handler, out var action)) return false;
                Application.unloading -= action;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var action in m_Handlers.Values)
                {
                    Application.unloading -= action;
                }

                m_Handlers.Clear();
            }
        }
    }
}