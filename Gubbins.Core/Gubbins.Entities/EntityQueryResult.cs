using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult(TraceablePool<List<ITraceablePool>> pool, Batch<Entity> batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly Batch<Entity> Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}
/// <summary>
/// Query result with a temporary cache.
/// </summary>
/// <param name="pool">Used for caching the temp object in query processing.</param>
public readonly struct EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(TraceablePool<List<ITraceablePool>> pool, (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>, Batch<T16>) batches) : IDisposable
{
    /// <summary>
    /// The batches of entities.
    /// </summary>
    public readonly (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>, Batch<T16>) Batches = batches;

    /// <summary>
    /// Used for caching the temp object in query processing.
    /// </summary>
    public readonly TraceablePool<List<ITraceablePool>> TracingPool = pool;

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var tracers in TracingPool)
        {
            foreach (var tracer in tracers)
            {
                tracer.RecycleAll();
            }
        }

        TracingPool.RecycleAll();
    }
}

public static class EntityQueryResultExtensions
{

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities(this EntityQueryResult result, EntityForeach callback)
    {
        var batches = result.Batches;
        var batchCount = batches.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span = batches[i];
            var elementCount = span.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets(this EntityQueryResult result, SnippetForeach callback)
    {
        var batches = result.Batches;
        var batchCount = batches.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span = batches[i];

            callback(span);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1>(this EntityQueryResult<T1> result, EntityForeach<T1> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1>(this EntityQueryResult<T1> result, SnippetForeach<T1> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];

            callback(span1, span2);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2>(this EntityQueryResult<T1, T2> result, EntityForeach<T1, T2> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2>(this EntityQueryResult<T1, T2> result, SnippetForeach<T1, T2> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];

            callback(span1, span2, span3);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3>(this EntityQueryResult<T1, T2, T3> result, EntityForeach<T1, T2, T3> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3>(this EntityQueryResult<T1, T2, T3> result, SnippetForeach<T1, T2, T3> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];

            callback(span1, span2, span3, span4);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4>(this EntityQueryResult<T1, T2, T3, T4> result, EntityForeach<T1, T2, T3, T4> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4>(this EntityQueryResult<T1, T2, T3, T4> result, SnippetForeach<T1, T2, T3, T4> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];

            callback(span1, span2, span3, span4, span5);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5>(this EntityQueryResult<T1, T2, T3, T4, T5> result, EntityForeach<T1, T2, T3, T4, T5> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5>(this EntityQueryResult<T1, T2, T3, T4, T5> result, SnippetForeach<T1, T2, T3, T4, T5> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];

            callback(span1, span2, span3, span4, span5, span6);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6>(this EntityQueryResult<T1, T2, T3, T4, T5, T6> result, EntityForeach<T1, T2, T3, T4, T5, T6> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6>(this EntityQueryResult<T1, T2, T3, T4, T5, T6> result, SnippetForeach<T1, T2, T3, T4, T5, T6> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];

            callback(span1, span2, span3, span4, span5, span6, span7);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j], ref span13[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12, span13);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j], ref span13[j], ref span14[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12, span13, span14);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j], ref span13[j], ref span14[j], ref span15[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12, span13, span14, span15);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];
            var span16 = batches.Item16[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j], ref span13[j], ref span14[j], ref span15[j], ref span16[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];
            var span16 = batches.Item16[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12, span13, span14, span15, span16);
        }
    }

    /// <summary>
    /// Executes the given action on each entity matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachEntities<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> result, EntityForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];
            var span16 = batches.Item16[i];
            var span17 = batches.Item17[i];
            var elementCount = span1.Length;
            for (var j = 0; j < elementCount; j++)
            {
                callback(in span1[j], ref span2[j], ref span3[j], ref span4[j], ref span5[j], ref span6[j], ref span7[j], ref span8[j], ref span9[j], ref span10[j], ref span11[j], ref span12[j], ref span13[j], ref span14[j], ref span15[j], ref span16[j], ref span17[j]);
            }
        }
    }

    /// <summary>
    /// Executes the given action on each entities snippet matching the given query.
    /// </summary>
    /// <param name="result">The Query result.</param>
    /// <param name="callback">Foreach action.</param>
    public static void ForeachSnippets<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> result, SnippetForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> callback)
    {
        var batches = result.Batches;
        var batchCount = batches.Item1.SegmentCount;
        for (var i = 0; i < batchCount; i++)
        {
            var span1 = batches.Item1[i];
            var span2 = batches.Item2[i];
            var span3 = batches.Item3[i];
            var span4 = batches.Item4[i];
            var span5 = batches.Item5[i];
            var span6 = batches.Item6[i];
            var span7 = batches.Item7[i];
            var span8 = batches.Item8[i];
            var span9 = batches.Item9[i];
            var span10 = batches.Item10[i];
            var span11 = batches.Item11[i];
            var span12 = batches.Item12[i];
            var span13 = batches.Item13[i];
            var span14 = batches.Item14[i];
            var span15 = batches.Item15[i];
            var span16 = batches.Item16[i];
            var span17 = batches.Item17[i];

            callback(span1, span2, span3, span4, span5, span6, span7, span8, span9, span10, span11, span12, span13, span14, span15, span16, span17);
        }
    }
}