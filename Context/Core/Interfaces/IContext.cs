namespace Gubbins.Context;

/// <summary>
/// Represents a read-only context that provides dependency resolution and access to its parent context.
/// </summary>
public interface IReadOnlyContext : IDependenciesResolver, IDisposable
{
    /// <summary>
    /// The parent context.
    /// </summary>
    /// <remarks>
    /// When resolve failed, the context would resolve from its parent context.
    /// </remarks>
    IReadOnlyContext? Parent { get; }
}

/// <summary>
/// Represents a context that provides dependency resolution, registration, and access to its parent context.
/// </summary>
public interface IContext : IReadOnlyContext, IDependenciesRegistry;