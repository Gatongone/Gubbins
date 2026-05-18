using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gubbins.Span;

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdNumberOperations<T> : ISpanNumberOperations<T>, ISpanGetMax<T>, ISpanGetMin<T> where T : unmanaged
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    private static readonly int s_VectorSize = Vector<T>.Count;

    private delegate Vector<T> VectorOperandOp(Vector<T> left, Vector<T> right);

    private delegate T ScalarOperandOp(T left, T right);

    private delegate Vector<T> VectorPairOp(Vector<T> left, Vector<T> right);

    private delegate T ScalarPairOp(T left, T right);

    private static void RunOperand(Span<T> src, T operand, Span<T> result, VectorOperandOp vectorOp, ScalarOperandOp scalarOp)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;
        var vb = new Vector<T>(operand);

        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(src.Slice(i, s_VectorSize));
            vectorOp(va, vb).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = scalarOp(src[i], operand);
        }
    }

    private static void RunPair(Span<T> left, Span<T> right, Span<T> result, VectorPairOp vectorOp, ScalarPairOp scalarOp)
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Left and right spans must have the same length.");
        }

        var i = 0;
        var length = left.Length - s_VectorSize;
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(left.Slice(i, s_VectorSize));
            var vb = new Vector<T>(right.Slice(i, s_VectorSize));
            vectorOp(va, vb).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = scalarOp(left[i], right[i]);
        }
    }

    /// <inheritdoc/>
    public void Add(Span<T> src, T operand, Span<T> result) =>
        RunOperand(src, operand, result, static (va, vb) => va + vb, Operations<T>.Add);

    /// <inheritdoc/>
    public void Subtract(Span<T> src, T operand, Span<T> result) =>
        RunOperand(src, operand, result, static (va, vb) => va - vb, Operations<T>.Subtract);

    /// <inheritdoc/>
    public void Multiply(Span<T> src, T operand, Span<T> result) =>
        RunOperand(src, operand, result, static (va, vb) => va * vb, Operations<T>.Multiply);

    /// <inheritdoc/>
    public void Divide(Span<T> src, T operand, Span<T> result) =>
        RunOperand(src, operand, result, static (va, vb) => va / vb, Operations<T>.Divide);

    /// <inheritdoc/>
    public void Modulo(Span<T> src, T operand, Span<T> result) =>
        RunOperand(src, operand, result, static (va, vb) => va - va / vb * vb, Operations<T>.Modulo);

    /// <inheritdoc />
    public void Max(Span<T> left, Span<T> right, Span<T> result) =>
        RunPair(left, right, result, Vector.Max, static (l, r) => Operations<T>.GreaterThan(l, r) ? l : r);

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
    public void Min(Span<T> left, Span<T> right, Span<T> result) =>
        RunPair(left, right, result, Vector.Min, static (l, r) => Operations<T>.LessThan(l, r) ? l : r);

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
internal static class SimdShiftRunner<T> where T : unmanaged
{
    public delegate Vector<T> VectorShiftOp(Vector<T> value, int count);

    public delegate T ScalarShiftOp(T value, int count);

    private static readonly int s_VectorSize = Vector<T>.Count;

    public static void Run(Span<T> src, int count, Span<T> result, VectorShiftOp vectorOp, ScalarShiftOp scalarOp)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(src.Slice(i, s_VectorSize));
            vectorOp(va, count).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = scalarOp(src[i], count);
        }
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdIntOperation : ISpanShiftLeft<int>, ISpanShiftRight<int>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    public void ShiftLeft(Span<int> src, int count, Span<int> result) =>
        SimdShiftRunner<int>.Run(src, count, result, Vector.ShiftLeft, static (v, c) => v << c);

    public void ShiftRight(Span<int> src, int count, Span<int> result) =>
        SimdShiftRunner<int>.Run(src, count, result, Vector.ShiftRightArithmetic, static (v, c) => v >> c);
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdLongOperation : ISpanShiftLeft<long>, ISpanShiftRight<long>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    public void ShiftLeft(Span<long> src, int count, Span<long> result) =>
        SimdShiftRunner<long>.Run(src, count, result, Vector.ShiftLeft, static (v, c) => v << c);

    public void ShiftRight(Span<long> src, int count, Span<long> result) =>
        SimdShiftRunner<long>.Run(src, count, result, Vector.ShiftRightArithmetic, static (v, c) => v >> c);
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdUintOperation : ISpanShiftLeft<uint>, ISpanShiftRight<uint>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    public void ShiftLeft(Span<uint> src, int count, Span<uint> result) =>
        SimdShiftRunner<uint>.Run(src, count, result, Vector.ShiftLeft, static (v, c) => v << c);

    public void ShiftRight(Span<uint> src, int count, Span<uint> result) =>
        SimdShiftRunner<uint>.Run(src, count, result, Vector.ShiftRightLogical, static (v, c) => v >> c);
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdUlongOperation : ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
{
    /// <inheritdoc/>
    public bool Supported => Vector.IsHardwareAccelerated;

    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result) =>
        SimdShiftRunner<ulong>.Run(src, count, result, Vector.ShiftLeft, static (v, c) => v << c);

    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result) =>
        SimdShiftRunner<ulong>.Run(src, count, result, Vector.ShiftRightLogical, static (v, c) => v >> c);
}
#endif

internal static class SimdRealRunner<T> where T : unmanaged
{
    public delegate Vector<T> UnaryVectorOp(Vector<T> value);

    public delegate T UnaryScalarOp(T value);

    public delegate Vector<T> BinaryVectorOp(Vector<T> left, Vector<T> right);

    public delegate T BinaryScalarOp(T left, T right);

    public delegate Vector<T> TernaryVectorOp(Vector<T> a, Vector<T> b, Vector<T> c);

    public delegate T TernaryScalarOp(T a, T b, T c);

    private static readonly int s_VectorSize = Vector<T>.Count;

    public static void RunUnary(Span<T> src, Span<T> result, UnaryVectorOp vectorOp, UnaryScalarOp scalarOp)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(src.Slice(i, s_VectorSize));
            vectorOp(va).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = scalarOp(src[i]);
        }
    }

    public static void RunBinary(Span<T> left, Span<T> right, Span<T> result, BinaryVectorOp vectorOp, BinaryScalarOp scalarOp)
    {
        var i = 0;
        var length = left.Length - s_VectorSize;
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(left.Slice(i, s_VectorSize));
            var vb = new Vector<T>(right.Slice(i, s_VectorSize));
            vectorOp(va, vb).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = scalarOp(left[i], right[i]);
        }
    }

    public static void RunBinaryRightScalar(Span<T> left, T right, Span<T> result, BinaryVectorOp vectorOp, BinaryScalarOp scalarOp)
    {
        var i = 0;
        var length = left.Length - s_VectorSize;
        var vb = new Vector<T>(right);
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(left.Slice(i, s_VectorSize));
            vectorOp(va, vb).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = scalarOp(left[i], right);
        }
    }

    public static void RunTernary(Span<T> a, Span<T> b, Span<T> c, Span<T> result, TernaryVectorOp vectorOp, TernaryScalarOp scalarOp)
    {
        var i = 0;
        var length = a.Length - s_VectorSize;
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(a.Slice(i, s_VectorSize));
            var vb = new Vector<T>(b.Slice(i, s_VectorSize));
            var vc = new Vector<T>(c.Slice(i, s_VectorSize));
            vectorOp(va, vb, vc).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < a.Length; i++)
        {
            result[i] = scalarOp(a[i], b[i], c[i]);
        }
    }

    public static void RunTernaryMiddleScalar(Span<T> a, T b, Span<T> c, Span<T> result, TernaryVectorOp vectorOp, TernaryScalarOp scalarOp)
    {
        var i = 0;
        var length = a.Length - s_VectorSize;
        var vb = new Vector<T>(b);
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(a.Slice(i, s_VectorSize));
            var vc = new Vector<T>(c.Slice(i, s_VectorSize));
            vectorOp(va, vb, vc).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < a.Length; i++)
        {
            result[i] = scalarOp(a[i], b, c[i]);
        }
    }

    public static void RunUnaryWithTwoScalars(Span<T> src, T b, T c, Span<T> result, TernaryVectorOp vectorOp, TernaryScalarOp scalarOp)
    {
        var i = 0;
        var length = src.Length - s_VectorSize;
        var vb = new Vector<T>(b);
        var vc = new Vector<T>(c);
        for (; i <= length; i += s_VectorSize)
        {
            var va = new Vector<T>(src.Slice(i, s_VectorSize));
            vectorOp(va, vb, vc).CopyTo(result.Slice(i, s_VectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = scalarOp(src[i], b, c);
        }
    }
}

internal static class SimdFloatRunner
{
    private static readonly int s_VectorSize = Vector<float>.Count;

    public static void ApplySqrt(Span<float> values, int count)
    {
        var i = 0;
        var simdLength = count - count % s_VectorSize;
        for (; i < simdLength; i += s_VectorSize)
        {
            Vector.SquareRoot(new Vector<float>(values.Slice(i, s_VectorSize))).CopyTo(values.Slice(i, s_VectorSize));
        }

        for (; i < count; i++)
        {
            values[i] = MathF.Sqrt(values[i]);
        }
    }
}

internal static class SimdPackedFloatRunner
{
    public static void SumGroups(ReadOnlySpan<float> source, Span<float> destination, int groupSize, int groupCount)
    {
        for (var group = 0; group < groupCount; group++)
        {
            var offset = group * groupSize;
            var sum = 0f;
            for (var lane = 0; lane < groupSize; lane++)
            {
                sum += source[offset + lane];
            }

            destination[group] = sum;
        }
    }

    public static void ExpandGroups(ReadOnlySpan<float> source, Span<float> destination, int groupSize, int groupCount)
    {
        for (var group = 0; group < groupCount; group++)
        {
            var value = source[group];
            var offset = group * groupSize;
            for (var lane = 0; lane < groupSize; lane++)
            {
                destination[offset + lane] = value;
            }
        }
    }

    public static Vector<int> BuildNegativeMask(ReadOnlySpan<float> values, int groupSize, int groupCount)
    {
        Span<int> expanded = stackalloc int[Vector<int>.Count];
        for (var group = 0; group < groupCount; group++)
        {
            var maskValue = values[group] < 0f ? -1 : 0;
            var offset = group * groupSize;
            for (var lane = 0; lane < groupSize; lane++)
            {
                expanded[offset + lane] = maskValue;
            }
        }

        return new Vector<int>(expanded);
    }
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdFloatOperation : ISpanRealOperations<float>
{
    public bool Supported => Vector.IsHardwareAccelerated;

    public void Sqrt(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, Vector.SquareRoot, MathF.Sqrt);

    public void Ceiling(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Ceiling, MathF.Ceiling);

    public void Floor(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Floor, MathF.Floor);

    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result) =>
        SimdRealRunner<float>.RunTernary(src, min, max, result, VectorMath.Clamp, static (v, mn, mx) => MathF.Min(MathF.Max(v, mn), mx));

    public void Clamp(Span<float> src, float min, float max, Span<float> result) =>
        SimdRealRunner<float>.RunUnaryWithTwoScalars(src, min, max, result, VectorMath.Clamp, static (v, mn, mx) => MathF.Min(MathF.Max(v, mn), mx));

    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result) =>
        SimdRealRunner<float>.RunTernary(x, y, amount, result, VectorMath.Lerp, static (a, b, t) => a * (1 - t) + b * t);

    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result) =>
        SimdRealRunner<float>.RunTernaryMiddleScalar(x, y, amount, result, VectorMath.Lerp, static (a, b, t) => a * (1 - t) + b * t);

    public void Hypot(Span<float> x, Span<float> y, Span<float> result) =>
        SimdRealRunner<float>.RunBinary(x, y, result, VectorMath.Hypot, static (a, b) => MathF.Sqrt(a * a * b * b));

    public void Hypot(Span<float> x, float y, Span<float> result) =>
        SimdRealRunner<float>.RunBinaryRightScalar(x, y, result, VectorMath.Hypot, static (a, b) => MathF.Sqrt(a * a * b * b));

    public void Pow(Span<float> src, float exponent, Span<float> result) =>
        SimdRealRunner<float>.RunBinaryRightScalar(src, exponent, result, VectorMath.Pow, MathF.Pow);

    public void Pow(Span<float> src, Span<float> exponent, Span<float> result) =>
        SimdRealRunner<float>.RunBinary(src, exponent, result, VectorMath.Pow, MathF.Pow);

    public void Round(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Round, MathF.Round);

    public void Exp(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Exp, MathF.Exp);

    public void Log(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Log, MathF.Log);

    public void Truncate(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Truncate, MathF.Truncate);

    public void Sin(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Sin, MathF.Sin);

    public void Cos(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Cos, MathF.Cos);

    public void Tan(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Tan, MathF.Tan);

    public void Sinh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Sinh, MathF.Sinh);

    public void Cosh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Cosh, MathF.Cosh);

    public void Tanh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Tanh, MathF.Tanh);

    public void Asin(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Asin, MathF.Asin);

    public void Acos(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Acos, MathF.Acos);

    public void Atan(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Atan, MathF.Atan);

    public void Asinh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Asinh, MathF.Asinh);

    public void Acosh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Acosh, MathF.Acosh);

    public void Atanh(Span<float> src, Span<float> result) =>
        SimdRealRunner<float>.RunUnary(src, result, VectorMath.Atanh, MathF.Atanh);
}

/// <summary>
/// SIMD span operation.
/// </summary>
internal sealed class SimdDoubleOperation : ISpanRealOperations<double>
{
    public bool Supported => Vector.IsHardwareAccelerated;

    public void Sqrt(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, Vector.SquareRoot, Math.Sqrt);

    public void Ceiling(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Ceiling, Math.Ceiling);

    public void Floor(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Floor, Math.Floor);

    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result) =>
        SimdRealRunner<double>.RunTernary(src, min, max, result, VectorMath.Clamp, Math.Clamp);

    public void Clamp(Span<double> src, double min, double max, Span<double> result) =>
        SimdRealRunner<double>.RunUnaryWithTwoScalars(src, min, max, result, VectorMath.Clamp, Math.Clamp);

    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result) =>
        SimdRealRunner<double>.RunTernary(x, y, amount, result, VectorMath.Lerp, static (a, b, t) => a * (1 - t) + b * t);

    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result) =>
        SimdRealRunner<double>.RunTernaryMiddleScalar(x, y, amount, result, VectorMath.Lerp, static (a, b, t) => a * (1 - t) + b * t);

    public void Hypot(Span<double> x, Span<double> y, Span<double> result) =>
        SimdRealRunner<double>.RunBinary(x, y, result, VectorMath.Hypot, static (a, b) => Math.Sqrt(a * a * b * b));

    public void Hypot(Span<double> x, double y, Span<double> result) =>
        SimdRealRunner<double>.RunBinaryRightScalar(x, y, result, VectorMath.Hypot, static (a, b) => Math.Sqrt(a * a * b * b));

    public void Pow(Span<double> src, double exponent, Span<double> result) =>
        SimdRealRunner<double>.RunBinaryRightScalar(src, exponent, result, VectorMath.Pow, Math.Pow);

    public void Pow(Span<double> src, Span<double> exponent, Span<double> result) =>
        SimdRealRunner<double>.RunBinary(src, exponent, result, VectorMath.Pow, Math.Pow);

    public void Round(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Round, Math.Round);

    public void Exp(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Exp, Math.Exp);

    public void Log(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Log, Math.Log);

    public void Truncate(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Truncate, Math.Truncate);

    public void Sin(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Sin, Math.Sin);

    public void Cos(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Cos, Math.Cos);

    public void Tan(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Tan, Math.Tan);

    public void Sinh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Sinh, Math.Sinh);

    public void Cosh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Cosh, Math.Cosh);

    public void Tanh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Tanh, Math.Tanh);

    public void Asin(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Asin, Math.Asin);

    public void Acos(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Acos, Math.Acos);

    public void Atan(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Atan, Math.Atan);

    public void Asinh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Asinh, Math.Asinh);

    public void Acosh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Acosh, Math.Acosh);

    public void Atanh(Span<double> src, Span<double> result) =>
        SimdRealRunner<double>.RunUnary(src, result, VectorMath.Atanh, Math.Atanh);
}

internal sealed class SimdVectorOperation : ISpanVectorOperations<Vector2>
{
    private static readonly int s_FloatVectorSize  = Vector<float>.Count;
    private static readonly int s_Vector2BatchSize = Vector<float>.Count / 2;

    public bool Supported => Vector.IsHardwareAccelerated;

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector2, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector2, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector2BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dotPairs = stackalloc float[s_Vector2BatchSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var va = new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize));
            var vb = new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize));
            (va * vb).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dotPairs, 2, s_Vector2BatchSize);
            WriteReplicatedScalars(dotPairs, result, i, s_Vector2BatchSize);
        }

        for (; i < left.Length; i++)
        {
            var dot = Vector2.Dot(left[i], right[i]);
            result[i] = new Vector2(dot, dot);
        }
    }

    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector2, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector2, float>(right);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector2BatchSize;
        Span<float> leftBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> rightBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> crossPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> crossExpanded = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize)).CopyTo(leftBuffer);
            new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize)).CopyTo(rightBuffer);

            for (var pair = 0; pair < s_Vector2BatchSize; pair++)
            {
                var lane = pair * 2;
                crossPairs[pair] = leftBuffer[lane] * rightBuffer[lane + 1] - leftBuffer[lane + 1] * rightBuffer[lane];
            }

            SimdPackedFloatRunner.ExpandGroups(crossPairs, crossExpanded, 2, s_Vector2BatchSize);
            new Vector<float>(crossExpanded).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var cross = l.X * r.Y - l.Y * r.X;
            result[i] = new Vector2(cross, cross);
        }
    }

    public void Normalize(Span<Vector2> src, Span<Vector2> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> lengths = stackalloc float[s_Vector2BatchSize];
        Span<float> expandedLengths = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var va = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            (va * va).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, lengths, 2, s_Vector2BatchSize);

            for (var pair = 0; pair < s_Vector2BatchSize; pair++)
            {
                lengths[pair] = MathF.Sqrt(lengths[pair]);
            }

            SimdPackedFloatRunner.ExpandGroups(lengths, expandedLengths, 2, s_Vector2BatchSize);
            (va / new Vector<float>(expandedLengths)).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector2.Normalize(src[i]);
        }
    }

    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector2, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector2, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector2BatchSize;
        Span<float> pairBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dotPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> leftLengths = stackalloc float[s_Vector2BatchSize];
        Span<float> rightLengths = stackalloc float[s_Vector2BatchSize];
        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var vl = new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize));
            var vr = new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize));

            (vl * vr).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, dotPairs, 2, s_Vector2BatchSize);

            (vl * vl).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, leftLengths, 2, s_Vector2BatchSize);

            (vr * vr).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, rightLengths, 2, s_Vector2BatchSize);

            for (var pair = 0; pair < s_Vector2BatchSize; pair++)
            {
                var denominator = MathF.Sqrt(leftLengths[pair]) * MathF.Sqrt(rightLengths[pair]);
                result[i + pair] = denominator <= 0f
                    ? 0f
                    : MathF.Acos(Math.Clamp(dotPairs[pair] / denominator, -1f, 1f));
            }
        }

        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            if (denominator <= 0f)
            {
                result[i] = 0f;
                continue;
            }

            var dot = Vector2.Dot(l, r) / denominator;
            result[i] = MathF.Acos(Math.Clamp(dot, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector2, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dotPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> expandedDots = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vn = new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize));
            (vs * vn).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dotPairs, 2, s_Vector2BatchSize);
            SimdPackedFloatRunner.ExpandGroups(dotPairs, expandedDots, 2, s_Vector2BatchSize);
            (vs - vn * (new Vector<float>(expandedDots) + new Vector<float>(expandedDots))).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector2.Reflect(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result)
    {
        // Use eta.X as scalar ratio because Vector2 refraction is scalar-based.
        var etaRatio = eta.X;
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector2, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var etaVector = new Vector<float>(etaRatio);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dotPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> kPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> factorPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> expandedK = stackalloc float[s_FloatVectorSize];
        Span<float> expandedFactors = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vn = new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize));
            (vs * vn).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dotPairs, 2, s_Vector2BatchSize);

            for (var pair = 0; pair < s_Vector2BatchSize; pair++)
            {
                var dot = dotPairs[pair];
                var k = 1f - etaRatio * etaRatio * (1f - dot * dot);
                kPairs[pair]      = k;
                factorPairs[pair] = etaRatio * dot + (k < 0f ? 0f : MathF.Sqrt(k));
            }

            SimdPackedFloatRunner.ExpandGroups(kPairs, expandedK, 2, s_Vector2BatchSize);
            SimdPackedFloatRunner.ExpandGroups(factorPairs, expandedFactors, 2, s_Vector2BatchSize);

            var refracted = etaVector * vs - vn * new Vector<float>(expandedFactors);
            var zeroMask = Vector.LessThan(new Vector<float>(expandedK), Vector<float>.Zero);
            Vector.ConditionalSelect(zeroMask, Vector<float>.Zero, refracted).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            var incident = src[i];
            var n = normal[i];
            var dot = Vector2.Dot(n, incident);
            var k = 1f - etaRatio * etaRatio * (1f - dot * dot);
            result[i] = k < 0f
                ? Vector2.Zero
                : etaRatio * incident - (etaRatio * dot + MathF.Sqrt(k)) * n;
        }
    }

    public void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var targetValues = MemoryMarshal.Cast<Vector2, float>(target);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> distances = stackalloc float[s_Vector2BatchSize];
        Span<float> expandedDistances = stackalloc float[s_FloatVectorSize];
        Span<float> expandedMaxDelta = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vt = new Vector<float>(targetValues.Slice(offset, s_FloatVectorSize));
            var vd = vt - vs;
            (vd * vd).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, distances, 2, s_Vector2BatchSize);

            for (var pair = 0; pair < s_Vector2BatchSize; pair++)
            {
                distances[pair] = MathF.Sqrt(distances[pair]);
            }

            SimdPackedFloatRunner.ExpandGroups(distances, expandedDistances, 2, s_Vector2BatchSize);
            SimdPackedFloatRunner.ExpandGroups(maxDistanceDelta.Slice(i, s_Vector2BatchSize), expandedMaxDelta, 2, s_Vector2BatchSize);

            var vDistance = new Vector<float>(expandedDistances);
            var vMaxDelta = new Vector<float>(expandedMaxDelta);
            var moved = vs + vd / vDistance * vMaxDelta;
            var useTargetMask = Vector.LessThanOrEqual(vDistance, vMaxDelta) | Vector.Equals(vDistance, Vector<float>.Zero);
            Vector.ConditionalSelect(useTargetMask, vt, moved).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            var current = src[i];
            var toTarget = target[i] - current;
            var distance = toTarget.Length();
            var maxDelta = maxDistanceDelta[i];

            if (distance <= maxDelta || distance == 0f)
            {
                result[i] = target[i];
                continue;
            }

            result[i] = current + toTarget / distance * maxDelta;
        }
    }

    public void Length(Span<Vector2> src, Span<float> result)
    {
        LengthSquared(src, result);

        SimdFloatRunner.ApplySqrt(result, src.Length);
    }

    public void LengthSquared(Span<Vector2> src, Span<float> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> sqrBuffer = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var va = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            (va * va).CopyTo(sqrBuffer);

            SimdPackedFloatRunner.SumGroups(sqrBuffer, result.Slice(i, s_Vector2BatchSize), 2, s_Vector2BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        SimdFloatRunner.ApplySqrt(result, left.Length);
    }

    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector2, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector2, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector2BatchSize;
        Span<float> sqrBuffer = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var va = new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize));
            var vb = new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize));
            var vd = va - vb;
            (vd * vd).CopyTo(sqrBuffer);

            SimdPackedFloatRunner.SumGroups(sqrBuffer, result.Slice(i, s_Vector2BatchSize), 2, s_Vector2BatchSize);
        }

        for (; i < left.Length; i++)
        {
            result[i] = Vector2.DistanceSquared(left[i], right[i]);
        }
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> expandedIncident = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            SimdPackedFloatRunner.ExpandGroups(incident.Slice(i, s_Vector2BatchSize), expandedIncident, 2, s_Vector2BatchSize);
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var mask = Vector.LessThan(new Vector<float>(expandedIncident), Vector<float>.Zero);
            Vector.ConditionalSelect(mask, vs, -vs).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector2, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector2, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector2, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector2BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dotPairs = stackalloc float[s_Vector2BatchSize];
        Span<float> expandedDots = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector2BatchSize)
        {
            var offset = i * 2;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vn = new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize));
            (vs * vn).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dotPairs, 2, s_Vector2BatchSize);
            SimdPackedFloatRunner.ExpandGroups(dotPairs, expandedDots, 2, s_Vector2BatchSize);
            var mask = Vector.LessThan(new Vector<float>(expandedDots), Vector<float>.Zero);
            Vector.ConditionalSelect(mask, vs, -vs).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector2.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    private static void WriteReplicatedScalars(ReadOnlySpan<float> source, Span<Vector2> destination, int startIndex, int count)
    {
        for (var pair = 0; pair < count; pair++)
        {
            var value = source[pair];
            destination[startIndex + pair] = new Vector2(value, value);
        }
    }
}

internal sealed class SimdVector3Operation : ISpanVectorOperations<Vector3>
{
    private static readonly int s_BatchSize = Vector<float>.Count;

    public bool Supported => Vector.IsHardwareAccelerated;

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = left.Length - left.Length % s_BatchSize;
        Span<float> lx = stackalloc float[s_BatchSize];
        Span<float> ly = stackalloc float[s_BatchSize];
        Span<float> lz = stackalloc float[s_BatchSize];
        Span<float> rx = stackalloc float[s_BatchSize];
        Span<float> ry = stackalloc float[s_BatchSize];
        Span<float> rz = stackalloc float[s_BatchSize];
        Span<float> dots = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(left, i, lx, ly, lz, s_BatchSize);
            Gather(right, i, rx, ry, rz, s_BatchSize);
            (new Vector<float>(lx) * new Vector<float>(rx)
                + new Vector<float>(ly) * new Vector<float>(ry)
                + new Vector<float>(lz) * new Vector<float>(rz)).CopyTo(dots);
            ScatterReplicated(result, i, dots, s_BatchSize);
        }

        for (; i < left.Length; i++)
        {
            var dot = Vector3.Dot(left[i], right[i]);
            result[i] = new Vector3(dot, dot, dot);
        }
    }

    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = left.Length - left.Length % s_BatchSize;
        Span<float> lx = stackalloc float[s_BatchSize];
        Span<float> ly = stackalloc float[s_BatchSize];
        Span<float> lz = stackalloc float[s_BatchSize];
        Span<float> rx = stackalloc float[s_BatchSize];
        Span<float> ry = stackalloc float[s_BatchSize];
        Span<float> rz = stackalloc float[s_BatchSize];
        Span<float> x = stackalloc float[s_BatchSize];
        Span<float> y = stackalloc float[s_BatchSize];
        Span<float> z = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(left, i, lx, ly, lz, s_BatchSize);
            Gather(right, i, rx, ry, rz, s_BatchSize);

            (new Vector<float>(ly) * new Vector<float>(rz) - new Vector<float>(lz) * new Vector<float>(ry)).CopyTo(x);
            (new Vector<float>(lz) * new Vector<float>(rx) - new Vector<float>(lx) * new Vector<float>(rz)).CopyTo(y);
            (new Vector<float>(lx) * new Vector<float>(ry) - new Vector<float>(ly) * new Vector<float>(rx)).CopyTo(z);
            Scatter(result, i, x, y, z, s_BatchSize);
        }

        for (; i < left.Length; i++)
        {
            result[i] = Vector3.Cross(left[i], right[i]);
        }
    }

    public void Normalize(Span<Vector3> src, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> x = stackalloc float[s_BatchSize];
        Span<float> y = stackalloc float[s_BatchSize];
        Span<float> z = stackalloc float[s_BatchSize];
        Span<float> lengths = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, x, y, z, s_BatchSize);

            var vx = new Vector<float>(x);
            var vy = new Vector<float>(y);
            var vz = new Vector<float>(z);
            Vector.SquareRoot(vx * vx + vy * vy + vz * vz).CopyTo(lengths);

            (vx / new Vector<float>(lengths)).CopyTo(x);
            (vy / new Vector<float>(lengths)).CopyTo(y);
            (vz / new Vector<float>(lengths)).CopyTo(z);
            Scatter(result, i, x, y, z, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector3.Normalize(src[i]);
        }
    }

    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        var i = 0;
        var simdLength = left.Length - left.Length % s_BatchSize;
        Span<float> lx = stackalloc float[s_BatchSize];
        Span<float> ly = stackalloc float[s_BatchSize];
        Span<float> lz = stackalloc float[s_BatchSize];
        Span<float> rx = stackalloc float[s_BatchSize];
        Span<float> ry = stackalloc float[s_BatchSize];
        Span<float> rz = stackalloc float[s_BatchSize];
        Span<float> dots = stackalloc float[s_BatchSize];
        Span<float> leftLengths = stackalloc float[s_BatchSize];
        Span<float> rightLengths = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(left, i, lx, ly, lz, s_BatchSize);
            Gather(right, i, rx, ry, rz, s_BatchSize);

            var vlx = new Vector<float>(lx);
            var vly = new Vector<float>(ly);
            var vlz = new Vector<float>(lz);
            var vrx = new Vector<float>(rx);
            var vry = new Vector<float>(ry);
            var vrz = new Vector<float>(rz);

            (vlx * vrx + vly * vry + vlz * vrz).CopyTo(dots);
            Vector.SquareRoot(vlx * vlx + vly * vly + vlz * vlz).CopyTo(leftLengths);
            Vector.SquareRoot(vrx * vrx + vry * vry + vrz * vrz).CopyTo(rightLengths);

            for (var lane = 0; lane < s_BatchSize; lane++)
            {
                var denominator = leftLengths[lane] * rightLengths[lane];
                result[i + lane] = denominator <= 0f
                    ? 0f
                    : MathF.Acos(Math.Clamp(dots[lane] / denominator, -1f, 1f));
            }
        }

        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            result[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(l, r) / denominator, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> sx = stackalloc float[s_BatchSize];
        Span<float> sy = stackalloc float[s_BatchSize];
        Span<float> sz = stackalloc float[s_BatchSize];
        Span<float> nx = stackalloc float[s_BatchSize];
        Span<float> ny = stackalloc float[s_BatchSize];
        Span<float> nz = stackalloc float[s_BatchSize];
        Span<float> dots = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, sx, sy, sz, s_BatchSize);
            Gather(normal, i, nx, ny, nz, s_BatchSize);

            var vsx = new Vector<float>(sx);
            var vsy = new Vector<float>(sy);
            var vsz = new Vector<float>(sz);
            var vnx = new Vector<float>(nx);
            var vny = new Vector<float>(ny);
            var vnz = new Vector<float>(nz);
            ((vsx * vnx + vsy * vny + vsz * vnz) + (vsx * vnx + vsy * vny + vsz * vnz)).CopyTo(dots);

            (vsx - vnx * new Vector<float>(dots)).CopyTo(sx);
            (vsy - vny * new Vector<float>(dots)).CopyTo(sy);
            (vsz - vnz * new Vector<float>(dots)).CopyTo(sz);
            Scatter(result, i, sx, sy, sz, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector3.Reflect(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result)
    {
        var etaRatio = eta.X;
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> sx = stackalloc float[s_BatchSize];
        Span<float> sy = stackalloc float[s_BatchSize];
        Span<float> sz = stackalloc float[s_BatchSize];
        Span<float> nx = stackalloc float[s_BatchSize];
        Span<float> ny = stackalloc float[s_BatchSize];
        Span<float> nz = stackalloc float[s_BatchSize];
        Span<float> dots = stackalloc float[s_BatchSize];
        Span<float> ks = stackalloc float[s_BatchSize];
        Span<float> factors = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, sx, sy, sz, s_BatchSize);
            Gather(normal, i, nx, ny, nz, s_BatchSize);

            var vsx = new Vector<float>(sx);
            var vsy = new Vector<float>(sy);
            var vsz = new Vector<float>(sz);
            var vnx = new Vector<float>(nx);
            var vny = new Vector<float>(ny);
            var vnz = new Vector<float>(nz);
            (vsx * vnx + vsy * vny + vsz * vnz).CopyTo(dots);

            for (var lane = 0; lane < s_BatchSize; lane++)
            {
                var dot = dots[lane];
                var k = 1f - etaRatio * etaRatio * (1f - dot * dot);
                ks[lane]      = k;
                factors[lane] = etaRatio * dot + (k < 0f ? 0f : MathF.Sqrt(k));
            }

            var vf = new Vector<float>(factors);
            (new Vector<float>(etaRatio) * vsx - vnx * vf).CopyTo(sx);
            (new Vector<float>(etaRatio) * vsy - vny * vf).CopyTo(sy);
            (new Vector<float>(etaRatio) * vsz - vnz * vf).CopyTo(sz);

            for (var lane = 0; lane < s_BatchSize; lane++)
            {
                if (ks[lane] < 0f)
                {
                    sx[lane] = 0f;
                    sy[lane] = 0f;
                    sz[lane] = 0f;
                }
            }

            Scatter(result, i, sx, sy, sz, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = RefractScalar(src[i], normal[i], etaRatio);
        }
    }

    public void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> sx = stackalloc float[s_BatchSize];
        Span<float> sy = stackalloc float[s_BatchSize];
        Span<float> sz = stackalloc float[s_BatchSize];
        Span<float> tx = stackalloc float[s_BatchSize];
        Span<float> ty = stackalloc float[s_BatchSize];
        Span<float> tz = stackalloc float[s_BatchSize];
        Span<float> distances = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, sx, sy, sz, s_BatchSize);
            Gather(target, i, tx, ty, tz, s_BatchSize);

            var vsx = new Vector<float>(sx);
            var vsy = new Vector<float>(sy);
            var vsz = new Vector<float>(sz);
            var vtx = new Vector<float>(tx);
            var vty = new Vector<float>(ty);
            var vtz = new Vector<float>(tz);
            var vdx = vtx - vsx;
            var vdy = vty - vsy;
            var vdz = vtz - vsz;
            var vMaxDelta = new Vector<float>(maxDistanceDelta.Slice(i, s_BatchSize));
            var vDistance = Vector.SquareRoot(vdx * vdx + vdy * vdy + vdz * vdz);
            vDistance.CopyTo(distances);

            var movedX = vsx + vdx / vDistance * vMaxDelta;
            var movedY = vsy + vdy / vDistance * vMaxDelta;
            var movedZ = vsz + vdz / vDistance * vMaxDelta;
            var useTargetMask = Vector.LessThanOrEqual(vDistance, vMaxDelta) | Vector.Equals(vDistance, Vector<float>.Zero);

            Vector.ConditionalSelect(useTargetMask, vtx, movedX).CopyTo(sx);
            Vector.ConditionalSelect(useTargetMask, vty, movedY).CopyTo(sy);
            Vector.ConditionalSelect(useTargetMask, vtz, movedZ).CopyTo(sz);
            Scatter(result, i, sx, sy, sz, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = MoveTowardsScalar(src[i], target[i], maxDistanceDelta[i]);
        }
    }

    public void Length(Span<Vector3> src, Span<float> result)
    {
        LengthSquared(src, result);

        SimdFloatRunner.ApplySqrt(result, src.Length);
    }

    public void LengthSquared(Span<Vector3> src, Span<float> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> x = stackalloc float[s_BatchSize];
        Span<float> y = stackalloc float[s_BatchSize];
        Span<float> z = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, x, y, z, s_BatchSize);
            (new Vector<float>(x) * new Vector<float>(x)
                + new Vector<float>(y) * new Vector<float>(y)
                + new Vector<float>(z) * new Vector<float>(z)).CopyTo(result.Slice(i, s_BatchSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        SimdFloatRunner.ApplySqrt(result, left.Length);
    }

    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        var i = 0;
        var simdLength = left.Length - left.Length % s_BatchSize;
        Span<float> lx = stackalloc float[s_BatchSize];
        Span<float> ly = stackalloc float[s_BatchSize];
        Span<float> lz = stackalloc float[s_BatchSize];
        Span<float> rx = stackalloc float[s_BatchSize];
        Span<float> ry = stackalloc float[s_BatchSize];
        Span<float> rz = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(left, i, lx, ly, lz, s_BatchSize);
            Gather(right, i, rx, ry, rz, s_BatchSize);

            var dx = new Vector<float>(lx) - new Vector<float>(rx);
            var dy = new Vector<float>(ly) - new Vector<float>(ry);
            var dz = new Vector<float>(lz) - new Vector<float>(rz);
            (dx * dx + dy * dy + dz * dz).CopyTo(result.Slice(i, s_BatchSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = Vector3.DistanceSquared(left[i], right[i]);
        }
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> x = stackalloc float[s_BatchSize];
        Span<float> y = stackalloc float[s_BatchSize];
        Span<float> z = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, x, y, z, s_BatchSize);
            var incidentVector = new Vector<float>(incident.Slice(i, s_BatchSize));
            var mask = Vector.LessThan(incidentVector, Vector<float>.Zero);
            Vector.ConditionalSelect(mask, new Vector<float>(x), -new Vector<float>(x)).CopyTo(x);
            Vector.ConditionalSelect(mask, new Vector<float>(y), -new Vector<float>(y)).CopyTo(y);
            Vector.ConditionalSelect(mask, new Vector<float>(z), -new Vector<float>(z)).CopyTo(z);
            Scatter(result, i, x, y, z, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        var i = 0;
        var simdLength = src.Length - src.Length % s_BatchSize;
        Span<float> sx = stackalloc float[s_BatchSize];
        Span<float> sy = stackalloc float[s_BatchSize];
        Span<float> sz = stackalloc float[s_BatchSize];
        Span<float> nx = stackalloc float[s_BatchSize];
        Span<float> ny = stackalloc float[s_BatchSize];
        Span<float> nz = stackalloc float[s_BatchSize];
        Span<float> dots = stackalloc float[s_BatchSize];

        for (; i < simdLength; i += s_BatchSize)
        {
            Gather(src, i, sx, sy, sz, s_BatchSize);
            Gather(normal, i, nx, ny, nz, s_BatchSize);
            (new Vector<float>(sx) * new Vector<float>(nx)
                + new Vector<float>(sy) * new Vector<float>(ny)
                + new Vector<float>(sz) * new Vector<float>(nz)).CopyTo(dots);

            var mask = Vector.LessThan(new Vector<float>(dots), Vector<float>.Zero);
            Vector.ConditionalSelect(mask, new Vector<float>(sx), -new Vector<float>(sx)).CopyTo(sx);
            Vector.ConditionalSelect(mask, new Vector<float>(sy), -new Vector<float>(sy)).CopyTo(sy);
            Vector.ConditionalSelect(mask, new Vector<float>(sz), -new Vector<float>(sz)).CopyTo(sz);
            Scatter(result, i, sx, sy, sz, s_BatchSize);
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector3.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    private static void Gather(ReadOnlySpan<Vector3> source, int start, Span<float> x, Span<float> y, Span<float> z, int count)
    {
        for (var lane = 0; lane < count; lane++)
        {
            var value = source[start + lane];
            x[lane] = value.X;
            y[lane] = value.Y;
            z[lane] = value.Z;
        }
    }

    private static void Scatter(Span<Vector3> destination, int start, ReadOnlySpan<float> x, ReadOnlySpan<float> y, ReadOnlySpan<float> z, int count)
    {
        for (var lane = 0; lane < count; lane++)
        {
            destination[start + lane] = new Vector3(x[lane], y[lane], z[lane]);
        }
    }

    private static void ScatterReplicated(Span<Vector3> destination, int start, ReadOnlySpan<float> values, int count)
    {
        for (var lane = 0; lane < count; lane++)
        {
            var value = values[lane];
            destination[start + lane] = new Vector3(value, value, value);
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

internal sealed class SimdVector4Operation : ISpanVectorOperations<Vector4>
{
    private static readonly int s_FloatVectorSize  = Vector<float>.Count;
    private static readonly int s_Vector4BatchSize = Vector<float>.Count / 4;

    public bool Supported => Vector.IsHardwareAccelerated;

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector4, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector4, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector4BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dots = stackalloc float[s_Vector4BatchSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            (new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize))
                * new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize))).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dots, 4, s_Vector4BatchSize);
            WriteReplicatedScalars(dots, result, i, s_Vector4BatchSize);
        }

        for (; i < left.Length; i++)
        {
            var dot = Vector4.Dot(left[i], right[i]);
            result[i] = new Vector4(dot, dot, dot, dot);
        }
    }

    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector4, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector4, float>(right);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector4BatchSize;
        Span<float> leftBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> rightBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> crossBuffer = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize)).CopyTo(leftBuffer);
            new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize)).CopyTo(rightBuffer);

            for (var lane = 0; lane < s_Vector4BatchSize; lane++)
            {
                var quad = lane * 4;
                var cross = CrossScalar(
                    new Vector4(leftBuffer[quad], leftBuffer[quad + 1], leftBuffer[quad + 2], leftBuffer[quad + 3]),
                    new Vector4(rightBuffer[quad], rightBuffer[quad + 1], rightBuffer[quad + 2], rightBuffer[quad + 3]));
                crossBuffer[quad]     = cross.X;
                crossBuffer[quad + 1] = cross.Y;
                crossBuffer[quad + 2] = cross.Z;
                crossBuffer[quad + 3] = cross.W;
            }

            new Vector<float>(crossBuffer).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = CrossScalar(left[i], right[i]);
        }
    }

    public void Normalize(Span<Vector4> src, Span<Vector4> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> lengths = stackalloc float[s_Vector4BatchSize];
        Span<float> expandedLengths = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var va = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            (va * va).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, lengths, 4, s_Vector4BatchSize);

            for (var lane = 0; lane < s_Vector4BatchSize; lane++)
            {
                lengths[lane] = MathF.Sqrt(lengths[lane]);
            }

            SimdPackedFloatRunner.ExpandGroups(lengths, expandedLengths, 4, s_Vector4BatchSize);
            (va / new Vector<float>(expandedLengths)).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector4.Normalize(src[i]);
        }
    }

    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector4, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector4, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector4BatchSize;
        Span<float> pairBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dots = stackalloc float[s_Vector4BatchSize];
        Span<float> leftLengths = stackalloc float[s_Vector4BatchSize];
        Span<float> rightLengths = stackalloc float[s_Vector4BatchSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var vl = new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize));
            var vr = new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize));
            (vl * vr).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, dots, 4, s_Vector4BatchSize);
            (vl * vl).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, leftLengths, 4, s_Vector4BatchSize);
            (vr * vr).CopyTo(pairBuffer);
            SimdPackedFloatRunner.SumGroups(pairBuffer, rightLengths, 4, s_Vector4BatchSize);

            for (var lane = 0; lane < s_Vector4BatchSize; lane++)
            {
                var denominator = MathF.Sqrt(leftLengths[lane]) * MathF.Sqrt(rightLengths[lane]);
                result[i + lane] = denominator <= 0f
                    ? 0f
                    : MathF.Acos(Math.Clamp(dots[lane] / denominator, -1f, 1f));
            }
        }

        for (; i < left.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            result[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(l, r) / denominator, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector4, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dots = stackalloc float[s_Vector4BatchSize];
        Span<float> expandedDots = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vn = new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize));
            (vs * vn).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dots, 4, s_Vector4BatchSize);
            for (var lane = 0; lane < s_Vector4BatchSize; lane++) dots[lane] += dots[lane];
            SimdPackedFloatRunner.ExpandGroups(dots, expandedDots, 4, s_Vector4BatchSize);
            (vs - vn * new Vector<float>(expandedDots)).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = ReflectScalar(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result)
    {
        var etaRatio = eta.X;
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector4, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dots = stackalloc float[s_Vector4BatchSize];
        Span<float> ks = stackalloc float[s_Vector4BatchSize];
        Span<float> factors = stackalloc float[s_Vector4BatchSize];
        Span<float> expandedFactors = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vn = new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize));
            (vs * vn).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dots, 4, s_Vector4BatchSize);

            for (var lane = 0; lane < s_Vector4BatchSize; lane++)
            {
                var dot = dots[lane];
                var k = 1f - etaRatio * etaRatio * (1f - dot * dot);
                ks[lane]      = k;
                factors[lane] = etaRatio * dot + (k < 0f ? 0f : MathF.Sqrt(k));
            }

            SimdPackedFloatRunner.ExpandGroups(factors, expandedFactors, 4, s_Vector4BatchSize);
            var refracted = new Vector<float>(etaRatio) * vs - vn * new Vector<float>(expandedFactors);
            var mask = SimdPackedFloatRunner.BuildNegativeMask(ks, 4, s_Vector4BatchSize);
            Vector.ConditionalSelect(mask, Vector<float>.Zero, refracted).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = RefractScalar(src[i], normal[i], etaRatio);
        }
    }

    public void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var targetValues = MemoryMarshal.Cast<Vector4, float>(target);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> distances = stackalloc float[s_Vector4BatchSize];
        Span<float> expandedDistances = stackalloc float[s_FloatVectorSize];
        Span<float> expandedMaxDelta = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var vt = new Vector<float>(targetValues.Slice(offset, s_FloatVectorSize));
            var vd = vt - vs;
            (vd * vd).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, distances, 4, s_Vector4BatchSize);
            for (var lane = 0; lane < s_Vector4BatchSize; lane++) distances[lane] = MathF.Sqrt(distances[lane]);
            SimdPackedFloatRunner.ExpandGroups(distances, expandedDistances, 4, s_Vector4BatchSize);
            SimdPackedFloatRunner.ExpandGroups(maxDistanceDelta.Slice(i, s_Vector4BatchSize), expandedMaxDelta, 4, s_Vector4BatchSize);

            var vDistance = new Vector<float>(expandedDistances);
            var vMaxDelta = new Vector<float>(expandedMaxDelta);
            var moved = vs + vd / vDistance * vMaxDelta;
            var useTargetMask = Vector.LessThanOrEqual(vDistance, vMaxDelta) | Vector.Equals(vDistance, Vector<float>.Zero);
            Vector.ConditionalSelect(useTargetMask, vt, moved).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = MoveTowardsScalar(src[i], target[i], maxDistanceDelta[i]);
        }
    }

    public void Length(Span<Vector4> src, Span<float> result)
    {
        LengthSquared(src, result);

        SimdFloatRunner.ApplySqrt(result, src.Length);
    }

    public void LengthSquared(Span<Vector4> src, Span<float> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> lengths = stackalloc float[s_Vector4BatchSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var va = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            (va * va).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, lengths, 4, s_Vector4BatchSize);
            lengths.CopyTo(result.Slice(i, s_Vector4BatchSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        DistanceSquared(left, right, result);

        SimdFloatRunner.ApplySqrt(result, left.Length);
    }

    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        var leftValues = MemoryMarshal.Cast<Vector4, float>(left);
        var rightValues = MemoryMarshal.Cast<Vector4, float>(right);
        var i = 0;
        var simdLength = left.Length - left.Length % s_Vector4BatchSize;
        Span<float> squaredBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> lengths = stackalloc float[s_Vector4BatchSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            var vd = new Vector<float>(leftValues.Slice(offset, s_FloatVectorSize)) - new Vector<float>(rightValues.Slice(offset, s_FloatVectorSize));
            (vd * vd).CopyTo(squaredBuffer);
            SimdPackedFloatRunner.SumGroups(squaredBuffer, lengths, 4, s_Vector4BatchSize);
            lengths.CopyTo(result.Slice(i, s_Vector4BatchSize));
        }

        for (; i < left.Length; i++)
        {
            result[i] = Vector4.DistanceSquared(left[i], right[i]);
        }
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> expandedIncident = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            SimdPackedFloatRunner.ExpandGroups(incident.Slice(i, s_Vector4BatchSize), expandedIncident, 4, s_Vector4BatchSize);
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            var mask = Vector.LessThan(new Vector<float>(expandedIncident), Vector<float>.Zero);
            Vector.ConditionalSelect(mask, vs, -vs).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        var srcValues = MemoryMarshal.Cast<Vector4, float>(src);
        var normalValues = MemoryMarshal.Cast<Vector4, float>(normal);
        var resultValues = MemoryMarshal.Cast<Vector4, float>(result);
        var i = 0;
        var simdLength = src.Length - src.Length % s_Vector4BatchSize;
        Span<float> productBuffer = stackalloc float[s_FloatVectorSize];
        Span<float> dots = stackalloc float[s_Vector4BatchSize];
        Span<float> expandedDots = stackalloc float[s_FloatVectorSize];

        for (; i < simdLength; i += s_Vector4BatchSize)
        {
            var offset = i * 4;
            (new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize)) * new Vector<float>(normalValues.Slice(offset, s_FloatVectorSize))).CopyTo(productBuffer);
            SimdPackedFloatRunner.SumGroups(productBuffer, dots, 4, s_Vector4BatchSize);
            SimdPackedFloatRunner.ExpandGroups(dots, expandedDots, 4, s_Vector4BatchSize);
            var mask = Vector.LessThan(new Vector<float>(expandedDots), Vector<float>.Zero);
            var vs = new Vector<float>(srcValues.Slice(offset, s_FloatVectorSize));
            Vector.ConditionalSelect(mask, vs, -vs).CopyTo(resultValues.Slice(offset, s_FloatVectorSize));
        }

        for (; i < src.Length; i++)
        {
            result[i] = Vector4.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    private static void WriteReplicatedScalars(ReadOnlySpan<float> source, Span<Vector4> destination, int startIndex, int count)
    {
        for (var lane = 0; lane < count; lane++)
        {
            var value = source[lane];
            destination[startIndex + lane] = new Vector4(value, value, value, value);
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