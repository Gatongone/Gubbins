using System;
using Unity.Burst;
using UnityEngine;

namespace Gubbins.Unity
{
    namespace Gubbins.Enhance
    {
        [BurstCompile]
        public sealed class BurstUshortOperation : IBatchOperation<ushort>
        {
            /// <inheritdoc />
            public bool Supported => BurstCompiler.IsEnabled;

            /// <inheritdoc/>
            [BurstCompile]
            public void Add(Span<ushort> src, ushort operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] += operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Subtract(Span<ushort> src, ushort operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] -= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Multiply(Span<ushort> src, ushort operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] *= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Divide(Span<ushort> src, ushort operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] /= operand;
                }
            }

            /// <inheritdoc/>
            [BurstCompile]
            public void Modulo(Span<ushort> src, ushort operand)
            {
                for (var i = 0; i < src.Length; i++)
                {
                    src[i] %= operand;
                }
            }
        }
    }
}