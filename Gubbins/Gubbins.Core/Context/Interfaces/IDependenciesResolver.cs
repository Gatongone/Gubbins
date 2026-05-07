namespace Gubbins.Context;

/// <summary>
/// Describing the dependency resolved ability.
/// </summary>
public interface IDependenciesResolver
{
    /// <summary>
    /// Resolve the type and get the dependency.
    /// </summary>
    /// <param name="type">The type of the dependency.</param>
    /// <param name="key">The only id of the dependency.</param>
    /// <returns>The dependence instance of the <c>type</c> or <c>key</c>.</returns>
    object? Resolve(Type? type, string? key);

    /// <summary>
    /// Resolve all the dependencies of <c>type</c>.
    /// </summary>
    /// <param name="type">The type of the biding dependency.</param>
    /// <returns>The dependencies instances of target <c>type</c>.</returns>
    object?[] ResolveAll(Type type);
}