namespace Gubbins.Context;

/// <summary>
/// Dependency installer with registry.
/// </summary>
public interface IDependenciesInstaller
{
    /// <summary>
    /// Install with dependencies registry.
    /// </summary>
    /// <param name="registry"><inheritdoc cref="IDependenciesRegistry"/></param>
    void Install(IDependenciesRegistry registry);
}