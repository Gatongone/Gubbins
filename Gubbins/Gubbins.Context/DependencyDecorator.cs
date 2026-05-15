using Gubbins.Spawner;

namespace Gubbins.Context;

/// <summary>
/// The decorator of the dependency info, which is used for setting the scope, key, spawner and bind types of the dependency
/// </summary>
public class DependencyDecorator :
    IBindingDecorator,
    INotMultitonBindingDecorator,
    IMultitonBindingDecorator,
    INotMultitonScopeDecorator,
    IInstanceSpawnerDecorator
{
    /// <summary>
    /// Whether the scope has been set, if not set, the default scope is singleton,
    /// but if the user has set the scope, even if it's custom with a spawner, it won't be changed to singleton by default
    /// </summary>
    private bool m_HasSetScope;

    /// <summary>
    /// The container that the dependency belongs to, used for unregistering the instance when the scope is custom and finished
    /// </summary>
    private readonly ApplicationContext m_Container;

    /// <summary>
    /// The dependency info that the decorator is decorating, used for setting the scope, key, spawner and bind types of the dependency
    /// </summary>
    internal readonly DependencyInfo Info;

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyDecorator"/> class with the specified container and dependency info.
    /// </summary>
    internal DependencyDecorator(ApplicationContext container, DependencyInfo dependencyInfo)
    {
        m_Container = container;
        Info        = dependencyInfo;
    }

    /// <inheritdoc/>
    INotMultitonSpawnerDecorator IScopeDecorator.AsSingleton()
    {
        Info.Scope = Scope.Singleton;
        return this;
    }

    /// <inheritdoc/>
    void IInstanceScopeDecorator.AsSingleton() => Info.Scope = Scope.Singleton;

    /// <inheritdoc/>
    INotMultitonSpawnerDecorator INotMultitonScopeDecorator.AsSingleton()
    {
        Info.Scope = Scope.Singleton;
        return this;
    }

    /// <inheritdoc/>
    INotMultitonSpawnerDecorator INotMultitonScopeDecorator.AsCustom(IScopeController controller)
    {
        m_HasSetScope            =  true;
        Info.Scope               =  Scope.Custom;
        controller.OnScopeFinish += () => m_Container.UnregisterInstance(Info.Key!);
        return this;
    }

    /// <inheritdoc/>
    IInstanceSpawnerDecorator IInstanceScopeDecorator.AsCustom(IScopeController controller)
    {
        m_HasSetScope            =  true;
        Info.Scope               =  Scope.Custom;
        controller.OnScopeFinish += () => m_Container.UnregisterInstance(Info.Key!);
        return this;
    }

    /// <inheritdoc/>
    INotMultitonSpawnerDecorator IScopeDecorator.AsCustom(IScopeController controller)
    {
        m_HasSetScope            =  true;
        Info.Scope               =  Scope.Custom;
        controller.OnScopeFinish += () => m_Container.UnregisterInstance(Info.Key!);
        return this;
    }

    /// <inheritdoc/>
    IMultitonSpawnerDecorator IScopeDecorator.AsMultiton()
    {
        m_HasSetScope = true;
        Info.Scope    = Scope.Multiton;
        return this;
    }

    /// <inheritdoc/>
    IInstanceScopeDecorator INotMultitonKeyDecorator.WithKey(string key)
    {
        Info.Key = key;
        return this;
    }

    /// <inheritdoc/>
    IScopeDecorator IKeyDecorator.WithKey(string key)
    {
        Info.Key = key;
        return this;
    }

    /// <inheritdoc/>
    IMultitonSpawnerDecorator IMultitonKeyDecorator.WithKey(string key)
    {
        Info.Key = key;
        return this;
    }

    /// <inheritdoc/>
    IMultitonBindingDecorator IMultitonBindingDecorator.BindTo(Type[] types)
    {
        Info.BindTypes ??= new List<Type>();
        Info.BindTypes.AddRange(types);
        return this;
    }

    /// <inheritdoc/>
    INotMultitonBindingDecorator INotMultitonBindingDecorator.BindTo(Type[] types)
    {
        Info.BindTypes ??= new List<Type>();
        Info.BindTypes.AddRange(types);
        return this;
    }

    /// <inheritdoc/>
    IBindingDecorator IBindingDecorator.BindTo(Type[] types)
    {
        Info.BindTypes ??= new List<Type>();
        Info.BindTypes.AddRange(types);
        return this;
    }

    /// <inheritdoc/>
    IMultipleBakingDecorator IMultitonSpawnerDecorator.WithSpawner(ISpawner spawner)
    {
        if (!m_HasSetScope) Info.Scope = Scope.Multiton;
        Info.Spawner = spawner;
        return this;
    }

    /// <inheritdoc/>
    IBackingDecorator INotMultitonSpawnerDecorator.WithSpawner(ISpawner spawner)
    {
        if (!m_HasSetScope) Info.Scope = Scope.Singleton;
        Info.Spawner = spawner;
        return this;
    }

    /// <inheritdoc/>
    void IInstanceSpawnerDecorator.WithSpawner(ISpawner spawner)
    {
        if (!m_HasSetScope) Info.Scope = Scope.Singleton;
        Info.Spawner = spawner;
    }

    /// <inheritdoc/>
    object IBackingDecorator.Bake()
    {
        if (!m_HasSetScope) Info.Scope = Scope.Singleton;
        var instance = Info.Spawner?.Spawn() ?? throw new ArgumentNullException();
        Info.Instance = instance;
        return instance;
    }

    /// <inheritdoc/>
    object[] IMultipleBakingDecorator.Bake(uint count)
    {
        if (!m_HasSetScope) Info.Scope = Scope.Multiton;
        var instances = new object[count];
        for (var index = 0; index < count; index++)
        {
            var instance = Info.Spawner?.Spawn();
            instances[index] = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        Info.BakedInstances = instances;
        return instances;
    }
}