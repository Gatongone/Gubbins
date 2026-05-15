namespace Gubbins.Context;

/// <summary>
/// The post-processing after the <see cref="DependencyResolverExtensions.Resolve(Gubbins.Context.IDependenciesResolver,object)"/>.
/// </summary>
public interface IResolvePostprocessing
{
    /// <inheritdoc cref="IResolvePostprocessing"/>
    void AfterResolve();
}
