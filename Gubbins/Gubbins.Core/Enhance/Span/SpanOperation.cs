using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Enhance;

[Flags]
public enum SpanOperationMask
{
    /// <summary>
    /// Automatically select the best available implementation.
    /// The order of preference is SIMD + Parallel > SIMD > Parallel > Serial.
    /// </summary>
    Auto = -1,

    /// <summary>
    /// Indicates that the span operation implementation supports SIMD acceleration.
    /// If this flag is set, the system will attempt to use SIMD-optimized code paths for the operation, which can significantly improve performance for large spans on supported hardware.
    /// </summary>
    Simd = 2,

    /// <summary>
    /// Indicates that the span operation implementation supports parallel execution.
    /// </summary>
    Serial = 3,

    /// <summary>
    /// Indicates that the span operation implementation supports parallel execution.
    /// </summary>
    Parallel = 4
}

internal static class SpanOperations
{
    private static class Simd<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    private static class Serial<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    private static class Parallel<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    private static class SimdParallel<T>
    {
        internal static readonly Queue<ISpanOperation> Operations = new();
    }

    internal static void RegisterSpanOperations<T>(ISpanOperation operation, SpanOperationMask mask)
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

    internal static TOperation? GetOperation<T, TOperation>(SpanOperationMask mask = SpanOperationMask.Auto) where TOperation : class, ISpanOperation
    {
        if ((mask & SpanOperationMask.Auto) == SpanOperationMask.Auto)
        {
            while (SimdParallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
            }

            while (Simd<T>.Operations.TryDequeue(out var operation))
            {
                if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
            }

            while (Parallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
            }

            while (Serial<T>.Operations.TryDequeue(out var operation))
            {
                if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
            }
        }

        if ((mask & SpanOperationMask.Simd) == SpanOperationMask.Simd)
        {
            if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
            {
                while (SimdParallel<T>.Operations.TryDequeue(out var operation))
                {
                    if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
                }
            }
            else
            {
                while (Simd<T>.Operations.TryDequeue(out var operation))
                {
                    if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
                }
            }
        }
        else if ((mask & SpanOperationMask.Parallel) == SpanOperationMask.Parallel)
        {
            while (Parallel<T>.Operations.TryDequeue(out var operation))
            {
                if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
            }
        }

        while (Serial<T>.Operations.TryDequeue(out var operation))
        {
            if (operation.Supported && operation.GetType() == typeof(TOperation)) return (TOperation) operation;
        }

        return null;
    }
}
