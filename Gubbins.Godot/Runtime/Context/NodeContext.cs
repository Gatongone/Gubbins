using Godot;
using Godot.Collections;

namespace Gubbins.Context;

/// <summary>
/// A node that represents a context in the application.
/// </summary>
[GlobalClass, Tool]
public partial class NodeContext : Node, IContext
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

    /// <inheritdoc/>
    IReadOnlyContext IReadOnlyContext.Parent => field ??= Parent?.Value;

    /// <summary>
    /// Initializes the context when the node is ready.
    /// </summary>
    public override void _Ready() => m_Context = new ApplicationContext(Installers.Select(static item => item.Value), ((IReadOnlyContext) this).Parent);

    /// <summary>
    /// Releases any resources held by the context.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_Context.Dispose();
        }
    }

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