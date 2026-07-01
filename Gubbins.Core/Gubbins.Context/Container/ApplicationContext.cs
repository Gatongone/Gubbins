using System.Runtime.CompilerServices;
using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// The dependence information collection with scope management and dependency injection/resolution.
/// </summary>
public class ApplicationContext : IScopeController, IContext
{
    public static readonly IContext Global = new ApplicationContext();

    /// <inheritdoc/>
    public event Action? OnScopeFinish;

    /// <summary>
    /// Flushed flag that use to check all <see cref="DependencyInfo"/> have been registered.
    /// </summary>
    private bool m_HasFlushed;

    /// <summary>
    /// Key to baked multiton instance mappings.
    /// </summary>
    private readonly Dictionary<string, Stack<object>> m_MultitonCache = new();

    /// <summary>
    /// Key to baked singleton instance mappings.
    /// </summary>
    private readonly Dictionary<string, object> m_SingletonsCache = new();

    /// <summary>
    /// Key to scope mappings.
    /// </summary>
    private readonly Dictionary<string, Scope> m_ScopeMaps = new();

    /// <summary>
    /// Key to spawner mappings.
    /// </summary>
    private readonly Dictionary<string, ISpawner> m_SpawnerMaps = new();

    /// <summary>
    /// Actual type to key mappings.
    /// </summary>
    private readonly Dictionary<Type, List<string>> m_ActualTypeMaps = new();

    /// <summary>
    /// Key to actual type mappings, used to construct spawner-less dependencies via constructor injection.
    /// </summary>
    private readonly Dictionary<string, Type> m_KeyToType = new();

    /// <summary>
    /// Keys currently being constructed via constructor injection, used to detect circular dependencies.
    /// </summary>
    private readonly HashSet<string> m_Constructing = new();

    /// <summary>
    /// Binding type to key mappings.
    /// </summary>
    private readonly Dictionary<Type, List<string>> m_BindingMaps = new();

    /// <summary>
    /// Register info that waits for flushing into cache.
    /// </summary>
    private readonly Queue<DependencyInfo> m_InstallQueue = new();

    /// <inheritdoc/>
    public IReadOnlyContext? Parent { get; }

    /// <summary>
    /// Disposed flag for deconstruct method.
    /// </summary>
    private bool m_Disposed;

    private ApplicationContext() { }

    public ApplicationContext(IEnumerable<IDependenciesInstaller> installers, IReadOnlyContext? parent = null)
    {
        Parent = parent;
        foreach (var installer in installers)
        {
            installer.Install(this);
        }

        Bake();
    }

    ~ApplicationContext()
    {
        if (!m_Disposed)
        {
            OnScopeFinish?.Invoke();
        }
    }

    /// <summary>
    /// Finish the custom scope.
    /// </summary>
    public void Dispose()
    {
        m_MultitonCache.Clear();
        m_SingletonsCache.Clear();
        m_ScopeMaps.Clear();
        m_SpawnerMaps.Clear();
        m_ActualTypeMaps.Clear();
        m_KeyToType.Clear();
        m_Constructing.Clear();
        m_BindingMaps.Clear();
        m_InstallQueue.Clear();
        OnScopeFinish?.Invoke();
        OnScopeFinish = null;
        m_Disposed    = true;
    }

    #region Resolve

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Throw when the binding type exist but the actual type doesn't exist.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object? Resolve(Type? type, string? key)
    {
        FlushInstallObject();

        // Resolve from key.
        if (!string.IsNullOrEmpty(key))
        {
            return ResolveKey(key);
        }

        if (type == null)
        {
            throw new ArgumentException("Type is null.");
        }

        key = m_BindingMaps.TryGetValue(type!, out var keys)
            // If the type has binding, then we could only get first key from cache.
            ? keys.First()
            // Else get key from actual type.
            : GetKeyFromActualType(type!);

        return ResolveKey(key) ?? Parent?.Resolve(type, null);
    }

    /// <inheritdoc/>
    public object[] ResolveAll(Type type)
    {
        FlushInstallObject();

        // If the type has non binding, then resolve from actual type.
        if (!m_BindingMaps.TryGetValue(type, out var keys))
        {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
            return m_ActualTypeMaps.TryGetValue(type, out keys)
                ? keys.Select(ResolveKey).Where(instance => null != instance).ToArray()
                : [];
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
        }

        // Else resolve all the actual types of the binding.
        var result = new List<object>();
        for (var index = 0; index < keys.Count; index++)
        {
            var instance = ResolveKey(keys[index]);
            if (instance != null)
            {
                result.Add(instance);
            }
        }

        if (Parent != null)
            result.AddRange(Parent.ResolveAll(type)!);
        return result.ToArray();
    }

    /// <summary>
    /// Get key from the type.
    /// </summary>
    /// <param name="actualType">The actual type which is not the binding type.</param>
    /// <returns>Key of the actual type.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetKeyFromActualType(Type actualType)
    {
        if (!m_ActualTypeMaps.TryGetValue(actualType, out var keys) || keys.Count == 0)
        {
            return actualType.FullName ?? actualType.Name;
        }

        return keys[0];
    }

    /// <summary>
    /// Resolve dependency by key.
    /// </summary>
    /// <param name="key">Key of the dependency.</param>
    /// <returns>Resolved instance</returns>
    /// <exception cref="ArgumentOutOfRangeException">Throw when the scope is not Custom, Singleton or Multiton value.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? ResolveKey(string key)
    {
        if (!m_ScopeMaps.TryGetValue(key, out var scope))
        {
            // The dependency does not exist.
            return null;
        }

        switch (scope)
        {
            case Scope.Custom:
            case Scope.Singleton:
                return ResolveSingleton(key);
            case Scope.Multiton:
                return ResolveMultiton(key);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Resolve multiton by key.
    /// </summary>
    /// <param name="key">Key of the dependency.</param>
    /// <returns>Resolved instance</returns>
    /// <exception cref="ArgumentException">Throw when the spawner never been registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? ResolveMultiton(string key)
    {
        // Try get from cache.
        if (m_MultitonCache.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            // The cache one has been resolved, so we don't need to resolve again.
            return stack.Pop();
        }

        // Create instance from spawner, or fall back to constructor injection.
        object? instance;
        if (m_SpawnerMaps.TryGetValue(key, out var spawner))
        {
            instance = spawner.Spawn();
        }
        else
        {
            instance = ConstructByInjection(key)
                ?? throw new ArgumentException($"Spawner never been registered. Key : {key}");
        }

        if (instance == null) return null;
        this.Inject(instance);
        return instance;
    }

    /// <summary>
    /// Resolve singleton by key.
    /// </summary>
    /// <param name="key">Key of the dependency.</param>
    /// <returns>Resolved instance</returns>
    /// <exception cref="ArgumentException">Throw when the spawner never been registered.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object? ResolveSingleton(string key)
    {
        // Try get from cache.
        if (m_SingletonsCache.TryGetValue(key, out var instance))
        {
            // The cache one has been resolved, so we don't need to resolve again.
            return instance;
        }

        // Create instance from spawner, or fall back to constructor injection.
        if (m_SpawnerMaps.TryGetValue(key, out var spawner))
        {
            instance = spawner.Spawn();
        }
        else
        {
            instance = ConstructByInjection(key)
                ?? throw new ArgumentException($"Spawner never been registered. Key : {key}");
        }

        if (instance == null) return null;

        // Append to cache.
        m_SingletonsCache.Add(key, instance);
        this.Inject(instance);
        return instance;
    }

    /// <summary>
    /// Construct a spawner-less dependency by resolving the parameters of its inject/greediest constructor.
    /// </summary>
    /// <param name="key">Key of the dependency.</param>
    /// <returns>The constructed instance, or <see langword="null"/> when the key has no known type or usable constructor.</returns>
    /// <exception cref="ArgumentException">Throw when a constructor parameter cannot be resolved.</exception>
    /// <exception cref="InvalidOperationException">Throw when a circular constructor dependency is detected.</exception>
    private object? ConstructByInjection(string key)
    {
        if (!m_KeyToType.TryGetValue(key, out var type))
        {
            return null;
        }

        var injectConstructor = InjectCache.GetInjectConstructor(type);
        if (injectConstructor == null)
        {
            return null;
        }

        if (!m_Constructing.Add(key))
        {
            throw new InvalidOperationException($"Circular constructor dependency detected for type '{type}' (key: {key}).");
        }

        try
        {
            var parameters = injectConstructor.Parameters;
            var args = new object[parameters.Length];
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                args[index] = Resolve(parameter.Type, parameter.Key)
                    ?? throw new ArgumentException($"Cannot resolve constructor parameter of type '{parameter.Type}' for '{type}'.");
            }

            return injectConstructor.Constructor.Invoke(args);
        }
        finally
        {
            m_Constructing.Remove(key);
        }
    }

    /// <summary>
    /// Create instances where need to be baked.
    /// </summary>
    private void Bake()
    {
        FlushInstallObject();
        // Resolve multitons.
        foreach (var multiton in m_MultitonCache.Values.SelectMany(static stack => stack))
        {
            this.Inject(multiton);
        }

        // Resolve singletons.
        foreach (var singleton in m_SingletonsCache.Values)
        {
            this.Inject(singleton);
        }
    }

    #endregion

    #region Register/Unregister

    /// <inheritdoc/>
    /// <exception cref="ArgumentException">Throw when the type could be a interface, abstract, static, struct, enum or null type.</exception>
    IBindingDecorator IDependenciesRegistry.Register(Type type)
    {
        // Check type validity.
        var typeChecker = type.CheckType();
        if (typeChecker.IsInterface || typeChecker.IsAbstract)
        {
            throw new ArgumentException($"Invalid type: {type}, it cannot be following type: Interface, Abstract, Static, Null.");
        }

        // Set dirty.
        m_HasFlushed = false;
        var installInfo = new DependencyInfo {Type = type};

        // Append to the queue that wait for registering.
        m_InstallQueue.Enqueue(installInfo);
        return new DependencyDecorator(this, installInfo);
    }

    /// <inheritdoc/>
    INotMultitonBindingDecorator IDependenciesRegistry.Register(object instance)
    {
        if (instance == null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        // Set dirty.
        m_HasFlushed = false;
        var installInfo = new DependencyInfo
        {
            Instance = instance,
            Type     = instance.GetType()
        };

        // Append to the queue that wait for registering.
        m_InstallQueue.Enqueue(installInfo);
        return new DependencyDecorator(this, installInfo);
    }

    /// <inheritdoc/>
    IMultitonBindingDecorator IDependenciesRegistry.Register(object[] instances)
    {
        if (instances == null)
        {
            throw new ArgumentNullException(nameof(instances));
        }

        // Set dirty.
        m_HasFlushed = false;
        var installInfo = new DependencyInfo
        {
            Instances = instances,
            Type      = instances.GetType()
        };

        // Append to the queue that wait for registering.
        m_InstallQueue.Enqueue(installInfo);
        return new DependencyDecorator(this, installInfo);
    }

    /// <summary>
    /// Remove the prototype from the container. When next resolve the type, it'll be new prototype.
    /// </summary>
    internal void UnregisterInstance(string dependencyKey) => m_SingletonsCache.Remove(dependencyKey);

    /// <summary>
    /// Flush all install info and set to the container.
    /// </summary>
    /// <exception cref="ArgumentException">The spawner cannot produce the prototype.</exception>
    private void FlushInstallObject()
    {
        if (m_HasFlushed) return;

        m_HasFlushed = true;

        // Add to container.
        while (m_InstallQueue.TryDequeue(out var installObject))
        {
            RegisterDependenciesInfo(installObject);
            // Register scope info.
            switch (installObject.Scope)
            {
                case Scope.Multiton:
                    RegisterMultiton(installObject);
                    break;
                case Scope.Custom:
                case Scope.Singleton:
                    RegisterSingleton(installObject);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Register multiton with install info.
    /// </summary>
    /// <param name="dependencyObject">The install info witch scope is Multiton.</param>
    private void RegisterMultiton(DependencyInfo dependencyObject)
    {
        if (dependencyObject.Instances != null && dependencyObject.Instances.Any())
        {
            var cache = new Stack<object>(dependencyObject.Instances);
            m_MultitonCache.Add(dependencyObject.Key!, cache);
        }
        else if (dependencyObject.BakedInstances != null && dependencyObject.BakedInstances.Any())
        {
            var cache = new Stack<object>(dependencyObject.BakedInstances);
            m_MultitonCache.Add(dependencyObject.Key!, cache);
        }
    }

    /// <summary>
    /// Register singleton with install info.
    /// </summary>
    /// <param name="dependencyObject">The install info witch scope is Singleton.</param>
    private void RegisterSingleton(DependencyInfo dependencyObject)
    {
        var instance = dependencyObject.Instance;
        if (instance != null)
        {
            m_SingletonsCache.Add(dependencyObject.Key!, instance);
        }
    }

    /// <summary>
    /// Resolve install info and append to cache.
    /// </summary>
    /// <param name="dependencyObject">The install info that need to be resolved.</param>
    private void RegisterDependenciesInfo(DependencyInfo dependencyObject)
    {
        if (dependencyObject.Type == null) throw new ArgumentNullException(nameof(dependencyObject.Type));
        if (string.IsNullOrEmpty(dependencyObject.Key)) dependencyObject.Key = dependencyObject.Type.ToString();

        // Add scope.
        m_ScopeMaps.Add(dependencyObject.Key, dependencyObject.Scope);

        // Remember the concrete type for spawner-less constructor injection.
        m_KeyToType[dependencyObject.Key] = dependencyObject.Type;

        // Set type to key mapping.
        if (!m_ActualTypeMaps.TryGetValue(dependencyObject.Type, out var keys))
        {
            keys = [];
            m_ActualTypeMaps.Add(dependencyObject.Type, keys);
        }

        keys.Add(dependencyObject.Key);

        // Set bind type.
        if (dependencyObject.BindTypes != null)
        {
            foreach (var bind in dependencyObject.BindTypes)
            {
                if (!m_BindingMaps.TryGetValue(bind, out keys))
                {
                    // Create a new collection.
                    keys = new List<string>();
                    m_BindingMaps.Add(bind, keys);
                }

                keys.Add(dependencyObject.Key);
            }
        }

        // Add spawner.
        if (dependencyObject.Spawner != null)
        {
            var spawner = dependencyObject.Spawner;
            m_SpawnerMaps.Add(dependencyObject.Key, spawner);
        }
    }

    #endregion
}