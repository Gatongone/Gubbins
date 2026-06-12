using System;
using UnityEngine;

namespace Gubbins.Spawner
{
    [Serializable]
    public class ScriptableSpawner<T> : ISpawner<T> where T : ScriptableObject
    {
        public T Instance;

        public T Spawn() => Instance == null ? ScriptableObject.CreateInstance<T>() : Instance;

        object ISpawner.Spawn() => Spawn();
    }
}