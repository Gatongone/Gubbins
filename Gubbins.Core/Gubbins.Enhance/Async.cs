using System.Collections.Concurrent;

namespace Gubbins.Enhance;

/// <summary>
/// An asynchronous auto-reset event.
/// </summary>
/// <remarks>
/// Implements by Microsoft.VisualStudio.Threading (Version=18.7.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a).
/// </remarks>
public class AsyncAutoResetEvent
{
    /// <summary>A queue of folks awaiting signals.</summary>
    private readonly Queue<WaiterCompletionSource> m_SignalAwaiters = new();

    /// <summary>
    /// Whether to complete the task synchronously in the <see cref="M:AsyncAutoResetEvent.Set" /> method,
    /// as opposed to asynchronously.
    /// </summary>
    private readonly bool m_AllowInliningAwaiters;

    /// <summary>
    /// A reusable delegate that points to the <see cref="M:AsyncAutoResetEvent.OnCancellationRequest(System.Object)" /> method.
    /// </summary>
    private readonly Action<object> m_OnCancellationRequestHandler;

    /// <summary>
    /// A value indicating whether this event is already in a signaled state.
    /// </summary>
    /// <devremarks>
    /// This should not need the volatile modifier because it is
    /// always accessed within a lock.
    /// </devremarks>
    private bool m_Signaled;

    /// <summary>
    /// Initializes a new instance of the <see cref="T:AsyncAutoResetEvent" /> class
    /// that does not inline awaiters.
    /// </summary>
    public AsyncAutoResetEvent() : this(false) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:AsyncAutoResetEvent" /> class.
    /// </summary>
    /// <param name="allowInliningAwaiters">
    /// A value indicating whether to complete the task synchronously in the <see cref="M:AsyncAutoResetEvent.Set" /> method,
    /// as opposed to asynchronously. <see langword="false" /> better simulates the behavior of the
    /// <see cref="T:System.Threading.AutoResetEvent" /> class, but <see langword="true" /> can result in slightly better performance.
    /// </param>
    public AsyncAutoResetEvent(bool allowInliningAwaiters)
    {
        m_AllowInliningAwaiters        = allowInliningAwaiters;
        m_OnCancellationRequestHandler = OnCancellationRequest;
    }

    /// <summary>
    /// Returns an awaitable that may be used to asynchronously acquire the next signal.
    /// </summary>
    /// <returns>An awaitable.</returns>
    public Task WaitAsync() => WaitAsync(CancellationToken.None);

    /// <summary>
    /// Returns an awaitable that may be used to asynchronously acquire the next signal.
    /// </summary>
    /// <param name="cancellationToken">A token whose cancellation removes the caller from the queue of those waiting for the event.</param>
    /// <returns>An awaitable.</returns>
    public Task WaitAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        lock (m_SignalAwaiters)
        {
            if (m_Signaled)
            {
                m_Signaled = false;
                return Task.CompletedTask;
            }

            var completionSource = new WaiterCompletionSource(this, m_AllowInliningAwaiters, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                completionSource.TrySetCanceled(cancellationToken);
            else
                m_SignalAwaiters.Enqueue(completionSource);
            return completionSource.Task;
        }
    }

    /// <summary>
    /// Unblocks one waiter or sets the signal if no waiters are present so the next waiter may proceed immediately.
    /// </summary>
    public void Set()
    {
        var completionSource = (WaiterCompletionSource) null!;
        lock (m_SignalAwaiters)
        {
            if (m_SignalAwaiters.Count > 0)
                completionSource = m_SignalAwaiters.Dequeue();
            else if (!m_Signaled)
                m_Signaled = true;
        }

        if (completionSource == null)
            return;
        completionSource.registration.Dispose();
        completionSource.TrySetResult(Unit.Instance);
    }

    /// <summary>
    /// Responds to cancellation requests by removing the request from the waiter queue.
    /// </summary>
    /// <param name="state">The <see cref="T:AsyncAutoResetEvent.WaiterCompletionSource" /> passed in to the <see cref="M:System.Threading.CancellationToken.Register(System.Action{System.Object},System.Object)" /> method.</param>
    private void OnCancellationRequest(object state)
    {
        var valueToRemove = (WaiterCompletionSource) state;
        bool flag;
        lock (m_SignalAwaiters)
            flag = m_SignalAwaiters.RemoveMidQueue(valueToRemove);
        if (!flag)
            return;
        valueToRemove.TrySetCanceled(valueToRemove.cancellationToken);
    }

    /// <summary>Tracks someone waiting for a signal from the event.</summary>
    private class WaiterCompletionSource : TaskCompletionSourceWithoutInlining<Unit>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:AsyncAutoResetEvent.WaiterCompletionSource" /> class.
        /// </summary>
        /// <param name="owner">The event that is initializing this value.</param>
        /// <param name="allowInliningContinuations"><see langword="true" /> to allow continuations to be inlined upon the completer's callstack.</param>
        /// <param name="cancellationToken">The cancellation token associated with the waiter.</param>
        internal WaiterCompletionSource(AsyncAutoResetEvent owner, bool allowInliningContinuations, CancellationToken cancellationToken) : base(allowInliningContinuations)
        {
            this.cancellationToken = cancellationToken;
            registration           = cancellationToken.Register(owner.m_OnCancellationRequestHandler, this);
        }

        /// <summary>
        /// Gets the <see cref="P:cancellationToken" /> provided by the waiter.
        /// </summary>
        internal CancellationToken cancellationToken { get; }

        /// <summary>
        /// Gets the registration to dispose of when the waiter receives their event.
        /// </summary>
        internal CancellationTokenRegistration registration { get; }
    }
}

/// <summary>
/// A <see cref="T:System.Threading.Tasks.TaskCompletionSource`1" />-derivative that
/// does not inline continuations if so configured.
/// </summary>
/// <typeparam name="T">The type of the task's resulting value.</typeparam>
internal class TaskCompletionSourceWithoutInlining<T> : TaskCompletionSource<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="T:TaskCompletionSourceWithoutInlining`1" /> class.
    /// </summary>
    /// <param name="allowInliningContinuations">
    /// <see langword="true" /> to allow continuations to be inlined; otherwise <see langword="false" />.
    /// </param>
    /// <param name="options">
    /// TaskCreationOptions to pass on to the base constructor.
    /// </param>
    /// <param name="state">The state to set on the Task.</param>
    internal TaskCompletionSourceWithoutInlining(
        bool allowInliningContinuations,
        TaskCreationOptions options = TaskCreationOptions.None,
        object? state = null)
        : base(state, AdjustFlags(options, allowInliningContinuations)) => Task = base.Task;

    /// <summary>
    /// Gets the <see cref="P:TaskCompletionSourceWithoutInlining`1.Task" /> that may never complete inline with completion of this <see cref="T:System.Threading.Tasks.TaskCompletionSource`1" />.
    /// </summary>
    /// <devremarks>
    /// Return the base.Task if it is already completed since inlining continuations
    /// on the completer is no longer a concern. Also, when we are not inlining continuations,
    /// this.exposedTask completes slightly later than base.Task, and callers expect
    /// the Task we return to be complete as soon as they call TrySetResult.
    /// </devremarks>
    internal new Task<T> Task => !base.Task.IsCompleted ? field : base.Task;

    /// <summary>
    /// Modifies the specified flags to include RunContinuationsAsynchronously
    /// if wanted by the caller and supported by the platform.
    /// </summary>
    /// <param name="options">The base options supplied by the caller.</param>
    /// <param name="allowInliningContinuations"><see langword="true" /> to allow inlining continuations.</param>
    /// <returns>The possibly modified flags.</returns>
    private static TaskCreationOptions AdjustFlags(
        TaskCreationOptions options,
        bool allowInliningContinuations) =>
        !allowInliningContinuations ? options | TaskCreationOptions.RunContinuationsAsynchronously : options & ~TaskCreationOptions.RunContinuationsAsynchronously;
}

/// <summary>
/// Internal helper/extension methods for this assembly's own use.
/// </summary>
internal static class InternalUtilities
{
    /// <summary>
    /// Removes an element from the middle of a queue without disrupting the other elements.
    /// </summary>
    /// <typeparam name="T">The element to remove.</typeparam>
    /// <param name="queue">The queue to modify.</param>
    /// <param name="valueToRemove">The value to remove.</param>
    /// <remarks>
    /// If a value appears multiple times in the queue, only its first entry is removed.
    /// </remarks>
    internal static bool RemoveMidQueue<T>(this Queue<T> queue, T valueToRemove) where T : class
    {
        var count = queue.Count;
        var num = 0;
        var flag = false;
        while (num < count)
        {
            ++num;
            var obj = queue.Dequeue();
            if (!flag && obj == valueToRemove)
            {
                flag = true;
            }
            else
            {
                queue.Enqueue(obj);
            }
        }

        return flag;
    }
}

public static class Async
{
    /// <summary>
    /// Executes an action in parallel for a range of integers, with support for cancellation and batching.
    /// </summary>
    /// <param name="from">The starting integer of the range (inclusive).</param>
    /// <param name="to">The ending integer of the range (exclusive).</param>
    /// <param name="state">The state object to pass to the action.</param>
    /// <param name="action">The action to execute for each integer in the range, taking the state and the current integer as parameters.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the tasks to complete.</param>
    /// <param name="isShortTask">A boolean indicating whether the tasks are short-lived, which affects how the waiting is handled.</param>
    /// <typeparam name="T">The type of the state object.</typeparam>
    public static async Task ForAsync<T>(int from, int to, T state, Action<T, int, CancellationToken> action, CancellationToken cancellationToken = default, bool isShortTask = true)
    {
        var range = to - from;
        var batchCount = Environment.ProcessorCount;
        var batchSize = Math.Max(1, range / batchCount);
        var countdown = CountdownState.Spawn(CountdownEventPool.Default.Spawn(to - from), cancellationToken);
        try
        {
            for (var i = 0; i < batchCount; i++)
            {
                var start = from + i * batchSize;
                var end = i == batchCount - 1 ? to : start + batchSize;
                ThreadPool.QueueUserWorkItem(static s =>
                {
                    var (start, end) = (s.Range.Start.Value, s.Range.End.Value);
                    var cancellationToken = s.Countdown.CancellationToken;
                    try
                    {
                        for (var j = start; j < end; j++)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                break;
                            }
                            s.Callback(s.State, j, cancellationToken);
                        }
                    }
                    catch (Exception e)
                    {
                        s.Countdown.Exceptions.Add(e);
                    }
                    finally
                    {
                        s.Countdown.Handle.Signal();
                    }
                }, (State: state, Range: new Range(start, end), Callback: action, Countdown: countdown), false);
            }

            await countdown.Handle.WaitAsync(cancellationToken, isShortTask);
        }
        finally
        {
            CountdownEventPool.Default.Recycle(countdown.Handle);
            CountdownState.Recycle(countdown);
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

    private class CountdownState
    {
        private static readonly ConcurrentQueue<CountdownState> s_Pool     = new();
        public readonly         List<Exception>                 Exceptions = new();
        public                  CountdownEvent                  Handle;
        public                  CancellationToken               CancellationToken;

        internal static CountdownState Spawn(CountdownEvent countdownEvent, CancellationToken cancellationToken)
        {
            if (!s_Pool.TryDequeue(out var state))
            {
                state = new CountdownState();
            }

            state.Handle         = countdownEvent;
            state.CancellationToken = cancellationToken;
            return state;
        }

        internal static void Recycle(CountdownState state)
        {
            state.Handle         = null!;
            state.CancellationToken = default!;
            state.Exceptions.Clear();
            s_Pool.Enqueue(state);
        }
    }

    public static async ValueTask WaitAsync(this CountdownEvent countdown, CancellationToken cancellationToken, bool isShortTask)
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
            var state = CountdownState.Spawn(countdown, cancellationToken);
            await Task.Factory.StartNew(static arg =>
            {
                var state = (CountdownState) arg!;
                state.Handle.Wait(state.CancellationToken);
            }, state, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}