using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides strongly-typed wrappers for Unity SceneManager events.
    /// </summary>
    public static class SceneEvents
    {
        /// <summary>
        /// Notifications about a scene being loaded. This is not raised for scenes loaded additively with <c>SceneManager.LoadSceneAsync</c> or <c>SceneManager.LoadScene</c>.
        /// </summary>
        public class SceneLoaded : IEventSubscriable<(Scene scene, LoadSceneMode mode)>
        {
            private readonly Dictionary<IEventHandler<(Scene scene, LoadSceneMode mode)>, UnityAction<Scene, LoadSceneMode>> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<(Scene scene, LoadSceneMode mode)> handler)
            {
                UnityAction<Scene, LoadSceneMode> wrap = (scene, mode) => handler.Handle((scene, mode));
                SceneManager.sceneLoaded += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(Scene scene, LoadSceneMode mode)> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                SceneManager.sceneLoaded -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    SceneManager.sceneLoaded -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// Notifications about a scene being unloaded. This is not raised for scenes loaded additively that are unloaded with <c>SceneManager.UnloadSceneAsync</c> or <c>SceneManager.UnloadScene</c>.
        /// </summary>
        public class SceneUnloaded : IEventSubscriable<Scene>
        {
            private readonly Dictionary<IEventHandler<Scene>, UnityAction<Scene>> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<Scene> handler)
            {
                UnityAction<Scene> wrap = scene => handler.Handle((scene));
                SceneManager.sceneUnloaded += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<Scene> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                SceneManager.sceneUnloaded -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    SceneManager.sceneUnloaded -= wrap;
                }

                m_Handlers.Clear();
            }
        }

        /// <summary>
        /// This event is raised when the active scene has changed. The old scene is the previously active scene, and the new scene is the currently active scene.
        /// Note that the active scene can be changed by loading a new scene, unloading the current active scene, or by explicitly setting it with <c>SceneManager.SetActiveScene</c>.
        /// </summary>
        public class ActiveSceneChanged : IEventSubscriable<(Scene oldScene, Scene newScene)>
        {
            private readonly Dictionary<IEventHandler<(Scene oldScene, Scene newScene)>, UnityAction<Scene, Scene>> m_Handlers = new();

            /// <inheritdoc/>
            public void Subscribe(IEventHandler<(Scene oldScene, Scene newScene)> handler)
            {
                UnityAction<Scene, Scene> wrap = (oldScene, newScene) => handler.Handle((oldScene, newScene));
                SceneManager.activeSceneChanged += wrap;
                m_Handlers.Add(handler, wrap);
            }

            /// <inheritdoc/>
            public bool Unsubscribe(IEventHandler<(Scene oldScene, Scene newScene)> handler)
            {
                if (!m_Handlers.Remove(handler, out var wrap)) return false;
                SceneManager.activeSceneChanged -= wrap;
                return true;
            }

            /// <inheritdoc/>
            public void Clear()
            {
                foreach (var wrap in m_Handlers.Values)
                {
                    SceneManager.activeSceneChanged -= wrap;
                }

                m_Handlers.Clear();
            }
        }
    }
}