using Gubbins.Spawner;

namespace Gubbins.Collections;

/// <summary>
/// Represents a collection that can be cleared, allowing it to be reused without the need for reinitialization.
/// </summary>
/// <remarks>
/// Duck typing is used to allow any collection type that implements a compatible Clear method to be treated as an IClearable,
/// enabling flexible and reusable code without the need for explicit interface implementation.
/// </remarks>
[Duck]
public interface IClearable
{
    /// <summary>
    /// Clears the collection, removing all items and resetting it to its initial state, allowing it to be reused without the need for reinitialization.
    /// </summary>
    void Clear();
}