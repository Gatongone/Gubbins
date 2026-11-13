using System.Runtime.CompilerServices;
using System.Threading.Tasks.Sources;

namespace Gubbins.Async;

/// <summary>
/// A single-consumption awaitable returned by pooled asynchronous operations. It is a value type that holds the
/// pooled operation directly, so awaiting it performs no allocation and avoids the interface dispatch that
/// <see cref="ValueTask"/> requires.
/// </summary>
/// <remarks>
/// The value must be awaited exactly once. A default value represents an already completed operation.
/// </remarks>
[AsyncMethodBuilder(typeof(AsynchronousMethodBuilder))]
public readonly struct Asynchronous : IAwaitable
{
    private readonly PooledValueTaskSource? m_Operation;
    private readonly short m_Token;

    internal Asynchronous(PooledValueTaskSource operation, short token)
    {
        m_Operation = operation;
        m_Token = token;
    }

    /// <summary>
    /// Gets a value indicating whether the operation has completed.
    /// </summary>
    public bool IsCompleted => m_Operation is null || m_Operation.GetStatus(m_Token) != ValueTaskSourceStatus.Pending;

    /// <summary>
    /// Converts the operation to a <see cref="Task"/>. This allocates and consumes the operation; do not also await
    /// the same <see cref="Asynchronous"/>.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public Task AsTask() => m_Operation is null ? Task.CompletedTask : new ValueTask(m_Operation, m_Token).AsTask();

    /// <summary>
    /// Gets an awaiter for this operation.
    /// </summary>
    /// <returns>The awaiter.</returns>
    public Awaiter GetAwaiter() => new(m_Operation, m_Token);

    IAwaiter IAwaitable.GetAwaiter() => GetAwaiter();

    /// <summary>
    /// Awaits the pooled operation.
    /// </summary>
    public readonly struct Awaiter : ICriticalNotifyCompletion, IAwaiter
    {
        private static readonly Action<object?> invokeContinuation = static state => ((Action)state!).Invoke();
        private readonly PooledValueTaskSource? m_Operation;
        private readonly short m_Token;

        internal Awaiter(PooledValueTaskSource? operation, short token)
        {
            m_Operation = operation;
            m_Token = token;
        }

        /// <summary>
        /// Gets a value indicating whether the operation has completed.
        /// </summary>
        public bool IsCompleted =>
            m_Operation is null || m_Operation.GetStatus(m_Token) != ValueTaskSourceStatus.Pending;

        /// <summary>
        /// Schedules the continuation. The operation completes continuations asynchronously.
        /// </summary>
        /// <param name="continuation">The continuation to invoke after completion.</param>
        public void OnCompleted(Action continuation) => Schedule(continuation);

        /// <summary>
        /// Schedules the continuation without flowing the execution context.
        /// </summary>
        /// <param name="continuation">The continuation to invoke after completion.</param>
        public void UnsafeOnCompleted(Action continuation) => Schedule(continuation);

        /// <summary>
        /// Gets the result of the operation, throwing its aggregated exception when it failed.
        /// </summary>
        public void GetResult() => m_Operation?.GetResult(m_Token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Flow the synchronization context so an await resumes on it (the Unity main thread, for example). The
        // execution context is captured by the async method builder, which schedules this continuation.
        private void Schedule(Action continuation) => m_Operation?.OnCompleted(
            invokeContinuation,
            continuation,
            m_Token,
            ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
    }
}

/// <summary>
/// A single-consumption awaitable that produces a result. It is a value type that holds the pooled completion
/// source directly, so awaiting it performs no allocation and avoids the interface dispatch that
/// <see cref="ValueTask{TResult}"/> requires.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <remarks>
/// The value must be awaited exactly once. A default value represents an already completed operation whose
/// result is <c>default</c>.
/// </remarks>
[AsyncMethodBuilder(typeof(AsynchronousMethodBuilder<>))]
public readonly struct Asynchronous<TResult> : IAwaitable<TResult>
{
    private readonly AsynchronousSource<TResult>? m_Source;
    private readonly short m_Token;

    internal Asynchronous(AsynchronousSource<TResult> source, short token)
    {
        m_Source = source;
        m_Token = token;
    }

    /// <summary>
    /// Gets a value indicating whether the operation has completed.
    /// </summary>
    public bool IsCompleted => m_Source is null || m_Source.GetStatus(m_Token) != ValueTaskSourceStatus.Pending;

    /// <summary>
    /// Converts the operation to a <see cref="Task{TResult}"/>. This allocates and consumes the operation; do not
    /// also await the same <see cref="Asynchronous{TResult}"/>.
    /// </summary>
    /// <returns>A task representing the operation.</returns>
    public Task<TResult> AsTask() =>
        m_Source is null
            ? Task.FromResult(default(TResult)!)
            : new ValueTask<TResult>(m_Source, m_Token).AsTask();

    /// <summary>
    /// Gets an awaiter for this operation.
    /// </summary>
    /// <returns>The awaiter.</returns>
    public Awaiter GetAwaiter() => new(m_Source, m_Token);

    IAwaiter<TResult> IAwaitable<TResult>.GetAwaiter() => GetAwaiter();

    IAwaiter IAwaitable.GetAwaiter() => GetAwaiter();

    /// <summary>
    /// Awaits the pooled operation.
    /// </summary>
    public readonly struct Awaiter : ICriticalNotifyCompletion, IAwaiter<TResult>
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Action<object?> invokeContinuation = static state => ((Action)state!).Invoke();
        private readonly AsynchronousSource<TResult>? m_Source;
        private readonly short m_Token;

        internal Awaiter(AsynchronousSource<TResult>? source, short token)
        {
            m_Source = source;
            m_Token = token;
        }

        /// <summary>
        /// Gets a value indicating whether the operation has completed.
        /// </summary>
        public bool IsCompleted =>
            m_Source is null || m_Source.GetStatus(m_Token) != ValueTaskSourceStatus.Pending;

        /// <summary>
        /// Schedules the continuation. The operation completes continuations asynchronously.
        /// </summary>
        /// <param name="continuation">The continuation to invoke after completion.</param>
        public void OnCompleted(Action continuation) => Schedule(continuation);

        /// <summary>
        /// Schedules the continuation without flowing the execution context.
        /// </summary>
        /// <param name="continuation">The continuation to invoke after completion.</param>
        public void UnsafeOnCompleted(Action continuation) => Schedule(continuation);

        /// <summary>
        /// Gets the operation result, throwing when the operation failed.
        /// </summary>
        /// <returns>The result.</returns>
        public TResult GetResult() => m_Source is null ? default! : m_Source.GetResult(m_Token);

        void IAwaiter.GetResult() => GetResult();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // Flow the synchronization context so an await resumes on it (the Unity main thread, for example). The
        // execution context is captured by the async method builder, which schedules this continuation.
        private void Schedule(Action continuation) => m_Source?.OnCompleted(
            invokeContinuation,
            continuation,
            m_Token,
            ValueTaskSourceOnCompletedFlags.UseSchedulingContext);
    }
}