using System;
using System.Linq;
using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    /// <summary>
    /// A ScriptableObject that represents the game context. It is used to register dependencies and listen to events.
    /// </summary>
    [CreateAssetMenu(fileName = "GameContext", menuName = "Context/GameContext")]
    public class GameContext : ScriptableObject, IContext, IDependenciesRegistry
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
        /// The parent context for the GameContext, which is a special context that is initialized on preload phase.
        /// </summary>
        public IContext Parent => PreloadContext.Instance;

        /// <summary>
        /// Gets the dependencies registry of the context.
        /// </summary>
        public IDependenciesRegistry Registry => m_Context;

        /// <summary>
        /// Gets the dependencies resolver of the context.
        /// </summary>
        public IDependenciesResolver Resolver => m_Context;

        /// <summary>
        /// Gets the current instance of the GameContext.
        /// </summary>
        public static IContext Instance { get; private set; }

        /// <summary>
        /// Indicates whether the GameContext instance has been initialized.
        /// </summary>
        [NonSerialized] private bool m_HasInit;

        /// <summary>
        /// Initializes the GameContext instance and sets up the application context with the specified installers and listeners.
        /// </summary>
        private void OnEnable()
        {
#if UNITY_EDITOR
            var isPlaying = Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            var isPlaying = Application.isPlaying;
#endif
            // We don't initialize instance on editor mode.
            if (!isPlaying || m_HasInit || Instance != null)
            {
                return;
            }

            Instance = this;
            var installers = m_Installers.Where(static installer => installer.Value != null)
                                         .Select(static installer => installer.Value);
            m_Context = new ApplicationContext(installers, Parent);
            foreach (var listener in m_Listeners)
            {
                var target = listener.Value;
                if (target != null)
                {
                    m_Context.Inject(target);
                    target.Listen(Resolver, Registry);
                }
                else
                {
                    var type = listener.ExpectedType;
                    var ctor = type == null ? null : InjectCache.GetInjectConstructor(type);
                    if (ctor != null)
                    {
                        target = m_Context.InjectByCtor(listener.ExpectedType) as IEventListener;
                        target?.Listen(Resolver, Registry);
                    }
                }
            }

            m_HasInit = true;
        }

        /// <summary>
        /// Releases any resources held by the context.
        /// </summary>
        public void Dispose()
        {
            foreach (var item in m_Listeners)
            {
                item.Value?.Clear(Resolver);
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

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only class that automatically registers any imported GameContext assets to the Preloaded Assets in Player Settings.
        /// </summary>
        private class PreloadAutoRegister : UnityEditor.AssetPostprocessor
        {
            /// <summary>
            /// Automatically registers any imported GameContext assets to the Preloaded Assets in Player Settings.
            /// </summary>
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (var assetPath in importedAssets)
                {
                    var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                    if (obj is GameContext)
                    {
                        RegisterToPreload(obj);
                    }
                }
            }

            /// <summary>
            /// Registers the given ScriptableObject asset to the Preloaded Assets in Player Settings if it's not already registered.
            /// </summary>
            private static void RegisterToPreload(ScriptableObject asset)
            {
                var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets().ToList();

                // Prevent duplicate registration.
                if (preloadedAssets.Contains(asset))
                {
                    return;
                }

                if (preloadedAssets.Any(static a => a is GameContext))
                {
                    Debug.LogWarning($"A GameContext asset is already registered in Preloaded Assets. Multiple GameContext assets may lead to unexpected behavior. Please ensure only one GameContext asset is registered. Asset path: {UnityEditor.AssetDatabase.GetAssetPath(asset)}");
                }

                // Remove null entries that may exist in the Preloaded Assets list.
                preloadedAssets.RemoveAll(static a => a == null);
                preloadedAssets.Add(asset);
                UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif
    }
}