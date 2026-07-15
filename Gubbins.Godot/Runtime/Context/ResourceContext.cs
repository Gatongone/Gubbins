#if GUBBINS_ENABLED
using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace Gubbins.Context;

/// <summary>
/// A resource that represents a context in the application.
/// </summary>
[GlobalClass, Tool]
public partial class ResourceContext : global::Godot.Resource, IContext
{
    /// <summary>
    /// The parent context to inherit from. If null, the context will be a root context.
    /// </summary>
    [Export] private SerializedContext Parent = new();

    /// <summary>
    /// The installers to use for this context. These will be executed in the order they are defined.
    /// </summary>
    [Export] private Array<SerializedInstaller> Installers;

    private IContext m_Context;

    private IReadOnlyContext m_Parent;

    /// <inheritdoc/>
    IReadOnlyContext IReadOnlyContext.Parent => m_Parent ??= Parent?.Value;

    private IContext Context => m_Context ??= new ApplicationContext(Installers.Select(static item => item.Value), ((IReadOnlyContext) this).Parent);

    /// <summary>
    /// Releases any resources held by the context.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_Context?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc/>
    object IDependenciesResolver.Resolve(Type type, string key) => Context.Resolve(type, key);

    /// <inheritdoc/>
    object[] IDependenciesResolver.ResolveAll(Type type) => m_Context.ResolveAll(type);

    /// <inheritdoc/>
    public IBindingDecorator Register(Type type) => m_Context.Register(type);

    /// <inheritdoc/>
    INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance) => m_Context.Register(instance);

    /// <inheritdoc/>
    IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances) => m_Context.Register(instances);
}
#endif