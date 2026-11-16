using Gubbins.Span;

#if NET7_0_OR_GREATER
using System.Numerics;
#endif

namespace Gubbins.Entities;

/// <summary>
/// Applies a caller-supplied action to one segment slice produced by a parallel bulk dispatch.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <typeparam name="TState">The state passed to the action.</typeparam>
/// <param name="span">The slice of the current segment covered by this worker range.</param>
/// <param name="state">The state supplied to <c>ApplyParallel</c>.</param>
public delegate void SpanSegmentAction<T, in TState>(Span<T> span, TState state) where T : unmanaged;

/// <summary>
/// Applies a resolved span operation to one segment slice produced by a parallel bulk dispatch.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <typeparam name="TOperation">The span operation interface to resolve for the requested mask.</typeparam>
/// <typeparam name="TState">The state passed to the action.</typeparam>
/// <param name="operation">The operation implementation selected from <see cref="IterationMode"/>.</param>
/// <param name="span">The slice of the current segment covered by this worker range.</param>
/// <param name="state">The state supplied to the caller.</param>
public delegate void SpanOperationAction<T, TOperation, in TState>(TOperation operation, Span<T> span, TState state)
    where T : unmanaged
    where TOperation : class, ISpanOperation<T>;

/// <summary>
/// Parallel bulk operations over the components of a query result.
/// </summary>
public static class ComponentsParallelExtensions
{
    /// <summary>
    /// Applies <paramref name="action"/> to every element of <paramref name="components"/> using a single parallel
    /// dispatch across all segments. The action receives contiguous slices of the component spans, so any
    /// single-span operation (add, subtract, multiply, divide, modulo, shift, unary vector operations, …) can be
    /// used; it is not limited to a specific operation.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyParallel<T, TState>(
        this Components<T> components,
        TState state,
        SpanSegmentAction<T, TState> action,
        int workerCount = 0)
        where T : unmanaged
    {
        var segments = components.Segments;
        var total = components.Elements.Count;

        if (total == 0)
        {
            return;
        }

        var dispatch = new ApplyState<T, TState>(segments, action, state);
        if (workerCount <= 0)
        {
            ParallelRange.For(total, dispatch, static (start, length, value) => value.Apply(start, length));
            return;
        }

        ParallelRange.For(total, workerCount, dispatch, static (start, length, value) => value.Apply(start, length));
    }

    /// <summary>
    /// Applies the number operation selected by <paramref name="operation"/> and <paramref name="operand"/> to every
    /// element of <paramref name="components"/> using the strategy selected by <paramref name="mask"/>: with the
    /// <see cref="IterationMode.Parallel"/> bit the segments are dispatched across the worker pool, otherwise the
    /// whole query runs on the calling thread. The per-segment implementation is Serial or SIMD.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operation">The operation to apply.</param>
    /// <param name="operand">The value used by the operation.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyParallel<T>(
        this Components<T> components,
        IterationMode mask,
        NumberOperation operation,
        T operand,
        int workerCount = 0)
        where T : unmanaged
        => ApplyNumberStrategy(components, mask, operation, operand, workerCount);

    /// <summary>
    /// Resolves any span operation interface for <paramref name="mask"/> and applies <paramref name="action"/> to
    /// every contiguous segment slice using a single parallel dispatch across all segments.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TOperation">The span operation interface to resolve.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyParallel<T, TOperation, TState>(
        this Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, TOperation, TState> action,
        int workerCount = 0)
        where T : unmanaged
        where TOperation : class, ISpanOperation<T>
        => ApplyOperationParallel(components, mask, state, action, workerCount);

    /// <summary>
    /// Resolves a number operation and applies <paramref name="action"/> to every segment slice. Unlike the
    /// <see cref="NumberOperation"/> overload this also reaches operations that consume another span or aggregate,
    /// such as <see cref="ISpanMax{T}"/> and <see cref="ISpanMin{T}"/>.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyNumber<T, TState>(
        this Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, ISpanNumberOperation<T>, TState> action,
        int workerCount = 0)
        where T : unmanaged
        => ApplyOperationParallel(components, mask, state, action, workerCount);

    /// <summary>
    /// Resolves a real operation (<see cref="ISpanRealOperation{T}"/>: sqrt, clamp, lerp, trigonometry, …) and
    /// applies <paramref name="action"/> to every segment slice.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyReal<T, TState>(
        this Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, ISpanRealOperation<T>, TState> action,
        int workerCount = 0)
        where T : unmanaged
        => ApplyOperationParallel(components, mask, state, action, workerCount);

    /// <summary>
    /// Resolves a vector operation (<see cref="ISpanVectorOperation{T}"/>: dot, cross, normalize, length, …) and
    /// applies <paramref name="action"/> to every segment slice.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyVector<T, TState>(
        this Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, ISpanVectorOperation<T>, TState> action,
        int workerCount = 0)
        where T : unmanaged
        => ApplyOperationParallel(components, mask, state, action, workerCount);

    /// <summary>
    /// Resolves a shift operation (<see cref="ISpanShift{T}"/>: shift left / shift right) and applies
    /// <paramref name="action"/> to every segment slice.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="state">The state passed to the action.</param>
    /// <param name="action">The action applied to every slice.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void ApplyShift<T, TState>(
        this Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, ISpanShift<T>, TState> action,
        int workerCount = 0)
        where T : unmanaged
        => ApplyOperationParallel(components, mask, state, action, workerCount);

#if NET7_0_OR_GREATER
    /// <summary>
    /// Adds <paramref name="operand"/> to every numeric element of <paramref name="components"/> using the strategy
    /// selected by <paramref name="mask"/>: the <see cref="IterationMode.Parallel"/> bit enables the cross-segment
    /// dispatch, otherwise the whole query runs on the calling thread; the body is SIMD when the
    /// <see cref="IterationMode.Simd"/> bit is set and scalar otherwise.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void Add<T>(this Components<T> components, IterationMode mask, T operand, int workerCount = 0)
        where T : unmanaged, INumber<T>
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every numeric element of <paramref name="components"/> using the
    /// strategy selected by <paramref name="mask"/>.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void Subtract<T>(this Components<T> components, IterationMode mask, T operand, int workerCount = 0)
        where T : unmanaged, INumber<T>
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Multiplies every numeric element of <paramref name="components"/> by <paramref name="operand"/> using the
    /// strategy selected by <paramref name="mask"/>.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void Multiply<T>(this Components<T> components, IterationMode mask, T operand, int workerCount = 0)
        where T : unmanaged, INumber<T>
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Divides every numeric element of <paramref name="components"/> by <paramref name="operand"/> using the
    /// strategy selected by <paramref name="mask"/>.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void Divide<T>(this Components<T> components, IterationMode mask, T operand, int workerCount = 0)
        where T : unmanaged, INumber<T>
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Replaces every numeric element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/> using the strategy selected by <paramref name="mask"/>.
    /// </summary>
    /// <typeparam name="T">The numeric component type.</typeparam>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">
    /// The requested worker count, or <c>0</c> to derive it from the total element count.
    /// </param>
    public static void Modulo<T>(this Components<T> components, IterationMode mask, T operand, int workerCount = 0)
        where T : unmanaged, INumber<T>
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);
#else
    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="int"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<int> components, IterationMode mask, int operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="uint"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<uint> components, IterationMode mask, uint operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="long"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<long> components, IterationMode mask, long operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="ulong"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<ulong> components, IterationMode mask, ulong operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="float"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<float> components, IterationMode mask, float operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Adds <paramref name="operand"/> to every <see cref="double"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to add to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Add(this Components<double> components, IterationMode mask, double operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Add, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="int"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<int> components, IterationMode mask, int operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="uint"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<uint> components, IterationMode mask, uint operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="long"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<long> components, IterationMode mask, long operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="ulong"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<ulong> components, IterationMode mask, ulong operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="float"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<float> components, IterationMode mask, float operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Subtracts <paramref name="operand"/> from every <see cref="double"/> element of <paramref name="components"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to subtract from every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Subtract(this Components<double> components, IterationMode mask, double operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Subtract, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="int"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<int> components, IterationMode mask, int operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="uint"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<uint> components, IterationMode mask, uint operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="long"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<long> components, IterationMode mask, long operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="ulong"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<ulong> components, IterationMode mask, ulong operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="float"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<float> components, IterationMode mask, float operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Multiplies every <see cref="double"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to multiply every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Multiply(this Components<double> components, IterationMode mask, double operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Multiply, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="int"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<int> components, IterationMode mask, int operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="uint"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<uint> components, IterationMode mask, uint operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="long"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<long> components, IterationMode mask, long operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="ulong"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<ulong> components, IterationMode mask, ulong operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="float"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<float> components, IterationMode mask, float operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Divides every <see cref="double"/> element of <paramref name="components"/> by <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The value to divide every element by.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Divide(this Components<double> components, IterationMode mask, double operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Divide, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="int"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<int> components, IterationMode mask, int operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="uint"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<uint> components, IterationMode mask, uint operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="long"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<long> components, IterationMode mask, long operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="ulong"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<ulong> components, IterationMode mask, ulong operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="float"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<float> components, IterationMode mask, float operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);

    /// <summary>
    /// Replaces every <see cref="double"/> element of <paramref name="components"/> with its remainder modulo
    /// <paramref name="operand"/>.
    /// </summary>
    /// <param name="components">The component spans to update.</param>
    /// <param name="mask">The span operation strategy to use.</param>
    /// <param name="operand">The modulus applied to every element.</param>
    /// <param name="workerCount">The requested worker count, or <c>0</c> to derive it from the total element count.</param>
    public static void Modulo(this Components<double> components, IterationMode mask, double operand, int workerCount = 0)
        => ApplyNumberStrategy(components, mask, NumberOperation.Modulo, operand, workerCount);
#endif

    private static void ApplyNumberStrategy<T>(
        Components<T> components,
        IterationMode mask,
        NumberOperation operation,
        T operand,
        int workerCount)
        where T : unmanaged
    {
        // The per-segment implementation is always single-threaded. Parallelism only ever comes from the
        // cross-segment dispatch, so the two levels never nest their worker pools.
        var operationMask = (mask & IterationMode.Simd) != 0
            ? IterationMode.Simd
            : IterationMode.Serial;

        var spanOperation = SpanOperations.GetOperation<T, ISpanNumberOperation<T>>(operationMask)
                            ?? throw new NotSupportedException($"Type {typeof(T)} does not support number operations.");

        var state = new NumberState<T>(spanOperation, operation, operand);

        // No Parallel bit: run the whole query on the calling thread.
        if ((mask & IterationMode.Parallel) == 0)
        {
            var segments = components.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                state.Apply(segments[i]);
            }

            return;
        }

        // Parallel bit: one cross-segment dispatch, with a serial or SIMD body.
        components.ApplyParallel(state, static (span, value) => value.Apply(span), workerCount);
    }

    private static void ApplyOperationParallel<T, TOperation, TState>(
        Components<T> components,
        IterationMode mask,
        TState state,
        SpanOperationAction<T, TOperation, TState> action,
        int workerCount)
        where T : unmanaged
        where TOperation : class, ISpanOperation<T>
    {
        var operation = SpanOperations.GetOperation<T, TOperation>(mask)
                        ?? throw new NotSupportedException($"Type {typeof(T)} does not support {typeof(TOperation).Name}.");

        var dispatch = new OperationState<T, TOperation, TState>(operation, action, state);
        components.ApplyParallel(dispatch, static (span, value) => value.Apply(span), workerCount);
    }

    /// <summary>
    /// State for one bulk parallel dispatch. It maps an element range onto the underlying segments and invokes
    /// the caller action for the intersecting slices.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    private readonly struct ApplyState<T, TState> where T : unmanaged
    {
        private readonly Components<T>.SegmentsEnumerable m_Segments;
        private readonly SpanSegmentAction<T, TState> m_Action;
        private readonly TState m_State;

        public ApplyState(Components<T>.SegmentsEnumerable segments, SpanSegmentAction<T, TState> action, TState state)
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

    /// <summary>
    /// State for a resolved operation dispatch. The operation is resolved once per call, not per segment slice.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <typeparam name="TOperation">The span operation interface.</typeparam>
    /// <typeparam name="TState">The state passed to the action.</typeparam>
    private readonly struct OperationState<T, TOperation, TState>
        where T : unmanaged
        where TOperation : class, ISpanOperation<T>
    {
        private readonly TOperation m_Operation;
        private readonly SpanOperationAction<T, TOperation, TState> m_Action;
        private readonly TState m_State;

        public OperationState(TOperation operation, SpanOperationAction<T, TOperation, TState> action, TState state)
        {
            m_Operation = operation;
            m_Action = action;
            m_State = state;
        }

        public void Apply(Span<T> span) => m_Action(m_Operation, span, m_State);
    }

    /// <summary>
    /// State for the enum-driven number operation overload.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    private readonly struct NumberState<T> where T : unmanaged
    {
        private readonly ISpanNumberOperation<T> m_Operation;
        private readonly NumberOperation m_Kind;
        private readonly T m_Operand;

        public NumberState(ISpanNumberOperation<T> operation, NumberOperation kind, T operand)
        {
            m_Operation = operation;
            m_Kind = kind;
            m_Operand = operand;
        }

        public void Apply(Span<T> span)
        {
            switch (m_Kind)
            {
                case NumberOperation.Add:
                    m_Operation.Add(span, m_Operand, span);
                    break;
                case NumberOperation.Subtract:
                    m_Operation.Subtract(span, m_Operand, span);
                    break;
                case NumberOperation.Multiply:
                    m_Operation.Multiply(span, m_Operand, span);
                    break;
                case NumberOperation.Divide:
                    m_Operation.Divide(span, m_Operand, span);
                    break;
                case NumberOperation.Modulo:
                    m_Operation.Modulo(span, m_Operand, span);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_Kind), m_Kind, "Unknown number operation.");
            }
        }
    }
}