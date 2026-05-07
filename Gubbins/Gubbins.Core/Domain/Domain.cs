using System.Collections.Concurrent;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Event;

namespace Gubbins.Domain;

/// <summary>
/// Defines the contract for a domain that manages modules and application context with reset capabilities.
/// </summary>
public interface IDomain
{
    /// <summary>
    /// Event that is raised when the domain is reset.
    /// </summary>
    event Action OnReset;

    /// <summary>
    /// Gets the collection of modules managed by this domain.
    /// </summary>
    IModules Modules { get; }

    /// <summary>
    /// Gets the application context associated with this domain.
    /// </summary>
    IContext Context { get; }

    /// <summary>
    /// Resets the domain to its initial state, clearing modules and disposing resources.
    /// </summary>
    void Reset();
}

/// <summary>
/// Default implementation of IDomain that provides module management, dependency injection context, and caching capabilities.
/// </summary>
public class Domain : IDomain
{
    private const string DOMAIN_KEY  = "DefaultDomain";
    private const string CONTEXT_KEY = "DefaultContext";
    private const string REPO_KEY    = "DefaultReponsitory";

    /// <summary>
    /// Gets the standard singleton instance of the default Domain.
    /// </summary>
    public static IDomain Standard => GetDomain<Domain>();

    private static readonly ConcurrentDictionary<Type, IDomain> s_DomainCache = new();

    /// <summary>
    /// Event that is raised when the domain is reset.
    /// </summary>
    public event Action? OnReset;

    /// <summary>
    /// Gets the collection of modules managed by this domain.
    /// </summary>
    public virtual IModules Modules { get; }

    /// <summary>
    /// Gets the application context with default dependency registrations.
    /// </summary>
    public virtual IContext Context { get; } = new Context.ApplicationContext([new DefaultInstaller()], Gubbins.Context.ApplicationContext.Global);

    /// <summary>
    /// Initializes a new instance of the Domain class and sets up the modules collection.
    /// </summary>
    public Domain() => Modules = new Modules(this);

    /// <summary>
    /// Gets or creates a cached instance of the specified domain type.
    /// </summary>
    /// <typeparam name="TDomain">The type of domain to retrieve, must implement IDomain and have a parameterless constructor.</typeparam>
    /// <returns>A singleton instance of the specified domain type.</returns>
    public static TDomain GetDomain<TDomain>() where TDomain : IDomain, new()
    {
        var category = Cache<TDomain>.Domain;
        s_DomainCache[typeof(TDomain)] = category;
        return category;
    }

    /// <summary>
    /// Gets a domain instance by type, creating it if necessary and the type supports instantiation.
    /// </summary>
    /// <param name="domainType">The type of domain to retrieve, must implement IDomain.</param>
    /// <returns>An instance of the specified domain type, or null if the type cannot be instantiated.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified type does not implement IDomain.</exception>
    public static IDomain? GetDomain(Type domainType)
    {
        if (s_DomainCache.TryGetValue(domainType, out var category))
        {
            return category;
        }

        if (!typeof(IDomain).IsAssignableFrom(domainType))
        {
            throw new ArgumentException($"{domainType} doesn't implement Gubbins.Entities.ICategory");
        }

        if (domainType.IsNewable(out var factory))
        {
            category = factory.New() as IDomain;
        }

        return category;
    }

    /// <summary>
    /// Resets the domain to its initial state by clearing modules, disposing all registered disposable resources, and disposing the context.
    /// </summary>
    public virtual void Reset()
    {
        OnReset?.Invoke();
        Modules.Clear();
        foreach (var res in Context.ResolveAll<IDisposable>())
        {
            res.Dispose();
        }

        Context.Dispose();
    }

    /// <summary>
    /// Provides cached singleton instances for domain types.
    /// </summary>
    /// <typeparam name="TDomain">The domain type to cache, must implement IDomain and have a parameterless constructor.</typeparam>
    private static class Cache<TDomain> where TDomain : IDomain, new()
    {
        internal static readonly TDomain Domain = new();
    }

    /// <summary>
    /// Default dependency installer that registers core services and domain events.
    /// </summary>
    private class DefaultInstaller : IDependenciesInstaller
    {
        /// <summary>
        /// Installs default dependencies including context services, repository, and domain events.
        /// </summary>
        /// <param name="registry">The dependency registry to register services with.</param>
        public void Install(IDependenciesRegistry registry)
        {
            registry.Register(registry)
                    .BindTo<IDependenciesRegistry, IDependenciesResolver, IContext>()
                    .WithKey(CONTEXT_KEY)
                    .AsSingleton();
            registry.Register(new DomainEvents.BeforeModuleAdd())
                    .BindTo<IEvent<IModule>, IEventBroadcastable<IModule>, IEventSubscriable<IModule>, IWeakEventSubscriable<IModule>>()
                    .WithKey(nameof(DomainEvents.BeforeModuleAdd))
                    .AsSingleton();
            registry.Register(new DomainEvents.AfterModuleAdded())
                    .BindTo<IEvent<IModule>, IEventBroadcastable<IModule>, IEventSubscriable<IModule>, IWeakEventSubscriable<IModule>>()
                    .WithKey(nameof(DomainEvents.AfterModuleAdded))
                    .AsSingleton();
            registry.Register(new DomainEvents.BeforeModuleRemove())
                    .BindTo<IEvent<IModule>, IEventBroadcastable<IModule>, IEventSubscriable<IModule>, IWeakEventSubscriable<IModule>>()
                    .WithKey(nameof(DomainEvents.BeforeModuleRemove))
                    .AsSingleton();
            registry.Register(new DomainEvents.AfterModuleRemoved())
                    .BindTo<IEvent<IModule>, IEventBroadcastable<IModule>, IEventSubscriable<IModule>, IWeakEventSubscriable<IModule>>()
                    .WithKey(nameof(DomainEvents.AfterModuleRemoved))
                    .AsSingleton();
        }
    }
}