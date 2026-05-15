using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Collections;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstIntOperation : IBatchOperation<int>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<int> src, int operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] += operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<int> src, int operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] -= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<int> src, int operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] *= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<int> src, int operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] /= operand;
            }
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<int> src, int operand)
        {
            for (var i = 0; i < src.Length; i++)
            {
                src[i] %= operand;
            }
        }
    }

    [BurstCompile]
    public sealed class BurstJobIntOperation : IBatchOperation<int>
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled;

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<int> src, int operand)
        {
            var job = new AddJob {Src = src.ToNativeArray(Allocator.TempJob), Operand = operand};
            job.Schedule(src.Length, Environment.ProcessorCount).Complete();
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<int> src, int operand)
        {
            var job = new SubtractJob {Src = src.ToNativeArray(Allocator.TempJob), Operand = operand};
            job.Schedule(src.Length, Environment.ProcessorCount).Complete();
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<int> src, int operand)
        {
            var job = new MultiplyJob {Src = src.ToNativeArray(Allocator.TempJob), Operand = operand};
            job.Schedule(src.Length, Environment.ProcessorCount).Complete();
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<int> src, int operand)
        {
            var job = new DivideJob {Src = src.ToNativeArray(Allocator.TempJob), Operand = operand};
            job.Schedule(src.Length, Environment.ProcessorCount).Complete();
        }

        /// <inheritdoc/>
        [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<int> src, int operand)
        {
            var job = new ModuloJob {Src = src.ToNativeArray(Allocator.TempJob), Operand = operand};
            job.Schedule(src.Length, Environment.ProcessorCount).Complete();
        }

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<int> Src;

            [ReadOnly] public int Operand;

            /// <inheritdoc />
            [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int index) => Src[index] += Operand;
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<int> Src;

            [ReadOnly] public int Operand;

            /// <inheritdoc />
            [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int index) => Src[index] -= Operand;
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<int> Src;

            [ReadOnly] public int Operand;

            /// <inheritdoc />
            [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int index) => Src[index] *= Operand;
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<int> Src;

            [ReadOnly] public int Operand;

            /// <inheritdoc />
            [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int index) => Src[index] /= Operand;
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<int> Src;

            [ReadOnly] public int Operand;

            /// <inheritdoc />
            [BurstCompile, MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Execute(int index) => Src[index] %= Operand;
        }
    }
}