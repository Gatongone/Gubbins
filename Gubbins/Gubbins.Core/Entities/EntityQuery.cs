namespace Gubbins.Entities;

/// <summary>
/// Provides methods for getting entity query handles for unmanaged entities.
/// </summary>
public static class EntityQueryGetQueryHandleExtensions
{
    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1> GetQueryHandle<T1>(this IEntityQuery query, EntityQueryContext<T1> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2> GetQueryHandle<T1, T2>(this IEntityQuery query, EntityQueryContext<T1, T2> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3> GetQueryHandle<T1, T2, T3>(this IEntityQuery query, EntityQueryContext<T1, T2, T3> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4> GetQueryHandle<T1, T2, T3, T4>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5> GetQueryHandle<T1, T2, T3, T4, T5>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6> GetQueryHandle<T1, T2, T3, T4, T5, T6>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> context)
    {
        return new(query.Archetypes, context);
    }

    /// <summary>
    /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
    /// </summary>
    public static EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this IEntityQuery query, EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> context)
    {
        return new(query.Archetypes, context);
    }
}