using System;
using System.Runtime.CompilerServices;
using Gubbins.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Gubbins.Enhance
{
    /// <summary>
    /// Extension methods for working with Spans in the context of Gubbins.Enhance.
    /// </summary>
    public static class SpanExtensions
    {
        /// <summary>
        /// Aliases a snippet span as a <see cref="NativeArray{T}"/> over the pinned chunk memory with no
        /// allocation and no copy (<see cref="Allocator.None"/> — nothing to dispose), attaching a temp
        /// safety handle so the job scheduler accepts the alias. Preferred over the Span→NativeArray
        /// bridge here because that bridge allocates a throwaway Temp array on every call.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<T> AsNativeArray<T>(this Span<T> span) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                Native.GetFirstElementAddress(span), span.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            return array;
        }

        /// <summary>
        /// Aliases a snippet span as a <see cref="NativeArray{T}"/> over the pinned chunk memory with no
        /// allocation and no copy (<see cref="Allocator.None"/> — nothing to dispose), attaching a temp
        /// safety handle so the job scheduler accepts the alias. Preferred over the Span→NativeArray
        /// bridge here because that bridge allocates a throwaway Temp array on every call.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe NativeArray<T> AsNativeArray<T>(this ReadOnlySpan<T> span) where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                Native.GetFirstElementAddress(span), span.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
#endif
            return array;
        }
    }
}