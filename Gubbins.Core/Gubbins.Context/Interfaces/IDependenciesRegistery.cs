namespace Gubbins.Context;

/// <summary>
/// Describing the dependency registered ability.
/// </summary>
public interface IDependenciesRegistry
{
    /// <summary>
    /// Register type.
    /// </summary>
    /// <param name="type">Dependency type.</param>
    /// <returns>Register options.</returns>
    /// <exception cref="ArgumentException">Throw when the register type is value type.</exception>
    IBindingDecorator Register(Type type);

    /// <summary>
    /// Register the instance as singleton or custom scope.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns>Register options.</returns>
    INotMultitonBindingDecorator Register(object instance);

    /// <summary>
    /// Register the instance as multiton scope.
    /// </summary>
    /// <param name="instances"></param>
    /// <returns>Register options.</returns>
    IMultitonBindingDecorator Register(object[] instances);
}