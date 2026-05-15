namespace Gubbins.Entities;

/// <summary>
/// Represents an entity in the system with a unique identifier and version.
/// Implements value equality based on the entity's index and version.
/// </summary>
public struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// Entity Global Unique Identifier.
    /// </summary>
    public int Index;

    /// <summary>
    /// Monotonic version used to detect stale entity handles.
    /// </summary>
    public uint Version;

    /// <summary>
    /// Determines whether the current entity is equal to another entity by comparing index and version.
    /// </summary>
    /// <param name="other">The entity to compare with the current entity.</param>
    /// <returns>True if the entities have the same index and version; otherwise, false.</returns>
    public bool Equals(Entity other) => Index == other.Index && Version == other.Version;

    /// <summary>
    /// Determines whether the current entity is equal to the specified object.
    /// </summary>
    /// <param name="obj">The object to compare with the current entity.</param>
    /// <returns>True if the specified object is an Entity with the same index; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    /// <summary>
    /// Returns the hash code for this entity based on its index and version.
    /// </summary>
    /// <returns>A hash code for the current entity.</returns>
    public override int GetHashCode() => HashCode.Combine(Index, Version);
}
