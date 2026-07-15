#if GUBBINS_ENABLED
using Gubbins.Spawner;

/// <summary>
/// A spawner that creates new instances of type T using the default constructor.
/// </summary>
/// <typeparam name="T">The type of object to spawn. Must have a parameterless constructor.</typeparam>
public class NewableSpawner<T> : ISpawner<T> where T : new()
{
    /// <summary>
    /// Spawns a new instance of type T.
    /// </summary>
    public T Spawn() => new();

    /// <inheritdoc/>
    object ISpawner.Spawn() => Spawn();
}
#endif
