using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Gubbins.Context;
using Gubbins.Enhance;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gubbins.Pipeline
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> that represents a scene-level pipeline for managing event listeners and context in a Unity scene.
    /// </summary>
    public class ScenePipeline : MonoBehaviour, IPipeline
    {
        /// <summary>
        /// The context reference for the Scene. This context will be used to register the event listeners defined in the pipeline. If not set, it will default to the global application context.
        /// </summary>
        [Tooltip("The context reference for the Scene. This context will be used to register the event listeners defined in the pipeline. If not set, it will default to the global application context."), SerializeField]
        private SerializedReference<IContext> m_Context;

        /// <summary>
        /// The list of event listeners to register with the context. These listeners will be executed in the order they are defined in the array.
        /// When the listener type contains a constructor with parameters, the context will attempt to resolve the dependencies and inject them into the constructor.
        /// Else, it will be instantiated using the default constructor an inject by fields/properties or method.
        /// </summary>
        [Tooltip("The list of event listeners to register with the context. These listeners will be executed in the order they are defined in the array. When the listener type contains a constructor with parameters, the context will attempt to resolve the dependencies and inject them into the constructor. Else, it will be instantiated using the default constructor an inject by fields/properties or method."), SerializeField, AllowDefaultConstructorMissing]
        private SerializedReference<IEventListener>[] m_Listeners;

        /// <inheritdoc/>
        public PipeLineState State { get; private set; }

        /// <summary>
        /// Gets the current active instance of the ScenePipeline. This property returns the ScenePipeline instance that is currently active in the scene, or null if no ScenePipeline is active.
        /// </summary>
        public static ScenePipeline Current { get; private set; }

        /// <inheritdoc cref="m_Context"/>
        public IContext Context => m_Context?.Value;

        /// <summary>
        /// Context location cache.
        /// </summary>
        private static readonly Dictionary<string, WeakReference<ScenePipeline>> s_PipelineCache = new();

        /// <summary>
        /// A list of instantiated event listeners that have been registered with the context.
        /// </summary>
        private readonly List<IEventListener> m_ListenerInstances = new();

        /// <summary>
        /// Initializes the ScenePipeline instance and sets up the application context with the specified installers and listeners.
        /// </summary>
        private void Awake()
        {
            Current = this;
            StartPipeline();
        }

        /// <summary>
        /// Starts the ScenePipeline by registering the event listeners with the context and transitioning the pipeline state to Running.
        /// If the pipeline has already been started, an <see cref="InvalidOperationException"/> is thrown to prevent multiple invocations of the Start method.
        /// If there are no listeners to register, the pipeline state is set to Completed immediately.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the Start method is called more than once during its lifecycle.</exception>
        private void StartPipeline()
        {
            if (State == PipeLineState.Running)
            {
                throw new InvalidOperationException("The ScenePipeline has already been started. Please ensure that the Start method is called only once during the application lifecycle.");
            }

            if (m_Listeners.Length == 0)
            {
                State = PipeLineState.Completed;
                return;
            }

            var context = m_Context.Value;
            if (context == null)
            {
                context = m_Context.Value = ApplicationContext.Global;
                Debug.LogWarning("The context reference for the GamePipeline is not set. Defaulting to the global application context. Please assign a valid context reference to ensure proper functionality.");
            }

            try
            {
                if (State == PipeLineState.NotStarted)
                {
                    RegisterListeners(context);
                }
                else
                {
                    foreach (var listener in m_ListenerInstances)
                    {
                        listener.Listen(context, context);
                    }
                }
                State = PipeLineState.Running;
            }
            catch
            {
                State = PipeLineState.Failed;
                throw;
            }
        }

        /// <summary>
        /// Registers the event listeners defined in the GamePipeline with the provided context.
        /// </summary>
        private void RegisterListeners(IContext context)
        {
            foreach (var listener in m_Listeners)
            {
                var targetType = listener.ExpectedType;
                // Try inject by ctor first.
                if (targetType != null && InjectCache.GetInjectConstructor(targetType) != null)
                {
                    var item = context.InjectByCtor(targetType) as IEventListener;
                    listener.Value = item;
                    if (item != null)
                    {
                        item.Listen(context, context);
                        m_ListenerInstances.Add(item);
                    }
                }
                else
                {
                    var item = listener.Value;
                    if (item != null)
                    {
                        context.Inject(item);
                        item.Listen(context, context);
                        m_ListenerInstances.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the GamePipeline by clearing the registered event listeners from the context and transitioning the pipeline state to Completed.
        /// </summary>
        public void OnDestroy()
        {
            if (gameObject.scene == SceneManager.GetActiveScene())
            {
                Current = null;
            }

            if (State != PipeLineState.Running)
            {
                return;
            }

            var context = m_Context.Value;
            if (context != null)
            {
                foreach (var listener in m_ListenerInstances)
                {
                    listener?.Clear(context);
                }
            }

            State = PipeLineState.Completed;
        }

        /// <inheritdoc/>
        [SuppressMessage("ReSharper", "Unity.IncorrectMethodSignature")]
        Unit IPhase<Unit, Unit>.Start(Unit input)
        {
            StartPipeline();
            return input;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new GameObject with a ScenePipeline component attached to it in the Unity Editor.
        /// </summary>
        [UnityEditor.MenuItem("GameObject/Context/SceneContext", false, -1)]
        private static void CreateSceneContext()
        {
            var curScene = UnityEditor.Selection.activeGameObject?.scene ?? SceneManager.GetActiveScene();
            if (GetSceneContext(curScene) != null)
            {
                Debug.LogWarning("SceneContext has already been created in current scene. You should keep only one of them.");
            }

            new GameObject("SceneContext").AddComponent<ScenePipeline>();
        }
#endif

        /// <summary>
        /// Get scene context by name.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        /// <returns>The first scene context in target scene.</returns>
        public static IContext GetSceneContext(string sceneName) => GetSceneContext(SceneManager.GetSceneByName(sceneName));

        /// <summary>
        /// Get the first scene context from scene.
        /// </summary>
        /// <param name="scene">Search scene.</param>
        /// <returns>The first scene context in target scene.</returns>
        public static IContext GetSceneContext(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.name)) return null;
            ScenePipeline pipeline;
            var isSceneValid = scene.isLoaded && scene.IsValid();
            // Try to get the context from cache if the scene is valid.
            if (!s_PipelineCache.TryGetValue(scene.name, out var weakRef) && isSceneValid)
            {
                foreach (var go in scene.GetRootGameObjects())
                {
                    pipeline = go.GetComponent<ScenePipeline>();
                    if (pipeline)
                    {
                        weakRef = new WeakReference<ScenePipeline>(pipeline);
                        s_PipelineCache.Add(scene.name, weakRef);
                        break;
                    }
                }
            }

            if (weakRef == null) return null;

            if (weakRef.TryGetTarget(out pipeline) && pipeline != null && isSceneValid)
            {
                return pipeline.Context;
            }

            // The cached scene was dead, then remove it.
            s_PipelineCache.Remove(scene.name);
            return null;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="SceneContext"/>.
    /// </summary>
    public static class SceneContextExtensions
    {
        /// <summary>
        /// Get the first scene context from the scene.
        /// </summary>
        /// <param name="scene">Scene.</param>
        /// <returns>The first scene context in the scene.</returns>
        public static IContext GetSceneContext(this Scene scene) => ScenePipeline.GetSceneContext(scene);
    }
}