namespace Gubbins.Enhance;

public interface ISpanOperation
{
    /// <summary>
    /// Whether the span operation supported.
    /// </summary>
    bool Supported { get; }
}

#region Number

public interface ISpanEquals<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply equals all operation to the span.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    bool EqualsAll(ReadOnlySpan<T> left, ReadOnlySpan<T> right);

    /// <summary>
    /// Apply equals any operation to the span.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    bool EqualsAny(ReadOnlySpan<T> left, ReadOnlySpan<T> right);
}

public interface ISpanAddition<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply addition operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="operand">Object of the operation.</param>
    /// <param name="result">Result.</param>
    void Add(Span<T> src, T operand, Span<T> result);
}

public interface ISpanSubtraction<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply subtraction operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="operand">Object of the operation.</param>
    /// <param name="result">Result.</param>
    void Subtract(Span<T> src, T operand, Span<T> result);
}

public interface ISpanMultiply<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply multiply operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="operand">Object of the operation.</param>
    /// <param name="result">Result.</param>
    void Multiply(Span<T> src, T operand, Span<T> result);
}

public interface ISpanDivision<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply division operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="operand">Object of the operation.</param>
    /// <param name="result">Result.</param>
    void Divide(Span<T> src, T operand, Span<T> result);
}

public interface ISpanModulus<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply modulo operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="operand">Object of the operation.</param>
    /// <param name="result">Result.</param>
    void Modulo(Span<T> src, T operand, Span<T> result);
}

public interface ISpanSqrt<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply square root operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Sqrt(Span<T> src, Span<T> result);
}

public interface ISpanShiftLeft<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply shift left operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="count">Shift length.</param>
    /// <param name="result">Result.</param>
    void ShiftLeft(Span<T> src, int count, Span<T> result);
}

public interface ISpanShiftRight<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply shift right operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="count">Shift length.</param>
    /// <param name="result">Result.</param>
    void ShiftRight(Span<T> src, int count, Span<T> result);
}

public interface ISpanMax<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply max operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void Max(Span<T> left, Span<T> right, Span<T> result);
}

public interface ISpanMin<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply min operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void Min(Span<T> left, Span<T> right, Span<T> result);
}

public interface ISpanNumberOperations<T> : ISpanAddition<T>, ISpanSubtraction<T>, ISpanMultiply<T>, ISpanDivision<T>, ISpanModulus<T>, ISpanMax<T>, ISpanMin<T> where T : struct;

#endregion

# region Real

public interface ISpanFloor<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply floor operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Floor(Span<T> src, Span<T> result);
}

public interface ISpanCeiling<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply ceiling operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Ceiling(Span<T> src, Span<T> result);
}

public interface ISpanClamp<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply Clamp operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <param name="result">Result.</param>
    void Clamp(Span<T> src, Span<T> min, Span<T> max, Span<T> result);

    /// <summary>
    /// Apply Clamp operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="min">Min value.</param>
    /// <param name="max">Max value.</param>
    /// <param name="result">Result.</param>
    void Clamp(Span<T> src, T min, T max, Span<T> result);
}

public interface ISpanLerp<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply Lerp operation to the span.
    /// </summary>
    /// <param name="x">Span x.</param>
    /// <param name="y">Span y.</param>
    /// <param name="amount">A value between 0 and 1 that indicates the weight of <c>y</c>.</param>
    /// <param name="result">Result.</param>
    void Lerp(Span<T> x, Span<T> y, Span<T> amount, Span<T> result);

    /// <summary>
    /// Apply Lerp operation to the span.
    /// </summary>
    /// <param name="x">Span x.</param>
    /// <param name="y">Span y.</param>
    /// <param name="amount">A value between 0 and 1 that indicates the weight of <c>y</c>.</param>
    /// <param name="result">Result.</param>
    void Lerp(Span<T> x, T y, Span<T> amount, Span<T> result);
}

public interface ISpanHypot<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply Hypot operation to the span.
    /// </summary>
    /// <param name="x">Span x.</param>
    /// <param name="y">Span y.</param>
    /// <param name="result">Result.</param>
    void Hypot(Span<T> x, Span<T> y, Span<T> result);

    /// <summary>
    /// Apply Hypot operation to the span.
    /// </summary>
    /// <param name="x">Span x.</param>
    /// <param name="y">Span y.</param>
    /// <param name="result">Result.</param>
    void Hypot(Span<T> x, T y, Span<T> result);
}

public interface ISpanExp<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply exp operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Exp(Span<T> src, Span<T> result);
}

public interface ISpanLog<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply log operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Log(Span<T> src, Span<T> result);
}

public interface ISpanRound<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply round operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Round(Span<T> src, Span<T> result);
}

public interface ISpanTruncate<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply truncate operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Truncate(Span<T> src, Span<T> result);
}

public interface ISpanPow<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply pow operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="exponent">Exponent.</param>
    /// <param name="result">Result.</param>
    void Pow(Span<T> src, T exponent, Span<T> result);

    /// <summary>
    /// Apply pow operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="exponent">Exponent.</param>
    /// <param name="result">Result.</param>
    void Pow(Span<T> src, Span<T> exponent, Span<T> result);
}

public interface ISpanTrigonometric<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply sin operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Sin(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply cos operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Cos(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply tan operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Tan(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply sinh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Sinh(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply cosh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Cosh(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply tanh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Tanh(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply asin operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Asin(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply acos operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Acos(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply atan operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Atan(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply asinh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Asinh(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply acosh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Acosh(Span<T> src, Span<T> result);

    /// <summary>
    /// Apply atanh operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Atanh(Span<T> src, Span<T> result);
}

public interface ISpanRealOperations<T> : ISpanSqrt<T>, ISpanClamp<T>, ISpanRound<T>, ISpanLerp<T>, ISpanHypot<T>, ISpanExp<T>, ISpanLog<T>, ISpanPow<T>, ISpanFloor<T>, ISpanCeiling<T>, ISpanTruncate<T>, ISpanTrigonometric<T> where T : struct;

#endregion

#region Vector

public interface ISpanDot<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply dot operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    /// <returns>Result.</returns>
    void Dot(Span<T> left, Span<T> right, Span<T> result);
}

public interface ISpanCross<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply cross operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void Cross(Span<T> left, Span<T> right, Span<T> result);
}

public interface ISpanNormalize<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply normalize operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Normalize(Span<T> src, Span<T> result);
}

public interface ISpanLength<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply length operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void Length(Span<T> src, Span<float> result);

    /// <summary>
    /// Apply length squared operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="result">Result.</param>
    void LengthSquared(Span<T> src, Span<float> result);
}

public interface ISpanDistance<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply distance operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void Distance(Span<T> left, Span<T> right, Span<float> result);

    /// <summary>
    /// Apply distance squared operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void DistanceSquared(Span<T> left, Span<T> right, Span<float> result);
}

public interface ISpanAngle<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply angle operation to the span.
    /// </summary>
    /// <param name="left">Span that has the same length with <c>right</c>.</param>
    /// <param name="right">Span that has the same length with <c>left</c>.</param>
    /// <param name="result">Result.</param>
    void Angle(Span<T> left, Span<T> right, Span<float> result);
}

public interface ISpanReflect<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply reflect operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="normal">Normal.</param>
    /// <param name="result">Result.</param>
    void Reflect(Span<T> src, Span<T> normal, Span<T> result);
}

public interface ISpanRefract<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply refract operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="normal">Normal.</param>
    /// <param name="eta">Eta.</param>
    /// <param name="result">Result.</param>
    void Refract(Span<T> src, Span<T> normal, T eta, Span<T> result);
}

public interface ISpanFaceForward<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply face forward operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="normal">Normal.</param>
    /// <param name="incident">Incident.</param>
    /// <param name="result">Result.</param>
    void FaceForward(Span<T> src, Span<T> normal, Span<float> incident, Span<T> result);

    /// <summary>
    /// Apply face forward operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="normal">Normal.</param>
    /// <param name="result">Result.</param>
    void FaceForward(Span<T> src, Span<T> normal, Span<T> result);
}

public interface ISpanMoveTowards<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Apply move towards operation to the span.
    /// </summary>
    /// <param name="src">Span.</param>
    /// <param name="target">Target.</param>
    /// <param name="maxDistanceDelta">Max distance delta.</param>
    /// <param name="result">Result.</param>
    void MoveTowards(Span<T> src, Span<T> target, Span<float> maxDistanceDelta, Span<T> result);
}

public interface ISpanVectorOperations<T> : ISpanDot<T>, ISpanCross<T>, ISpanNormalize<T>, ISpanLength<T>, ISpanDistance<T>, ISpanAngle<T>, ISpanReflect<T>, ISpanRefract<T>, ISpanFaceForward<T>, ISpanMoveTowards<T> where T : struct;

#endregion

#region Other

public interface ISpanGetMax<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Get a max element from the span.
    /// </summary>
    /// <param name="src">Span.</param>
    T GetMax(Span<T> src);
}

public interface ISpanGetMin<T> : ISpanOperation where T : struct
{
    /// <summary>
    /// Get a min element from the span.
    /// </summary>
    /// <param name="src">Span.</param>
    T GetMin(Span<T> src);
}

#endregion