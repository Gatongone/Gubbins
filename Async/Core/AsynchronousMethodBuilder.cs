using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
namespace Gubbins.Async;

/// <summary>
/// Async method builder that lets a method be declared <c>async Asynchronous</c>.
/// </summary>
/// <remarks>
/// The completion source and the boxed state machine are both pooled. A call that never suspends allocates
/// nothing; a suspended call rents its box from a per-state-machine pool and returns it when the awaitable is
/// consumed, so the steady state is allocation-free.
/// </remarks>
public struct AsynchronousMethodBuilder
{
    private AsynchronousSource? m_Source;
    private IStateMachineBox?    m_Box;

    /// <summary>
    /// Creates the builder.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public static AsynchronousMethodBuilder Create() => default;

    /// <summary>
    /// Gets the awaitable returned to the caller of the async method.
    /// </summary>
    public Asynchronous Task => m_Source?.Awaitable ?? default;

    /// <summary>
    /// Starts the async state machine.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        var source = AsynchronousSource.Rent();
        source.Prepare();
        m_Source = source;

        try
        {
            stateMachine.MoveNext();
        }
        catch (Exception exception)
        {
            SetException(exception);
        }
    }

    /// <summary>
    /// Associates the builder with a state machine. Not used outside debuggers.
    /// </summary>
    /// <param name="stateMachine">The state machine.</param>
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    /// <summary>
    /// Completes the operation successfully.
    /// </summary>
    public void SetResult()
    {
        m_Box?.Complete();
        m_Source?.SetResult();
    }

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception)
    {
        m_Box?.Complete();
        m_Source?.SetException(exception);
    }

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that flows the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = GetOrCreateBox(ref stateMachine, captureContext: false);
        awaiter.OnCompleted(box.MoveNext);
    }

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that does not flow the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = GetOrCreateBox(ref stateMachine, captureContext: true);
        awaiter.UnsafeOnCompleted(box.MoveNext);
    }

    /// <summary>
    /// Returns the box that already hosts the state machine, or rents one and copies the state machine into it on
    /// the first suspension. Later suspensions reuse the same box: the builder reference is stored before the copy,
    /// so the boxed state machine carries it too.
    /// </summary>
    private IStateMachineBox GetOrCreateBox<TStateMachine>(ref TStateMachine stateMachine, bool captureContext)
        where TStateMachine : IAsyncStateMachine
    {
        var box = m_Box;
        if (box is not null)
        {
            return box;
        }

        StateMachineBox<TStateMachine>.SetStateMachine(ref stateMachine, ref m_Box, captureContext);
        box = m_Box!;
        m_Source!.KeepAlive(box);
        return box;
    }
}

/// <summary>
/// Async method builder that lets a method be declared <c>async IAwaitable</c>.
/// </summary>
/// <remarks>
/// The returned <see cref="IAwaitable"/> boxes the underlying <see cref="Asynchronous"/> value, so this shape
/// allocates one small object per invocation in addition to the boxed state machine.
/// </remarks>
public struct IAwaitableMethodBuilder
{
    private AsynchronousMethodBuilder m_Builder;

    /// <summary>
    /// Creates the builder.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public static IAwaitableMethodBuilder Create() => default;

    /// <summary>
    /// Gets the awaitable returned to the caller of the async method.
    /// </summary>
    public IAwaitable Task => m_Builder.Task;

    /// <summary>
    /// Starts the async state machine.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => m_Builder.Start(ref stateMachine);

    /// <summary>
    /// Associates the builder with a state machine. Not used outside debuggers.
    /// </summary>
    /// <param name="stateMachine">The state machine.</param>
    public void SetStateMachine(IAsyncStateMachine stateMachine) => m_Builder.SetStateMachine(stateMachine);

    /// <summary>
    /// Completes the operation successfully.
    /// </summary>
    public void SetResult() => m_Builder.SetResult();

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception) => m_Builder.SetException(exception);

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that flows the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => m_Builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that does not flow the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => m_Builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}

/// <summary>
/// Non-generic handle used by the pooled completion source to return a boxed state machine to its type pool.
/// </summary>
internal interface IStateMachineBox
{
    /// <summary>
    /// Gets the cached continuation delegate.
    /// </summary>
    Action MoveNext { get; }

    /// <summary>
    /// Marks the state machine as finished. The box must not be reused before this is set.
    /// </summary>
    void Complete();

    /// <summary>
    /// Clears the box and returns it to the pool.
    /// </summary>
    void Return();
}

/// <summary>
/// Heap box for an async state machine that suspended. Boxes are pooled per state machine type, so a suspended
/// async call allocates only when the pool is empty. The box also restores the captured execution context when
/// the builder had to schedule an awaiter that does not flow it itself.
/// </summary>
internal sealed class StateMachineBox<TStateMachine> : IStateMachineBox where TStateMachine : IAsyncStateMachine
{
    private static readonly ConcurrentQueue<StateMachineBox<TStateMachine>> pool = new();

    private readonly object m_Gate = new();
    private TStateMachine m_StateMachine;
    private ExecutionContext? m_Context;
    private bool m_Running;
    private bool m_Completed;
    private bool m_ReturnRequested;
    private bool m_Returned;

    /// <summary>
    /// The continuation delegate, cached once per box so each await does not allocate a new delegate.
    /// </summary>
    public Action MoveNext { get; }

    private StateMachineBox()
    {
        MoveNext = Run;
    }

    /// <summary>
    /// Rents a box, stores it in the builder, then copies the state machine into the box. Storing the reference
    /// first means the copy carries it, so later suspensions reuse the same box instead of boxing again.
    /// </summary>
    /// <param name="stateMachine">The state machine to box.</param>
    /// <param name="boxField">The builder field that must reference the box.</param>
    /// <param name="captureContext">Whether to capture the execution context for the continuation.</param>
    public static void SetStateMachine(
        ref TStateMachine stateMachine,
        ref IStateMachineBox? boxField,
        bool captureContext)
    {
        if (!pool.TryDequeue(out var box))
        {
            box = new StateMachineBox<TStateMachine>();
        }

        boxField = box;
        box.m_StateMachine = stateMachine;
        box.m_Context = captureContext ? ExecutionContext.Capture() : null;
        box.m_Running = false;
        box.m_Completed = false;
        box.m_ReturnRequested = false;
        box.m_Returned = false;
    }

    /// <inheritdoc/>
    public void Complete()
    {
        lock (m_Gate)
        {
            m_Completed = true;
        }
    }

    /// <inheritdoc/>
    public void Return()
    {
        // A consumer that observes completion on another thread can call GetResult while this box is still on the
        // stack (Run unwinding). The gate makes exactly one of Return and the end of Run release the box, and the
        // completed flag guarantees the box is never released before the state machine actually finished.
        lock (m_Gate)
        {
            m_ReturnRequested = true;
            if (!m_Running && m_Completed)
            {
                ReturnToPool();
            }
        }
    }

    private void Run()
    {
        lock (m_Gate)
        {
            m_Running = true;
        }

        try
        {
            var context = m_Context;
            if (context is null)
            {
                m_StateMachine.MoveNext();
                return;
            }

            m_Context = null;
            ExecutionContext.Run(context, static state => ((StateMachineBox<TStateMachine>)state!).m_StateMachine.MoveNext(), this);
        }
        finally
        {
            lock (m_Gate)
            {
                m_Running = false;
                if (m_ReturnRequested && m_Completed)
                {
                    ReturnToPool();
                }
            }
        }
    }

    /// <summary>
    /// Releases the box. Must be called under <see cref="m_Gate"/> so only one of the two release paths wins.
    /// </summary>
    private void ReturnToPool()
    {
        if (m_Returned)
        {
            return;
        }

        m_Returned = true;
        m_StateMachine = default!;
        m_Context = null;
        pool.Enqueue(this);
    }
}

/// <summary>
/// Async method builder that lets a method be declared <c>async Asynchronous&lt;TResult&gt;</c>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <remarks>
/// The completion source and the boxed state machine are both pooled. A call that never suspends allocates
/// nothing; a suspended call rents its box from a per-state-machine pool and returns it when the awaitable is
/// consumed, so the steady state is allocation-free.
/// </remarks>
public struct AsynchronousMethodBuilder<TResult>
{
    private AsynchronousSource<TResult>? m_Source;
    private IStateMachineBox?             m_Box;

    /// <summary>
    /// Creates the builder.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public static AsynchronousMethodBuilder<TResult> Create() => default;

    /// <summary>
    /// Gets the awaitable returned to the caller of the async method.
    /// </summary>
    public Asynchronous<TResult> Task => m_Source is null ? default : m_Source.Awaitable;

    /// <summary>
    /// Starts the async state machine.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
    {
        var source = AsynchronousSource<TResult>.Rent();
        source.Prepare();
        m_Source = source;

        try
        {
            stateMachine.MoveNext();
        }
        catch (Exception exception)
        {
            SetException(exception);
        }
    }

    /// <summary>
    /// Associates the builder with a state machine. Not used outside debuggers.
    /// </summary>
    /// <param name="stateMachine">The state machine.</param>
    public void SetStateMachine(IAsyncStateMachine stateMachine) { }

    /// <summary>
    /// Completes the operation successfully with the given result.
    /// </summary>
    /// <param name="result">The result.</param>
    public void SetResult(TResult result)
    {
        m_Box?.Complete();
        m_Source?.SetResult(result);
    }

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception)
    {
        m_Box?.Complete();
        m_Source?.SetException(exception);
    }

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that flows the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = GetOrCreateBox(ref stateMachine, captureContext: false);
        awaiter.OnCompleted(box.MoveNext);
    }

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that does not flow the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
    {
        var box = GetOrCreateBox(ref stateMachine, captureContext: true);
        awaiter.UnsafeOnCompleted(box.MoveNext);
    }

    /// <summary>
    /// Returns the box that already hosts the state machine, or rents one and copies the state machine into it on
    /// the first suspension. Later suspensions reuse the same box: the builder reference is stored before the copy,
    /// so the boxed state machine carries it too.
    /// </summary>
    private IStateMachineBox GetOrCreateBox<TStateMachine>(ref TStateMachine stateMachine, bool captureContext)
        where TStateMachine : IAsyncStateMachine
    {
        var box = m_Box;
        if (box is not null)
        {
            return box;
        }

        StateMachineBox<TStateMachine>.SetStateMachine(ref stateMachine, ref m_Box, captureContext);
        box = m_Box!;
        m_Source!.KeepAlive(box);
        return box;
    }
}

/// <summary>
/// Async method builder that lets a method be declared <c>async IAwaitable&lt;TResult&gt;</c>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <remarks>
/// The returned <see cref="IAwaitable{TResult}"/> boxes the underlying <see cref="Asynchronous{TResult}"/>
/// value, so this shape allocates one extra small object per invocation.
/// </remarks>
public struct IAwaitableMethodBuilder<TResult>
{
    private AsynchronousMethodBuilder<TResult> m_Builder;

    /// <summary>
    /// Creates the builder.
    /// </summary>
    /// <returns>The builder instance.</returns>
    public static IAwaitableMethodBuilder<TResult> Create() => default;

    /// <summary>
    /// Gets the awaitable returned to the caller of the async method.
    /// </summary>
    public IAwaitable<TResult> Task => m_Builder.Task;

    /// <summary>
    /// Starts the async state machine.
    /// </summary>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="stateMachine">The state machine.</param>
    public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        => m_Builder.Start(ref stateMachine);

    /// <summary>
    /// Associates the builder with a state machine. Not used outside debuggers.
    /// </summary>
    /// <param name="stateMachine">The state machine.</param>
    public void SetStateMachine(IAsyncStateMachine stateMachine) => m_Builder.SetStateMachine(stateMachine);

    /// <summary>
    /// Completes the operation successfully with the given result.
    /// </summary>
    /// <param name="result">The result.</param>
    public void SetResult(TResult result) => m_Builder.SetResult(result);

    /// <summary>
    /// Completes the operation with an exception.
    /// </summary>
    /// <param name="exception">The exception to surface to the awaiter.</param>
    public void SetException(Exception exception) => m_Builder.SetException(exception);

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that flows the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : INotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => m_Builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

    /// <summary>
    /// Schedules the state machine continuation for an awaiter that does not flow the execution context.
    /// </summary>
    /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
    /// <typeparam name="TStateMachine">The state machine type.</typeparam>
    /// <param name="awaiter">The awaiter.</param>
    /// <param name="stateMachine">The state machine.</param>
    public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
        where TAwaiter : ICriticalNotifyCompletion
        where TStateMachine : IAsyncStateMachine
        => m_Builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
}