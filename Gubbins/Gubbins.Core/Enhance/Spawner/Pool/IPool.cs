using System.Collections;

namespace Gubbins.Enhance;

/// <summary>
/// Represents a pool of objects that can be reused, without a specific type.
/// </summary>
public interface IPool
{
    /// <summary>
    /// Recycles an object by returning it back to the pool.
    /// </summary>
    /// <param name="instance">The object to be recycled.</param>
    void Recycle(object instance);

    /// <summary>
    /// Removes all objects in pool.
    /// </summary>
    void Clear();
}

/// <summary>
/// Represents a pool of objects that can be reused.
/// </summary>
/// <typeparam name="TPoolObject">The type of object stored in the pool.</typeparam>
public interface IPool<TPoolObject> : ISpawner<TPoolObject>, IPool
{
    /// <summary>
    /// Recycles an object by returning it back to the pool.
    /// </summary>
    /// <param name="instance">The object to be recycled.</param>
    void Recycle(TPoolObject instance);
}

/// <summary>
/// Represents a pool of objects that can be traced and recycled.
/// </summary>
public interface ITraceablePool : IEnumerable
{
    /// <summary>
    /// Recycles all objects that came from this pool.
    /// </summary>
    void RecycleAll();
}

/// <summary>
/// Represents a pool of objects that can be traced and recycled, with a specific type.
/// </summary>
/// <typeparam name="TPoolObject">The type of object stored in the pool.</typeparam>
public readonly struct PooledHandle<TPoolObject> : IDisposable
{
    /// <inheritdoc cref="PooledHandle.m_Pool"/>
    private readonly IPool<TPoolObject> m_Pool;

    /// <inheritdoc cref="PooledHandle.m_ArgPool"/>
    private readonly FixedArrayPool<TPoolObject> m_ArgPool;

    /// <inheritdoc cref="PooledHandle.m_PooledObject"/>
    private readonly object m_PooledObject;

    /// <inheritdoc cref="PooledHandle.m_Count"/>
    private readonly int m_Count;

    /// <inheritdoc cref="PooledHandle(IPool, object)"/>
    public PooledHandle(IPool<TPoolObject> pool, TPoolObject obj)
    {
        m_Pool         = pool;
        m_PooledObject = obj!;
        m_ArgPool      = null!;
        m_Count        = 0;
    }

    /// <inheritdoc cref="PooledHandle(IPool, FixedArrayPool{object}, object[], int)"/>
    internal PooledHandle(IPool<TPoolObject> pool, FixedArrayPool<TPoolObject> argPool, TPoolObject[] obj, int count)
    {
        m_Pool         = pool;
        m_ArgPool      = argPool;
        m_PooledObject = obj;
        m_Count        = count;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_ArgPool != null && m_PooledObject is TPoolObject[] pooledObjects)
        {
            for (var i = 0; i < m_Count; i++)
            {
                m_Pool?.Recycle(pooledObjects[i]);
            }
        }
        else if (m_PooledObject is TPoolObject pooledObject)
        {
            m_Pool?.Recycle(pooledObject);
        }
    }
}

/// <summary>
/// Represents a pool of objects that can be traced and recycled, without a specific type.
/// </summary>
public readonly struct PooledHandle : IDisposable
{
    /// <summary>
    /// The pool from which the object(s) were spawned. This is used to recycle the object(s) back to the pool when the handle is disposed.
    /// </summary>
    private readonly IPool m_Pool;

    /// <summary>
    /// The array pool used to rent the array of objects when spawning multiple objects from the pool at once.
    /// This is used to return the rented array back to the pool when the handle is disposed.
    /// </summary>
    private readonly FixedArrayPool<object> m_ArgPool;

    /// <summary>
    /// The object or array of objects that were spawned from the pool. This is used to recycle the object(s) back to the pool when the handle is disposed.
    /// </summary>
    private readonly object m_PooledObject;

    /// <summary>
    /// The number of objects in the array that were spawned from the pool when spawning multiple objects at once.
    /// This is used to determine how many objects to recycle back to the pool when the handle is disposed.
    /// </summary>
    private readonly int m_Count;

    /// <summary>
    /// Internal constructor for creating a pooled handle with a single object. This is used when spawning a single object from the pool.
    /// </summary>
    /// <param name="pool">The pool from which the object was spawned.</param>
    /// <param name="obj">The object that was spawned from the pool.</param>
    public PooledHandle(IPool pool, object obj)
    {
        m_Pool         = pool;
        m_PooledObject = obj!;
        m_ArgPool      = null!;
        m_Count        = 0;
    }

    /// <summary>
    /// Internal constructor for creating a pooled handle with an array of objects. This is used when spawning multiple objects from the pool at once.
    /// </summary>
    /// <param name="pool">The pool from which the objects were spawned.</param>
    /// <param name="argPool">The array pool used to rent the array of objects.</param>
    /// <param name="obj">The array of objects that were spawned from the pool.</param>
    /// <param name="count">The number of objects in the array that were spawned from the pool.</param>
    internal PooledHandle(IPool pool, FixedArrayPool<object> argPool, object[] obj, int count)
    {
        m_Pool         = pool;
        m_ArgPool      = argPool;
        m_PooledObject = obj;
        m_Count        = count;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (m_ArgPool != null && m_PooledObject is object[] pooledObjects)
        {
            for (var i = 0; i < m_Count; i++)
            {
                m_Pool?.Recycle(pooledObjects[i]);
            }
        }
        else
        {
            m_Pool?.Recycle(m_PooledObject);
        }
    }
}

/// <summary>
/// Extension methods for IPool to provide additional functionalities such as prewarming and spawning with handles.
/// </summary>
public static partial class PoolExtensions
{
    /// <summary>
    /// Prewarms the pool by spawning a specified number of objects and returning them in an array.
    /// </summary>
    /// <param name="pool">The pool to be prewarmed.</param>
    /// <typeparam name="TPoolObject">The type of object stored in the pool.</typeparam>
    extension<TPoolObject>(IPool<TPoolObject> pool)
    {
        /// <summary>
        /// Prewarms the pool by spawning a specified number of objects and returning them in an array.
        /// </summary>
        /// <param name="count">The number of objects to spawn for prewarming the pool.</param>
        /// <returns>An array containing the spawned objects for prewarming the pool.</returns>
        public TPoolObject[] Prewarm(int count)
        {
            var results = new TPoolObject[count];
            for (var index = 0; index < count; index++)
            {
                results[index] = pool.Spawn();
            }

            return results;
        }

        /// <summary>
        /// Prewarms the pool by spawning a specified number of objects in parallel and returning them in an array.
        /// </summary>
        /// <param name="count">The number of objects to spawn for prewarming the pool.</param>
        /// <returns>An array containing the spawned objects for prewarming the pool.</returns>
        public TPoolObject[] PrewarmAsync(int count)
        {
            var results = new TPoolObject[count];
            Parallel.For(0, count, index => { results[index] = pool.Spawn(); });
            return results;
        }

        /// <summary>
        /// Spawns an object from the pool and returns a handle that can be used to automatically recycle the object back to the pool when it is no longer needed.
        /// </summary>
        /// <param name="instance">The spawned object from the pool.</param>
        /// <returns>A handle that can be used to automatically recycle the spawned object back to the pool when it is no longer needed.</returns>
        public PooledHandle<TPoolObject> Spawn(out TPoolObject instance)
        {
            instance = pool.Spawn();
            return new PooledHandle<TPoolObject>(pool, instance);
        }

        /// <summary>
        /// Spawns a specified number of objects from the pool and returns a handle that can be used to automatically recycle the objects back to the pool when they are no longer needed.
        /// </summary>
        /// <param name="count">The number of objects to spawn from the pool.</param>
        /// <param name="instance">The span that will contain the spawned objects from the pool.</param>
        /// <returns>A handle that can be used to automatically recycle the spawned objects back to the pool when they are no longer needed.</returns>
        public PooledHandle<TPoolObject> Spawn(int count, out Span<TPoolObject> instance)
        {
            var arrayPool = FixedArrayPool<TPoolObject>.Instance;
            var array = arrayPool.Rent(count);

            for (var i = 0; i < count; i++)
            {
                array[i] = pool.Spawn();
            }

            instance = array.AsSpan(0, count);
            return new PooledHandle<TPoolObject>(pool, arrayPool, array, count);
        }
    }
}