using Gubbins.Spawner;

namespace Gubbins.Context;

/// <summary>
/// The information for installing a dependency into the context. It is used to store the information for
/// installing a dependency into the context, and is used to determine whether the dependency is still needed
/// and can be removed from the context when it is not needed anymore.
/// </summary>
[Serializable]
internal struct InstallInfo
{
    /// <summary>
    /// The unique identifier for the dependency.
    /// </summary>
    public string? Key;

    /// <summary>
    /// The instance object for the <c>Singleton</c> or <c>Custom</c> scope.
    /// </summary>
    public object? Instance;

    /// <summary>
    /// The instance objects for the <c>Multiton</c> or <c>Custom</c> scope.
    /// </summary>
    public object[]? Instances;

    /// <summary>
    /// The scope of the dependency.
    /// </summary>
    public Scope Scope;

    /// <summary>
    /// The dependency type.
    /// </summary>
    public Type Type;

    /// <summary>
    /// The binding types of the <c>Type</c> that must could be assignable from.
    /// </summary>
    public HashSet<Type>? Bindings;

    /// <summary>
    /// The factory for producing instance of <c>Type</c>.
    /// </summary>
    public ISpawner? Spawner;

    /// <summary>
    /// The controller for controlling the scope of the dependency. It is used to determine whether the dependency is still needed and can be removed from the context when it is not needed anymore.
    /// </summary>
    public IScopeController? Controller;

    /// <summary>
    /// The number of times the dependency has been resolved. It is used to determine whether the dependency is still needed and can be removed from the context when it is not needed anymore.
    /// </summary>
    public uint BakeCount;
}