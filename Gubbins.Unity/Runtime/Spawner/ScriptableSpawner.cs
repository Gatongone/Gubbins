using UnityEngine;

namespace Gubbins.Spawner
{
    public class ScriptableSpawner<T> : ISpawner<T> where T : ScriptableObject
    {
        public T Spawn() => ScriptableObject.CreateInstance<T>();

        object ISpawner.Spawn() => Spawn();
    }
}