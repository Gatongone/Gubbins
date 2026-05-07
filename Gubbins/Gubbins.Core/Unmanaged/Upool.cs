using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using Gubbins.Enhance;

namespace Gubbins.Unmanaged;

/// <summary>
/// A specialized pool implementation that manages instances of reference types with persistent pinned GC handles
/// for safe unmanaged interop operations. This pool extends the base Pool functionality by maintaining a cache
/// of GC handles that prevent the garbage collector from moving pooled objects in memory, ensuring they remain
/// accessible to unmanaged code throughout their lifetime in the pool.
/// </summary>
/// <typeparam name="T">The reference type to be pooled. Must be a class type that can be pinned in memory for unmanaged access.</typeparam>
public sealed class Upool<T> : Pool<T> where T : unmanaged
{
    public static readonly Upool<T> Default = new();

    /// <summary>
    /// A thread-safe cache that maintains the association between rented character arrays and their corresponding
    /// pinned GC handles. This cache ensures that each array has a persistent pinned handle for unmanaged interop
    /// operations, preventing the garbage collector from moving the array in memory while it's being used by
    /// unmanaged code. The cache persists handles across multiple rent/return cycles to avoid the overhead
    /// of repeatedly creating and destroying GC handles for the same arrays.
    /// </summary>
    private readonly ConcurrentDictionary<T, GCHandle> m_HandleCache = new();

    /// <inheritdoc />
    public override T Spawn() => Spawn(out _);

    /// <summary>
    /// Spawns an instance from the pool and provides its associated pinned GC handle for unmanaged interop operations.
    /// If the instance doesn't have an existing handle in the cache, a new pinned handle is created and cached.
    /// </summary>
    /// <param name="handle">When this method returns, contains the pinned GC handle associated with the spawned instance.</param>
    /// <returns>An instance of type T from the pool with a guaranteed pinned GC handle for safe unmanaged access.</returns>
    public T Spawn(out GCHandle handle)
    {
        var result = base.Spawn();
        if (!m_HandleCache.TryGetValue(result, out handle))
        {
            handle                = GCHandle.Alloc(result, GCHandleType.Pinned);
            m_HandleCache[result] = handle;
        }

        return result;
    }

    /// <inheritdoc />
    public override void Recycle(T instance)
    {
        if (!m_HandleCache.TryGetValue(instance, out var handle))
        {
            handle                  = GCHandle.Alloc(instance, GCHandleType.Pinned);
            m_HandleCache[instance] = handle;
        }

        base.Recycle(instance);
    }
}