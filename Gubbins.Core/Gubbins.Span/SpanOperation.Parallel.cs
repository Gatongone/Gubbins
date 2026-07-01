using System.Buffers;
using System.Numerics;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

namespace Gubbins.Span;

internal static class ParallelExecutor
{
    private sealed class Worker<TState>
    {
        private static readonly ConcurrentQueue<Worker<TState>> s_Pool = new();

        private TState               m_State = default!;
        private Action<int, TState>? m_Body;

        public readonly Action<int> ExecuteAction;

        private Worker() => ExecuteAction = Execute;

        public static Worker<TState> Rent(TState state, Action<int, TState> body)
        {
            if (!s_Pool.TryDequeue(out var worker))
            {
                worker = new Worker<TState>();
            }

            worker.m_State = state;
            worker.m_Body  = body;
            return worker;
        }

        public static void Return(Worker<TState> worker)
        {
            worker.m_State = default!;
            worker.m_Body  = null;
            s_Pool.Enqueue(worker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Execute(int i) => m_Body!(i, m_State);
    }

    public static void For<TState>(int length, TState state, Action<int, TState> body)
    {
        if (length <= 0)
        {
            return;
        }

        var worker = Worker<TState>.Rent(state, body);
        try
        {
            Parallel.For(0, length, worker.ExecuteAction);
        }
        finally
        {
            Worker<TState>.Return(worker);
        }
    }
}

internal static class ParallelSpanRunner
{
    public delegate TResult UnaryOp<in TIn, out TResult>(TIn value);

    public delegate TResult BinaryOp<in TLeft, in TRight, out TResult>(TLeft left, TRight right);

    public delegate TResult BinaryScalarOp<in TLeft, in TRight, in TScalar, out TResult>(TLeft left, TRight right, TScalar scalar);

    public delegate TResult TernaryOp<in T1, in T2, in T3, out TResult>(T1 a, T2 b, T3 c);

    private unsafe struct UnaryState<T> where T : unmanaged
    {
        public T*            Src;
        public T*            Dst;
        public UnaryOp<T, T> Op;
    }

    private unsafe struct ProjectState<TIn, TOut>
        where TIn : unmanaged
        where TOut : unmanaged
    {
        public TIn*               Src;
        public TOut*              Dst;
        public UnaryOp<TIn, TOut> Op;
    }

    private unsafe struct InPlaceState<T> where T : unmanaged
    {
        public T*            Data;
        public UnaryOp<T, T> Op;
    }

    private unsafe struct BinaryState<TLeft, TRight, TResult>
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged
    {
        public TLeft*                           Left;
        public TRight*                          Right;
        public TResult*                         Dst;
        public BinaryOp<TLeft, TRight, TResult> Op;
    }

    private unsafe struct TernaryState<T1, T2, T3, TResult>
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where TResult : unmanaged
    {
        public T1*                            A;
        public T2*                            B;
        public T3*                            C;
        public TResult*                       Dst;
        public TernaryOp<T1, T2, T3, TResult> Op;
    }

    private unsafe struct BinaryScalarState<TLeft, TRight, TScalar, TResult>
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged
    {
        public TLeft*                                          Left;
        public TRight*                                         Right;
        public TScalar                                         Scalar;
        public TResult*                                        Dst;
        public BinaryScalarOp<TLeft, TRight, TScalar, TResult> Op;
    }

    private static unsafe void ExecuteUnary<T>(int i, UnaryState<T> state) where T : unmanaged =>
        state.Dst[i] = state.Op(state.Src[i]);

    private static unsafe void ExecuteProject<TIn, TOut>(int i, ProjectState<TIn, TOut> state)
        where TIn : unmanaged
        where TOut : unmanaged =>
        state.Dst[i] = state.Op(state.Src[i]);

    private static unsafe void ExecuteInPlace<T>(int i, InPlaceState<T> state) where T : unmanaged =>
        state.Data[i] = state.Op(state.Data[i]);

    private static unsafe void ExecuteBinary<TLeft, TRight, TResult>(int i, BinaryState<TLeft, TRight, TResult> state)
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged =>
        state.Dst[i] = state.Op(state.Left[i], state.Right[i]);

    private static unsafe void ExecuteTernary<T1, T2, T3, TResult>(int i, TernaryState<T1, T2, T3, TResult> state)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where TResult : unmanaged =>
        state.Dst[i] = state.Op(state.A[i], state.B[i], state.C[i]);

    private static unsafe void ExecuteBinaryScalar<TLeft, TRight, TScalar, TResult>(int i, BinaryScalarState<TLeft, TRight, TScalar, TResult> state)
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged =>
        state.Dst[i] = state.Op(state.Left[i], state.Right[i], state.Scalar);

    public static unsafe void RunUnary<T>(Span<T> src, Span<T> result, UnaryOp<T, T> op) where T : unmanaged
    {
        fixed (T* ps = src)
        fixed (T* pd = result)
        {
            var state = new UnaryState<T> {Src = ps, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteUnary);
        }
    }

    public static unsafe void RunProject<TIn, TOut>(Span<TIn> src, Span<TOut> result, UnaryOp<TIn, TOut> op)
        where TIn : unmanaged
        where TOut : unmanaged
    {
        fixed (TIn* ps = src)
        fixed (TOut* pd = result)
        {
            var state = new ProjectState<TIn, TOut> {Src = ps, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteProject);
        }
    }

    public static unsafe void RunInPlace<T>(Span<T> span, UnaryOp<T, T> op) where T : unmanaged
    {
        fixed (T* p = span)
        {
            var state = new InPlaceState<T> {Data = p, Op = op};
            ParallelExecutor.For(span.Length, state, ExecuteInPlace);
        }
    }

    public static unsafe void RunBinary<TLeft, TRight, TResult>(Span<TLeft> left, Span<TRight> right, Span<TResult> result, BinaryOp<TLeft, TRight, TResult> op)
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged
    {
        fixed (TLeft* pl = left)
        fixed (TRight* pr = right)
        fixed (TResult* pd = result)
        {
            var state = new BinaryState<TLeft, TRight, TResult> {Left = pl, Right = pr, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteBinary);
        }
    }

    public static unsafe void RunTernary<T1, T2, T3, TResult>(Span<T1> first, Span<T2> second, Span<T3> third, Span<TResult> result, TernaryOp<T1, T2, T3, TResult> op)
        where T1 : unmanaged
        where T2 : unmanaged
        where T3 : unmanaged
        where TResult : unmanaged
    {
        fixed (T1* p1 = first)
        fixed (T2* p2 = second)
        fixed (T3* p3 = third)
        fixed (TResult* pd = result)
        {
            var state = new TernaryState<T1, T2, T3, TResult> {A = p1, B = p2, C = p3, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteTernary);
        }
    }

    public static unsafe void RunBinaryScalar<TLeft, TRight, TScalar, TResult>(
        Span<TLeft> left,
        Span<TRight> right,
        TScalar scalar,
        Span<TResult> result,
        BinaryScalarOp<TLeft, TRight, TScalar, TResult> op)
        where TLeft : unmanaged
        where TRight : unmanaged
        where TResult : unmanaged
    {
        fixed (TLeft* pl = left)
        fixed (TRight* pr = right)
        fixed (TResult* pd = result)
        {
            var state = new BinaryScalarState<TLeft, TRight, TScalar, TResult>
            {
                Left   = pl,
                Right  = pr,
                Scalar = scalar,
                Dst    = pd,
                Op     = op,
            };
            ParallelExecutor.For(result.Length, state, ExecuteBinaryScalar);
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelNumberOperation<T> : ISpanNumberOperation<T> where T : unmanaged
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;
    public bool Supported => s_Supported;

    private delegate T OperandOp(T value, T operand);

    private delegate T PairOp(T left, T right);

    private unsafe struct OperandState
    {
        public T*        From;
        public T         Operand;
        public T*        To;
        public OperandOp Op;
    }

    private unsafe struct PairState
    {
        public T*     Left;
        public T*     Right;
        public T*     To;
        public PairOp Op;
    }

    private unsafe struct ReduceState
    {
        public T*     Src;
        public int    Length;
        public int    PartitionCount;
        public T*     Partials;
        public PairOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteOperand(int i, OperandState state) =>
        state.To[i] = state.Op(state.From[i], state.Operand);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecutePair(int i, PairState state) =>
        state.To[i] = state.Op(state.Left[i], state.Right[i]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteReduce(int i, ReduceState state)
    {
        var start = state.Length * i / state.PartitionCount;
        var end = state.Length * (i + 1) / state.PartitionCount;
        var value = state.Src[start];
        for (var index = start + 1; index < end; index++)
        {
            value = state.Op(value, state.Src[index]);
        }

        state.Partials[i] = value;
    }

    private static unsafe void RunOperand(Span<T> src, T operand, Span<T> result, OperandOp op)
    {
        fixed (T* ptr = src)
        fixed (T* dest = result)
        {
            var state = new OperandState {From = ptr, Operand = operand, To = dest, Op = op};
            ParallelExecutor.For(src.Length, state, ExecuteOperand);
        }
    }

    private static unsafe void RunPair(Span<T> left, Span<T> right, Span<T> result, PairOp op)
    {
        fixed (T* lsrc = left)
        fixed (T* rsrc = right)
        fixed (T* dest = result)
        {
            var state = new PairState {Left = lsrc, Right = rsrc, To = dest, Op = op};
            ParallelExecutor.For(result.Length, state, ExecutePair);
        }
    }

    private static unsafe T RunReduce(Span<T> src, PairOp op)
    {
        if (src.Length == 0)
        {
            throw new ArgumentException("Span must not be empty", nameof(src));
        }

        var partitionCount = Math.Min(src.Length, Math.Max(1, Environment.ProcessorCount));
        var partials = ArrayPool<T>.Shared.Rent(partitionCount);

        fixed (T* ps = src)
        fixed (T* pp = partials)
        {
            var state = new ReduceState
            {
                Src            = ps,
                Length         = src.Length,
                PartitionCount = partitionCount,
                Partials       = pp,
                Op             = op
            };

            ParallelExecutor.For(partitionCount, state, ExecuteReduce);
        }

        var result = partials[0];
        for (var index = 1; index < partitionCount; index++)
        {
            result = op(result, partials[index]);
        }

        return result;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(Span<T> src, T operand, Span<T> result) => RunOperand(src, operand, result, Operations<T>.Add);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Subtract(Span<T> src, T operand, Span<T> result) => RunOperand(src, operand, result, Operations<T>.Subtract);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Multiply(Span<T> src, T operand, Span<T> result) => RunOperand(src, operand, result, Operations<T>.Multiply);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Divide(Span<T> src, T operand, Span<T> result) => RunOperand(src, operand, result, Operations<T>.Divide);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Modulo(Span<T> src, T operand, Span<T> result) => RunOperand(src, operand, result, Operations<T>.Modulo);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Max(Span<T> left, Span<T> right, Span<T> result) => RunPair(left, right, result, static (l, r) => Operations<T>.GreaterThan(l, r) ? l : r);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMax(Span<T> src) => RunReduce(src, static (left, right) => Operations<T>.GreaterThan(left, right) ? left : right);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Min(Span<T> left, Span<T> right, Span<T> result) => RunPair(left, right, result, static (l, r) => Operations<T>.LessThan(l, r) ? l : r);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetMin(Span<T> src) => RunReduce(src, static (left, right) => Operations<T>.LessThan(left, right) ? left : right);
}

internal sealed class ParallelIntOperation : ISpanShift<int>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate int ShiftOp(int value, int count);

    private unsafe struct ShiftState
    {
        public int*    From;
        public int     Count;
        public int*    To;
        public ShiftOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteShift(int i, ShiftState state) =>
        state.To[i] = state.Op(state.From[i], state.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ShiftLeftScalar(int value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ShiftRightScalar(int value, int count) => value >> count;

    private static unsafe void RunShift(Span<int> src, int count, Span<int> result, ShiftOp op)
    {
        fixed (int* ptr = src)
        fixed (int* dest = result)
        {
            var state = new ShiftState {From = ptr, Count = count, To = dest, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteShift);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<int> src, int count, Span<int> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<int> src, int count, Span<int> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelLongOperation : ISpanShift<long>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate long ShiftOp(long value, int count);

    private unsafe struct ShiftState
    {
        public long*   From;
        public int     Count;
        public long*   To;
        public ShiftOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteShift(int i, ShiftState state) =>
        state.To[i] = state.Op(state.From[i], state.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ShiftLeftScalar(long value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ShiftRightScalar(long value, int count) => value >> count;

    private static unsafe void RunShift(Span<long> src, int count, Span<long> result, ShiftOp op)
    {
        fixed (long* ptr = src)
        fixed (long* dest = result)
        {
            var state = new ShiftState {From = ptr, Count = count, To = dest, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteShift);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<long> src, int count, Span<long> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<long> src, int count, Span<long> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelUintOperation : ISpanShift<uint>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate uint ShiftOp(uint value, int count);

    private unsafe struct ShiftState
    {
        public uint*   From;
        public int     Count;
        public uint*   To;
        public ShiftOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteShift(int i, ShiftState state) =>
        state.To[i] = state.Op(state.From[i], state.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ShiftLeftScalar(uint value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ShiftRightScalar(uint value, int count) => value >> count;

    private static unsafe void RunShift(Span<uint> src, int count, Span<uint> result, ShiftOp op)
    {
        fixed (uint* ptr = src)
        fixed (uint* dest = result)
        {
            var state = new ShiftState {From = ptr, Count = count, To = dest, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteShift);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<uint> src, int count, Span<uint> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelUlongOperation : ISpanShift<ulong>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate ulong ShiftOp(ulong value, int count);

    private unsafe struct ShiftState
    {
        public ulong*  From;
        public int     Count;
        public ulong*  To;
        public ShiftOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteShift(int i, ShiftState state) =>
        state.To[i] = state.Op(state.From[i], state.Count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftLeftScalar(ulong value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftRightScalar(ulong value, int count) => value >> count;

    private static unsafe void RunShift(Span<ulong> src, int count, Span<ulong> result, ShiftOp op)
    {
        fixed (ulong* ptr = src)
        fixed (ulong* dest = result)
        {
            var state = new ShiftState {From = ptr, Count = count, To = dest, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteShift);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelFloatOperation : ISpanRealOperation<float>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate float UnaryOp(float value);

    private delegate float BinaryOp(float left, float right);

    private delegate float TernaryOp(float a, float b, float c);

    private unsafe struct UnaryState
    {
        public float*  Src;
        public float*  Dst;
        public UnaryOp Op;
    }

    private unsafe struct BinaryState
    {
        public float*   Left;
        public float*   Right;
        public float*   Dst;
        public BinaryOp Op;
    }

    private unsafe struct BinaryScalarState
    {
        public float*   Src;
        public float    Scalar;
        public float*   Dst;
        public BinaryOp Op;
    }

    private unsafe struct TernaryState
    {
        public float*    A;
        public float*    B;
        public float*    C;
        public float*    Dst;
        public TernaryOp Op;
    }

    private unsafe struct TernaryMiddleScalarState
    {
        public float*    A;
        public float     B;
        public float*    C;
        public float*    Dst;
        public TernaryOp Op;
    }

    private unsafe struct UnaryTwoScalarsState
    {
        public float*    Src;
        public float     A;
        public float     B;
        public float*    Dst;
        public TernaryOp Op;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteUnary(int i, UnaryState state) => state.Dst[i] = state.Op(state.Src[i]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteBinary(int i, BinaryState state) => state.Dst[i] = state.Op(state.Left[i], state.Right[i]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteBinaryScalar(int i, BinaryScalarState state) => state.Dst[i] = state.Op(state.Src[i], state.Scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteTernary(int i, TernaryState state) => state.Dst[i] = state.Op(state.A[i], state.B[i], state.C[i]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteTernaryMiddleScalar(int i, TernaryMiddleScalarState state) => state.Dst[i] = state.Op(state.A[i], state.B, state.C[i]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void ExecuteUnaryTwoScalars(int i, UnaryTwoScalarsState state) => state.Dst[i] = state.Op(state.Src[i], state.A, state.B);

    private static unsafe void RunUnary(Span<float> src, Span<float> result, UnaryOp op)
    {
        fixed (float* ps = src)
        fixed (float* pd = result)
        {
            var state = new UnaryState {Src = ps, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteUnary);
        }
    }

    private static unsafe void RunBinary(Span<float> left, Span<float> right, Span<float> result, BinaryOp op)
    {
        fixed (float* pl = left)
        fixed (float* pr = right)
        fixed (float* pd = result)
        {
            var state = new BinaryState {Left = pl, Right = pr, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteBinary);
        }
    }

    private static unsafe void RunBinaryScalar(Span<float> src, float scalar, Span<float> result, BinaryOp op)
    {
        fixed (float* ps = src)
        fixed (float* pd = result)
        {
            var state = new BinaryScalarState {Src = ps, Scalar = scalar, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteBinaryScalar);
        }
    }

    private static unsafe void RunTernary(Span<float> a, Span<float> b, Span<float> c, Span<float> result, TernaryOp op)
    {
        fixed (float* pa = a)
        fixed (float* pb = b)
        fixed (float* pc = c)
        fixed (float* pd = result)
        {
            var state = new TernaryState {A = pa, B = pb, C = pc, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteTernary);
        }
    }

    private static unsafe void RunTernaryMiddleScalar(Span<float> a, float b, Span<float> c, Span<float> result, TernaryOp op)
    {
        fixed (float* pa = a)
        fixed (float* pc = c)
        fixed (float* pd = result)
        {
            var state = new TernaryMiddleScalarState {A = pa, B = b, C = pc, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteTernaryMiddleScalar);
        }
    }

    private static unsafe void RunUnaryTwoScalars(Span<float> src, float a, float b, Span<float> result, TernaryOp op)
    {
        fixed (float* ps = src)
        fixed (float* pd = result)
        {
            var state = new UnaryTwoScalarsState {Src = ps, A = a, B = b, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteUnaryTwoScalars);
        }
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Floor(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Floor);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Ceiling(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Ceiling);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sqrt(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sqrt);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Round(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Round);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exp(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Exp);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Log(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Log);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Truncate(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Truncate);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result) =>
        RunTernary(src, min, max, result, static (s, lo, hi) => MathF.Min(MathF.Max(s, lo), hi));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clamp(Span<float> src, float min, float max, Span<float> result) =>
        RunUnaryTwoScalars(src, min, max, result, static (x, lo, hi) => MathF.Min(MathF.Max(x, lo), hi));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result) =>
        RunTernary(x, y, amount, result, static (vx, vy, a) => vx * (1 - a) + vy * a);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result) =>
        RunTernaryMiddleScalar(x, y, amount, result, static (vx, vy, a) => vx * (1 - a) + vy * a);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Hypot(Span<float> x, Span<float> y, Span<float> result) =>
        RunBinary(x, y, result, static (vx, vy) => MathF.Sqrt(vx * vx + vy * vy));

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Hypot(Span<float> x, float y, Span<float> result)
    {
        var yy = y * y;
        RunBinaryScalar(x, yy, result, static (vx, yyValue) => MathF.Sqrt(vx * vx + yyValue));
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pow(Span<float> src, float exponent, Span<float> result) =>
        RunBinaryScalar(src, exponent, result, MathF.Pow);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Pow(Span<float> src, Span<float> exponent, Span<float> result) =>
        RunBinary(src, exponent, result, MathF.Pow);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sin(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sin);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Cos(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Cos);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Tan(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Tan);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Sinh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sinh);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Cosh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Cosh);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Tanh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Tanh);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Asin(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Asin);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Acos(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Acos);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Atan(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Atan);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Asinh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Asinh);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Acosh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Acosh);

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Atanh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Atanh);
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelDoubleOperation : ISpanRealOperation<double>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    private delegate double UnaryOp(double value);

    private delegate double BinaryOp(double left, double right);

    private delegate double TernaryOp(double a, double b, double c);

    private unsafe struct UnaryState
    {
        public double* Src;
        public double* Dst;
        public UnaryOp Op;
    }

    private unsafe struct BinaryState
    {
        public double*  Left;
        public double*  Right;
        public double*  Dst;
        public BinaryOp Op;
    }

    private unsafe struct BinaryScalarState
    {
        public double*  Src;
        public double   Scalar;
        public double*  Dst;
        public BinaryOp Op;
    }

    private unsafe struct TernaryState
    {
        public double*   A;
        public double*   B;
        public double*   C;
        public double*   Dst;
        public TernaryOp Op;
    }

    private unsafe struct TernaryMiddleScalarState
    {
        public double*   A;
        public double    B;
        public double*   C;
        public double*   Dst;
        public TernaryOp Op;
    }

    private unsafe struct UnaryTwoScalarsState
    {
        public double*   Src;
        public double    A;
        public double    B;
        public double*   Dst;
        public TernaryOp Op;
    }

    private static unsafe void ExecuteUnary(int i, UnaryState state) => state.Dst[i] = state.Op(state.Src[i]);
    private static unsafe void ExecuteBinary(int i, BinaryState state) => state.Dst[i] = state.Op(state.Left[i], state.Right[i]);
    private static unsafe void ExecuteBinaryScalar(int i, BinaryScalarState state) => state.Dst[i] = state.Op(state.Src[i], state.Scalar);
    private static unsafe void ExecuteTernary(int i, TernaryState state) => state.Dst[i] = state.Op(state.A[i], state.B[i], state.C[i]);
    private static unsafe void ExecuteTernaryMiddleScalar(int i, TernaryMiddleScalarState state) => state.Dst[i] = state.Op(state.A[i], state.B, state.C[i]);
    private static unsafe void ExecuteUnaryTwoScalars(int i, UnaryTwoScalarsState state) => state.Dst[i] = state.Op(state.Src[i], state.A, state.B);

    private static unsafe void RunUnary(Span<double> src, Span<double> result, UnaryOp op)
    {
        fixed (double* ps = src)
        fixed (double* pd = result)
        {
            var state = new UnaryState {Src = ps, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteUnary);
        }
    }

    private static unsafe void RunBinary(Span<double> left, Span<double> right, Span<double> result, BinaryOp op)
    {
        fixed (double* pl = left)
        fixed (double* pr = right)
        fixed (double* pd = result)
        {
            var state = new BinaryState {Left = pl, Right = pr, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteBinary);
        }
    }

    private static unsafe void RunBinaryScalar(Span<double> src, double scalar, Span<double> result, BinaryOp op)
    {
        fixed (double* ps = src)
        fixed (double* pd = result)
        {
            var state = new BinaryScalarState {Src = ps, Scalar = scalar, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteBinaryScalar);
        }
    }

    private static unsafe void RunTernary(Span<double> a, Span<double> b, Span<double> c, Span<double> result, TernaryOp op)
    {
        fixed (double* pa = a)
        fixed (double* pb = b)
        fixed (double* pc = c)
        fixed (double* pd = result)
        {
            var state = new TernaryState {A = pa, B = pb, C = pc, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteTernary);
        }
    }

    private static unsafe void RunTernaryMiddleScalar(Span<double> a, double b, Span<double> c, Span<double> result, TernaryOp op)
    {
        fixed (double* pa = a)
        fixed (double* pc = c)
        fixed (double* pd = result)
        {
            var state = new TernaryMiddleScalarState {A = pa, B = b, C = pc, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteTernaryMiddleScalar);
        }
    }

    private static unsafe void RunUnaryTwoScalars(Span<double> src, double a, double b, Span<double> result, TernaryOp op)
    {
        fixed (double* ps = src)
        fixed (double* pd = result)
        {
            var state = new UnaryTwoScalarsState {Src = ps, A = a, B = b, Dst = pd, Op = op};
            ParallelExecutor.For(result.Length, state, ExecuteUnaryTwoScalars);
        }
    }

    /// <inheritdoc />
    public void Floor(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Floor);

    /// <inheritdoc />
    public void Ceiling(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Ceiling);

    /// <inheritdoc />
    public void Sqrt(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sqrt);

    /// <inheritdoc />
    public void Round(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Round);

    /// <inheritdoc />
    public void Exp(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Exp);

    /// <inheritdoc />
    public void Log(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Log);

    /// <inheritdoc />
    public void Truncate(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Truncate);

    /// <inheritdoc />
    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result) =>
        RunTernary(src, min, max, result, static (s, lo, hi) => Math.Min(Math.Max(s, lo), hi));

    /// <inheritdoc />
    public void Clamp(Span<double> src, double min, double max, Span<double> result) =>
        RunUnaryTwoScalars(src, min, max, result, static (x, lo, hi) => Math.Min(Math.Max(x, lo), hi));

    /// <inheritdoc />
    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result) =>
        RunTernary(x, y, amount, result, static (vx, vy, a) => vx * (1 - a) + vy * a);

    /// <inheritdoc />
    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result) =>
        RunTernaryMiddleScalar(x, y, amount, result, static (vx, vy, a) => vx * (1 - a) + vy * a);

    /// <inheritdoc />
    public void Hypot(Span<double> x, Span<double> y, Span<double> result) =>
        RunBinary(x, y, result, static (vx, vy) => Math.Sqrt(vx * vx + vy * vy));

    /// <inheritdoc />
    public void Hypot(Span<double> x, double y, Span<double> result)
    {
        var yy = y * y;
        RunBinaryScalar(x, yy, result, static (vx, yyValue) => Math.Sqrt(vx * vx + yyValue));
    }

    /// <inheritdoc />
    public void Pow(Span<double> src, double exponent, Span<double> result) =>
        RunBinaryScalar(src, exponent, result, Math.Pow);

    /// <inheritdoc />
    public void Pow(Span<double> src, Span<double> exponent, Span<double> result) =>
        RunBinary(src, exponent, result, Math.Pow);

    /// <inheritdoc />
    public void Sin(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sin);

    /// <inheritdoc />
    public void Cos(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Cos);

    /// <inheritdoc />
    public void Tan(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Tan);

    /// <inheritdoc />
    public void Sinh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sinh);

    /// <inheritdoc />
    public void Cosh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Cosh);

    /// <inheritdoc />
    public void Tanh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Tanh);

    /// <inheritdoc />
    public void Asin(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Asin);

    /// <inheritdoc />
    public void Acos(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Acos);

    /// <inheritdoc />
    public void Atan(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Atan);

    /// <inheritdoc />
    public void Asinh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Asinh);

    /// <inheritdoc />
    public void Acosh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Acosh);

    /// <inheritdoc />
    public void Atanh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Atanh);
}

internal sealed class ParallelVector2Operation : ISpanVectorOperation<Vector2>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var dot = Vector2.Dot(l, r);
            return new Vector2(dot, dot);
        });

    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var cross = l.X * r.Y - l.Y * r.X;
            return new Vector2(cross, cross);
        });

    public void Normalize(Span<Vector2> src, Span<Vector2> result) =>
        ParallelSpanRunner.RunUnary(src, result, Vector2.Normalize);

    public void Length(Span<Vector2> src, Span<float> result)
    {
        LengthSquared(src, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void LengthSquared(Span<Vector2> src, Span<float> result) =>
        ParallelSpanRunner.RunProject(src, result, static v => v.LengthSquared());

    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, Vector2.DistanceSquared);

    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var denominator = l.Length() * r.Length();
            return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector2.Dot(l, r) / denominator, -1f, 1f));
        });

    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, Vector2.Reflect);

    public void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result)
    {
        var etaRatio = eta.X;
        ParallelSpanRunner.RunBinaryScalar(src, normal, etaRatio, result, RefractScalarWithEta);
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result) =>
        ParallelSpanRunner.RunBinary(src, incident, result, static (s, inc) => inc < 0f ? s : -s);

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, static (s, n) => Vector2.Dot(n, s) < 0f ? s : -s);

    public void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result) =>
        ParallelSpanRunner.RunTernary(src, target, maxDistanceDelta, result, MoveTowardsScalar);

    private static Vector2 RefractScalar(Vector2 incident, Vector2 normal, float eta)
    {
        var dot = Vector2.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector2.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static Vector2 RefractScalarWithEta(Vector2 incident, Vector2 normal, float eta) =>
        RefractScalar(incident, normal, eta);

    private static Vector2 MoveTowardsScalar(Vector2 current, Vector2 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f)
        {
            return target;
        }

        return current + toTarget / distance * maxDistanceDelta;
    }
}

internal sealed class ParallelVector3Operation : ISpanVectorOperation<Vector3>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var dot = Vector3.Dot(l, r);
            return new Vector3(dot, dot, dot);
        });

    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, Vector3.Cross);

    public void Normalize(Span<Vector3> src, Span<Vector3> result) =>
        ParallelSpanRunner.RunUnary(src, result, Vector3.Normalize);

    public void Length(Span<Vector3> src, Span<float> result)
    {
        LengthSquared(src, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void LengthSquared(Span<Vector3> src, Span<float> result) =>
        ParallelSpanRunner.RunProject(src, result, static v => v.LengthSquared());

    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, Vector3.DistanceSquared);

    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var denominator = l.Length() * r.Length();
            return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(l, r) / denominator, -1f, 1f));
        });

    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, Vector3.Reflect);

    public void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result)
    {
        var etaRatio = eta.X;
        ParallelSpanRunner.RunBinaryScalar(src, normal, etaRatio, result, RefractScalarWithEta);
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result) =>
        ParallelSpanRunner.RunBinary(src, incident, result, static (s, inc) => inc < 0f ? s : -s);

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, static (s, n) => Vector3.Dot(n, s) < 0f ? s : -s);

    public void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result) =>
        ParallelSpanRunner.RunTernary(src, target, maxDistanceDelta, result, MoveTowardsScalar);

    private static Vector3 RefractScalar(Vector3 incident, Vector3 normal, float eta)
    {
        var dot = Vector3.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector3.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static Vector3 RefractScalarWithEta(Vector3 incident, Vector3 normal, float eta) =>
        RefractScalar(incident, normal, eta);

    private static Vector3 MoveTowardsScalar(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f)
        {
            return target;
        }

        return current + toTarget / distance * maxDistanceDelta;
    }
}

internal sealed class ParallelVector4Operation : ISpanVectorOperation<Vector4>
{
    private static readonly bool s_Supported = Environment.ProcessorCount > 1;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var dot = Vector4.Dot(l, r);
            return new Vector4(dot, dot, dot, dot);
        });

    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, CrossScalar);

    public void Normalize(Span<Vector4> src, Span<Vector4> result) =>
        ParallelSpanRunner.RunUnary(src, result, Vector4.Normalize);

    public void Length(Span<Vector4> src, Span<float> result)
    {
        LengthSquared(src, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void LengthSquared(Span<Vector4> src, Span<float> result) =>
        ParallelSpanRunner.RunProject(src, result, static v => v.LengthSquared());

    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        ParallelSpanRunner.RunInPlace(result, MathF.Sqrt);
    }

    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, Vector4.DistanceSquared);

    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result) =>
        ParallelSpanRunner.RunBinary(left, right, result, static (l, r) =>
        {
            var denominator = l.Length() * r.Length();
            return denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(l, r) / denominator, -1f, 1f));
        });

    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, ReflectScalar);

    public void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result)
    {
        var etaRatio = eta.X;
        ParallelSpanRunner.RunBinaryScalar(src, normal, etaRatio, result, RefractScalarWithEta);
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result) =>
        ParallelSpanRunner.RunBinary(src, incident, result, static (s, inc) => inc < 0f ? s : -s);

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) =>
        ParallelSpanRunner.RunBinary(src, normal, result, static (s, n) => Vector4.Dot(n, s) < 0f ? s : -s);

    public void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result) =>
        ParallelSpanRunner.RunTernary(src, target, maxDistanceDelta, result, MoveTowardsScalar);

    private static Vector4 CrossScalar(Vector4 left, Vector4 right)
    {
        return new Vector4(
            left.Y * right.Z - left.Z * right.Y,
            left.Z * right.X - left.X * right.Z,
            left.X * right.Y - left.Y * right.X,
            0f);
    }

    private static Vector4 ReflectScalar(Vector4 src, Vector4 normal)
    {
        var dot = Vector4.Dot(src, normal);
        return src - normal * (2f * dot);
    }

    private static Vector4 RefractScalar(Vector4 incident, Vector4 normal, float eta)
    {
        var dot = Vector4.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector4.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

    private static Vector4 RefractScalarWithEta(Vector4 incident, Vector4 normal, float eta) =>
        RefractScalar(incident, normal, eta);

    private static Vector4 MoveTowardsScalar(Vector4 current, Vector4 target, float maxDistanceDelta)
    {
        var toTarget = target - current;
        var distance = toTarget.Length();
        if (distance <= maxDistanceDelta || distance == 0f)
        {
            return target;
        }

        return current + toTarget / distance * maxDistanceDelta;
    }
}