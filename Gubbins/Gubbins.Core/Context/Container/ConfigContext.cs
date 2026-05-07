using Gubbins.Enhance;

namespace Gubbins.Context;

/// <summary>
/// ApplicationContext that was built from a configuration.
/// </summary>
/// <typeparam name="TConfig">Config type.</typeparam>
public class ConfigContext<TConfig> : IContext, IDependenciesRegistry
{
    /// <summary>
    /// The proxy target.
    /// </summary>
    private readonly ApplicationContext m_ApplicationContext;

    /// <inheritdoc />
    public IContext? Parent => m_ApplicationContext.Parent;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigContext{TConfig}"/> class with the specified materials, formatter, and optional parent context.
    /// </summary>
    /// <param name="materials">The materials used to build the context.</param>
    /// <param name="formatter">The formatter used to deserialize the materials.</param>
    /// <param name="parent">The optional parent context.</param>
    public ConfigContext(IEnumerable<TConfig> materials, IDeserializer<TConfig> formatter, IContext? parent = null) => m_ApplicationContext = new ApplicationContext(materials.SelectMany(formatter.Deserialize<IEnumerable<InstallInfo>>), parent ?? ApplicationContext.Global);

    public ConfigContext(TConfig material, IDeserializer<TConfig> formatter, IContext? parent = null) => m_ApplicationContext = new ApplicationContext(formatter.Deserialize<IEnumerable<InstallInfo>>(material), parent ?? ApplicationContext.Global);

    /// <inheritdoc />
    public void Dispose() => m_ApplicationContext.Dispose();

    /// <inheritdoc />
    public object? Resolve(Type? type, string? key) => m_ApplicationContext.Resolve(type, key);

    /// <inheritdoc />
    public object?[] ResolveAll(Type type) => m_ApplicationContext.ResolveAll(type);

    /// <inheritdoc />
    IBindingDecorator IDependenciesRegistry.Register(Type type) => ((IDependenciesRegistry) m_ApplicationContext).Register(type);

    /// <inheritdoc />
    INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance) => ((IDependenciesRegistry) m_ApplicationContext).Register(instance);

    /// <inheritdoc />
    IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances) => ((IDependenciesRegistry) m_ApplicationContext).Register(instances);
}