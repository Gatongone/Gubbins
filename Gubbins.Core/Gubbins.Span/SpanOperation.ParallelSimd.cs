using System.Numerics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

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

        private static readonly ConcurrentQueue<ChunkWorker<TState>> s_Pool = new();

        public readonly Action<int> ExecuteAction;

        private ChunkWorker()
        {
            ExecuteAction = Execute;
        }

        public static ChunkWorker<TState> Rent(int length, int chunkSize, TState state, Action<int, int, TState> body)
        {
            if (!s_Pool.TryDequeue(out var worker))
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
            s_Pool.Enqueue(worker);
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

    public static void ParallelChunks<TState>(int length, TState state, Action<int, int, TState> body)
    {
        if (length <= 0)
        {
            return;
        }

        var workers = Math.Min(Environment.ProcessorCount, length);
        var chunkSize = (length + workers - 1) / workers;
        var workerState = ChunkWorker<TState>.Rent(length, chunkSize, state, body);
        try
        {
            Parallel.For(0, workers, workerState.ExecuteAction);
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
internal sealed class ParallelSimdNumberOperations<T> : ISpanNumberOperations<T> where T : unmanaged
{
    private static readonly bool                                s_Supported = typeof(T).CheckType().IsNumberType && Vector.IsHardwareAccelerated;
    private static readonly SimdNumberOperations<T>             s_Simd      = new();
    private static readonly ParallelNumberOperations<T>         s_Parallel  = new();
    private static readonly ParallelSimdSpanRunner.OperandOp<T> s_Add       = s_Simd.Add;
    private static readonly ParallelSimdSpanRunner.OperandOp<T> s_Subtract  = s_Simd.Subtract;
    private static readonly ParallelSimdSpanRunner.OperandOp<T> s_Multiply  = s_Simd.Multiply;
    private static readonly ParallelSimdSpanRunner.OperandOp<T> s_Divide    = s_Simd.Divide;
    private static readonly ParallelSimdSpanRunner.OperandOp<T> s_Modulo    = s_Simd.Modulo;
    private static readonly ParallelSimdSpanRunner.PairOp<T>    s_Max       = s_Simd.Max;
    private static readonly ParallelSimdSpanRunner.PairOp<T>    s_Min       = s_Simd.Min;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, s_Add);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subtract(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, s_Subtract);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Multiply(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, s_Multiply);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Divide(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, s_Divide);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Modulo(Span<T> src, T operand, Span<T> result) => ParallelSimdSpanRunner.RunOperand(src, operand, result, s_Modulo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Max(Span<T> left, Span<T> right, Span<T> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Max);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMax(Span<T> src) => s_Parallel.GetMax(src);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Min(Span<T> left, Span<T> right, Span<T> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Min);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMin(Span<T> src) => s_Parallel.GetMin(src);
}

#if NET7_0_OR_GREATER
/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdIntOperation : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdIntOperation                    s_Simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<int> s_ShiftLeft  = s_Simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<int> s_ShiftRight = s_Simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<int> src, int count, Span<int> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<int> src, int count, Span<int> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdLongOperation : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdLongOperation                    s_Simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<long> s_ShiftLeft  = s_Simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<long> s_ShiftRight = s_Simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<long> src, int count, Span<long> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<long> src, int count, Span<long> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUintOperation : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdUintOperation                    s_Simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<uint> s_ShiftLeft  = s_Simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<uint> s_ShiftRight = s_Simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<uint> src, int count, Span<uint> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftRight);
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUlongOperation : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdUlongOperation                    s_Simd       = new();
    private static readonly ParallelSimdSpanRunner.ShiftOp<ulong> s_ShiftLeft  = s_Simd.ShiftLeft;
    private static readonly ParallelSimdSpanRunner.ShiftOp<ulong> s_ShiftRight = s_Simd.ShiftRight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftLeft);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result) => ParallelSimdSpanRunner.RunShift(src, count, result, s_ShiftRight);
}
#endif

internal sealed class ParallelSimdFloatOperation : ISpanRealOperations<float>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdFloatOperation                                s_Simd             = new();
    private static readonly ParallelSimdSpanRunner.TernaryOp<float>           s_Clamp            = s_Simd.Clamp;
    private static readonly ParallelSimdSpanRunner.SpanTwoScalarOp<float>     s_ClampScalar      = s_Simd.Clamp;
    private static readonly ParallelSimdSpanRunner.TernaryOp<float>           s_Lerp             = s_Simd.Lerp;
    private static readonly ParallelSimdSpanRunner.SpanScalarTernaryOp<float> s_LerpMiddleScalar = s_Simd.Lerp;
    private static readonly ParallelSimdSpanRunner.PairOp<float>              s_Hypot            = s_Simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<float>        s_HypotScalar      = s_Simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<float>        s_PowScalar        = s_Simd.Pow;
    private static readonly ParallelSimdSpanRunner.PairOp<float>              s_Pow              = s_Simd.Pow;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Sqrt             = s_Simd.Sqrt;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Ceiling          = s_Simd.Ceiling;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Floor            = s_Simd.Floor;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Round            = s_Simd.Round;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Exp              = s_Simd.Exp;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Log              = s_Simd.Log;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Truncate         = s_Simd.Truncate;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Sin              = s_Simd.Sin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Cos              = s_Simd.Cos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Tan              = s_Simd.Tan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Sinh             = s_Simd.Sinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Cosh             = s_Simd.Cosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Tanh             = s_Simd.Tanh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Asin             = s_Simd.Asin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Acos             = s_Simd.Acos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Atan             = s_Simd.Atan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Asinh            = s_Simd.Asinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Acosh            = s_Simd.Acosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<float>             s_Atanh            = s_Simd.Atanh;

    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result) =>
        ParallelSimdSpanRunner.RunTernary(src, min, max, result, s_Clamp);

    public void Clamp(Span<float> src, float min, float max, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanTwoScalars(src, min, max, result, s_ClampScalar);

    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result) =>
        ParallelSimdSpanRunner.RunTernary(x, y, amount, result, s_Lerp);

    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalarTernary(x, y, amount, result, s_LerpMiddleScalar);

    public void Hypot(Span<float> x, Span<float> y, Span<float> result) =>
        ParallelSimdSpanRunner.RunPair(x, y, result, s_Hypot);

    public void Hypot(Span<float> x, float y, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(x, y, result, s_HypotScalar);

    public void Pow(Span<float> src, float exponent, Span<float> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(src, exponent, result, s_PowScalar);

    public void Pow(Span<float> src, Span<float> exponent, Span<float> result) =>
        ParallelSimdSpanRunner.RunPair(src, exponent, result, s_Pow);

    public void Sqrt(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sqrt);
    public void Ceiling(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Ceiling);
    public void Floor(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Floor);
    public void Round(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Round);
    public void Exp(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Exp);
    public void Log(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Log);
    public void Truncate(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Truncate);
    public void Sin(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sin);
    public void Cos(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Cos);
    public void Tan(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Tan);
    public void Sinh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sinh);
    public void Cosh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Cosh);
    public void Tanh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Tanh);
    public void Asin(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Asin);
    public void Acos(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Acos);
    public void Atan(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Atan);
    public void Asinh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Asinh);
    public void Acosh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Acosh);
    public void Atanh(Span<float> src, Span<float> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Atanh);
}

internal sealed class ParallelSimdDoubleOperation : ISpanRealOperations<double>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdDoubleOperation                                s_Simd             = new();
    private static readonly ParallelSimdSpanRunner.TernaryOp<double>           s_Clamp            = s_Simd.Clamp;
    private static readonly ParallelSimdSpanRunner.SpanTwoScalarOp<double>     s_ClampScalar      = s_Simd.Clamp;
    private static readonly ParallelSimdSpanRunner.TernaryOp<double>           s_Lerp             = s_Simd.Lerp;
    private static readonly ParallelSimdSpanRunner.SpanScalarTernaryOp<double> s_LerpMiddleScalar = s_Simd.Lerp;
    private static readonly ParallelSimdSpanRunner.PairOp<double>              s_Hypot            = s_Simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<double>        s_HypotScalar      = s_Simd.Hypot;
    private static readonly ParallelSimdSpanRunner.SpanScalarOp<double>        s_PowScalar        = s_Simd.Pow;
    private static readonly ParallelSimdSpanRunner.PairOp<double>              s_Pow              = s_Simd.Pow;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Sqrt             = s_Simd.Sqrt;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Ceiling          = s_Simd.Ceiling;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Floor            = s_Simd.Floor;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Round            = s_Simd.Round;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Exp              = s_Simd.Exp;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Log              = s_Simd.Log;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Truncate         = s_Simd.Truncate;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Sin              = s_Simd.Sin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Cos              = s_Simd.Cos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Tan              = s_Simd.Tan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Sinh             = s_Simd.Sinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Cosh             = s_Simd.Cosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Tanh             = s_Simd.Tanh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Asin             = s_Simd.Asin;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Acos             = s_Simd.Acos;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Atan             = s_Simd.Atan;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Asinh            = s_Simd.Asinh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Acosh            = s_Simd.Acosh;
    private static readonly ParallelSimdSpanRunner.UnaryOp<double>             s_Atanh            = s_Simd.Atanh;

    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result) =>
        ParallelSimdSpanRunner.RunTernary(src, min, max, result, s_Clamp);

    public void Clamp(Span<double> src, double min, double max, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanTwoScalars(src, min, max, result, s_ClampScalar);

    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result) =>
        ParallelSimdSpanRunner.RunTernary(x, y, amount, result, s_Lerp);

    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalarTernary(x, y, amount, result, s_LerpMiddleScalar);

    public void Hypot(Span<double> x, Span<double> y, Span<double> result) =>
        ParallelSimdSpanRunner.RunPair(x, y, result, s_Hypot);

    public void Hypot(Span<double> x, double y, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(x, y, result, s_HypotScalar);

    public void Pow(Span<double> src, double exponent, Span<double> result) =>
        ParallelSimdSpanRunner.RunSpanScalar(src, exponent, result, s_PowScalar);

    public void Pow(Span<double> src, Span<double> exponent, Span<double> result) =>
        ParallelSimdSpanRunner.RunPair(src, exponent, result, s_Pow);

    public void Sqrt(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sqrt);
    public void Ceiling(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Ceiling);
    public void Floor(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Floor);
    public void Round(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Round);
    public void Exp(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Exp);
    public void Log(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Log);
    public void Truncate(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Truncate);
    public void Sin(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sin);
    public void Cos(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Cos);
    public void Tan(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Tan);
    public void Sinh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Sinh);
    public void Cosh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Cosh);
    public void Tanh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Tanh);
    public void Asin(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Asin);
    public void Acos(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Acos);
    public void Atan(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Atan);
    public void Asinh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Asinh);
    public void Acosh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Acosh);
    public void Atanh(Span<double> src, Span<double> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Atanh);
}

internal sealed class ParallelSimdVectorOperation : ISpanVectorOperations<Vector2>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVectorOperation                                              s_Simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           s_Dot                 = s_Simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           s_Cross               = s_Simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector2>                          s_Normalize           = s_Simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector2, float>            s_Length              = s_Simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector2, float>            s_LengthSquared       = s_Simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             s_Distance            = s_Simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             s_DistanceSquared     = s_Simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector2, float>             s_Angle               = s_Simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           s_Reflect             = s_Simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector2>                           s_FaceForward         = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector2>                 s_Refract             = s_Simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector2, float, Vector2> s_FaceForwardIncident = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector2, float, Vector2> s_MoveTowards         = s_Simd.MoveTowards;

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Dot);
    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Cross);
    public void Normalize(Span<Vector2> src, Span<Vector2> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Normalize);
    public void Length(Span<Vector2> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_Length);
    public void LengthSquared(Span<Vector2> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_LengthSquared);
    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Distance);
    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_DistanceSquared);
    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Angle);
    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_Reflect);
    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_FaceForward);
    public void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, s_Refract);
    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, s_FaceForwardIncident);
    public void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, s_MoveTowards);
}

internal sealed class ParallelSimdVector3Operation : ISpanVectorOperations<Vector3>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVector3Operation                                             s_Simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           s_Dot                 = s_Simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           s_Cross               = s_Simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector3>                          s_Normalize           = s_Simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector3, float>            s_Length              = s_Simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector3, float>            s_LengthSquared       = s_Simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             s_Distance            = s_Simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             s_DistanceSquared     = s_Simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector3, float>             s_Angle               = s_Simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           s_Reflect             = s_Simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector3>                           s_FaceForward         = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector3>                 s_Refract             = s_Simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector3, float, Vector3> s_FaceForwardIncident = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector3, float, Vector3> s_MoveTowards         = s_Simd.MoveTowards;

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Dot);
    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Cross);
    public void Normalize(Span<Vector3> src, Span<Vector3> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Normalize);
    public void Length(Span<Vector3> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_Length);
    public void LengthSquared(Span<Vector3> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_LengthSquared);
    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Distance);
    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_DistanceSquared);
    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Angle);
    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_Reflect);
    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_FaceForward);
    public void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, s_Refract);
    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, s_FaceForwardIncident);
    public void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, s_MoveTowards);
}

internal sealed class ParallelSimdVector4Operation : ISpanVectorOperations<Vector4>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVector4Operation                                             s_Simd                = new();
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           s_Dot                 = s_Simd.Dot;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           s_Cross               = s_Simd.Cross;
    private static readonly ParallelSimdSpanRunner.UnaryOp<Vector4>                          s_Normalize           = s_Simd.Normalize;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector4, float>            s_Length              = s_Simd.Length;
    private static readonly ParallelSimdSpanRunner.UnaryProjectOp<Vector4, float>            s_LengthSquared       = s_Simd.LengthSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             s_Distance            = s_Simd.Distance;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             s_DistanceSquared     = s_Simd.DistanceSquared;
    private static readonly ParallelSimdSpanRunner.PairProjectOp<Vector4, float>             s_Angle               = s_Simd.Angle;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           s_Reflect             = s_Simd.Reflect;
    private static readonly ParallelSimdSpanRunner.PairOp<Vector4>                           s_FaceForward         = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.SpanSpanScalarOp<Vector4>                 s_Refract             = s_Simd.Refract;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector4, float, Vector4> s_FaceForwardIncident = s_Simd.FaceForward;
    private static readonly ParallelSimdSpanRunner.TernaryProjectOp<Vector4, float, Vector4> s_MoveTowards         = s_Simd.MoveTowards;

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Dot);
    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(left, right, result, s_Cross);
    public void Normalize(Span<Vector4> src, Span<Vector4> result) => ParallelSimdSpanRunner.RunUnary(src, result, s_Normalize);
    public void Length(Span<Vector4> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_Length);
    public void LengthSquared(Span<Vector4> src, Span<float> result) => ParallelSimdSpanRunner.RunUnaryProject(src, result, s_LengthSquared);
    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Distance);
    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_DistanceSquared);
    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result) => ParallelSimdSpanRunner.RunPairProject(left, right, result, s_Angle);
    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_Reflect);
    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => ParallelSimdSpanRunner.RunPair(src, normal, result, s_FaceForward);
    public void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result) => ParallelSimdSpanRunner.RunSpanSpanScalar(src, normal, eta, result, s_Refract);
    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result) => ParallelSimdSpanRunner.RunTernaryProject(src, normal, incident, result, s_FaceForwardIncident);
    public void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result) => ParallelSimdSpanRunner.RunTernaryProject(src, target, maxDistanceDelta, result, s_MoveTowards);
}