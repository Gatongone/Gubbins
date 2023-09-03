namespace Gubbins.Resources;

internal class InternalObjectPool<T> : IPool<T> where T : new()
{
    private readonly Queue<T> s_UnsafetyCache = new();
    
    public T Spawn() => s_UnsafetyCache.Count > 0 ? s_UnsafetyCache.Dequeue() : new T();

    public void Recycle(T instance) => s_UnsafetyCache.Enqueue(instance);
}