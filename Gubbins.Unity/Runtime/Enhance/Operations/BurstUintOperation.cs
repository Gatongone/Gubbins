using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstUintOperation : IBatchOperation<uint>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile]
        public void Add(Span<uint> src, uint operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Subtract(Span<uint> src, uint operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Multiply(Span<uint> src, uint operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Divide(Span<uint> src, uint operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Modulo(Span<uint> src, uint operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }
}