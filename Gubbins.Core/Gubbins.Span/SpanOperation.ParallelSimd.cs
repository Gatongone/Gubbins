using System.Numerics;
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Gubbins.Span;

internal static class ParallelSimdExecutor
{
    private readonly struct ChunkActionState(Action<int, int> body)
    {
        public readonly Action<int, int> Body = body;
    }

    private sealed class ChunkWorker<TState>
    {
        private int                       m_Length;
        private int                       m_ChunkSize;
        private TState                    m_State = default!;
        private Action<int, int, TState>? m_Body;

        private static readonly ConcurrentQueue<ChunkWorker<TState>> pool = new();

        public readonly Action<int> ExecuteAction;

        private ChunkWorker()
        {
            ExecuteAction = Execute;
        }

        public static ChunkWorker<TState> Rent(int length, int chunkSize, TState state, Action<int, int, TState> body)
        {
            if (!pool.TryDequeue(out var worker))
            {
                worker = new ChunkWorker<TState>();
            }

            worker.Initialize(length, chunkSize, state, body);
            return worker;
        }

        public static void Return(ChunkWorker<TState> worker)
        {
            worker.m_Length    = 0;
            worker.m_ChunkSize = 0;
            worker.m_State     = default!;
            worker.m_Body      = null;
            pool.Enqueue(worker);
        }

        private void Initialize(int length, int chunkSize, TState state, Action<int, int, TState> body)
        {
            m_Length    = length;
            m_ChunkSize = chunkSize;
            m_State     = state;
            m_Body      = body;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int worker)
        {
            var start = worker * m_ChunkSize;
            if (start >= m_Length)
            {
                return;
            }

            var end = Math.Min(start + m_ChunkSize, m_Length);
            m_Body!(start, end - start, m_State);
        }
    }

    public static void ParallelChunks(int length, Action<int, int> body) =>
        ParallelChunks(length, new ChunkActionState(body), static (start, len, state) => state.Body(start, len));

    /// <summary>
    /// Minimum number of elements each worker must own before spawning it is worth the wake-up cost.
    /// The worker count is derived from the work length, so a short span runs inline and a long span scales up
    /// to the number of pool threads instead of always waking every worker.
    /// </summary>
    private const int MIN_ELEMENTS_PER_WORKER = 32_768;

    public static void ParallelChunks<TState>(int length, TState state, Action<int, int, TState> body)
    {
        if (length <= 0)
        {
            return;
        }

        var threadCount = ParallelWorkerPool.ThreadCount;
        ParallelChunks(length, Math.Min(threadCount, length / MIN_ELEMENTS_PER_WORKER), state, body);
    }

    /// <summary>
    /// Runs <paramref name="body"/> over contiguous chunks using an explicit worker count.
    /// </summary>
    /// <typeparam name="TState">The state passed to each worker.</typeparam>
    /// <param name="length">The total number of elements.</param>
    /// <param name="workerCount">The requested worker count; clamped to <c>[1, ThreadCount]</c>.</param>
    /// <param name="state">The state passed to each worker.</param>
    /// <param name="body">The body invoked as <c>(start, length, state)</c>.</param>
    public static void ParallelChunks<TState>(int length, int workerCount, TState state, Action<int, int, TState> body)
    {
        if (length <= 0)
        {
            return;
        }

        var workers = Math.Clamp(workerCount, 1, ParallelWorkerPool.ThreadCount);
        if (workers <= 1)
        {
            body(0, length, state);
            return;
        }

        var chunkSize   = (length + workers - 1) / workers;
        var workerState = ChunkWorker<TState>.Rent(length, chunkSize, state, body);
        try
        {
            ParallelWorkerPool.For(workers, workerState.ExecuteAction);
        }
        finally
        {
            ChunkWorker<TState>.Return(workerState);
        }
    }
}

internal static class ParallelSimdSpanRunner
{
    public delegate void OperandOp<T>(Span<T> src, T operand, Span<T> result) where T : unmanaged;

    public delegate void PairOp<T>(Span<T> left, Span<T> right, Span<T> result) where T : unmanaged;

    public delegate void UnaryOp<T>(Span<T> src, Span<T> result) where T : unmanaged;

    public delegate void UnaryProjectOp<TIn, TOut>(Span<TIn> src, Span<TOut> result)
        where TIn : unmanaged where TOut : unmanaged;

    public delegate void TernaryOp<T>(Span<T> x, Span<T> y, Span<T> z, Span<T> result) where T : unmanaged;

    public delegate void SpanScalarOp<T>(Span<T> src, T scalar, Span<T> result) where T : unmanaged;

    public delegate void ShiftOp<T>(Span<T> src, int count, Span<T> result) where T : unmanaged;

    public delegate void SpanTwoScalarOp<T>(Span<T> src, T a, T b, Span<T> result) where T : unmanaged;

    public delegate void SpanScalarTernaryOp<T>(Span<T> x, T y, Span<T> z, Span<T> result) where T : unmanaged;

    public delegate void SpanSpanScalarOp<T>(Span<T> src, Span<T> normal, T eta, Span<T> result) where T : unmanaged;

    public delegate void TernaryProjectOp<TIn, TOther, TOut>(Span<TIn> a, Span<TIn> b, Span<TOther> c, Span<TOut> result)
        where TIn : unmanaged where TOther : unmanaged where TOut : unmanaged;

    public delegate T SimdReduceOp<T>(Span<T> span) where T : unmanaged;

    public delegate bool CompareOp<T>(T left, T right) where T : unmanaged;

    private unsafe struct OperandState<T> where T : unmanaged
    {
        public T*           Src;
        public T            Operand;
        public T*           Dst;
        public OperandOp<T> Op;
    }

    private unsafe struct PairState<T> where T : unmanaged
    {
        public T*        Left;
        public T*        Right;
        public T*        Dst;
        public PairOp<T> Op;
    }

    private unsafe struct UnaryState<T> where T : unmanaged
    {
        public T*         Src;
        public T*         Dst;
        public UnaryOp<T> Op;
    }

    private unsafe struct UnaryProjectState<TIn, TOut>
        where TIn : unmanaged where TOut : unmanaged
    {
        public TIn*                      Src;
        public TOut*                     Dst;
        public UnaryProjectOp<TIn, TOut> Op;
    }

    private unsafe struct TernaryState<T> where T : unmanaged
    {
        public T*           X;
        public T*           Y;
        public T*           Z;
        public T*           Dst;
        public TernaryOp<T> Op;
    }

    private unsafe struct SpanScalarState<T> where T : unmanaged
    {
        public T*              Src;
        public T               Scalar;
        public T*              Dst;
        public SpanScalarOp<T> Op;
    }

    private unsafe struct ShiftState<T> where T : unmanaged
    {
        public T*         Src;
        public int        Count;
        public T*         Dst;
        public ShiftOp<T> Op;
    }

    private unsafe struct SpanTwoScalarsState<T> where T : unmanaged
    {
        public T*                 Src;
        public T                  A;
        public T                  B;
        public T*                 Dst;
        public SpanTwoScalarOp<T> Op;
    }

    private unsafe struct SpanScalarTernaryState<T> where T : unmanaged
    {
        public T*                     X;
        public T                      Y;
        public T*                     Z;
        public T*                     Dst;
        public SpanScalarTernaryOp<T> Op;
    }

    private unsafe struct SpanSpanScalarState<T> where T : unmanaged
    {
        public T*                  Src;
        public T*                  Normal;
        public T                   Eta;
        public T*                  Dst;
        public SpanSpanScalarOp<T> Op;
    }

    private unsafe struct TernaryProjectState<TIn, TOther, TOut>
        where TIn : unmanaged where TOther : unmanaged where TOut : unmanaged
    {
        public TIn*                                A;
        public TIn*                                B;
        public TOther*                             C;
        public TOut*                               Dst;
        public TernaryProjectOp<TIn, TOther, TOut> Op;
    }

    private unsafe struct PairProjectState<TIn, TOut>
        where TIn : unmanaged where TOut : unmanaged
    {
        public TIn*                     Left;
        public TIn*                     Right;
        public TOut*                    Dst;
        public PairProjectOp<TIn, TOut> Op;
    }

    private unsafe struct ReduceState<T> where T : unmanaged
    {
        public T*              Src;
        public int             ChunkSize;
        public T*              Partials;
        public SimdReduceOp<T> Reduce;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteOperandChunk<T>(int start, int len, OperandState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), state.Operand, new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecutePairChunk<T>(int start, int len, PairState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Left + start, len), new Span<T>(state.Right + start, len), new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteUnaryChunk<T>(int start, int len, UnaryState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteUnaryProjectChunk<TIn, TOut>(int start, int len, UnaryProjectState<TIn, TOut> state)
        where TIn : unmanaged where TOut : unmanaged =>
        state.Op(new Span<TIn>(state.Src + start, len), new Span<TOut>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteTernaryChunk<T>(int start, int len, TernaryState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.X + start, len), new Span<T>(state.Y + start, len), new Span<T>(state.Z + start, len), new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteSpanScalarChunk<T>(int start, int len, SpanScalarState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), state.Scalar, new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteShiftChunk<T>(int start, int len, ShiftState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), state.Count, new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteSpanTwoScalarsChunk<T>(int start, int len, SpanTwoScalarsState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), state.A, state.B, new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteSpanScalarTernaryChunk<T>(int start, int len, SpanScalarTernaryState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.X + start, len), state.Y, new Span<T>(state.Z + start, len), new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteSpanSpanScalarChunk<T>(int start, int len, SpanSpanScalarState<T> state) where T : unmanaged =>
        state.Op(new Span<T>(state.Src + start, len), new Span<T>(state.Normal + start, len), state.Eta, new Span<T>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteTernaryProjectChunk<TIn, TOther, TOut>(int start, int len, TernaryProjectState<TIn, TOther, TOut> state)
        where TIn : unmanaged where TOther : unmanaged where TOut : unmanaged =>
        state.Op(new Span<TIn>(state.A + start, len), new Span<TIn>(state.B + start, len), new Span<TOther>(state.C + start, len), new Span<TOut>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecutePairProjectChunk<TIn, TOut>(int start, int len, PairProjectState<TIn, TOut> state)
        where TIn : unmanaged where TOut : unmanaged =>
        state.Op(new Span<TIn>(state.Left + start, len), new Span<TIn>(state.Right + start, len), new Span<TOut>(state.Dst + start, len));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteReduceChunk<T>(int start, int len, ReduceState<T> state) where T : unmanaged
    {
        var index = start / state.ChunkSize;
        state.Partials[index] = state.Reduce(new Span<T>(state.Src + start, len));
    }

    public static unsafe T RunReduce<T>(Span<T> src, SimdReduceOp<T> reduceOp, CompareOp<T> compareOp) where T : unmanaged
    {
        if (src.Length == 0)
        {
            throw new ArgumentException("Span must not be empty", nameof(src));
        }

        var workers = Math.Min(Environment.ProcessorCount, src.Length);
        var chunkSize = (src.Length + workers - 1) / workers;
        var partials = ArrayPool<T>.Shared.Rent(workers);
        try
        {
            fixed (T* ps = src)
            fixed (T* pp = partials)
            {
                var state = new ReduceState<T> {Src = ps, ChunkSize = chunkSize, Partials = pp, Reduce = reduceOp};
                ParallelSimdExecutor.ParallelChunks(src.Length, state, ExecuteReduceChunk);
            }

            var result = partials[0];
            for (var index = 1; index < workers; index++)
            {
                if (compareOp(partials[index], result))
                {
                    result = partials[index];
                }
            }

            return result;
        }
        finally
        {
            ArrayPool<T>.Shared.Return(partials);
        }
    }

    public static unsafe void RunOperand<T>(Span<T> src, T operand, Span<T> result, OperandOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new OperandState<T> {Src = ps, Operand = operand, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteOperandChunk);
        }
    }

    public static unsafe void RunPair<T>(Span<T> left, Span<T> right, Span<T> result, PairOp<T> op) where T : unmanaged
    {
        fixed (T* pl = left)
        fixed (T* pr = right)
        fixed (T* pd = result)
        {
            var state = new PairState<T> {Left = pl, Right = pr, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecutePairChunk);
        }
    }

    public static unsafe void RunUnary<T>(Span<T> src, Span<T> result, UnaryOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new UnaryState<T> {Src = ps, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteUnaryChunk);
        }
    }

    public static unsafe void RunUnaryProject<TIn, TOut>(Span<TIn> src, Span<TOut> result, UnaryProjectOp<TIn, TOut> op)
        where TIn : unmanaged where TOut : unmanaged
    {
        fixed (TIn* ps = src)
        fixed (TOut* pd = result)
        {
            var state = new UnaryProjectState<TIn, TOut> {Src = ps, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteUnaryProjectChunk);
        }
    }

    public static unsafe void RunTernary<T>(Span<T> x, Span<T> y, Span<T> z, Span<T> result, TernaryOp<T> op) where T : unmanaged
    {
        fixed (T* px = x)
        fixed (T* py = y)
        fixed (T* pz = z)
        fixed (T* pd = result)
        {
            var state = new TernaryState<T> {X = px, Y = py, Z = pz, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteTernaryChunk);
        }
    }

    public static unsafe void RunSpanScalar<T>(Span<T> src, T scalar, Span<T> result, SpanScalarOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new SpanScalarState<T> {Src = ps, Scalar = scalar, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteSpanScalarChunk);
        }
    }

    public static unsafe void RunShift<T>(Span<T> src, int count, Span<T> result, ShiftOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new ShiftState<T> {Src = ps, Count = count, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteShiftChunk);
        }
    }

    public static unsafe void RunSpanTwoScalars<T>(Span<T> src, T a, T b, Span<T> result, SpanTwoScalarOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new SpanTwoScalarsState<T> {Src = ps, A = a, B = b, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteSpanTwoScalarsChunk);
        }
    }

    public static unsafe void RunSpanScalarTernary<T>(Span<T> x, T y, Span<T> z, Span<T> result, SpanScalarTernaryOp<T> op) where T : unmanaged
    {
        fixed (T* px = x)
        fixed (T* pz = z)
        fixed (T* pd = result)
        {
            var state = new SpanScalarTernaryState<T> {X = px, Y = y, Z = pz, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteSpanScalarTernaryChunk);
        }
    }

    public static unsafe void RunSpanSpanScalar<T>(Span<T> src, Span<T> normal, T eta, Span<T> result, SpanSpanScalarOp<T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pn = normal)
        fixed (T* pd = result)
        {
            var state = new SpanSpanScalarState<T> {Src = ps, Normal = pn, Eta = eta, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteSpanSpanScalarChunk);
        }
    }

    public static unsafe void RunTernaryProject<TIn, TOther, TOut>(
        Span<TIn> a,
        Span<TIn> b,
        Span<TOther> c,
        Span<TOut> result,
        TernaryProjectOp<TIn, TOther, TOut> op)
        where TIn : unmanaged where TOther : unmanaged where TOut : unmanaged
    {
        fixed (TIn* pa = a)
        fixed (TIn* pb = b)
        fixed (TOther* pc = c)
        fixed (TOut* pd = result)
        {
            var state = new TernaryProjectState<TIn, TOther, TOut> {A = pa, B = pb, C = pc, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecuteTernaryProjectChunk);
        }
    }

    public delegate void PairProjectOp<TIn, TOut>(Span<TIn> left, Span<TIn> right, Span<TOut> result)
        where TIn : unmanaged where TOut : unmanaged;

    public static unsafe void RunPairProject<TIn, TOut>(Span<TIn> left, Span<TIn> right, Span<TOut> result, PairProjectOp<TIn, TOut> op)
        where TIn : unmanaged where TOut : unmanaged
    {
        fixed (TIn* pl = left)
        fixed (TIn* pr = right)
        fixed (TOut* pd = result)
        {
            var state = new PairProjectState<TIn, TOut> {Left = pl, Right = pr, Dst = pd, Op = op};
            ParallelSimdExecutor.ParallelChunks(result.Length, state, ExecutePairProjectChunk);
        }
    }
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdNumberOperation<T> : ISpanNumberOperation<T> where T : unmanaged
{
    private static readonly bool                                   supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    private static readonly SimdNumberOperation<T>                 simd      = new();
    private static readonly ParallelSimdSpanRunner.OperandOp<T>    add       = simd.Add;
    private static readonly ParallelSimdSpanRunner.OperandOp<T>    subtract  = simd.Subtract;
    private static readonly ParallelSimdSpanRunner.OperandOp<T>    multiply  = simd.Multiply;
    private static readonly ParallelSimdSpanRunner.OperandOp<T>    divide    = simd.Divide;
    private static readonly ParallelSimdSpanRunner.OperandOp<T>    modulo    = simd.Modulo;
    private static readonly ParallelSimdSpanRunner.PairOp<T>       max       = simd.Max;
    private static readonly ParallelSimdSpanRunner.PairOp<T>       min       = simd.Min;
    private static readonly ParallelSimdSpanRunner.SimdReduceOp<T> getMax    = simd.GetMax;
    private static readonly ParallelSimdSpanRunner.SimdReduceOp<T> getMin    = simd.GetMin;
    private static readonly ParallelSimdSpanRunner.CompareOp<T>    greater   = Operations<T>.GreaterThan;
    private static readonly ParallelSimdSpanRunner.CompareOp<T>    less      = Operations<T>.LessThan;

    /// <inheritdoc />
    public bool Supported => supported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, add);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subtract(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, subtract);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Multiply(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, multiply);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Divide(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, divide);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Modulo(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, modulo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Max(Span<T> left, Span<T> right, Span<T> result) => ParallelSimdSpanRunner.RunPair(left, right, result, max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMax(Span<T> src) => ParallelSimdSpanRunner.RunReduce(src, getMax, greater);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Min(Span<T> left, Span<T> right, Span<T> result) => ParallelSimdSpanRunner.RunPair(left, right, result, min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMin(Span<T> src) => ParallelSimdSpanRunner.RunReduce(src, getMin, less);
}

#if NET7_0_OR_GREATER
/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdIntOperation : ISpanShift<int>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdIntOperation                    simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<int> shiftLeft  = simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<int> shiftRight = simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<int> src, int count, Span<int> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<int> src, int count, Span<int> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdLongOperation : ISpanShift<long>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdLongOperation                    simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<long> shiftLeft  = simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<long> shiftRight = simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<long> src, int count, Span<long> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<long> src, int count, Span<long> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUintOperation : ISpanShift<uint>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdUintOperation                    simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<uint> shiftLeft  = simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<uint> shiftRight = simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<uint> src, int count, Span<uint> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUlongOperation : ISpanShift<ulong>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdUlongOperation                    simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<ulong> shiftLeft  = simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<ulong> shiftRight = simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result) => ParallelSimdSpanRunner.RunShift(src, count, result, shiftRight);
}
#endif

internal sealed class ParallelSimdFloatOperation : ISpanRealOperation<float>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdFloatOperation                                simd             = new();
    private static readonly ParallelSimdSpanRunner.TernaryOp<float>           clamp            = simd.Clamp;
    private static readonly ParallelSimdSpanRunner.SpanTwoScalarOp<float>     clampScalar      = simd.Clamp;
    private static readonly ParallelSimdSpanRunner.TernaryOp<float>           lerp             = simd.Lerp;
    private static readonly ParallelSimdSpanRunner.SpanScalarTernaryOp<float> lerpMiddleScalar = simd.Lerp;
    private static readonly ParallelSimdSpanRunner.PairOp<float>              hypot            = simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<float>        hypotScalar      = simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<float>        powScalar        = simd.Pow;
    private static readonly ParallelSimdSpanRunner.PairOp<float>              pow              = simd.Pow;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             sqrt             = simd.Sqrt;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             ceiling          = simd.Ceiling;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             floor            = simd.Floor;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             round            = simd.Round;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             exp              = simd.Exp;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             log              = simd.Log;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             truncate         = simd.Truncate;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             sin              = simd.Sin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             cos              = simd.Cos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             tan              = simd.Tan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             sinh             = simd.Sinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             cosh             = simd.Cosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             tanh             = simd.Tanh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             asin             = simd.Asin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             acos             = simd.Acos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             atan             = simd.Atan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             asinh            = simd.Asinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             acosh            = simd.Acosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             atanh            = simd.Atanh;

    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result) =>
        ParallelSimdSpanRunner.RunTernary(src, min, max, result, clamp);

    public void Clamp(Span<float> src, float min, float max, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanTwoScalars(src, min, max, result, clampScalar);

    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result) =>
        ParallelSimdSpanRunner.RunTernary(x, y, amount, result, lerp);

    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalarTernary(x, y, amount, result, lerpMiddleScalar);

    public void Hypot(Span<float> x, Span<float> y, Span<float> result) =>
        ParallelSimdSpanRunner.RunPair(x, y, result, hypot);

    public void Hypot(Span<float> x, float y, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(x, y, result, hypotScalar);

    public void Pow(Span<float> src, float exponent, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(src, exponent, result, powScalar);

    public void Pow(Span<float> src, Span<float> exponent, Span<float> result) =>
        ParallelSimdSpanRunner.RunPair(src, exponent, result, pow);

    public void Sqrt(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, sqrt);
    public void Ceiling(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, ceiling);
    public void Floor(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, floor);
    public void Round(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, round);
    public void Exp(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, exp);
    public void Log(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, log);
    public void Truncate(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, truncate);
    public void Sin(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, sin);
    public void Cos(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, cos);
    public void Tan(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, tan);
    public void Sinh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, sinh);
    public void Cosh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, cosh);
    public void Tanh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, tanh);
    public void Asin(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, asin);
    public void Acos(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, acos);
    public void Atan(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, atan);
    public void Asinh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, asinh);
    public void Acosh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, acosh);
    public void Atanh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, atanh);
}

internal sealed class ParallelSimdDoubleOperation : ISpanRealOperation<double>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdDoubleOperation                                simd             = new();
    private static readonly ParallelSimdSpanRunner.TernaryOp<double>           clamp            = simd.Clamp;
    private static readonly ParallelSimdSpanRunner.SpanTwoScalarOp<double>     clampScalar      = simd.Clamp;
    private static readonly ParallelSimdSpanRunner.TernaryOp<double>           lerp             = simd.Lerp;
    private static readonly ParallelSimdSpanRunner.SpanScalarTernaryOp<double> lerpMiddleScalar = simd.Lerp;
    private static readonly ParallelSimdSpanRunner.PairOp<double>              hypot            = simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<double>        hypotScalar      = simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<double>        powScalar        = simd.Pow;
    private static readonly ParallelSimdSpanRunner.PairOp<double>              pow              = simd.Pow;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             sqrt             = simd.Sqrt;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             ceiling          = simd.Ceiling;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             floor            = simd.Floor;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             round            = simd.Round;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             exp              = simd.Exp;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             log              = simd.Log;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             truncate         = simd.Truncate;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             sin              = simd.Sin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             cos              = simd.Cos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             tan              = simd.Tan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             sinh             = simd.Sinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             cosh             = simd.Cosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             tanh             = simd.Tanh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             asin             = simd.Asin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             acos             = simd.Acos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             atan             = simd.Atan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             asinh            = simd.Asinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             acosh            = simd.Acosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             atanh            = simd.Atanh;

    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result) =>
        ParallelSimdSpanRunner.RunTernary(src, min, max, result, clamp);

    public void Clamp(Span<double> src, double min, double max, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanTwoScalars(src, min, max, result, clampScalar);

    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result) =>
        ParallelSimdSpanRunner.RunTernary(x, y, amount, result, lerp);

    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalarTernary(x, y, amount, result, lerpMiddleScalar);

    public void Hypot(Span<double> x, Span<double> y, Span<double> result) =>
        ParallelSimdSpanRunner.RunPair(x, y, result, hypot);

    public void Hypot(Span<double> x, double y, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(x, y, result, hypotScalar);

    public void Pow(Span<double> src, double exponent, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(src, exponent, result, powScalar);

    public void Pow(Span<double> src, Span<double> exponent, Span<double> result) =>
        ParallelSimdSpanRunner.RunPair(src, exponent, result, pow);

    public void Sqrt(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, sqrt);
    public void Ceiling(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, ceiling);
    public void Floor(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, floor);
    public void Round(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, round);
    public void Exp(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, exp);
    public void Log(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, log);
    public void Truncate(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, truncate);
    public void Sin(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, sin);
    public void Cos(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, cos);
    public void Tan(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, tan);
    public void Sinh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, sinh);
    public void Cosh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, cosh);
    public void Tanh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, tanh);
    public void Asin(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, asin);
    public void Acos(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, acos);
    public void Atan(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, atan);
    public void Asinh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, asinh);
    public void Acosh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, acosh);
    public void Atanh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, atanh);
}

internal sealed class ParallelSimdVector2Operation : ISpanVectorOperation<Vector2>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdVector2Operation                                             simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           dot                 = simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           cross               = simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector2>                          normalize           = simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector2, float>            length              = simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector2, float>            lengthSquared       = simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             distance            = simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             distanceSquared     = simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             angle               = simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           reflect             = simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           faceForward         = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector2>                 refract             = simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector2, float, Vector2> faceForwardIncident = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector2, float, Vector2> moveTowards         = simd.MoveTowards;

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(left, right, result, dot);
    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(left, right, result, cross);
    public void Normalize(Span<Vector2> src, Span<Vector2> result) => ParallelSimdSpanRunner.RunUnary(src, result, normalize);
    public void Length(Span<Vector2> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, length);
    public void LengthSquared(Span<Vector2> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, lengthSquared);
    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distance);
    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distanceSquared);
    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, angle);
    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, reflect);
    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, faceForward);
    public void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, refract);
    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, faceForwardIncident);
    public void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, moveTowards);
}

internal sealed class ParallelSimdVector3Operation : ISpanVectorOperation<Vector3>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdVector3Operation                                             simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           dot                 = simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           cross               = simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector3>                          normalize           = simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector3, float>            length              = simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector3, float>            lengthSquared       = simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             distance            = simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             distanceSquared     = simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             angle               = simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           reflect             = simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           faceForward         = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector3>                 refract             = simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector3, float, Vector3> faceForwardIncident = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector3, float, Vector3> moveTowards         = simd.MoveTowards;

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(left, right, result, dot);
    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(left, right, result, cross);
    public void Normalize(Span<Vector3> src, Span<Vector3> result) => ParallelSimdSpanRunner.RunUnary(src, result, normalize);
    public void Length(Span<Vector3> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, length);
    public void LengthSquared(Span<Vector3> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, lengthSquared);
    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distance);
    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distanceSquared);
    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, angle);
    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, reflect);
    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, faceForward);
    public void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, refract);
    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, faceForwardIncident);
    public void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, moveTowards);
}

internal sealed class ParallelSimdVector4Operation : ISpanVectorOperation<Vector4>
{
    private static readonly bool supported = Environment.ProcessorCount > 1 && Vector.IsHardwareAccelerated;
    public bool Supported => supported;
    private static readonly SimdVector4Operation                                             simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           dot                 = simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           cross               = simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector4>                          normalize           = simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector4, float>            length              = simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector4, float>            lengthSquared       = simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             distance            = simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             distanceSquared     = simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             angle               = simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           reflect             = simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           faceForward         = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector4>                 refract             = simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector4, float, Vector4> faceForwardIncident = simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector4, float, Vector4> moveTowards         = simd.MoveTowards;

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(left, right, result, dot);
    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(left, right, result, cross);
    public void Normalize(Span<Vector4> src, Span<Vector4> result) => ParallelSimdSpanRunner.RunUnary(src, result, normalize);
    public void Length(Span<Vector4> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, length);
    public void LengthSquared(Span<Vector4> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, lengthSquared);
    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distance);
    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, distanceSquared);
    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, angle);
    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, reflect);
    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, faceForward);
    public void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, refract);
    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, faceForwardIncident);
    public void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, moveTowards);
}