using System;
using System.Runtime.CompilerServices;

namespace Gubbins.Spawner
{
    /// <summary>
    /// A spawner that creates uninitialized instances of a given type T.
    /// </summary>
    /// <typeparam name="T">The type of object to spawn.</typeparam>
    [Serializable]
    public class UninitializedSpawner<T> : ISpawner<T>
    {
        /// <inheritdoc/>
        public T Spawn() => (T) RuntimeHelpers.GetUninitializedObject(typeof(T));

        /// <inheritdoc/>
        object ISpawner.Spawn() => Spawn();
    }
}