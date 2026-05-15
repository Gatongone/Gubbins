using System;
using UnityEngine.Collections;

namespace UnityEngine
{
    public static class ComputeSpanExtensions
    {
        /// <summary>
        ///   <para>Set the buffer with values from an array.</para>
        /// </summary>
        /// <param name="buffer">Compute buffer.</param>
        /// <param name="data">Array of values to fill the buffer.</param>
        public static void SetData<T>(this ComputeBuffer buffer, Span<T> data) where T : struct => buffer.SetData(data.ToNativeArray());
    }
}