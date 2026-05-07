using System.Collections;
using System.Runtime.CompilerServices;

namespace Gubbins.Unmanaged;

/// <summary>
/// Represents an unmanaged resizable list that provides dynamic array functionality with automatic memory management through unmanaged memory allocation.
/// This structure implements multiple collection interfaces and supports efficient operations on elements of type <typeparamref name="T"/>.
/// The list automatically expands its capacity when needed and provides both fast ref-based access and standard collection operations.
/// </summary>
/// <typeparam name="T">The type of elements stored in the unmanaged list. This type should be unmanaged or blittable for optimal performance.</typeparam>
public struct Ulist<T> : IList, IList<T>, IReadOnlyList<T>, IDisposable where T : unmanaged
{
    /// <summary>
    /// The current capacity of the underlying array, representing the maximum number of elements that can be stored without expansion.
    /// </summary>
    private int m_Capacity;

    /// <summary>
    /// The current number of elements actually stored in the list.
    /// </summary>
    private int m_Count;

    /// <summary>
    /// Indicates whether this UnmanagedList instance has been disposed and its resources returned to the pool.
    /// </summary>
    private bool m_IsDisposed;

    /// <inheritdoc />
    bool IList.IsFixedSize => false;

    /// <summary>
    /// Gets a reference to the element at the specified index, allowing direct modification without copying.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get a reference to.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the index is less than 0 or greater than or equal to Count.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (index < 0 || index >= m_Count) throw new IndexOutOfRangeException($"Index '{index}' out of range.");
            return ref m_Array[index];
        }
    }

    /// <inheritdoc />
    T IList<T>.this[int index]
    {
        get => m_Array[index];
        set => m_Array[index] = value;
    }

    /// <inheritdoc />
    T IReadOnlyList<T>.this[int index] => m_Array[index];

    /// <inheritdoc />
    object IList.this[int index]
    {
        get => ((IList) m_Array)[index];
        set => ((IList) m_Array)[index] = value;
    }

    /// <summary>
    /// Gets a value indicating whether this unmanaged array instance is valid and can be safely used.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the array has allocated memory and has not been disposed; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !m_IsDisposed && m_Array.IsValid;
    }

    /// <summary>
    /// Gets a value indicating whether this unmanaged array contains no elements.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the array has zero length; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Count == 0;
    }

    /// <inheritdoc cref="IReadOnlyList{T}.Count"/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Count;
    }

    /// <summary>
    /// The current capacity of the underlying array, representing the maximum number of elements that can be stored without expansion.
    /// </summary>
    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Capacity;
    }

    /// <inheritdoc />
    bool ICollection<T>.IsReadOnly => false;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot => m_Array;

    /// <inheritdoc />
    bool IList.IsReadOnly => false;

    /// <summary>
    /// Gets the underlying ExpandArray instance from the unmanaged memory address.
    /// </summary>
    /// <returns>The ExpandArray instance stored at the unmanaged memory address.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the underlying array has been disposed or the address is invalid.</exception>
    private Uarray<T> m_Array;

    /// <summary>
    /// Initializes a new instance of the UnmanagedList struct by spawning an ExpandArray from the unmanaged pool.
    /// </summary>
    public Ulist()
    {
        m_Array    = Uarray<T>.Empty;
        m_Capacity = 0;
        m_Count    = 0;
    }

    /// <summary>
    /// Initializes a new instance of the Ulist struct with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial capacity of the list, representing the number of elements that can be stored without expansion.</param>
    public Ulist(int capacity)
    {
        m_Array    = new Uarray<T>(capacity);
        m_Capacity = capacity;
        m_Count    = 0;
    }

    /// <summary>
    /// Initializes a new instance of the Ulist struct with elements copied from the specified span.
    /// </summary>
    /// <param name="items">A read-only span containing the elements to copy into the new list.</param>
    public Ulist(ReadOnlySpan<T> items)
    {
        m_Capacity = items.Length;
        m_Array    = new Uarray<T>(items);
        m_Count    = items.Length;
    }

    /// <summary>
    /// Returns a fast enumerator that provides direct access to the list elements without boxing.
    /// </summary>
    /// <returns>An Enumerator struct that can iterate over the elements in the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        EnsureNotDisposed();
        return new Enumerator(m_Array.AsSpan().Slice(0, m_Count));
    }

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        EnsureNotDisposed();
        return new SlowEnumerator(m_Array, m_Count);
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        EnsureNotDisposed();
        return new SlowEnumerator(m_Array, m_Count);
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if (m_Count >= m_Capacity)
        {
            AutoExpand();
        }

        m_Array[m_Count] = item;
        m_Count++;
    }

    /// <inheritdoc />
    int IList.Add(object? value)
    {
        if (value is T item)
        {
            Add(item);
            return m_Count - 1;
        }

        throw new ArgumentException("Value is not of the correct type", nameof(value));
    }

    /// <summary>
    /// Removes all elements from the list and optionally resets the underlying array elements to their default values.
    /// </summary>
    /// <param name="reset">If true, clears the underlying array elements to their default values and dispose them; otherwise, only resets the count.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear(bool reset)
    {
        EnsureNotDisposed();
        if (reset)
        {
            DisposeElements();
            m_Array.AsSpan().Clear();
        }
        m_Count = 0;
    }

    /// <inheritdoc cref="IList.Clear()"/>
    /// <remarks>It will clear the underlying array elements to their default values and dispose them.</remarks>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => Clear(true);

    /// <summary>
    /// Determines whether the list contains the specified value using the default equality comparer.
    /// </summary>
    /// <param name="value">The value to search for in the list.</param>
    /// <returns>true if the value is found in the list; otherwise, false.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T value) => IndexOf(value) >= 0;

    /// <summary>
    /// Searches for the specified value and returns the zero-based index of the first occurrence within the list.
    /// </summary>
    /// <param name="value">The value to search for in the list.</param>
    /// <returns>The zero-based index of the first occurrence of the value if found; otherwise, -1.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(T value)
    {
        EnsureNotDisposed();
        var span = m_Array.AsSpan();
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Count; i++)
        {
            if (comparer.Equals(span[i], value))
            {
                return i;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    bool IList.Contains(object? value) => ((IList) m_Array).Contains(value);

    /// <inheritdoc />
    int IList.IndexOf(object value) => ((IList) m_Array).IndexOf(value);

    /// <inheritdoc />
    void IList.Insert(int index, object value) => ((IList) m_Array).Insert(index, value);

    /// <inheritdoc />
    void IList.Remove(object value)
    {
        if (value is T item)
            Remove(item);
    }

    /// <summary>
    /// Determines whether this list and the specified span contain the same sequence of elements.
    /// </summary>
    /// <param name="other">The span to compare with this array.</param>
    /// <returns><see langword="true"/> if the arrays contain the same sequence of elements; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public bool SequenceEqual(ReadOnlySpan<T> other)
    {
        EnsureNotDisposed();
        if (other.Length != m_Count) return false;
        var items = m_Array.AsSpan();
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Count; i++)
        {
            if (!comparer.Equals(items[i], other[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of the unmanaged list.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> that provides access to the list elements.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        EnsureNotDisposed();
        return m_Array.AsSpan()[..m_Count];
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of a portion of the unmanaged list starting at the specified index with the specified length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to include in the span.</param>
    /// <param name="length">The number of elements to include in the span.</param>
    /// <returns>A <see cref="Span{T}"/> that provides access to the specified portion of the array elements.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start, int length)
    {
        EnsureNotDisposed();
        return m_Array.AsSpan()[start..(start + length)];
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of a portion of the unmanaged list starting at the specified index and continuing to the end of the list.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to include in the span.</param>
    /// <returns>A <see cref="Span{T}"/> that provides access to the array elements from the specified starting index to the end of the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start)
    {
        EnsureNotDisposed();
        return m_Array.AsSpan()[start..(m_Count - start)];
    }

    /// <summary>
    /// Returns a view of the list as an <see cref="Uarray"/> containing only the elements currently in use.
    /// </summary>
    /// <returns>A Uarray representing a slice of the underlying array from index 0 to Count.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Uarray<T> AsUarray()
    {
        EnsureNotDisposed();
        return m_Array.Slice(0, m_Count);
    }

    /// <summary>
    /// Creates a new <see cref="Uarray"/> containing a subset of elements from the list starting at the specified index with the specified length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the subset to extract.</param>
    /// <param name="length">The number of elements to include in the new Uarray.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new Uarray; otherwise, leaves the original list intact.</param>
    /// <returns>A new Uarray containing the specified subset of elements from the original list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Uarray<T> ToUarray(int start, int length, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var span = m_Array.AsSpan(start, length);
        var result = new Uarray<T>(span);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new <see cref="Uarray"/> containing all elements from the list starting at the specified index to the end of the list.
    /// </summary>
    /// <param name="start">The zero-based starting index from which to begin extracting elements.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new Uarray; otherwise, leaves the original list intact.</param>
    /// <returns>A new <see cref="Uarray"/> containing all elements from the specified starting index to the end of the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Uarray<T> ToUarray(int start, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = new Uarray<T>(m_Array.AsSpan(start, m_Count - start));
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new <see cref="Uarray"/> containing all elements currently in the list.
    /// </summary>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new Uarray; otherwise, leaves the original list intact.</param>
    /// <returns>A new <see cref="Uarray"/> containing a copy of all elements from the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Uarray<T> ToUarray(bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = new Uarray<T>(m_Array.AsSpan());
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed array containing all elements currently in the list.
    /// </summary>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new array; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed array containing copies of all elements from the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public T[] ToArray(bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = AsUarray().ToArray(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed array containing elements from the list starting at the specified index to the end of the list.
    /// </summary>
    /// <param name="start">The zero-based starting index from which to begin extracting elements.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new array; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed array containing copies of all elements from the specified starting index to the end of the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public T[] ToArray(int start, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = m_Array.Slice(start, m_Count - start).ToArray(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed array containing a subset of elements from the list starting at the specified index with the specified length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the subset to extract.</param>
    /// <param name="length">The number of elements to include in the new array.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new array; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed array containing copies of the specified subset of elements from the original list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public T[] ToArray(int start, int length, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = m_Array.Slice(start, length).ToArray(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed List containing all elements currently in the list.
    /// </summary>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new List; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed List containing copies of all elements from the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = AsUarray().ToList(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed List containing elements from the list starting at the specified index to the end of the list.
    /// </summary>
    /// <param name="start">The zero-based starting index from which to begin extracting elements.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new List; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed List containing copies of all elements from the specified starting index to the end of the list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(int start, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = m_Array.Slice(start, m_Count - start).ToList(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Creates a new managed List containing a subset of elements from the list starting at the specified index with the specified length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the subset to extract.</param>
    /// <param name="length">The number of elements to include in the new List.</param>
    /// <param name="disposeOrigin">If true, disposes the original list after creating the new List; otherwise, leaves the original list intact.</param>
    /// <returns>A new managed List containing copies of the specified subset of elements from the original list.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(int start, int length, bool disposeOrigin = true)
    {
        EnsureNotDisposed();
        var result = m_Array.Slice(start, length).ToList(false);
        if (disposeOrigin)
        {
            Dispose();
        }

        return result;
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array.
    /// </summary>
    /// <param name="destination">The destination unmanaged array to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void CopyTo(ref Uarray<T> destination)
    {
        EnsureNotDisposed();
        var dst = destination.AsSpan();
        var src = m_Array.AsSpan()[..m_Count];
        src.CopyTo(dst);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination span.
    /// </summary>
    /// <param name="destination">The destination span to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void CopyTo(Span<T> destination)
    {
        EnsureNotDisposed();
        var src = m_Array.AsSpan()[..m_Count];
        src.CopyTo(destination);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array.
    /// </summary>
    /// <param name="array">The destination array to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void CopyTo(T[] array)
    {
        EnsureNotDisposed();
        var dst = array.AsSpan();
        var src = m_Array.AsSpan()[..m_Count];
        src.CopyTo(dst);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array to copy elements to.</param>
    /// <param name="index">The zero-based index in the destination array at which copying begins.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void CopyTo(T[] array, int index)
    {
        EnsureNotDisposed();
        var dst = array.AsSpan(index);
        var src = m_Array.AsSpan()[..m_Count];
        src.CopyTo(dst);
    }

    public bool Remove(T item, bool dispose)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            ref var target = ref RemoveAndGetRefAt(index);
            if (dispose && Disposer<T>.IsDisposable)
            {
                Disposer<T>.Dispose(ref target);
            }
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ICollection.CopyTo(Array array, int index) => ((ICollection) m_Array).CopyTo(array, index);

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Insert(int index, T item)
    {
        EnsureNotDisposed();
        if (index < 0 || index > m_Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (m_Count >= m_Capacity)
        {
            AutoExpand();
        }

        if (index < m_Count)
        {
            var span = m_Array.AsSpan();
            span.Slice(index, m_Count - index).CopyTo(span.Slice(index + 1));
        }

        m_Array[index] = item;
        m_Count++;
    }

    /// <summary>
    /// Removes the last element from the list.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the list is empty and there are no elements to remove.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public void RemoveLast() => RemoveAt(m_Count - 1);

    /// <inheritdoc cref="IList{T}.Count"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAt(int index)
    {
        ref var target = ref RemoveAndGetRefAt(index);
        if (Disposer<T>.IsDisposable)
        {
            Disposer<T>.Dispose(ref target);
        }
    }

    /// <summary>
    /// Removes the element at the specified index and returns a reference to the removed element.
    /// The element is removed from the list but not disposed, allowing the caller to handle disposal or reuse of the element.
    /// </summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <returns>A reference to the element that was removed from the list.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the index is less than 0 or greater than or equal to Count.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public ref T RemoveAndGetRefAt(int index)
    {
        EnsureNotDisposed();
        if (index < 0 || index >= m_Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index < m_Count - 1)
        {
            var span = m_Array.AsSpan();
            span.Slice(index + 1, m_Count - index - 1).CopyTo(span.Slice(index));
        }

        return ref m_Array[--m_Count];
    }
    /// <summary>
    /// Disposes all elements currently stored in the list by calling their dispose methods if they implement IDisposable.
    /// This method is used internally to clean up resources held by individual elements before the list itself is disposed.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void DisposeElements() => m_Array.DisposeElements(0, m_Count);

    /// <summary>
    /// Releases all resources used by the Ulist instance, including disposing all contained elements and returning the underlying unmanaged memory to the pool.
    /// After calling this method, the list becomes invalid and should not be used further. This method is safe to call multiple times.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (m_IsDisposed)
        {
            return;
        }

        if (!m_Array.IsValid)
        {
            DisposeElements();
            m_Array.DisposeOnlySelf();
        }

        m_IsDisposed = true;
        m_Count      = 0;
        m_Capacity   = 0;
    }

    /// <summary>
    /// Ensures that this Ulist instance has not been disposed and is still valid for use.
    /// This method is used internally to validate the state of the list before performing operations that require access to the underlying unmanaged memory.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the Ulist instance has been disposed and is no longer valid for use.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotDisposed()
    {
        if (m_IsDisposed) throw new ObjectDisposedException(nameof(Ulist<T>));
    }

    /// <summary>
    /// Automatically expands the capacity of the underlying array when more space is needed, doubling the current capacity or setting it to 4 if currently empty.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    private void AutoExpand()
    {
        EnsureNotDisposed();
        var newCapacity = m_Capacity == 0 ? 4 : m_Capacity * 2;
        var newArray = new Uarray<T>(newCapacity);

        if (m_Count > 0)
        {
            var sourceSpan = m_Array.AsSpan()[..m_Count];
            var destSpan = newArray.AsSpan();
            sourceSpan.CopyTo(destSpan);
        }

        m_Array.Dispose();
        m_Array    = newArray;
        m_Capacity = newCapacity;
    }

    /// <summary>
    /// Provides a fast enumerator for the unmanaged array using ref struct.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// The span being enumerated.
        /// </summary>
        private readonly Span<T> m_Span;

        /// <summary>
        /// The current index position in the enumeration, starting at -1 before the first element.
        /// </summary>
        private int m_Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="span">The span to enumerate.</param>
        public Enumerator(Span<T> span)
        {
            m_Span  = span;
            m_Index = -1;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_Index < m_Span.Length;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_Index = -1;

        /// <summary>
        /// Gets a reference to the current element.
        /// </summary>
        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_Span[m_Index];
        }

        /// <inheritdoc />
        T IEnumerator<T>.Current => m_Span[m_Index];

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() { }
    }

    /// <summary>
    /// Provides a slower enumerator for the unmanaged array that implements IEnumerator.
    /// </summary>
    private struct SlowEnumerator : IEnumerator<T>
    {
        /// <summary>
        /// The underlying unmanaged array being enumerated.
        /// </summary>
        private readonly Uarray<T> m_Array;

        /// <summary>
        /// The total number of elements to enumerate, representing the actual count of items in the list.
        /// </summary>
        private readonly int m_Length;

        /// <summary>
        /// The current position in the enumeration sequence, starting at -1 before the first element.
        /// </summary>
        private int m_Index;

        /// <summary>
        /// Initializes a new instance of the <see cref="SlowEnumerator"/> struct.
        /// </summary>
        /// <param name="array">The array to enumerate.</param>
        /// <param name="length">The actual count of <see cref="Ulist{T}"/>.</param>
        public SlowEnumerator(Uarray<T> array, int length)
        {
            m_Array  = array;
            m_Index  = -1;
            m_Length = length;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_Index < m_Length;

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_Index = -1;

        /// <inheritdoc />
        public T Current => m_Array[m_Index];

        /// <inheritdoc />
        object? IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => Reset();
    }
}