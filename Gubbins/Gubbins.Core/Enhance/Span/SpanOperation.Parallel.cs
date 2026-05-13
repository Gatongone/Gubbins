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