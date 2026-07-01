using System;

namespace Gubbins.Spawner
{
    /// <summary>
    /// A generic spawner that creates new instances of a type T that has a parameterless constructor.
    /// </summary>
    [Serializable]
    public class NewableSpawner<T> : ISpawner<T> where T : new()
    {
        /// <summary>
        /// Spawns a new instance of type T.
        /// </summary>
        public T Spawn() => new();

        /// <inheritdoc/>
        object ISpawner.Spawn() => Spawn();
    }
}