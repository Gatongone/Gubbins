using System.Threading.Tasks.Sources;

namespace Gubbins.Async;

/// <summary>
/// Base class for pooled, allocation-free asynchronous operations that split a range of segments across worker
/// thread pool items and complete a <see cref="ValueTask"/> when the last worker finishes.
/// </summary>
/// <remarks>
/// <para>
/// An operation is rented from an <c>OperationPool&lt;TOperation&gt;</c>, prepared, started and then exposed to the
/// caller as an <see cref="Asynchronous"/>. The operation disposes its resources and returns itself to the
/// pool when the awaitable is consumed. The awaitable must be consumed exactly once and must not be awaited twice
/// or converted to a task and re-awaited.
/// </para>
/// <para>
/// No allocation is performed per operation once the pool and worker items are warm: completion uses
/// <see cref="ManualResetValueTaskSourceCore{T}"/>, exceptions are collected lazily, and workers are resumed
/// through pooled <see cref="ParallelWorkItem"/> instances.
/// </para>
/// <para>
/// Cancellation is observed between segments. An already-canceled token makes the entry point return a completed
/// value task; cancellation during execution stops the remaining segments and completes successfully.
/// </para>
/// </remarks>
public abstract class PooledValueTaskSource : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<bool> m_Core;
    private ParallelWorkItem[] m_WorkItems = [];
    private readonly object m_ExceptionGate = new();
    private Exception? m_FirstException;
    private List<Exception>? m_AdditionalExceptions;
    private int m_SegmentCount;
    private int m_WorkerCount;
    private int m_ChunkSize;
    private int m_Remaining;
    private int m_Released;
    private int m_Aborted;
    private short m_Token;
    private CancellationToken m_CancellationToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledValueTaskSource"/> class.
    /// </summary>
    protected PooledValueTaskSource()
    {
        // Completion continuations must never run inline on a worker: the caller may have moved on to other work
        // and the operation recycles itself as soon as the value task is consumed.
        m_Core.RunContinuationsAsynchronously = true;
    }

    /// <summary>
    /// Gets a value indicating whether cancellation or an abort request has been observed.
    /// </summary>
    protected bool IsCancellationRequested =>
        Volatile.Read(ref m_Aborted) != 0 || m_CancellationToken.IsCancellationRequested;

    /// <summary>
    /// Prepares the operation for a new batch. Call this before <see cref="Run"/>.
    /// </summary>
    /// <param name="segmentCount">The total number of segments to process.</param>
    /// <param name="workerCount">The number of workers that will process the segments.</param>
    /// <param name="chunkSize">The number of segments each worker owns.</param>
    /// <param name="cancellationToken">The token observed between segments.</param>
    protected void Prepare(int segmentCount, int workerCount, int chunkSize, CancellationToken cancellationToken)
    {
        m_SegmentCount = segmentCount;
        m_WorkerCount = workerCount;
        m_ChunkSize = chunkSize;
        m_CancellationToken = cancellationToken;
        m_Remaining = workerCount;
        m_Released = 0;
        m_Aborted = 0;
        m_FirstException = null;
        m_AdditionalExceptions = null;
        EnsureWorkItems(workerCount);
        m_Core.Reset();
        m_Token = m_Core.Version;
    }

    /// <summary>
    /// Creates the awaitable that represents this operation. Call this after <see cref="Run"/>.
    /// </summary>
    /// <returns>A single-consumption awaitable backed by this operation.</returns>
    protected Asynchronous CreateTask() => new(this, m_Token);

    /// <summary>
    /// Starts the prepared batch: runs inline for a single worker, otherwise queues one work item per worker.
    /// </summary>
    protected void Run()
    {
        if (m_WorkerCount <= 1)
        {
            ExecuteWorker(0);
            return;
        }

        for (var i = 0; i < m_WorkerCount; i++)
        {
            bool queued;
            try
            {
                queued = Queue(m_WorkItems[i]);
            }
            catch
            {
                queued = false;
            }

            if (queued)
            {
                continue;
            }

            ReportException(new InvalidOperationException("Failed to queue work item for chunk foreach iteration."));
            Volatile.Write(ref m_Aborted, 1);

            // Account for this and every worker that was not queued so the operation still completes.
            var unqueued = m_WorkerCount - i;
            if (Interlocked.Add(ref m_Remaining, -unqueued) == 0)
            {
                Complete();
            }

            return;
        }
    }

    /// <summary>
    /// Executes one worker range. Called by a <see cref="ParallelWorkItem"/> or inline for a single worker.
    /// </summary>
    /// <param name="workerIndex">The worker index.</param>
    internal void ExecuteWorker(int workerIndex)
    {
        try
        {
            if (IsCancellationRequested)
            {
                return;
            }

            var start = workerIndex * m_ChunkSize;
            var end = Math.Min(start + m_ChunkSize, m_SegmentCount);
            ExecuteRange(start, end);
        }
        catch (Exception exception)
        {
            ReportException(exception);
        }
        finally
        {
            if (Interlocked.Decrement(ref m_Remaining) == 0)
            {
                Complete();
            }
        }
    }

    /// <summary>
    /// Processes the segments in the given range. Implementations should check <see cref="IsCancellationRequested"/>
    /// between segments.
    /// </summary>
    /// <param name="start">The first segment index (inclusive).</param>
    /// <param name="end">The last segment index (exclusive).</param>
    protected abstract void ExecuteRange(int start, int end);

    /// <summary>
    /// Releases the unmanaged component views held by the operation. Called once, after every worker has finished.
    /// </summary>
    protected abstract void DisposeResources();

    /// <summary>
    /// Clears the operation's fields and returns it to its pool. Called once, when the value task is consumed.
    /// </summary>
    protected abstract void ReturnToPool();

    private void Complete()
    {
        try
        {
            DisposeResources();
        }
        catch (Exception exception)
        {
            ReportException(exception);
        }

        var first = m_FirstException;
        if (first is null)
        {
            m_Core.SetResult(true);
            return;
        }

        var additional = m_AdditionalExceptions;
        if (additional is null)
        {
            m_Core.SetException(new AggregateException(first));
            return;
        }

        var exceptions = new Exception[additional.Count + 1];
        exceptions[0] = first;
        additional.CopyTo(exceptions, 1);
        m_Core.SetException(new AggregateException(exceptions));
    }

    private void ReportException(Exception exception)
    {
        if (Volatile.Read(ref m_FirstException) is null &&
            Interlocked.CompareExchange(ref m_FirstException, exception, null) is null)
        {
            return;
        }

        lock (m_ExceptionGate)
        {
            (m_AdditionalExceptions ??= []).Add(exception);
        }
    }

    private void EnsureWorkItems(int count)
    {
        if (m_WorkItems.Length < count)
        {
            var items = new ParallelWorkItem[count];
            for (var i = 0; i < count; i++)
            {
                items[i] = new ParallelWorkItem(i);
            }

            m_WorkItems = items;
        }

        for (var i = 0; i < count; i++)
        {
            m_WorkItems[i].Owner = this;
        }
    }

    internal void SetResultDirect() => m_Core.SetResult(true);

    internal void SetExceptionDirect(Exception exception) => m_Core.SetException(exception);

    internal ValueTaskSourceStatus GetStatus(short token) => m_Core.GetStatus(token);

    internal void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => m_Core.OnCompleted(continuation, state, token, flags);

    internal void GetResult(short token)
    {
        try
        {
            m_Core.GetResult(token);
        }
        finally
        {
            if (Interlocked.Exchange(ref m_Released, 1) == 0)
            {
                ReturnToPool();
            }
        }
    }

    // Keep the interface implementation so the source can still be wrapped in a ValueTask for interop.
    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token) => GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => OnCompleted(continuation, state, token, flags);

    void IValueTaskSource.GetResult(short token) => GetResult(token);

    private static bool Queue(ParallelWorkItem item)
    {
#if NET8_0_OR_GREATER
        ThreadPool.UnsafeQueueUserWorkItem(item, preferLocal: false);
        return true;
#else
        return ThreadPool.UnsafeQueueUserWorkItem(queueCallback, item);
#endif
    }

#if !NET8_0_OR_GREATER
    private static readonly WaitCallback queueCallback = static state => ((ParallelWorkItem)state!).Execute();
#endif
}