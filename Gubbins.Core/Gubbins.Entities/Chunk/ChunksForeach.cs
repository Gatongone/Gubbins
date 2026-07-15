namespace Gubbins.Entities;

public delegate void ElementForeach(in Entity entity);

public delegate void ElementForeach<T1>(in Entity entity, ref T1 arg1);

public delegate void ElementForeach<T1, T2>(in Entity entity, ref T1 arg1, ref T2 arg2);

public delegate void ElementForeach<T1, T2, T3>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3);

public delegate void ElementForeach<T1, T2, T3, T4>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4);

public delegate void ElementForeach<T1, T2, T3, T4, T5>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14, ref T15 arg15);

public delegate void ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(in Entity entity, ref T1 arg1, ref T2 arg2, ref T3 arg3, ref T4 arg4, ref T5 arg5, ref T6 arg6, ref T7 arg7, ref T8 arg8, ref T9 arg9, ref T10 arg10, ref T11 arg11, ref T12 arg12, ref T13 arg13, ref T14 arg14, ref T15 arg15, ref T16 arg16);

public delegate void SegmentForeach(ReadOnlySpan<Entity> entity);

public delegate void SegmentForeach<T1>(ReadOnlySpan<Entity> entity, Span<T1> arg1);

public delegate void SegmentForeach<T1, T2>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2);

public delegate void SegmentForeach<T1, T2, T3>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3);

public delegate void SegmentForeach<T1, T2, T3, T4>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4);

public delegate void SegmentForeach<T1, T2, T3, T4, T5>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11, Span<T12> arg12);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11, Span<T12> arg12, Span<T13> arg13);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11, Span<T12> arg12, Span<T13> arg13, Span<T14> arg14);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11, Span<T12> arg12, Span<T13> arg13, Span<T14> arg14, Span<T15> arg15);

public delegate void SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ReadOnlySpan<Entity> entity, Span<T1> arg1, Span<T2> arg2, Span<T3> arg3, Span<T4> arg4, Span<T5> arg5, Span<T6> arg6, Span<T7> arg7, Span<T8> arg8, Span<T9> arg9, Span<T10> arg10, Span<T11> arg11, Span<T12> arg12, Span<T13> arg13, Span<T14> arg14, Span<T15> arg15, Span<T16> arg16);

public static class ChunksForeachExtensions
{
    extension(Chunks chunks)
    {
        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunks.</param>
        public void ForEach(ElementForeach action)
        {
            using var entities = chunks.GetComponents<Entity>();
            var elements = entities.Elements;
            for (var i = 0; i < elements.Count; i++)
            {
                action(in elements[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T>(ElementForeach<T> action) where T : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components = chunks.GetComponents<T>();
            var elements0 = entities.Elements;
            var elements1 = components.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2>(ElementForeach<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3>(ElementForeach<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4>(ElementForeach<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5>(ElementForeach<T1, T2, T3, T4, T5> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6>(ElementForeach<T1, T2, T3, T4, T5, T6> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7>(ElementForeach<T1, T2, T3, T4, T5, T6, T7> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            using var components3 = chunks.GetComponents<T3>();
            using var components4 = chunks.GetComponents<T4>();
            using var components5 = chunks.GetComponents<T5>();
            using var components6 = chunks.GetComponents<T6>();
            using var components7 = chunks.GetComponents<T7>();
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            var elements12 = components12.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i], ref elements12[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            var elements12 = components12.Elements;
            var elements13 = components13.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i], ref elements12[i], ref elements13[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            var elements12 = components12.Elements;
            var elements13 = components13.Elements;
            var elements14 = components14.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i], ref elements12[i], ref elements13[i], ref elements14[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            var elements12 = components12.Elements;
            var elements13 = components13.Elements;
            var elements14 = components14.Elements;
            var elements15 = components15.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i], ref elements12[i], ref elements13[i], ref elements14[i], ref elements15[i]);
            }
        }

        /// <summary>
        /// Iterates over each element in chunks in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each element in chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ElementForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
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
            var elements0 = entities.Elements;
            var elements1 = components1.Elements;
            var elements2 = components2.Elements;
            var elements3 = components3.Elements;
            var elements4 = components4.Elements;
            var elements5 = components5.Elements;
            var elements6 = components6.Elements;
            var elements7 = components7.Elements;
            var elements8 = components8.Elements;
            var elements9 = components9.Elements;
            var elements10 = components10.Elements;
            var elements11 = components11.Elements;
            var elements12 = components12.Elements;
            var elements13 = components13.Elements;
            var elements14 = components14.Elements;
            var elements15 = components15.Elements;
            var elements16 = components16.Elements;
            for (var i = 0; i < elements0.Count; i++)
            {
                action(in elements0[i], ref elements1[i], ref elements2[i], ref elements3[i], ref elements4[i], ref elements5[i], ref elements6[i], ref elements7[i], ref elements8[i], ref elements9[i], ref elements10[i], ref elements11[i], ref elements12[i], ref elements13[i], ref elements14[i], ref elements15[i], ref elements16[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach(SegmentForeach action)
        {
            using var entities = chunks.GetComponents<Entity>();
            var segments = entities.Segments;
            for (var i = 0; i < segments.Count; i++)
            {
                action(segments[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T>(SegmentForeach<T> action) where T : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components = chunks.GetComponents<T>();
            var segment0 = entities.Segments;
            var segment1 = components.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[i], segment1[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2>(SegmentForeach<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            using var entities = chunks.GetComponents<Entity>();
            using var components1 = chunks.GetComponents<T1>();
            using var components2 = chunks.GetComponents<T2>();
            var segment0 = entities.Segments;
            var segment1 = components1.Segments;
            var segment2 = components2.Segments;
            for (var i = 0; i < segment0.Count; i++)
            {
                action(segment0[i], segment1[i], segment2[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3>(SegmentForeach<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4>(SegmentForeach<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5>(SegmentForeach<T1, T2, T3, T4, T5> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6>(SegmentForeach<T1, T2, T3, T4, T5, T6> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i], segment12[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i], segment12[i], segment13[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i], segment12[i], segment13[i], segment14[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i], segment12[i], segment13[i], segment14[i], segment15[i]);
            }
        }

        /// <summary>
        /// Iterates over each chunk in the collection and applies the specified action to it.
        /// </summary>
        /// <param name="action">The action to perform on each chunk.</param>
        public void ForEach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(SegmentForeach<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
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
                action(segment0[i], segment1[i], segment2[i], segment3[i], segment4[i], segment5[i], segment6[i], segment7[i], segment8[i], segment9[i], segment10[i], segment11[i], segment12[i], segment13[i], segment14[i], segment15[i], segment16[i]);
            }
        }
    }
}