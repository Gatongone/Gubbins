namespace Gubbins.Enhance;

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialNumberOperations<T> : ISpanNumberOperations<T>, ISpanGetMax<T>, ISpanGetMin<T> where T : unmanaged
{
    public bool Supported => true;

    /// <inheritdoc/>
    public void Add(Span<T> src, T operand, Span<T> result)
    {
        for (var i = 0; i < src.Length; i++)
        {
            result[i] = Operations<T>.Add(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Subtract(Span<T> src, T operand, Span<T> result)
    {
        for (var i = 0; i < src.Length; i++)
        {
            result[i] = Operations<T>.Subtract(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Multiply(Span<T> src, T operand, Span<T> result)
    {
        for (var i = 0; i < src.Length; i++)
        {
            result[i] = Operations<T>.Multiply(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Divide(Span<T> src, T operand, Span<T> result)
    {
        for (var i = 0; i < src.Length; i++)
        {
            result[i] = Operations<T>.Divide(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Modulo(Span<T> src, T operand, Span<T> result)
    {
        for (var i = 0; i < src.Length; i++)
        {
            result[i] = Operations<T>.Modulo(src[i], operand);
        }
    }

    /// <inheritdoc />
    public void Max(Span<T> left, Span<T> right, Span<T> result)
    {
        for (var index = 0; index < left.Length; index++)
        {
            var l = left[index];
            var r = right[index];
            result[index] = Operations<T>.GreaterThan(l, r) ? l : r;
        }
    }

    /// <inheritdoc />
    public T GetMax(Span<T> src)
    {
        var max = src[0];
        for (var index = 0; index < src.Length; index++)
        {
            var cur = src[index];
            if (Operations<T>.GreaterThan(cur, max))
            {
                max = cur;
            }
        }

        return max;
    }

    /// <inheritdoc />
    public void Min(Span<T> left, Span<T> right, Span<T> result)
    {
        for (var index = 0; index < left.Length; index++)
        {
            var l = left[index];
            var r = right[index];
            result[index] = Operations<T>.LessThan(l, r) ? l : r;
        }
    }

    /// <inheritdoc />
    public T GetMin(Span<T> src)
    {
        var min = src[0];
        for (var index = 0; index < src.Length; index++)
        {
            var cur = src[index];
            if (Operations<T>.LessThan(cur, min))
            {
                min = cur;
            }
        }

        return min;
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialIntOperations : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc/>
    public void ShiftLeft(Span<int> src, int count, Span<int> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<int> src, int count, Span<int> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] >> count;
        }
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialUintOperations : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc/>
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<uint> src, int count, Span<uint> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] >> count;
        }
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialLongOperations : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc/>
    public void ShiftLeft(Span<long> src, int count, Span<long> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<long> src, int count, Span<long> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] >> count;
        }
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialUlongOperations : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc/>
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = src[index] >> count;
        }
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialFloatOperations : ISpanRealOperations<float>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public void Round(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Round(src[index]);
        }
    }

    /// <inheritdoc />
    public void Exp(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Exp(src[index]);
        }
    }

    /// <inheritdoc />
    public void Log(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Log(src[index]);
        }
    }

    /// <inheritdoc />
    public void Truncate(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Truncate(src[index]);
        }
    }

    /// <inheritdoc />
    public void Floor(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Floor(src[index]);
        }
    }

    /// <inheritdoc />
    public void Ceiling(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Ceiling(src[index]);
        }
    }

    /// <inheritdoc/>
    public void Sqrt(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Sqrt(src[index]);
        }
    }

    /// <inheritdoc/>
    public void Pow(Span<float> src, float exponent, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Pow(src[index], exponent);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<float> src, Span<float> exponent, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Pow(src[index], exponent[index]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Min(MathF.Max(src[index], min[index]), max[index]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, float min, float max, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Min(MathF.Max(src[index], min), max);
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = x[index] * (1 - amount[index]) + y[index] * amount[index];
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = x[index] * (1 - amount[index]) + y * amount[index];
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, Span<float> y, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = MathF.Sqrt(x[index] * x[index] + y[index] * y[index]);
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, float y, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = MathF.Sqrt(x[index] * x[index] + y * y);
        }
    }

    /// <inheritdoc />
    public void Sin(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Sin(src[index]);
        }
    }

    /// <inheritdoc />
    public void Cos(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Cos(src[index]);
        }
    }

    /// <inheritdoc />
    public void Tan(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Tan(src[index]);
        }
    }

    /// <inheritdoc />
    public void Sinh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Sinh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Cosh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Cosh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Tanh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Tanh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Asin(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Asin(src[index]);
        }
    }

    /// <inheritdoc />
    public void Acos(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Acos(src[index]);
        }
    }

    /// <inheritdoc />
    public void Atan(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Atan(src[index]);
        }
    }

    /// <inheritdoc />
    public void Asinh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Asinh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Acosh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Acosh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Atanh(Span<float> src, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = MathF.Atanh(src[index]);
        }
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
internal sealed class SerialDoubleOperations : ISpanRealOperations<double>
{
    /// <inheritdoc />
    public bool Supported => true;

    /// <inheritdoc />
    public void Round(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Round(src[index]);
        }
    }

    /// <inheritdoc />
    public void Exp(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Exp(src[index]);
        }
    }

    /// <inheritdoc />
    public void Log(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Log(src[index]);
        }
    }

    /// <inheritdoc />
    public void Truncate(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Truncate(src[index]);
        }
    }

    /// <inheritdoc />
    public void Floor(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Floor(src[index]);
        }
    }

    /// <inheritdoc />
    public void Ceiling(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Ceiling(src[index]);
        }
    }

    /// <inheritdoc/>
    public void Sqrt(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Sqrt(src[index]);
        }
    }

    /// <inheritdoc/>
    public void Pow(Span<double> src, double exponent, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Pow(src[index], exponent);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<double> src, Span<double> exponent, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Pow(src[index], exponent[index]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Min(Math.Max(src[index], min[index]), max[index]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, double min, double max, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Min(Math.Max(src[index], min), max);
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = x[index] * (1 - amount[index]) + y[index] * amount[index];
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = x[index] * (1 - amount[index]) + y * amount[index];
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, Span<double> y, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = Math.Sqrt(x[index] * x[index] + y[index] * y[index]);
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, double y, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
        {
            result[index] = Math.Sqrt(x[index] * x[index] + y * y);
        }
    }

    /// <inheritdoc />
    public void Sin(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Sin(src[index]);
        }
    }

    /// <inheritdoc />
    public void Cos(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Cos(src[index]);
        }
    }

    /// <inheritdoc />
    public void Tan(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Tan(src[index]);
        }
    }

    /// <inheritdoc />
    public void Sinh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Sinh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Cosh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Cosh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Tanh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Tanh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Asin(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Asin(src[index]);
        }
    }

    /// <inheritdoc />
    public void Acos(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Acos(src[index]);
        }
    }

    /// <inheritdoc />
    public void Atan(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Atan(src[index]);
        }
    }

    /// <inheritdoc />
    public void Asinh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Asinh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Acosh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Acosh(src[index]);
        }
    }

    /// <inheritdoc />
    public void Atanh(Span<double> src, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
        {
            result[index] = Math.Atanh(src[index]);
        }
    }
}