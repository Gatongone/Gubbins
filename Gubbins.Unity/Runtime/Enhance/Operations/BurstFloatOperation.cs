using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    internal sealed class BurstFloatOperation : IBatchOperation<float>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile]
        public void Add(Span<float> src, float operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Subtract(Span<float> src, float operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Multiply(Span<float> src, float operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Divide(Span<float> src, float operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Modulo(Span<float> src, float operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }
}