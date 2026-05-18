using System.Collections;
using System.Collections.Concurrent;

namespace Gubbins.Spawner;

/// <summary>
/// Provides a concurrent object pool for instances of <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The pooled instance type.</typeparam>
public class Pool<T> : IPool<T>
{
    /// <summary>
    /// Gets the shared default pool instance.
    /// </summary>
    public static readonly Pool<T> Default = new();

    private readonly ConcurrentQueue<T> m_Cache = new();

    /// <summary>
    /// Prewarms the pool in parallel by spawning <paramref name="count"/> instances.
    /// </summary>
    /// <param name="count">The number of instances to create and cache.</param>
    public virtual void PrewarmAsync(int count) => Parallel.For(0, count, _ => { Spawn(); });

    /// <summary>
    /// Prewarms the pool sequentially by spawning <paramref name="count"/> instances.
    /// </summary>
    /// <param name="count">The number of instances to create and cache.</param>
    public virtual void Prewarm(int count)
    {
        for (var i = 0; i < count; i++)
        {
            Spawn();
        }
    }

    /// <summary>
    /// Gets an instance from the pool, creating one when the cache is empty.
    /// </summary>
    /// <returns>A pooled or newly created instance.</returns>
    public virtual T Spawn()
    {
        if (m_Cache.TryDequeue(out var instance))
            return instance;
        instance = CreateInstance();
        return instance;
    }

    /// <summary>
    /// Creates a new pooled instance.
    /// </summary>
    /// <returns>A newly created <typeparamref name="T"/> instance.</returns>
    protected virtual T CreateInstance() => Activator.CreateInstance<T>();

    /// <summary>
    /// Recycles an instance back into the pool.
    /// </summary>
    /// <param name="instance">The instance to recycle.</param>
    /// <remarks>
    /// If supported, the instance is reset/cleared via <see cref="IResetable"/>, or have a <c>Clear</c> method called.
    /// or duck-typed equivalents before being re-queued.
    /// </remarks>
    public virtual void Recycle(T instance)
    {
        if (instance == null) return;

        if (instance is IResetable directReseter)
        {
            directReseter.Reset();
        }
        else if (instance is IList directList)
        {
            directList.Clear();
        }
        else if (instance is IDictionary directDictionary)
        {
            directDictionary.Clear();
        }
        else if (Duck.Like(instance, out IResetable duckReseter, out var resetable))
        {
            duckReseter.Reset();
            resetable.Dispose();
        }
        else if (Duck.Like(instance, out IClearable duckClearer, out resetable))
        {
            duckClearer.Clear();
            resetable.Dispose();
        }

        m_Cache.Enqueue(instance);
    }

    /// <summary>
    /// Called when an instance is removed permanently from the pool cache.
    /// </summary>
    /// <param name="instance">The removed instance.</param>
    protected virtual void OnInstanceRemoved(T instance) { }

    /// <inheritdoc />
    void IPool.Recycle(object instance)
    {
        if (instance is T target) Recycle(target);
    }

    /// <summary>
    /// Empties the pool cache and releases removed instances when applicable.
    /// </summary>
    public void Clear()
    {
        while (m_Cache.Count > 0)
        {
            if (m_Cache.TryDequeue(out var instance))
                OnInstanceRemoved(instance);
        }
    }
}

/// <summary>
/// Wraps <see cref="Pool{T}"/> with tracking of currently spawned instances.
/// </summary>
/// <typeparam name="T">The pooled instance type.</typeparam>
public class TraceablePool<T> : IPool<T>, ITraceablePool, IEnumerable<T>
{
    private readonly Pool<T>    m_Pool    = new();
    private readonly HashSet<T> m_Tracing = new();

    /// <summary>
    /// Gets the shared default traceable pool instance.
    /// </summary>
    public static readonly TraceablePool<T> Default = new();

    /// <summary>
    /// Spawns an instance and marks it as tracked.
    /// </summary>
    /// <returns>The spawned instance.</returns>
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

    /// <summary>
    /// Recycles a tracked instance and removes it from tracking.
    /// </summary>
    /// <param name="instance">The instance to recycle.</param>
    /// <inheritdoc />
    public void Recycle(T instance)
    {
        if (!m_Tracing.Remove(instance))
            return;

        m_Pool.Recycle(instance);
    }

    /// <summary>
    /// Recycles all tracked instances and clears the underlying pool cache.
    /// </summary>
    /// <inheritdoc />
    public void Clear()
    {
        RecycleAll();
        m_Pool.Clear();
    }

    /// <summary>
    /// Recycles all currently tracked spawned instances.
    /// </summary>
    /// <inheritdoc />
    public void RecycleAll()
    {
        foreach (var instance in m_Tracing)
        {
            m_Pool.Recycle(instance);
        }

        m_Tracing.Clear();
    }

    /// <summary>
    /// Returns an enumerator over currently tracked instances.
    /// </summary>
    public HashSet<T>.Enumerator GetEnumerator() => m_Tracing.GetEnumerator();

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Duck]
file interface IClearable
{
    /// <summary>
    /// Clears the state of the object, preparing it for reuse.
    /// </summary>
    void Clear();
}