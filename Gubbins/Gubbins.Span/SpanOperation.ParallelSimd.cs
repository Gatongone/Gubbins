using System.Numerics;
using Gubbins.Unsafe;

namespace Gubbins.Span;

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdNumberOperations<T> : ISpanNumberOperations<T> where T : unmanaged
{
    private static readonly bool s_Supported  = typeof(T).CheckType().IsNumberType && Vector.IsHardwareAccelerated;
    private static readonly int  s_VectorSize = Vector<T>.Count;

    /// <inheritdoc />
    public bool Supported => s_Supported;

    /// <inheritdoc />
    public unsafe void Add(Span<T> src, T operand, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vb = new Vector<T>(operand);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<T>(from + j, s_VectorSize);
                        var va = new Vector<T>(current);
                        (va + vb).CopyTo(new Span<T>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Operations<T>.Add(from[j], operand);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Subtract(Span<T> src, T operand, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vb = new Vector<T>(operand);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<T>(from + j, s_VectorSize);
                        var va = new Vector<T>(current);
                        (va - vb).CopyTo(new Span<T>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Operations<T>.Subtract(from[j], operand);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Multiply(Span<T> src, T operand, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vb = new Vector<T>(operand);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<T>(from + j, s_VectorSize);
                        var va = new Vector<T>(current);
                        (va * vb).CopyTo(new Span<T>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Operations<T>.Multiply(from[j], operand);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Divide(Span<T> src, T operand, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vb = new Vector<T>(operand);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<T>(from + j, s_VectorSize);
                        var va = new Vector<T>(current);
                        (va / vb).CopyTo(new Span<T>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Operations<T>.Divide(from[j], operand);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Modulo(Span<T> src, T operand, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vb = new Vector<T>(operand);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<T>(from + j, s_VectorSize);
                        var va = new Vector<T>(current);
                        (va - va / vb * vb).CopyTo(new Span<T>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Operations<T>.Modulo(from[j], operand);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Max(Span<T> left, Span<T> right, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = result.Length;
        var chunkSize = srcLength / processorCount;
        fixed (T* lsrc = left)
        {
            fixed (T* rsrc = left)
            {
                fixed (T* dest = result)
                {
                    // Make the compiler happy.
                    var l = lsrc;
                    var r = rsrc;
                    var to = dest;
                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<int>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var leftCur = new Span<T>(l + i, s_VectorSize);
                            var rightCur = new Span<T>(r + i, s_VectorSize);
                            var va = new Vector<T>(leftCur);
                            var vb = new Vector<T>(rightCur);
                            var vr = Vector.Max(va, vb);
                            vr.CopyTo(new Span<T>(to + i, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            var leftOperand = l[i];
                            var rightOperand = r[i];
                            to[i] = Operations<T>.GreaterThan(leftOperand, rightOperand) ? leftOperand : rightOperand;
                        }
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Min(Span<T> left, Span<T> right, Span<T> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = result.Length;
        var chunkSize = srcLength / processorCount;
        fixed (T* lsrc = left)
        {
            fixed (T* rsrc = left)
            {
                fixed (T* dest = result)
                {
                    // Make the compiler happy.
                    var l = lsrc;
                    var r = rsrc;
                    var to = dest;
                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<int>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var leftCur = new Span<T>(l + i, s_VectorSize);
                            var rightCur = new Span<T>(r + i, s_VectorSize);
                            var va = new Vector<T>(leftCur);
                            var vb = new Vector<T>(rightCur);
                            var vr = Vector.Min(va, vb);
                            vr.CopyTo(new Span<T>(to + i, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            var leftOperand = l[i];
                            var rightOperand = r[i];
                            to[i] = Operations<T>.LessThan(leftOperand, rightOperand) ? leftOperand : rightOperand;
                        }
                    });
                }
            }
        }
    }
}

#if NET7_0_OR_GREATER
/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdIntOperation : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<int>.Count;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<int> src, int count, Span<int> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (int* ptr = src)
        {
            fixed (int* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<int>(from + j, s_VectorSize);
                        var va = new Vector<int>(current);
                        Vector.ShiftLeft(va, count).CopyTo(new Span<int>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] << count;
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<int> src, int count, Span<int> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (int* ptr = src)
        {
            fixed (int* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<int>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<int>(from + j, s_VectorSize);
                        var va = new Vector<int>(current);
                        Vector.ShiftRightArithmetic(va, count).CopyTo(new Span<int>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] >> count;
                    }
                });
            }
        }
    }
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdLongOperation : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<long>.Count;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<long> src, int count, Span<long> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (long* ptr = src)
        {
            fixed (long* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<long>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<long>(from + j, s_VectorSize);
                        var va = new Vector<long>(current);
                        Vector.ShiftLeft(va, count).CopyTo(new Span<long>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] << count;
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<long> src, int count, Span<long> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (long* ptr = src)
        {
            fixed (long* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<long>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<long>(from + j, s_VectorSize);
                        var va = new Vector<long>(current);
                        Vector.ShiftRightArithmetic(va, count).CopyTo(new Span<long>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] >> count;
                    }
                });
            }
        }
    }
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUintOperation : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<uint>.Count;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<uint> src, int count, Span<uint> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (uint* ptr = src)
        {
            fixed (uint* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<uint>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<uint>(from + j, s_VectorSize);
                        var va = new Vector<uint>(current);
                        Vector.ShiftLeft(va, count).CopyTo(new Span<uint>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] << count;
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<uint> src, int count, Span<uint> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (uint* ptr = src)
        {
            fixed (uint* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<uint>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<uint>(from + j, s_VectorSize);
                        var va = new Vector<uint>(current);
                        Vector.ShiftRightLogical(va, count).CopyTo(new Span<uint>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] >> count;
                    }
                });
            }
        }
    }
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdUlongOperation : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<ulong>.Count;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (ulong* ptr = src)
        {
            fixed (ulong* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<ulong>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<ulong>(from + j, s_VectorSize);
                        var va = new Vector<ulong>(current);
                        Vector.ShiftLeft(va, count).CopyTo(new Span<ulong>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] << count;
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (ulong* ptr = src)
        {
            fixed (ulong* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<ulong>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<ulong>(from + j, s_VectorSize);
                        var va = new Vector<ulong>(current);
                        Vector.ShiftRightLogical(va, count).CopyTo(new Span<ulong>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = from[j] >> count;
                    }
                });
            }
        }
    }
}
#endif

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdFloatOperation : ISpanRealOperations<float>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<float>.Count;

    /// <inheritdoc />
    public unsafe void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* px = x)
        {
            fixed (float* py = y)
            {
                fixed (float* pa = amount)
                {
                    fixed (float* dest = result)
                    {
                        // Make Compiler happy.
                        var fx = px;
                        var fy = px;
                        var fa = pa;
                        var to = dest;
                        Parallel.For(0, processorCount, i =>
                        {
                            var start = i * chunkSize;
                            var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                            // Calculate aligned.
                            var vectorSize = Vector<float>.Count;
                            var alignedStart = start;
                            var alignedEnd = end - (end - start) % vectorSize;

                            // SIMD Processing.
                            for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                            {
                                var vx = new Vector<float>(new Span<float>(fx + j, s_VectorSize));
                                var vy = new Vector<float>(new Span<float>(fy + j, s_VectorSize));
                                var va = new Vector<float>(new Span<float>(fa + j, s_VectorSize));
                                VectorMath.Lerp(vx, vy, va).CopyTo(new Span<float>(to + j, s_VectorSize));
                            }

                            // Calculate remaining elements.
                            for (var j = alignedEnd; j < end; j++)
                            {
                                to[j] = MathF.Log(fx[j]);
                            }
                        });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* px = x)
        {
            fixed (float* pa = amount)
            {
                fixed (float* dest = result)
                {
                    // Make Compiler happy.
                    var fx = px;
                    var fy = px;
                    var fa = pa;
                    var to = dest;
                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<float>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        var vy = new Vector<float>(y);
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var vx = new Vector<float>(new Span<float>(fx + j, s_VectorSize));
                            var va = new Vector<float>(new Span<float>(fa + j, s_VectorSize));
                            VectorMath.Lerp(vx, vy, va).CopyTo(new Span<float>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = MathF.Log(fx[j]);
                        }
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<float> src, float min, float max, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;
                var vmin = new Vector<float>(min);
                var vmax = new Vector<float>(max);
                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Clamp(va, vmin, vmax).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Min(MathF.Max(from[j], min), max);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                fixed (float* pmin = min)
                {
                    fixed (float* pmax = max)
                    {
                        // Make Compiler happy.
                        var from = ptr;
                        var to = dest;
                        var cmin = pmin;
                        var cmax = pmax;
                        Parallel.For(0, processorCount, i =>
                        {
                            var start = i * chunkSize;
                            var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                            // Calculate aligned.
                            var vectorSize = Vector<float>.Count;
                            var alignedStart = start;
                            var alignedEnd = end - (end - start) % vectorSize;

                            // SIMD Processing.
                            for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                            {
                                var va = new Vector<float>(new Span<float>(from + j, s_VectorSize));
                                var vmin = new Vector<float>(new Span<float>(cmin + j, s_VectorSize));
                                var vmax = new Vector<float>(new Span<float>(cmax + j, s_VectorSize));
                                VectorMath.Clamp(va, vmin, vmax).CopyTo(new Span<float>(to + j, s_VectorSize));
                            }

                            // Calculate remaining elements.
                            for (var j = alignedEnd; j < end; j++)
                            {
                                to[j] = MathF.Log(from[j]);
                            }
                        });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<float> x, Span<float> y, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptrx = x)
        {
            fixed (float* ptry = y)
            {
                fixed (float* dest = result)
                {
                    // Make Compiler happy.
                    var fromx = ptrx;
                    var fromy = ptry;
                    var to = dest;

                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<float>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var currentx = new Span<float>(fromx + j, s_VectorSize);
                            var currenty = new Span<float>(fromy + j, s_VectorSize);
                            var vx = new Vector<float>(currentx);
                            var vy = new Vector<float>(currenty);
                            VectorMath.Hypot(vx, vy).CopyTo(new Span<float>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = MathF.Sqrt(fromx[j] * fromx[j] + fromy[j] * fromy[j]);
                        }
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<float> x, float y, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptrx = x)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var fromx = ptrx;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vy = new Vector<float>(y);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var currentx = new Span<float>(fromx + j, s_VectorSize);
                        var vx = new Vector<float>(currentx);
                        VectorMath.Hypot(vx, vy).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    var doubleY = y * y;
                    for (var j = alignedEnd; j < end; j++)
                    {
                        var doubleX = fromx[j] * fromx[j];
                        to[j] = MathF.Sqrt(doubleX + doubleY);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Truncate(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Truncate(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Truncate(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sqrt(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        Vector.SquareRoot(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Sqrt(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Round(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Round(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Round(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Exp(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Exp(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Exp(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Log(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Log(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Log(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Floor(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Floor(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Floor(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Ceiling(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Ceiling(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Ceiling(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sin(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Sin(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Sin(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cos(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Cos(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Cos(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tan(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Tan(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Tan(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sinh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Sinh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Sinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cosh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Cosh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Cosh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tanh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Tanh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Tanh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asin(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Asinh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Asinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acos(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Acos(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Acos(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atan(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Atan(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Atan(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asinh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Asinh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Asinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acosh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Acosh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Acosh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atanh(Span<float> src, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<float>(from + j, s_VectorSize);
                        var va = new Vector<float>(current);
                        VectorMath.Atanh(va).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Atanh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<float> src, float exponent, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<float>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vy = new Vector<float>(exponent);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var currentx = new Span<float>(from + j, s_VectorSize);
                        var vx = new Vector<float>(currentx);
                        VectorMath.Pow(vx, vy).CopyTo(new Span<float>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = MathF.Pow(from[j], exponent);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<float> src, Span<float> exponent, Span<float> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (float* ptrx = src)
        {
            fixed (float* ptry = exponent)
            {
                fixed (float* dest = result)
                {
                    // Make Compiler happy.
                    var fromx = ptrx;
                    var fromy = ptry;
                    var to = dest;

                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<float>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var vx = new Vector<float>(new Span<float>(fromx + j, s_VectorSize));
                            var vy = new Vector<float>(new Span<float>(fromy + j, s_VectorSize));
                            VectorMath.Pow(vx, vy).CopyTo(new Span<float>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = MathF.Pow(fromx[j], fromy[j]);
                        }
                    });
                }
            }
        }
    }
}

/// <summary>
/// Parallel SIMD span operation.
/// </summary>
internal sealed class ParallelSimdDoubleOperation : ISpanRealOperations<double>
{
    /// <inheritdoc />
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<double>.Count;

    /// <inheritdoc />
    public unsafe void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* px = x)
        {
            fixed (double* py = y)
            {
                fixed (double* pa = amount)
                {
                    fixed (double* dest = result)
                    {
                        // Make Compiler happy.
                        var fx = px;
                        var fy = py;
                        var fa = pa;
                        var to = dest;
                        Parallel.For(0, processorCount, i =>
                        {
                            var start = i * chunkSize;
                            var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                            // Calculate aligned.
                            var vectorSize = Vector<double>.Count;
                            var alignedStart = start;
                            var alignedEnd = end - (end - start) % vectorSize;

                            // SIMD Processing.
                            for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                            {
                                var vx = new Vector<double>(new Span<double>(fx + j, s_VectorSize));
                                var vy = new Vector<double>(new Span<double>(fy + j, s_VectorSize));
                                var va = new Vector<double>(new Span<double>(fa + j, s_VectorSize));
                                VectorMath.Lerp(vx, vy, va).CopyTo(new Span<double>(to + j, s_VectorSize));
                            }

                            // Calculate remaining elements.
                            for (var j = alignedEnd; j < end; j++)
                            {
                                to[j] = Math.Log(fx[j]);
                            }
                        });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* px = x)
        {
            fixed (double* pa = amount)
            {
                fixed (double* dest = result)
                {
                    // Make Compiler happy.
                    var fx = px;
                    var fa = pa;
                    var to = dest;
                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<double>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        var vy = new Vector<double>(y);
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var vx = new Vector<double>(new Span<double>(fx + j, s_VectorSize));
                            var va = new Vector<double>(new Span<double>(fa + j, s_VectorSize));
                            VectorMath.Lerp(vx, vy, va).CopyTo(new Span<double>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = Math.Log(fx[j]);
                        }
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<double> src, double min, double max, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;
                var vmin = new Vector<double>(min);
                var vmax = new Vector<double>(max);
                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Clamp(va, vmin, vmax).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Clamp(from[j], min, max);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                fixed (double* pmin = min)
                {
                    fixed (double* pmax = max)
                    {
                        // Make Compiler happy.
                        var from = ptr;
                        var to = dest;
                        var cmin = pmin;
                        var cmax = pmax;
                        Parallel.For(0, processorCount, i =>
                        {
                            var start = i * chunkSize;
                            var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                            // Calculate aligned.
                            var vectorSize = Vector<double>.Count;
                            var alignedStart = start;
                            var alignedEnd = end - (end - start) % vectorSize;

                            // SIMD Processing.
                            for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                            {
                                var va = new Vector<double>(new Span<double>(from + j, s_VectorSize));
                                var vmin = new Vector<double>(new Span<double>(cmin + j, s_VectorSize));
                                var vmax = new Vector<double>(new Span<double>(cmax + j, s_VectorSize));
                                VectorMath.Clamp(va, vmin, vmax).CopyTo(new Span<double>(to + j, s_VectorSize));
                            }

                            // Calculate remaining elements.
                            for (var j = alignedEnd; j < end; j++)
                            {
                                to[j] = Math.Log(from[j]);
                            }
                        });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<double> x, Span<double> y, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptrx = x)
        {
            fixed (double* ptry = y)
            {
                fixed (double* dest = result)
                {
                    // Make Compiler happy.
                    var fromx = ptrx;
                    var fromy = ptry;
                    var to = dest;

                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<double>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var currentx = new Span<double>(fromx + j, s_VectorSize);
                            var currenty = new Span<double>(fromy + j, s_VectorSize);
                            var vx = new Vector<double>(currentx);
                            var vy = new Vector<double>(currenty);
                            VectorMath.Hypot(vx, vy).CopyTo(new Span<double>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = Math.Sqrt(fromx[j] * fromx[j] + fromy[j] * fromy[j]);
                        }
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<double> x, double y, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = x.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptrx = x)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var fromx = ptrx;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vy = new Vector<double>(y);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var currentx = new Span<double>(fromx + j, s_VectorSize);
                        var vx = new Vector<double>(currentx);
                        VectorMath.Hypot(vx, vy).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    var doubleY = y * y;
                    for (var j = alignedEnd; j < end; j++)
                    {
                        var doubleX = fromx[j] * fromx[j];
                        to[j] = Math.Sqrt(doubleX + doubleY);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Truncate(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Truncate(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Truncate(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sqrt(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        Vector.SquareRoot(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Sqrt(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Round(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Round(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Round(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Exp(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Exp(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Exp(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Log(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Log(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Log(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Floor(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Floor(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Floor(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Ceiling(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Ceiling(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Ceiling(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sin(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Sin(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Sin(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cos(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Cos(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Cos(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tan(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Tan(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Tan(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sinh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Sinh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Sinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cosh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Cosh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Cosh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tanh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Tanh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Tanh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asin(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Asinh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Asinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acos(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Acos(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Acos(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atan(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Atan(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Atan(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asinh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Asinh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Asinh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acosh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Acosh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Acosh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atanh(Span<double> src, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var current = new Span<double>(from + j, s_VectorSize);
                        var va = new Vector<double>(current);
                        VectorMath.Atanh(va).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Atanh(from[j]);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<double> src, double exponent, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make Compiler happy.
                var from = ptr;
                var to = dest;

                Parallel.For(0, processorCount, i =>
                {
                    var start = i * chunkSize;
                    var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                    // Calculate aligned.
                    var vectorSize = Vector<double>.Count;
                    var alignedStart = start;
                    var alignedEnd = end - (end - start) % vectorSize;

                    // SIMD Processing.
                    var vy = new Vector<double>(exponent);
                    for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                    {
                        var currentx = new Span<double>(from + j, s_VectorSize);
                        var vx = new Vector<double>(currentx);
                        VectorMath.Pow(vx, vy).CopyTo(new Span<double>(to + j, s_VectorSize));
                    }

                    // Calculate remaining elements.
                    for (var j = alignedEnd; j < end; j++)
                    {
                        to[j] = Math.Pow(from[j], exponent);
                    }
                });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<double> src, Span<double> exponent, Span<double> result)
    {
        var processorCount = Environment.ProcessorCount;
        var srcLength = src.Length;
        var chunkSize = srcLength / processorCount;

        fixed (double* ptrx = src)
        {
            fixed (double* ptry = exponent)
            {
                fixed (double* dest = result)
                {
                    // Make Compiler happy.
                    var fromx = ptrx;
                    var fromy = ptry;
                    var to = dest;

                    Parallel.For(0, processorCount, i =>
                    {
                        var start = i * chunkSize;
                        var end = i == processorCount - 1 ? srcLength : start + chunkSize;

                        // Calculate aligned.
                        var vectorSize = Vector<double>.Count;
                        var alignedStart = start;
                        var alignedEnd = end - (end - start) % vectorSize;

                        // SIMD Processing.
                        for (var j = alignedStart; j < alignedEnd; j += vectorSize)
                        {
                            var vx = new Vector<double>(new Span<double>(fromx + j, s_VectorSize));
                            var vy = new Vector<double>(new Span<double>(fromy + j, s_VectorSize));
                            VectorMath.Pow(vx, vy).CopyTo(new Span<double>(to + j, s_VectorSize));
                        }

                        // Calculate remaining elements.
                        for (var j = alignedEnd; j < end; j++)
                        {
                            to[j] = Math.Pow(fromx[j], fromy[j]);
                        }
                    });
                }
            }
        }
    }
}

internal sealed class ParallelSimdVectorOperation : ISpanVectorOperations<Vector2>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVectorOperation s_Simd = new();

    private enum BinaryVecOp
    {
        Dot,
        Cross,
        Reflect,
        FaceForwardNormal
    }

    private enum UnaryVecOp
    {
        Normalize
    }

    private enum UnaryScalarOp
    {
        Length,
        LengthSquared
    }

    private enum BinaryScalarOp
    {
        Distance,
        DistanceSquared,
        Angle
    }

    private static void ParallelChunks(int length, Action<int, int> body)
    {
        if (length <= 0) return;
        var workers = Math.Min(Environment.ProcessorCount, length);
        var chunkSize = (length + workers - 1) / workers;
        Parallel.For(0, workers, worker =>
        {
            var start = worker * chunkSize;
            if (start >= length) return;
            var end = Math.Min(start + chunkSize, length);
            body(start, end - start);
        });
    }

    private static unsafe void RunBinary(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result, BinaryVecOp op)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (Vector2* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector2>(l + start, len);
                var sr = new Span<Vector2>(r + start, len);
                var sd = new Span<Vector2>(d + start, len);
                switch (op)
                {
                    case BinaryVecOp.Dot:               s_Simd.Dot(sl, sr, sd); break;
                    case BinaryVecOp.Cross:             s_Simd.Cross(sl, sr, sd); break;
                    case BinaryVecOp.Reflect:           s_Simd.Reflect(sl, sr, sd); break;
                    case BinaryVecOp.FaceForwardNormal: s_Simd.FaceForward(sl, sr, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnary(Span<Vector2> src, Span<Vector2> result, UnaryVecOp op)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector2>(s + start, len);
                var sd = new Span<Vector2>(d + start, len);
                switch (op)
                {
                    case UnaryVecOp.Normalize: s_Simd.Normalize(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnaryScalar(Span<Vector2> src, Span<float> result, UnaryScalarOp op)
    {
        fixed (Vector2* ps = src)
        fixed (float* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector2>(s + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case UnaryScalarOp.Length:        s_Simd.Length(ss, sd); break;
                    case UnaryScalarOp.LengthSquared: s_Simd.LengthSquared(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunBinaryScalar(Span<Vector2> left, Span<Vector2> right, Span<float> result, BinaryScalarOp op)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (float* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector2>(l + start, len);
                var sr = new Span<Vector2>(r + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case BinaryScalarOp.Distance:        s_Simd.Distance(sl, sr, sd); break;
                    case BinaryScalarOp.DistanceSquared: s_Simd.DistanceSquared(sl, sr, sd); break;
                    case BinaryScalarOp.Angle:           s_Simd.Angle(sl, sr, sd); break;
                }
            });
        }
    }

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => RunBinary(left, right, result, BinaryVecOp.Dot);
    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result) => RunBinary(left, right, result, BinaryVecOp.Cross);
    public void Normalize(Span<Vector2> src, Span<Vector2> result) => RunUnary(src, result, UnaryVecOp.Normalize);
    public void Length(Span<Vector2> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.Length);
    public void LengthSquared(Span<Vector2> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.LengthSquared);
    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Distance);
    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.DistanceSquared);
    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Angle);
    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => RunBinary(src, normal, result, BinaryVecOp.Reflect);

    public unsafe void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pn = normal)
        fixed (Vector2* pd = result)
        {
            var s = ps;
            var n = pn;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.Refract(new Span<Vector2>(s + start, len), new Span<Vector2>(n + start, len), eta, new Span<Vector2>(d + start, len)));
        }
    }

    public unsafe void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pn = normal)
        fixed (float* pi = incident)
        fixed (Vector2* pd = result)
        {
            var s = ps;
            var n = pn;
            var i = pi;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.FaceForward(new Span<Vector2>(s + start, len), new Span<Vector2>(n + start, len), new Span<float>(i + start, len), new Span<Vector2>(d + start, len)));
        }
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result) => RunBinary(src, normal, result, BinaryVecOp.FaceForwardNormal);

    public unsafe void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pt = target)
        fixed (float* pm = maxDistanceDelta)
        fixed (Vector2* pd = result)
        {
            var s = ps;
            var t = pt;
            var m = pm;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.MoveTowards(new Span<Vector2>(s + start, len), new Span<Vector2>(t + start, len), new Span<float>(m + start, len), new Span<Vector2>(d + start, len)));
        }
    }
}

internal sealed class ParallelSimdVector3Operation : ISpanVectorOperations<Vector3>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVector3Operation s_Simd = new();

    private enum BinaryVecOp
    {
        Dot,
        Cross,
        Reflect,
        FaceForwardNormal
    }

    private enum UnaryVecOp
    {
        Normalize
    }

    private enum UnaryScalarOp
    {
        Length,
        LengthSquared
    }

    private enum BinaryScalarOp
    {
        Distance,
        DistanceSquared,
        Angle
    }

    private static void ParallelChunks(int length, Action<int, int> body)
    {
        if (length <= 0) return;
        var workers = Math.Min(Environment.ProcessorCount, length);
        var chunkSize = (length + workers - 1) / workers;
        Parallel.For(0, workers, worker =>
        {
            var start = worker * chunkSize;
            if (start >= length) return;
            var end = Math.Min(start + chunkSize, length);
            body(start, end - start);
        });
    }

    private static unsafe void RunBinary(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result, BinaryVecOp op)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (Vector3* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector3>(l + start, len);
                var sr = new Span<Vector3>(r + start, len);
                var sd = new Span<Vector3>(d + start, len);
                switch (op)
                {
                    case BinaryVecOp.Dot:               s_Simd.Dot(sl, sr, sd); break;
                    case BinaryVecOp.Cross:             s_Simd.Cross(sl, sr, sd); break;
                    case BinaryVecOp.Reflect:           s_Simd.Reflect(sl, sr, sd); break;
                    case BinaryVecOp.FaceForwardNormal: s_Simd.FaceForward(sl, sr, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnary(Span<Vector3> src, Span<Vector3> result, UnaryVecOp op)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector3>(s + start, len);
                var sd = new Span<Vector3>(d + start, len);
                switch (op)
                {
                    case UnaryVecOp.Normalize: s_Simd.Normalize(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnaryScalar(Span<Vector3> src, Span<float> result, UnaryScalarOp op)
    {
        fixed (Vector3* ps = src)
        fixed (float* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector3>(s + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case UnaryScalarOp.Length:        s_Simd.Length(ss, sd); break;
                    case UnaryScalarOp.LengthSquared: s_Simd.LengthSquared(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunBinaryScalar(Span<Vector3> left, Span<Vector3> right, Span<float> result, BinaryScalarOp op)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (float* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector3>(l + start, len);
                var sr = new Span<Vector3>(r + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case BinaryScalarOp.Distance:        s_Simd.Distance(sl, sr, sd); break;
                    case BinaryScalarOp.DistanceSquared: s_Simd.DistanceSquared(sl, sr, sd); break;
                    case BinaryScalarOp.Angle:           s_Simd.Angle(sl, sr, sd); break;
                }
            });
        }
    }

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => RunBinary(left, right, result, BinaryVecOp.Dot);
    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result) => RunBinary(left, right, result, BinaryVecOp.Cross);
    public void Normalize(Span<Vector3> src, Span<Vector3> result) => RunUnary(src, result, UnaryVecOp.Normalize);
    public void Length(Span<Vector3> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.Length);
    public void LengthSquared(Span<Vector3> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.LengthSquared);
    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Distance);
    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.DistanceSquared);
    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Angle);
    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => RunBinary(src, normal, result, BinaryVecOp.Reflect);

    public unsafe void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pn = normal)
        fixed (Vector3* pd = result)
        {
            var s = ps;
            var n = pn;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.Refract(new Span<Vector3>(s + start, len), new Span<Vector3>(n + start, len), eta, new Span<Vector3>(d + start, len)));
        }
    }

    public unsafe void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pn = normal)
        fixed (float* pi = incident)
        fixed (Vector3* pd = result)
        {
            var s = ps;
            var n = pn;
            var i = pi;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.FaceForward(new Span<Vector3>(s + start, len), new Span<Vector3>(n + start, len), new Span<float>(i + start, len), new Span<Vector3>(d + start, len)));
        }
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result) => RunBinary(src, normal, result, BinaryVecOp.FaceForwardNormal);

    public unsafe void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pt = target)
        fixed (float* pm = maxDistanceDelta)
        fixed (Vector3* pd = result)
        {
            var s = ps;
            var t = pt;
            var m = pm;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.MoveTowards(new Span<Vector3>(s + start, len), new Span<Vector3>(t + start, len), new Span<float>(m + start, len), new Span<Vector3>(d + start, len)));
        }
    }
}

internal sealed class ParallelSimdVector4Operation : ISpanVectorOperations<Vector4>
{
    public bool Supported => Vector.IsHardwareAccelerated;
    private static readonly SimdVector4Operation s_Simd = new();

    private enum BinaryVecOp
    {
        Dot,
        Cross,
        Reflect,
        FaceForwardNormal
    }

    private enum UnaryVecOp
    {
        Normalize
    }

    private enum UnaryScalarOp
    {
        Length,
        LengthSquared
    }

    private enum BinaryScalarOp
    {
        Distance,
        DistanceSquared,
        Angle
    }

    private static void ParallelChunks(int length, Action<int, int> body)
    {
        if (length <= 0) return;
        var workers = Math.Min(Environment.ProcessorCount, length);
        var chunkSize = (length + workers - 1) / workers;
        Parallel.For(0, workers, worker =>
        {
            var start = worker * chunkSize;
            if (start >= length) return;
            var end = Math.Min(start + chunkSize, length);
            body(start, end - start);
        });
    }

    private static unsafe void RunBinary(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result, BinaryVecOp op)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (Vector4* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector4>(l + start, len);
                var sr = new Span<Vector4>(r + start, len);
                var sd = new Span<Vector4>(d + start, len);
                switch (op)
                {
                    case BinaryVecOp.Dot:               s_Simd.Dot(sl, sr, sd); break;
                    case BinaryVecOp.Cross:             s_Simd.Cross(sl, sr, sd); break;
                    case BinaryVecOp.Reflect:           s_Simd.Reflect(sl, sr, sd); break;
                    case BinaryVecOp.FaceForwardNormal: s_Simd.FaceForward(sl, sr, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnary(Span<Vector4> src, Span<Vector4> result, UnaryVecOp op)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector4>(s + start, len);
                var sd = new Span<Vector4>(d + start, len);
                switch (op)
                {
                    case UnaryVecOp.Normalize: s_Simd.Normalize(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunUnaryScalar(Span<Vector4> src, Span<float> result, UnaryScalarOp op)
    {
        fixed (Vector4* ps = src)
        fixed (float* pd = result)
        {
            var s = ps;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var ss = new Span<Vector4>(s + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case UnaryScalarOp.Length:        s_Simd.Length(ss, sd); break;
                    case UnaryScalarOp.LengthSquared: s_Simd.LengthSquared(ss, sd); break;
                }
            });
        }
    }

    private static unsafe void RunBinaryScalar(Span<Vector4> left, Span<Vector4> right, Span<float> result, BinaryScalarOp op)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (float* pd = result)
        {
            var l = pl;
            var r = pr;
            var d = pd;
            ParallelChunks(result.Length, (start, len) =>
            {
                var sl = new Span<Vector4>(l + start, len);
                var sr = new Span<Vector4>(r + start, len);
                var sd = new Span<float>(d + start, len);
                switch (op)
                {
                    case BinaryScalarOp.Distance:        s_Simd.Distance(sl, sr, sd); break;
                    case BinaryScalarOp.DistanceSquared: s_Simd.DistanceSquared(sl, sr, sd); break;
                    case BinaryScalarOp.Angle:           s_Simd.Angle(sl, sr, sd); break;
                }
            });
        }
    }

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => RunBinary(left, right, result, BinaryVecOp.Dot);
    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result) => RunBinary(left, right, result, BinaryVecOp.Cross);
    public void Normalize(Span<Vector4> src, Span<Vector4> result) => RunUnary(src, result, UnaryVecOp.Normalize);
    public void Length(Span<Vector4> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.Length);
    public void LengthSquared(Span<Vector4> src, Span<float> result) => RunUnaryScalar(src, result, UnaryScalarOp.LengthSquared);
    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Distance);
    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.DistanceSquared);
    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result) => RunBinaryScalar(left, right, result, BinaryScalarOp.Angle);
    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => RunBinary(src, normal, result, BinaryVecOp.Reflect);

    public unsafe void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pn = normal)
        fixed (Vector4* pd = result)
        {
            var s = ps;
            var n = pn;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.Refract(new Span<Vector4>(s + start, len), new Span<Vector4>(n + start, len), eta, new Span<Vector4>(d + start, len)));
        }
    }

    public unsafe void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pn = normal)
        fixed (float* pi = incident)
        fixed (Vector4* pd = result)
        {
            var s = ps;
            var n = pn;
            var i = pi;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.FaceForward(new Span<Vector4>(s + start, len), new Span<Vector4>(n + start, len), new Span<float>(i + start, len), new Span<Vector4>(d + start, len)));
        }
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result) => RunBinary(src, normal, result, BinaryVecOp.FaceForwardNormal);

    public unsafe void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pt = target)
        fixed (float* pm = maxDistanceDelta)
        fixed (Vector4* pd = result)
        {
            var s = ps;
            var t = pt;
            var m = pm;
            var d = pd;
            ParallelChunks(result.Length, (start, len) => s_Simd.MoveTowards(new Span<Vector4>(s + start, len), new Span<Vector4>(t + start, len), new Span<float>(m + start, len), new Span<Vector4>(d + start, len)));
        }
    }
}