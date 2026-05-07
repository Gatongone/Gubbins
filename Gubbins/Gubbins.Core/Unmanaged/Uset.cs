// using System.Collections;
// using System.Diagnostics;
// using System.Runtime.CompilerServices;
//
// namespace Gubbins.Unmanaged;
//
// public struct Uset<T> : ICollection<T> where T : unmanaged
// {
//     private int           m_Count;
//     private int           m_FreeCount;
//     private Uarray<Entry> m_Entries;
//     private Uarray<int>   m_Buckets;
//     private ulong         m_FastModMultiplier;
//     private int           _freeCount;
//     private int           _freeList;
//
//     /// <inheritdoc />
//     public int Count { get; }
//
//     /// <inheritdoc />
//     public bool IsReadOnly { get; }
//
//     public bool Add(T item) => AddIfNotPresent(item, out _);
//
//     /// <inheritdoc />
//     void ICollection<T>.Add(T item) => Add(item);
//     private bool AddIfNotPresent(T value, out int location)
//     {
//         var entries = m_Entries;
//         var comparer = EqualityComparer<T>.Default;
//         int hashCode;
//
//         uint collisionCount = 0;
//         ref int bucket = ref ;
//
//         if (typeof(T).IsValueType && // comparer can only be null for value types; enable JIT to eliminate entire if block for ref types
//             comparer == null)
//         {
//             hashCode = value!.GetHashCode();
//             bucket   = ref GetBucketRef(hashCode);
//             int i = bucket - 1; // Value in m_Buckets is 1-based
//
//             // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
//             while (i >= 0)
//             {
//                 ref Entry entry = ref entries[i];
//                 if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, value))
//                 {
//                     location = i;
//                     return false;
//                 }
//
//                 i = entry.Next;
//
//                 collisionCount++;
//                 if (collisionCount > (uint) entries.Count)
//                 {
//                     // The chain of entries forms a loop, which means a concurrent update has happened.
//                     //ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
//                 }
//             }
//         }
//         else
//         {
//             hashCode = comparer.GetHashCode(value);
//             bucket   = ref GetBucketRef(hashCode);
//             int i = bucket - 1; // Value in m_Buckets is 1-based
//             while (i >= 0)
//             {
//                 ref Entry entry = ref entries[i];
//                 if (entry.HashCode == hashCode && comparer.Equals(entry.Value, value))
//                 {
//                     location = i;
//                     return false;
//                 }
//
//                 i = entry.Next;
//
//                 collisionCount++;
//                 if (collisionCount > (uint) entries.Length)
//                 {
//                     // The chain of entries forms a loop, which means a concurrent update has happened.
//                     ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
//                 }
//             }
//         }
//
//         int index;
//         if (_freeCount > 0)
//         {
//             index = _freeList;
//             _freeCount--;
//             Debug.Assert((StartOfFreeList - entries![_freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
//             _freeList = StartOfFreeList - entries[_freeList].Next;
//         }
//         else
//         {
//             int count = m_Count;
//             if (count == entries.Count)
//             {
//                 Resize();
//                 bucket = ref GetBucketRef(hashCode);
//             }
//
//             index   = count;
//             m_Count = count + 1;
//             entries = m_Entries;
//         }
//
//         {
//             ref Entry entry = ref entries![index];
//             entry.HashCode = hashCode;
//             entry.Next     = bucket - 1; // Value in m_Buckets is 1-based
//             entry.Value    = value;
//             bucket         = index + 1;
//             _version++;
//             location = index;
//         }
//
//         // Value types never rehash
//         if (!typeof(T).IsValueType && collisionCount > HashHelpers.HashCollisionThreshold && comparer is NonRandomizedStringEqualityComparer)
//         {
//             // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
//             // i.e. EqualityComparer<string>.Default.
//             Resize(entries.Length, forceNewHashCodes: true);
//             location = FindItemIndex(value);
//             Debug.Assert(location >= 0);
//         }
//
//         return true;
//     }
//
//     /// <inheritdoc />
//     public void Clear() { }
//
//     /// <inheritdoc />
//     public bool Contains(T item)
//     {
//         HashSet<> 
//     }
//
//     /// <inheritdoc />
//     public void CopyTo(T[] array, int arrayIndex) { }
//
//     /// <inheritdoc />
//     public bool Remove(T item) { }
//
//     public Enumerator GetEnumerator();
//
//     /// <inheritdoc />
//     IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
//
//     /// <inheritdoc />
//     IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
//
//     private void Resize(int newSize)
//     {
//         var entries = new Uarray<Entry>(newSize);
//
//         var count = m_Count;
//
//         m_Entries.CopyTo(entries);
//
//         // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
//         m_Buckets = new Uarray<int>(newSize);
// #if TARGET_64BIT
//         m_FastModMultiplier = Hash.GetFastModMultiplier((uint)newSize);
// #endif
//         for (var i = 0; i < count; i++)
//         {
//             ref var entry = ref entries[i];
//             if (entry.Next < -1) continue;
//             ref var bucket = ref GetBucketRef(entry.HashCode);
//             entry.Next = bucket - 1; // Value in m_Buckets is 1-based
//             bucket     = i + 1;
//         }
//
//         m_Entries = entries;
//     }
//
//     [MethodImpl(MethodImplOptions.AggressiveInlining)]
//     private ref int GetBucketRef(int hashCode)
//     {
// #if TARGET_64BIT
//         return ref m_Buckets[(int) Hash.FastMod((uint)hashCode, (uint)m_Buckets.Count, m_FastModMultiplier)];
// #else
//         return ref m_Buckets[(int) ((uint) hashCode % (uint) m_Buckets.Count)];
// #endif
//     }
//
//     private struct Entry
//     {
//         public int HashCode;
//
//         /// <summary>
//         /// 0-based index of next entry in chain: -1 means end of chain
//         /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
//         /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
//         /// </summary>
//         public int Next;
//
//         public T Value;
//     }
// }