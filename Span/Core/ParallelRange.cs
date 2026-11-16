namespace Gubbins.Span;

/// <summary>
/// Runs a range body once across the pooled worker threads, splitting the index range into contiguous chunks.
/// </summary>
/// <remarks>
/// This is the bulk-dispatch counterpart to <see cref="ISpanNumberOperation{T}"/>: instead of invoking a span
/// operation once per segment, callers hand the whole element range to one dispatch, and each worker processes
/// a contiguous slice. Workers are woken once per call, so the per-segment synchronization cost disappears.
/// </remarks>
public static class ParallelRange
{
    /// <summary>
    /// Runs <paramref name="body"/> over contiguous ranges of <c>[0, length)</c> across the worker pool.
    /// The number of workers is derived from the length, at most one worker per 32,768 elements.
    /// </summary>
    /// <typeparam name="TState">The state passed to each worker.</typeparam>
    /// <param name="length">The total number of elements to process.</param>
    /// <param name="state">The state passed to each worker.</param>
    /// <param name="body">The body invoked as <c>(start, length, state)</c> for each worker range.</param>
    public static void For<TState>(int length, TState state, Action<int, int, TState> body) => ParallelSimdExecutor.ParallelChunks(length, state, body);

    /// <summary>
    /// Runs <paramref name="body"/> over contiguous ranges of <c>[0, length)</c> with an explicit worker count.
    /// </summary>
    /// <typeparam name="TState">The state passed to each worker.</typeparam>
    /// <param name="length">The total number of elements to process.</param>
    /// <param name="workerCount">The requested worker count; clamped to <c>[1, ThreadCount]</c>.</param>
    /// <param name="state">The state passed to each worker.</param>
    /// <param name="body">The body invoked as <c>(start, length, state)</c> for each worker range.</param>
    public static void For<TState>(int length, int workerCount, TState state, Action<int, int, TState> body)
        => ParallelSimdExecutor.ParallelChunks(length, workerCount, state, body);
}