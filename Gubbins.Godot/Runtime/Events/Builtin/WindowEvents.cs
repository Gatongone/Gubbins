#if GUBBINS_ENABLED
using System;
using System.Collections.Generic;
using Godot;
using Gubbins.Enhance;

namespace Gubbins.Events;

/// <summary>
/// Provides a collection of window-related events.
/// </summary>
public static class WindowEvents
{
    /// <summary>
    /// Emitted when the size of the viewport is changed, whether by resizing of window, or some other means.
    /// </summary>
    public sealed class Resize : IEventSubscriable<Vector2I>
    {
        private readonly Dictionary<IEventHandler<Vector2I>, Action<Vector2I>> m_Handlers = new();

        public void Subscribe(IEventHandler<Vector2I> handler)
        {
            Action<Vector2I> action;
            if (handler is ActionEventHandler<Vector2I> actionHandler)
            {
                action = actionHandler.Invocation;
            }
            else
            {
                action = handler.Handle;
            }

            GodotWindow.RegisterWindowSizeChangedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Vector2I> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                m_Handlers.Remove(handler);
            }

            GodotWindow.UnregisterWindowSizeChangedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterWindowSizeChangedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the Window's DPI changes as a result of OS-level changes (e.g. moving the window from a Retina display to a lower resolution one).
    /// </summary>
    /// <remarks>
    /// Only implemented on macOS and Linux (Wayland).
    /// </remarks>
    public sealed class DPIChanged : IEventSubscriable<(int dpi, float scale)>
    {
        private readonly Dictionary<IEventHandler<(int dpi, float scale)>, Action<int, float>> m_Handlers = new();

        public void Subscribe(IEventHandler<(int dpi, float scale)> handler)
        {
            Action<int, float> action;
            if (handler is ActionEventHandler<int, float> actionHandler)
            {
                action = actionHandler.Invocation;
            }
            else
            {
                action = (dpi, scale) => handler.Handle((dpi, scale));
            }

            GodotWindow.RegisterWindowDpiChangedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<(int dpi, float scale)> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                m_Handlers.Remove(handler);
            }

            GodotWindow.UnregisterWindowDpiChangedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterWindowDpiChangedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the mouse cursor enters the Window's visible area, that is not occluded behind other Controls or windows, provided its GuiDisableInput is false and regardless if it's currently focused or not.
    /// </summary>
    public sealed class MouseEntered : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterMouseEnteredCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterMouseEnteredCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterMouseEnteredCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the mouse cursor leaves the Window's visible area, that is not occluded behind other Controls or windows, provided its GuiDisableInput is false and regardless if it's currently focused or not.
    /// </summary>
    public sealed class MouseExited : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterMouseExitedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterMouseExitedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterMouseExitedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the Window gains focus.
    /// </summary>
    public sealed class FocusEntered : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterFocusEnteredCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterFocusEnteredCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterFocusEnteredCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the Window loses its focus.
    /// </summary>
    public sealed class FocusExited : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterFocusExitedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterFocusExitedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterFocusExitedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the Window's close button is pressed or when PopupWindow is enabled and user clicks outside the window.
    /// </summary>
    public sealed class Close : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterCloseRequestedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterCloseRequestedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterCloseRequestedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Notification received from the OS when a go back request is sent (e.g. pressing the "Back" button on Android).
    /// </summary>
    public sealed class Goback : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterGobackRequestedCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterGobackRequestedCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterGobackRequestedCallback(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Emitted when the node is considered ready, after _Ready() is called.
    /// This signal can be used to handle window closing, e.g. by connecting it to Hide().
    /// </summary>
    public sealed class Ready : IEventSubscriable
    {
        private readonly Dictionary<IEventHandler<Unit>, Action> m_Handlers = new();

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

            GodotWindow.RegisterWindowReadyCallback(action);
            m_Handlers.Add(handler, action);
        }

        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
            {
                return false;
            }

            GodotWindow.UnregisterWindowReadyCallback(action);
            return m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotWindow.UnregisterWindowReadyCallback(action);
            }

            m_Handlers.Clear();
        }
    }
}
#endif
