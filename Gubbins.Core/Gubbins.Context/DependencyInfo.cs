using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// The IOC component definition.
/// </summary>
internal class DependencyInfo
{
    /// <summary>
    /// The unique identifier for the dependency.
    /// </summary>
    internal string? Key;

    /// <summary>
    /// Lifecycle scope of the dependence instance in the IOC container.
    /// </summary>
    internal Scope Scope;

    /// <summary>
    /// The dependency type.
    /// </summary>
    internal Type? Type;

    /// <summary>
    /// The binding types of the <c>Type</c> that must could be assignable from.
    /// </summary>
    internal List<Type>? BindTypes;

    /// <summary>
    /// The instance object for the <c>Singleton</c> or <c>Custom</c> scope.
    /// </summary>
    internal object? Instance;

    /// <summary>
    /// The instance objects for the <c>Multiton</c> or <c>Custom</c> scope.
    /// </summary>
    internal IEnumerable<object>? Instances;

    /// <summary>
    /// The factory for producing instance of <c>Type</c>.
    /// </summary>
    internal ISpawner? Spawner;

    /// <summary>
    /// The baked instance objects for the <c>Multiton</c> or <c>Custom</c> scope.
    /// </summary>
    public IEnumerable<object>? BakedInstances;
}