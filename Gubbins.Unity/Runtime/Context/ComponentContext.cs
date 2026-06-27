using System;
using System.Linq;
using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    /// <summary>
    /// Represents a context that is attached to a GameObject in the scene or hanging on a prefab.
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class ComponentContext : MonoBehaviour, IContext
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

        /// <inheritdoc/>
        public IReadOnlyContext Parent => m_Parent.Value ?? ApplicationContext.Global;

        /// <summary>
        /// Initializes the ComponentContext instance and sets up the application context with the specified installers.
        /// </summary>
        private void Awake()
        {
            var installers = m_Installers.Where(static installer => installer.Value != null)
                                         .Select(static installer => installer.Value);
            m_Context = new ApplicationContext(installers, Parent);
        }

        /// <summary>
        /// Unsubscribes all and releases any resources held by the context when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy() => Dispose();

#if UNITY_EDITOR
        /// <summary>
        /// Creates a new GameObject with a ComponentContext component attached to it in the Unity Editor.
        /// </summary>
        [UnityEditor.MenuItem("GameObject/Context/ComponentContext", false, -1)]
        private static void CreateComponentContext() => new GameObject("ComponentContext").AddComponent<ComponentContext>();
#endif

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