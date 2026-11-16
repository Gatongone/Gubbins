using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Gubbins.Enhance;
using Gubbins.Unsafe;

namespace Gubbins.Unmanaged;

/// <summary>
/// A hashset-like collection that stores its data in unmanaged memory.
/// </summary>
/// <typeparam name="T">Value type of the elements in the set. Must be unmanaged to be stored in unmanaged memory.</typeparam>
/// <remarks>
/// This class was adapted from the .NET runtime's <see cref="HashSet{T}"/> implementation,
/// with modifications to use unmanaged memory and support value types without boxing.
/// Licensed to the .NET Foundation under one or more agreements.
/// </remarks>
public struct Uset<T> : ICollection<T>, IDisposable
{
    /// <summary>
    /// Total number of slots that have been used, including removed entries.
    /// </summary>
    private int m_Count;

    /// <summary>
    /// Number of entries currently tracked in the free list.
    /// </summary>
    private int m_FreeCount;

    /// <summary>
    /// Head index of the free-list chain, or -1 when no free slots exist.
    /// </summary>
    private int m_FreeList;

    /// <summary>
    /// Version token incremented on mutations to detect invalid enumeration.
    /// </summary>
    private int m_Version;
#if TARGET_64BIT
    /// <summary>
    /// Precomputed multiplier used by fast modulo on 64-bit targets.
    /// </summary>
    private ulong m_FastModMultiplier;
#endif

    /// <summary>
    /// Backing storage for set entries.
    /// </summary>
    private Uarray<Entry>? m_Entries;

    /// <summary>
    /// Bucket heads using 1-based indexing into <see cref="m_Entries"/>.
    /// </summary>
    private Uarray<int>? m_Buckets;

    /// <summary>
    /// The allocator used for the underlying entry and bucket storage.
    /// </summary>
    private Allocator m_Allocator;

    /// <summary>
    /// Initializes a new, empty <see cref="Uset{T}"/> backed by the specified allocator.
    /// </summary>
    /// <param name="allocator">The allocator used for the underlying storage.</param>
    public Uset(Allocator allocator)
    {
        m_Count      = 0;
        m_FreeCount  = 0;
        m_FreeList   = 0;
        m_Version    = 0;
#if TARGET_64BIT
        m_FastModMultiplier = 0;
#endif
        m_Entries   = null;
        m_Buckets   = null;
        m_Allocator = allocator;

        // Allocate the initial (empty) storage so that the first Add can index into the buckets.
        Resize();
    }

    /// <summary>
    /// Initializes a new, empty <see cref="Uset{T}"/> with the specified initial capacity,
    /// backed by the specified allocator.
    /// </summary>
    /// <param name="capacity">The initial capacity of the set.</param>
    /// <param name="allocator">The allocator used for the underlying storage.</param>
    public Uset(int capacity, Allocator allocator) : this(allocator)
    {
        if (capacity > 0)
        {
            Resize(Math.Max(Hash.GetPrime(capacity), 3));
        }
    }

    /// <summary>
    /// Initializes a new <see cref="Uset{T}"/> that contains the elements of the specified collection,
    /// backed by the specified allocator. Duplicates are ignored.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied into the new set.</param>
    /// <param name="allocator">The allocator used for the underlying storage.</param>
    public Uset(IEnumerable<T> collection, Allocator allocator) : this(allocator)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        foreach (var item in collection)
        {
            Add(item);
        }
    }

    /// <summary>
    /// When constructing a hashset from an existing collection, it may contain duplicates,
    /// so this is used as the max acceptable excess ratio of capacity to count. Note that
    /// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
    /// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
    /// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
    /// </summary>
    private const int SHRINK_THRESHOLD = 3;

    /// <summary>
    /// The value stored in the Next field of an entry to indicate that the entry is on the free list.
    /// </summary>
    private const int START_OF_FREE_LIST = -3;

    /// <inheritdoc />
    public int Count => m_Count - m_FreeCount;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds an element to the <see cref="Uset{T}"/>. Returns true if the element was added to the set, false if the element was already present.
    /// </summary>
    /// <param name="item">The element to add to the set.</param>
    /// <returns>True if the element was added to the set; false if the element was already present.</returns>
    public bool Add(T item) => AddIfNotPresent(item, out _);

    /// <inheritdoc />
    void ICollection<T>.Add(T item) => Add(item);

    /// <summary>
    /// Adds a value to the set if it's not already present, and returns the index of the value in the entries array.
    /// </summary>
    /// <param name="value">The value to add to the set.</param>
    /// <param name="location">
    /// When this method returns, contains the index of the value in the entries array.
    /// If the value was already present, this is the index of the existing value; if the value was added, this is the index of the new value.
    /// </param>
    /// <returns>>True if the value was added to the set; false if the value was already present.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the method detects a concurrent update to the set, which can corrupt the internal state. This can happen if multiple threads are modifying the set without proper synchronization.
    /// </exception>
    private bool AddIfNotPresent(T value, out int location)
    {
        // A default-initialized set has no storage yet; allocate the initial arrays on first use.
        if (m_Buckets == null)
        {
            Resize();
        }

        ref var entries = ref m_Entries;

        uint collisionCount = 0;

        var hashCode = value.GetHashCode();
        ref var bucket = ref GetBucketRef(hashCode);
        var i = bucket - 1; // Value in m_Buckets is 1-based

        // Devirtualize with EqualityComparer<TValue>.Default intrinsic
        while (i >= 0)
        {
            ref var entry = ref entries!.Value[i];
            if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, value))
            {
                location = i;
                return false;
            }

            i = entry.Next;

            collisionCount++;
            if (collisionCount > (uint) entries.Value.Count)
            {
                // The chain of entries forms a loop, which means a concurrent update has happened.
                throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
            }
        }

        int index;
        if (m_FreeCount > 0)
        {
            index = m_FreeList;
            m_FreeCount--;
            Debug.Assert(START_OF_FREE_LIST - entries!.Value[m_FreeList].Next >= -1, "shouldn't overflow because `next` cannot underflow");
            m_FreeList = START_OF_FREE_LIST - entries.Value[m_FreeList].Next;
        }
        else
        {
            var count = Count;
            if (count == entries!.Value.Count)
            {
                Resize();
                bucket = ref GetBucketRef(hashCode);
            }

            index   = count;
            m_Count = count + 1;
            entries = m_Entries;
        }

        {
            ref var entry = ref entries!.Value![index];
            entry.HashCode = hashCode;
            entry.Next     = bucket - 1; // Value in m_Buckets is 1-based
            entry.Value    = value;
            bucket         = index + 1;
            m_Version++;
            location = index;
        }

        return true;
    }

    /// <summary>
    /// Removes all elements from the set while keeping the allocated storage for reuse.
    /// </summary>
    public void Clear()
    {
        if (m_Count != 0)
        {
            if (m_Entries.HasValue)
            {
                m_Entries.Value.AsSpan(0, m_Count).Clear();
            }

            if (m_Buckets.HasValue)
            {
                m_Buckets.Value.AsSpan().Clear();
            }

            m_Count     = 0;
            m_FreeCount = 0;
            m_FreeList  = 0;
        }

        m_Version++;
    }

    /// <inheritdoc />
    public bool Contains(T item) => FindItemIndex(item) >= 0;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is negative or greater than the array length.</exception>
    /// <exception cref="ArgumentException">Thrown when the destination does not have enough space for all elements.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        CopyTo(array.AsSpan(arrayIndex));
    }

    /// <summary>
    /// Copies the elements of the set into a span, starting at the beginning of the span.
    /// </summary>
    /// <param name="destination">The destination span.</param>
    /// <exception cref="ArgumentException">Thrown when the destination does not have enough space for all elements.</exception>
    public void CopyTo(Span<T> destination)
    {
        if (destination.Length < Count)
        {
            throw new ArgumentException("Destination is not long enough.", nameof(destination));
        }

        var written = 0;
        foreach (var item in this)
        {
            destination[written++] = item;
        }
    }

    /// <summary>
    /// Copies the elements of the set into a new managed array.
    /// </summary>
    /// <returns>A new array containing all elements of the set.</returns>
    public T[] ToArray()
    {
        var result = new T[Count];
        CopyTo(result);
        return result;
    }

    /// <inheritdoc />
    public bool Remove(T item)
    {
        if (m_Buckets == null) return false;
        ref var entries = ref m_Entries;

        uint collisionCount = 0;
        var last = -1;

        var comparer = EqualityComparer<T>.Default;
        var hashCode = item.GetHashCode();

        ref var bucket = ref GetBucketRef(hashCode);
        var i = bucket - 1; // Value in buckets is 1-based

        while ((uint) i < (uint) entries!.Value.Count)
        {
            ref var entry = ref entries.Value[i];

            if (entry.HashCode == hashCode && (comparer?.Equals(entry.Value, item) ?? EqualityComparer<T>.Default.Equals(entry.Value, item)))
            {
                if (last < 0)
                {
                    bucket = entry.Next + 1; // Value in buckets is 1-based
                }
                else
                {
                    entries.Value[last].Next = entry.Next;
                }

                Debug.Assert(START_OF_FREE_LIST - m_FreeList < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                entry.Next = START_OF_FREE_LIST - m_FreeList;

                if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                {
                    entry.Value = default!;
                }

                m_FreeList = i;
                m_FreeCount++;
                m_Version++;
                return true;
            }

            last = i;
            i    = entry.Next;

            collisionCount++;
            if (collisionCount > (uint) entries.Value.Count)
            {
                // The chain of entries forms a loop; which means a concurrent update has happened.
                // Break out of the loop and throw, rather than looping forever.
                throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (m_Entries != null)
        {
            m_Entries.Value.Dispose();
            m_Entries = null;
        }

        if (m_Buckets != null)
        {
            m_Buckets.Value.Dispose();
            m_Buckets = null;
        }

        m_Count     = 0;
        m_FreeCount = 0;
        m_FreeList  = 0;
        m_Version++;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the <see cref="Uset{T}"/>.
    /// The enumerator is a ref struct that can be used without boxing,
    /// but it is not thread-safe and does not support concurrent modifications to the set during enumeration.
    /// </summary>
    /// <returns></returns>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => new ReadOnlyEnumerator(this);

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => new ReadOnlyEnumerator(this);

    /// <summary>
    /// Finds the index of the specified item in the entries array, or returns -1 if the item is not found.
    /// </summary>
    /// <param name="item">The item to find in the set.</param>
    /// <returns>The index of the item in the entries array if found; otherwise, -1.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the method detects a concurrent update to the set, which can corrupt the internal state. This can happen if multiple threads are modifying the set without proper synchronization.
    /// </exception>
    private int FindItemIndex(T item)
    {
        ref var buckets = ref m_Buckets;
        if (buckets != null)
        {
            ref var entries = ref m_Entries;
            Debug.Assert(entries != null, "Expected _entries to be initialized");

            uint collisionCount = 0;
            // Devirtualize with EqualityComparer<TValue>.Default intrinsic
            var hashCode = item.GetHashCode();
            var i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
            while ((uint) i < (uint) entries.Value.Count)
            {
                ref var entry = ref entries.Value[i];
                if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, item))
                {
                    return i;
                }

                i = entry.Next;

                collisionCount++;
                if (collisionCount > (uint) entries.Value.Count)
                {
                    // The chain of entries forms a loop, which means a concurrent update has happened.
                    throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.");
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Resizes the internal data structures to accommodate more entries when the current capacity is exceeded.
    /// </summary>
    private void Resize() => Resize(Hash.ExpandPrime(Count));

    /// <summary>
    /// Resizes the internal data structures to accommodate the specified new size.
    /// This involves allocating new arrays for buckets and entries,
    /// rehashing existing entries, and updating internal references.
    /// The new size should be greater than or equal to the current number of entries to avoid data loss.
    /// </summary>
    /// <param name="newSize">The new size for the internal data structures. Must be greater than or equal to the current number of entries.</param>
    private void Resize(int newSize)
    {
        var activeCount = Count;

        if (newSize < activeCount)
        {
            newSize = activeCount;
        }

        if (newSize < 3)
        {
            newSize = 3;
        }

        var oldEntries = m_Entries;
        var usedCount  = m_Count;

        var entries = new Uarray<Entry>(newSize, m_Allocator);

        // Replace the buckets before rebuilding the chains so GetBucketRef targets the new array.
        m_Buckets?.Dispose();
        m_Buckets = new Uarray<int>(newSize, m_Allocator);

        // The unmanaged allocator does not zero memory; buckets must start at 0 (1-based chain heads).
        m_Buckets.Value.AsSpan().Clear();
#if TARGET_64BIT
        m_FastModMultiplier = Hash.GetFastModMultiplier((uint)newSize);
#endif

        // Compact the live entries to the front of the new array and rebuild the bucket chains.
        var index = 0;
        if (oldEntries != null)
        {
            var oldSpan = oldEntries.Value.AsSpan();
            for (var i = 0; i < usedCount; i++)
            {
                ref var oldEntry = ref oldSpan[i];
                if (oldEntry.Next < -1)
                {
                    continue; // Slot is on the free list.
                }

                ref var newEntry = ref entries[index];
                newEntry.HashCode = oldEntry.HashCode;
                newEntry.Value    = oldEntry.Value;

                ref var bucket = ref GetBucketRef(newEntry.HashCode);
                newEntry.Next = bucket - 1; // Value in buckets is 1-based
                bucket        = index + 1;
                index++;
            }
        }

        oldEntries?.Dispose();

        m_Entries   = entries;
        m_Count     = index;
        m_FreeCount = 0;
        m_FreeList  = 0;
    }

    /// <summary>
    /// Returns a reference to the bucket corresponding to the specified hash code.
    /// </summary>
    /// <param name="hashCode">The hash code for which to retrieve the bucket reference.</param>
    /// <returns>A reference to the bucket corresponding to the specified hash code.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref int GetBucketRef(int hashCode)
    {
        Debug.Assert(m_Buckets != null);
#if TARGET_64BIT
        return ref m_Buckets.Value[(int) Hash.FastMod((uint)hashCode, (uint)m_Buckets.Count, m_FastModMultiplier)];
#else
        return ref m_Buckets.Value[(int) ((uint) hashCode % (uint) m_Buckets.Value.Count)];
#endif
    }

    /// <summary>
    /// An entry in the hashset's internal data structure, representing a single element in the set.
    /// </summary>
    private struct Entry
    {
        /// <summary>
        /// Hash code of <see cref="Value"/>.
        /// </summary>
        public int HashCode;

        /// <summary>
        /// 0-based index of next entry in chain: -1 means end of chain
        /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
        /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
        /// </summary>
        public int Next;

        /// <summary>
        /// Stored set value.
        /// </summary>
        public T Value;
    }

    /// <summary>
    /// A ref struct enumerator for the <see cref="Uset{T}"/> that implements the standard <see cref="IEnumerator{T}"/> interface.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// Snapshot of the set being enumerated.
        /// </summary>
        private readonly Uset<T> m_HashSet;

        /// <summary>
        /// Version captured at enumerator creation.
        /// </summary>
        private readonly int m_Version;

        /// <summary>
        /// Current scan position in the entries array.
        /// </summary>
        private int m_Index;

        /// <summary>
        /// Index of the entry exposed by <see cref="Current"/>, or -1 before enumeration starts.
        /// </summary>
        private int m_CurrentIndex;

        /// <summary>
        /// Cached current value for interface-based enumeration.
        /// </summary>
        private T m_Current;

        /// <summary>
        /// Initializes an enumerator over the provided set.
        /// </summary>
        /// <param name="hashSet">Set instance to enumerate.</param>
        internal Enumerator(Uset<T> hashSet)
        {
            m_HashSet      = hashSet;
            m_Version      = hashSet.m_Version;
            m_Index        = 0;
            m_CurrentIndex = -1;
            m_Current      = default!;
        }

        /// <summary>
        /// Advances the enumerator to the next occupied entry.
        /// </summary>
        /// <returns><see langword="true"/> if another value is available; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the set is modified during enumeration.</exception>
        public bool MoveNext()
        {
            if (m_Version != m_HashSet.m_Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            // Use unsigned comparison since we set index to usedSlots+1 when the enumeration ends.
            // usedSlots+1 could be negative if usedSlots is int.MaxValue.
            // Iterate over used slots (including free-list slots), not the active element count.
            while ((uint)m_Index < (uint)m_HashSet.m_Count)
            {
                var index = m_Index++;
                ref var entry = ref m_HashSet.m_Entries!.Value[index];
                if (entry.Next >= -1)
                {
                    m_CurrentIndex = index;
                    m_Current      = entry.Value;
                    return true;
                }
            }

            m_Index        = m_HashSet.m_Count + 1;
            m_CurrentIndex = -1;
            m_Current      = default!;
            return false;
        }

        /// <summary>
        /// Releases resources used by the enumerator.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Gets a reference to the current element in the set.
        /// </summary>
        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_HashSet.m_Entries!.Value[m_CurrentIndex].Value;
        }

        /// <inheritdoc />
        T IEnumerator<T>.Current => Current;

        /// <inheritdoc />
        object IEnumerator.Current
        {
            get
            {
                if (m_Index == 0 || m_Index == m_HashSet.m_Count + 1)
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return m_Current;
            }
        }

        /// <inheritdoc />
        void IEnumerator.Reset()
        {
            if (m_Version != m_HashSet.m_Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            m_Index        = 0;
            m_CurrentIndex = -1;
            m_Current      = default!;
        }
    }

    /// <summary>
    /// A read-only enumerator for the <see cref="Uset{T}"/> that implements the standard <see cref="IEnumerator{T}"/> interface.
    /// </summary>
    public struct ReadOnlyEnumerator : IEnumerator<T>
    {
        /// <summary>
        /// Snapshot of the set being enumerated.
        /// </summary>
        private readonly Uset<T> m_HashSet;

        /// <summary>
        /// Version captured at enumerator creation.
        /// </summary>
        private readonly int m_Version;

        /// <summary>
        /// Current scan position in the entries array.
        /// </summary>
        private int m_Index;

        /// <summary>
        /// Current value exposed through the read-only enumerator contract.
        /// </summary>
        private T m_Current;

        /// <summary>
        /// Initializes an enumerator over the provided set.
        /// </summary>
        /// <param name="hashSet">Set instance to enumerate.</param>
        internal ReadOnlyEnumerator(Uset<T> hashSet)
        {
            m_HashSet = hashSet;
            m_Version = hashSet.m_Version;
            m_Index   = 0;
            m_Current = default!;
        }

        /// <summary>
        /// Advances the enumerator to the next occupied entry.
        /// </summary>
        /// <returns><see langword="true"/> if another value is available; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the set is modified during enumeration.</exception>
        public bool MoveNext()
        {
            if (m_Version != m_HashSet.m_Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            // Use unsigned comparison since we set index to usedSlots+1 when the enumeration ends.
            // usedSlots+1 could be negative if usedSlots is int.MaxValue.
            // Iterate over used slots (including free-list slots), not the active element count.
            while ((uint)m_Index < (uint)m_HashSet.m_Count)
            {
                ref var entry = ref m_HashSet.m_Entries!.Value[m_Index++];
                if (entry.Next >= -1)
                {
                    m_Current = entry.Value;
                    return true;
                }
            }

            m_Index   = m_HashSet.m_Count + 1;
            m_Current = default!;
            return false;
        }

        /// <summary>
        /// Releases resources used by the enumerator.
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Gets the current element in the set.
        /// </summary>
        public T Current => m_Current;

        /// <inheritdoc />
        object IEnumerator.Current
        {
            get
            {
                if (m_Index == 0 || m_Index == m_HashSet.m_Count + 1)
                {
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                }

                return m_Current;
            }
        }

        /// <inheritdoc />
        void IEnumerator.Reset()
        {
            if (m_Version != m_HashSet.m_Version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }

            m_Index   = 0;
            m_Current = default!;
        }
    }
}

/// <summary>
/// NonRandomizedStringEqualityComparer is the comparer used by default with <c>Dictionary&lt;string, ...&gt;</c>.
/// We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which
/// keeps the performance not affected till we hit collision threshold and then we switch to the comparer which is using
/// randomized string hashing.
/// </summary>
/// <remarks>
/// Licensed to the .NET Foundation under one or more agreements.
/// </remarks>
[Serializable] // Required for compatibility with .NET Core 2.0 as we exposed the NonRandomizedStringEqualityComparer inside the serialization blob
// Needs to be public to support binary serialization compatibility
file class NonRandomizedStringEqualityComparer : IEqualityComparer<string?>
{
    // Dictionary<...>.Comparer and similar methods need to return the original IEqualityComparer
    // that was passed in to the ctor. The caller chooses one of these singletons so that the
    // GetUnderlyingEqualityComparer method can return the correct value.

    /// <summary>
    /// Comparer instance that should be surfaced as the original user-provided comparer.
    /// </summary>
    private readonly IEqualityComparer<string?> m_UnderlyingComparer;

    /// <summary>
    /// Initializes a comparer wrapper around the provided underlying comparer.
    /// </summary>
    /// <param name="underlyingComparer">Comparer to return from <see cref="GetUnderlyingEqualityComparer"/>.</param>
    private NonRandomizedStringEqualityComparer(IEqualityComparer<string?> underlyingComparer)
    {
        Debug.Assert(underlyingComparer != null);
        m_UnderlyingComparer = underlyingComparer;
    }

    // This is used by the serialization engine.
    /// <summary>
    /// Initializes an instance for serialization compatibility.
    /// </summary>
    /// <param name="information">Serialized data for this instance.</param>
    /// <param name="context">Source and destination context for the serialization operation.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected NonRandomizedStringEqualityComparer(SerializationInfo information, StreamingContext context)
        : this(EqualityComparer<string?>.Default) { }

    /// <inheritdoc />
    public virtual bool Equals(string? x, string? y)
    {
        // This instance may have been deserialized into a class that doesn't guarantee
        // these parameters are non-null. Can't short-circuit the null checks.
        return string.Equals(x, y);
    }

    /// <inheritdoc />
    public virtual int GetHashCode(string? obj)
    {
        // This instance may have been deserialized into a class that doesn't guarantee
        // these parameters are non-null. Can't short-circuit the null checks.
        return obj == null ? 0 : GetNonRandomizedHashCode(obj);
    }

    // Gets the comparer that should be returned back to the caller when querying the
    // ICollection.Comparer property. Also used for serialization purposes.
    /// <summary>
    /// Returns the original comparer represented by this wrapper.
    /// </summary>
    /// <returns>The underlying comparer instance.</returns>
    public IEqualityComparer<string?> GetUnderlyingEqualityComparer() => m_UnderlyingComparer;

    /// <summary>
    /// Computes a non-randomized hash code for a character span.
    /// </summary>
    /// <param name="str">Character span to hash.</param>
    /// <returns>The non-randomized hash code.</returns>
    internal unsafe int GetNonRandomizedHashCode(ReadOnlySpan<char> str)
    {
        fixed (char* src = str)
        {
            Debug.Assert(src[str.Length] == '\0', "src[Length] == '\\0'");
            Debug.Assert((int) src % 4 == 0, "Managed string should start at 4 bytes boundary");

            var hash1 = (5381 << 16) + 5381u;
            var hash2 = hash1;

            var ptr = (uint*) src;
            var length = str.Length;

            while (length > 2)
            {
                length -= 4;
                hash1  =  (RotateLeft(hash1, 5) + hash1) ^ ptr[0];
                hash2  =  (RotateLeft(hash2, 5) + hash2) ^ ptr[1];
                ptr    += 2;
            }

            if (length > 0)
            {
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ ptr[0];
            }

            return (int) (hash1 + hash2 * 1566083941);
        }

        static uint RotateLeft(uint value, int offset)
        {
            return value << offset | value >> 32 /*0x20*/ - offset;
        }
    }
}