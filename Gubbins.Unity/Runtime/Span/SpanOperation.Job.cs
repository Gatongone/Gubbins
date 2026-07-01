using System;
using Gubbins.Enhance;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine.Device;

namespace Gubbins.Span
{
    /// <summary>
    /// Job-scheduled int span operation.
    /// </summary>
    internal class JobIntOperation : ISpanNumberOperation<int>, ISpanShift<int>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct ShiftLeftJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] << Count;
            }
        }

        [BurstCompile]
        private struct ShiftRightJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Dst;
            public int              Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] >> Count;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<int> Left;
            public NativeArray<int> Right;
            public NativeArray<int> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<int> Left;
            public NativeArray<int> Right;
            public NativeArray<int> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Partials;
            public int              PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<int> Src;
            public NativeArray<int> Partials;
            public int              PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        public void Add(Span<int> src, int operand, Span<int> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<int> src, int operand, Span<int> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<int> src, int operand, Span<int> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<int> src, int operand, Span<int> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<int> src, int operand, Span<int> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void ShiftLeft(Span<int> src, int count, Span<int> result)
        {
            RunAndComplete(new ShiftLeftJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void ShiftRight(Span<int> src, int count, Span<int> result)
        {
            RunAndComplete(new ShiftRightJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void Max(Span<int> left, Span<int> right, Span<int> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public int GetMax(Span<int> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<int>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount,
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<int> left, Span<int> right, Span<int> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public int GetMin(Span<int> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<int>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }
    }

    /// <summary>
    /// Job-scheduled uint span operation.
    /// </summary>
    internal class JobUintOperation : ISpanNumberOperation<uint>, ISpanShift<uint>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public uint              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public uint              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public uint              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public uint              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public uint              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct ShiftLeftJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public int               Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] << Count;
            }
        }

        [BurstCompile]
        private struct ShiftRightJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Dst;
            public int               Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] >> Count;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<uint> Left;
            public NativeArray<uint> Right;
            public NativeArray<uint> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<uint> Left;
            public NativeArray<uint> Right;
            public NativeArray<uint> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Partials;
            public int               PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<uint> Src;
            public NativeArray<uint> Partials;
            public int               PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        public void Add(Span<uint> src, uint operand, Span<uint> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<uint> src, uint operand, Span<uint> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<uint> src, uint operand, Span<uint> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<uint> src, uint operand, Span<uint> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<uint> src, uint operand, Span<uint> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void ShiftLeft(Span<uint> src, int count, Span<uint> result)
        {
            RunAndComplete(new ShiftLeftJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void ShiftRight(Span<uint> src, int count, Span<uint> result)
        {
            RunAndComplete(new ShiftRightJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void Max(Span<uint> left, Span<uint> right, Span<uint> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public uint GetMax(Span<uint> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<uint>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount,
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<uint> left, Span<uint> right, Span<uint> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public uint GetMin(Span<uint> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<uint>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }
    }

    /// <summary>
    /// Job-scheduled long span operation.
    /// </summary>
    internal class JobLongOperation : ISpanNumberOperation<long>, ISpanShift<long>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public long              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public long              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public long              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public long              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public long              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct ShiftLeftJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public int               Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] << Count;
            }
        }

        [BurstCompile]
        private struct ShiftRightJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Dst;
            public int               Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] >> Count;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<long> Left;
            public NativeArray<long> Right;
            public NativeArray<long> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<long> Left;
            public NativeArray<long> Right;
            public NativeArray<long> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Partials;
            public int               PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<long> Src;
            public NativeArray<long> Partials;
            public int               PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        public void Add(Span<long> src, long operand, Span<long> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<long> src, long operand, Span<long> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<long> src, long operand, Span<long> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<long> src, long operand, Span<long> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<long> src, long operand, Span<long> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void ShiftLeft(Span<long> src, int count, Span<long> result)
        {
            RunAndComplete(new ShiftLeftJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void ShiftRight(Span<long> src, int count, Span<long> result)
        {
            RunAndComplete(new ShiftRightJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void Max(Span<long> left, Span<long> right, Span<long> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public long GetMax(Span<long> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<long>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount,
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<long> left, Span<long> right, Span<long> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public long GetMin(Span<long> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<long>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }
    }

    /// <summary>
    /// Job-scheduled ulong span operation.
    /// </summary>
    internal class JobUlongOperation : ISpanNumberOperation<ulong>, ISpanShift<ulong>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public ulong              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public ulong              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public ulong              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public ulong              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public ulong              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct ShiftLeftJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public int                Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] << Count;
            }
        }

        [BurstCompile]
        private struct ShiftRightJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Dst;
            public int                Count;

            public void Execute(int index)
            {
                Dst[index] = Src[index] >> Count;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<ulong> Left;
            public NativeArray<ulong> Right;
            public NativeArray<ulong> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<ulong> Left;
            public NativeArray<ulong> Right;
            public NativeArray<ulong> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Partials;
            public int                PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<ulong> Src;
            public NativeArray<ulong> Partials;
            public int                PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        public void Add(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<ulong> src, ulong operand, Span<ulong> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void ShiftLeft(Span<ulong> src, int count, Span<ulong> result)
        {
            RunAndComplete(new ShiftLeftJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void ShiftRight(Span<ulong> src, int count, Span<ulong> result)
        {
            RunAndComplete(new ShiftRightJob
            {
                Src   = src.AsNativeArray(),
                Dst   = result.AsNativeArray(),
                Count = count
            }, src.Length);
        }

        public void Max(Span<ulong> left, Span<ulong> right, Span<ulong> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public ulong GetMax(Span<ulong> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<ulong>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount,
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<ulong> left, Span<ulong> right, Span<ulong> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public ulong GetMin(Span<ulong> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<ulong>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }
    }

    /// <summary>
    /// Job-scheduled float span operation.
    /// </summary>
    internal class JobFloatOperation : ISpanNumberOperation<float>, ISpanRealOperation<float>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<float> Left;
            public NativeArray<float> Right;
            public NativeArray<float> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<float> Left;
            public NativeArray<float> Right;
            public NativeArray<float> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Partials;
            public int                PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Partials;
            public int                PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        [BurstCompile]
        private struct ClampJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Min;
            public NativeArray<float> Max;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.clamp(Src[index], Min[index], Max[index]);
            }
        }

        [BurstCompile]
        private struct ClampScalarJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Min;
            public float              Max;

            public void Execute(int index)
            {
                Dst[index] = math.clamp(Src[index], Min, Max);
            }
        }

        [BurstCompile]
        private struct LerpJob : IJobParallelFor
        {
            public NativeArray<float> X;
            public NativeArray<float> Y;
            public NativeArray<float> Amount;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.lerp(X[index], Y[index], Amount[index]);
            }
        }

        [BurstCompile]
        private struct LerpScalarYJob : IJobParallelFor
        {
            public NativeArray<float> X;
            public NativeArray<float> Amount;
            public NativeArray<float> Dst;
            public float              Y;

            public void Execute(int index)
            {
                Dst[index] = math.lerp(X[index], Y, Amount[index]);
            }
        }

        [BurstCompile]
        private struct HypotJob : IJobParallelFor
        {
            public NativeArray<float> X;
            public NativeArray<float> Y;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                var x = X[index];
                var y = Y[index];
                Dst[index] = math.sqrt(x * x + y * y);
            }
        }

        [BurstCompile]
        private struct HypotScalarYJob : IJobParallelFor
        {
            public NativeArray<float> X;
            public NativeArray<float> Dst;
            public float              YSquared;

            public void Execute(int index)
            {
                var x = X[index];
                Dst[index] = math.sqrt(x * x + YSquared);
            }
        }

        [BurstCompile]
        private struct PowScalarExponentJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;
            public float              Exponent;

            public void Execute(int index)
            {
                Dst[index] = math.pow(Src[index], Exponent);
            }
        }

        [BurstCompile]
        private struct PowJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Exponent;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.pow(Src[index], Exponent[index]);
            }
        }

        [BurstCompile]
        private struct SinJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sin(Src[index]);
            }
        }

        [BurstCompile]
        private struct CosJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.cos(Src[index]);
            }
        }

        [BurstCompile]
        private struct TanJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.tan(Src[index]);
            }
        }

        [BurstCompile]
        private struct SinhJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sinh(Src[index]);
            }
        }

        [BurstCompile]
        private struct CoshJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.cosh(Src[index]);
            }
        }

        [BurstCompile]
        private struct TanhJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.tanh(Src[index]);
            }
        }

        [BurstCompile]
        private struct AsinJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.asin(Src[index]);
            }
        }

        [BurstCompile]
        private struct AcosJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.acos(Src[index]);
            }
        }

        [BurstCompile]
        private struct AtanJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.atan(Src[index]);
            }
        }

        [BurstCompile]
        private struct AsinhJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = math.log(x + math.sqrt(x * x + 1f));
            }
        }

        [BurstCompile]
        private struct AcoshJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = math.log(x + math.sqrt((x - 1f) * (x + 1f)));
            }
        }

        [BurstCompile]
        private struct AtanhJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = 0.5f * math.log((1f + x) / (1f - x));
            }
        }

        [BurstCompile]
        private struct SqrtJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sqrt(Src[index]);
            }
        }

        [BurstCompile]
        private struct RoundJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.round(Src[index]);
            }
        }

        [BurstCompile]
        private struct ExpJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.exp(Src[index]);
            }
        }

        [BurstCompile]
        private struct LogJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.log(Src[index]);
            }
        }

        [BurstCompile]
        private struct FloorJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.floor(Src[index]);
            }
        }

        [BurstCompile]
        private struct CeilingJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.ceil(Src[index]);
            }
        }

        [BurstCompile]
        private struct TruncateJob : IJobParallelFor
        {
            public NativeArray<float> Src;
            public NativeArray<float> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.trunc(Src[index]);
            }
        }

        public void Add(Span<float> src, float operand, Span<float> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<float> src, float operand, Span<float> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<float> src, float operand, Span<float> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<float> src, float operand, Span<float> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<float> src, float operand, Span<float> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Max(Span<float> left, Span<float> right, Span<float> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public float GetMax(Span<float> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<float>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<float> left, Span<float> right, Span<float> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public float GetMin(Span<float> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<float>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Clamp(Span<float> src, Span<float> min, Span<float> max, Span<float> result)
        {
            RunAndComplete(new ClampJob
            {
                Src = src.AsNativeArray(),
                Min = min.AsNativeArray(),
                Max = max.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Clamp(Span<float> src, float min, float max, Span<float> result)
        {
            RunAndComplete(new ClampScalarJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray(),
                Min = min,
                Max = max
            }, src.Length);
        }

        public void Lerp(Span<float> x, Span<float> y, Span<float> amount, Span<float> result)
        {
            RunAndComplete(new LerpJob
            {
                X      = x.AsNativeArray(),
                Y      = y.AsNativeArray(),
                Amount = amount.AsNativeArray(),
                Dst    = result.AsNativeArray()
            }, x.Length);
        }

        public void Lerp(Span<float> x, float y, Span<float> amount, Span<float> result)
        {
            RunAndComplete(new LerpScalarYJob
            {
                X      = x.AsNativeArray(),
                Amount = amount.AsNativeArray(),
                Dst    = result.AsNativeArray(),
                Y      = y
            }, x.Length);
        }

        public void Hypot(Span<float> x, Span<float> y, Span<float> result)
        {
            RunAndComplete(new HypotJob
            {
                X   = x.AsNativeArray(),
                Y   = y.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, x.Length);
        }

        public void Hypot(Span<float> x, float y, Span<float> result)
        {
            RunAndComplete(new HypotScalarYJob
            {
                X        = x.AsNativeArray(),
                Dst      = result.AsNativeArray(),
                YSquared = y * y
            }, x.Length);
        }

        public void Pow(Span<float> src, float exponent, Span<float> result)
        {
            RunAndComplete(new PowScalarExponentJob
            {
                Src      = src.AsNativeArray(),
                Dst      = result.AsNativeArray(),
                Exponent = exponent
            }, src.Length);
        }

        public void Pow(Span<float> src, Span<float> exponent, Span<float> result)
        {
            RunAndComplete(new PowJob
            {
                Src      = src.AsNativeArray(),
                Exponent = exponent.AsNativeArray(),
                Dst      = result.AsNativeArray()
            }, src.Length);
        }

        public void Sin(Span<float> src, Span<float> result)
        {
            RunAndComplete(new SinJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Cos(Span<float> src, Span<float> result)
        {
            RunAndComplete(new CosJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Tan(Span<float> src, Span<float> result)
        {
            RunAndComplete(new TanJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Sinh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new SinhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Cosh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new CoshJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Tanh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new TanhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Asin(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AsinJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Acos(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AcosJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Atan(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AtanJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Asinh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AsinhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Acosh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AcoshJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Atanh(Span<float> src, Span<float> result)
        {
            RunAndComplete(new AtanhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Sqrt(Span<float> src, Span<float> result)
        {
            RunAndComplete(new SqrtJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Round(Span<float> src, Span<float> result)
        {
            RunAndComplete(new RoundJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Exp(Span<float> src, Span<float> result)
        {
            RunAndComplete(new ExpJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Log(Span<float> src, Span<float> result)
        {
            RunAndComplete(new LogJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Floor(Span<float> src, Span<float> result)
        {
            RunAndComplete(new FloorJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Ceiling(Span<float> src, Span<float> result)
        {
            RunAndComplete(new CeilingJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Truncate(Span<float> src, Span<float> result)
        {
            RunAndComplete(new TruncateJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }
    }

    /// <summary>
    /// Job-scheduled double span operation.
    /// </summary>
    internal class JobDoubleOperation : ISpanNumberOperation<double>, ISpanRealOperation<double>
    {
        private static readonly int s_JobBatchSize = SystemInfo.processorCount;

        public virtual bool Supported => JobsUtility.JobWorkerCount > 0;

        [BurstCompile]
        private struct AddJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] + Operand;
            }
        }

        [BurstCompile]
        private struct SubtractJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] - Operand;
            }
        }

        [BurstCompile]
        private struct MultiplyJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] * Operand;
            }
        }

        [BurstCompile]
        private struct DivideJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] / Operand;
            }
        }

        [BurstCompile]
        private struct ModuloJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Operand;

            public void Execute(int index)
            {
                Dst[index] = Src[index] % Operand;
            }
        }

        [BurstCompile]
        private struct MaxJob : IJobParallelFor
        {
            public NativeArray<double> Left;
            public NativeArray<double> Right;
            public NativeArray<double> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Max(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct MinJob : IJobParallelFor
        {
            public NativeArray<double> Left;
            public NativeArray<double> Right;
            public NativeArray<double> Result;

            public void Execute(int index)
            {
                Result[index] = Math.Min(Left[index], Right[index]);
            }
        }

        [BurstCompile]
        private struct GetMaxJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Partials;
            public int                 PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.max(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        [BurstCompile]
        private struct GetMinJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Partials;
            public int                 PartitionCount;

            public void Execute(int index)
            {
                var start = Src.Length * index / PartitionCount;
                var end = Src.Length * (index + 1) / PartitionCount;
                var value = Src[start];

                for (var i = start + 1; i < end; i++)
                {
                    value = math.min(value, Src[i]);
                }

                Partials[index] = value;
            }
        }

        private static void RunAndComplete<T>(T job, int length) where T : struct, IJobParallelFor
        {
            job.Schedule(length, s_JobBatchSize).Complete();
        }

        [BurstCompile]
        private struct ClampJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Min;
            public NativeArray<double> Max;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.clamp(Src[index], Min[index], Max[index]);
            }
        }

        [BurstCompile]
        private struct ClampScalarJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Min;
            public double              Max;

            public void Execute(int index)
            {
                Dst[index] = math.clamp(Src[index], Min, Max);
            }
        }

        [BurstCompile]
        private struct LerpJob : IJobParallelFor
        {
            public NativeArray<double> X;
            public NativeArray<double> Y;
            public NativeArray<double> Amount;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.lerp(X[index], Y[index], Amount[index]);
            }
        }

        [BurstCompile]
        private struct LerpScalarYJob : IJobParallelFor
        {
            public NativeArray<double> X;
            public NativeArray<double> Amount;
            public NativeArray<double> Dst;
            public double              Y;

            public void Execute(int index)
            {
                Dst[index] = math.lerp(X[index], Y, Amount[index]);
            }
        }

        [BurstCompile]
        private struct HypotJob : IJobParallelFor
        {
            public NativeArray<double> X;
            public NativeArray<double> Y;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                var x = X[index];
                var y = Y[index];
                Dst[index] = math.sqrt(x * x + y * y);
            }
        }

        [BurstCompile]
        private struct HypotScalarYJob : IJobParallelFor
        {
            public NativeArray<double> X;
            public NativeArray<double> Dst;
            public double              YSquared;

            public void Execute(int index)
            {
                var x = X[index];
                Dst[index] = math.sqrt(x * x + YSquared);
            }
        }

        [BurstCompile]
        private struct PowScalarExponentJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;
            public double              Exponent;

            public void Execute(int index)
            {
                Dst[index] = math.pow(Src[index], Exponent);
            }
        }

        [BurstCompile]
        private struct PowJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Exponent;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.pow(Src[index], Exponent[index]);
            }
        }

        [BurstCompile]
        private struct SinJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sin(Src[index]);
            }
        }

        [BurstCompile]
        private struct CosJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.cos(Src[index]);
            }
        }

        [BurstCompile]
        private struct TanJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.tan(Src[index]);
            }
        }

        [BurstCompile]
        private struct SinhJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sinh(Src[index]);
            }
        }

        [BurstCompile]
        private struct CoshJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.cosh(Src[index]);
            }
        }

        [BurstCompile]
        private struct TanhJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.tanh(Src[index]);
            }
        }

        [BurstCompile]
        private struct AsinJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.asin(Src[index]);
            }
        }

        [BurstCompile]
        private struct AcosJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.acos(Src[index]);
            }
        }

        [BurstCompile]
        private struct AtanJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.atan(Src[index]);
            }
        }

        [BurstCompile]
        private struct AsinhJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = math.log(x + math.sqrt(x * x + 1f));
            }
        }

        [BurstCompile]
        private struct AcoshJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = math.log(x + math.sqrt((x - 1f) * (x + 1f)));
            }
        }

        [BurstCompile]
        private struct AtanhJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                var x = Src[index];
                Dst[index] = 0.5f * math.log((1f + x) / (1f - x));
            }
        }

        [BurstCompile]
        private struct SqrtJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.sqrt(Src[index]);
            }
        }

        [BurstCompile]
        private struct RoundJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.round(Src[index]);
            }
        }

        [BurstCompile]
        private struct ExpJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.exp(Src[index]);
            }
        }

        [BurstCompile]
        private struct LogJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.log(Src[index]);
            }
        }

        [BurstCompile]
        private struct FloorJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.floor(Src[index]);
            }
        }

        [BurstCompile]
        private struct CeilingJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.ceil(Src[index]);
            }
        }

        [BurstCompile]
        private struct TruncateJob : IJobParallelFor
        {
            public NativeArray<double> Src;
            public NativeArray<double> Dst;

            public void Execute(int index)
            {
                Dst[index] = math.trunc(Src[index]);
            }
        }

        public void Add(Span<double> src, double operand, Span<double> result)
        {
            RunAndComplete(new AddJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Subtract(Span<double> src, double operand, Span<double> result)
        {
            RunAndComplete(new SubtractJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Multiply(Span<double> src, double operand, Span<double> result)
        {
            RunAndComplete(new MultiplyJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Divide(Span<double> src, double operand, Span<double> result)
        {
            RunAndComplete(new DivideJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Modulo(Span<double> src, double operand, Span<double> result)
        {
            RunAndComplete(new ModuloJob
            {
                Src     = src.AsNativeArray(),
                Dst     = result.AsNativeArray(),
                Operand = operand
            }, src.Length);
        }

        public void Max(Span<double> left, Span<double> right, Span<double> result)
        {
            RunAndComplete(new MaxJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public double GetMax(Span<double> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<double>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMaxJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var max = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    max = Math.Max(max, partials[i]);

                return max;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Min(Span<double> left, Span<double> right, Span<double> result)
        {
            RunAndComplete(new MinJob
            {
                Left   = left.AsNativeArray(),
                Right  = right.AsNativeArray(),
                Result = result.AsNativeArray()
            }, left.Length);
        }

        public double GetMin(Span<double> src)
        {
            if (src.Length == 0)
                throw new ArgumentException("Source span must not be empty.", nameof(src));

            var srcArray = src.AsNativeArray();
            var partitionCount = Math.Min(src.Length, Math.Max(1, s_JobBatchSize));
            var partials = new NativeArray<double>(partitionCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new GetMinJob
                {
                    Src            = srcArray,
                    Partials       = partials,
                    PartitionCount = partitionCount
                };

                job.Schedule(partitionCount, 1).Complete();

                var min = partials[0];
                for (var i = 1; i < partitionCount; i++)
                    min = Math.Min(min, partials[i]);

                return min;
            }
            finally
            {
                if (partials.IsCreated) partials.Dispose();
            }
        }

        public void Clamp(Span<double> src, Span<double> min, Span<double> max, Span<double> result)
        {
            RunAndComplete(new ClampJob
            {
                Src = src.AsNativeArray(),
                Min = min.AsNativeArray(),
                Max = max.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Clamp(Span<double> src, double min, double max, Span<double> result)
        {
            RunAndComplete(new ClampScalarJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray(),
                Min = min,
                Max = max
            }, src.Length);
        }

        public void Lerp(Span<double> x, Span<double> y, Span<double> amount, Span<double> result)
        {
            RunAndComplete(new LerpJob
            {
                X      = x.AsNativeArray(),
                Y      = y.AsNativeArray(),
                Amount = amount.AsNativeArray(),
                Dst    = result.AsNativeArray()
            }, x.Length);
        }

        public void Lerp(Span<double> x, double y, Span<double> amount, Span<double> result)
        {
            RunAndComplete(new LerpScalarYJob
            {
                X      = x.AsNativeArray(),
                Amount = amount.AsNativeArray(),
                Dst    = result.AsNativeArray(),
                Y      = y
            }, x.Length);
        }

        public void Hypot(Span<double> x, Span<double> y, Span<double> result)
        {
            RunAndComplete(new HypotJob
            {
                X   = x.AsNativeArray(),
                Y   = y.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, x.Length);
        }

        public void Hypot(Span<double> x, double y, Span<double> result)
        {
            RunAndComplete(new HypotScalarYJob
            {
                X        = x.AsNativeArray(),
                Dst      = result.AsNativeArray(),
                YSquared = y * y
            }, x.Length);
        }

        public void Pow(Span<double> src, double exponent, Span<double> result)
        {
            RunAndComplete(new PowScalarExponentJob
            {
                Src      = src.AsNativeArray(),
                Dst      = result.AsNativeArray(),
                Exponent = exponent
            }, src.Length);
        }

        public void Pow(Span<double> src, Span<double> exponent, Span<double> result)
        {
            RunAndComplete(new PowJob
            {
                Src      = src.AsNativeArray(),
                Exponent = exponent.AsNativeArray(),
                Dst      = result.AsNativeArray()
            }, src.Length);
        }

        public void Sin(Span<double> src, Span<double> result)
        {
            RunAndComplete(new SinJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Cos(Span<double> src, Span<double> result)
        {
            RunAndComplete(new CosJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Tan(Span<double> src, Span<double> result)
        {
            RunAndComplete(new TanJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Sinh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new SinhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Cosh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new CoshJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Tanh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new TanhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Asin(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AsinJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Acos(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AcosJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Atan(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AtanJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Asinh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AsinhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Acosh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AcoshJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Atanh(Span<double> src, Span<double> result)
        {
            RunAndComplete(new AtanhJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Sqrt(Span<double> src, Span<double> result)
        {
            RunAndComplete(new SqrtJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Round(Span<double> src, Span<double> result)
        {
            RunAndComplete(new RoundJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Exp(Span<double> src, Span<double> result)
        {
            RunAndComplete(new ExpJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Log(Span<double> src, Span<double> result)
        {
            RunAndComplete(new LogJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Floor(Span<double> src, Span<double> result)
        {
            RunAndComplete(new FloorJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Ceiling(Span<double> src, Span<double> result)
        {
            RunAndComplete(new CeilingJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }

        public void Truncate(Span<double> src, Span<double> result)
        {
            RunAndComplete(new TruncateJob
            {
                Src = src.AsNativeArray(),
                Dst = result.AsNativeArray()
            }, src.Length);
        }
    }
}