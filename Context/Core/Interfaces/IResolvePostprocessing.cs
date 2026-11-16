namespace Gubbins.Context;

/// <summary>
/// The pre-processing before the <see cref="DependencyResolverExtensions.Resolve(Gubbins.Context.IDependenciesResolver,object)"/>.
/// </summary>
public interface IResolvePreprocessing
{
    /// <inheritdoc cref="IResolvePreprocessing"/>
    void BeforeResolve();
}