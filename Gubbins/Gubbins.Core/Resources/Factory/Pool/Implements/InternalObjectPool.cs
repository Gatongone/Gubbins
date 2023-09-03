/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/09/03-04:34:41
 * Github: https://github.com/Gatongone
 * Description: Internal object pool.
 */

namespace Gubbins.Resources;

/// <summary>
/// Internal object pool implement.
/// </summary>
/// <typeparam name="T">The type of objects stored in the pool.</typeparam>
internal class InternalObjectPool<T> : IPool<T> where T : new()
{
    private readonly Queue<T> m_Cache = new();

    public T Spawn() => m_Cache.Count > 0 ? m_Cache.Dequeue() : new T();
    
    public void Recycle(T instance) => m_Cache.Enqueue(instance);
}