using Gubbins.Async;

namespace Gubbins.Entities;

public static class ChunksForeachAsync
{
    extension(Chunks chunks)
    {
        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync(ElementForeach action, CancellationToken cancellationToken = default)
            => ForEachAsyncOperation.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1>(ElementForeach<T1> action, CancellationToken cancellationToken = default) where T1 : unmanaged
            => ForEachAsyncOperation<T1>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2>(ElementForeach<T1, T2> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged
            => ForEachAsyncOperation<T1, T2>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3>(ElementForeach<T1, T2, T3> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4>(ElementForeach<T1, T2, T3, T4> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5>(ElementForeach<T1, T2, T3, T4, T5> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6>(ElementForeach<T1, T2, T3, T4, T5, T6> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7>(ElementForeach<T1, T2, T3, T4, T5, T6, T7> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, CancellationToken cancellationToken = default)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, CancellationToken cancellationToken = default)
            where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>.Start(chunks, action, cancellationToken);

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>An awaitable that completes when every element has been processed.</returns>
        /// <exception cref="AggregateException">Thrown asynchronously when one or more exceptions occur during the execution of the action.</exception>
        /// <remarks>The returned awaitable must be awaited exactly once.</remarks>
        public Asynchronous ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, CancellationToken cancellationToken = default) where T1 : unmanaged
            where T2 : unmanaged
            where T3 : unmanaged
            where T4 : unmanaged
            where T5 : unmanaged
            where T6 : unmanaged
            where T7 : unmanaged
            where T8 : unmanaged
            where T9 : unmanaged
            where T10 : unmanaged
            where T11 : unmanaged
            where T12 : unmanaged
            where T13 : unmanaged
            where T14 : unmanaged
            where T15 : unmanaged
            where T16 : unmanaged
            => ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>.Start(chunks, action, cancellationToken);
    }

    internal sealed class ForEachAsyncOperation : PooledValueTaskSource
    {
        private ElementForeach m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;

        public static Asynchronous Start(Chunks chunks, ElementForeach action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();

            var segments0 = components0.Segments;


            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;


            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];

                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1> : PooledValueTaskSource where T1 : unmanaged
    {
        private ElementForeach<T1> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T1>.SegmentsEnumerable m_Segments1;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged
    {
        private ElementForeach<T1, T2> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        private ElementForeach<T1, T2, T3> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T12> m_Components12;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;
        private Components<T12>.SegmentsEnumerable m_Segments12;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var components12 = chunks.GetComponents<T12>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;
            var segments12 = components12.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;
            operation.m_Components12 = components12;
            operation.m_Segments12 = segments12;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                var component12 = m_Segments12[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i], ref component12[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components12.Dispose();
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
            m_Components12 = default;
            m_Segments12 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T12> m_Components12;
        private Components<T13> m_Components13;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;
        private Components<T12>.SegmentsEnumerable m_Segments12;
        private Components<T13>.SegmentsEnumerable m_Segments13;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var components12 = chunks.GetComponents<T12>();
            var components13 = chunks.GetComponents<T13>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;
            var segments12 = components12.Segments;
            var segments13 = components13.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;
            operation.m_Components12 = components12;
            operation.m_Segments12 = segments12;
            operation.m_Components13 = components13;
            operation.m_Segments13 = segments13;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                var component12 = m_Segments12[index];
                var component13 = m_Segments13[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i], ref component12[i], ref component13[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components13.Dispose();
            m_Components12.Dispose();
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
            m_Components12 = default;
            m_Segments12 = default;
            m_Components13 = default;
            m_Segments13 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T12> m_Components12;
        private Components<T13> m_Components13;
        private Components<T14> m_Components14;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;
        private Components<T12>.SegmentsEnumerable m_Segments12;
        private Components<T13>.SegmentsEnumerable m_Segments13;
        private Components<T14>.SegmentsEnumerable m_Segments14;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var components12 = chunks.GetComponents<T12>();
            var components13 = chunks.GetComponents<T13>();
            var components14 = chunks.GetComponents<T14>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;
            var segments12 = components12.Segments;
            var segments13 = components13.Segments;
            var segments14 = components14.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;
            operation.m_Components12 = components12;
            operation.m_Segments12 = segments12;
            operation.m_Components13 = components13;
            operation.m_Segments13 = segments13;
            operation.m_Components14 = components14;
            operation.m_Segments14 = segments14;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                var component12 = m_Segments12[index];
                var component13 = m_Segments13[index];
                var component14 = m_Segments14[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i], ref component12[i], ref component13[i], ref component14[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components14.Dispose();
            m_Components13.Dispose();
            m_Components12.Dispose();
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
            m_Components12 = default;
            m_Segments12 = default;
            m_Components13 = default;
            m_Segments13 = default;
            m_Components14 = default;
            m_Segments14 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T12> m_Components12;
        private Components<T13> m_Components13;
        private Components<T14> m_Components14;
        private Components<T15> m_Components15;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;
        private Components<T12>.SegmentsEnumerable m_Segments12;
        private Components<T13>.SegmentsEnumerable m_Segments13;
        private Components<T14>.SegmentsEnumerable m_Segments14;
        private Components<T15>.SegmentsEnumerable m_Segments15;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var components12 = chunks.GetComponents<T12>();
            var components13 = chunks.GetComponents<T13>();
            var components14 = chunks.GetComponents<T14>();
            var components15 = chunks.GetComponents<T15>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;
            var segments12 = components12.Segments;
            var segments13 = components13.Segments;
            var segments14 = components14.Segments;
            var segments15 = components15.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components15.Dispose();
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;
            operation.m_Components12 = components12;
            operation.m_Segments12 = segments12;
            operation.m_Components13 = components13;
            operation.m_Segments13 = segments13;
            operation.m_Components14 = components14;
            operation.m_Segments14 = segments14;
            operation.m_Components15 = components15;
            operation.m_Segments15 = segments15;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components15.Dispose();
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                var component12 = m_Segments12[index];
                var component13 = m_Segments13[index];
                var component14 = m_Segments14[index];
                var component15 = m_Segments15[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i], ref component12[i], ref component13[i], ref component14[i], ref component15[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components15.Dispose();
            m_Components14.Dispose();
            m_Components13.Dispose();
            m_Components12.Dispose();
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
            m_Components12 = default;
            m_Segments12 = default;
            m_Components13 = default;
            m_Segments13 = default;
            m_Components14 = default;
            m_Segments14 = default;
            m_Components15 = default;
            m_Segments15 = default;
        }
    }

    internal sealed class ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : PooledValueTaskSource where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        private ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> m_Action = null!;
        private Components<Entity> m_Components0;
        private Components<Entity>.SegmentsEnumerable m_Segments0;
        private Components<T1> m_Components1;
        private Components<T2> m_Components2;
        private Components<T3> m_Components3;
        private Components<T4> m_Components4;
        private Components<T5> m_Components5;
        private Components<T6> m_Components6;
        private Components<T7> m_Components7;
        private Components<T8> m_Components8;
        private Components<T9> m_Components9;
        private Components<T10> m_Components10;
        private Components<T11> m_Components11;
        private Components<T12> m_Components12;
        private Components<T13> m_Components13;
        private Components<T14> m_Components14;
        private Components<T15> m_Components15;
        private Components<T16> m_Components16;
        private Components<T1>.SegmentsEnumerable m_Segments1;
        private Components<T2>.SegmentsEnumerable m_Segments2;
        private Components<T3>.SegmentsEnumerable m_Segments3;
        private Components<T4>.SegmentsEnumerable m_Segments4;
        private Components<T5>.SegmentsEnumerable m_Segments5;
        private Components<T6>.SegmentsEnumerable m_Segments6;
        private Components<T7>.SegmentsEnumerable m_Segments7;
        private Components<T8>.SegmentsEnumerable m_Segments8;
        private Components<T9>.SegmentsEnumerable m_Segments9;
        private Components<T10>.SegmentsEnumerable m_Segments10;
        private Components<T11>.SegmentsEnumerable m_Segments11;
        private Components<T12>.SegmentsEnumerable m_Segments12;
        private Components<T13>.SegmentsEnumerable m_Segments13;
        private Components<T14>.SegmentsEnumerable m_Segments14;
        private Components<T15>.SegmentsEnumerable m_Segments15;
        private Components<T16>.SegmentsEnumerable m_Segments16;

        public static Asynchronous Start(Chunks chunks, ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return default;
            }

            var components0 = chunks.GetComponents<Entity>();
            var components1 = chunks.GetComponents<T1>();
            var components2 = chunks.GetComponents<T2>();
            var components3 = chunks.GetComponents<T3>();
            var components4 = chunks.GetComponents<T4>();
            var components5 = chunks.GetComponents<T5>();
            var components6 = chunks.GetComponents<T6>();
            var components7 = chunks.GetComponents<T7>();
            var components8 = chunks.GetComponents<T8>();
            var components9 = chunks.GetComponents<T9>();
            var components10 = chunks.GetComponents<T10>();
            var components11 = chunks.GetComponents<T11>();
            var components12 = chunks.GetComponents<T12>();
            var components13 = chunks.GetComponents<T13>();
            var components14 = chunks.GetComponents<T14>();
            var components15 = chunks.GetComponents<T15>();
            var components16 = chunks.GetComponents<T16>();
            var segments0 = components0.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var segments9 = components9.Segments;
            var segments10 = components10.Segments;
            var segments11 = components11.Segments;
            var segments12 = components12.Segments;
            var segments13 = components13.Segments;
            var segments14 = components14.Segments;
            var segments15 = components15.Segments;
            var segments16 = components16.Segments;

            var blockCount = segments0.Count;
            if (blockCount == 0)
            {
                components16.Dispose();
                components15.Dispose();
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                return default;
            }

            var workerCount = Math.Min(Environment.ProcessorCount, blockCount);
            var chunkSize = (blockCount + workerCount - 1) / workerCount;

            var operation = OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>.Rent();
            operation.m_Action = action;
            operation.m_Components0 = components0;
            operation.m_Segments0 = segments0;
            operation.m_Components1 = components1;
            operation.m_Segments1 = segments1;
            operation.m_Components2 = components2;
            operation.m_Segments2 = segments2;
            operation.m_Components3 = components3;
            operation.m_Segments3 = segments3;
            operation.m_Components4 = components4;
            operation.m_Segments4 = segments4;
            operation.m_Components5 = components5;
            operation.m_Segments5 = segments5;
            operation.m_Components6 = components6;
            operation.m_Segments6 = segments6;
            operation.m_Components7 = components7;
            operation.m_Segments7 = segments7;
            operation.m_Components8 = components8;
            operation.m_Segments8 = segments8;
            operation.m_Components9 = components9;
            operation.m_Segments9 = segments9;
            operation.m_Components10 = components10;
            operation.m_Segments10 = segments10;
            operation.m_Components11 = components11;
            operation.m_Segments11 = segments11;
            operation.m_Components12 = components12;
            operation.m_Segments12 = segments12;
            operation.m_Components13 = components13;
            operation.m_Segments13 = segments13;
            operation.m_Components14 = components14;
            operation.m_Segments14 = segments14;
            operation.m_Components15 = components15;
            operation.m_Segments15 = segments15;
            operation.m_Components16 = components16;
            operation.m_Segments16 = segments16;

            try
            {
                operation.Prepare(blockCount, workerCount, chunkSize, cancellationToken);
                operation.Run();
            }
            catch
            {
                components16.Dispose();
                components15.Dispose();
                components14.Dispose();
                components13.Dispose();
                components12.Dispose();
                components11.Dispose();
                components10.Dispose();
                components9.Dispose();
                components8.Dispose();
                components7.Dispose();
                components6.Dispose();
                components5.Dispose();
                components4.Dispose();
                components3.Dispose();
                components2.Dispose();
                components1.Dispose();
                components0.Dispose();
                operation.Clear();
                OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>.Return(operation);
                throw;
            }

            return operation.CreateTask();
        }

        protected override void ExecuteRange(int start, int end)
        {
            for (var index = start; index < end; index++)
            {
                if (IsCancellationRequested)
                {
                    return;
                }

                var count = m_Segments0[index].Length;
                var entities = m_Segments0[index];
                var component1 = m_Segments1[index];
                var component2 = m_Segments2[index];
                var component3 = m_Segments3[index];
                var component4 = m_Segments4[index];
                var component5 = m_Segments5[index];
                var component6 = m_Segments6[index];
                var component7 = m_Segments7[index];
                var component8 = m_Segments8[index];
                var component9 = m_Segments9[index];
                var component10 = m_Segments10[index];
                var component11 = m_Segments11[index];
                var component12 = m_Segments12[index];
                var component13 = m_Segments13[index];
                var component14 = m_Segments14[index];
                var component15 = m_Segments15[index];
                var component16 = m_Segments16[index];
                for (var i = 0; i < count; i++)
                {
                    m_Action(in entities[i], ref component1[i], ref component2[i], ref component3[i], ref component4[i], ref component5[i], ref component6[i], ref component7[i], ref component8[i], ref component9[i], ref component10[i], ref component11[i], ref component12[i], ref component13[i], ref component14[i], ref component15[i], ref component16[i]);
                }
            }
        }

        protected override void DisposeResources()
        {
            m_Components16.Dispose();
            m_Components15.Dispose();
            m_Components14.Dispose();
            m_Components13.Dispose();
            m_Components12.Dispose();
            m_Components11.Dispose();
            m_Components10.Dispose();
            m_Components9.Dispose();
            m_Components8.Dispose();
            m_Components7.Dispose();
            m_Components6.Dispose();
            m_Components5.Dispose();
            m_Components4.Dispose();
            m_Components3.Dispose();
            m_Components2.Dispose();
            m_Components1.Dispose();
            m_Components0.Dispose();
        }

        protected override void ReturnToPool()
        {
            Clear();
            OperationPool<ForEachAsyncOperation<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>.Return(this);
        }

        private void Clear()
        {
            m_Action = null!;
            m_Components0 = default;
            m_Segments0 = default;
            m_Components1 = default;
            m_Segments1 = default;
            m_Components2 = default;
            m_Segments2 = default;
            m_Components3 = default;
            m_Segments3 = default;
            m_Components4 = default;
            m_Segments4 = default;
            m_Components5 = default;
            m_Segments5 = default;
            m_Components6 = default;
            m_Segments6 = default;
            m_Components7 = default;
            m_Segments7 = default;
            m_Components8 = default;
            m_Segments8 = default;
            m_Components9 = default;
            m_Segments9 = default;
            m_Components10 = default;
            m_Segments10 = default;
            m_Components11 = default;
            m_Segments11 = default;
            m_Components12 = default;
            m_Segments12 = default;
            m_Components13 = default;
            m_Segments13 = default;
            m_Components14 = default;
            m_Segments14 = default;
            m_Components15 = default;
            m_Segments15 = default;
            m_Components16 = default;
            m_Segments16 = default;
        }
    }
}

/// <summary>
/// Provides a minimal pool of reusable asynchronous operations.
/// </summary>
/// <remarks>
/// Rent and return never allocate once the pool is warm. The fast path is a thread-local single slot, which
/// serves the common sequential case without any synchronization. When the slot is already occupied, the
/// operation goes through a <see cref="SpinLock"/>-guarded stack, so no queue nodes are ever allocated.
/// </remarks>
/// <typeparam name="TOperation">The operation type. It must expose a public parameterless constructor.</typeparam>
public static class OperationPool<TOperation> where TOperation : class, new()
{
    [ThreadStatic] private static TOperation? s_TCached;

    private static readonly Stack<TOperation> items = new();
    private static SpinLock s_Lock = new(false);

    /// <summary>
    /// Gets a cleared operation from the pool, or creates a new one when the pool is empty.
    /// </summary>
    /// <returns>The rented operation.</returns>
    public static TOperation Rent()
    {
        var cached = s_TCached;

        if (cached is not null)
        {
            s_TCached = null;
            return cached;
        }

        var taken = false;
        s_Lock.Enter(ref taken);
        try
        {
            return items.Count > 0 ? items.Pop() : new TOperation();
        }
        finally
        {
            s_Lock.Exit();
        }
    }

    /// <summary>
    /// Returns an operation to the pool after it has been awaited and its resources released.
    /// </summary>
    /// <param name="operation">The operation to recycle.</param>
    public static void Return(TOperation operation)
    {
        if (s_TCached is null)
        {
            s_TCached = operation;
            return;
        }

        var taken = false;
        s_Lock.Enter(ref taken);
        try
        {
            items.Push(operation);
        }
        finally
        {
            s_Lock.Exit();
        }
    }
}