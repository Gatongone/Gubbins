using System.Runtime.ExceptionServices;

namespace Gubbins.Span;

/// <summary>
/// A fixed set of reusable worker threads shared by the parallel span operations.
/// </summary>
/// <remarks>
/// <para>
/// The pool is allocation-free after its first use: the worker threads, gates and the delegates handed
/// to <see cref="For"/> are reused instead of going through <see cref="System.Threading.Tasks.Parallel"/>, which
/// allocates tasks and range state on every call.
/// </para>
/// <para>
/// Each worker has its own gate: the caller raises a signal flag and only touches the kernel event when the
/// worker has already published that it is blocked. Workers spin briefly before blocking, which removes the
/// multi-microsecond wake-up latency when dispatches arrive back to back (the common case in a frame loop) while
/// still parking the cores when the pool is idle. The caller spin-waits for the batch to finish before falling
/// back to its completion event.
/// </para>
/// <para>
/// The pool is neither reentrant nor safe for concurrent callers. A parallel body must not start another pooled
/// operation, and different application threads must not call into the pool at the same time.
/// </para>
/// </remarks>
internal static class ParallelWorkerPool
{
    /// <summary>
    /// Number of spin rounds a worker performs before parking. Each round is <see cref="SPIN_ITERATIONS"/>
    /// pause instructions, roughly a microsecond in total.
    /// </summary>
    private const int WORKER_SPIN_ROUNDS = 64;

    /// <summary>
    /// Number of pause instructions per spin round.
    /// </summary>
    private const int SPIN_ITERATIONS = 64;

    /// <summary>
    /// Number of spin rounds the caller performs before blocking on the completion event.
    /// </summary>
    private const int CALLER_SPIN_ROUNDS = 64;

    /// <summary>
    /// Number of logical processors reserved for threads outside the pool: the calling thread that waits for a
    /// batch plus one spare for the host (GC, engine, application threads).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A parallel dispatch from <see cref="For"/> does not hand the caller a range: the caller signals the workers
    /// and then spins (or blocks) until the batch completes, so it stays runnable for the whole batch. Sizing the
    /// pool to <see cref="Environment.ProcessorCount"/> therefore makes <c>workers + 1</c> runnable threads for
    /// <see cref="Environment.ProcessorCount"/> hardware threads, and the caller has to be descheduled.
    /// </para>
    /// <para>
    /// Measured on a 6-core / 12-thread machine, 1M <c>int</c> components (16 MB working set, inside L3), with
    /// BenchmarkDotNet: 12 workers 227.8 µs versus 10 workers 32.8 µs — a 6.9x cliff. The cliff disappears when
    /// the same kernel becomes bandwidth bound (4M elements: all worker counts within 10%), so its magnitude
    /// depends on the workload while its mechanism does not.
    /// </para>
    /// <para>
    /// The exact headroom beyond the caller is tuning, not correctness: on the machine above the plateau spans
    /// 8–11 workers (within 30%, mostly noise) with 9–10 the steadiest. Reserving two is the portable choice;
    /// no offset should be read as a portable optimum.
    /// </para>
    /// </remarks>
    private const int RESERVED_PROCESSORS = 2;

    private static readonly Worker[] workers;
    private static readonly ManualResetEventSlim done = new(false);

    private static Action<int>? s_Action;
    private static int          s_Count;
    private static int          s_Pending;
    private static Exception?   s_Exception;

    static ParallelWorkerPool()
    {
        var count = Math.Max(1, Environment.ProcessorCount - RESERVED_PROCESSORS);
        workers = new Worker[count];

        for (var i = 0; i < count; i++)
        {
            workers[i] = new Worker(i);
            var thread = new Thread(workers[i].Loop)
            {
                IsBackground = true,
                Name         = "Gubbins.Span.Parallel"
            };
            thread.Start();
        }
    }

    /// <summary>
    /// Gets the number of worker threads in the pool.
    /// </summary>
    public static int ThreadCount => workers.Length;

    /// <summary>
    /// Runs <paramref name="action"/> once for every index in <c>[0, count)</c> across the pool threads.
    /// </summary>
    /// <param name="count">The number of work items.</param>
    /// <param name="action">The action invoked with each work item index.</param>
    public static void For(int count, Action<int> action)
    {
        if (count <= 0)
        {
            return;
        }

        if (count == 1)
        {
            action(0);
            return;
        }

        if (count > workers.Length)
        {
            count = workers.Length;
        }

        s_Action    = action;
        s_Count     = count;
        s_Exception = null;
        Volatile.Write(ref s_Pending, count);

        // Wake exactly the workers that own a range in this batch. Each gate is consumed once per signal, so a
        // worker is guaranteed to observe the batch fields written above before it runs.
        done.Reset();
        for (var i = 0; i < count; i++)
        {
            workers[i].Signal();
        }

        // Spin briefly for the batch to finish; back-to-back dispatches complete well inside this window, so the
        // caller normally avoids the kernel wait entirely. Spin on the done event rather than the pending count:
        // the last worker decrements the count *before* setting the event, and returning early would let that
        // delayed set complete a later batch prematurely.
        for (var round = 0; round < CALLER_SPIN_ROUNDS && !done.IsSet; round++)
        {
            Thread.SpinWait(SPIN_ITERATIONS);
        }

        if (!done.IsSet)
        {
            done.Wait();
        }

        s_Action = null;
        s_Count  = 0;

        var exception = s_Exception;
        s_Exception = null;
        if (exception != null)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private sealed class Worker(int index)
    {
        /// <summary>
        /// Set by the caller when this worker has a range in the current batch.
        /// </summary>
        private int m_Signal;

        /// <summary>
        /// Parking gate. The caller always releases one permit per batch, so a signal can never be lost even
        /// if it races with the spin-to-block transition. Permits left over from such a race only cause one
        /// harmless spurious wake-up.
        /// </summary>
        private readonly SemaphoreSlim m_Gate = new(0, int.MaxValue);

        public void Signal()
        {
            // Publish the work before releasing the permit so a waking worker observes the batch fields.
            Volatile.Write(ref m_Signal, 1);
            m_Gate.Release();
        }

        public void Loop()
        {
            while (true)
            {
                WaitForSignal();

                try
                {
                    var action = s_Action;
                    if (action != null && index < s_Count)
                    {
                        action(index);
                    }
                }
                catch (Exception exception)
                {
                    // Surface the first worker exception on the calling thread instead of crashing the process.
                    Interlocked.CompareExchange(ref s_Exception, exception, null);
                }
                finally
                {
                    if (Interlocked.Decrement(ref s_Pending) == 0)
                    {
                        done.Set();
                    }
                }
            }
        }

        private void WaitForSignal()
        {
            while (true)
            {
                for (var round = 0; round < WORKER_SPIN_ROUNDS; round++)
                {
                    if (Volatile.Read(ref m_Signal) != 0)
                    {
                        // Consume this batch's permit if it has already been released. If the release has not
                        // happened yet, the leftover permit causes a single spurious wake later.
                        m_Gate.Wait(0);
                        Volatile.Write(ref m_Signal, 0);
                        return;
                    }

                    Thread.SpinWait(SPIN_ITERATIONS);
                }

                m_Gate.Wait();

                if (Volatile.Read(ref m_Signal) != 0)
                {
                    Volatile.Write(ref m_Signal, 0);
                    return;
                }

                // A permit left over from a racing release: loop back and park again.
            }
        }
    }
}
