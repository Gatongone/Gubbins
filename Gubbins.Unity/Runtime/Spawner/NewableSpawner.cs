namespace Gubbins.Spawner
{
    public class NewableSpawner<T> : ISpawner<T> where T : new()
    {
        public T Spawn() => new();

        object ISpawner.Spawn() => Spawn();
    }
}