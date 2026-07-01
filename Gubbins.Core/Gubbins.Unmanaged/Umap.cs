using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Unmanaged;

/// <summary>
/// Represents an unmanaged hash map (dictionary) that provides fast key-value pair storage and retrieval.
/// This structure uses separate chaining for collision resolution and implements tombstone-based deletion
/// for efficient memory management. It implements both <see cref="IDictionary{TKey, TValue}"/> and
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> interfaces.
/// </summary>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public struct Umap<TKey, TValue> : IDisposable, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
{
    /// <summary>
    /// The maximum load factor threshold before the hash map triggers a resize operation.
    /// When the ratio of elements to buckets exceeds this value, the hash map will expand
    /// to maintain optimal performance characteristics.
    /// </summary>
    private const float LOAD_FACTOR = 0.72f;

    /// <summary>
    /// The underlying storage for hash map entries containing key-value pairs and chain links.
    /// Each entry includes the key, value, hash code, and a pointer to the next entry in the chain.
    /// </summary>
    private Ulist<Entry> m_Items;

    /// <summary>
    /// Array of bucket heads where each element points to the first entry in that bucket's chain.
    /// A value of -1 indicates an empty bucket with no entries.
    /// </summary>
    private Ulist<int> m_Buckets;

    /// <summary>
    /// The current number of buckets in the hash map, used for hash code modulo operations
    /// to determine bucket placement for entries.
    /// </summary>
    private int m_BucketCount;

    /// <summary>
    /// Track head of tombstone chain.
    /// </summary>
    private int m_FreeList;

    /// <summary>
    /// Count of available tombstones
    /// </summary>
    private int m_FreeCount;

    /// <summary>
    /// The current number of active key-value pairs stored in the hash map,
    /// excluding tombstoned (deleted) entries.
    /// </summary>
    private int m_Count;

    /// <summary>
    /// Indicates whether this hash map instance has been disposed and its resources released.
    /// When true, most operations will throw an ObjectDisposedException.
    /// </summary>
    private bool m_IsDisposed;

    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !m_IsDisposed;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Count == 0;
    }

    /// <inheritdoc cref="ICollection{T}.Count"/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Count;
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

    /// <inheritdoc />
    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
    {
        get
        {
            var result = new TKey[Count];
            Keys.CopyTo(result);
            return result;
        }
    }

    /// <inheritdoc />
    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
    {
        get
        {
            var result = new TValue[Count];
            Values.CopyTo(result);
            return result;
        }
    }

    /// <inheritdoc />
    ICollection<TKey> IDictionary<TKey, TValue>.Keys
    {
        get
        {
            var result = new TKey[Count];
            Keys.CopyTo(result);
            return result;
        }
    }

    /// <inheritdoc />
    ICollection<TValue> IDictionary<TKey, TValue>.Values
    {
        get
        {
            var result = new TValue[Count];
            Values.CopyTo(result);
            return result;
        }
    }

    /// <summary>
    /// Gets a collection containing the values in the dictionary.
    /// </summary>
    /// <value>A <see cref="ValueCollection"/> containing the values in the dictionary.</value>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public ValueCollection Values
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureNotDisposed();
            return new(m_Buckets.AsSpan(), m_Items.AsSpan(), Count);
        }
    }

    /// <summary>
    /// Gets a collection containing the keys in the dictionary.
    /// </summary>
    /// <value>A <see cref="KeyCollection"/> containing the keys in the dictionary.</value>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public KeyCollection Keys
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureNotDisposed();
            return new(m_Buckets.AsSpan(), m_Items.AsSpan(), Count);
        }
    }

    /// <inheritdoc />
    TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => TryGetValueRefOrAddDefault(key, out _);
    }

    /// <inheritdoc />
    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get => TryGetValueRefOrAddDefault(key, out _);
        set
        {
            EnsureNotDisposed();
            ref var target = ref TryGetValueRefOrAddDefault(key, out _);
            target = value;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Umap{TKey, TValue}"/> struct with a default capacity of 4.
    /// </summary>
    public Umap() : this(4) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Umap{TKey, TValue}"/> struct with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of elements that the dictionary can contain.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is less than 0.</exception>
    public Umap(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        m_BucketCount = capacity == 0 ? 4 : Hash.GetPrime(capacity);
        m_Buckets     = new Ulist<int>(m_BucketCount);
        for (var i = 0; i < m_BucketCount; i++)
        {
            m_Buckets.Add(-1);
        }

        m_Items      = new Ulist<Entry>(capacity);
        m_Count      = 0;
        m_FreeList   = -1;
        m_FreeCount  = 0;
        m_IsDisposed = false;
    }

    public Umap(ICollection<KeyValuePair<TKey, TValue>> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        m_BucketCount = collection.Count == 0 ? 4 : Hash.GetPrime(collection.Count);
        m_Buckets     = new Ulist<int>(m_BucketCount);
        for (var i = 0; i < m_BucketCount; i++)
        {
            m_Buckets.Add(-1);
        }

        m_Items      = new Ulist<Entry>(collection.Count);
        m_Count      = 0;
        m_FreeList   = -1;
        m_FreeCount  = 0;
        m_IsDisposed = false;

        foreach (var item in collection)
        {
            Add(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key by reference.
    /// </summary>
    /// <param name="key">The key of the value to get or set.</param>
    /// <returns>A reference to the value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the key is not found in the dictionary.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the dictionary has been disposed.</exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public ref TValue this[TKey key]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref TryGetValueRefOrAddDefault(key, out _);
    }

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    /// <inheritdoc cref="Add(KeyValuePair{TKey, TValue})"/>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add((TKey Key, TValue Value) item) => Add(item.Key, item.Value);

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        EnsureNotDisposed();
        DisposeElements();
        m_Count     = 0;
        m_FreeList  = -1;
        m_FreeCount = 0;
        m_Items.Clear();
        for (var i = 0; i < m_BucketCount; i++)
        {
            m_Buckets[i] = -1;
        }
    }

    /// <summary>
    /// Copies the elements of the dictionary to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the dictionary.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => CopyTo(array.AsSpan(), arrayIndex);

    /// <inheritdoc cref="CopyTo(KeyValuePair{TKey, TValue}[], int)"/>
    public void CopyTo(Span<KeyValuePair<TKey, TValue>> array, int arrayIndex)
    {
        EnsureNotDisposed();
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (array.Length - arrayIndex < m_Count)
            throw new ArgumentException("The number of elements in the source collection is greater than the available space.");

        var pos = 0;
        for (var b = 0; b < m_BucketCount; b++)
        {
            var i = m_Buckets[b];
            while (i != -1)
            {
                ref var e = ref m_Items[i];
                if (e.HashCode != -1)
                {
                    array[arrayIndex + pos++] = new KeyValuePair<TKey, TValue>(e.Key, e.Value);
                }

                i = e.Next;
            }
        }
    }

    /// <summary>
    /// Attempts to add the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <returns><c>true</c> if the key/value pair was added to the dictionary successfully; otherwise, <c>false</c>.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the dictionary has been disposed.</exception>
    public bool TryAdd(TKey key, TValue value)
    {
        EnsureNotDisposed();
        if (ContainsKey(key)) return false;

        // Reuse tombstone if available
        int index;
        if (m_FreeCount > 0)
        {
            index      = m_FreeList;
            m_FreeList = m_Items[index].Next;
            m_FreeCount--;
        }
        else
        {
            if (m_Items.Count == m_Items.Capacity)
                EnsureCapacity(m_Items.Count + 1);
            index = m_Items.Count;
            m_Items.Add(default);
        }

        var hashCode = ComputeHashCode(key);
        // Unsigned for speed
        var bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount);

        // Insert at head of bucket chain
        m_Items[index] = new Entry
        {
            Key      = key,
            Value    = value,
            HashCode = hashCode,
            Next     = m_Buckets[bucketIndex]
        };
        m_Buckets[bucketIndex] = index;
        m_Count++;

        if (m_Count > (int) (m_BucketCount * LOAD_FACTOR))
        {
            Resize(Hash.ExpandPrime(m_Count));
        }

        return true;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TKey key, TValue value)
    {
        if (!TryAdd(key, value)) throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
    }

    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.ContainsKey(TKey)"/>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsKey(TKey key) => FindIndex(key) >= 0;

    /// <inheritdoc />
    bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
    {
        var index = FindIndex(item.Key);
        return index >= 0 && EqualityComparer<TValue>.Default.Equals(m_Items[index].Value, item.Value);
    }

    /// <inheritdoc />
    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => CopyTo(array, arrayIndex);

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public bool Remove(TKey key)
    {
        EnsureNotDisposed();
        var hashCode = ComputeHashCode(key);
        var bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount);
        var comparer = EqualityComparer<TKey>.Default;
        var last = -1;

        for (var i = m_Buckets[bucketIndex]; i >= 0; last = i, i = m_Items[i].Next)
        {
            ref var entry = ref m_Items[i];
            if (entry.HashCode != hashCode || !comparer.Equals(entry.Key, key)) continue;

            // Remove from chain
            if (last < 0)
                m_Buckets[bucketIndex] = entry.Next;
            else
                m_Items[last].Next = entry.Next;

            // Mark as tombstone
            entry.HashCode = -1;
            entry.Next     = m_FreeList;
            m_FreeList     = i;
            m_FreeCount++;
            m_Count--;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to get a reference to the value associated with the specified key, or adds a new entry with the default value if the key does not exist.
    /// This method provides high-performance access by returning a reference to the value, allowing for direct modification without additional lookups.
    /// If the key is not found, a new entry is created with the default value for the value type.
    /// </summary>
    /// <param name="key">The key to search for or add to the dictionary.</param>
    /// <param name="exists">When this method returns, contains <c>true</c> if the key was found in the dictionary; otherwise, <c>false</c> if a new entry was created.</param>
    /// <returns>
    /// A reference to the value associated with the specified key. If the key existed, this is a reference to the existing value.
    /// If the key did not exist, this is a reference to the newly created entry's value, which is initialized to the default value of <typeparamref name="TValue"/>.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown when the dictionary has been disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue TryGetValueRefOrAddDefault(TKey key, out bool exists)
    {
        EnsureNotDisposed();

        var comparer = EqualityComparer<TKey>.Default;
        var hashCode = ComputeHashCode(key);
        var bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount); // Unsigned for perf

        // Search
        for (var i = m_Buckets[bucketIndex]; i >= 0; i = m_Items[i].Next)
        {
            ref var entry = ref m_Items[i];
            if (entry.HashCode == hashCode && comparer.Equals(entry.Key, key))
            {
                exists = true;
                return ref entry.Value;
            }
        }

        exists = false;

        // Resize if needed (before add)
        if (m_Count + 1 > (int) (m_BucketCount * LOAD_FACTOR))
        {
            Resize(Hash.ExpandPrime(m_Count + 1));
            // No need to search again—key wasn't present
            bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount); // Recalc post-resize
        }

        // Get slot
        int newIndex;
        if (m_FreeCount > 0)
        {
            newIndex   = m_FreeList;
            m_FreeList = m_Items[newIndex].Next;
            m_FreeCount--;
        }
        else
        {
            if (m_Items.Count >= m_Items.Capacity)
            {
                EnsureCapacity(m_Items.Count + 1);
                bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount); // Recalc if grew (rare)
            }

            newIndex = m_Items.Count;
            m_Items.Add(default);
        }

        // Insert
        m_Items[newIndex] = new Entry
        {
            Key      = key,
            Value    = default!,
            HashCode = hashCode,
            Next     = m_Buckets[bucketIndex]
        };
        m_Buckets[bucketIndex] = newIndex;
        m_Count++;

        return ref m_Items[newIndex].Value;
    }

    /// <inheritdoc cref="IReadOnlyDictionary{TKey,TValue}.TryGetValue(TKey, out TValue)"/>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(TKey key, out TValue value)
    {
        var index = FindIndex(key);
        if (index >= 0)
        {
            value = m_Items[index].Value;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Disposes all disposable elements (keys and/or values) stored in the hash map entries.
    /// This method iterates through all entries and calls the appropriate dispose methods
    /// for keys and values that implement IDisposable, ensuring proper cleanup of unmanaged
    /// resources before the hash map itself is disposed.
    /// </summary>
    internal void DisposeElements()
    {
        EnsureNotDisposed();
        if (Disposer<TKey>.IsDisposable && Disposer<TValue>.IsDisposable)
        {
            foreach (ref var item in m_Items)
            {
                Disposer<TKey>.Dispose(ref item.Key);
                Disposer<TValue>.Dispose(ref item.Value);
            }
        }
        else if (Disposer<TKey>.IsDisposable && !Disposer<TValue>.IsDisposable)
        {
            foreach (ref var item in m_Items)
            {
                Disposer<TKey>.Dispose(ref item.Key);
            }
        }
        else if (!Disposer<TKey>.IsDisposable && Disposer<TValue>.IsDisposable)
        {
            foreach (ref var item in m_Items)
            {
                Disposer<TValue>.Dispose(ref item.Value);
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="Umap{TKey, TValue}"/> instance.
    /// This method disposes all disposable elements (keys and values), releases the underlying
    /// unmanaged storage arrays, and resets all internal state to prevent further use.
    /// After disposal, the hash map becomes invalid and most operations will throw
    /// <see cref="ObjectDisposedException"/>.
    /// </summary>
    /// <remarks>
    /// This method is safe to call multiple times. Subsequent calls after the first
    /// disposal will have no effect. The method ensures proper cleanup of:
    /// <list type="bullet">
    /// <item><description>All disposable keys and values stored in the hash map</description></item>
    /// <item><description>The underlying unmanaged storage for entries and buckets</description></item>
    /// <item><description>All internal counters and state variables</description></item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (m_IsDisposed) return;
        DisposeElements();
        m_Items.Dispose();
        m_Buckets.Dispose();
        m_Count       = 0;
        m_BucketCount = 0;
        m_FreeCount   = 0;
        m_FreeList    = -1;
        m_IsDisposed  = true;
    }

    /// <summary>
    /// Searches for the index of an entry with the specified key in the hash map.
    /// This method traverses the bucket chain starting from the computed bucket index,
    /// comparing hash codes and keys until a match is found or the chain is exhausted.
    /// </summary>
    /// <param name="key">The key to search for in the hash map.</param>
    /// <returns>
    /// The zero-based index of the entry in the items array if the key is found;
    /// otherwise, -1 if the key does not exist in the hash map.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    private int FindIndex(TKey key)
    {
        EnsureNotDisposed();
        var comparer = EqualityComparer<TKey>.Default;
        var hashCode = ComputeHashCode(key);
        var bucketIndex = (int) ((uint) hashCode % (uint) m_BucketCount);

        for (var i = m_Buckets[bucketIndex]; i >= 0; i = m_Items[i].Next)
        {
            ref var entry = ref m_Items[i];
            if (entry.HashCode == hashCode && comparer.Equals(entry.Key, key))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Computes a hash code for the specified key with collision reduction.
    /// This method applies a simple mixing function to the key's hash code
    /// to improve distribution and reduce hash collisions.
    /// </summary>
    /// <param name="key">The key for which to compute the hash code.</param>
    /// <returns>
    /// A non-negative 32-bit integer hash code derived from the key's hash code
    /// with improved distribution characteristics.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ComputeHashCode(TKey key)
    {
        var h = key.GetHashCode();
        // Simple mixer: reduce collisions
        return (h ^ (h >> 16)) & 0x7FFFFFFF;
    }

    /// <summary>
    /// Resizes the hash map to accommodate a new capacity by rehashing all existing entries.
    /// This method creates new bucket and item arrays, then redistributes all non-tombstoned
    /// entries into the new bucket structure. The old arrays are disposed after migration.
    /// </summary>
    /// <param name="newSize">
    /// The desired new capacity for the hash map. If less than or equal to 0, defaults to 4.
    /// The actual bucket count will be adjusted to the nearest prime number for optimal distribution.
    /// </param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    private void Resize(int newSize)
    {
        EnsureNotDisposed();
        if (newSize <= 0) newSize = 4;
        var newBucketCount = Hash.GetPrime(newSize);
        var oldBuckets = m_Buckets;
        var oldItems = m_Items;
        var oldBucketCount = m_BucketCount;

        m_BucketCount = newBucketCount;
        m_Buckets     = new Ulist<int>(m_BucketCount);
        for (var i = 0; i < m_BucketCount; i++) m_Buckets.Add(-1);
        m_Items = new Ulist<Entry>(newSize);

        for (var b = 0; b < oldBucketCount; b++)
        {
            var i = oldBuckets[b];
            while (i != -1)
            {
                ref var oldE = ref oldItems[i];
                if (oldE.HashCode != -1)
                {
                    var hash = oldE.HashCode;
                    var bi = (int) ((uint) hash % (uint) m_BucketCount);
                    var ni = m_Items.Count;
                    // Set Next to current head
                    m_Items.Add(oldE with {HashCode = hash, Next = m_Buckets[bi]});
                    // New head
                    m_Buckets[bi] = ni;
                }

                i = oldE.Next;
            }
        }

        m_Count     = m_Items.Count;
        m_FreeList  = -1;
        m_FreeCount = 0;
        oldItems.Dispose();
        oldBuckets.Dispose();
    }

    /// <summary>
    /// Ensures that the hash map has sufficient capacity to accommodate the specified number of elements.
    /// If the current capacity is insufficient, this method triggers a resize operation when the
    /// new capacity would exceed the load factor threshold.
    /// </summary>
    /// <param name="capacity">
    /// The minimum capacity required for the hash map. The method ensures the hash map
    /// can accommodate at least this many elements.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int capacity)
    {
        if (m_Items.Capacity >= capacity) return;

        var newCapacity = Math.Max(capacity, m_Items.Capacity * 2);
        if (newCapacity > (int) (m_BucketCount * LOAD_FACTOR))
        {
            Resize(Hash.ExpandPrime(newCapacity));
        }
    }

    /// <summary>
    /// Verifies that the hash map has not been disposed and is still valid for operations.
    /// This method is called by public methods to ensure the hash map is in a usable state
    /// before performing any operations on its internal data structures.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotDisposed()
    {
        if (m_IsDisposed) throw new ObjectDisposedException(nameof(Umap<TKey, TValue>));
    }

    /// <summary>
    /// Returns a high-performance enumerator that iterates through the key-value pairs in the hash map.
    /// This method provides a ref struct enumerator that avoids heap allocations during enumeration.
    /// </summary>
    /// <returns>
    /// An <see cref="Enumerator"/> that can be used to iterate through the hash map's key-value pairs
    /// without heap allocations.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public Enumerator GetEnumerator()
    {
        EnsureNotDisposed();
        return new Enumerator(m_Items.AsSpan());
    }

    /// <inheritdoc />
    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        EnsureNotDisposed();
        for (var i = 0; i < m_Items.Count; i++)
        {
            yield return m_Items[i].GetPair();
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        EnsureNotDisposed();
        for (var i = 0; i < m_Items.Count; i++)
        {
            yield return m_Items[i].GetPair();
        }
    }

    /// <summary>
    /// Represents an internal entry structure that stores a key-value pair along with hash map metadata.
    /// This structure contains the actual data stored in the hash map as well as the hash code and
    /// chain linking information used for collision resolution through separate chaining.
    /// </summary>
    internal struct Entry
    {
        /// <summary>
        /// The key component of the key-value pair stored in this entry.
        /// </summary>
        public TKey Key;

        /// <summary>
        /// The value component of the key-value pair stored in this entry.
        /// </summary>
        public TValue Value;

        /// <summary>
        /// The cached hash code of the key used for bucket placement and collision detection.
        /// A value of -1 indicates this entry is a tombstone (deleted entry).
        /// </summary>
        public int HashCode;

        /// <summary>
        /// The index of the next entry in the collision chain, or -1 if this is the last entry in the chain.
        /// This field implements the separate chaining collision resolution mechanism.
        /// </summary>
        public int Next;

        /// <summary>
        /// Creates a key-value pair from this entry's key and value.
        /// This method provides a convenient way to convert an entry into
        /// a standard <see cref="KeyValuePair{TKey, TValue}"/> structure.
        /// </summary>
        /// <returns>
        /// A <see cref="KeyValuePair{TKey, TValue}"/> containing this entry's key and value.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public KeyValuePair<TKey, TValue> GetPair() => new(Key, Value);
    }

    /// <summary>
    /// Provides a high-performance enumerator for iterating over key-value pairs in the unmanaged hash map.
    /// This ref struct enumerator traverses the hash map's bucket chains, skipping tombstoned entries
    /// and providing efficient iteration without heap allocations.
    /// </summary>
    public ref struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        private readonly Span<Entry> m_Items;
        private          int         m_ItemIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="items">A span containing the entries storage with key-value pairs and chain links.</param>
        internal Enumerator(Span<Entry> items)
        {
            m_Items     = items;
            m_ItemIndex = -1;
        }

        /// <inheritdoc />
        public KeyValuePair<TKey, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Items[m_ItemIndex].GetPair();
        }

        /// <inheritdoc />
        object IEnumerator.Current => m_Items[m_ItemIndex].GetPair();

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_ItemIndex < m_Items.Length;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_ItemIndex = -1;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { } // Do nothing.
    }

    /// <summary>
    /// Represents a collection of keys in the unmanaged hash map. This ref struct provides
    /// read-only access to the keys without heap allocations, implementing both
    /// <see cref="ICollection{T}"/> and <see cref="IReadOnlyCollection{T}"/> interfaces.
    /// </summary>
    public ref struct KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>
    {
        private readonly Span<int>   m_Buckets;
        private readonly Span<Entry> m_Items;
        private readonly int         m_Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyCollection"/> struct.
        /// </summary>
        /// <param name="buckets">A span containing the bucket heads array where each element points to the first entry in that bucket's chain.</param>
        /// <param name="items">A span containing the entries storage with key-value pairs and chain links.</param>
        /// <param name="count">The actual number of keys (non-tombstones).</param>
        internal KeyCollection(Span<int> buckets, Span<Entry> items, int count)
        {
            m_Buckets = buckets;
            m_Items   = items;
            m_Count   = count;
        }

        /// <inheritdoc cref="ICollection{T}.Count"/>
        public int Count => m_Count;

        /// <inheritdoc />
        bool ICollection<TKey>.IsReadOnly => true;

        /// <inheritdoc />
        void ICollection<TKey>.Add(TKey item) => throw new NotSupportedException();

        /// <inheritdoc />
        void ICollection<TKey>.Clear() => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Contains(TKey item)
        {
            var comparer = EqualityComparer<TKey>.Default;
            for (var b = 0; b < m_Buckets.Length; b++)
            {
                var i = m_Buckets[b];
                while (i != -1)
                {
                    ref var e = ref m_Items[i];
                    if (e.HashCode != -1 && comparer.Equals(e.Key, item))
                    {
                        return true;
                    }

                    i = e.Next;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> containing all keys from the collection.
        /// This method provides a convenient way to convert the key collection to a standard
        /// managed list for scenarios where heap allocation is acceptable.
        /// </summary>
        /// <returns>
        /// A new <see cref="List{TKey}"/> containing all keys from the hash map.
        /// The order of keys in the list corresponds to the enumeration order of the collection.
        /// </returns>
        public List<TKey> ToList()
        {
            using var result = new Uarray<TKey>(m_Count);
            CopyTo(result);
            return result.ToList();
        }

        /// <summary>
        /// Creates a new array containing all keys from the collection.
        /// This method provides a convenient way to convert the key collection to a standard
        /// managed array for scenarios where heap allocation is acceptable.
        /// </summary>
        /// <returns>
        /// A new array of type <typeparamref name="TKey"/> containing all keys from the hash map.
        /// The order of keys in the array corresponds to the enumeration order of the collection.
        /// </returns>
        public TKey[] ToArray()
        {
            var result = new TKey[m_Count];
            CopyTo(result);
            return result;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TKey[] array, int arrayIndex = 0) => CopyTo(array.AsSpan(), arrayIndex);

        /// <summary>
        /// Copies all keys from the collection to a span, starting at the specified index.
        /// </summary>
        /// <param name="array">The destination span to copy keys to.</param>
        /// <param name="arrayIndex">The zero-based index in the span at which copying begins.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">Thrown when the destination span does not have enough space to accommodate all keys.</exception>
        public void CopyTo(Span<TKey> array, int arrayIndex = 0)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < m_Count)
                throw new ArgumentException("The number of elements in the source collection is greater than the available space.");

            for (var i = 0; i < m_Items.Length; i++)
            {
                array[i + arrayIndex] = m_Items[i].Key;
            }
        }

        /// <inheritdoc />
        bool ICollection<TKey>.Remove(TKey item) => throw new NotSupportedException();

        /// <summary>
        /// Returns an enumerator that iterates through the key collection.
        /// </summary>
        /// <returns>A <see cref="KeyEnumerator"/> for the key collection.</returns>
        public KeyEnumerator GetEnumerator() => new(m_Items);

        /// <inheritdoc />
        IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator() => ToList().GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ToArray().GetEnumerator();
    }

    /// <summary>
    /// Provides a high-performance enumerator for iterating over keys in the unmanaged hash map.
    /// This ref struct enumerator traverses the hash map's bucket chains, skipping tombstoned entries
    /// and providing efficient key iteration without heap allocations.
    /// </summary>
    public ref struct KeyEnumerator : IEnumerator<TKey>
    {
        private readonly Span<Entry> m_Items;

        private int m_ItemIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyEnumerator"/> struct.
        /// </summary>
        /// <param name="items">A span containing the entries storage with key-value pairs and chain links.</param>
        internal KeyEnumerator(Span<Entry> items)
        {
            m_Items     = items;
            m_ItemIndex = -1;
        }

        /// <inheritdoc />
        public TKey Current => m_Items[m_ItemIndex].Key;

        /// <inheritdoc />
        object IEnumerator.Current => m_Items[m_ItemIndex].Key!;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_ItemIndex < m_Items.Length;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { } // Do nothing.

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_ItemIndex = -1;
    }

    /// <summary>
    /// Represents a collection of values in the unmanaged hash map. This ref struct provides
    /// read-only access to the values without heap allocations, implementing both
    /// <see cref="ICollection{T}"/> and <see cref="IReadOnlyCollection{T}"/> interfaces.
    /// </summary>
    public ref struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>
    {
        private readonly Span<int>   m_Buckets;
        private readonly Span<Entry> m_Items;
        private readonly int         m_Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueCollection"/> struct.
        /// </summary>
        /// <param name="buckets">A span containing the bucket heads array where each element points to the first entry in that bucket's chain.</param>
        /// <param name="items">A span containing the entries storage with key-value pairs and chain links.</param>
        /// <param name="count">The actual number of values (non-tombstones).</param>
        internal ValueCollection(Span<int> buckets, Span<Entry> items, int count)
        {
            m_Buckets = buckets;
            m_Items   = items;
            m_Count   = count;
        }

        /// <inheritdoc cref="ICollection{T}.Count"/>
        public int Count => m_Count;

        /// <inheritdoc />
        bool ICollection<TValue>.IsReadOnly => true;

        /// <inheritdoc />
        void ICollection<TValue>.Add(TValue item) => throw new NotSupportedException();

        /// <inheritdoc />
        void ICollection<TValue>.Clear() => throw new NotSupportedException();

        /// <inheritdoc />
        public bool Contains(TValue item)
        {
            var comparer = EqualityComparer<TValue>.Default;
            for (var b = 0; b < m_Buckets.Length; b++)
            {
                var i = m_Buckets[b];
                while (i != -1)
                {
                    ref var e = ref m_Items[i];
                    if (e.HashCode != -1 && comparer.Equals(e.Value, item))
                    {
                        return true;
                    }

                    i = e.Next;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new <see cref="List{T}"/> containing all keys from the collection.
        /// This method provides a convenient way to convert the key collection to a standard
        /// managed list for scenarios where heap allocation is acceptable.
        /// </summary>
        /// <returns>
        /// A new <see cref="List{TKey}"/> containing all keys from the hash map.
        /// The order of keys in the list corresponds to the enumeration order of the collection.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public List<TValue> ToList()
        {
            using var result = new Uarray<TValue>(m_Count);
            CopyTo(result);
            return result.ToList();
        }

        /// <summary>
        /// Creates a new array containing all keys from the collection.
        /// This method provides a convenient way to convert the key collection to a standard
        /// managed array for scenarios where heap allocation is acceptable.
        /// </summary>
        /// <returns>
        /// A new array of type <typeparamref name="TKey"/> containing all keys from the hash map.
        /// The order of keys in the array corresponds to the enumeration order of the collection.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue[] ToArray()
        {
            var result = new TValue[m_Count];
            CopyTo(result);
            return result;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(TValue[] array, int arrayIndex = 0) => CopyTo(array.AsSpan(), arrayIndex);

        /// <summary>
        /// Copies all values from the collection to a span, starting at the specified index.
        /// </summary>
        /// <param name="array">The destination span to copy values to.</param>
        /// <param name="arrayIndex">The zero-based index in the span at which copying begins.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="ArgumentException">Thrown when the destination span does not have enough space to accommodate all values.</exception>
        public void CopyTo(Span<TValue> array, int arrayIndex = 0)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < m_Count)
                throw new ArgumentException("The number of elements in the source collection is greater than the available space.");

            for (var i = 0; i < m_Items.Length; i++)
            {
                array[i + arrayIndex] = m_Items[i].Value;
            }
        }

        /// <inheritdoc />
        bool ICollection<TValue>.Remove(TValue item) => throw new NotSupportedException();

        /// <summary>
        /// Returns an enumerator that iterates through the value collection.
        /// </summary>
        /// <returns>A <see cref="ValueEnumerator"/> for the value collection.</returns>
        public ValueEnumerator GetEnumerator() => new(m_Items);

        /// <inheritdoc />
        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => ToList().GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => ToList().GetEnumerator();
    }

    /// <summary>
    /// Provides a high-performance enumerator for iterating over values in the unmanaged hash map.
    /// This ref struct enumerator traverses the hash map's bucket chains, skipping tombstoned entries
    /// and providing efficient value iteration without heap allocations.
    /// </summary>
    public ref struct ValueEnumerator : IEnumerator<TValue>
    {
        private readonly Span<Entry> m_Items;
        private          int         m_ItemIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueEnumerator"/> struct.
        /// </summary>
        /// <param name="items">A span containing the entries storage with key-value pairs and chain links.</param>
        internal ValueEnumerator(Span<Entry> items)
        {
            m_Items     = items;
            m_ItemIndex = -1;
        }

        /// <summary>
        /// Gets a reference to the current value in the enumeration.
        /// </summary>
        /// <value>A reference to the current value element in the hash map.</value>
        public ref TValue Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref var entry = ref m_Items[m_ItemIndex];
                return ref entry.Value;
            }
        }

        /// <inheritdoc />
        TValue IEnumerator<TValue>.Current => Current;

        /// <inheritdoc />
        object IEnumerator.Current => Current!;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_ItemIndex < m_Items.Length;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_ItemIndex = -1;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() { } // Do nothing.
    }
}