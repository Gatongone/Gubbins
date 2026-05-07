namespace Gubbins.Enhance;

/// <summary>
/// Represents a spawner that can create instances of specified type.
/// allowing for flexibility in how objects are created and managed within the context of the application.
/// </summary>
/// <typeparam name="TProduct">The type of the product.</typeparam>
public interface ISpawner<out TProduct>
{
    /// <summary>
    /// Get a instance of the specified type.
    /// </summary>
    /// <returns>An instance of the specified type.</returns>
    TProduct Spawn();
}

/// <summary>
/// Represents a spawner that can create instances of objects.
/// This interface defines a contract for spawning objects,
/// allowing for flexibility in how objects are created and managed within the context of the application.
/// </summary>
public interface ISpawner
{
    /// <summary>
    /// Spawns an instance of an object.
    /// </summary>
    /// <returns>The spawned object instance.</returns>
    object? Spawn();
}