using Gubbins.Event;

namespace Gubbins.Domain;

/// <summary>
/// Contains domain-specific events that are raised during module lifecycle operations within a domain.
/// These events provide hooks for observing and reacting to module additions and removals.
/// </summary>
public static class DomainEvents
{
    /// <summary>
    /// Event raised before a module is added to a domain.
    /// This event allows subscribers to perform validation or preparation logic before the module addition occurs.
    /// </summary>
    public sealed class BeforeModuleAdd : Event<(IDomain domain, IModule module)>;

    /// <summary>
    /// Event raised after a module has been successfully added to a domain.
    /// This event allows subscribers to perform post-addition logic such as logging or triggering dependent operations.
    /// </summary>
    public sealed class AfterModuleAdded : Event<(IDomain domain, IModule module)>;

    /// <summary>
    /// Event raised before a module is removed from a domain.
    /// This event allows subscribers to perform cleanup or validation logic before the module removal occurs.
    /// </summary>
    public sealed class BeforeModuleRemove : Event<(IDomain domain, IModule module)>;

    /// <summary>
    /// Event raised after a module has been successfully removed from a domain.
    /// This event allows subscribers to perform post-removal logic such as cleanup or triggering dependent operations.
    /// </summary>
    public sealed class AfterModuleRemoved : Event<(IDomain domain, IModule module)>;
}