using System.Runtime.CompilerServices;

namespace Gubbins.Spawner
{
    public class UninitializedSpawner<T> : ISpawner<T>
    {
        public T Spawn()
        {
            return (T) RuntimeHelpers.GetUninitializedObject(typeof(T));
        }

        object ISpawner.Spawn() => Spawn();
    }
}