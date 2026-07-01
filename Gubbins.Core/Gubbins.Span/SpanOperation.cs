namespace Gubbins.Span;

/// <summary>
/// Flags that describe which span operation implementation strategy should be used.
/// </summary>
[Flags]
public enum SpanOperationMask
{
    /// <summary>
    /// Automatically select the best available implementation.
    /// The order of preference is SIMD + Parallel, then SIMD, then Parallel, then Serial.
    /// </summary>
    Auto = -1,

    /// <summary>
    /// Indicates that the operation can use SIMD acceleration.
    /// </summary>
    Simd = 2,

    /// <summary>
    /// Indicates that the operation should run on a single thread.
    /// </summary>
    Serial = 3,

    /// <summary>
    /// Indicates that the operation can run in parallel.
    /// </summary>
    Parallel = 4
}

/// <summary>
/// Marks a struct component and declares which span operation interfaces should be generated.
/// </summary>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
public sealed class SpanOperationAttribute : Attribute
{
    /// <summary>
    /// Initializes the attribute with the operation interfaces to generate.
    /// </summary>
    /// <param name="operations">Operation interface types that should be generated for the annotated struct.</param>
    public SpanOperationAttribute(params Type[] operations) { }

    /// <summary>
    /// Backward-compatible overload for existing call sites that bind generation metadata explicitly.
    /// </summary>
    /// <param name="type">Legacy operation type argument retained for compatibility.</param>
    /// <param name="member">The component member to bind generated operations to.</param>
    /// <param name="method">A custom generated extension method name.</param>
    /// <param name="overwrite">A method name that should be overwritten/replaced by generation.</param>
    public SpanOperationAttribute(Type type, string member, string method, string overwrite)
    {
        Member    = member;
        Method    = method;
        Overwrite = overwrite;
    }

    /// <summary>
    /// Optional component member name that listed operations should bind to.
    /// </summary>
    public string Member { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom generated extension method name.
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Optional generated method name to overwrite.
    /// </summary>
    public string Overwrite { get; set; } = string.Empty;
}

/// <summary>
/// Registers and resolves span operation implementations for component types.
/// </summary>
public static class SpanOperations
{
    /// <summary>
    /// Internal storage for registered span operation implementations, organized by component type and execution strategy.
    /// </summary>
    private static class Simd<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support parallel execution, organized by component type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private static class Serial<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class Parallel<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class SimdParallel<T>
    {
        internal static readonly Queue<ISpanOperation<T>> Operations = new();
    }

    static SpanOperations()
    {
        RegisterSpanOperation(new SerialNumberOperation<int>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialNumberOperation<uint>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialNumberOperation<long>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialNumberOperation<ulong>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialNumberOperation<float>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialNumberOperation<double>(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialIntOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialUintOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialLongOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialUlongOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialFloatOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialDoubleOperation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialVector2Operation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialVector3Operation(), SpanOperationMask.Serial);
        RegisterSpanOperation(new SerialVector4Operation(), SpanOperationMask.Serial);

        RegisterSpanOperation(new ParallelNumberOperation<int>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<uint>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<long>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<ulong>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<float>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelNumberOperation<double>(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelIntOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelUintOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelLongOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelUlongOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelFloatOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelDoubleOperation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelVector2Operation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelVector3Operation(), SpanOperationMask.Parallel);
        RegisterSpanOperation(new ParallelVector4Operation(), SpanOperationMask.Parallel);

        RegisterSpanOperation(new SimdNumberOperation<int>(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdNumberOperation<uint>(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdNumberOperation<long>(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdNumberOperation<ulong>(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdNumberOperation<float>(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdNumberOperation<double>(), SpanOperationMask.Simd);
#if NET7_0_OR_GREATER
        RegisterSpanOperation(new SimdIntOperation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdUintOperation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdLongOperation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdUlongOperation(), SpanOperationMask.Simd);
#endif
        RegisterSpanOperation(new SimdFloatOperation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdDoubleOperation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdVector2Operation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdVector3Operation(), SpanOperationMask.Simd);
        RegisterSpanOperation(new SimdVector4Operation(), SpanOperationMask.Simd);

        RegisterSpanOperation(new ParallelSimdNumberOperation<int>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<uint>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<long>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<ulong>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<float>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdNumberOperation<double>(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
#if NET7_0_OR_GREATER
        RegisterSpanOperation(new ParallelSimdIntOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdUintOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdLongOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdUlongOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
#endif
        RegisterSpanOperation(new ParallelSimdFloatOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdDoubleOperation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdVector2Operation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdVector3Operation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
        RegisterSpanOperation(new ParallelSimdVector4Operation(), SpanOperationMask.Parallel | SpanOperationMask.Simd);
    }

    /// <summary>
    /// Registers a span operation implementation for a component type and execution mask.
    /// </summary>
    /// <typeparam name="T">The component type the operation applies to.</typeparam>
    /// <param name="operation">The operation implementation to register.</param>
    /// <param name="mask">
    /// The strategy flags that determine which internal queue receives the operation.
    /// If no explicit SIMD/Parallel match is found, registration falls back to the serial queue.
    /// </param>
    public static void RegisterSpanOperation<T>(ISpanOperation<T> operation, SpanOperationMask mask)
    {
        if ((mask & SpanOperationMask.Simd) == SpanOperationMask.Simd)
        {
            if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
            {
                SimdParallel<T>.Operations.Enqueue(operation);
            }
            else
            {
                Simd<T>.Operations.Enqueue(operation);
            }
        }
        else if ((mask & SpanOperationMask.Serial) == SpanOperationMask.Serial)
        {
            Serial<T>.Operations.Enqueue(operation);
        }
        else if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
        {
            Parallel<T>.Operations.Enqueue(operation);
        }
        else
        {
            Serial<T>.Operations.Enqueue(operation);
        }
    }

    /// <summary>
    /// Gets the first supported operation implementation that matches the requested mask.
    /// </summary>
    /// <typeparam name="T">The component type the operation applies to.</typeparam>
    /// <typeparam name="TOperation">The specific operation interface/class to resolve.</typeparam>
    /// <param name="mask">The strategy flags used to choose candidate queues. Defaults to <see cref="SpanOperationMask.Auto"/>.</param>
    /// <returns>
    /// The first supported operation of type <typeparamref name="TOperation"/>, or <see langword="null"/> when no matching supported operation is available.
    /// </returns>
    /// <remarks>
    /// This method dequeues candidates while searching, so registrations are consumed as they are evaluated.
    /// With <see cref="SpanOperationMask.Auto"/>, lookup preference is SIMD + Parallel, then SIMD, then Parallel, then Serial.
    /// </remarks>
    public static TOperation? GetOperation<T, TOperation>(SpanOperationMask mask = SpanOperationMask.Auto) where TOperation : class, ISpanOperation<T>
    {
        if ((mask & SpanOperationMask.Auto) == SpanOperationMask.Auto)
        {
            while (SimdParallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation is TOperation {Supported: true} result) return result;
            }

            while (Simd<T>.Operations.TryDequeue(out var operation))
            {
                if (operation is TOperation {Supported: true} result) return result;
            }

            while (Parallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation is TOperation {Supported: true} result) return result;
            }

            while (Serial<T>.Operations.TryDequeue(out var operation))
            {
                if (operation is TOperation {Supported: true} result) return result;
            }
        }

        if ((mask & SpanOperationMask.Simd) == SpanOperationMask.Simd)
        {
            if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
            {
                while (SimdParallel<T>.Operations.TryDequeue(out var operation))
                {
                    if (operation is TOperation {Supported: true} result) return result;
                }
            }
            else
            {
                while (Simd<T>.Operations.TryDequeue(out var operation))
                {
                    if (operation is TOperation {Supported: true} result) return result;
                }
            }
        }
        else if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
        {
            while (Parallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation is TOperation {Supported: true} result) return result;
            }
        }

        while (Serial<T>.Operations.TryDequeue(out var operation))
        {
            if (operation is TOperation {Supported: true} result) return result;
        }

        return null;
    }
}