using System;

namespace Gubbins.Spawner
{
    [Serializable]
    public class NewableSpawner<T> : ISpawner<T> where T : new()
    {
        public T Spawn() => new();

        object ISpawner.Spawn() => Spawn();
    }
}