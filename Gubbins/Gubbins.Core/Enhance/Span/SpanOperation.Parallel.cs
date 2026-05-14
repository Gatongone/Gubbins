using System.Numerics;

namespace Gubbins.Enhance;

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelNumberOperations<T> : ISpanNumberOperations<T> where T : unmanaged
{
    private static readonly bool s_Supported = Environment.ProcessorCount >= 1 && typeof(T).CheckType().IsNumberType;
    public bool Supported => s_Supported;

    /// <inheritdoc/>
    public unsafe void Add(Span<T> src, T operand, Span<T> result)
    {
        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, src.Length, i => to[i] = Operations<T>.Add(from[i], operand));
            }
        }
    }

    /// <inheritdoc/>
    public unsafe void Subtract(Span<T> src, T operand, Span<T> result)
    {
        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, src.Length, i => to[i] = Operations<T>.Subtract(from[i], operand));
            }
        }
    }

    /// <inheritdoc/>
    public unsafe void Multiply(Span<T> src, T operand, Span<T> result)
    {
        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, src.Length, i => to[i] = Operations<T>.Multiply(from[i], operand));
            }
        }
    }

    /// <inheritdoc/>
    public unsafe void Divide(Span<T> src, T operand, Span<T> result)
    {
        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, src.Length, i => to[i] = Operations<T>.Divide(from[i], operand));
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Modulo(Span<T> src, T operand, Span<T> result)
    {
        fixed (T* ptr = src)
        {
            fixed (T* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, src.Length, i => to[i] = Operations<T>.Divide(from[i], operand));
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Max(Span<T> left, Span<T> right, Span<T> result)
    {
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
                    Parallel.For(0, result.Length, i =>
                    {
                        var leftOperand = l[i];
                        var rightOperand = r[i];
                        to[i] = Operations<T>.GreaterThan(leftOperand, rightOperand) ? leftOperand : rightOperand;
                    });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Min(Span<T> left, Span<T> right, Span<T> result)
    {
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
                    Parallel.For(0, result.Length, i =>
                    {
                        var leftOperand = l[i];
                        var rightOperand = r[i];
                        to[i] = Operations<T>.LessThan(leftOperand, rightOperand) ? leftOperand : rightOperand;
                    });
                }
            }
        }
    }
}

internal sealed class ParallelIntOperation : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<int> src, int count, Span<int> result)
    {
        fixed (int* ptr = src)
        {
            fixed (int* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] << count; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<int> src, int count, Span<int> result)
    {
        fixed (int* ptr = src)
        {
            fixed (int* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] >> count; });
            }
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelLongOperation : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<long> src, int count, Span<long> result)
    {
        fixed (long* ptr = src)
        {
            fixed (long* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] << count; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<long> src, int count, Span<long> result)
    {
        fixed (long* ptr = src)
        {
            fixed (long* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] >> count; });
            }
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelUintOperation : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<uint> src, int count, Span<uint> result)
    {
        fixed (uint* ptr = src)
        {
            fixed (uint* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] << count; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<uint> src, int count, Span<uint> result)
    {
        fixed (uint* ptr = src)
        {
            fixed (uint* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] >> count; });
            }
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelUlongOperation : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
    {
        fixed (ulong* ptr = src)
        {
            fixed (ulong* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] << count; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
    {
        fixed (ulong* ptr = src)
        {
            fixed (ulong* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = from[i] >> count; });
            }
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelFloatOperation : ISpanRealOperations<float>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void Floor(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Floor(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Ceiling(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Ceiling(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sqrt(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Sqrt(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Round(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Round(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Exp(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Exp(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Log(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Log(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Truncate(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Truncate(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
    {
        fixed (float* psrc = src)
        {
            fixed (float* pmin = min)
            {
                fixed (float* pmax = max)
                {
                    fixed (float* dest = result)
                    {
                        // Make the compiler happy.
                        var fromSrc = psrc;
                        var fromMin = pmin;
                        var fromMax = pmax;
                        var to = dest;
                        Parallel.For(0, result.Length, i => { to[i] = MathF.Min(MathF.Max(fromSrc[i], fromMin[i]), fromMax[i]); });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<float> src, float min, float max, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var fromSrc = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Min(MathF.Max(fromSrc[i], min), max); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
    {
        fixed (float* px = x)
        {
            fixed (float* py = y)
            {
                fixed (float* pa = amount)
                {
                    fixed (float* dest = result)
                    {
                        // Make the compiler happy.
                        var fromX = px;
                        var fromY = py;
                        var fromA = pa;
                        var to = dest;
                        Parallel.For(0, result.Length, i => { to[i] = fromX[i] * (1 - fromA[i]) + fromY[i] * fromA[i]; });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
    {
        fixed (float* px = x)
        {
            fixed (float* pa = amount)
            {
                fixed (float* dest = result)
                {
                    // Make the compiler happy.
                    var fromX = px;
                    var fromA = pa;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = fromX[i] * (1 - fromA[i]) + y * fromA[i]; });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<float> x, Span<float> y, Span<float> result)
    {
        fixed (float* px = x)
        {
            fixed (float* py = y)
            {
                fixed (float* dest = result)
                {
                    // Make the compiler happy.
                    var fromX = px;
                    var fromY = py;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = fromX[i] * fromX[i] + fromY[i] * fromY[i]; });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<float> x, float y, Span<float> result)
    {
        fixed (float* px = x)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var fromX = px;
                var fromY = y * y;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = fromX[i] * fromX[i] + fromY; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<float> src, float exponent, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Pow(from[i], exponent); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<float> src, Span<float> exponent, Span<float> result)
    {
        fixed (float* ps = src)
        {
            fixed (float* pe = exponent)
            {
                fixed (float* dest = result)
                {
                    // Make the compiler happy.
                    var fromS = ps;
                    var fromE = pe;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = MathF.Pow(fromS[i], fromE[i]); });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sin(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Sin(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cos(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Cos(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tan(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Tan(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sinh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Sinh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cosh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Cosh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tanh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Tanh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asin(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Asin(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acos(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Acos(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atan(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Atan(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asinh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Asinh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acosh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Acosh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atanh(Span<float> src, Span<float> result)
    {
        fixed (float* ptr = src)
        {
            fixed (float* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = MathF.Atanh(from[i]); });
            }
        }
    }
}

/// <summary>
/// Parallel span operation.
/// </summary>
internal sealed class ParallelDoubleOperation : ISpanRealOperations<double>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public unsafe void Floor(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Floor(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Ceiling(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Ceiling(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sqrt(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Sqrt(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Round(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Round(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Exp(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Exp(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Log(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Log(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Truncate(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Truncate(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
    {
        fixed (double* psrc = src)
        {
            fixed (double* pmin = min)
            {
                fixed (double* pmax = max)
                {
                    fixed (double* dest = result)
                    {
                        // Make the compiler happy.
                        var fromSrc = psrc;
                        var fromMin = pmin;
                        var fromMax = pmax;
                        var to = dest;
                        Parallel.For(0, result.Length, i => { to[i] = Math.Min(Math.Max(fromSrc[i], fromMin[i]), fromMax[i]); });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Clamp(Span<double> src, double min, double max, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var fromSrc = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Min(Math.Max(fromSrc[i], min), max); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
    {
        fixed (double* px = x)
        {
            fixed (double* py = y)
            {
                fixed (double* pa = amount)
                {
                    fixed (double* dest = result)
                    {
                        // Make the compiler happy.
                        var fromX = px;
                        var fromY = py;
                        var fromA = pa;
                        var to = dest;
                        Parallel.For(0, result.Length, i => { to[i] = fromX[i] * (1 - fromA[i]) + fromY[i] * fromA[i]; });
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
    {
        fixed (double* px = x)
        {
            fixed (double* pa = amount)
            {
                fixed (double* dest = result)
                {
                    // Make the compiler happy.
                    var fromX = px;
                    var fromA = pa;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = fromX[i] * (1 - fromA[i]) + y * fromA[i]; });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<double> x, Span<double> y, Span<double> result)
    {
        fixed (double* px = x)
        {
            fixed (double* py = y)
            {
                fixed (double* dest = result)
                {
                    // Make the compiler happy.
                    var fromX = px;
                    var fromY = py;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = fromX[i] * fromX[i] + fromY[i] * fromY[i]; });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Hypot(Span<double> x, double y, Span<double> result)
    {
        fixed (double* px = x)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var fromX = px;
                var fromY = y * y;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = fromX[i] * fromX[i] + fromY; });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<double> src, double exponent, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Pow(from[i], exponent); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Pow(Span<double> src, Span<double> exponent, Span<double> result)
    {
        fixed (double* ps = src)
        {
            fixed (double* pe = exponent)
            {
                fixed (double* dest = result)
                {
                    // Make the compiler happy.
                    var fromS = ps;
                    var fromE = pe;
                    var to = dest;
                    Parallel.For(0, result.Length, i => { to[i] = Math.Pow(fromS[i], fromE[i]); });
                }
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sin(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Sin(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cos(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Cos(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tan(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Tan(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Sinh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Sinh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Cosh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Cosh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Tanh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Tanh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asin(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Asin(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acos(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Acos(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atan(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Atan(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Asinh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Asinh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Acosh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Acosh(from[i]); });
            }
        }
    }

    /// <inheritdoc />
    public unsafe void Atanh(Span<double> src, Span<double> result)
    {
        fixed (double* ptr = src)
        {
            fixed (double* dest = result)
            {
                // Make the compiler happy.
                var from = ptr;
                var to = dest;
                Parallel.For(0, result.Length, i => { to[i] = Math.Atanh(from[i]); });
            }
        }
    }
}

internal sealed class ParallelVector2Operation : ISpanVectorOperations<Vector2>
{
    public bool Supported => true;

    public unsafe void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (Vector2* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var dot = Vector2.Dot(l[i], r[i]);
                to[i] = new Vector2(dot, dot);
            });
        }
    }

    public unsafe void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (Vector2* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var lv = l[i];
                var rv = r[i];
                var cross = lv.X * rv.Y - lv.Y * rv.X;
                to[i] = new Vector2(cross, cross);
            });
        }
    }

    public unsafe void Normalize(Span<Vector2> src, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector2.Normalize(s[i]));
        }
    }

    public unsafe void Length(Span<Vector2> src, Span<float> result)
    {
        LengthSquared(src, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void LengthSquared(Span<Vector2> src, Span<float> result)
    {
        fixed (Vector2* ps = src)
        fixed (float* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = s[i].LengthSquared());
        }
    }

    public unsafe void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector2.DistanceSquared(l[i], r[i]));
        }
    }

    public unsafe void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        fixed (Vector2* pl = left)
        fixed (Vector2* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var lv = l[i];
                var rv = r[i];
                var denominator = lv.Length() * rv.Length();
                to[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector2.Dot(lv, rv) / denominator, -1f, 1f));
            });
        }
    }

    public unsafe void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pn = normal)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector2.Reflect(s[i], n[i]));
        }
    }

    public unsafe void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pn = normal)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            var etaRatio = eta.X;
            Parallel.For(0, result.Length, i => to[i] = RefractScalar(s[i], n[i], etaRatio));
        }
    }

    public unsafe void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (float* pi = incident)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var inc = pi;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = inc[i] < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pn = normal)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector2.Dot(n[i], s[i]) < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result)
    {
        fixed (Vector2* ps = src)
        fixed (Vector2* pt = target)
        fixed (float* pd = maxDistanceDelta)
        fixed (Vector2* dest = result)
        {
            var s = ps;
            var t = pt;
            var d = pd;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = MoveTowardsScalar(s[i], t[i], d[i]));
        }
    }

    private static Vector2 RefractScalar(Vector2 incident, Vector2 normal, float eta)
    {
        var dot = Vector2.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector2.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

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

internal sealed class ParallelVector3Operation : ISpanVectorOperations<Vector3>
{
    public bool Supported => true;

    public unsafe void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (Vector3* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var dot = Vector3.Dot(l[i], r[i]);
                to[i] = new Vector3(dot, dot, dot);
            });
        }
    }

    public unsafe void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (Vector3* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector3.Cross(l[i], r[i]));
        }
    }

    public unsafe void Normalize(Span<Vector3> src, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector3.Normalize(s[i]));
        }
    }

    public unsafe void Length(Span<Vector3> src, Span<float> result)
    {
        LengthSquared(src, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void LengthSquared(Span<Vector3> src, Span<float> result)
    {
        fixed (Vector3* ps = src)
        fixed (float* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = s[i].LengthSquared());
        }
    }

    public unsafe void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector3.DistanceSquared(l[i], r[i]));
        }
    }

    public unsafe void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        fixed (Vector3* pl = left)
        fixed (Vector3* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var lv = l[i];
                var rv = r[i];
                var denominator = lv.Length() * rv.Length();
                to[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(lv, rv) / denominator, -1f, 1f));
            });
        }
    }

    public unsafe void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pn = normal)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector3.Reflect(s[i], n[i]));
        }
    }

    public unsafe void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pn = normal)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            var etaRatio = eta.X;
            Parallel.For(0, result.Length, i => to[i] = RefractScalar(s[i], n[i], etaRatio));
        }
    }

    public unsafe void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (float* pi = incident)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var inc = pi;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = inc[i] < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pn = normal)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector3.Dot(n[i], s[i]) < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result)
    {
        fixed (Vector3* ps = src)
        fixed (Vector3* pt = target)
        fixed (float* pd = maxDistanceDelta)
        fixed (Vector3* dest = result)
        {
            var s = ps;
            var t = pt;
            var d = pd;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = MoveTowardsScalar(s[i], t[i], d[i]));
        }
    }

    private static Vector3 RefractScalar(Vector3 incident, Vector3 normal, float eta)
    {
        var dot = Vector3.Dot(normal, incident);
        var k = 1f - eta * eta * (1f - dot * dot);
        return k < 0f ? Vector3.Zero : eta * incident - (eta * dot + MathF.Sqrt(k)) * normal;
    }

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

internal sealed class ParallelVector4Operation : ISpanVectorOperations<Vector4>
{
    public bool Supported => true;

    public unsafe void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (Vector4* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var dot = Vector4.Dot(l[i], r[i]);
                to[i] = new Vector4(dot, dot, dot, dot);
            });
        }
    }

    public unsafe void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (Vector4* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = CrossScalar(l[i], r[i]));
        }
    }

    public unsafe void Normalize(Span<Vector4> src, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector4.Normalize(s[i]));
        }
    }

    public unsafe void Length(Span<Vector4> src, Span<float> result)
    {
        LengthSquared(src, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void LengthSquared(Span<Vector4> src, Span<float> result)
    {
        fixed (Vector4* ps = src)
        fixed (float* dest = result)
        {
            var s = ps;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = s[i].LengthSquared());
        }
    }

    public unsafe void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        fixed (float* pr = result)
        {
            var values = pr;
            Parallel.For(0, result.Length, i => values[i] = MathF.Sqrt(values[i]));
        }
    }

    public unsafe void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector4.DistanceSquared(l[i], r[i]));
        }
    }

    public unsafe void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        fixed (Vector4* pl = left)
        fixed (Vector4* pr = right)
        fixed (float* dest = result)
        {
            var l = pl;
            var r = pr;
            var to = dest;
            Parallel.For(0, result.Length, i =>
            {
                var lv = l[i];
                var rv = r[i];
                var denominator = lv.Length() * rv.Length();
                to[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(lv, rv) / denominator, -1f, 1f));
            });
        }
    }

    public unsafe void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pn = normal)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = ReflectScalar(s[i], n[i]));
        }
    }

    public unsafe void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pn = normal)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            var etaRatio = eta.X;
            Parallel.For(0, result.Length, i => to[i] = RefractScalar(s[i], n[i], etaRatio));
        }
    }

    public unsafe void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (float* pi = incident)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var inc = pi;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = inc[i] < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pn = normal)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var n = pn;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = Vector4.Dot(n[i], s[i]) < 0f ? s[i] : -s[i]);
        }
    }

    public unsafe void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result)
    {
        fixed (Vector4* ps = src)
        fixed (Vector4* pt = target)
        fixed (float* pd = maxDistanceDelta)
        fixed (Vector4* dest = result)
        {
            var s = ps;
            var t = pt;
            var d = pd;
            var to = dest;
            Parallel.For(0, result.Length, i => to[i] = MoveTowardsScalar(s[i], t[i], d[i]));
        }
    }

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
