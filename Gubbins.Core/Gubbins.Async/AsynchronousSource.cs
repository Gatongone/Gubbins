using System.Threading.Tasks.Sources;

namespace Gubbins.Async;

/// <summary>
/// Pooled completion source used by <see cref="AsynchronousMethodBuilder"/> for <c>async Asynchronous</c> and
/// <c>async IAwaitable</c> methods.
/// </summary>
internal sealed class AsynchronousSource : PooledValueTaskSource
{
    [ThreadStatic] private static AsynchronousSource? s_TCached;

    private static readonly Stack<AsynchronousSource> pool = new();
    private static SpinLock s_Lock = new(false);

    /// <summary>
    /// Keeps the boxed async state machine alive until the operation completes.
    /// </summary>
    private object? m_StateMachine;

    /// <summary>
    /// Rents a cleared source from the pool.
    /// </summary>
    public static AsynchronousSource Rent()
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
            return pool.Count > 0 ? pool.Pop() : new AsynchronousSource();
        }
        finally
        {
            s_Lock.Exit();
        }
    }

    /// <summary>
    /// Resets the source for a new async method invocation.
    /// </summary>
    public void Prepare() => Prepare(0, 0, 0, default);

    /// <summary>
    /// Gets the awaitable handed to the caller of the async method.
    /// </summary>
    public Asynchronous Awaitable => CreateTask();

    /// <summary>
    /// Stores the boxed state machine so it stays alive across awaits.
    /// </summary>
    /// <param name="stateMachine">The boxed state machine.</param>
    public void KeepAlive(object stateMachine) => m_StateMachine = stateMachine;

    /// <summary>
    /// Completes the operation successfully.
    /// </summary>
    public void SetResult() => SetResultDirect();

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception) => SetExceptionDirect(exception);

    protected override void ExecuteRange(int start, int end) { }

    // The boxed state machine is still needed by the continuation that finishes the async method, so it is only
    // released when the awaitable is consumed.
    protected override void DisposeResources() { }

    protected override void ReturnToPool()
    {
        (m_StateMachine as IStateMachineBox)?.Return();
        m_StateMachine = null;

        if (s_TCached is null)
        {
            s_TCached = this;
            return;
        }

        var taken = false;
        s_Lock.Enter(ref taken);
        try
        {
            pool.Push(this);
        }
        finally
        {
            s_Lock.Exit();
        }
    }
}

/// <summary>
/// Pooled completion source used by <see cref="AsynchronousMethodBuilder{TResult}"/> for
/// <c>async Asynchronous&lt;TResult&gt;</c> and <c>async IAwaitable&lt;TResult&gt;</c> methods.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal sealed class AsynchronousSource<TResult> : IValueTaskSource<TResult>
{
    [ThreadStatic] private static AsynchronousSource<TResult>? s_TCached;

    private static readonly Stack<AsynchronousSource<TResult>> pool = new();
    private static SpinLock s_Lock = new(false);

    private ManualResetValueTaskSourceCore<TResult> m_Core;

    /// <summary>
    /// Keeps the boxed async state machine alive until the operation completes.
    /// </summary>
    private object? m_StateMachine;

    public AsynchronousSource()
    {
        m_Core.RunContinuationsAsynchronously = true;
    }

    /// <summary>
    /// Rents a cleared source from the pool.
    /// </summary>
    /// <returns>The rented source.</returns>
    public static AsynchronousSource<TResult> Rent()
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
            return pool.Count > 0 ? pool.Pop() : new AsynchronousSource<TResult>();
        }
        finally
        {
            s_Lock.Exit();
        }
    }

    /// <summary>
    /// Resets the source for a new async method invocation.
    /// </summary>
    public void Prepare()
    {
        m_StateMachine = null;
        m_Core.Reset();
    }

    /// <summary>
    /// Gets the awaitable handed to the caller of the async method.
    /// </summary>
    public Asynchronous<TResult> Awaitable => new(this, m_Core.Version);

    /// <summary>
    /// Stores the boxed state machine so it stays alive across awaits.
    /// </summary>
    /// <param name="stateMachine">The boxed state machine.</param>
    public void KeepAlive(object stateMachine) => m_StateMachine = stateMachine;

    /// <summary>
    /// Completes the operation successfully with the given result.
    /// </summary>
    /// <param name="result">The result.</param>
    public void SetResult(TResult result) => m_Core.SetResult(result);

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception) => m_Core.SetException(exception);

    /// <summary>
    /// Gets the completion status of the operation.
    /// </summary>
    /// <param name="token">The awaitable token.</param>
    /// <returns>The status.</returns>
    public ValueTaskSourceStatus GetStatus(short token) => m_Core.GetStatus(token);

    /// <summary>
    /// Schedules a continuation on the operation.
    /// </summary>
    /// <param name="continuation">The continuation.</param>
    /// <param name="state">The continuation state.</param>
    /// <param name="token">The awaitable token.</param>
    /// <param name="flags">Continuation scheduling flags.</param>
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => m_Core.OnCompleted(continuation, state, token, flags);

    /// <summary>
    /// Gets the result and recycles the source.
    /// </summary>
    /// <param name="token">The awaitable token.</param>
    /// <returns>The result.</returns>
    public TResult GetResult(short token)
    {
        try
        {
            return m_Core.GetResult(token);
        }
        finally
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        (m_StateMachine as IStateMachineBox)?.Return();
        m_StateMachine = null;

        if (s_TCached is null)
        {
            s_TCached = this;
            return;
        }

        var taken = false;
        s_Lock.Enter(ref taken);
        try
        {
            pool.Push(this);
        }
        finally
        {
            s_Lock.Exit();
        }
    }
}