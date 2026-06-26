using System;
using UnityEngine;

namespace Gubbins.Spawner
{
    /// <summary>
    /// A spawner that creates instances of a ScriptableObject type T. If an instance is provided, it will return that instance;
    /// otherwise, it will create a new instance using ScriptableObject.CreateInstance<T>().
    /// </summary>
    /// <typeparam name="T">The type of ScriptableObject to spawn.</typeparam>
    [Serializable]
    public class ScriptableSpawner<T> : ISpawner<T> where T : ScriptableObject
    {
        /// <inheritdoc/>
        public T Spawn() => ScriptableObject.CreateInstance<T>();

        /// <inheritdoc/>
        object ISpawner.Spawn() => Spawn();
    }
}