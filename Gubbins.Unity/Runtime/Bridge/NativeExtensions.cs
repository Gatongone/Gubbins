#pragma warning disable CS8500
using System;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Collections
{
    public static class NativeExtensions
    {
        /// <summary>Copies the contents of this span into a new <see cref="NativeArray{T}"/>.</summary>
        /// <returns>An array containing the data in the current span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<T> ToNativeArray<T>(this Span<T> src, Allocator allocator = Allocator.Temp) where T : struct
        {
            var dest = new NativeArray<T>(src.Length, allocator, NativeArrayOptions.UninitializedMemory);
            // var ptr = UnsafeUtility.AddressOf(ref dest);
            // var buffer = (int*) ptr;
            // *ptr = 12;
            // dest.m_MaxIndex = src.Length;
            // dest.m_MinIndex = 0;
            // dest.m_Safety   = AtomicSafetyHandle.GetTempUnsafePtrSliceHandle();
            dest.m_Buffer = Native.GetFirstElementAddress(src);
            dest.m_Length = src.Length;
            return dest;
        }
    }
}