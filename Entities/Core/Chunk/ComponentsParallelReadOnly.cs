using Gubbins.Span;

namespace Gubbins.Entities;

/// <summary>
/// Applies a caller-supplied read-only action to one segment slice produced by a parallel bulk dispatch.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <typeparam name="TState">The state passed to the action.</typeparam>
/// <param name="span">The slice of the current segment covered by this worker range.</param>
/// <param name="state">The state supplied to <c>ApplyParallel</c>.</param>
public delegate void ReadOnlySpanSegmentAction<T, in TState>(ReadOnlySpan<T> span, TState state) where T : unmanaged;

/// <summary>
/// Parallel bulk operations over a read-only query result.
/// </summary>
/// <remarks>
/// <para>
/// The reader's counterpart of <see cref="ComponentsParallelExtensions"/>: the same dispatch, but the action receives
/// <see cref="ReadOnlySpan{T}"/> slices and the chunks the dispatch walks are never marked as changed.
/// </para>
/// <para>
/// This is the shape a reduction takes — sum, minimum, count, copy out — where the write path's in-place span
/// operations do not apply. The state carries whatever the workers accumulate into, so a caller that writes the same
/// sink from several threads owns the synchronization of that sink; <see cref="System.Threading.Interlocked"/> over a
/// shared slot is enough for a sum.
/// </para>
/// </remarks>
public static class ComponentsParallelReadOnlyExtensions
{
    /// <summary>
    /// Applies <paramref name="action"/> to every element of <paramref name="components"/> using a single parallel
    /// dispatch across all segments. The action receives contiguous read-only slices of the component spans.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to read.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyParallel<T, TState>(
        this ReadOnlyChunkLayout<T> components,
        TState state,
        ReadOnlySpanSegmentAction<T, TState> action,
        int workerCount = 0)
        where T : unmanaged
    {
        var total = components.Count;
        if (total == 0)
        {
            return;
        }

        var dispatch = new ReadOnlyApplyState<T, TState>(components.Segments, action, state);
        if (workerCount <= 0)
        {
            ParallelRange.For(total, dispatch, static (start, length, value) => value.Apply(start, length));
            return;
        }

        ParallelRange.For(total, workerCount, dispatch, static (start, length, value) => value.Apply(start, length));
    }

    /// <summary>
    /// State for one read-only bulk parallel dispatch. It maps an element range onto the underlying segments and invokes
    /// the caller action for the intersecting slices.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <remarks>
    /// The read-only twin of the write path's dispatch state; the segment indexer already yields
    /// <see cref="ReadOnlySpan{T}"/>, so only the stored enumerable and the action type differ.
    /// </remarks>
    private readonly struct ReadOnlyApplyState<T, TState> where T : unmanaged
    {
        private readonly ReadOnlyChunkLayout<T>.SegmentsEnumerable m_Segments;
        private readonly ReadOnlySpanSegmentAction<T, TState> m_Action;
        private readonly TState m_State;

        public ReadOnlyApplyState(ReadOnlyChunkLayout<T>.SegmentsEnumerable segments, ReadOnlySpanSegmentAction<T, TState> action, TState state)
        {
            m_Segments = segments;
            m_Action = action;
            m_State = state;
        }

        public void Apply(int start, int length)
        {
            var end = start + length;
            var offset = 0;
            var segments = m_Segments;

            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var segmentEnd = offset + segment.Length;

                if (segmentEnd <= start)
                {
                    offset = segmentEnd;
                    continue;
                }

                if (offset >= end)
                {
                    return;
                }

                var localStart = Math.Max(start, offset) - offset;
                var localEnd = Math.Min(end, segmentEnd) - offset;
                if (localEnd > localStart)
                {
                    m_Action(segment.Slice(localStart, localEnd - localStart), m_State);
                }

                offset = segmentEnd;
                if (offset >= end)
                {
                    return;
                }
            }
        }
    }
}
