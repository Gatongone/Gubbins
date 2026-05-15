namespace Gubbins.Entities;

public static class EntityQueryExtensions
{
    extension(IEntityQuery query)
    {
        /// <summary>
        /// Determines whether the specified entity handle is currently alive.
        /// </summary>
        public bool Contains(Entity entity)
        {
            if (!query.Contains(entity.Index))
            {
                return false;
            }

            return query.Get(entity.Index).Entity.Version == entity.Version;
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1> GetQueryHandle<T1>(EntityQueryContext<T1> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2> GetQueryHandle<T1, T2>(EntityQueryContext<T1, T2> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3> GetQueryHandle<T1, T2, T3>(EntityQueryContext<T1, T2, T3> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4> GetQueryHandle<T1, T2, T3, T4>(EntityQueryContext<T1, T2, T3, T4> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5> GetQueryHandle<T1, T2, T3, T4, T5>(EntityQueryContext<T1, T2, T3, T4, T5> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6> GetQueryHandle<T1, T2, T3, T4, T5, T6>(EntityQueryContext<T1, T2, T3, T4, T5, T6> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> context)
        {
            return new(query.Archetypes, context);
        }

        /// <summary>
        /// Get unmanaged <see cref="IEntityQueryHandle{TResult}">EntityQueryHandle</see> entity query handle for the specified entity types.
        /// </summary>
        public EntityQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> GetQueryHandle<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(EntityQueryContext<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> context)
        {
            return new(query.Archetypes, context);
        }
    }
}