using System;
using System.Runtime.CompilerServices;
using Gubbins.Enhance.Gubbins.Enhance;
using Unity.Burst;

namespace Gubbins.Unity
{
    [BurstCompile]
    public sealed class BurstOperation<T> : IBatchOperation<T> where T : unmanaged
    {
        /// <inheritdoc />
        public bool Supported => BurstCompiler.IsEnabled && typeof(T).CheckType().IsNumberType;

        private static readonly IBatchOperation<T> s_Proxy;

        static BurstOperation()
        {
            s_Proxy = Type.GetTypeCode(typeof(T)) switch
            {
                TypeCode.Byte    => new BurstByteOperation() as IBatchOperation<T>,
                TypeCode.Decimal => new BurstDecimalOperation() as IBatchOperation<T>,
                TypeCode.Double  => new BurstDoubleOperation() as IBatchOperation<T>,
                TypeCode.Int16   => new BurstShortOperation() as IBatchOperation<T>,
                TypeCode.Int32   => new BurstIntOperation() as IBatchOperation<T>,
                TypeCode.Int64   => new BurstLongOperation() as IBatchOperation<T>,
                TypeCode.SByte   => new BurstSbyteOperation() as IBatchOperation<T>,
                TypeCode.Single  => new BurstFloatOperation() as IBatchOperation<T>,
                TypeCode.UInt16  => new BurstUshortOperation() as IBatchOperation<T>,
                TypeCode.UInt32  => new BurstUintOperation() as IBatchOperation<T>,
                TypeCode.UInt64  => new BurstUlongOperation() as IBatchOperation<T>,
                _                => throw new ArgumentOutOfRangeException()
            };
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(Span<T> src, T operand) => s_Proxy.Add(src, operand);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Subtract(Span<T> src, T operand)  => s_Proxy.Subtract(src, operand);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Multiply(Span<T> src, T operand) => s_Proxy.Multiply(src, operand);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Divide(Span<T> src, T operand) => s_Proxy.Divide(src, operand);

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modulo(Span<T> src, T operand) => s_Proxy.Modulo(src, operand);
    }
}