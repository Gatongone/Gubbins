namespace Gubbins.Entities;

/// <summary>
/// Represents a record that contains an entity along with its storage location information within the entity-component system.
/// This structure provides a complete reference to an entity, including which chunk it resides in and its position within that chunk.
/// </summary>
internal struct EntityRecord
{
    /// <summary>
    /// The entity instance that this record represents.
    /// </summary>
    public Entity Entity;

    /// <summary>
    /// The chunk where the entity's component data is stored.
    /// </summary>
    public Chunk Chunk;

    /// <summary>
    /// The zero-based index position of the entity within the chunk's internal storage arrays.
    /// </summary>
    public int IndexInChunk;
}