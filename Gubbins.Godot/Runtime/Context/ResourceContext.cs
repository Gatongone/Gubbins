using System;
using Godot;
using Gubbins.Enhance;

namespace Gubbins.Context;

public partial class ResourceContext : Resource, IContext
{
    private ApplicationContext m_Context;

    /// <summary>
    /// Gets the dependencies registry of the context.
    /// </summary>
    private IDependenciesRegistry Registry => m_Context;

    /// <summary>
    /// Gets the dependencies resolver of the context.
    /// </summary>
    private IDependenciesResolver Resolver => m_Context;

    [Export] private SerializedInstallInfo[] m_InstallInfos;

    [Export] private SerializedReference<IContext> m_Parent;

    /// <inheritdoc/>
    public IReadOnlyContext Parent => m_Parent.Value ?? ApplicationContext.Global;

    /// <summary>
    /// Releases any resources held by the context.
    /// </summary>
    public void Dispose() => m_Context.Dispose();

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