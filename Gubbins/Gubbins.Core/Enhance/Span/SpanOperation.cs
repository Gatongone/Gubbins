namespace Gubbins.Enhance;

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
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support parallel execution, organized by component type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private static class Serial<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class Parallel<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    /// <summary>
    /// Internal storage for registered span operation implementations that support both SIMD and parallel execution, organized by component type.
    /// </summary>
    private static class SimdParallel<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
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
    public static void RegisterSpanOperations<T>(ISpanOperation operation, SpanOperationMask mask)
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
    public static TOperation? GetOperation<T, TOperation>(SpanOperationMask mask = SpanOperationMask.Auto) where TOperation : class, ISpanOperation
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