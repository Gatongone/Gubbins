using System;
using Unity.Burst;

namespace Gubbins.Unity
{
    namespace Gubbins.Enhance
    {
        [BurstCompile]
        public sealed class BurstUlongOperation : IBatchOperation<ulong>
        {
            /// <inheritdoc />
            public bool Supported => BurstCompiler.IsEnabled;

            /// <inheritdoc/>
            [BurstCompile]
            public void Add(Span<ulong> src, ulong operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] += operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Subtract(Span<ulong> src, ulong operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] -= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Multiply(Span<ulong> src, ulong operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] *= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Divide(Span<ulong> src, ulong operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] /= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Modulo(Span<ulong> src, ulong operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] %= operand;
                }
            }
        }
    }
}