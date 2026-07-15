#if GUBBINS_ENABLED
namespace Gubbins.Entities
{
    /// <summary>
    /// Marker interface for components in the entities architecture.
    /// Components are data containers that hold the state of an entity,
    /// and they are used to define the properties and behaviors of entities in a decoupled manner.
    /// The implementation should be a unmanaged struct.
    /// </summary>
    public interface IComponent { }
}
#endif
