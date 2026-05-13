using System.Numerics;

namespace Gubbins.Enhance;

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdNumberOperations<T> : ISpanNumberOperations<T>, ISpanGetMax<T>, ISpanGetMin<T> where T : unmanaged
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<T>.Count;

    /// <inheritdoc/>
    public void Add(Span<T> src, T operand, Span<T> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vb = new Vector<T>(operand);
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<T>(current);
            var vr = va + vb;
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < src.Length; i++)
        {
            result[i] = Operations<T>.Add(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Subtract(Span<T> src, T operand, Span<T> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vb = new Vector<T>(operand);
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<T>(current);
            var vr = va - vb;
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < src.Length; i++)
        {
            src[i] = Operations<T>.Subtract(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Multiply(Span<T> src, T operand, Span<T> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vb = new Vector<T>(operand);
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<T>(current);
            var vr = va * vb;
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < src.Length; i++)
        {
            result[i] = Operations<T>.Multiply(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Divide(Span<T> src, T operand, Span<T> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vb = new Vector<T>(operand);
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<T>(current);
            var vr = va / vb;
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < src.Length; i++)
        {
            result[i] = Operations<T>.Divide(src[i], operand);
        }
    }

    /// <inheritdoc/>
    public void Modulo(Span<T> src, T operand, Span<T> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vb = new Vector<T>(operand);
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<T>(current);
            var vr = va - va / vb * vb;
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < src.Length; i++)
        {
            result[i] = Operations<T>.Modulo(src[i], operand);
        }
    }

    /// <inheritdoc />
    public void Max(Span<T> left, Span<T> right, Span<T> result)
    {
        var i = 0;
        var length = left.Length - s_VectorSize;
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Left and right spans must have the same length.");
        }

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(left.Slice(i, s_VectorSize));
            var vb = new Vector<T>(right.Slice(i, s_VectorSize));
            var vr = Vector.Max(va, vb);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            result[i] = Operations<T>.GreaterThan(l, r) ? l : r;
        }
    }

    public T GetMax(Span<T> span)
    {
        if (span.Length == 0)
            throw new ArgumentException("Span must not be empty");


        if (span.Length < Vector<T>.Count)
        {
            return FindMinScalar(span);
        }

        var minVector = new Vector<T>(span);
        var processedCount = Vector<T>.Count;

        while (processedCount <= span.Length - Vector<T>.Count)
        {
            var nextVector = new Vector<T>(span[processedCount..]);
            minVector      =  Vector.Max(minVector, nextVector);
            processedCount += Vector<T>.Count;
        }

        var min = ExtractMinFromVector(minVector);
        for (var i = processedCount; i < span.Length; i++)
        {
            if (Operations<T>.GreaterThan(span[i], min))
                min = span[i];
        }

        return min;

        static T ExtractMinFromVector(Vector<T> vector)
        {
            Span<T> temp = stackalloc T[Vector<T>.Count];
            vector.CopyTo(temp);
            var max = temp[0];
            for (var i = 1; i < temp.Length; i++)
            {
                if (Operations<T>.GreaterThan(temp[i], max))
                {
                    max = temp[i];
                }
            }

            return max;
        }

        static T FindMinScalar(Span<T> span)
        {
            var max = span[0];
            for (var i = 1; i < span.Length; i++)
            {
                if (Operations<T>.GreaterThan(span[i], max))
                {
                    max = span[i];
                }
            }

            return max;
        }
    }

    /// <inheritdoc />
    public void Min(Span<T> left, Span<T> right, Span<T> result)
    {
        var i = 0;
        var length = left.Length - s_VectorSize;
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Left and right spans must have the same length.");
        }

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(left.Slice(i, s_VectorSize));
            var vb = new Vector<T>(right.Slice(i, s_VectorSize));
            var vr = Vector.Min(va, vb);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remaining.
        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            result[i] = Operations<T>.LessThan(l, r) ? l : r;
        }
    }

    public T GetMin(Span<T> span)
    {
        if (span.Length == 0)
            throw new ArgumentException("Span must not be empty");


        if (span.Length < Vector<T>.Count)
        {
            return FindMinScalar(span);
        }


        var minVector = new Vector<T>(span);
        var processedCount = Vector<T>.Count;


        while (processedCount <= span.Length - Vector<T>.Count)
        {
            var nextVector = new Vector<T>(span[processedCount..]);
            minVector      =  Vector.Min(minVector, nextVector);
            processedCount += Vector<T>.Count;
        }

        var min = ExtractMinFromVector(minVector);
        for (var i = processedCount; i < span.Length; i++)
        {
            if (Operations<T>.LessThan(span[i], min))
                min = span[i];
        }

        return min;

        static T ExtractMinFromVector(Vector<T> vector)
        {
            Span<T> temp = stackalloc T[Vector<T>.Count];
            vector.CopyTo(temp);
            var min = temp[0];
            for (var i = 1; i < temp.Length; i++)
            {
                if (Operations<T>.LessThan(temp[i], min))
                {
                    min = temp[i];
                }
            }

            return min;
        }

        static T FindMinScalar(Span<T> span)
        {
            var min = span[0];
            for (var i = 1; i < span.Length; i++)
            {
                if (Operations<T>.LessThan(span[i], min))
                {
                    min = span[i];
                }
            }

            return min;
        }
    }
}

#if NET7_0_OR_GREATER
/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdIntOperation : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<int>.Count;

    /// <inheritdoc/>
    public void ShiftLeft(Span<int> src, int count, Span<int> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<int>(current);
            var vr = Vector.ShiftLeft(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<int> src, int count, Span<int> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<int>(current);
            var vr = Vector.ShiftRightArithmetic(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] >> count;
        }
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdLongOperation : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<int>.Count;

    /// <inheritdoc/>
    public void ShiftLeft(Span<long> src, int count, Span<long> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<long>(current);
            var vr = Vector.ShiftLeft(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<long> src, int count, Span<long> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<long>(current);
            var vr = Vector.ShiftRightArithmetic(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] >> count;
        }
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdUintOperation : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<uint>.Count;

    /// <inheritdoc/>
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<uint>(current);
            var vr = Vector.ShiftLeft(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<uint> src, int count, Span<uint> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<uint>(current);
            var vr = Vector.ShiftRightLogical(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] >> count;
        }
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdUlongOperation : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<uint>.Count;

    /// <inheritdoc/>
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<ulong>(current);
            var vr = Vector.ShiftLeft(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] << count;
        }
    }

    /// <inheritdoc/>
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.

        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<ulong>(current);
            var vr = Vector.ShiftRightLogical(va, count);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = src[i] >> count;
        }
    }
}
#endif

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdFloatOperation : ISpanRealOperations<float>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<float>.Count;

    /// <inheritdoc />
    public void Sqrt(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = Vector.SquareRoot(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Sqrt(src[i]);
        }
    }

    /// <inheritdoc />
    public void Ceiling(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Ceiling(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Ceiling(src[i]);
        }
    }

    /// <inheritdoc />
    public void Floor(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Floor(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Floor(src[i]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<float>(src.Slice(i, s_VectorSize));
            var vmin = new Vector<float>(min.Slice(i, s_VectorSize));
            var vmax = new Vector<float>(max.Slice(i, s_VectorSize));
            var vr = VectorMath.Clamp(va, vmin, vmax);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Min(MathF.Max(src[i], min[i]), max[i]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, float min, float max, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vmin = new Vector<float>(min);
        var vmax = new Vector<float>(max);
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<float>(src.Slice(i, s_VectorSize));
            var vr = VectorMath.Clamp(va, vmin, vmax);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Min(MathF.Max(src[i], min), max);
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(x.Slice(i, s_VectorSize));
            var vy = new Vector<float>(y.Slice(i, s_VectorSize));
            var va = new Vector<float>(amount.Slice(i, s_VectorSize));
            var vr = VectorMath.Lerp(vx, vy, va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            result[i] = x[i] * (1 - amount[i]) + y[i] * amount[i];
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<float>(y);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(x.Slice(i, s_VectorSize));
            var va = new Vector<float>(amount.Slice(i, s_VectorSize));
            var vr = VectorMath.Lerp(vx, vy, va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            result[i] = x[i] * (1 - amount[i]) * amount[i];
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, Span<float> y, Span<float> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(x.Slice(i, s_VectorSize));
            var vy = new Vector<float>(y.Slice(i, s_VectorSize));
            var vr = VectorMath.Hypot(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            var doubleX = x[i] * x[i];
            var doubleY = y[i] * y[i];
            result[i] = MathF.Sqrt(doubleX * doubleY);
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, float y, Span<float> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<float>(y);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(x.Slice(i, s_VectorSize));
            var vr = VectorMath.Hypot(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        var doubleY = y * y;
        for (; i < x.Length; i++)
        {
            var doubleX = x[i] * x[i];
            result[i] = MathF.Sqrt(doubleX * doubleY);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<float> src, float exponent, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<float>(exponent);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(src.Slice(i, s_VectorSize));
            var vr = VectorMath.Pow(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Pow(src[i], exponent);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<float> src, Span<float> exponent, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<float>(src.Slice(i, s_VectorSize));
            var vy = new Vector<float>(exponent.Slice(i, s_VectorSize));
            var vr = VectorMath.Pow(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Pow(src[i], exponent[i]);
        }
    }

    /// <inheritdoc />
    public void Round(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Round(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Round(src[i]);
        }
    }

    /// <inheritdoc />
    public void Exp(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Exp(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Exp(src[i]);
        }
    }

    /// <inheritdoc />
    public void Log(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Log(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Log(src[i]);
        }
    }

    /// <inheritdoc />
    public void Truncate(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Truncate(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Truncate(src[i]);
        }
    }

    /// <inheritdoc />
    public void Sin(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Sin(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Sin(src[i]);
        }
    }

    /// <inheritdoc />
    public void Cos(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Cos(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Cos(src[i]);
        }
    }

    /// <inheritdoc />
    public void Tan(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Tan(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Tan(src[i]);
        }
    }

    /// <inheritdoc />
    public void Sinh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Sinh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Sinh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Cosh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Cosh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Cosh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Tanh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Tanh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Tanh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Asin(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Asin(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Asin(src[i]);
        }
    }

    /// <inheritdoc />
    public void Acos(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Acos(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Acos(src[i]);
        }
    }

    /// <inheritdoc />
    public void Atan(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Atan(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Atan(src[i]);
        }
    }

    /// <inheritdoc />
    public void Asinh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Asinh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Asinh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Acosh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Acosh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Acosh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Atanh(Span<float> src, Span<float> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<float>(current);
            var vr = VectorMath.Atanh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = MathF.Atanh(src[i]);
        }
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdDoubleOperation : ISpanRealOperations<double>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<double>.Count;

    /// <inheritdoc />
    public void Sqrt(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = Vector.SquareRoot(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Sqrt(src[i]);
        }
    }

    /// <inheritdoc />
    public void Ceiling(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Ceiling(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Ceiling(src[i]);
        }
    }

    /// <inheritdoc />
    public void Floor(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Floor(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Floor(src[i]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<double>(src.Slice(i, s_VectorSize));
            var vmin = new Vector<double>(min.Slice(i, s_VectorSize));
            var vmax = new Vector<double>(max.Slice(i, s_VectorSize));
            var vr = VectorMath.Clamp(va, vmin, vmax);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Clamp(src[i], min[i], max[i]);
        }
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, double min, double max, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vmin = new Vector<double>(min);
        var vmax = new Vector<double>(max);
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<double>(src.Slice(i, s_VectorSize));
            var vr = VectorMath.Clamp(va, vmin, vmax);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Clamp(src[i], min, max);
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(x.Slice(i, s_VectorSize));
            var vy = new Vector<double>(y.Slice(i, s_VectorSize));
            var va = new Vector<double>(amount.Slice(i, s_VectorSize));
            var vr = VectorMath.Lerp(vx, vy, va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            result[i] = x[i] * (1 - amount[i]) + y[i] * amount[i];
        }
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<double>(y);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(x.Slice(i, s_VectorSize));
            var va = new Vector<double>(amount.Slice(i, s_VectorSize));
            var vr = VectorMath.Lerp(vx, vy, va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            result[i] = x[i] * (1 - amount[i]) + y * amount[i];
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, Span<double> y, Span<double> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(x.Slice(i, s_VectorSize));
            var vy = new Vector<double>(y.Slice(i, s_VectorSize));
            var vr = VectorMath.Hypot(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < x.Length; i++)
        {
            var doubleX = x[i] * x[i];
            var doubleY = y[i] * y[i];
            result[i] = Math.Sqrt(doubleX * doubleY);
        }
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, double y, Span<double> result)
    {
        var i = 0;
        var length = x.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<double>(y);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(x.Slice(i, s_VectorSize));
            var vr = VectorMath.Hypot(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        var doubleY = y * y;
        for (; i < x.Length; i++)
        {
            var doubleX = x[i] * x[i];
            result[i] = Math.Sqrt(doubleX * doubleY);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<double> src, double exponent, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        var vy = new Vector<double>(exponent);
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(src.Slice(i, s_VectorSize));
            var vr = VectorMath.Pow(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Pow(src[i], exponent);
        }
    }

    /// <inheritdoc />
    public void Pow(Span<double> src, Span<double> exponent, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var vx = new Vector<double>(src.Slice(i, s_VectorSize));
            var vy = new Vector<double>(exponent.Slice(i, s_VectorSize));
            var vr = VectorMath.Pow(vx, vy);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Pow(src[i], exponent[i]);
        }
    }

    /// <inheritdoc />
    public void Round(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Round(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Round(src[i]);
        }
    }

    /// <inheritdoc />
    public void Exp(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Exp(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Exp(src[i]);
        }
    }

    /// <inheritdoc />
    public void Log(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Log(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Log(src[i]);
        }
    }

    /// <inheritdoc />
    public void Truncate(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Truncate(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Truncate(src[i]);
        }
    }

    /// <inheritdoc />
    public void Sin(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Sin(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Sin(src[i]);
        }
    }

    /// <inheritdoc />
    public void Cos(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Cos(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Cos(src[i]);
        }
    }

    /// <inheritdoc />
    public void Tan(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Tan(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Tan(src[i]);
        }
    }

    /// <inheritdoc />
    public void Sinh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Sinh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Sinh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Cosh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Cosh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Cosh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Tanh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Tanh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Tanh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Asin(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Asin(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Asin(src[i]);
        }
    }

    /// <inheritdoc />
    public void Acos(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Acos(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Acos(src[i]);
        }
    }

    /// <inheritdoc />
    public void Atan(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Atan(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Atan(src[i]);
        }
    }

    /// <inheritdoc />
    public void Asinh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Asinh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Asinh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Acosh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Acosh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Acosh(src[i]);
        }
    }

    /// <inheritdoc />
    public void Atanh(Span<double> src, Span<double> result)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;

        // SIMD calculation.
        for (; i <= length; i += s_VectorSize)
        {
            var current = src.Slice(i, s_VectorSize);
            var va = new Vector<double>(current);
            var vr = VectorMath.Atanh(va);
            vr.CopyTo(result.Slice(i, s_VectorSize));
        }

        // Calculate remain.
        for (; i < src.Length; i++)
        {
            result[i] = Math.Atanh(src[i]);
        }
    }
}