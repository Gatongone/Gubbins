using System;
using System.Linq;
using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    /// <summary>
    /// A ScriptableObject that represents the persistant context.
    /// </summary>
    [CreateAssetMenu(fileName = "ScriptableContext", menuName = "Context/ScriptableContext")]
    public class ScriptableContext : ScriptableObject, IContext
    {
        /// <summary>
        /// The parent context for the ScriptableContext, which is a special context that is initialized on preload phase.
        /// </summary>
        [SerializeField] private SerializedReference<IContext> m_Parent;

        /// <summary>
        /// The list of dependencies installers to initialize the context with.
        /// These installers will be executed in the order they are defined in the array.
        /// </summary>
        [SerializeField] private SerializedReference<IDependenciesInstaller>[] m_Installers;

        /// <summary>
        /// Proxy to the actual application context.
        /// </summary>
        private IContext m_Context;

        /// <inheritdoc cref="m_Parent"/>
        public IReadOnlyContext Parent => m_Parent.Value;

        /// <summary>
        /// Initializes the ScriptableContext instance and sets up the application context with the specified installers.
        /// </summary>
        private void OnEnable()
        {
#if UNITY_EDITOR
            var isPlaying = Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
            var isPlaying = Application.isPlaying;
#endif
            // We don't initialize instance on editor mode.
            if (!isPlaying)
            {
                return;
            }

            // Prevent this scriptable object was built in preload phase, which will cause the context to be messed up.
            if (!Preload.HasInitialized)
            {
                Preload.Init();
            }

            var installers = m_Installers.Where(static installer => installer.Value != null)
                                         .Select(static installer => installer.Value);
            m_Context = new ApplicationContext(installers, Parent);
        }

        /// <summary>
        /// Releases any resources held by the context.
        /// </summary>
        public void Dispose() => m_Context.Dispose();

        /// <inheritdoc/>
        object IDependenciesResolver.Resolve(Type type, string key) => m_Context.Resolve(type, key);

        /// <inheritdoc/>
        object[] IDependenciesResolver.ResolveAll(Type type) => m_Context.ResolveAll(type);

        /// <inheritdoc/>
        public IBindingDecorator Register(Type type) => m_Context.Register(type);

        /// <inheritdoc/>
        INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance) => m_Context.Register(instance);

        /// <inheritdoc/>
        IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances) => m_Context.Register(instances);
    }
}