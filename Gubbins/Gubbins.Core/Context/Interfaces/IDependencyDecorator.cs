using Gubbins.Enhance;

namespace Gubbins.Context;

#region Baking decorator

/// <summary>
/// Prewarm the instance by baking it, and return the baked result.
/// This is used for multiton, which needs to create multiple instances at once, so that we can bake them at once to improve performance.
/// </summary>
public interface IMultipleBakingDecorator
{
    /// <summary>
    /// Prewarm the instance by baking it, and return the baked result.
    /// </summary>
    /// <param name="count">Number of instances to bake.</param>
    /// <returns>The baked result.</returns>
    object[] Bake(uint count);
}

/// <summary>
/// Prewarm the instance by baking it, and return the baked result.
/// </summary>
public interface IBackingDecorator
{
    /// <summary>
    /// Prewarm the instance by baking it, and return the baked result.
    /// </summary>
    /// <returns>The baked result.</returns>
    object Bake();
}

#endregion

#region Spawner decorator

/// <summary>
/// Prewarm the instance by baking it, and return the baked result.
/// </summary>
public interface INotMultitonSpawnerDecorator : IBackingDecorator
{
    /// <summary>
    /// Prewarm the instance by baking it, and return the baked result.
    /// </summary>
    /// <param name="spawner">The spawner to use for baking the instance.</param>
    /// <returns>The baked result.</returns>
    IBackingDecorator WithSpawner(ISpawner spawner);
}

/// <summary>
/// Prewarm the instance by baking it, and return the baked result.
/// </summary>
public interface IMultitonSpawnerDecorator : IMultipleBakingDecorator
{
    /// <summary>
    /// Prewarm the instance by baking it, and return the baked result.
    /// </summary>
    /// <param name="spawner">The spawner to use for baking the instance.</param>
    /// <returns>The baked result.</returns>
    IMultipleBakingDecorator WithSpawner(ISpawner spawner);
}

/// <summary>
/// Prewarm the instance by baking it, and return the baked result.
/// </summary>
public interface IInstanceSpawnerDecorator
{
    /// <summary>
    /// Prewarm the instance by baking it, and return the baked result.
    /// </summary>
    /// <param name="spawner">The spawner to use for baking the instance.</param>
    void WithSpawner(ISpawner spawner);
}

#endregion

#region Scope decorator

/// <summary>
/// Set the scope of the instance to singleton or custom, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IInstanceScopeDecorator
{
    /// <summary>
    /// Set the scope of the instance to singleton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    void AsSingleton();

    /// <summary>
    /// Set the scope of the instance to custom, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="controller">The scope controller to use for the custom scope.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IInstanceSpawnerDecorator AsCustom(IScopeController controller);
}

/// <summary>
/// Set the scope of the instance to singleton or custom, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface INotMultitonScopeDecorator
{
    /// <summary>
    /// Set the scope of the instance to singleton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    INotMultitonSpawnerDecorator AsSingleton();

    /// <summary>
    /// Set the scope of the instance to custom, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="controller">The scope controller to use for the custom scope.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    INotMultitonSpawnerDecorator AsCustom(IScopeController controller);
}

/// <summary>
/// Set the scope of the instance to singleton, custom or multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IScopeDecorator
{
    /// <summary>
    /// Set the scope of the instance to singleton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    INotMultitonSpawnerDecorator AsSingleton();

    /// <summary>
    /// Set the scope of the instance to custom, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="controller">The scope controller to use for the custom scope.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    INotMultitonSpawnerDecorator AsCustom(IScopeController controller);

    /// <summary>
    /// Set the scope of the instance to multiton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IMultitonSpawnerDecorator AsMultiton();
}

#endregion

#region Key decorator

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface INotMultitonKeyDecorator : IInstanceScopeDecorator
{
    /// <summary>
    /// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="key">The key to use for the multiton instance.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IInstanceScopeDecorator WithKey(string key);
}

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IMultitonKeyDecorator : IMultitonSpawnerDecorator
{
    /// <summary>
    /// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="key">The key to use for the multiton instance.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IMultitonSpawnerDecorator WithKey(string key);
}

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IKeyDecorator : IScopeDecorator, INotMultitonSpawnerDecorator, IMultipleBakingDecorator
{
    /// <summary>
    /// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="key">The key to use for the multiton instance.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IScopeDecorator WithKey(string key);
}

#endregion

#region Binding decorator

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface INotMultitonBindingDecorator : INotMultitonKeyDecorator
{
    /// <summary>
    /// Bind the instance to the specified types, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="types">The types to bind the instance to.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    INotMultitonBindingDecorator BindTo(params Type[] types);
}

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IMultitonBindingDecorator : IMultitonKeyDecorator
{
    /// <summary>
    /// Bind the instance to the specified types, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="types">The types to bind the instance to.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IMultitonBindingDecorator BindTo(params Type[] types);
}

/// <summary>
/// Set the key of the instance for multiton, and return the corresponding spawner decorator for further configuration if needed.
/// </summary>
public interface IBindingDecorator : IKeyDecorator
{
    /// <summary>
    /// Bind the instance to the specified types, and return the corresponding spawner decorator for further configuration if needed.
    /// </summary>
    /// <param name="types">The types to bind the instance to.</param>
    /// <returns>The corresponding spawner decorator for further configuration if needed.</returns>
    IBindingDecorator BindTo(params Type[] types);
}

#endregion