using Gubbins.Context;
using Gubbins.Event;

namespace Gubbins.Domain;

/// <summary>
/// Minimal logic unit for a specific domain. Basically, a module accepts domain events and responds to them by implement <see cref="IEventListener"/>.
/// </summary>
public interface IModule;

/// <summary>
/// Module attribute for modules that use a specific domain.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ModuleAttribute : Attribute
{
    /// <param name="domainType">Domain type.</param>
    /// <exception cref="ArgumentException"> Domain type must be a subclass of IDomain. </exception>
    public ModuleAttribute(Type domainType)
    {
        if (!domainType.IsAssignableFrom(typeof(IDomain))) throw new ArgumentException("Domain type must be a subclass of IDomain.");
    }
}

/// <summary>
/// Module attribute for modules that use a specific domain.
/// </summary>
/// <typeparam name="T">Domain type</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public class ModuleAttribute<T> : Attribute where T : IDomain;

/// <summary>
/// Represents a module in the application.
/// </summary>
public interface IModules
{
    /// <summary>
    /// Gets the domain for this module.
    /// </summary>
    IDomain Domain { get; }

    /// <summary>
    /// Gets the module for the given type.
    /// </summary>
    /// <param name="moduleType">Module type.</param>
    /// <returns>Module instance or null if not found.</returns>
    IModule Get(Type moduleType);

    /// <summary>
    /// Adds the module to the collection.
    /// </summary>
    /// <param name="module">Module instance.</param>
    void Add(IModule module);

    /// <summary>
    /// Removes the module for the given type.
    /// </summary>
    /// <param name="moduleType">Module type.</param>
    /// <returns>True if the module was removed, false otherwise.</returns>
    bool Remove(Type moduleType);

    /// <summary>
    /// Clears all modules from the collection.
    /// </summary>
    void Clear();
}

/// <summary>
/// Extension methods for IModules.
/// </summary>
public static class ModulesExtensions
{
    /// <param name="modules">Modules collection.</param>
    extension(IModules modules)
    {
        /// <summary>
        /// Removes the module for the given type.
        /// </summary>
        /// <typeparam name="TModule">Module type.</typeparam>
        /// <returns>True if the module was removed, false otherwise.</returns>
        public bool RemoveModule<TModule>() where TModule : IModule => modules.Remove(typeof(TModule));

        /// <summary>
        /// Gets the module for the given type.
        /// </summary>
        /// <returns>Module instance or null if not found.</returns>
        public TModule GetModule<TModule>() where TModule : class, IModule => (modules.Get(typeof(TModule)) as TModule)!;
    }
}

/// <summary>
/// Default implementation of IModules.
/// </summary>
/// <param name="domain">Base domain.</param>
public class Modules(IDomain domain) : IModules
{
    private readonly Dictionary<Type, IModule> m_Modules = new();

    public TModule GetModule<TModule>() where TModule : class => (Get(typeof(TModule)) as TModule)!;

    /// <inheritdoc />
    public IDomain Domain => domain;

    /// <inheritdoc />
    public IModule Get(Type systemType) => m_Modules[systemType];

    /// <inheritdoc />
    public void Add(IModule module)
    {
        var context = Domain.Context;
        context.Resolve<DomainEvents.BeforeModuleAdd>()?.Broadcast(domain, module);
        m_Modules.Add(module.GetType(), module);
        Domain.Context.Inject(module);
        if (module is IEventListener listener)
        {
            listener.Listen(context, Domain.Context.Resolve<IDependenciesRegistry>() ?? throw new InvalidOperationException("Dependencies registry not found"));
        }
        context.Resolve<DomainEvents.AfterModuleAdded>()?.Broadcast(domain, module);
    }

    /// <inheritdoc />
    public bool Remove(Type moduleType)
    {
        var context = Domain.Context;
        if (!m_Modules.TryGetValue(moduleType, out var module))
            return false;
        context.Resolve<DomainEvents.BeforeModuleRemove>()?.Broadcast(domain, module);
        if (m_Modules.Remove(moduleType))
        {
            if (module is IEventListener listener)
            {
                listener.Clear(Domain.Context);
            }
            context.Resolve<DomainEvents.AfterModuleRemoved>()?.Broadcast(domain, module);
            return true;
        }
        // Impossible.
        throw new InvalidOperationException($"{module.GetType()} not found in {Domain.GetType()}");
    }

    /// <inheritdoc />
    public void Clear()
    {
        foreach (var module in m_Modules.Values)
        {
            if (module is IEventListener listener)
            {
                listener.Clear(Domain.Context);
            }
        }

        m_Modules.Clear();
    }
}