using Godot;

namespace Gubbins.Events;

/// <summary>
/// Provides events for scene loading and unloading in Godot. It allows subscribing and unsubscribing to scene loaded and unloaded events, as well as clearing all subscriptions.
/// </summary>
public class SceneEvents
{
    /// <summary>
    /// Event that is triggered when a scene is loaded in Godot.
    /// </summary>
    public class SceneLoaded : IEventSubscriable<Node>
    {
        private readonly Dictionary<IEventHandler<Node>, Action<Node>> m_Handlers = new();

        /// <inheritdoc/>
        public void Subscribe(IEventHandler<Node> handler)
        {
            Action<Node> action;
            if (handler is ActionEventHandler<Node> actionHandler)
            {
                action = actionHandler.Invocation;
            }
            else
            {
                action = handler.Handle;
            }

            GodotScene.RegisterSceneLoaded(action);
        }

        /// <inheritdoc/>
        public bool Unsubscribe(IEventHandler<Node> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
                return false;
            GodotScene.UnregisterSceneLoaded(action);
            return m_Handlers.Remove(handler);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotScene.UnregisterSceneLoaded(action);
            }

            m_Handlers.Clear();
        }
    }

    /// <summary>
    /// Event that is triggered when a scene is unloaded in Godot.
    /// </summary>
    public class SceneUnloaded : IEventSubscriable<Node>
    {
        private readonly Dictionary<IEventHandler<Node>, Action<Node>> m_Handlers = new();

        /// <inheritdoc/>
        public void Subscribe(IEventHandler<Node> handler)
        {
            Action<Node> action;
            if (handler is ActionEventHandler<Node> actionHandler)
            {
                action = actionHandler.Invocation;
            }
            else
            {
                action = handler.Handle;
            }

            GodotScene.RegisterSceneUnloaded(action);
        }

        /// <inheritdoc/>
        public bool Unsubscribe(IEventHandler<Node> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
                return false;
            GodotScene.UnregisterSceneUnloaded(action);
            return m_Handlers.Remove(handler);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var action in m_Handlers.Values)
            {
                GodotScene.UnregisterSceneUnloaded(action);
            }

            m_Handlers.Clear();
        }
    }
}