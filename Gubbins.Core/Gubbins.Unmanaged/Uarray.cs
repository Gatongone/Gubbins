using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Unmanaged;

/// <summary>
/// Represents an unmanaged array that stores elements in unmanaged memory.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public unsafe struct Uarray<T> : IDisposable, IReadOnlyCollection<T>, IList, IStructuralComparable, IStructuralEquatable, ICloneable where T : unmanaged
{
    /// <summary>
    /// Gets an empty unmanaged array instance that contains no elements.
    /// </summary>
    /// <value>A read-only static instance of <see cref="Uarray{T}"/> with zero length.</value>
    public static readonly Uarray<T> Empty = new();

    /// <summary>
    /// The size of a single element in bytes.
    /// </summary>
    public static readonly int ElementSize = (int) Native.GetStackSize<T>();

    /// <summary>
    /// Indicates whether this UnmanagedList instance has been disposed and its resources returned to the pool.
    /// </summary>
    private bool m_IsDisposed;

    /// <summary>
    /// The number of elements in the unmanaged array.
    /// </summary>
    private int m_Length;

    /// <summary>
    /// The memory address pointing to the unmanaged memory block containing the array elements.
    /// </summary>
    private nint m_Address;

    /// <summary>
    /// Gets a value indicating whether this unmanaged array instance is valid and can be safely used.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the array has allocated memory and has not been disposed; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsValid
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Address != IntPtr.Zero && !m_IsDisposed;
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
        get => m_Length == 0;
    }

    /// <inheritdoc cref="IReadOnlyList{T}.Count"/>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Length;
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>A reference to the element at the specified index.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the hash map has been disposed and is no longer valid for use.
    /// </exception>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureNotDisposed();
            if (index < 0 || index >= m_Length) throw new IndexOutOfRangeException($"Index '{index}' out of range.");
            return ref m_Items[index];
        }
    }

    /// <inheritdoc />
    bool IList.IsFixedSize => true;

    /// <inheritdoc />
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc />
    object ICollection.SyncRoot => null!;

    /// <inheritdoc />
    bool IList.IsReadOnly => false;

    /// <inheritdoc />
    object IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T) value;
    }

    private readonly Span<T> m_Items
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new((void*) m_Address, m_Length);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Uarray{T}"/> struct with the specified length.
    /// </summary>
    /// <param name="length">The number of elements in the array.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public Uarray(int length)
    {
        m_Address = Native.Allocate(length * sizeof(T), Native.GetAlignment<T>());
        m_Length  = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Uarray{T}"/> struct with elements copied from the specified span.
    /// </summary>
    /// <param name="items">The span of items to copy.</param>/// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public Uarray(ReadOnlySpan<T> items)
    {
        EnsureNotDisposed();
        m_Length  = items.Length;
        m_Address = Native.Allocate(m_Length * sizeof(T), Native.GetAlignment<T>());
        items.CopyTo(m_Items);
    }

    /// <summary>
    /// Creates a new unmanaged array that represents a slice of this array, sharing the same underlying memory.
    /// </summary>
    /// <param name="start">The zero-based starting index of the slice within the current array.</param>
    /// <param name="length">The number of elements to include in the slice.</param>
    /// <returns>A new <see cref="Uarray{T}"/> instance that shares memory with this array, starting at the specified index and containing the specified number of elements.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when this array instance has been disposed or is invalid.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Uarray<T> Slice(int start, int length)
    {
        if (!IsValid) throw new ObjectDisposedException(nameof(Uarray<T>));
        return new Uarray<T> {m_Address = new IntPtr((byte*) m_Address + start * ElementSize), m_Length = length};
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of a portion of the unmanaged array starting at the specified index with the specified length.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to include in the span.</param>
    /// <param name="length">The number of elements to include in the span.</param>
    /// <returns>A <see cref="Span{T}"/> that provides access to the specified portion of the array elements.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start, int length)
    {
        EnsureNotDisposed();
        return m_Items[start..(start + length)];
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of a portion of the unmanaged array starting at the specified index and continuing to the end of the array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to include in the span.</param>
    /// <returns>A <see cref="Span{T}"/> that provides access to the array elements from the specified starting index to the end of the array.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan(int start)
    {
        EnsureNotDisposed();
        return m_Items[start..];
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> representation of the unmanaged array.
    /// </summary>
    /// <returns>A <see cref="Span{T}"/> that provides access to the array elements.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> AsSpan()
    {
        EnsureNotDisposed();
        return m_Items;
    }

    /// <summary>
    /// Converts this unmanaged array to a managed array, optionally disposing the original array.
    /// </summary>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new managed array containing copies of all elements from this unmanaged array.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray(bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new T[m_Length];
        CopyTo(result);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts this unmanaged array to a managed array starting from the specified index, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new managed array containing copies of all elements from this unmanaged array, with elements copied starting at the specified index.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray(int start, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new T[m_Length - start];
        m_Items[start..].CopyTo(result);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts a portion of this unmanaged array to a managed array, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="length">The number of elements to include in the conversion.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new managed array containing copies of the specified portion of elements from this unmanaged array.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray(int start, int length, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new T[length];
        m_Items[start..(start + length)].CopyTo(result);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts this unmanaged array to a managed list, optionally disposing the original array.
    /// </summary>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="List{T}"/> containing copies of all elements from this unmanaged array.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new List<T>(m_Length);
        for (var i = 0; i < m_Length; i++) result.Add(this[i]);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts this unmanaged array to a managed list starting from the specified index, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="List{T}"/> containing copies of elements from this unmanaged array starting at the specified index.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(int start, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var length = m_Length - start;
        var result = new List<T>(length);
        for (var i = start; i < start + length; i++) result.Add(this[i]);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts a portion of this unmanaged array to a managed list, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="length">The number of elements to include in the conversion.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="List{T}"/> containing copies of the specified portion of elements from this unmanaged array.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public List<T> ToList(int start, int length, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new List<T>(length);
        for (var i = start; i < start + length; i++) result.Add(this[i]);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts this unmanaged array to an unmanaged list, optionally disposing the original array.
    /// </summary>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="Ulist{T}"/> instance containing the same elements as this array.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ulist<T> ToUlist(bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new Ulist<T>(this);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts this unmanaged array to an unmanaged list starting from the specified index, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="Ulist{T}"/> instance containing elements from this array starting at the specified index.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ulist<T> ToUlist(int start, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new Ulist<T>(this[start..]);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Converts a portion of this unmanaged array to an unmanaged list, optionally disposing the original array.
    /// </summary>
    /// <param name="start">The zero-based starting index of the portion to convert.</param>
    /// <param name="length">The number of elements to include in the conversion.</param>
    /// <param name="disposedOrigin">
    /// <see langword="true"/> to dispose this array instance after conversion; 
    /// <see langword="false"/> to keep this array instance valid after conversion. 
    /// The default value is <see langword="true"/>.
    /// </param>
    /// <returns>
    /// A new <see cref="Ulist{T}"/> instance containing the specified portion of elements from this array.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ulist<T> ToUlist(int start, int length, bool disposedOrigin = true)
    {
        EnsureNotDisposed();
        var result = new Ulist<T>(this[start..(start + length)]);
        if (disposedOrigin) Dispose();
        return result;
    }

    /// <summary>
    /// Implicitly converts a <see cref="Uarray{T}"/> to a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <param name="array">The unmanaged array to convert.</param>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> representation of the array.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<T>(Uarray<T> array)
    {
        array.EnsureNotDisposed();
        return array.AsSpan();
    }

    /// <summary>
    /// Implicitly converts a <see cref="Uarray{T}"/> to a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="array">The unmanaged array to convert.</param>
    /// <returns>A <see cref="Span{T}"/> representation of the array.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Span<T>(Uarray<T> array)
    {
        array.EnsureNotDisposed();
        return array.AsSpan();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator()
    {
        EnsureNotDisposed();
        return new Enumerator(m_Items);
    }

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        EnsureNotDisposed();
        for (var i = 0; i < m_Length; i++)
        {
            yield return m_Items[i];
        }
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        EnsureNotDisposed();
        for (var i = 0; i < m_Length; i++)
        {
            yield return m_Items[i];
        }
    }

    /// <summary>
    /// Determines whether this array and the specified span contain the same sequence of elements.
    /// </summary>
    /// <param name="other">The span to compare with this array.</param>
    /// <returns><see langword="true"/> if the arrays contain the same sequence of elements; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public bool SequenceEqual(ReadOnlySpan<T> other)
    {
        EnsureNotDisposed();
        if (other.Length != m_Length) return false;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Length; i++)
        {
            if (!comparer.Equals(m_Items[i], other[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array.
    /// </summary>
    /// <param name="destination">The destination unmanaged array to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(ref Uarray<T> destination)
    {
        var dst = destination.AsSpan();
        var src = AsSpan();
        src.CopyTo(dst);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination span.
    /// </summary>
    /// <param name="destination">The destination span to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<T> destination)
    {
        var src = AsSpan();
        src.CopyTo(destination);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array.
    /// </summary>
    /// <param name="array">The destination array to copy elements to.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array)
    {
        var src = AsSpan();
        src.CopyTo(array);
    }

    /// <summary>
    /// Copies all elements from this array to the specified destination array starting at the specified index.
    /// </summary>
    /// <param name="array">The destination array to copy elements to.</param>
    /// <param name="index">The zero-based index in the destination array at which copying begins.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array, int index)
    {
        var src = AsSpan();
        src.CopyTo(array.AsSpan(index));
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    void ICollection.CopyTo(Array array, int index)
    {
        EnsureNotDisposed();
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (index < 0 || index >= array.Length)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (array.Length - index < m_Length)
            throw new ArgumentException("Destination array is not long enough.");

        for (var i = 0; i < m_Length; i++)
        {
            array.SetValue(m_Items[i], index + i);
        }
    }

    /// <inheritdoc />
    int IList.Add(object? value) => throw new NotSupportedException("Array is fixed size.");

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IList.Clear() => throw new NotSupportedException("Array is fixed size.");

    /// <inheritdoc />
    bool IList.Contains(object? value)
    {
        EnsureNotDisposed();
        var array = m_Items;
        if (value is not T v) return false;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Length; i++)
        {
            if (comparer.Equals(array[i], v))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether the array contains the specified element.
    /// </summary>
    /// <param name="item">The element to locate in the array.</param>
    /// <returns><see langword="true"/> if the array contains the specified element; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item) => IndexOf(item) >= 0;

    /// <summary>
    /// Searches for the specified element and returns the zero-based index of the first occurrence within the entire array.
    /// </summary>
    /// <param name="item">The element to locate in the array. The value can be null for reference types.</param>
    /// <returns>
    /// The zero-based index of the first occurrence of <paramref name="item"/> within the entire array, if found; 
    /// otherwise, -1.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public int IndexOf(T item)
    {
        EnsureNotDisposed();
        var array = m_Items;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Length; i++)
        {
            if (comparer.Equals(array[i], item))
            {
                return i;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    int IList.IndexOf(object? value)
    {
        var array = m_Items;
        if (value is not T v) return -1;
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < m_Length; i++)
        {
            if (comparer.Equals(array[i], v))
            {
                return i;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    void IList.Insert(int index, object? value)
    {
        throw new NotSupportedException("Array is fixed size.");
    }

    /// <inheritdoc />
    void IList.Remove(object? value)
    {
        throw new NotSupportedException("Array is fixed size.");
    }

    /// <inheritdoc />
    void IList.RemoveAt(int index)
    {
        throw new NotSupportedException("Array is fixed size.");
    }

    /// <inheritdoc />
    int IStructuralComparable.CompareTo(object? other, IComparer comparer)
    {
        EnsureNotDisposed();
        if (other == null) return 1;
        if (other is not Uarray<T> otherArray)
            throw new ArgumentException("Object is not a Uarray<T>.");

        var minLength = Math.Min(m_Length, otherArray.m_Length);
        for (var i = 0; i < minLength; i++)
        {
            var comparison = comparer.Compare(m_Items[i], otherArray.m_Items[i]);
            if (comparison != 0) return comparison;
        }

        return m_Length.CompareTo(otherArray.m_Length);
    }

    /// <inheritdoc />
    bool IStructuralEquatable.Equals(object? other, IEqualityComparer comparer)
    {
        if (IsEmpty || !IsValid) return false;
        if (other is not Uarray<T> otherArray) return false;
        if (m_Length != otherArray.m_Length) return false;

        for (var i = 0; i < m_Length; i++)
        {
            if (!comparer.Equals(m_Items[i], otherArray.m_Items[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => (int) m_Address;

    /// <inheritdoc />
    public int GetHashCode(IEqualityComparer comparer)
    {
        var hash = 17;
        for (var i = 0; i < m_Length; i++)
        {
            hash = hash * 31 + comparer.GetHashCode(m_Items[i]);
        }

        return hash;
    }

    /// <inheritdoc />
    object ICloneable.Clone()
    {
        EnsureNotDisposed();
        return new Uarray<T>(m_Items);
    }

    /// <summary>
    /// Creates a copy of this array.
    /// </summary>
    /// <returns>A new <see cref="Uarray{T}"/> that is a copy of this instance.</returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    public Uarray<T> Clone()
    {
        EnsureNotDisposed();
        return new Uarray<T>(m_Items);
    }

    /// <summary>
    /// Disposes only the unmanaged memory allocated for this array instance without disposing individual elements.
    /// </summary>
    /// <remarks>
    /// This method releases the unmanaged memory block and marks the array as disposed, but does not call dispose
    /// on individual elements. Use this method when elements do not require disposal or when elements have already
    /// been disposed separately.
    /// </remarks>
    public void DisposeOnlySelf()
    {
        if (m_IsDisposed) return;

        m_IsDisposed = true;
        Native.Free(m_Address);
        m_Address = 0;
        m_Length  = 0;
    }

    /// <summary>
    /// Disposes all elements in the array if they implement IDisposable.
    /// </summary>
    /// <remarks>
    /// This method calls the dispose method on each element in the array if the element type implements IDisposable.
    /// The unmanaged memory for the array itself is not released by this method.
    /// </remarks>
    public void DisposeElements() => DisposeElements(0, m_Length);

    /// <summary>
    /// Disposes elements in the array starting from the specified index to the end of the array if they implement IDisposable.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index from which to begin disposing elements.</param>
    /// <remarks>
    /// This method calls the dispose method on each element from the specified starting index to the end of the array
    /// if the element type implements IDisposable. The unmanaged memory for the array itself is not released by this method.
    /// </remarks>
    public void DisposeElements(int startIndex) => DisposeElements(startIndex, m_Length - startIndex);

    /// <summary>
    /// Disposes a specified range of elements in the array if they implement IDisposable.
    /// </summary>
    /// <param name="startIndex">The zero-based starting index from which to begin disposing elements.</param>
    /// <param name="length">The number of elements to dispose starting from the specified index.</param>
    /// <remarks>
    /// This method calls the dispose method on each element within the specified range if the element type implements IDisposable.
    /// The unmanaged memory for the array itself is not released by this method. If the array has already been disposed,
    /// this method returns without performing any operations.
    /// </remarks>
    public void DisposeElements(int startIndex, int length)
    {
        if (m_IsDisposed) return;
        if (Disposer<T>.IsDisposable)
        {
            var end = startIndex + length;
            for (var i = startIndex; i < end; i++)
            {
                Disposer<T>.Dispose(ref m_Items[i]);
            }
        }
    }

    /// <summary>
    /// Releases all resources used by the unmanaged array, including disposing individual elements and freeing unmanaged memory.
    /// </summary>
    /// <remarks>
    /// This method first disposes all elements in the array if they implement IDisposable, then releases the unmanaged memory
    /// allocated for the array. After calling this method, the array instance becomes invalid and should not be used.
    /// This method implements the IDisposable pattern.
    /// </remarks>
    public void Dispose()
    {
        DisposeElements(0, m_Length);
        DisposeOnlySelf();
    }

    /// <summary>
    /// Verifies that the array has not been disposed and is still valid for operations.
    /// This method is called by public methods to ensure the array is in a usable state
    /// before performing any operations on its internal data structures.
    /// </summary>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when the array has been disposed and is no longer valid for use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotDisposed()
    {
        if (m_IsDisposed) throw new ObjectDisposedException(nameof(Ulist<T>));
    }

    /// <summary>
    /// Provides a fast enumerator for the unmanaged array using ref struct.
    /// </summary>
    public ref struct Enumerator : IEnumerator<T>
    {
        /// <summary>
        /// The span of elements being enumerated.
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
        public void Dispose() => Reset();
    }
}