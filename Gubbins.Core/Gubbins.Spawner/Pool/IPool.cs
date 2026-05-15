using System.Collections;

namespace Gubbins.Spawner;

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