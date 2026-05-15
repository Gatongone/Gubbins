using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Gubbins.Unity
{
    public class GpuOperation<T> : IBatchOperation<T> where T : unmanaged
    {
        private const string VAR_BATCHSIZE = "BatchSize";
        private const string VAR_INTSRC    = "IntBatchSrc";
        private const string VAR_FLOATSRC  = "FloatBatchSrc";
        private const string SHADER_NAME   = "BatchOperations";

        private static readonly ComputeShader s_Shader      = Resources.Load<ComputeShader>(SHADER_NAME);
        private static readonly TypeCode      s_TypeCode    = Type.GetTypeCode(typeof(T));
        private static readonly int           s_ElementSize = (int) Native.GetStackSize<T>();
        private static readonly int           s_IDBatchSize = Shader.PropertyToID(VAR_BATCHSIZE);
        private static readonly int           s_IDIntSrc    = Shader.PropertyToID(VAR_INTSRC);
        private static readonly int           s_IDFloatSrc  = Shader.PropertyToID(VAR_FLOATSRC);

        public bool Supported => s_Shader != null && (typeof(T) == typeof(uint) || typeof(T) == typeof(int) || typeof(T) == typeof(float));

        public void Add(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Add, src, operand);
        public void Subtract(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Subtract, src, operand);
        public void Multiply(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Multiply, src, operand);
        public void Modulo(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Modulo, src, operand);
        public void Divide(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Divide, src, operand);
        public void Pow(Span<T> src, T operand) => ExecuteBinaryOp(Operations.Pow, src, operand);

        private static int GetSrcID() => s_TypeCode switch
        {
            TypeCode.Int32 or TypeCode.UInt32 => s_IDIntSrc,
            TypeCode.Single => s_IDFloatSrc
        };

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExecuteBinaryOp(Operations operation, Span<T> src, T operand)
        {
            if (s_Shader == null) return;

            var kernel = s_Shader.FindKernel(operation.GetKernelName<T>());
            if (kernel < 0) return;
            var length = src.Length;
            var srcBuffer = new ComputeBuffer(length, s_ElementSize);
            srcBuffer.SetData(src);

            s_Shader.SetBuffer(kernel, GetSrcID(), srcBuffer);
            SetOperand(operation, operand);
            s_Shader.SetInt(s_IDBatchSize, length);
            s_Shader.GetKernelThreadGroupSizes(kernel, out var threadGroupXSize, out _, out _);
            var threadGroups = Mathf.Min(65535, Mathf.CeilToInt(length * 1f / (threadGroupXSize * 4)));

            s_Shader.Dispatch(kernel, threadGroups, 1, 1);
            var result = ArrayPool<T>.Shared.Rent(length);
            srcBuffer.GetData(result);
            result.AsSpan(0, length).CopyTo(src);
            ArrayPool<T>.Shared.Return(result);
            srcBuffer.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetOperand(Operations operation, T operand)
        {
            switch (s_TypeCode)
            {
                case TypeCode.Single:
                    s_Shader.SetFloat(operation.GetOperandName<T>(), Native.Cast<T, int>(ref operand));
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    s_Shader.SetInt(operation.GetOperandName<T>(), Native.Cast<T, int>(ref operand));
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal enum Operations
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
        Pow
    }

    internal static class OperationsExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetKernelName<T>(this Operations operation) => Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.UInt32 or TypeCode.Int32 => operation switch
            {
                Operations.Add      => "IntAdd",
                Operations.Subtract => "IntSubtract",
                Operations.Multiply => "IntMultiply",
                Operations.Divide   => "IntDivide",
                Operations.Modulo   => "IntModulo",
                Operations.Pow      => "IntPow",
                _                   => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            TypeCode.Single => operation switch
            {
                Operations.Add      => "FloatAdd",
                Operations.Subtract => "FloatSubtract",
                Operations.Multiply => "FloatMultiply",
                Operations.Divide   => "FloatDivide",
                Operations.Modulo   => "FloatModulo",
                Operations.Pow      => "FloatPow",
                _                   => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            _ => throw new ArgumentOutOfRangeException()
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetOperandName<T>(this Operations operation) => Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.UInt32 or TypeCode.Int32 => operation switch
            {
                Operations.Add      => "IntAddOperand",
                Operations.Subtract => "IntSubtractOperand",
                Operations.Multiply => "IntMultiplyOperand",
                Operations.Divide   => "IntDivideOperand",
                Operations.Modulo   => "IntModuloOperand",
                Operations.Pow      => "IntPowOperand",
                _                   => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            TypeCode.UInt64 => operation switch
            {
                Operations.Add      => "ULongAddOperand",
                Operations.Subtract => "ULongSubtractOperand",
                Operations.Multiply => "ULongMultiplyOperand",
                Operations.Divide   => "ULongDivideOperand",
                Operations.Modulo   => "ULongModuloOperand",
                Operations.Pow      => "ULongPowOperand",
                _                   => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            TypeCode.Single => operation switch
            {
                Operations.Add      => "FloatAddOperand",
                Operations.Subtract => "FloatSubtractOperand",
                Operations.Multiply => "FloatMultiplyOperand",
                Operations.Divide   => "FloatDivideOperand",
                Operations.Modulo   => "FloatModuloOperand",
                Operations.Pow      => "FloatPowOperand",
                _                   => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
            },
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}