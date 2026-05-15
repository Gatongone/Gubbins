namespace Gubbins.Context;

/// <summary>
/// Controller that controls the lifecycle of an prototype within the application context.
/// </summary>
public interface IScopeController
{
    /// <summary>
    /// Remove the prototype from current application context.
    /// </summary>
    event Action OnScopeFinish;
}