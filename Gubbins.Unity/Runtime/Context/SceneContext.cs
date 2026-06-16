using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Enhance;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gubbins.Context
{
    /// <summary>
    /// A context that is specific to a Unity scene, allowing for scene-specific dependency registration and resolution.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class SceneContext : MonoBehaviour, IContext, IDependenciesRegistry
    {
        /// <summary>
        /// Proxy to the actual application context.
        /// </summary>
        private ApplicationContext m_Context;

        /// <summary>
        /// The list of dependencies installers to initialize the context with.
        /// These installers will be executed in the order they are defined in the array.
        /// </summary>
        [SerializeField] private SerializedReference<IDependenciesInstaller>[] m_Installers;

        /// <summary>
        /// The list of event listeners to register with the context.
        /// These listeners will be executed in the order they are defined in the array.
        /// </summary>
        [SerializeField] private SerializedReference<IEventListener>[] m_Listeners;

        /// <summary>
        /// The parent context for the SceneContext, which is the GameContext instance if it has been initialized,
        /// or the PreloadContext instance which is a special context that is initialized on preload phase
        /// if the GameContext instance has not been initialized.
        /// </summary>
        public IContext Parent => GameContext.Instance ?? PreloadContext.Instance;

        /// <summary>
        /// Gets the dependencies registry of the context.
        /// </summary>
        public IDependenciesRegistry Registry => m_Context;

        /// <summary>
        /// Gets the dependencies resolver of the context.
        /// </summary>
        public IDependenciesResolver Resolver => m_Context;

        /// <summary>
        /// Gets the SceneContext instance of the current scene.
        /// </summary>
        public static SceneContext Current { get; private set; }

        /// <summary>
        /// Indicates whether the GameContext instance has been initialized.
        /// </summary>
        private bool m_HasInit;

        /// <summary>
        /// Context location cache.
        /// </summary>
        private static readonly Dictionary<string, WeakReference<SceneContext>> s_ContextCache = new();

        /// <summary>
        /// </summary>
        private void Awake()
        {
            if (m_HasInit)
            {
                return;
            }

            Current  = this;
            var installers = m_Installers.Where(static installer => installer.Value != null)
                                         .Select(static installer => installer.Value);
            m_Context = new ApplicationContext(installers, Parent);
            foreach (var listener in m_Listeners)
            {
                var target = listener.Value;
                if (target != null)
                {
                    target.Listen(Resolver, Registry);
                    m_Context.Inject(target);
                }
            }

            m_HasInit = true;
        }

        /// <summary>
        /// Unsubscribes all event listeners and releases any resources held by the context when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (gameObject.scene == SceneManager.GetActiveScene())
            {
                Current = null;
            }

            Dispose();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Context/SceneContext", false, -1)]
        private static void CreateSceneContext()
        {
            var curScene = UnityEditor.Selection.activeGameObject?.scene ?? SceneManager.GetActiveScene();
            if (GetSceneContext(curScene) != null)
            {
                Debug.LogWarning("SceneContext has already been created in current scene. You should keep only one of them.");
            }

            new GameObject("SceneContext").AddComponent<SceneContext>();
        }
#endif

        /// <summary>
        /// Get scene context by name.
        /// </summary>
        /// <param name="sceneName">Scene name.</param>
        /// <returns>The first scene context in target scene.</returns>
        public static SceneContext GetSceneContext(string sceneName) => GetSceneContext(SceneManager.GetSceneByName(sceneName));

        /// <summary>
        /// Get the first scene context from scene.
        /// </summary>
        /// <param name="scene">Search scene.</param>
        /// <returns>The first scene context in target scene.</returns>
        public static SceneContext GetSceneContext(Scene scene)
        {
            if (string.IsNullOrEmpty(scene.name)) return null;
            SceneContext context;
            if (!s_ContextCache.TryGetValue(scene.name, out var weakContext))
            {
                context = scene
                          .GetRootGameObjects()
                          .FirstOrDefault(static go => go.GetComponent<SceneContext>() != null)
                          ?.GetComponent<SceneContext>();
                if (context != null)
                {
                    weakContext = new WeakReference<SceneContext>(context);
                    s_ContextCache.Add(scene.name, weakContext);
                }
            }

            if (weakContext == null)
            {
                return null;
            }

            if (weakContext.TryGetTarget(out context) && context != null)
            {
                return context;
            }

            // Here means the cached scene was dead, so we ganna remove it and re-get again.
            s_ContextCache.Remove(scene.name);
            // If the scene is valid, then re-get the context from it.
            return scene.isLoaded && scene.IsValid() ? GetSceneContext(scene) : null;
        }

        /// <summary>
        /// Releases any resources held by the context.
        /// </summary>
        public void Dispose()
        {
            foreach (var listener in m_Listeners)
            {
                listener.Value?.Clear(Resolver);
            }

            m_Context.Dispose();
        }

        /// <inheritdoc/>
        object IDependenciesResolver.Resolve(Type type, string key) => Resolver.Resolve(type, key);

        /// <inheritdoc/>
        object[] IDependenciesResolver.ResolveAll(Type type) => Resolver.ResolveAll(type);

        /// <inheritdoc/>
        public IBindingDecorator Register(Type type) => Registry.Register(type);

        /// <inheritdoc/>
        INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance) => Registry.Register(instance);

        /// <inheritdoc/>
        IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances) => Registry.Register(instances);
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
        public static SceneContext GetSceneContext(this Scene scene) => SceneContext.GetSceneContext(scene);
    }
}