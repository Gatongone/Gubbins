using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstDecimalOperation : IBatchOperation<decimal>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile]
        public void Add(Span<decimal> src, decimal operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Subtract(Span<decimal> src, decimal operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Multiply(Span<decimal> src, decimal operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Divide(Span<decimal> src, decimal operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Modulo(Span<decimal> src, decimal operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }
}