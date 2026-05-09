
using Gubbins.Enhance;

namespace Gubbins.Entities;

/// <summary>
/// Query entity with context.
/// </summary>
/// <typeparam name="TResult">Query result.</typeparam>
public interface IEntityQueryHandle<TResult>
{
    /// <summary>
    /// Query entities.
    /// </summary>
    /// <typeparam name="TResult">Query result.</typeparam>
    /// <returns></returns>
    TResult Query();
}


/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle(IReadOnlyList<IArchetype> archetypes, EntityQueryContext context) : IEntityQueryHandle<EntityQueryResult>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult Query()
    {
        Batch<Entity> result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;


        var memories0 = pool0.Spawn();


        handles.Add(pool0);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                break;
            }
        }

        result = new Batch<Entity>(memories0);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1> context) : IEntityQueryHandle<EntityQueryResult<T1>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1> Query()
    {
        (Batch<Entity>, Batch<T1>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2> context) : IEntityQueryHandle<EntityQueryResult<T1, T2>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;
        var pool12 = TraceablePool<List<Snippet<T12>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();
        var memories12 = pool12.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);
        handles.Add(pool12);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                archetype.GetComponents(memories12);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);
        result.Item13 = new Batch<T12>(memories12);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;
        var pool12 = TraceablePool<List<Snippet<T12>>>.Default;
        var pool13 = TraceablePool<List<Snippet<T13>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();
        var memories12 = pool12.Spawn();
        var memories13 = pool13.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);
        handles.Add(pool12);
        handles.Add(pool13);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                archetype.GetComponents(memories12);
                archetype.GetComponents(memories13);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);
        result.Item13 = new Batch<T12>(memories12);
        result.Item14 = new Batch<T13>(memories13);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;
        var pool12 = TraceablePool<List<Snippet<T12>>>.Default;
        var pool13 = TraceablePool<List<Snippet<T13>>>.Default;
        var pool14 = TraceablePool<List<Snippet<T14>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();
        var memories12 = pool12.Spawn();
        var memories13 = pool13.Spawn();
        var memories14 = pool14.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);
        handles.Add(pool12);
        handles.Add(pool13);
        handles.Add(pool14);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                archetype.GetComponents(memories12);
                archetype.GetComponents(memories13);
                archetype.GetComponents(memories14);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);
        result.Item13 = new Batch<T12>(memories12);
        result.Item14 = new Batch<T13>(memories13);
        result.Item15 = new Batch<T14>(memories14);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;
        var pool12 = TraceablePool<List<Snippet<T12>>>.Default;
        var pool13 = TraceablePool<List<Snippet<T13>>>.Default;
        var pool14 = TraceablePool<List<Snippet<T14>>>.Default;
        var pool15 = TraceablePool<List<Snippet<T15>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();
        var memories12 = pool12.Spawn();
        var memories13 = pool13.Spawn();
        var memories14 = pool14.Spawn();
        var memories15 = pool15.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);
        handles.Add(pool12);
        handles.Add(pool13);
        handles.Add(pool14);
        handles.Add(pool15);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                archetype.GetComponents(memories12);
                archetype.GetComponents(memories13);
                archetype.GetComponents(memories14);
                archetype.GetComponents(memories15);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);
        result.Item13 = new Batch<T12>(memories12);
        result.Item14 = new Batch<T13>(memories13);
        result.Item15 = new Batch<T14>(memories14);
        result.Item16 = new Batch<T15>(memories15);

        return new (pool, result);
    }
}

/// <summary>
/// Query entities with specified components.
/// </summary>
public readonly struct EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(IReadOnlyList<IArchetype> archetypes, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> context) : IEntityQueryHandle<EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>
{
    /// <summary>
    /// Entity collections.
    /// </summary>
    private readonly IReadOnlyList<IArchetype> m_Archetypes = archetypes;

    /// <summary>
    /// Query context.
    /// </summary>
    private readonly EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> m_Context = context;

    /// <summary>
    /// Query entities.
    /// </summary>
    public EntityQueryResult<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Query()
    {
        (Batch<Entity>, Batch<T1>, Batch<T2>, Batch<T3>, Batch<T4>, Batch<T5>, Batch<T6>, Batch<T7>, Batch<T8>, Batch<T9>, Batch<T10>, Batch<T11>, Batch<T12>, Batch<T13>, Batch<T14>, Batch<T15>, Batch<T16>) result;
        var pool = TraceablePool<List<ITraceablePool>>.Default;

        var handles = pool.Spawn();

        var pool0 = TraceablePool<List<Snippet<Entity>>>.Default;
        var pool1 = TraceablePool<List<Snippet<T1>>>.Default;
        var pool2 = TraceablePool<List<Snippet<T2>>>.Default;
        var pool3 = TraceablePool<List<Snippet<T3>>>.Default;
        var pool4 = TraceablePool<List<Snippet<T4>>>.Default;
        var pool5 = TraceablePool<List<Snippet<T5>>>.Default;
        var pool6 = TraceablePool<List<Snippet<T6>>>.Default;
        var pool7 = TraceablePool<List<Snippet<T7>>>.Default;
        var pool8 = TraceablePool<List<Snippet<T8>>>.Default;
        var pool9 = TraceablePool<List<Snippet<T9>>>.Default;
        var pool10 = TraceablePool<List<Snippet<T10>>>.Default;
        var pool11 = TraceablePool<List<Snippet<T11>>>.Default;
        var pool12 = TraceablePool<List<Snippet<T12>>>.Default;
        var pool13 = TraceablePool<List<Snippet<T13>>>.Default;
        var pool14 = TraceablePool<List<Snippet<T14>>>.Default;
        var pool15 = TraceablePool<List<Snippet<T15>>>.Default;
        var pool16 = TraceablePool<List<Snippet<T16>>>.Default;

        var memories0 = pool0.Spawn();
        var memories1 = pool1.Spawn();
        var memories2 = pool2.Spawn();
        var memories3 = pool3.Spawn();
        var memories4 = pool4.Spawn();
        var memories5 = pool5.Spawn();
        var memories6 = pool6.Spawn();
        var memories7 = pool7.Spawn();
        var memories8 = pool8.Spawn();
        var memories9 = pool9.Spawn();
        var memories10 = pool10.Spawn();
        var memories11 = pool11.Spawn();
        var memories12 = pool12.Spawn();
        var memories13 = pool13.Spawn();
        var memories14 = pool14.Spawn();
        var memories15 = pool15.Spawn();
        var memories16 = pool16.Spawn();

        handles.Add(pool0);
        handles.Add(pool1);
        handles.Add(pool2);
        handles.Add(pool3);
        handles.Add(pool4);
        handles.Add(pool5);
        handles.Add(pool6);
        handles.Add(pool7);
        handles.Add(pool8);
        handles.Add(pool9);
        handles.Add(pool10);
        handles.Add(pool11);
        handles.Add(pool12);
        handles.Add(pool13);
        handles.Add(pool14);
        handles.Add(pool15);
        handles.Add(pool16);

        for (var index = 0; index < m_Archetypes.Count; index++)
        {
            var archetype = m_Archetypes[index];
            if (archetype.MatchComponents(m_Context.Hash, m_Context.Includes, m_Context.Excludes))
            {
                archetype.GetComponents(memories0);
                archetype.GetComponents(memories1);
                archetype.GetComponents(memories2);
                archetype.GetComponents(memories3);
                archetype.GetComponents(memories4);
                archetype.GetComponents(memories5);
                archetype.GetComponents(memories6);
                archetype.GetComponents(memories7);
                archetype.GetComponents(memories8);
                archetype.GetComponents(memories9);
                archetype.GetComponents(memories10);
                archetype.GetComponents(memories11);
                archetype.GetComponents(memories12);
                archetype.GetComponents(memories13);
                archetype.GetComponents(memories14);
                archetype.GetComponents(memories15);
                archetype.GetComponents(memories16);
                break;
            }
        }

        result.Item1 = new Batch<Entity>(memories0);
        result.Item2 = new Batch<T1>(memories1);
        result.Item3 = new Batch<T2>(memories2);
        result.Item4 = new Batch<T3>(memories3);
        result.Item5 = new Batch<T4>(memories4);
        result.Item6 = new Batch<T5>(memories5);
        result.Item7 = new Batch<T6>(memories6);
        result.Item8 = new Batch<T7>(memories7);
        result.Item9 = new Batch<T8>(memories8);
        result.Item10 = new Batch<T9>(memories9);
        result.Item11 = new Batch<T10>(memories10);
        result.Item12 = new Batch<T11>(memories11);
        result.Item13 = new Batch<T12>(memories12);
        result.Item14 = new Batch<T13>(memories13);
        result.Item15 = new Batch<T14>(memories14);
        result.Item16 = new Batch<T15>(memories15);
        result.Item17 = new Batch<T16>(memories16);

        return new (pool, result);
    }
}
