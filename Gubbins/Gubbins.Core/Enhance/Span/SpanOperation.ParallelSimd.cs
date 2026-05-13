using System.Numerics;

namespace Gubbins.Enhance;

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