using System.Collections;
using System.Collections.Concurrent;
using Gubbins.Collection;
using Gubbins.Resource;

namespace Gubbins.Enhance;

public class Pool<T> : IPool<T>
{
    public static readonly Pool<T> Default = new();

    private readonly ConcurrentQueue<T> m_Cache = new();

    public virtual void PrewarmAsync(int count) => Parallel.For(0, count, _ => { Spawn(); });

    public virtual void Prewarm(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Spawn();
        }
    }

    public virtual T Spawn()
    {
        if (m_Cache.TryDequeue(out var instance))
            return instance;
        instance = CreateInstance();
        return instance;
    }

    protected virtual T CreateInstance() => Activator.CreateInstance<T>();

    /// <summary>
    /// Recycles an instance back into the pool.
    /// </summary>
    /// <param name="instance">Recycled target.</param>
    /// <remarks>
    /// The <c>instance</c> can be a type that implicated implements (duck type) <see cref="IResetable"/> or <see cref="IClearable"/> to be recycled.
    /// </remarks>
    public virtual void Recycle(T instance)
    {
        if (instance == null) return;
        if (instance is IResetable reseter || instance.CanBeResetable(out reseter))
        {
            reseter.Reset();
        }
        else if (instance is IClearable clearer || instance.CanBeClearable(out clearer))
        {
            clearer.Clear();
        }

        m_Cache.Enqueue(instance);
    }

    protected virtual void OnInstanceRemoved(T instance)
    {
        if (instance is IReleasable releasable)
            releasable.Release();
    }

    /// <inheritdoc />
    void IPool.Recycle(object instance)
    {
        if (instance is T target) Recycle(target);
    }

    public void Clear()
    {
        while (m_Cache.Count > 0)
        {
            if (m_Cache.TryDequeue(out var instance))
                OnInstanceRemoved(instance);
        }
    }
}

public class TraceablePool<T> : IPool<T>, ITraceablePool, IEnumerable<T>
{
    private readonly Pool<T>    m_Pool    = new();
    private readonly HashSet<T> m_Tracing = new();

    public static readonly TraceablePool<T> Default = new();

    /// <inheritdoc />
    public T Spawn()
    {
        var instance = m_Pool.Spawn();
        m_Tracing.Add(instance);
        return instance;
    }

    /// <inheritdoc />
    void IPool.Recycle(object instance)
    {
        if (instance is T target) Recycle(target);
    }

    /// <inheritdoc />
    public void Recycle(T instance)
    {
        m_Pool.Recycle(instance);
        m_Tracing.Remove(instance);
    }

    /// <inheritdoc />
    public void Clear()
    {
        RecycleAll();
        m_Pool.Clear();
    }

    /// <inheritdoc />
    public void RecycleAll()
    {
        foreach (var instance in m_Tracing)
        {
            m_Pool.Recycle(instance);
        }
    }

    public HashSet<T>.Enumerator GetEnumerator() => m_Tracing.GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}