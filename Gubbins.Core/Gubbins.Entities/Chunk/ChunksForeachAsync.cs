using System.Collections.Concurrent;
using Gubbins.Spawner;

namespace Gubbins.Entities;

public static class ChunksForeachAsync
{
    extension(Chunks chunks)
    {
        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync(ElementForeach action, TaskKind taskKind, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            var segments0 = entities.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState
            {
                Action            = action,
                Segments0         = segments0,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T>(ElementForeach<T> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components = chunks.GetComponents<T>();
            var segments0 = entities.Segments;
            var segments1 = components.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components = state.Segments1[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2>(ElementForeach<T1, T2> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3>(ElementForeach<T1, T2, T3> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4>(ElementForeach<T1, T2, T3, T4> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5>(ElementForeach<T1, T2, T3, T4, T5> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6>(ElementForeach<T1, T2, T3, T4, T5, T6> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7>(ElementForeach<T1, T2, T3, T4, T5, T6, T7> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            var segments0 = entities.Segments;
            var segments1 = components1.Segments;
            var segments2 = components2.Segments;
            var segments3 = components3.Segments;
            var segments4 = components4.Segments;
            var segments5 = components5.Segments;
            var segments6 = components6.Segments;
            var segments7 = components7.Segments;
            var segments8 = components8.Segments;
            var blockCount = segments0.Count;
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                Segments12        = segments12,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        var components12 = state.Segments12[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i], ref components12[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                Segments12        = segments12,
                Segments13        = segments13,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        var components12 = state.Segments12[index];
                        var components13 = state.Segments13[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i], ref components12[i], ref components13[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                Segments12        = segments12,
                Segments13        = segments13,
                Segments14        = segments14,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        var components12 = state.Segments12[index];
                        var components13 = state.Segments13[index];
                        var components14 = state.Segments14[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i], ref components12[i], ref components13[i], ref components14[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            using var components15 = chunks.GetComponents<T15>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                Segments12        = segments12,
                Segments13        = segments13,
                Segments14        = segments14,
                Segments15        = segments15,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        var components12 = state.Segments12[index];
                        var components13 = state.Segments13[index];
                        var components14 = state.Segments14[index];
                        var components15 = state.Segments15[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i], ref components12[i], ref components13[i], ref components14[i], ref components15[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }

        /// <summary>
        /// Iterates in parallel over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        /// <param name="taskKind">Specifies whether the task is short or long running.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <exception cref="InvalidOperationException">Thrown when a work item cannot be queued for execution.</exception>
        /// <exception cref="AggregateException">Thrown when one or more exceptions occur during the execution of the action on the elements.</exception>
        public async ValueTask ForEachAsync<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action, TaskKind taskKind, CancellationToken cancellationToken = default) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            if (cancellationToken.IsCancellationRequested) return;
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            using var components15 = chunks.GetComponents<T15>();
            using var components16 = chunks.GetComponents<T16>();
            var segments0 = entities.Segments;
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
            if (blockCount == 0) return;
            var exceptions = Pool<ConcurrentQueue<Exception>>.Default.Spawn();
            var countdown = CountdownEventPool.Default.Spawn(blockCount);
            var state = new LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>
            {
                Action            = action,
                Segments0         = segments0,
                Segments1         = segments1,
                Segments2         = segments2,
                Segments3         = segments3,
                Segments4         = segments4,
                Segments5         = segments5,
                Segments6         = segments6,
                Segments7         = segments7,
                Segments8         = segments8,
                Segments9         = segments9,
                Segments10        = segments10,
                Segments11        = segments11,
                Segments12        = segments12,
                Segments13        = segments13,
                Segments14        = segments14,
                Segments15        = segments15,
                Segments16        = segments16,
                CancellationToken = cancellationToken,
                Exceptions        = exceptions,
                Countdown         = countdown
            };
            for (var i = 0; i < blockCount; i++)
            {
                if (ThreadPool.QueueUserWorkItem(static s =>
                {
                    var state = s.State;
                    if (state.CancellationToken.IsCancellationRequested) return;
                    try
                    {
                        var index = s.Index;
                        var count = state.Segments0[index].Length;
                        var entities = state.Segments0[index];
                        var components1 = state.Segments1[index];
                        var components2 = state.Segments2[index];
                        var components3 = state.Segments3[index];
                        var components4 = state.Segments4[index];
                        var components5 = state.Segments5[index];
                        var components6 = state.Segments6[index];
                        var components7 = state.Segments7[index];
                        var components8 = state.Segments8[index];
                        var components9 = state.Segments9[index];
                        var components10 = state.Segments10[index];
                        var components11 = state.Segments11[index];
                        var components12 = state.Segments12[index];
                        var components13 = state.Segments13[index];
                        var components14 = state.Segments14[index];
                        var components15 = state.Segments15[index];
                        var components16 = state.Segments16[index];
                        for (var i = 0; i < count; i++)
                        {
                            state.Action(in entities[i], ref components1[i], ref components2[i], ref components3[i], ref components4[i], ref components5[i], ref components6[i], ref components7[i], ref components8[i], ref components9[i], ref components10[i], ref components11[i], ref components12[i], ref components13[i], ref components14[i], ref components15[i], ref components16[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        state.Exceptions.Enqueue(ex);
                    }
                    finally
                    {
                        state.Countdown.Signal();
                    }
                }, (State: state, Index: i), false)) continue;

                CountdownEventPool.Default.Recycle(countdown);
                exceptions.Clear();
                Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                throw new InvalidOperationException("Failed to queue work item for chunk foreach iteration.");
            }

            await countdown.WaitAsync(cancellationToken, taskKind == TaskKind.Short);
            CountdownEventPool.Default.Recycle(countdown);
            if (!exceptions.IsEmpty)
            {
                try
                {
                    throw new AggregateException(exceptions);
                }
                finally
                {
                    exceptions.Clear();
                    Pool<ConcurrentQueue<Exception>>.Default.Recycle(exceptions);
                }
            }
        }
    }

    private class CountdownEventPool
    {
        public static CountdownEventPool Default { get; } = new();
        private readonly ConcurrentQueue<CountdownEvent> m_Pool = new();

        public CountdownEvent Spawn(int initialCount)
        {
            if (m_Pool.TryDequeue(out var countdown))
            {
                countdown.Reset(initialCount);
                return countdown;
            }

            return new CountdownEvent(initialCount);
        }

        public void Recycle(CountdownEvent countdown)
        {
            countdown.Reset();
            m_Pool.Enqueue(countdown);
        }
    }

    private static async ValueTask WaitAsync(this CountdownEvent countdown, CancellationToken cancellationToken, bool isShortTask)
    {
        if (isShortTask)
        {
            // Short tasks can use a busy-wait loop to avoid context switching overhead.
            while (!countdown.IsSet ||
                !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
            }
        }
        else
        {
            // Long tasks should use a cancellation token registration to avoid blocking the thread pool.
            await using var registration = cancellationToken.Register(static state =>
            {
                var countdown = (CountdownEvent) state!;
                while (!countdown.IsSet)
                {
                    countdown.Signal();
                }
            }, countdown);
            var state = Pool<CountdownState>.Default.Spawn();
            state.Countdown         = countdown;
            state.CancellationToken = cancellationToken;
            await Task.Factory.StartNew(static arg =>
            {
                var state = (CountdownState) arg!;
                state.Countdown.Wait(state.CancellationToken);
            }, state, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    private class CountdownState
    {
        public CountdownEvent    Countdown;
        public CancellationToken CancellationToken;
    }

    private struct LoopState
    {
        public ElementForeach                        Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T> where T : unmanaged
    {
        public ElementForeach<T>                     Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public Components<T>.SegmentsEnumerable      Segments1;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T1, T2> where T1 : unmanaged where T2 : unmanaged
    {
        public ElementForeach<T1, T2>                Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public Components<T1>.SegmentsEnumerable     Segments1;
        public Components<T2>.SegmentsEnumerable     Segments2;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T1, T2, T3> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        public ElementForeach<T1, T2, T3>            Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public Components<T1>.SegmentsEnumerable     Segments1;
        public Components<T2>.SegmentsEnumerable     Segments2;
        public Components<T3>.SegmentsEnumerable     Segments3;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T1, T2, T3, T4> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4>        Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public Components<T1>.SegmentsEnumerable     Segments1;
        public Components<T2>.SegmentsEnumerable     Segments2;
        public Components<T3>.SegmentsEnumerable     Segments3;
        public Components<T4>.SegmentsEnumerable     Segments4;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5>    Action;
        public Components<Entity>.SegmentsEnumerable Segments0;
        public Components<T1>.SegmentsEnumerable     Segments1;
        public Components<T2>.SegmentsEnumerable     Segments2;
        public Components<T3>.SegmentsEnumerable     Segments3;
        public Components<T4>.SegmentsEnumerable     Segments4;
        public Components<T5>.SegmentsEnumerable     Segments5;
        public CancellationToken                     CancellationToken;
        public ConcurrentQueue<Exception>            Exceptions;
        public CountdownEvent                        Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6> Action;
        public Components<Entity>.SegmentsEnumerable  Segments0;
        public Components<T1>.SegmentsEnumerable      Segments1;
        public Components<T2>.SegmentsEnumerable      Segments2;
        public Components<T3>.SegmentsEnumerable      Segments3;
        public Components<T4>.SegmentsEnumerable      Segments4;
        public Components<T5>.SegmentsEnumerable      Segments5;
        public Components<T6>.SegmentsEnumerable      Segments6;
        public CancellationToken                      CancellationToken;
        public ConcurrentQueue<Exception>             Exceptions;
        public CountdownEvent                         Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7> Action;
        public Components<Entity>.SegmentsEnumerable      Segments0;
        public Components<T1>.SegmentsEnumerable          Segments1;
        public Components<T2>.SegmentsEnumerable          Segments2;
        public Components<T3>.SegmentsEnumerable          Segments3;
        public Components<T4>.SegmentsEnumerable          Segments4;
        public Components<T5>.SegmentsEnumerable          Segments5;
        public Components<T6>.SegmentsEnumerable          Segments6;
        public Components<T7>.SegmentsEnumerable          Segments7;
        public CancellationToken                          CancellationToken;
        public ConcurrentQueue<Exception>                 Exceptions;
        public CountdownEvent                             Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> Action;
        public Components<Entity>.SegmentsEnumerable          Segments0;
        public Components<T1>.SegmentsEnumerable              Segments1;
        public Components<T2>.SegmentsEnumerable              Segments2;
        public Components<T3>.SegmentsEnumerable              Segments3;
        public Components<T4>.SegmentsEnumerable              Segments4;
        public Components<T5>.SegmentsEnumerable              Segments5;
        public Components<T6>.SegmentsEnumerable              Segments6;
        public Components<T7>.SegmentsEnumerable              Segments7;
        public Components<T8>.SegmentsEnumerable              Segments8;
        public CancellationToken                              CancellationToken;
        public ConcurrentQueue<Exception>                     Exceptions;
        public CountdownEvent                                 Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> Action;
        public Components<Entity>.SegmentsEnumerable              Segments0;
        public Components<T1>.SegmentsEnumerable                  Segments1;
        public Components<T2>.SegmentsEnumerable                  Segments2;
        public Components<T3>.SegmentsEnumerable                  Segments3;
        public Components<T4>.SegmentsEnumerable                  Segments4;
        public Components<T5>.SegmentsEnumerable                  Segments5;
        public Components<T6>.SegmentsEnumerable                  Segments6;
        public Components<T7>.SegmentsEnumerable                  Segments7;
        public Components<T8>.SegmentsEnumerable                  Segments8;
        public Components<T9>.SegmentsEnumerable                  Segments9;
        public CancellationToken                                  CancellationToken;
        public ConcurrentQueue<Exception>                         Exceptions;
        public CountdownEvent                                     Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Action;
        public Components<Entity>.SegmentsEnumerable                   Segments0;
        public Components<T1>.SegmentsEnumerable                       Segments1;
        public Components<T2>.SegmentsEnumerable                       Segments2;
        public Components<T3>.SegmentsEnumerable                       Segments3;
        public Components<T4>.SegmentsEnumerable                       Segments4;
        public Components<T5>.SegmentsEnumerable                       Segments5;
        public Components<T6>.SegmentsEnumerable                       Segments6;
        public Components<T7>.SegmentsEnumerable                       Segments7;
        public Components<T8>.SegmentsEnumerable                       Segments8;
        public Components<T9>.SegmentsEnumerable                       Segments9;
        public Components<T10>.SegmentsEnumerable                      Segments10;
        public CancellationToken                                       CancellationToken;
        public ConcurrentQueue<Exception>                              Exceptions;
        public CountdownEvent                                          Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Action;
        public Components<Entity>.SegmentsEnumerable                        Segments0;
        public Components<T1>.SegmentsEnumerable                            Segments1;
        public Components<T2>.SegmentsEnumerable                            Segments2;
        public Components<T3>.SegmentsEnumerable                            Segments3;
        public Components<T4>.SegmentsEnumerable                            Segments4;
        public Components<T5>.SegmentsEnumerable                            Segments5;
        public Components<T6>.SegmentsEnumerable                            Segments6;
        public Components<T7>.SegmentsEnumerable                            Segments7;
        public Components<T8>.SegmentsEnumerable                            Segments8;
        public Components<T9>.SegmentsEnumerable                            Segments9;
        public Components<T10>.SegmentsEnumerable                           Segments10;
        public Components<T11>.SegmentsEnumerable                           Segments11;
        public CancellationToken                                            CancellationToken;
        public ConcurrentQueue<Exception>                                   Exceptions;
        public CountdownEvent                                               Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Action;
        public Components<Entity>.SegmentsEnumerable                             Segments0;
        public Components<T1>.SegmentsEnumerable                                 Segments1;
        public Components<T2>.SegmentsEnumerable                                 Segments2;
        public Components<T3>.SegmentsEnumerable                                 Segments3;
        public Components<T4>.SegmentsEnumerable                                 Segments4;
        public Components<T5>.SegmentsEnumerable                                 Segments5;
        public Components<T6>.SegmentsEnumerable                                 Segments6;
        public Components<T7>.SegmentsEnumerable                                 Segments7;
        public Components<T8>.SegmentsEnumerable                                 Segments8;
        public Components<T9>.SegmentsEnumerable                                 Segments9;
        public Components<T10>.SegmentsEnumerable                                Segments10;
        public Components<T11>.SegmentsEnumerable                                Segments11;
        public Components<T12>.SegmentsEnumerable                                Segments12;
        public CancellationToken                                                 CancellationToken;
        public ConcurrentQueue<Exception>                                        Exceptions;
        public CountdownEvent                                                    Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Action;
        public Components<Entity>.SegmentsEnumerable                                  Segments0;
        public Components<T1>.SegmentsEnumerable                                      Segments1;
        public Components<T2>.SegmentsEnumerable                                      Segments2;
        public Components<T3>.SegmentsEnumerable                                      Segments3;
        public Components<T4>.SegmentsEnumerable                                      Segments4;
        public Components<T5>.SegmentsEnumerable                                      Segments5;
        public Components<T6>.SegmentsEnumerable                                      Segments6;
        public Components<T7>.SegmentsEnumerable                                      Segments7;
        public Components<T8>.SegmentsEnumerable                                      Segments8;
        public Components<T9>.SegmentsEnumerable                                      Segments9;
        public Components<T10>.SegmentsEnumerable                                     Segments10;
        public Components<T11>.SegmentsEnumerable                                     Segments11;
        public Components<T12>.SegmentsEnumerable                                     Segments12;
        public Components<T13>.SegmentsEnumerable                                     Segments13;
        public CancellationToken                                                      CancellationToken;
        public ConcurrentQueue<Exception>                                             Exceptions;
        public CountdownEvent                                                         Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Action;
        public Components<Entity>.SegmentsEnumerable                                       Segments0;
        public Components<T1>.SegmentsEnumerable                                           Segments1;
        public Components<T2>.SegmentsEnumerable                                           Segments2;
        public Components<T3>.SegmentsEnumerable                                           Segments3;
        public Components<T4>.SegmentsEnumerable                                           Segments4;
        public Components<T5>.SegmentsEnumerable                                           Segments5;
        public Components<T6>.SegmentsEnumerable                                           Segments6;
        public Components<T7>.SegmentsEnumerable                                           Segments7;
        public Components<T8>.SegmentsEnumerable                                           Segments8;
        public Components<T9>.SegmentsEnumerable                                           Segments9;
        public Components<T10>.SegmentsEnumerable                                          Segments10;
        public Components<T11>.SegmentsEnumerable                                          Segments11;
        public Components<T12>.SegmentsEnumerable                                          Segments12;
        public Components<T13>.SegmentsEnumerable                                          Segments13;
        public Components<T14>.SegmentsEnumerable                                          Segments14;
        public CancellationToken                                                           CancellationToken;
        public ConcurrentQueue<Exception>                                                  Exceptions;
        public CountdownEvent                                                              Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Action;
        public Components<Entity>.SegmentsEnumerable                                            Segments0;
        public Components<T1>.SegmentsEnumerable                                                Segments1;
        public Components<T2>.SegmentsEnumerable                                                Segments2;
        public Components<T3>.SegmentsEnumerable                                                Segments3;
        public Components<T4>.SegmentsEnumerable                                                Segments4;
        public Components<T5>.SegmentsEnumerable                                                Segments5;
        public Components<T6>.SegmentsEnumerable                                                Segments6;
        public Components<T7>.SegmentsEnumerable                                                Segments7;
        public Components<T8>.SegmentsEnumerable                                                Segments8;
        public Components<T9>.SegmentsEnumerable                                                Segments9;
        public Components<T10>.SegmentsEnumerable                                               Segments10;
        public Components<T11>.SegmentsEnumerable                                               Segments11;
        public Components<T12>.SegmentsEnumerable                                               Segments12;
        public Components<T13>.SegmentsEnumerable                                               Segments13;
        public Components<T14>.SegmentsEnumerable                                               Segments14;
        public Components<T15>.SegmentsEnumerable                                               Segments15;
        public CancellationToken                                                                CancellationToken;
        public ConcurrentQueue<Exception>                                                       Exceptions;
        public CountdownEvent                                                                   Countdown;
    }

    private struct LoopState<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        public ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Action;
        public Components<Entity>.SegmentsEnumerable                                                 Segments0;
        public Components<T1>.SegmentsEnumerable                                                     Segments1;
        public Components<T2>.SegmentsEnumerable                                                     Segments2;
        public Components<T3>.SegmentsEnumerable                                                     Segments3;
        public Components<T4>.SegmentsEnumerable                                                     Segments4;
        public Components<T5>.SegmentsEnumerable                                                     Segments5;
        public Components<T6>.SegmentsEnumerable                                                     Segments6;
        public Components<T7>.SegmentsEnumerable                                                     Segments7;
        public Components<T8>.SegmentsEnumerable                                                     Segments8;
        public Components<T9>.SegmentsEnumerable                                                     Segments9;
        public Components<T10>.SegmentsEnumerable                                                    Segments10;
        public Components<T11>.SegmentsEnumerable                                                    Segments11;
        public Components<T12>.SegmentsEnumerable                                                    Segments12;
        public Components<T13>.SegmentsEnumerable                                                    Segments13;
        public Components<T14>.SegmentsEnumerable                                                    Segments14;
        public Components<T15>.SegmentsEnumerable                                                    Segments15;
        public Components<T16>.SegmentsEnumerable                                                    Segments16;
        public CancellationToken                                                                     CancellationToken;
        public ConcurrentQueue<Exception>                                                            Exceptions;
        public CountdownEvent                                                                        Countdown;
    }
}

/// <summary>
/// Specifies the kind of task to be executed in the ForEachAsync method.
/// </summary>
public enum TaskKind
{
    /// <summary>Indicates that the task is expected to be short-running and may benefit from a busy-wait loop.</summary>
    Short,

    /// <summary>Indicates that the task is expected to be long-running and should use a cancellation token registration to avoid blocking the thread pool.</summary>
    Long
}