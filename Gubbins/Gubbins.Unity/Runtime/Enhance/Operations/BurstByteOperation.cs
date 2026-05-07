using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstByteOperation : IBatchOperation<byte>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile]
        public void Add(Span<byte> src, byte operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Subtract(Span<byte> src, byte operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Multiply(Span<byte> src, byte operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Divide(Span<byte> src, byte operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Modulo(Span<byte> src, byte operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }
}