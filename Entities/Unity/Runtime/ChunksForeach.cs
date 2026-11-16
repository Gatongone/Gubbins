using Gubbins.Enhance;
using Unity.Collections;

namespace Gubbins.Entities
{
    public delegate void NativeForeach(NativeArray<Entity> entity);

    public delegate void NativeForeach<T1>(NativeArray<Entity> entity, NativeArray<T1> arg1) where T1 : unmanaged;

    public delegate void NativeForeach<T1, T2>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2) where T1 : unmanaged where T2 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11, NativeArray<T12> arg12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11, NativeArray<T12> arg12, NativeArray<T13> arg13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11, NativeArray<T12> arg12, NativeArray<T13> arg13, NativeArray<T14> arg14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11, NativeArray<T12> arg12, NativeArray<T13> arg13, NativeArray<T14> arg14, NativeArray<T15> arg15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged;

    public delegate void NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(NativeArray<Entity> entity, NativeArray<T1> arg1, NativeArray<T2> arg2, NativeArray<T3> arg3, NativeArray<T4> arg4, NativeArray<T5> arg5, NativeArray<T6> arg6, NativeArray<T7> arg7, NativeArray<T8> arg8, NativeArray<T9> arg9, NativeArray<T10> arg10, NativeArray<T11> arg11, NativeArray<T12> arg12, NativeArray<T13> arg13, NativeArray<T14> arg14, NativeArray<T15> arg15, NativeArray<T16> arg16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged;

    public static class ChunksForeachExtensions
    {
        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach(this Chunks chunks, NativeForeach action)
        {
            using var entities = chunks.GetComponents<Entity>();
            var segments = entities.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                action(segments[0].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T>(this Chunks chunks, NativeForeach<T> action) where T : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components = chunks.GetComponents<T>();
            var segment0 = entities.Segments;
            var segment1 = components.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2>(this Chunks chunks, NativeForeach<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3>(this Chunks chunks, NativeForeach<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4>(this Chunks chunks, NativeForeach<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            var segment12 = components12.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray(), segment12[12].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            var segment12 = components12.Segments;
            var segment13 = components13.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray(), segment12[12].AsNativeArray(), segment13[13].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            var segment12 = components12.Segments;
            var segment13 = components13.Segments;
            var segment14 = components14.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray(), segment12[12].AsNativeArray(), segment13[13].AsNativeArray(), segment14[14].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            using var components15 = chunks.GetComponents<T15>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            var segment12 = components12.Segments;
            var segment13 = components13.Segments;
            var segment14 = components14.Segments;
            var segment15 = components15.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray(), segment12[12].AsNativeArray(), segment13[13].AsNativeArray(), segment14[14].AsNativeArray(), segment15[15].AsNativeArray());
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public static void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this Chunks chunks, NativeForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            using var components8 = chunks.GetComponents<T8>();
            using var components9 = chunks.GetComponents<T9>();
            using var components10 = chunks.GetComponents<T10>();
            using var components11 = chunks.GetComponents<T11>();
            using var components12 = chunks.GetComponents<T12>();
            using var components13 = chunks.GetComponents<T13>();
            using var components14 = chunks.GetComponents<T14>();
            using var components15 = chunks.GetComponents<T15>();
            using var components16 = chunks.GetComponents<T16>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            var segment3 = components3.Segments;
            var segment4 = components4.Segments;
            var segment5 = components5.Segments;
            var segment6 = components6.Segments;
            var segment7 = components7.Segments;
            var segment8 = components8.Segments;
            var segment9 = components9.Segments;
            var segment10 = components10.Segments;
            var segment11 = components11.Segments;
            var segment12 = components12.Segments;
            var segment13 = components13.Segments;
            var segment14 = components14.Segments;
            var segment15 = components15.Segments;
            var segment16 = components16.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[0].AsNativeArray(), segment1[1].AsNativeArray(), segment2[2].AsNativeArray(), segment3[3].AsNativeArray(), segment4[4].AsNativeArray(), segment5[5].AsNativeArray(), segment6[6].AsNativeArray(), segment7[7].AsNativeArray(), segment8[8].AsNativeArray(), segment9[9].AsNativeArray(), segment10[10].AsNativeArray(), segment11[11].AsNativeArray(), segment12[12].AsNativeArray(), segment13[13].AsNativeArray(), segment14[14].AsNativeArray(), segment15[15].AsNativeArray(), segment16[16].AsNativeArray());
            }
        }
    }
}