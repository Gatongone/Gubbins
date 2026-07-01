namespace Gubbins.Enhance;

/// <summary>
/// Represents a unit type that signifies the absence of a meaningful value.
/// This type is commonly used in functional programming scenarios where a method
/// needs to return something but has no meaningful data to return.
/// </summary>
public struct Unit : IEquatable<Unit>, IComparer<Unit>, IComparable<Unit>
{
    /// <summary>
    /// Gets a singleton instance of the Unit type.
    /// </summary>
    public static readonly Unit Instance = new();

    /// <summary>
    /// Determines whether the specified Unit instance is equal to the current Unit instance.
    /// Since all Unit instances are considered equal, this method always returns true.
    /// </summary>
    /// <param name="other">The Unit instance to compare with the current instance.</param>
    /// <returns>Always returns true since all Unit instances are considered equal.</returns>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether the specified object is equal to the current Unit instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current Unit instance.</param>
    /// <returns>True if the specified object is a Unit instance; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is Unit other && Equals(other);

    /// <summary>
    /// Returns the hash code for this Unit instance.
    /// Since all Unit instances are considered equal, they all return the same hash code.
    /// </summary>
    /// <returns>Always returns 0 as the hash code for all Unit instances.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Compares two Unit instances. Since all Unit instances are considered equal,
    /// this method always returns 1 to indicate they are equivalent.
    /// </summary>
    /// <param name="x">The first Unit instance to compare.</param>
    /// <param name="y">The second Unit instance to compare.</param>
    /// <returns>Always returns 1 since all Unit instances are considered equal.</returns>
    public int Compare(Unit x, Unit y) => 1;

    /// <summary>
    /// Compares the current Unit instance with another Unit instance.
    /// Since all Unit instances are considered equal, this method always returns 1.
    /// </summary>
    /// <param name="other">The Unit instance to compare with the current instance.</param>
    /// <returns>Always returns 1 since all Unit instances are considered equal.</returns>
    public int CompareTo(Unit other) => 1;
}