namespace Gubbins.Entities;

/// <summary>
/// Represents an entity in the system with a unique identifier and validity state.
/// Implements value equality based on the entity's index.
/// </summary>
public struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// Entity Global Unique Identifier.
    /// </summary>
    public int Index;

    /// <summary>
    /// Whether the entity is valid.
    /// </summary>
    public bool Valid;

    /// <summary>
    /// Determines whether the current entity is equal to another entity by comparing their indices.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>True if the entities have the same index; otherwise, false.</returns>
    public bool Equals(Entity other) => Index == other.Index;

    /// <summary>
    /// Determines whether the current entity is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the specified object is an Entity with the same index; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    /// <summary>
    /// Returns the hash code for this entity based on its index.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => Index;
}
