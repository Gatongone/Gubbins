using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;

namespace Gubbins.Span;

#if !NETCOREAPP3_0_OR_GREATER && !NET5_0_OR_GREATER
/// <summary>
/// Extensions for the <see cref="System.Numerics.Vector"/>.
/// </summary>
internal static class VectorExtensions
{
    /// <summary>
    /// Non-calculation const values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private static class Constant<T> where T : struct
    {
        public static readonly long Length = Vector<T>.Count * Native.GetStackSize<T>();
    }

    /// <summary>
    /// Copies the vector to the given <see cref="T:System.Span`1" />.
    /// </summary>
    /// <param name="vector">Vector.</param>
    /// <param name="destination">The destination span to which the values are copied. The destination span must be At least size <see cref="P:System.Numerics.Vector`1.Count" />.</param>
    internal static unsafe void CopyTo<T>(this Vector<T> vector, Span<T> destination) where T : unmanaged
    {
        var length = Constant<T>.Length;
        fixed (void* dest = destination)
        {
            void* src = &vector;
            Native.CopyMemory(src, dest, (uint) length);
        }
    }
}
#endif


internal static class VectorMath
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Vector<float> Log(Vector<float> x)
    {
#if NET9_0_OR_GREATER
        return Vector.Log(x);
#else
        // This code is based on `vrs4_expf` from amd/aocl-libm-ose
        // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes:
        // 1. Argument Reduction:
        //      e^x = 2^(x/ln2)                          --- (1)
        //
        //      Let x/ln(2) = z                          --- (2)
        //
        //      Let z = n + r , where n is an integer    --- (3)
        //                      |r| <= 1/2
        //
        //     From (1), (2) and (3),
        //      e^x = 2^z
        //          = 2^(N+r)
        //          = (2^N)*(2^r)                        --- (4)
        //
        // 2. Polynomial Evaluation
        //   From (4),
        //     r   = z - N
        //     2^r = C1 + C2*r + C3*r^2 + C4*r^3 + C5 *r^4 + C6*r^5
        //
        // 4. Reconstruction
        //      Thus,
        //        e^x = (2^N) * (2^r)
        const uint vMin = 0x00800000;
        const uint vMax = 0x7F800000;
        const uint vMask = 0x007FFFFF;
        const uint vOff = 0x3F2AAAAB;

        const float vLn2 = 0.6931472f;

        const float c02 = -0.5000001f;
        const float c03 = +0.33332965f;
        const float c04 = -0.24999046f;
        const float c05 = +0.20018855f;
        const float c06 = -0.16700386f;
        const float c07 = +0.13902695f;
        const float c08 = -0.1197452f;
        const float c09 = +0.14401625f;
        const float c10 = -0.13657966f;

        // x is zero, subnormal, infinity, or NaN
        var specialMask = Vector.GreaterThanOrEqual(Native.BitCast<Vector<float>, Vector<uint>>(x) - new Vector<uint>(vMin), new Vector<uint>(vMax - vMin));
        var specialResult = x;

        if (specialMask != Vector<uint>.Zero)
        {
            var isNegtiveMask = Vector.AsVectorSingle(Vector.LessThan(Vector.AsVectorInt32(x), Vector<int>.Zero));
            var nanMask = new Vector<float>(float.NaN);
            var zeroMask = Vector.AsVectorSingle(Vector.Equals(x, Vector<float>.Zero));

            // float.IsNegative(x) ? float.NaN : x
            specialResult = Vector.ConditionalSelect(isNegtiveMask, nanMask, specialResult);

            // float.IsZero(x) ? float.NegativeInfinity : x
            specialResult = Vector.ConditionalSelect(zeroMask, new Vector<float>(float.NegativeInfinity), specialResult);

            // float.IsZero(x) | float.IsNegative(x) | float.IsNaN(x) | float.IsPositiveInfinity(x)
            var temp = zeroMask | isNegtiveMask | Vector.AsVectorSingle(Vector.Equals(x, nanMask) | Vector.Equals(x, new Vector<float>(float.PositiveInfinity)));

            // Subnormal
            var subnormalMask = Vector.AndNot(Native.BitCast<Vector<uint>, Vector<float>>(specialMask), temp);

            x = Vector.ConditionalSelect(
                subnormalMask,
                Native.BitCast<Vector<uint>, Vector<float>>(Native.BitCast<Vector<float>, Vector<uint>>(x * 8388608.0f) - new Vector<uint>(23u << 23)),
                x);
            specialMask = Native.BitCast<Vector<float>, Vector<uint>>(temp);
        }

        // Reduce the mantissa to [+2/3, +4/3]
        var vx = Native.BitCast<Vector<float>, Vector<uint>>(x) - new Vector<uint>(vOff);
#if NET7_0_OR_GREATER
        var n = Vector.ConvertToSingle(Native.ShiftRightArithmetic(Native.BitCast<Vector<ulong>, Vector<long>>(vx), 23));
#else
        var longN = Native.BitCast<Vector<uint>, Vector<int>>(vx);
        Span<int> spanN = stackalloc int[Vector<int>.Count];
        for (var i = 0; i < Vector<long>.Count; i++)
        {
            spanN[i] = longN[i] >> 23;
        }

        var n = Vector.ConvertToSingle(new Vector<int>(spanN));
#endif
        vx = (vx & new Vector<uint>(vMask)) + new Vector<uint>(vOff);

        // Adjust the mantissa to [-1/3, +1/3]
        var r = Native.BitCast<Vector<uint>, Vector<float>>(vx) - Vector<float>.One;

        var r02 = r * r;
        var r04 = r02 * r02;
        var r08 = r04 * r04;

        // Compute log(x + 1) using Polynomial approximation
        var poly = (r02
                * new Vector<float>(c10)
                + (new Vector<float>(c09) * r + new Vector<float>(c08)))
            * r08
            + (((new Vector<float>(c07) * r + new Vector<float>(c06))
                    * r02
                    + (new Vector<float>(c05) * r + new Vector<float>(c04)))
                * r04
                + (new Vector<float>(c03) * r04 + new Vector<float>(c02))
                * r02
                + r);
        return Vector.ConditionalSelect(
            Native.BitCast<Vector<uint>, Vector<float>>(specialMask),
            specialResult,
            n * new Vector<float>(vLn2) + poly);

#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Log(Vector<double> x)
    {
#if NET9_0_OR_GREATER
        return Vector.Log(x);
#else
        // This code is based on `vrd2_log` from amd/aocl-libm-ose
        // Copyright (C) 2018-2020 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Reduce x into the form:
        //        x = (-1)^s*2^n*m
        // s will be always zero, as log is defined for positive numbers
        // n is an integer known as the exponent
        // m is mantissa
        //
        // x is reduced such that the mantissa, m lies in [2/3,4/3]
        //      x = 2^n*m where m is in [2/3,4/3]
        //      log(x) = log(2^n*m)                 We have log(a*b) = log(a)+log(b)
        //             = log(2^n) + log(m)          We have log(a^n) = n*log(a)
        //             = n*log(2) + log(m)
        //             = n*log(2) + log(1+(m-1))
        //             = n*log(2) + log(1+f)        Where f = m-1
        //             = n*log(2) + log1p(f)        f lies in [-1/3,+1/3]
        //
        // Thus we have :
        // log(x) = n*log(2) + log1p(f)
        // In the above, the first term n*log(2), n can be calculated by using right shift operator and the value of log(2)
        // is known and is stored as a constant
        // The second term log1p(F) is approximated by using a polynomial

        const ulong vMin = 0x0010_0000_0000_0000;
        const ulong vMax = 0x7FF0_0000_0000_0000;
        const ulong vMsk = 0x000FFFFF_FFFFFFFF; // (1 << 52) - 1
        const ulong vOff = 0x3FE55555_55555555; // 2.0 / 3.0

        const double ln2Head = 0.693359375;
        const double ln2Tail = -0.00021219444005469057;

        const double c02 = -0.499999999999999560;
        const double c03 = +0.333333333333414750;
        const double c04 = -0.250000000000297430;
        const double c05 = +0.199999999975985220;
        const double c06 = -0.166666666608919500;
        const double c07 = +0.142857145600277100;
        const double c08 = -0.125000005127831270;
        const double c09 = +0.111110952357159440;
        const double c10 = -0.099999750495501240;
        const double c11 = +0.090914349823462390;
        const double c12 = -0.083340600527551860;
        const double c13 = +0.076817603328311300;
        const double c14 = -0.071296718946287310;
        const double c15 = +0.067963465211535730;
        const double c16 = -0.063995035098960040;
        const double c17 = +0.049370587082412105;
        const double c18 = -0.045370170994891980;
        const double c19 = +0.088970636003577750;
        const double c20 = -0.086906174116908760;

        // x is zero, subnormal, infinity, or NaN
        var specialMask = Vector.GreaterThanOrEqual(Native.BitCast<Vector<double>, Vector<ulong>>(x) - new Vector<ulong>(vMin), new Vector<ulong>(vMax - vMin));
        var specialResult = x;

        if (specialMask != Vector<ulong>.Zero)
        {
            var isNegtiveMask = Vector.AsVectorDouble(Vector.LessThan(Vector.AsVectorInt64(x), Vector<long>.Zero));
            var nanMask = new Vector<double>(double.NaN);
            var zeroMask = Vector.AsVectorDouble(Vector.Equals(x, Vector<double>.Zero));

            // double.IsNegative(x) ? double.NaN : x
            specialResult = Vector.ConditionalSelect(isNegtiveMask, nanMask, specialResult);

            // double.IsZero(x) ? double.NegativeInfinity : x
            specialResult = Vector.ConditionalSelect(zeroMask, new Vector<double>(double.NegativeInfinity), specialResult);

            // double.IsZero(x) | double.IsNegative(x) | double.IsNaN(x) | double.IsPositiveInfinity(x)
            var temp = zeroMask | isNegtiveMask | Vector.AsVectorDouble(Vector.Equals(x, nanMask) | Vector.Equals(x, new Vector<double>(double.PositiveInfinity)));

            // Subnormal
            var subnormalMask = Vector.AndNot(Native.BitCast<Vector<ulong>, Vector<double>>(specialMask), temp);

            // Multiply by 2^52, then normalize
            x = Vector.ConditionalSelect(
                subnormalMask,
                Native.BitCast<Vector<ulong>, Vector<double>>(Native.BitCast<Vector<double>, Vector<ulong>>(x * 4503599627370496.0) - new Vector<ulong>(52ul << 52)),
                x);
            specialMask = Native.BitCast<Vector<double>, Vector<ulong>>(temp);
        }

        // Reduce the mantissa to [+2/3, +4/3]
        var vx = Native.BitCast<Vector<double>, Vector<ulong>>(x) - new Vector<ulong>(vOff);
#if NET7_0_OR_GREATER
        var n = Vector.ConvertToDouble(Native.ShiftRightArithmetic(Native.BitCast<Vector<ulong>, Vector<long>>(vx), 52));
#else
        var longN = Native.BitCast<Vector<ulong>, Vector<long>>(vx);
        Span<long> spanN = stackalloc long[Vector<long>.Count];
        for (var i = 0; i < Vector<long>.Count; i++)
        {
            spanN[i] = longN[i] >> 52;
        }

        var n = Vector.ConvertToDouble(new Vector<long>(spanN));
#endif
        vx = (vx & new Vector<ulong>(vMsk)) + new Vector<ulong>(vOff);

        // Adjust the mantissa to [-1/3, +1/3]
        var r = Native.BitCast<Vector<ulong>, Vector<double>>(vx) - Vector<double>.One;

        var r02 = r * r;
        var r04 = r02 * r02;
        var r08 = r04 * r04;
        var r16 = r08 * r08;

        // Compute log(x + 1) using Polynomial approximation
        // C0 + (r * C1) + (r^2 * C2) + ... + (r^20 * C20)
        var poly1 = new Vector<double>(c20)
            * r04
            + (new Vector<double>(c19) * r + new Vector<double>(c18))
            * r02
            + (new Vector<double>(c17) * r + new Vector<double>(c16));
        var poly2 = new Vector<double>(c08)
            * (r04
                * ((new Vector<double>(c15) * r + new Vector<double>(c14))
                    * r02
                    + (new Vector<double>(c13) * r + new Vector<double>(c12)))
                + ((new Vector<double>(c11) * r + new Vector<double>(c10))
                    * r02
                    + (new Vector<double>(c09) * r + new Vector<double>(c08))))
            + (r04
                * ((new Vector<double>(c07) * r + new Vector<double>(c06))
                    * r02
                    + (new Vector<double>(c05) * r + new Vector<double>(c04)))
                + ((new Vector<double>(c03) * r + new Vector<double>(c02))
                    * r02
                    + r));
        var poly = r16 * poly1 + poly2;

        return Vector.ConditionalSelect(
            Native.BitCast<Vector<ulong>, Vector<double>>(specialMask),
            specialResult,
            n * new Vector<double>(ln2Head) + n * new Vector<double>(ln2Tail) + poly);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Vector<float> Exp(Vector<float> x)
    {
#if NET9_0_OR_GREATER
        return Vector.Exp(x);
#else
        // This code is based on `vrs4_expf` from amd/aocl-libm-ose
        // Copyright (C) 2019-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes:
        // 1. Argument Reduction:
        //      e^x = 2^(x/ln2)                          --- (1)
        //
        //      Let x/ln(2) = z                          --- (2)
        //
        //      Let z = n + r , where n is an integer    --- (3)
        //                      |r| <= 1/2
        //
        //     From (1), (2) and (3),
        //      e^x = 2^z
        //          = 2^(N+r)
        //          = (2^N)*(2^r)                        --- (4)
        //
        // 2. Polynomial Evaluation
        //   From (4),
        //     r   = z - N
        //     2^r = C1 + C2*r + C3*r^2 + C4*r^3 + C5 *r^4 + C6*r^5
        //
        // 4. Reconstruction
        //      Thus,
        //        e^x = (2^N) * (2^r)

        const uint vArgMax = 0x42AE0000;

        const float vExpfMin = -103.97208f;
        const float vExpfMax = +88.72284f;

        const double vExpfHuge = 6755399441055744;
        const double vTblLn2 = 1.4426950408889634;

        const double c1 = 1.0000000754895704;
        const double c2 = 0.6931472254087585;
        const double c3 = 0.2402210737432219;
        const double c4 = 0.05550297297702539;
        const double c5 = 0.009676036358193323;
        const double c6 = 0.001341000536524434;

        var result = Vector.Narrow(CoreImpl(WidenLower(x)), CoreImpl(WidenUpper(x)));

        // Check if -103 < |x| < 88
        if (!Vector.LessThanOrEqualAll(Native.BitCast<Vector<float>, Vector<uint>>(Vector.Abs(x)), new Vector<uint>(vArgMax)))
        {
            // (x > V_EXPF_MAX) ? float.PositiveInfinity : x
            var infinityMask = Vector.GreaterThan(x, new Vector<float>(vExpfMax));

            result = Vector.ConditionalSelect(infinityMask, new Vector<float>(float.PositiveInfinity), result);

            // (x < V_EXPF_MIN) ? 0 : x
            result = Vector.AndNot(result, Vector.AsVectorSingle(Vector.LessThan(x, new Vector<float>(vExpfMin))));
        }

        return result;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<double> CoreImpl(Vector<double> x)
        {
            // x * (64.0 / ln(2))
            var z = x * new Vector<double>(vTblLn2);

            var expfHuge = new Vector<double>(vExpfHuge);
            var dn = z + expfHuge;

            // n = (int)z
            var n = Native.BitCast<Vector<double>, Vector<ulong>>(dn);

            // r = z - n
            var r = z - (dn - expfHuge);

            var r2 = r * r;
            var r4 = r2 * r2;

            var poly = r4
                * (new Vector<double>(c6) * r + new Vector<double>(c5))
                + (r2
                    * (new Vector<double>(c4) * r + new Vector<double>(c3))
                    + (new Vector<double>(c2) * r + new Vector<double>(c1)));

            // result = poly + (n << 52)
#if NET7_0_OR_GREATER
            return Native.BitCast<Vector<ulong>, Vector<double>>(Native.BitCast<Vector<double>, Vector<ulong>>(poly) + Vector.ShiftLeft(n, 52));
#else
            var span = new Span<ulong>(Native.GetAddress(ref n), Vector<ulong>.Count);
            for (var j = 0; j < Vector<ulong>.Count; j += Vector<ulong>.Count)
            {
                span[j] <<= 52;
            }

            return Native.BitCast<Vector<ulong>, Vector<double>>(Native.BitCast<Vector<double>, Vector<ulong>>(poly) + new Vector<ulong>(span));
#endif
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe Vector<double> Exp(Vector<double> x)
    {
#if NET9_0_OR_GREATER
        return Vector.Exp(x);
#else
        // This code is based on `vrd2_exp` from amd/aocl-libm-ose
        // Copyright (C) 2019-2020 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes
        // ----------------------
        // 1. Argument Reduction:
        //      e^x = 2^(x/ln2) = 2^(x*(64/ln(2))/64)     --- (1)
        //
        //      Choose 'n' and 'f', such that
        //      x * 64/ln2 = n + f                        --- (2) | n is integer
        //                            | |f| <= 0.5
        //     Choose 'm' and 'j' such that,
        //      n = (64 * m) + j                          --- (3)
        //
        //     From (1), (2) and (3),
        //      e^x = 2^((64*m + j + f)/64)
        //          = (2^m) * (2^(j/64)) * 2^(f/64)
        //          = (2^m) * (2^(j/64)) * e^(f*(ln(2)/64))
        //
        // 2. Table Lookup
        //      Values of (2^(j/64)) are precomputed, j = 0, 1, 2, 3 ... 63
        //
        // 3. Polynomial Evaluation
        //   From (2),
        //     f = x*(64/ln(2)) - n
        //   Let,
        //     r  = f*(ln(2)/64) = x - n*(ln(2)/64)
        //
        // 4. Reconstruction
        //      Thus,
        //        e^x = (2^m) * (2^(j/64)) * e^r

        const ulong vArgMax = 0x40862000_00000000;
        const ulong vDp64Bias = 1023;

        const double vExpfHuge = 6755399441055744;
        const double vTblLn2 = 1.4426950408889634;

        const double vLn2Head = +0.693359375;
        const double vLn2Tail = -0.00021219444005469057;

        const double c03 = 0.5000000000000018;
        const double c04 = 0.1666666666666617;
        const double c05 = 0.04166666666649277;
        const double c06 = 0.008333333333559272;
        const double c07 = 0.001388888895122404;
        const double c08 = 0.00019841269432677495;
        const double c09 = 2.4801486521374483E-05;
        const double c10 = 2.7557622532543023E-06;
        const double c11 = 2.7632293298250954E-07;
        const double c12 = 2.499430431958571E-08;

        // Check if -709 < vx < 709
        if (!Vector.LessThanOrEqualAll(Native.BitCast<Vector<double>, Vector<ulong>>(Vector.Abs(x)), new Vector<ulong>(vArgMax)))
        {
            // x * (64.0 / ln(2))
            var dn = x * new Vector<double>(vTblLn2) + new Vector<double>(vExpfHuge);

            // n = (int)z
            var n = Native.BitCast<Vector<double>, Vector<ulong>>(dn);

            // dn = (double)n
            dn -= new Vector<double>(vExpfHuge);

            // r = x - (dn * (ln(2) / 64))
            // where ln(2) / 64 is split into Head and Tail values
            var r = dn * new Vector<double>(-vLn2Head) + x;
            r = dn * new Vector<double>(-vLn2Tail) + r;

            var r2 = r * r;
            var r4 = r2 * r2;
            var r8 = r4 * r4;

            // Compute polynomial
            var poly1 = (new Vector<double>(c12) * r + new Vector<double>(c11)) * r2 + new Vector<double>(c10) * new Vector<double>(c09);
            var poly2 = (new Vector<double>(c08) * r + new Vector<double>(c07)) * r2 + new Vector<double>(c06) * new Vector<double>(c05);
            var poly3 = (new Vector<double>(c04) * r + new Vector<double>(c03)) * r2 + (r + Vector<double>.One);
            var poly = poly1 * r8 + (poly2 * r4 + poly3);

            // m = (n - j) / 64
            // result = polynomial * 2^m
#if NET7_0_OR_GREATER
                return poly * Native.BitCast<Vector<ulong>, Vector<double>>(Vector.ShiftLeft(n + new Vector<ulong>(vDp64Bias), 52));
#else
            var result = n + new Vector<ulong>(vDp64Bias);
            var span = new Span<ulong>(Native.GetAddress(ref result), Vector<ulong>.Count);
            for (var j = 0; j < Vector<ulong>.Count; j += Vector<ulong>.Count)
            {
                span[j] <<= 52;
            }

            return poly * Native.BitCast<Vector<ulong>, Vector<double>>(result);
#endif
        }
        else
        {
            Span<double> result = stackalloc double[Vector<double>.Count];
            for (var j = 0; j < Vector<double>.Count; j++)
            {
                result[j] = Math.Exp(x[j]);
            }

            return new Vector<double>(result);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Round(Vector<float> va)
    {
#if NET9_0_OR_GREATER
        return Vector.Round(va);
#else

        // This represents the boundary at which point we can only represent whole integers
        const float integerBoundary = 8388608.0f; // 2^23
        var vb = new Vector<float>(integerBoundary);
        var temp = CopySign(vb, va);
        return Vector.ConditionalSelect(Vector.GreaterThanOrEqual(va, vb), va, CopySign(va + temp - temp, va));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<float> CopySign(Vector<float> x, Vector<float> y)
        {
            const uint signMask = 0x8000_0000;
            var xbits = Vector.ConvertToUInt32(x);
            var ybits = Vector.ConvertToUInt32(y);
            var xmask = new Vector<uint>(~signMask);
            var ymask = new Vector<uint>(signMask);
            return Vector.ConvertToSingle(Vector.BitwiseAnd(xbits, xmask) | Vector.BitwiseAnd(ybits, ymask));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Round(Vector<double> va)
    {
#if NET9_0_OR_GREATER
        return Vector.Round(va);
#else

        // This represents the boundary at which point we can only represent whole integers
        const double integerBoundary = 4503599627370496.0; // 2^52
        var vb = new Vector<double>(integerBoundary);
        var temp = CopySign(vb, va);
        return Vector.ConditionalSelect(Vector.GreaterThanOrEqual(va, vb), va, CopySign(va + temp - temp, va));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<double> CopySign(Vector<double> x, Vector<double> y)
        {
            const ulong signMask = 0x8000_0000_0000_0000;
            var xbits = Vector.ConvertToUInt64(x);
            var ybits = Vector.ConvertToUInt64(y);
            var xmask = new Vector<ulong>(~signMask);
            var ymask = new Vector<ulong>(signMask);
            return Vector.ConvertToDouble(Vector.BitwiseAnd(xbits, xmask) | Vector.BitwiseAnd(ybits, ymask));
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Truncate(Vector<float> v)
    {
#if NET9_0_OR_GREATER
        return Vector.Truncate(v);
#else
        Span<float> result = stackalloc float[Vector<float>.Count];
        for (var j = 0; j < Vector<float>.Count; j++)
        {
            result[j] = MathF.Truncate(v[j]);
        }

        return new Vector<float>(result);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Truncate(Vector<double> v)
    {
#if NET9_0_OR_GREATER
        return Vector.Truncate(v);
#else
        Span<double> result = stackalloc double[Vector<double>.Count];
        for (var j = 0; j < Vector<double>.Count; j++)
        {
            result[j] = Math.Truncate(v[j]);
        }

        return new Vector<double>(result);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Hypot(Vector<float> vx, Vector<float> vy)
    {
#if NET9_0_OR_GREATER
        return Vector.Hypot(vx, vy);
#else
        return Vector.SquareRoot(vx * vx + vy * vy);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Hypot(Vector<double> vx, Vector<double> vy)
    {
#if NET9_0_OR_GREATER
        return Vector.Hypot(vx, vy);
#else
        return Vector.SquareRoot(vx * vx + vy * vy);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Clamp(Vector<float> v, Vector<float> min, Vector<float> max)
    {
#if NET9_0_OR_GREATER
        return Vector.Clamp(v, min, max);
#else
        return Vector.Min(Vector.Max(v, min), max);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Clamp(Vector<double> v, Vector<double> min, Vector<double> max)
    {
#if NET9_0_OR_GREATER
        return Vector.Clamp(v, min, max);
#else
        return Vector.Min(Vector.Max(v, min), max);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Lerp(Vector<float> x, Vector<float> y, Vector<float> amount)
    {
#if NET9_0_OR_GREATER
        return Vector.Lerp(x, y, amount);
#else
        return x * (Vector<float>.One - amount) + y * amount;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Lerp(Vector<double> x, Vector<double> y, Vector<double> amount)
    {
#if NET9_0_OR_GREATER
        return Vector.Lerp(x, y, amount);
#else
        return x * (Vector<double>.One - amount) + y * amount;
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Floor(Vector<float> value)
    {
        var intVal = Vector.ConvertToInt32(value);
        var floatInt = Vector.ConvertToSingle(intVal);
        var hasFraction = Vector.OnesComplement(Vector.Equals(value, floatInt));
        var isNegative = Vector.LessThan(value, Vector<float>.Zero);
        var needsAdjust = Vector.BitwiseAnd(hasFraction, isNegative);
        var adjustment = Vector.ConditionalSelect(needsAdjust, Vector<float>.One, Vector<float>.Zero);
        return floatInt - adjustment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Floor(Vector<double> value)
    {
        var intVal = Vector.ConvertToInt64(value);
        var floatInt = Vector.ConvertToDouble(intVal);
        var hasFraction = Vector.OnesComplement(Vector.Equals(value, floatInt));
        var isNegative = Vector.LessThan(value, Vector<double>.Zero);
        var needsAdjust = Vector.BitwiseAnd(hasFraction, isNegative);
        var adjustment = Vector.ConditionalSelect(needsAdjust, Vector<double>.One, Vector<double>.Zero);
        return floatInt - adjustment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Ceiling(Vector<float> value)
    {
        var intVal = Vector.ConvertToInt32(value);
        var floatInt = Vector.ConvertToSingle(intVal);
        var hasFraction = Vector.OnesComplement(Vector.Equals(value, floatInt));
        var isPositive = Vector.GreaterThan(value, Vector<float>.Zero);
        var needsAdjust = Vector.BitwiseAnd(hasFraction, isPositive);
        var adjustment = Vector.ConditionalSelect(needsAdjust, Vector<float>.One, Vector<float>.Zero);
        return floatInt + adjustment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Ceiling(Vector<double> value)
    {
        var intVal = Vector.ConvertToInt64(value);
        var floatInt = Vector.ConvertToDouble(intVal);
        var hasFraction = Vector.OnesComplement(Vector.Equals(value, floatInt));
        var isPositive = Vector.GreaterThan(value, Vector<double>.Zero);
        var needsAdjust = Vector.BitwiseAnd(hasFraction, isPositive);
        var adjustment = Vector.ConditionalSelect(needsAdjust, Vector<double>.One, Vector<double>.Zero);
        return floatInt + adjustment;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Pow(Vector<float> x, Vector<float> exponent)
    {
        return Exp(exponent * Log(x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Pow(Vector<double> x, Vector<double> exponent)
    {
        return Exp(exponent * Log(x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Sin(Vector<float> x)
    {
#if NET7_0_OR_GREATER
        return Vector.Sin(x);
#else
        // This code is based on `sinf` from amd/aocl-libm-ose
        // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes
        // ---------------------
        // checks for special cases
        // if ( ux = infinity) raise overflow exception and return x
        // if x is NaN then raise invalid FP operation exception and return x.
        //
        // 1. Argument reduction
        // if |x| > 5e5 then
        //      __amd_remainder_piby2(x, &r, &rr, &region)
        // else
        //      Argument reduction
        //      Let z = |x| * 2/pi
        //      z = dn + r, where dn = round(z)
        //      rhead =  dn * pi/2_head
        //      rtail = dn * pi/2_tail
        //      r = z – dn = |x| - rhead – rtail
        //      expdiff = exp(dn) – exp(r)
        //      if(expdiff) > 15)
        //      rtail = |x| - dn*pi/2_tail2
        //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
        // rr = (|x| – rhead) – r + rtail
        //
        // 2. Polynomial approximation
        // if(dn is odd)
        //       rr = rr * r;
        //       x4 = x2 * x2;
        //       s = 0.5 * x2;
        //       t =  s - 1.0;
        //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4))))
        //       r = (((1.0 + t) - s) - rr) + poly – t
        // else
        //       x3 = x2 * r
        //       poly = S2 + (r2 * (S3 + (r2 * (S4))))
        //       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
        // if(((sign & region) | ((~sign) & (~region))) & 1)
        //       return r
        // else
        //       return -r;
        //
        // if |x| < pi/4 && |x| > 2.0^(-13)
        //   sin(x) = x + (x * (r2 * (S1 + r2 * (S2 + r2 * (S3 + r2 * (S4)))))
        // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
        //   sin(x) = x - (x * x * x * (1/6));

        const int argHuge = 0x4A989680;    // 5e6
        const int argLarge = 0x3F490FDB;   // PI / 4
        const int argSmall = 0x3C000000;   // 2^-13
        const int argSmaller = 0x39000000; // 2^-27

        var ax = Vector.Abs(x);
        var ux = Native.BitCast<Vector<float>, Vector<int>>(ax);

        Vector<float> result;

        if (Vector.LessThanAll(ux, new Vector<int>(argLarge + 1)))
        {
            // We must be a finite value: (pi / 4) >= |x|

            if (Vector.GreaterThanAny(ux, new Vector<int>(argSmall - 1)))
            {
                // At least one element is: |x| >= 2^-13
                result = Vector.Narrow(SinSinglePoly(WidenLower(x)), SinSinglePoly(WidenUpper(x))
                );
            }
            else
            {
                // At least one element is: 2^-13 > |x|
                var x3 = x * x * x;
                result = new Vector<float>(-0.16666667f) * x3 + x;
            }
        }
        else if (Vector.LessThanAll(ux, new Vector<int>(argHuge)))
        {
            // At least one element is: |x| > (pi / 4) -or- infinite -or- nan
            result = Vector.Narrow(
                CoreImpl(WidenLower(x)),
                CoreImpl(WidenUpper(x))
            );
        }
        else
        {
            Span<float> temp = stackalloc float[Vector<double>.Count];
            for (var j = 0; j < Vector<float>.Count; j++)
            {
                temp[j] = MathF.Sin(x[j]);
            }

            return new Vector<float>(temp);
        }

        return Vector.ConditionalSelect(
            Native.BitCast<Vector<int>, Vector<float>>(Vector.GreaterThan(ux, new Vector<int>(argSmaller - 1))),
            result, // for elements: |x| >= 2^-27, infinity, or NaN
            x       // for elements: 2^-27 > |x|
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<double> CoreImpl(Vector<double> x)
        {
            var ax = Vector.Abs(x);
            var (r, _, region) = SinCosReduce(ax);

            var sin = SinSinglePoly(r);
            var cos = CosSingleLarge(r);

            var tempSign = Native.BitCast<Vector<double>, Vector<long>>(x);
            Span<long> temp1 = stackalloc long[Vector<long>.Count];
            Span<long> temp2 = stackalloc long[Vector<long>.Count];
            for (var j = 0; j < Vector<long>.Count; j++)
            {
                temp1[j] = tempSign[j] >>> 63;
                temp2[j] = region[j] >>> 1;
            }

            var sign = new Vector<long>(temp2);
            var mask1 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(region & Vector<long>.One, Vector<long>.Zero));
            region = new Vector<long>(temp2);
            var mask2 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(((sign & region) | (~sign & ~region)) * Vector<long>.One, Vector<long>.Zero));

            var result = Vector.ConditionalSelect(mask1, sin, cos);
            return Vector.ConditionalSelect(mask2, -result, result);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Sin(Vector<double> x)
    {
#if NET7_0_OR_GREATER
        return Vector.Sin(x);
#else
        // This code is based on `cosf` from amd/aocl-libm-ose
        // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes
        // ---------------------
        // Checks for special cases
        // if ( ux = infinity) raise overflow exception and return x
        // if x is NaN then raise invalid FP operation exception and return x.
        //
        // 1. Argument reduction
        // if |x| > 5e5 then
        //      __amd_remainder_piby2d2f((uint64_t)x, &r, &region)
        // else
        //      Argument reduction
        //      Let z = |x| * 2/pi
        //      z = dn + r, where dn = round(z)
        //      rhead =  dn * pi/2_head
        //      rtail = dn * pi/2_tail
        //      r = z – dn = |x| - rhead – rtail
        //      expdiff = exp(dn) – exp(r)
        //      if(expdiff) > 15)
        //      rtail = |x| - dn*pi/2_tail2
        //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1
        //          -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
        //
        // 2. Polynomial approximation
        // if(dn is even)
        //       x4 = x2 * x2;
        //       s = 0.5 * x2;
        //       t =  1.0 - s;
        //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * C4 )))
        //       r = t + poly
        // else
        //       x3 = x2 * r
        //       poly = x3 * (S1 + x2 * (S2 + x2 * (S3 + x2 * S4)))
        //       r = r + poly
        // if((sign + 1) & 2)
        //       return r
        // else
        //       return -r;
        //
        // if |x| < pi/4 && |x| > 2.0^(-13)
        //   r = 0.5 * x2;
        //   t = 1 - r;
        //   cos(x) = t + ((1.0 - t) - r) + (x*x * (x*x * C1 + C2*x*x + C3*x*x
        //             + C4*x*x +x*x*C5 + x*x*C6)))
        //
        // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
        //   cos(x) = 1.0 - x*x*0.5;;
        //
        // else return 1.0

        const int argHuge = 0x4A989680;    // 5e6
        const int argLarge = 0x3F490FDB;   // PI / 4
        const int argSmall = 0x3C000000;   // 2^-13
        const int argSmaller = 0x39000000; // 2^-27

        var ax = Vector.Abs(x);
        var ux = Native.BitCast<Vector<double>, Vector<long>>(ax);

        Vector<double> result;

        if (Vector.LessThanAll(ux, new Vector<long>(argLarge + 1)))
        {
            // We must be finite value: (pi / 4) >= |x|
            var x2 = x * x;
            if (Vector.GreaterThanAny(ux, new Vector<long>(argSmall - 1)))
            {
                // At least one element is: |x| >= 2^-13
                result = SinDoublePoly(x);
            }
            else
            {
                // At least one element is: |x| >= 2^-13
                var x3 = x2 * x;
                result = new Vector<double>(-0.16666666666666666) * x3 + x;
            }
        }
        else if (Vector.LessThanAll(ux, new Vector<long>(argHuge)))
        {
            var (r, rr, region) = SinCosReduce(ax);
            var sin = SinDoubleLarge(r, rr);
            var cos = CosDoubleLarge(r, rr);

            var tempSign = Native.BitCast<Vector<double>, Vector<long>>(x);
            Span<long> temp1 = stackalloc long[Vector<long>.Count];
            Span<long> temp2 = stackalloc long[Vector<long>.Count];
            for (var j = 0; j < Vector<long>.Count; j++)
            {
                temp1[j] = tempSign[j] >>> 63;
                temp2[j] = region[j] >>> 1;
            }

            var sign = new Vector<long>(temp2);
            var mask1 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(region & Vector<long>.One, Vector<long>.Zero));
            region = new Vector<long>(temp2);
            var mask2 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(((sign & region) | (~sign & ~region)) * Vector<long>.One, Vector<long>.Zero));

            result = Vector.ConditionalSelect(mask1, sin, cos);
            result = Vector.ConditionalSelect(mask2, result, -result);
        }
        else
        {
            Span<double> temp = stackalloc double[Vector<double>.Count];
            for (var j = 0; j < Vector<double>.Count; j++)
            {
                temp[j] = Math.Sin(x[j]);
            }

            return new Vector<double>(temp);
        }

        return Vector.ConditionalSelect(
            Native.BitCast<Vector<long>, Vector<double>>(Vector.GreaterThan(ux, new Vector<long>(argSmaller - 1))),
            result, // for elements: |x| >= 2^-27, infinity, or NaN
            x       // for elements: 2^-27 > |x|
        );

#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Cos(Vector<float> x)
    {
#if NET7_0_OR_GREATER
        return Vector.Cos(x);
#else
        // This code is based on `cosf` from amd/aocl-libm-ose
        // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes
        // ---------------------
        // Checks for special cases
        // if ( ux = infinity) raise overflow exception and return x
        // if x is NaN then raise invalid FP operation exception and return x.
        //
        // 1. Argument reduction
        // if |x| > 5e5 then
        //      __amd_remainder_piby2d2f((uint64_t)x, &r, &region)
        // else
        //      Argument reduction
        //      Let z = |x| * 2/pi
        //      z = dn + r, where dn = round(z)
        //      rhead =  dn * pi/2_head
        //      rtail = dn * pi/2_tail
        //      r = z – dn = |x| - rhead – rtail
        //      expdiff = exp(dn) – exp(r)
        //      if(expdiff) > 15)
        //      rtail = |x| - dn*pi/2_tail2
        //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1
        //          -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
        //
        // 2. Polynomial approximation
        // if(dn is even)
        //       x4 = x2 * x2;
        //       s = 0.5 * x2;
        //       t =  1.0 - s;
        //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * C4 )))
        //       r = t + poly
        // else
        //       x3 = x2 * r
        //       poly = x3 * (S1 + x2 * (S2 + x2 * (S3 + x2 * S4)))
        //       r = r + poly
        // if((sign + 1) & 2)
        //       return r
        // else
        //       return -r;
        //
        // if |x| < pi/4 && |x| > 2.0^(-13)
        //   r = 0.5 * x2;
        //   t = 1 - r;
        //   cos(x) = t + ((1.0 - t) - r) + (x*x * (x*x * C1 + C2*x*x + C3*x*x
        //             + C4*x*x +x*x*C5 + x*x*C6)))
        //
        // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
        //   cos(x) = 1.0 - x*x*0.5;;
        //
        // else return 1.0

        const int argHuge = 0x4A989680;    // 5e6
        const int argLarge = 0x3F490FDB;   // PI / 4
        const int argSmall = 0x3C000000;   // 2^-13
        const int argSmaller = 0x39000000; // 2^-27

        var ax = Vector.Abs(x);
        var ux = Native.BitCast<Vector<float>, Vector<int>>(ax);

        Vector<float> result;

        if (Vector.LessThanAll(ux, new Vector<int>(argLarge + 1)))
        {
            // We must be a finite value: (pi / 4) >= |x|

            if (Vector.GreaterThanAny(ux, new Vector<int>(argSmall - 1)))
            {
                // at least one element is: |x| >= 2^-13
                result = Vector.Narrow(CosSingleSmall(WidenLower(x)), CosSingleSmall(WidenUpper(x)));
            }
            else
            {
                // at least one element is: 2^-13 > |x|
                var x2 = x * x;
                result = new Vector<float>(-0.5f) * x2 + Vector<float>.One;
            }
        }
        else if (Vector.LessThanAll(ux, new Vector<int>(argHuge)))
        {
            // at least one element is: |x| > (pi / 4) -or- infinite -or- nan
            result = Vector.Narrow(CoreImpl(WidenLower(ax)), CoreImpl(WidenUpper(ax)));
        }
        else
        {
            Span<float> temp = stackalloc float[Vector<double>.Count];
            for (var j = 0; j < Vector<float>.Count; j++)
            {
                temp[j] = MathF.Cos(x[j]);
            }

            return new Vector<float>(temp);
        }

        return Vector.ConditionalSelect(
            Native.BitCast<Vector<int>, Vector<float>>(Vector.GreaterThan(ux, new Vector<int>(argSmaller - 1))),
            result,           // for elements: |x| >= 2^-27, infinity, or NaN
            Vector<float>.One // for elements: 2^-27 > |x|
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector<double> CoreImpl(Vector<double> ax)
        {
            var (r, _, region) = SinCosReduce(ax);

            var sin = SinSinglePoly(r);
            var cos = CosSingleLarge(r);

            var result = Vector.ConditionalSelect(
                Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(region & Vector<long>.One, Vector<long>.Zero)),
                cos, // region 0 or 2
                sin  // region 1 or 3
            );

            return Vector.ConditionalSelect(
                Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals((region + Vector<long>.One) & new Vector<long>(2), Vector<long>.Zero)),
                result, // region 0 or 3
                -result // region 1 or 2
            );
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Cos(Vector<double> x)
    {
#if NET7_0_OR_GREATER
        return Vector.Cos(x);
#else
        // This code is based on `cos` from amd/aocl-libm-ose
        // Copyright (C) 2008-2022 Advanced Micro Devices, Inc. All rights reserved.
        //
        // Licensed under the BSD 3-Clause "New" or "Revised" License
        // See THIRD-PARTY-NOTICES.TXT for the full license text

        // Implementation Notes
        // ---------------------
        // checks for special cases
        // if ( ux = infinity) raise overflow exception and return x
        // if x is NaN then raise invalid FP operation exception and return x.
        //
        // 1. Argument reduction
        // if |x| > 5e5 then
        //      __amd_remainder_piby2(x, &r, &rr, &region)
        // else
        //      Argument reduction
        //      Let z = |x| * 2/pi
        //      z = dn + r, where dn = round(z)
        //      rhead =  dn * pi/2_head
        //      rtail = dn * pi/2_tail
        //      r = z – dn = |x| - rhead – rtail
        //      expdiff = exp(dn) – exp(r)
        //      if(expdiff) > 15)
        //      rtail = |x| - dn*pi/2_tail2
        //      r = |x| -  dn*pi/2_head -  dn*pi/2_tail1 -  dn*pi/2_tail2  - (((rhead + rtail) – rhead )-rtail)
        // rr = (|x| – rhead) – r + rtail
        //
        // 2. Polynomial approximation
        // if(dn is even)
        //       rr = rr * r;
        //       x4 = x2 * x2;
        //       s = 0.5 * x2;
        //       t =  s - 1.0;
        //       poly = x4 * (C1 + x2 * (C2 + x2 * (C3 + x2 * (C4 + x2 * (C5 + x2 * x6)))))
        //       r = (((1.0 + t) - s) - rr) + poly – t
        // else
        //       x3 = x2 * r
        //       poly = S2 + (r2 * (S3 + (r2 * (S4 + (r2 * (S5 + S6 * r2))))))
        //       r = r - ((x2 * (0.5*rr - x3 * poly)) - rr) - S1 * x3
        // if((sign + 1) & 2)
        //       return r
        // else
        //       return -r;
        //
        // if |x| < pi/4 && |x| > 2.0^(-13)
        //   cos(x) = 1.0 + x*x * (-0.5 + (C1*x*x + (C2*x*x + (C3*x*x
        //                              + (C4*x*x + (C5*x*x + C6*x*x))))))
        //
        // if |x| < 2.0^(-13) && |x| > 2.0^(-27)
        //   cos(x) = 1.0 - x*x*0.5;;
        //
        // else return 1.0

        const long argHuge = 0x415312D000000000;    // 5e6
        const long argLarge = 0x3FE921FB54442D18;   // PI / 4
        const long argSmall = 0x3F20000000000000;   // 2^-13
        const long argSmaller = 0x3E40000000000000; // 2^-27

        var ax = Vector.Abs(x);
        var ux = Native.BitCast<Vector<double>, Vector<long>>(ax);
        Vector<double> result;

        if (Vector.LessThanAll(ux, new Vector<long>(argLarge + 1)))
        {
            // We must be a finite value: (pi / 4) >= |x|
            var x2 = x * x;
            if (Vector.GreaterThanAny(ux, new Vector<long>(argSmall - 1)))
            {
                // At least one element is: |x| >= 2^-13
                result = (CosDoublePoly(x) * x2 + new Vector<double>(-0.5)) * x2 + Vector<double>.One;
            }
            else
            {
                result = new Vector<double>(0.5) + x2 * Vector<double>.One;
            }
        }
        else if (Vector.LessThanAny(ux, new Vector<long>(argHuge)))
        {
            // At least one element is: |x| > (pi / 4) -or- infinite -or- nan
            var (r, rr, region) = SinCosReduce(ax);
            var sin = SinDoubleLarge(r, rr);
            var cos = CosDoubleLarge(r, rr);
            var mask1 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals(region & Vector<long>.One, Vector<long>.Zero));
            var mask2 = Native.BitCast<Vector<long>, Vector<double>>(Vector.Equals((region + Vector<long>.One) & new Vector<long>(2), Vector<long>.Zero));

            result = Vector.ConditionalSelect(mask1, cos, sin);
            result = Vector.ConditionalSelect(mask2, result, -result);
        }
        else
        {
            Span<double> temp = stackalloc double[Vector<double>.Count];
            for (var j = 0; j < Vector<double>.Count; j++)
            {
                temp[j] = Math.Sin(x[j]);
            }

            return new Vector<double>(temp);
        }

        return Vector.ConditionalSelect(
            Native.BitCast<Vector<long>, Vector<double>>(Vector.GreaterThan(ux, new Vector<long>(argSmaller - 1))),
            result,            // for elements: |x| >= 2^-27, infinity, or NaN
            Vector<double>.One // for elements: |x| <= 2^-27
        );
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Tan(Vector<float> x)
    {
#if NET_7_0_OR_GREATER
        return Vector.Tan(x);
#else
        return Vector.Divide(Sin(x), Cos(x));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Tan(Vector<double> x)
    {
#if NET_7_0_OR_GREATER
        return Vector.Tan(x);
#else
        return Vector.Divide(Sin(x), Cos(x));
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Asin(Vector<float> x)
    {
        return Atan(x / Vector.SquareRoot(Vector<float>.One - x * x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Asin(Vector<double> x)
    {
        return Atan(x / Vector.SquareRoot(Vector<double>.One - x * x));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Acos(Vector<float> x)
    {
        const float piOver2 = MathF.PI / 2;
        return new Vector<float>(piOver2) - Asin(x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Acos(Vector<double> x)
    {
        const float piOver2 = MathF.PI / 2;
        return new Vector<double>(piOver2) - Asin(x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Atan(Vector<float> x)
    {
        const float piOver4 = MathF.PI / 4;
        const float piOver2 = MathF.PI / 2;
        const float c1 = 0.2447f;
        const float c2 = 0.0663f;

        var absValue = Vector.Abs(x);
        var greaterThanOne = Vector.GreaterThan(absValue, Vector<float>.One);

        // For |value| > 1, use atan(x) = sign(x) * π/2 - atan(1/x)
        var smallX = Vector.ConditionalSelect(greaterThanOne, Vector<float>.One / x, x);
        var absSmallX = Vector.Abs(smallX);

        // Minimax polynomial approx: (π/4)*smallX - smallX*(|smallX|-1)*(0.2447 + 0.0663*|smallX|)
        var pi4Term = piOver4 * smallX;
        var polyTerm = smallX * (absSmallX - Vector<float>.One) * (new Vector<float>(c1) + new Vector<float>(c2) * absSmallX);
        var approxSmall = pi4Term - polyTerm;

        // Sign of value
        var signValue = Vector.ConditionalSelect(Vector.Equals(x, Vector<float>.Zero), Vector<float>.Zero, x / absValue);

        // Adjust for |value| > 1
        var adjustment = signValue * piOver2 - approxSmall;
        return Vector.ConditionalSelect(greaterThanOne, adjustment, approxSmall);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Atan(Vector<double> x)
    {
        const double piOver2 = Math.PI / 2;
        const double c1 = 0.2447;
        const double c2 = 0.0663;

        var absValue = Vector.Abs(x);
        var greaterThanOne = Vector.GreaterThan(absValue, Vector<double>.One);

        // For |value| > 1, use atan(x) = sign(x) * π/2 - atan(1/x)
        var smallX = Vector.ConditionalSelect(greaterThanOne, Vector<double>.One / x, x);
        var absSmallX = Vector.Abs(smallX);

        // Minimax polynomial approx: (π/4)*smallX - smallX*(|smallX|-1)*(0.2447 + 0.0663*|smallX|)
        var pi4Term = new Vector<double>(Math.PI / 4) * smallX;
        var polyTerm = smallX * (absSmallX - Vector<double>.One) * (new Vector<double>(c1) + new Vector<double>(c2) * absSmallX);
        var approxSmall = pi4Term - polyTerm;

        // Sign of value
        var signValue = Vector.ConditionalSelect(Vector.Equals(x, Vector<double>.Zero), Vector<double>.Zero, x / absValue);

        // Adjust for |value| > 1
        var adjustment = signValue * piOver2 - approxSmall;
        return Vector.ConditionalSelect(greaterThanOne, adjustment, approxSmall);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Sinh(Vector<float> value)
    {
        var expPos = Exp(value);
        var expNeg = Exp(Vector.Negate(value));
        return (expPos - expNeg) * new Vector<float>(0.5f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Sinh(Vector<double> value)
    {
        var expPos = Exp(value);
        var expNeg = Exp(Vector.Negate(value));
        return (expPos - expNeg) * new Vector<double>(0.5d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Cosh(Vector<float> value)
    {
        var expPos = Exp(value);
        var expNeg = Exp(Vector.Negate(value));
        return (expPos + expNeg) * new Vector<float>(0.5f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Cosh(Vector<double> value)
    {
        var expPos = Exp(value);
        var expNeg = Exp(Vector.Negate(value));
        return (expPos + expNeg) * new Vector<double>(0.5d);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Tanh(Vector<float> value)
    {
        return Sinh(value) / Cosh(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Tanh(Vector<double> value)
    {
        return Sinh(value) / Cosh(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<float> Asinh(Vector<float> x)
    {
        return Log(x + Vector.SquareRoot(x * x + Vector<float>.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector<double> Asinh(Vector<double> x)
    {
        return Log(x + Vector.SquareRoot(x * x + Vector<double>.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Acosh(Vector<float> value)
    {
        return Log(value + Vector.SquareRoot(value * value - Vector<float>.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Acosh(Vector<double> value)
    {
        return Log(value + Vector.SquareRoot(value * value - Vector<double>.One));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<float> Atanh(Vector<float> value)
    {
        return new Vector<float>(0.5f) * Log((Vector<float>.One + value) / (Vector<float>.One - value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector<double> Atanh(Vector<double> value)
    {
        return new Vector<double>(0.5f) * Log((Vector<double>.One + value) / (Vector<double>.One - value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> SinSinglePoly(Vector<double> r)
    {
        const double s1 = -0.16666666666666666;
        const double s2 = +0.00833333333333095;
        const double s3 = -0.00019841269836761127;
        const double s4 = +2.7557316103728802E-06;

        var r2 = r * r;
        var r3 = r2 * r;
        var r4 = r2 * r2;

        return ((new Vector<double>(s4) * r2 + new Vector<double>(s3))
                * r4
                + (new Vector<double>(s2) * r2 + new Vector<double>(s1)))
            * r3
            + r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> SinDoublePoly(Vector<double> r)
    {
        const double s1 = -0.16666666666666666;
        const double s2 = +0.00833333333333095;
        const double s3 = -0.00019841269836761127;
        const double s4 = +2.7557316103728802E-06;
        const double s5 = -2.5051132068021698E-08;
        const double s6 = +1.5918144304485914E-10;

        var r2 = r * r;
        var r3 = r2 * r;
        var r4 = r2 * r2;
        var r8 = r4 * r4;

        var poly = (new Vector<double>(s6) * r2 + new Vector<double>(s5))
            * r8
            + ((new Vector<double>(s4) * r2 + new Vector<double>(s3))
                * r4
                + (new Vector<double>(s2) * r2 + new Vector<double>(s1)));

        return poly * r3 + r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> CosSinglePoly(Vector<double> r)
    {
        const double c1 = +0.041666666666666664;
        const double c2 = -0.0013888888888887398;
        const double c3 = +2.4801587298767044E-05;
        const double c4 = -2.755731727234489E-07;

        var r2 = r * r;
        var r4 = r2 * r2;

        return (new Vector<double>(c4) * r2 + new Vector<double>(c3))
            * r4
            + (new Vector<double>(c2) * r2 + new Vector<double>(c1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> CosDoublePoly(Vector<double> r)
    {
        const double c1 = +0.041666666666666664;
        const double c2 = -0.0013888888888887398;
        const double c3 = +2.4801587298767044E-05;
        const double c4 = -2.755731727234489E-07;
        const double c5 = +2.0876146382372144E-09;
        const double c6 = -1.138263981623609E-11;

        var r2 = r * r;
        var r4 = r2 * r2;
        var r8 = r4 * r4;

        return (new Vector<double>(c6) * r2 + new Vector<double>(c5))
            * r8
            + ((new Vector<double>(c4) * r2 + new Vector<double>(c3))
                * r4
                + (new Vector<double>(c2) * r2 + new Vector<double>(c1)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> SinDoubleLarge(Vector<double> r, Vector<double> rr)
    {
        const double s1 = -0.16666666666666666;
        const double s2 = +0.00833333333333095;
        const double s3 = -0.00019841269836761127;
        const double s4 = +2.7557316103728802E-06;
        const double s5 = -2.5051132068021698E-08;
        const double s6 = +1.5918144304485914E-10;

        var r2 = r * r;
        var r3 = r2 * r;
        var r4 = r2 * r2;
        var r8 = r4 * r4;

        var sinPoly = new Vector<double>(s6)
            * r8
            + ((new Vector<double>(s5) * r2 + new Vector<double>(s4))
                * r4
                + (new Vector<double>(s3) * r2 + new Vector<double>(s2)));

        return r - new Vector<double>(-s1)
            * r3
            + ((rr * new Vector<double>(0.5) - r3 * sinPoly)
                * r2
                - rr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> CosSingleLarge(Vector<double> r)
    {
        var r2 = r * r;
        var r4 = r2 * r2;

        return CosSinglePoly(r) * r4 + new Vector<double>(-0.5) * r2 + Vector<double>.One;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> CosDoubleLarge(Vector<double> r, Vector<double> rr)
    {
        var r2 = r * r;
        var r4 = r2 * r2;

        var s = r2 * 0.5;
        var t = s - Vector<double>.One;

        return CosDoublePoly(r) * r4 + r * rr + (Vector<double>.One + t) - s - t;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> CosSingleSmall(Vector<double> x)
    {
        var x2 = x * x;
        var x4 = x2 * x2;

        var r = x2 * 0.5;
        var t = Vector<double>.One - r;
        var s = t + (Vector<double>.One - t - r);

        return CosSinglePoly(x) * x4 + s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (Vector<double> r, Vector<double> rr, Vector<long> region) SinCosReduce(Vector<double> ax)
    {
        // reduce  the argument to be in a range from (-pi / 4) to (+pi / 4) by subtracting multiples of (pi / 2)

        const double vAlmShift = 6755399441055744.0;
        const double vTwoByPi = 0.6366197723675814;

        const double vPiByTwo1 = 1.5707963267341256;
        const double vPiByTwo2 = 6.077100506303966E-11;
        const double vPiByTwo2Tail = 2.0222662487959506E-21;

        // dn = (int)(|x| * 2 / pi)
        var npi2 = new Vector<double>(vTwoByPi) * ax + new Vector<double>(vAlmShift);
        var region = Native.BitCast<Vector<double>, Vector<long>>(npi2);
        npi2 -= new Vector<double>(vAlmShift);

        var rhead = new Vector<double>(-vPiByTwo1) * npi2 + ax;
        var rtail = npi2 * vPiByTwo2;
        var r = rhead - rtail;

        rtail =  new Vector<double>(vPiByTwo2Tail) * npi2 - (rhead - r - rtail);
        rhead =  r;
        r     -= rtail;

        return (r, rhead - r - rtail, region);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> WidenLower(Vector<float> source)
    {
        Span<double> span = stackalloc double[Vector<double>.Count];

        for (var i = 0; i < Vector<double>.Count; i++)
        {
            span[i] = source[i];
        }

        return new Vector<double>(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<double> WidenUpper(Vector<float> source)
    {
        Span<double> span = stackalloc double[Vector<double>.Count];

        for (var i = Vector<double>.Count; i < Vector<float>.Count; i++)
        {
            span[i - Vector<double>.Count] = source[i];
        }

        return new Vector<double>(span);
    }
}

/// <summary>
/// Operator invoker.
/// </summary>
/// <typeparam name="T">The type that provides operator override.</typeparam>
internal static class Operations<T> where T : struct
{
    private static readonly Func<T, T, T>?    s_Add      = (typeof(T).GetMethod("op_Addition", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, T>)) as Func<T, T, T>)!;
    private static readonly Func<T, T, T>?    s_Sub      = (typeof(T).GetMethod("op_Subtraction", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, T>)) as Func<T, T, T>)!;
    private static readonly Func<T, T, T>?    s_Mul      = (typeof(T).GetMethod("op_Multiply", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, T>)) as Func<T, T, T>)!;
    private static readonly Func<T, T, T>?    s_Div      = (typeof(T).GetMethod("op_Division", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, T>)) as Func<T, T, T>)!;
    private static readonly Func<T, T, T>?    s_Mod      = (typeof(T).GetMethod("op_Modulus", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, T>)) as Func<T, T, T>)!;
    private static readonly Func<T, T, bool>? s_Gt       = (typeof(T).GetMethod("op_GreaterThan", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>)!;
    private static readonly Func<T, T, bool>? s_Lt       = (typeof(T).GetMethod("op_LessThan", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>)!;
    private static readonly Func<T, T, bool>? s_Ge       = (typeof(T).GetMethod("op_GreaterThanOrEqual", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>)!;
    private static readonly Func<T, T, bool>? s_Le       = (typeof(T).GetMethod("op_LessThanOrEqual", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.CreateDelegate(typeof(Func<T, T, bool>)) as Func<T, T, bool>)!;
    private static readonly TypeCode          s_TypeCode = Type.GetTypeCode(typeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Add(T a, T b)
    {
        switch (s_TypeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            {
                var value = Native.Cast<T, int>(ref a) + Native.Cast<T, int>(ref b);
                return Native.Cast<int, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = Native.Cast<T, long>(ref a) + Native.Cast<T, long>(ref b);
                return Native.Cast<long, T>(ref value);
            }
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            {
                var value = Native.Cast<T, uint>(ref a) + Native.Cast<T, uint>(ref b);
                return Native.Cast<uint, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = Native.Cast<T, ulong>(ref a) + Native.Cast<T, ulong>(ref b);
                return Native.Cast<ulong, T>(ref value);
            }
            case TypeCode.Single:
            {
                var value = Native.Cast<T, float>(ref a) + Native.Cast<T, float>(ref b);
                return Native.Cast<float, T>(ref value);
            }
            case TypeCode.Double:
            {
                var value = Native.Cast<T, double>(ref a) + Native.Cast<T, double>(ref b);
                return Native.Cast<double, T>(ref value);
            }
            case TypeCode.Decimal:
            {
                var value = Native.Cast<T, decimal>(ref a) + Native.Cast<T, decimal>(ref b);
                return Native.Cast<decimal, T>(ref value);
            }
            default: return s_Add.Invoke(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Subtract(T a, T b)
    {
        switch (s_TypeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            {
                var value = Native.Cast<T, int>(ref a) - Native.Cast<T, int>(ref b);
                return Native.Cast<int, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = Native.Cast<T, long>(ref a) - Native.Cast<T, long>(ref b);
                return Native.Cast<long, T>(ref value);
            }
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            {
                var value = Native.Cast<T, uint>(ref a) - Native.Cast<T, uint>(ref b);
                return Native.Cast<uint, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = Native.Cast<T, ulong>(ref a) - Native.Cast<T, ulong>(ref b);
                return Native.Cast<ulong, T>(ref value);
            }
            case TypeCode.Single:
            {
                var value = Native.Cast<T, float>(ref a) - Native.Cast<T, float>(ref b);
                return Native.Cast<float, T>(ref value);
            }
            case TypeCode.Double:
            {
                var value = Native.Cast<T, double>(ref a) - Native.Cast<T, double>(ref b);
                return Native.Cast<double, T>(ref value);
            }
            case TypeCode.Decimal:
            {
                var value = Native.Cast<T, decimal>(ref a) - Native.Cast<T, decimal>(ref b);
                return Native.Cast<decimal, T>(ref value);
            }
            default: return s_Sub.Invoke(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Multiply(T a, T b)
    {
        switch (s_TypeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            {
                var value = Native.Cast<T, int>(ref a) * Native.Cast<T, int>(ref b);
                return Native.Cast<int, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = Native.Cast<T, long>(ref a) * Native.Cast<T, long>(ref b);
                return Native.Cast<long, T>(ref value);
            }
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            {
                var value = Native.Cast<T, uint>(ref a) * Native.Cast<T, uint>(ref b);
                return Native.Cast<uint, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = Native.Cast<T, ulong>(ref a) * Native.Cast<T, ulong>(ref b);
                return Native.Cast<ulong, T>(ref value);
            }
            case TypeCode.Single:
            {
                var value = Native.Cast<T, float>(ref a) * Native.Cast<T, float>(ref b);
                return Native.Cast<float, T>(ref value);
            }
            case TypeCode.Double:
            {
                var value = Native.Cast<T, double>(ref a) * Native.Cast<T, double>(ref b);
                return Native.Cast<double, T>(ref value);
            }
            case TypeCode.Decimal:
            {
                var value = Native.Cast<T, decimal>(ref a) * Native.Cast<T, decimal>(ref b);
                return Native.Cast<decimal, T>(ref value);
            }
            default: return s_Mul.Invoke(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Divide(T a, T b)
    {
        switch (s_TypeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            {
                var value = Native.Cast<T, int>(ref a) / Native.Cast<T, int>(ref b);
                return Native.Cast<int, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = Native.Cast<T, long>(ref a) / Native.Cast<T, long>(ref b);
                return Native.Cast<long, T>(ref value);
            }
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            {
                var value = Native.Cast<T, uint>(ref a) / Native.Cast<T, uint>(ref b);
                return Native.Cast<uint, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = Native.Cast<T, ulong>(ref a) / Native.Cast<T, ulong>(ref b);
                return Native.Cast<ulong, T>(ref value);
            }
            case TypeCode.Single:
            {
                var value = Native.Cast<T, float>(ref a) / Native.Cast<T, float>(ref b);
                return Native.Cast<float, T>(ref value);
            }
            case TypeCode.Double:
            {
                var value = Native.Cast<T, double>(ref a) / Native.Cast<T, double>(ref b);
                return Native.Cast<double, T>(ref value);
            }
            case TypeCode.Decimal:
            {
                var value = Native.Cast<T, decimal>(ref a) / Native.Cast<T, decimal>(ref b);
                return Native.Cast<decimal, T>(ref value);
            }
            default: return s_Div.Invoke(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static T Modulo(T a, T b)
    {
        switch (s_TypeCode)
        {
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            {
                var value = Native.Cast<T, int>(ref a) % Native.Cast<T, int>(ref b);
                return Native.Cast<int, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = Native.Cast<T, long>(ref a) % Native.Cast<T, long>(ref b);
                return Native.Cast<long, T>(ref value);
            }
            case TypeCode.SByte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            {
                var value = Native.Cast<T, uint>(ref a) % Native.Cast<T, uint>(ref b);
                return Native.Cast<uint, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = Native.Cast<T, ulong>(ref a) % Native.Cast<T, ulong>(ref b);
                return Native.Cast<ulong, T>(ref value);
            }
            case TypeCode.Single:
            {
                var value = Native.Cast<T, float>(ref a) % Native.Cast<T, float>(ref b);
                return Native.Cast<float, T>(ref value);
            }
            case TypeCode.Double:
            {
                var value = Native.Cast<T, double>(ref a) % Native.Cast<T, double>(ref b);
                return Native.Cast<double, T>(ref value);
            }
            case TypeCode.Decimal:
            {
                var value = Native.Cast<T, decimal>(ref a) % Native.Cast<T, decimal>(ref b);
                return Native.Cast<decimal, T>(ref value);
            }
            default: return s_Mod.Invoke(a, b);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GreaterThan(T a, T b) => s_TypeCode switch
    {
        TypeCode.Byte or TypeCode.Int16 or TypeCode.Int32    => Native.Cast<T, int>(ref a) > Native.Cast<T, int>(ref b),
        TypeCode.Int64                                       => Native.Cast<T, long>(ref a) > Native.Cast<T, long>(ref b),
        TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 => Native.Cast<T, uint>(ref a) > Native.Cast<T, uint>(ref b),
        TypeCode.UInt64                                      => Native.Cast<T, ulong>(ref a) > Native.Cast<T, ulong>(ref b),
        TypeCode.Single                                      => Native.Cast<T, float>(ref a) > Native.Cast<T, float>(ref b),
        TypeCode.Double                                      => Native.Cast<T, double>(ref a) > Native.Cast<T, double>(ref b),
        TypeCode.Decimal                                     => Native.Cast<T, decimal>(ref a) > Native.Cast<T, decimal>(ref b),
        _                                                    => s_Gt?.Invoke(a, b) ?? throw new NotSupportedException($"Type of {typeof(T)} done not support addition operation.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool LessThan(T a, T b) => s_TypeCode switch
    {
        TypeCode.Byte or TypeCode.Int16 or TypeCode.Int32    => Native.Cast<T, int>(ref a) < Native.Cast<T, int>(ref b),
        TypeCode.Int64                                       => Native.Cast<T, long>(ref a) < Native.Cast<T, long>(ref b),
        TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 => Native.Cast<T, uint>(ref a) < Native.Cast<T, uint>(ref b),
        TypeCode.UInt64                                      => Native.Cast<T, ulong>(ref a) < Native.Cast<T, ulong>(ref b),
        TypeCode.Single                                      => Native.Cast<T, float>(ref a) < Native.Cast<T, float>(ref b),
        TypeCode.Double                                      => Native.Cast<T, double>(ref a) < Native.Cast<T, double>(ref b),
        TypeCode.Decimal                                     => Native.Cast<T, decimal>(ref a) < Native.Cast<T, decimal>(ref b),
        _                                                    => s_Lt?.Invoke(a, b) ?? throw new NotSupportedException($"Type of {typeof(T)} done not support addition operation.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool GreaterThanOrEquals(T a, T b) => s_TypeCode switch
    {
        TypeCode.Byte or TypeCode.Int16 or TypeCode.Int32    => Native.Cast<T, int>(ref a) >= Native.Cast<T, int>(ref b),
        TypeCode.Int64                                       => Native.Cast<T, long>(ref a) >= Native.Cast<T, long>(ref b),
        TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 => Native.Cast<T, uint>(ref a) >= Native.Cast<T, uint>(ref b),
        TypeCode.UInt64                                      => Native.Cast<T, ulong>(ref a) >= Native.Cast<T, ulong>(ref b),
        TypeCode.Single                                      => Native.Cast<T, float>(ref a) >= Native.Cast<T, float>(ref b),
        TypeCode.Double                                      => Native.Cast<T, double>(ref a) >= Native.Cast<T, double>(ref b),
        TypeCode.Decimal                                     => Native.Cast<T, decimal>(ref a) >= Native.Cast<T, decimal>(ref b),
        _                                                    => s_Ge?.Invoke(a, b) ?? throw new NotSupportedException($"Type of {typeof(T)} done not support addition operation.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool LessThanOrEquals(T a, T b) => s_TypeCode switch
    {
        TypeCode.Byte or TypeCode.Int16 or TypeCode.Int32    => Native.Cast<T, int>(ref a) <= Native.Cast<T, int>(ref b),
        TypeCode.Int64                                       => Native.Cast<T, long>(ref a) <= Native.Cast<T, long>(ref b),
        TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 => Native.Cast<T, uint>(ref a) <= Native.Cast<T, uint>(ref b),
        TypeCode.UInt64                                      => Native.Cast<T, ulong>(ref a) <= Native.Cast<T, ulong>(ref b),
        TypeCode.Single                                      => Native.Cast<T, float>(ref a) <= Native.Cast<T, float>(ref b),
        TypeCode.Double                                      => Native.Cast<T, double>(ref a) <= Native.Cast<T, double>(ref b),
        TypeCode.Decimal                                     => Native.Cast<T, decimal>(ref a) <= Native.Cast<T, decimal>(ref b),
        _                                                    => s_Le?.Invoke(a, b) ?? throw new NotSupportedException($"Type of {typeof(T)} done not support addition operation.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Equals(T a, T b) => EqualityComparer<T>.Default.Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool NotEquals(T a, T b) => !EqualityComparer<T>.Default.Equals(a, b);
}