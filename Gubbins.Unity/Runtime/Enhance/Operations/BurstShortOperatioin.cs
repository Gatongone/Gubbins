using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstShortOperation : IBatchOperation<short>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile]
        public void Add(Span<short> src, short operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Subtract(Span<short> src, short operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Multiply(Span<short> src, short operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Divide(Span<short> src, short operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void Modulo(Span<short> src, short operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }
}