namespace Gubbins.Context;

/// <summary>
/// ApplicationContext for the dependencies.
/// </summary>
/// <remarks>
/// Basically, the context is a dependencies installer collection and IOC container proxy
/// that provides configuration for an application.
/// </remarks>
public interface IContext : IDependenciesResolver, IDisposable
{
    /// <summary>
    /// The parent context.
    /// </summary>
    /// <remarks>
    /// When resolve failed, the context would resolve from its parent context.
    /// </remarks>
    IContext? Parent { get; }
}