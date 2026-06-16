using System;
using Gubbins.Unsafe;

namespace Gubbins.Context
{
    /// <summary>
    /// Preload dependencies collections.
    /// </summary>
    internal sealed class PreloadContext : IContext
    {
        /// <summary>
        /// Proxy.
        /// </summary>
        private readonly ApplicationContext m_Context = new(new IDependenciesInstaller[] {new PreloadInstaller()});

        /// <summary>
        /// Global instance.
        /// </summary>
        internal static readonly IContext Instance = new PreloadContext();

        /// <inheritdoc/>
        IContext IContext.Parent => ApplicationContext.Global;

        /// <inheritdoc/>
        void IDisposable.Dispose() => m_Context.Dispose();

        /// <inheritdoc/>
        object IDependenciesResolver.Resolve(Type type, string key) => m_Context.Resolve(type, key);

        /// <inheritdoc/>
        object[] IDependenciesResolver.ResolveAll(Type type) => m_Context.ResolveAll(type);

        /// <summary>
        /// Installs dependencies required for the preload phase.
        /// </summary>
        private sealed class PreloadInstaller : IDependenciesInstaller
        {
            public void Install(IDependenciesRegistry registry)
            {
                // It should be registered when memory preload.
                registry.Register(new UnityMemory()).BindTo<Memory>().AsSingleton();
            }
        }
    }
}