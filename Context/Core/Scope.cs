namespace Gubbins.Context;

/// <summary>
/// The scope of the context. It determines how the context will be created and destroyed.
/// </summary>
public enum Scope
{
    /// <summary>
    /// A new context will be created for each request. This is the default scope.
    /// </summary>
    Multiton,

    /// <summary>
    /// A single context will be created and shared for all requests. The context will be destroyed when the <see cref="IContext"/> exits.
    /// </summary>
    Singleton,

    /// <summary>
    /// A new context will be created for each request, but the context will be destroyed when the request is completed.
    /// The destroyed request will be determined by <see cref="IScopeController"/>, which can be implemented by the user to control the lifecycle of the context.
    /// This is useful for web applications where each request is handled by a separate thread.
    /// </summary>
    Custom
}