namespace Gubbins.Spawner;

/// <summary>
/// Represents an object that can be reset to its initial state, allowing it to be reused without the need for reinitialization.
/// </summary>
/// <remarks>
/// Duck typing is used to allow any object that implements a compatible Reset method to be treated as an IResetable,
/// enabling flexible and reusable code without the need for explicit interface implementation.
/// </remarks>
[Duck]
public interface IResetable
{
    /// <summary>
    /// Resets the object to its initial state, allowing it to be reused without the need for reinitialization.
    /// </summary>
    void Reset();
}