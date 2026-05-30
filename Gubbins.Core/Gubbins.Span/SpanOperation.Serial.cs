using System.Numerics;
using System.Runtime.CompilerServices;

namespace Gubbins.Span;

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialNumberOperation<T> : ISpanNumberOperation<T>  where T : unmanaged
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
        for (var index = 1; index < src.Length; index++)
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
        for (var index = 1; index < src.Length; index++)
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
public sealed class SerialIntOperation : ISpanShift<int>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate int ShiftOp(int value, int count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ShiftLeftScalar(int value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ShiftRightScalar(int value, int count) => value >> count;

    private static void RunShift(Span<int> src, int count, Span<int> result, ShiftOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i], count);
    }

    /// <inheritdoc/>
    public void ShiftLeft(Span<int> src, int count, Span<int> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc/>
    public void ShiftRight(Span<int> src, int count, Span<int> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialUintOperation : ISpanShift<uint>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate uint ShiftOp(uint value, int count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ShiftLeftScalar(uint value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ShiftRightScalar(uint value, int count) => value >> count;

    private static void RunShift(Span<uint> src, int count, Span<uint> result, ShiftOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i], count);
    }

    /// <inheritdoc/>
    public void ShiftLeft(Span<uint> src, int count, Span<uint> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc/>
    public void ShiftRight(Span<uint> src, int count, Span<uint> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialLongOperation : ISpanShift<long>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate long ShiftOp(long value, int count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ShiftLeftScalar(long value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ShiftRightScalar(long value, int count) => value >> count;

    private static void RunShift(Span<long> src, int count, Span<long> result, ShiftOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i], count);
    }

    /// <inheritdoc/>
    public void ShiftLeft(Span<long> src, int count, Span<long> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc/>
    public void ShiftRight(Span<long> src, int count, Span<long> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialUlongOperation : ISpanShift<ulong>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate ulong ShiftOp(ulong value, int count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftLeftScalar(ulong value, int count) => value << count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ShiftRightScalar(ulong value, int count) => value >> count;

    private static void RunShift(Span<ulong> src, int count, Span<ulong> result, ShiftOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i], count);
    }

    /// <inheritdoc/>
    public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result) => RunShift(src, count, result, ShiftLeftScalar);

    /// <inheritdoc/>
    public void ShiftRight(Span<ulong> src, int count, Span<ulong> result) => RunShift(src, count, result, ShiftRightScalar);
}

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialFloatOperation : ISpanRealOperation<float>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate float UnaryOp(float value);

    private static void RunUnary(Span<float> src, Span<float> result, UnaryOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i]);
    }

    /// <inheritdoc />
    public void Round(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Round);

    /// <inheritdoc />
    public void Exp(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Exp);

    /// <inheritdoc />
    public void Log(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Log);

    /// <inheritdoc />
    public void Truncate(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Truncate);

    /// <inheritdoc />
    public void Floor(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Floor);

    /// <inheritdoc />
    public void Ceiling(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Ceiling);

    /// <inheritdoc/>
    public void Sqrt(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sqrt);

    /// <inheritdoc />
    public void Sin(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sin);

    /// <inheritdoc />
    public void Cos(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Cos);

    /// <inheritdoc />
    public void Tan(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Tan);

    /// <inheritdoc />
    public void Sinh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Sinh);

    /// <inheritdoc />
    public void Cosh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Cosh);

    /// <inheritdoc />
    public void Tanh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Tanh);

    /// <inheritdoc />
    public void Asin(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Asin);

    /// <inheritdoc />
    public void Acos(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Acos);

    /// <inheritdoc />
    public void Atan(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Atan);

    /// <inheritdoc />
    public void Asinh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Asinh);

    /// <inheritdoc />
    public void Acosh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Acosh);

    /// <inheritdoc />
    public void Atanh(Span<float> src, Span<float> result) => RunUnary(src, result, MathF.Atanh);

    /// <inheritdoc/>
    public void Pow(Span<float> src, float exponent, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = MathF.Pow(src[index], exponent);
    }

    /// <inheritdoc />
    public void Pow(Span<float> src, Span<float> exponent, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = MathF.Pow(src[index], exponent[index]);
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = MathF.Min(MathF.Max(src[index], min[index]), max[index]);
    }

    /// <inheritdoc />
    public void Clamp(Span<float> src, float min, float max, Span<float> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = MathF.Min(MathF.Max(src[index], min), max);
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = x[index] * (1 - amount[index]) + y[index] * amount[index];
    }

    /// <inheritdoc />
    public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = x[index] * (1 - amount[index]) + y * amount[index];
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, Span<float> y, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = MathF.Sqrt(x[index] * x[index] + y[index] * y[index]);
    }

    /// <inheritdoc />
    public void Hypot(Span<float> x, float y, Span<float> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = MathF.Sqrt(x[index] * x[index] + y * y);
    }
}

/// <summary>
/// Serial span operation.
/// </summary>
public sealed class SerialDoubleOperation : ISpanRealOperation<double>
{
    /// <inheritdoc />
    public bool Supported => true;

    private delegate double UnaryOp(double value);

    private static void RunUnary(Span<double> src, Span<double> result, UnaryOp op)
    {
        for (var i = 0; i < src.Length; i++)
            result[i] = op(src[i]);
    }

    /// <inheritdoc />
    public void Round(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Round);

    /// <inheritdoc />
    public void Exp(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Exp);

    /// <inheritdoc />
    public void Log(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Log);

    /// <inheritdoc />
    public void Truncate(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Truncate);

    /// <inheritdoc />
    public void Floor(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Floor);

    /// <inheritdoc />
    public void Ceiling(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Ceiling);

    /// <inheritdoc/>
    public void Sqrt(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sqrt);

    /// <inheritdoc />
    public void Sin(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sin);

    /// <inheritdoc />
    public void Cos(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Cos);

    /// <inheritdoc />
    public void Tan(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Tan);

    /// <inheritdoc />
    public void Sinh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Sinh);

    /// <inheritdoc />
    public void Cosh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Cosh);

    /// <inheritdoc />
    public void Tanh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Tanh);

    /// <inheritdoc />
    public void Asin(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Asin);

    /// <inheritdoc />
    public void Acos(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Acos);

    /// <inheritdoc />
    public void Atan(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Atan);

    /// <inheritdoc />
    public void Asinh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Asinh);

    /// <inheritdoc />
    public void Acosh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Acosh);

    /// <inheritdoc />
    public void Atanh(Span<double> src, Span<double> result) => RunUnary(src, result, Math.Atanh);

    /// <inheritdoc/>
    public void Pow(Span<double> src, double exponent, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = Math.Pow(src[index], exponent);
    }

    /// <inheritdoc />
    public void Pow(Span<double> src, Span<double> exponent, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = Math.Pow(src[index], exponent[index]);
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = Math.Min(Math.Max(src[index], min[index]), max[index]);
    }

    /// <inheritdoc />
    public void Clamp(Span<double> src, double min, double max, Span<double> result)
    {
        for (var index = 0; index < src.Length; index++)
            result[index] = Math.Min(Math.Max(src[index], min), max);
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = x[index] * (1 - amount[index]) + y[index] * amount[index];
    }

    /// <inheritdoc />
    public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = x[index] * (1 - amount[index]) + y * amount[index];
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, Span<double> y, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = Math.Sqrt(x[index] * x[index] + y[index] * y[index]);
    }

    /// <inheritdoc />
    public void Hypot(Span<double> x, double y, Span<double> result)
    {
        for (var index = 0; index < x.Length; index++)
            result[index] = Math.Sqrt(x[index] * x[index] + y * y);
    }
}

internal sealed class SerialVector2Operation : ISpanVectorOperation<Vector2>
{
    public bool Supported => true;

    public void Dot(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var dot = Vector2.Dot(left[i], right[i]);
            result[i] = new Vector2(dot, dot);
        }
    }

    public void Cross(Span<Vector2> left, Span<Vector2> right, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var cross = l.X * r.Y - l.Y * r.X;
            result[i] = new Vector2(cross, cross);
        }
    }

    public void Normalize(Span<Vector2> src, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector2.Normalize(src[i]);
        }
    }

    public void Length(Span<Vector2> src, Span<float> result)
    {
        LengthSquared(src, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void LengthSquared(Span<Vector2> src, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void DistanceSquared(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector2.DistanceSquared(left[i], right[i]);
        }
    }

    public void Angle(Span<Vector2> left, Span<Vector2> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            result[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector2.Dot(l, r) / denominator, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector2.Reflect(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector2> src, Span<Vector2> normal, Vector2 eta, Span<Vector2> result)
    {
        var etaRatio = eta.X;
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = RefractScalar(src[i], normal[i], etaRatio);
        }
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<float> incident, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector2> src, Span<Vector2> normal, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector2.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    public void MoveTowards(Span<Vector2> src, Span<Vector2> target, Span<float> maxDistanceDelta, Span<Vector2> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MoveTowardsScalar(src[i], target[i], maxDistanceDelta[i]);
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

internal sealed class SerialVector3Operation : ISpanVectorOperation<Vector3>
{
    public bool Supported => true;

    public void Dot(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var dot = Vector3.Dot(left[i], right[i]);
            result[i] = new Vector3(dot, dot, dot);
        }
    }

    public void Cross(Span<Vector3> left, Span<Vector3> right, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector3.Cross(left[i], right[i]);
        }
    }

    public void Normalize(Span<Vector3> src, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector3.Normalize(src[i]);
        }
    }

    public void Length(Span<Vector3> src, Span<float> result)
    {
        LengthSquared(src, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void LengthSquared(Span<Vector3> src, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void DistanceSquared(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector3.DistanceSquared(left[i], right[i]);
        }
    }

    public void Angle(Span<Vector3> left, Span<Vector3> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            result[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector3.Dot(l, r) / denominator, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector3.Reflect(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector3> src, Span<Vector3> normal, Vector3 eta, Span<Vector3> result)
    {
        var etaRatio = eta.X;
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = RefractScalar(src[i], normal[i], etaRatio);
        }
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<float> incident, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector3> src, Span<Vector3> normal, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector3.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    public void MoveTowards(Span<Vector3> src, Span<Vector3> target, Span<float> maxDistanceDelta, Span<Vector3> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MoveTowardsScalar(src[i], target[i], maxDistanceDelta[i]);
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

internal sealed class SerialVector4Operation : ISpanVectorOperation<Vector4>
{
    public bool Supported => true;

    public void Dot(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var dot = Vector4.Dot(left[i], right[i]);
            result[i] = new Vector4(dot, dot, dot, dot);
        }
    }

    public void Cross(Span<Vector4> left, Span<Vector4> right, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = CrossScalar(left[i], right[i]);
        }
    }

    public void Normalize(Span<Vector4> src, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector4.Normalize(src[i]);
        }
    }

    public void Length(Span<Vector4> src, Span<float> result)
    {
        LengthSquared(src, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void LengthSquared(Span<Vector4> src, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = src[i].LengthSquared();
        }
    }

    public void Distance(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        DistanceSquared(left, right, result);
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MathF.Sqrt(result[i]);
        }
    }

    public void DistanceSquared(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector4.DistanceSquared(left[i], right[i]);
        }
    }

    public void Angle(Span<Vector4> left, Span<Vector4> right, Span<float> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            var l = left[i];
            var r = right[i];
            var denominator = l.Length() * r.Length();
            result[i] = denominator <= 0f ? 0f : MathF.Acos(Math.Clamp(Vector4.Dot(l, r) / denominator, -1f, 1f));
        }
    }

    public void Reflect(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = ReflectScalar(src[i], normal[i]);
        }
    }

    public void Refract(Span<Vector4> src, Span<Vector4> normal, Vector4 eta, Span<Vector4> result)
    {
        var etaRatio = eta.X;
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = RefractScalar(src[i], normal[i], etaRatio);
        }
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<float> incident, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = incident[i] < 0f ? src[i] : -src[i];
        }
    }

    public void FaceForward(Span<Vector4> src, Span<Vector4> normal, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = Vector4.Dot(normal[i], src[i]) < 0f ? src[i] : -src[i];
        }
    }

    public void MoveTowards(Span<Vector4> src, Span<Vector4> target, Span<float> maxDistanceDelta, Span<Vector4> result)
    {
        for (var i = 0; i < result.Length; i++)
        {
            result[i] = MoveTowardsScalar(src[i], target[i], maxDistanceDelta[i]);
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