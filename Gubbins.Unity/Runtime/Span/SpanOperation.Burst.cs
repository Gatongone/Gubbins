using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Gubbins.Span
{
    [BurstCompile]
    public unsafe class BurstUlongOperations : ISpanNumberOperations<ulong>, ISpanGetMax<ulong>, ISpanGetMin<ulong>, ISpanShiftLeft<ulong>, ISpanShiftRight<ulong>
    {
        /// <inheritdoc/>
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstShiftLeft(pSrc, count, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstShiftRight(pSrc, count, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstAdd(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstSubtract(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstMultiply(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstDivide(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            fixed (ulong* pSrc = src, pResult = result)
                BurstModulo(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Max(Span<ulong> left, Span<ulong> right, Span<ulong> result)
        {
            fixed (ulong* pLeft = left, pRight = right, pResult = result)
                BurstMax(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Min(Span<ulong> left, Span<ulong> right, Span<ulong> result)
        {
            fixed (ulong* pLeft = left, pRight = right, pResult = result)
                BurstMin(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetMax(Span<ulong> src)
        {
            fixed (ulong* pSrc = src)
                return BurstGetMax(pSrc, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetMin(Span<ulong> src)
        {
            fixed (ulong* pSrc = src)
                return BurstGetMin(pSrc, src.Length);
        }

        [BurstCompile]
        private static void BurstAdd(in ulong* src, ulong operand, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] + operand;
            }
        }

        [BurstCompile]
        private static void BurstSubtract(in ulong* src, ulong operand, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] - operand;
            }
        }

        [BurstCompile]
        private static void BurstMultiply(in ulong* src, ulong operand, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] * operand;
            }
        }

        [BurstCompile]
        private static void BurstDivide(in ulong* src, ulong operand, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] / operand;
            }
        }

        [BurstCompile]
        private static void BurstModulo(in ulong* src, ulong operand, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] % operand;
            }
        }

        [BurstCompile]
        public static void BurstMax(in ulong* left, in ulong* right, ulong* result, int length)
        {
            for (var index = 0; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.max(l, r);
            }
        }

        [BurstCompile]
        private static ulong BurstGetMax(in ulong* src, int length)
        {
            var max = src[0];
            for (var index = 0; index < length; index++)
            {
                var cur = src[index];
                max = math.max(max, cur);
            }

            return max;
        }

        [BurstCompile]
        private static void BurstMin(in ulong* left, in ulong* right, ulong* result, int length)
        {
            for (var index = 1; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.min(l, r);
            }
        }

        [BurstCompile]
        private static ulong BurstGetMin(in ulong* src, int length)
        {
            var min = src[0];
            for (var index = 1; index < length; index++)
            {
                var cur = src[index];
                min = math.min(min, cur);
            }

            return min;
        }

        [BurstCompile]
        private static void BurstShiftLeft(in ulong* src, int count, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] << count;
            }
        }

        [BurstCompile]
        private static void BurstShiftRight(in ulong* src, int count, ulong* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] >> count;
            }
        }
    }
    
    [BurstCompile]
    public unsafe class BurstFloatOperations : ISpanNumberOperations<float>, ISpanGetMax<float>, ISpanGetMin<float>, ISpanRealOperations<float>
    {
        /// <inheritdoc/>
        public bool Supported => BurstCompiler.IsEnabled;

        public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
        {
            fixed (float* pSrc = src, pMin = min, pMax = max, pResult = result)
                BurstClamp(pSrc, pMin, pMax, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clamp(Span<float> src, float min, float max, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstClamp(pSrc, min, max, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
        {
            fixed (float* pX = x, pY = y, pAmount = amount, pResult = result)
                BurstLerp(pX, pY, pAmount, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
        {
            fixed (float* pX = x, pAmount = amount, pResult = result)
                BurstLerp(pX, y, pAmount, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hypot(Span<float> x, Span<float> y, Span<float> result)
        {
            fixed (float* pX = x, pY = y, pResult = result)
                BurstHypot(pX, pY, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hypot(Span<float> x, float y, Span<float> result)
        {
            fixed (float* pX = x, pResult = result)
                BurstHypot(pX, y, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pow(Span<float> src, float exponent, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstPow(pSrc, exponent, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pow(Span<float> src, Span<float> exponent, Span<float> result)
        {
            fixed (float* pSrc = src, pExp = exponent, pResult = result)
                BurstPow(pSrc, pExp, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sin(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstSin(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cos(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstCos(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tan(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstTan(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sinh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstSinh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cosh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstCosh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tanh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstTanh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Asin(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAsin(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acos(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAcos(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Atan(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAtan(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Asinh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAsinh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acosh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAcosh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Atanh(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAtanh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sqrt(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstSqrt(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Round(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstRound(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exp(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstExp(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstLog(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Floor(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstFloor(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ceiling(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstCeiling(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Truncate(Span<float> src, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstTruncate(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<float> src, float operand, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstAdd(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<float> src, float operand, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstSubtract(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<float> src, float operand, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstMultiply(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<float> src, float operand, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstDivide(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<float> src, float operand, Span<float> result)
        {
            fixed (float* pSrc = src, pResult = result)
                BurstModulo(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Max(Span<float> left, Span<float> right, Span<float> result)
        {
            fixed (float* pLeft = left, pRight = right, pResult = result)
                BurstMax(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Min(Span<float> left, Span<float> right, Span<float> result)
        {
            fixed (float* pLeft = left, pRight = right, pResult = result)
                BurstMin(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMax(Span<float> src)
        {
            fixed (float* pSrc = src)
                return BurstGetMax(pSrc, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetMin(Span<float> src)
        {
            fixed (float* pSrc = src)
                return BurstGetMin(pSrc, src.Length);
        }

        [BurstCompile]
        private static void BurstClamp(in float* src, in float* min, in float* max, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.clamp(src[i], min[i], max[i]);
            }
        }

        [BurstCompile]
        private static void BurstClamp(in float* src, float min, float max, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.clamp(src[i], min, max);
            }
        }

        [BurstCompile]
        private static void BurstLerp(in float* x, in float* y, in float* amount, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.lerp(x[i], y[i], amount[i]);
            }
        }

        [BurstCompile]
        private static void BurstLerp(in float* x, float y, in float* amount, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.lerp(x[i], y, amount[i]);
            }
        }

        [BurstCompile]
        private static void BurstHypot(in float* x, in float* y, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(math.mul(x[i], x[i]) + math.mul(y[i], y[i]));
            }
        }

        [BurstCompile]
        private static void BurstHypot(in float* x, float y, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(math.mul(x[i], x[i]) + math.mul(y, y));
            }
        }

        [BurstCompile]
        private static void BurstPow(in float* src, float exponent, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.pow(src[i], exponent);
            }
        }

        [BurstCompile]
        private static void BurstPow(in float* src, in float* exponent, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.pow(src[i], exponent[i]);
            }
        }

        [BurstCompile]
        private static void BurstSin(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sin(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCos(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.cos(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTan(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.tan(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstSinh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sinh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCosh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.cosh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTanh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.tanh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAsin(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.asin(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAcos(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.acos(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAtan(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.atan(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAsinh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = math.log(x + math.sqrt(x * x + 1f));
            }
        }

        [BurstCompile]
        private static void BurstAcosh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = math.log(x + math.sqrt((x - 1f) * (x + 1f)));
            }
        }

        [BurstCompile]
        private static void BurstAtanh(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = 0.5f * math.log((1f + x) / (1f - x));
            }
        }

        [BurstCompile]
        private static void BurstSqrt(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstRound(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.round(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstExp(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.exp(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstLog(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.log(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstFloor(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.floor(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCeiling(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.ceil(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTruncate(in float* src, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.trunc(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAdd(in float* src, float operand, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] + operand;
            }
        }

        [BurstCompile]
        private static void BurstSubtract(in float* src, float operand, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] - operand;
            }
        }

        [BurstCompile]
        private static void BurstMultiply(in float* src, float operand, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] * operand;
            }
        }

        [BurstCompile]
        private static void BurstDivide(in float* src, float operand, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] / operand;
            }
        }

        [BurstCompile]
        private static void BurstModulo(in float* src, float operand, float* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] % operand;
            }
        }

        [BurstCompile]
        public static void BurstMax(in float* left, in float* right, float* result, int length)
        {
            for (var index = 0; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.max(l, r);
            }
        }

        [BurstCompile]
        private static float BurstGetMax(in float* src, int length)
        {
            var max = src[0];
            for (var index = 0; index < length; index++)
            {
                var cur = src[index];
                max = math.max(max, cur);
            }

            return max;
        }

        [BurstCompile]
        private static void BurstMin(in float* left, in float* right, float* result, int length)
        {
            for (var index = 1; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.min(l, r);
            }
        }

        [BurstCompile]
        private static float BurstGetMin(in float* src, int length)
        {
            var min = src[0];
            for (var index = 1; index < length; index++)
            {
                var cur = src[index];
                min = math.min(min, cur);
            }

            return min;
        }
    }

    [BurstCompile]
    public unsafe class BurstDoubleOperations : ISpanNumberOperations<double>, ISpanGetMax<double>, ISpanGetMin<double>, ISpanRealOperations<double>
    {
        /// <inheritdoc/>
        public bool Supported => BurstCompiler.IsEnabled;

        public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
        {
            fixed (double* pSrc = src, pMin = min, pMax = max, pResult = result)
                BurstClamp(pSrc, pMin, pMax, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clamp(Span<double> src, double min, double max, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstClamp(pSrc, min, max, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
        {
            fixed (double* pX = x, pY = y, pAmount = amount, pResult = result)
                BurstLerp(pX, pY, pAmount, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
        {
            fixed (double* pX = x, pAmount = amount, pResult = result)
                BurstLerp(pX, y, pAmount, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hypot(Span<double> x, Span<double> y, Span<double> result)
        {
            fixed (double* pX = x, pY = y, pResult = result)
                BurstHypot(pX, pY, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Hypot(Span<double> x, double y, Span<double> result)
        {
            fixed (double* pX = x, pResult = result)
                BurstHypot(pX, y, pResult, x.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pow(Span<double> src, double exponent, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstPow(pSrc, exponent, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pow(Span<double> src, Span<double> exponent, Span<double> result)
        {
            fixed (double* pSrc = src, pExp = exponent, pResult = result)
                BurstPow(pSrc, pExp, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sin(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstSin(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cos(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstCos(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tan(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstTan(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sinh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstSinh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Cosh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstCosh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Tanh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstTanh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Asin(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAsin(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acos(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAcos(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Atan(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAtan(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Asinh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAsinh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acosh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAcosh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Atanh(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAtanh(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Sqrt(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstSqrt(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Round(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstRound(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Exp(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstExp(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstLog(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Floor(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstFloor(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Ceiling(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstCeiling(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Truncate(Span<double> src, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstTruncate(pSrc, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<double> src, double operand, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstAdd(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<double> src, double operand, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstSubtract(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<double> src, double operand, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstMultiply(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<double> src, double operand, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstDivide(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<double> src, double operand, Span<double> result)
        {
            fixed (double* pSrc = src, pResult = result)
                BurstModulo(pSrc, operand, pResult, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Max(Span<double> left, Span<double> right, Span<double> result)
        {
            fixed (double* pLeft = left, pRight = right, pResult = result)
                BurstMax(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Min(Span<double> left, Span<double> right, Span<double> result)
        {
            fixed (double* pLeft = left, pRight = right, pResult = result)
                BurstMin(pLeft, pRight, pResult, left.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetMax(Span<double> src)
        {
            fixed (double* pSrc = src)
                return BurstGetMax(pSrc, src.Length);
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetMin(Span<double> src)
        {
            fixed (double* pSrc = src)
                return BurstGetMin(pSrc, src.Length);
        }

        [BurstCompile]
        private static void BurstClamp(in double* src, in double* min, in double* max, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.clamp(src[i], min[i], max[i]);
            }
        }

        [BurstCompile]
        private static void BurstClamp(in double* src, double min, double max, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.clamp(src[i], min, max);
            }
        }

        [BurstCompile]
        private static void BurstLerp(in double* x, in double* y, in double* amount, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.lerp(x[i], y[i], amount[i]);
            }
        }

        [BurstCompile]
        private static void BurstLerp(in double* x, double y, in double* amount, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.lerp(x[i], y, amount[i]);
            }
        }

        [BurstCompile]
        private static void BurstHypot(in double* x, in double* y, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(math.mul(x[i], x[i]) + math.mul(y[i], y[i]));
            }
        }

        [BurstCompile]
        private static void BurstHypot(in double* x, double y, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(math.mul(x[i], x[i]) + math.mul(y, y));
            }
        }

        [BurstCompile]
        private static void BurstPow(in double* src, double exponent, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.pow(src[i], exponent);
            }
        }

        [BurstCompile]
        private static void BurstPow(in double* src, in double* exponent, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.pow(src[i], exponent[i]);
            }
        }

        [BurstCompile]
        private static void BurstSin(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sin(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCos(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.cos(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTan(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.tan(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstSinh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sinh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCosh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.cosh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTanh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.tanh(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAsin(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.asin(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAcos(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.acos(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAtan(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.atan(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAsinh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = math.log(x + math.sqrt(x * x + 1f));
            }
        }

        [BurstCompile]
        private static void BurstAcosh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = math.log(x + math.sqrt((x - 1f) * (x + 1f)));
            }
        }

        [BurstCompile]
        private static void BurstAtanh(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var x = src[i];
                result[i] = 0.5f * math.log((1f + x) / (1f - x));
            }
        }

        [BurstCompile]
        private static void BurstSqrt(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.sqrt(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstRound(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.round(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstExp(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.exp(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstLog(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.log(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstFloor(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.floor(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstCeiling(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.ceil(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstTruncate(in double* src, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = math.trunc(src[i]);
            }
        }

        [BurstCompile]
        private static void BurstAdd(in double* src, double operand, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] + operand;
            }
        }

        [BurstCompile]
        private static void BurstSubtract(in double* src, double operand, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] - operand;
            }
        }

        [BurstCompile]
        private static void BurstMultiply(in double* src, double operand, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] * operand;
            }
        }

        [BurstCompile]
        private static void BurstDivide(in double* src, double operand, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] / operand;
            }
        }

        [BurstCompile]
        private static void BurstModulo(in double* src, double operand, double* result, int length)
        {
            for (var i = 0; i < length; i++)
            {
                result[i] = src[i] % operand;
            }
        }

        [BurstCompile]
        public static void BurstMax(in double* left, in double* right, double* result, int length)
        {
            for (var index = 0; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.max(l, r);
            }
        }

        [BurstCompile]
        private static double BurstGetMax(in double* src, int length)
        {
            var max = src[0];
            for (var index = 0; index < length; index++)
            {
                var cur = src[index];
                max = math.max(max, cur);
            }

            return max;
        }

        [BurstCompile]
        private static void BurstMin(in double* left, in double* right, double* result, int length)
        {
            for (var index = 1; index < length; index++)
            {
                var l = left[index];
                var r = right[index];
                result[index] = math.min(l, r);
            }
        }

        [BurstCompile]
        private static double BurstGetMin(in double* src, int length)
        {
            var min = src[0];
            for (var index = 1; index < length; index++)
            {
                var cur = src[index];
                min = math.min(min, cur);
            }

            return min;
        }
    }
}