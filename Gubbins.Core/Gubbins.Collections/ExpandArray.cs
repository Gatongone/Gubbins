using System.Collections;
using System.Runtime.CompilerServices;

namespace Gubbins.Collections;

/// <summary>
/// Implements a variable-size Expand-Array that uses an array of objects to store the
/// elements. The expand array provide ref element accessor.
/// </summary>
/// <typeparam name="T">Element type</typeparam>
/// Fork from <a href="https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/List.cs"/>.
public class ExpandArray<T> : IList, IList<T>, IReadOnlyList<T>
{
    public ref T this[int index] => ref Items[index]!;

    T IList<T>.this[int index]
    {
        get => Items[index]!;
        set => Items[index] = value;
    }

    T IReadOnlyList<T>.this[int index] => Items[index]!;

    internal T[] Items;
    internal int Size;
    internal int Version;
    private static readonly T[] s_EmptyArray = [];

    /// <summary>Initializes a new instance of the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> class that is empty and has the default initial capacity.</summary>
    public ExpandArray() => Items = s_EmptyArray;

    /// <summary>Initializes a new instance of the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> class that is empty and has the specified initial capacity.</summary>
    /// <param name="capacity">The number of elements that the new expand array can initially store.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="capacity" /> is less than 0.</exception>
    public ExpandArray(int capacity) => Items = capacity == 0 ? s_EmptyArray : new T[capacity];

    /// <summary>Initializes a new instance of the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> class that contains elements copied from the specified collection and has sufficient capacity to accommodate the number of elements copied.</summary>
    /// <param name="collection">The collection whose elements are copied to the new list.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.</exception>
    public ExpandArray(IEnumerable<T> collection)
    {
        switch (collection)
        {
            case null:
            {
                Items = s_EmptyArray;
                break;
            }
            case ICollection<T> objs:
            {
                var count = objs.Count;
                if (count == 0)
                {
                    Items = s_EmptyArray;
                }
                else
                {
                    Items = new T[count];
                    objs.CopyTo(Items, 0);
                    Size = count;
                }

                break;
            }
            default:
            {
                Items = s_EmptyArray;
                foreach (var obj in collection)
                {
                    Add(obj);
                }

                break;
            }
        }
    }

    /// <summary>Gets or sets the total number of elements the internal data structure can hold without resizing.</summary>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <see cref="P:Gubbins.Collections.ExpandArray{T}.Capacity" /> is set to a value that is less than <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</exception>
    /// <exception cref="T:System.OutOfMemoryException">There is not enough memory available on the system.</exception>
    /// <returns>The number of elements that the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> can contain before resizing is required.</returns>
    public int Capacity
    {
        get => Items.Length;
        set
        {
            if (value < Size)
                throw new ArgumentOutOfRangeException(nameof(Size));
            if (value == Items.Length)
                return;
            if (value > 0)
            {
                var destinationArray = new T[value];
                if (Size > 0)
                {
                    Array.Copy(Items, destinationArray, Size);
                }

                Items = destinationArray;
            }
            else
                Items = s_EmptyArray;
        }
    }

    /// <summary>Gets the number of elements contained in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <returns>The number of elements contained in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public int Count => Size;

    /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, <see langword="false" />.  In the default implementation of <see cref="T:Gubbins.Collections.ExpandArray{T}" />, this property always returns <see langword="false" />.</returns>
    bool IList.IsFixedSize => false;

    /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only; otherwise, <see langword="false" />.  In the default implementation of <see cref="T:Gubbins.Collections.ExpandArray{T}" />, this property always returns <see langword="false" />.</returns>
    bool ICollection<
        T>.IsReadOnly => false;

    /// <summary>Gets a value indicating whether the <see cref="T:System.Collections.IList" /> is read-only.</summary>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:System.Collections.IList" /> is read-only; otherwise, <see langword="false" />.  In the default implementation of <see cref="T:Gubbins.Collections.ExpandArray{T}" />, this property always returns <see langword="false" />.</returns>
    bool IList.IsReadOnly => false;

    /// <summary>Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe).</summary>
    /// <returns>
    /// <see langword="true" /> if access to the <see cref="T:System.Collections.ICollection" /> is synchronized (thread safe); otherwise, <see langword="false" />.  In the default implementation of <see cref="T:Gubbins.Collections.ExpandArray{T}" />, this property always returns <see langword="false" />.</returns>
    bool ICollection.IsSynchronized => false;

    /// <summary>Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.</summary>
    /// <returns>An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection" />.  In the default implementation of <see cref="T:Gubbins.Collections.ExpandArray{T}" />, this property always returns the current instance.</returns>
    object ICollection.SyncRoot => this;

    private static bool IsCompatibleObject(object? value)
    {
        if (value is T) return true;
        return value == null && default(T) == null;
    }

    /// <summary>Gets or sets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />.</exception>
    /// <exception cref="T:System.ArgumentException">The property is set and <paramref name="value" /> is of a type that is not assignable to the <see cref="T:System.Collections.IList" />.</exception>
    /// <returns>The element at the specified index.</returns>
    object? IList.this[int index]
    {
        get => this[index];
        set
        {
            if (default(T) == null && value == null) throw new ArgumentNullException(nameof(value));
            try
            {
                this[index] = (T) value!;
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException($"The value '{value!.GetType()}' is not of type '{typeof(T)}' and cannot be used in this generic collection.");
            }
        }
    }

    /// <summary>Adds an object to the end of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="item">The object to be added to the end of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        ++Version;
        T[] items = Items;
        var size = Size;
        if ((uint) size < (uint) items.Length)
        {
            Size        = size + 1;
            items[size] = item;
        }
        else
            AddWithResize(item);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AddWithResize(T item)
    {
        var size = Size;
        Grow(size + 1);
        Size        = size + 1;
        Items[size] = item;
    }

    /// <summary>Adds an item to the <see cref="T:System.Collections.IList" />.</summary>
    /// <param name="item">The <see cref="T:System.Object" /> to add to the <see cref="T:System.Collections.IList" />.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="item" /> is of a type that is not assignable to the <see cref="T:System.Collections.IList" />.</exception>
    /// <returns>The position into which the new element was inserted.</returns>
    int IList.Add(object? item)
    {
        if (default(T) == null && item == null) throw new ArgumentNullException(nameof(item));
        try
        {
            Add((T) item!);
        }
        catch (InvalidCastException)
        {
            throw new ArgumentException($"The value '{item!.GetType()}' is not of type '{typeof(T)}' and cannot be used in this generic collection.");
        }

        return Count - 1;
    }

    /// <summary>Adds the elements of the specified collection to the end of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="collection">The collection whose elements should be added to the end of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The collection itself cannot be <see langword="null" />, but it can contain elements that are <see langword="null" />, if type is a reference type.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.</exception>
    public void AddRange(IEnumerable<T> collection)
    {
        switch (collection)
        {
            case null:
                throw new ArgumentNullException(nameof(collection));
            case ICollection<T> objs:
            {
                var count = objs.Count;
                if (count <= 0)
                    return;
                if (Items.Length - Size < count)
                    Grow(checked(Size + count));
                objs.CopyTo(Items, Size);
                Size += count;
                ++Version;
                break;
            }
            default:
            {
                foreach (var obj in collection)
                    Add(obj);
                break;
            }
        }
    }

    /// <summary>Returns a read-only <see cref="T:System.Collections.ObjectModel.ReadOnlyCollection`1" /> wrapper for the current collection.</summary>
    /// <returns>An object that acts as a read-only wrapper around the current <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public System.Collections.ObjectModel.ReadOnlyCollection<T> AsReadOnly() => new(this);

    /// <summary>Searches a range of elements in the sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" /> for an element using the specified comparer and returns the zero-based index of the element.</summary>
    /// <param name="index">The zero-based starting index of the range to search.</param>
    /// <param name="count">The length of the range to search.</param>
    /// <param name="item">The object to locate. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing elements, or <see langword="null" /> to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not denote a valid range in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is <see langword="null" />, and the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    /// <returns>The zero-based index of <paramref name="item" /> in the sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if <paramref name="item" /> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item" /> or, if there is no larger element, the bitwise complement of <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</returns>
    public int BinarySearch(int index, int count, T item, IComparer<T>? comparer)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        return Array.BinarySearch(Items, index, count, item, comparer);
    }

    /// <summary>Searches the entire sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" /> for an element using the default comparer and returns the zero-based index of the element.</summary>
    /// <param name="item">The object to locate. The value can be <see langword="null" /> for reference types.</param>
    /// <exception cref="T:System.InvalidOperationException">The default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    /// <returns>The zero-based index of <paramref name="item" /> in the sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if <paramref name="item" /> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item" /> or, if there is no larger element, the bitwise complement of <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</returns>
    public int BinarySearch(T item) => BinarySearch(0, Count, item, null);

    /// <summary>Searches the entire sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" /> for an element using the specified comparer and returns the zero-based index of the element.</summary>
    /// <param name="item">The object to locate. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing elements
    /// or
    /// <see langword="null" /> to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.</param>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is <see langword="null" />, and the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    /// <returns>The zero-based index of <paramref name="item" /> in the sorted <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if <paramref name="item" /> is found; otherwise, a negative number that is the bitwise complement of the index of the next element that is larger than <paramref name="item" /> or, if there is no larger element, the bitwise complement of <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</returns>
    public int BinarySearch(T item, IComparer<T>? comparer) => BinarySearch(0, Count, item, comparer);

    /// <summary>Removes all elements from the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        ++Version;
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            var size = Size;
            Size = 0;
            if (size <= 0)
                return;
            Array.Clear(Items, 0, size);
        }
        else
            Size = 0;
    }

    /// <summary>Determines whether an element is in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="item" /> is found in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />; otherwise, <see langword="false" />.</returns>
    public bool Contains(T item) => Size != 0 && IndexOf(item) >= 0;

    /// <summary>Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.</summary>
    /// <param name="item">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="item" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, <see langword="false" />.</returns>
    bool IList.Contains(object? item) => IsCompatibleObject(item) && Contains((T) item!);

    /// <summary>Converts the elements in the current <see cref="T:Gubbins.Collections.ExpandArray{T}" /> to another type, and returns a expand array containing the converted elements.</summary>
    /// <param name="converter">A <see cref="T:System.Converter`2" /> delegate that converts each element from one type to another type.</param>
    /// <typeparam name="TOutput">The type of the elements of the target array.</typeparam>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="converter" /> is <see langword="null" />.</exception>
    /// <returns>A <see cref="T:Gubbins.Collections.ExpandArray{T}" /> of the target type containing the converted elements from the current <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public ExpandArray<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
    {
        if (converter == null)
            throw new ArgumentNullException(nameof(converter));
        var outputList = new ExpandArray<TOutput>(Size);
        for (var index = 0; index < Size; ++index)
            outputList.Items[index] = converter(Items[index]);
        outputList.Size = Size;
        return outputList;
    }

    /// <summary>Copies the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" /> to a compatible one-dimensional array, starting at the beginning of the target array.</summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" /> is greater than the number of elements that the destination <paramref name="array" /> can contain.</exception>
    public void CopyTo(T[] array) => CopyTo(array, 0);

    /// <summary>Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.</summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="arrayIndex" /> is less than 0.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="array" /> is multidimensional
    /// or
    /// <paramref name="array" /> does not have zero-based indexing
    /// or
    /// The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />
    /// or
    /// The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />.
    /// </exception>
    void ICollection.CopyTo(Array array, int arrayIndex)
    {
        if (array != null && array.Rank != 1)
            throw new ArgumentException("Only single dimensional arrays are supported for the requested action.");
        try
        {
            Array.Copy(Items, 0, array!, arrayIndex, Size);
        }
        catch (ArrayTypeMismatchException)
        {
            throw new ArgumentException("Target array type is not compatible with the type of items in the collection.");
        }
    }

    /// <summary>Copies a range of elements from the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> to a compatible one-dimensional array, starting at the specified index of the target array.</summary>
    /// <param name="index">The zero-based index in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" /> at which copying begins.</param>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <param name="count">The number of elements to copy.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.
    /// <para/>
    /// or
    /// <para/>
    /// <paramref name="arrayIndex" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> is equal to or greater than the <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" /> of the source <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// The number of elements from <paramref name="index" /> to the end of the source <see cref="T:Gubbins.Collections.ExpandArray{T}" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        Array.Copy(Items, index, array, arrayIndex, count);
    }

    /// <summary>Copies the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" /> to a compatible one-dimensional array, starting at the specified index of the target array.</summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="array" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="arrayIndex" /> is less than 0.</exception>
    /// <exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" /> is greater than the available space from <paramref name="arrayIndex" /> to the end of the destination <paramref name="array" />.</exception>
    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(Items, 0, array, arrayIndex, Size);
    }

    /// <summary>Ensures that the capacity of this expand array is at least the specified <paramref name="capacity" />. If the current capacity is less than <paramref name="capacity" />, it is successively increased to twice the current capacity until it is at least the specified <paramref name="capacity" />.</summary>
    /// <param name="capacity">The minimum capacity to ensure.</param>
    /// <returns>The new capacity of this list.</returns>
    public int EnsureCapacity(int capacity)
    {
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Non-negative number required.");
        if (Items.Length < capacity)
            Grow(capacity);
        return Items.Length;
    }

    internal void Grow(int capacity) => Capacity = GetNewCapacity(capacity);

    private void GrowForInsertion(int indexToInsert, int insertionCount = 1)
    {
        var destinationArray = new T[GetNewCapacity(checked(Size + insertionCount))];
        if (indexToInsert != 0)
            Array.Copy(Items, destinationArray, indexToInsert);
        if (Size != indexToInsert)
            Array.Copy(Items, indexToInsert, destinationArray, indexToInsert + insertionCount, Size - indexToInsert);
        Items = destinationArray;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewCapacity(int capacity)
    {
        var newCapacity = Items.Length == 0 ? 4 : 2 * Items.Length;
        if ((uint) newCapacity > 2147483591U)
            newCapacity = 2147483591;
        if (newCapacity < capacity)
            newCapacity = capacity;
        return newCapacity;
    }

    /// <summary>Determines whether the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> contains elements that match the conditions defined by the specified predicate.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the elements to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>
    /// <see langword="true" /> if the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> contains one or more elements that match the conditions defined by the specified predicate; otherwise, <see langword="false" />.</returns>
    public bool Exists(Predicate<T> match) => FindIndex(match) != -1;

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>The first element that matches the conditions defined by the specified predicate, if found; otherwise, the default value for type.</returns>
    public T? Find(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        for (var index = 0; index < Size; ++index)
        {
            if (match(Items[index]))
                return Items[index];
        }

        return default;
    }

    /// <summary>Retrieves all the elements that match the conditions defined by the specified predicate.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the elements to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>A <see cref="T:Gubbins.Collections.ExpandArray{T}" /> containing all the elements that match the conditions defined by the specified predicate, if found; otherwise, an empty <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public ExpandArray<T> FindAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        var all = new ExpandArray<T>();
        for (var index = 0; index < Size; ++index)
        {
            if (match(Items[index]))
                all.Add(Items[index]);
        }

        return all;
    }

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindIndex(Predicate<T> match) => FindIndex(0, Size, match);

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from the specified index to the last element.</summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindIndex(int startIndex, Predicate<T> match) => FindIndex(startIndex, Size - startIndex, match);

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that starts at the specified index and contains the specified number of elements.</summary>
    /// <param name="startIndex">The zero-based starting index of the search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// <paramref name="count" /> is less than 0
    /// or
    /// <paramref name="startIndex" /> and <paramref name="count" /> do not specify a valid section in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.
    /// </exception>
    /// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
        if ((uint) startIndex > (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Index was out of range. Must be non-negative and less than or equal to the size of the collection.");
        if (count < 0 || startIndex > Size - count)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive and count must refer to a location within the string/array/collection.");
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        var num = startIndex + count;
        for (var index = startIndex; index < num; ++index)
        {
            if (match(Items[index]))
                return index;
        }

        return -1;
    }

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the last occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>The last element that matches the conditions defined by the specified predicate, if found; otherwise, the default value for type.</returns>
    public T? FindLast(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        for (var index = Size - 1; index >= 0; --index)
        {
            if (match(Items[index]))
                return Items[index];
        }

        return default;
    }

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindLastIndex(Predicate<T> match) => FindLastIndex(Size - 1, Size, match);

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from the first element to the specified index.</summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindLastIndex(int startIndex, Predicate<T> match) => FindLastIndex(startIndex, startIndex + 1, match);

    /// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that contains the specified number of elements and ends at the specified index.</summary>
    /// <param name="startIndex">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the element to search for.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="startIndex" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// <paramref name="count" /> is less than 0
    /// or
    /// <paramref name="startIndex" /> and <paramref name="count" /> do not specify a valid section in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.
    /// </exception>
    /// <returns>The zero-based index of the last occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        if (Size == 0)
        {
            if (startIndex != -1)
                throw new ArgumentOutOfRangeException(nameof(startIndex), "Index was out of range. Must be non-negative and less than the size of the collection.");
        }
        else if ((uint) startIndex >= (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "Index was out of range. Must be non-negative and less than the size of the collection.");

        if (count < 0 || startIndex - count + 1 < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive and count must refer to a location within the string/array/collection.");
        var num = startIndex - count;
        for (var lastIndex = startIndex; lastIndex > num; --lastIndex)
        {
            if (match(Items[lastIndex]))
                return lastIndex;
        }

        return -1;
    }

    /// <summary>Performs the specified action on each element of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="action">The <see cref="T:System.Action`1" /> delegate to perform on each element of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="action" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.InvalidOperationException">An element in the collection has been modified.</exception>
    public void ForEach(Action<T> action)
    {
        if (action == null)
            throw new ArgumentNullException(nameof(action));
        var version = Version;
        for (var index = 0; index < Size && version == Version; ++index)
            action(Items[index]);
        if (version == Version)
            return;
        throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
    }

    /// <summary>Returns an enumerator that iterates through the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <returns>A <see cref="T:Gubbins.Collections.ExpandArray{T}.Enumerator" /> for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public Enumerator GetEnumerator() => new(this);

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <summary>Returns an enumerator that iterates through a collection.</summary>
    /// <returns>An <see cref="T:System.Collections.IEnumerator" /> that can be used to iterate through the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>) this).GetEnumerator();

    /// <summary>Creates a shallow copy of a range of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="index">The zero-based <see cref="T:Gubbins.Collections.ExpandArray{T}" /> index at which the range starts.</param>
    /// <param name="count">The number of elements in the range.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not denote a valid range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>A shallow copy of a range of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public ExpandArray<T> GetRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        var range = new ExpandArray<T>(count);
        Array.Copy(Items, index, range.Items, 0, count);
        range.Size = count;
        return range;
    }

    /// <summary>Creates a shallow copy of a range of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="start">The zero-based <see cref="T:Gubbins.Collections.ExpandArray{T}" /> index at which the range starts.</param>
    /// <param name="length">The length of the range.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="start" /> is less than 0
    /// or
    /// <paramref name="length" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="start" /> and <paramref name="length" /> do not denote a valid range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>A shallow copy of a range of elements in the source <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public ExpandArray<T> Slice(int start, int length) => GetRange(start, length);

    /// <summary>Searches for the specified object and returns the zero-based index of the first occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item" /> within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if found; otherwise, -1.</returns>
    public int IndexOf(T item) => Array.IndexOf(Items, item, 0, Size);

    /// <summary>Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.</summary>
    /// <param name="item">The object to locate in the <see cref="T:System.Collections.IList" />.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="item" /> is of a type that is not assignable to the <see cref="T:System.Collections.IList" />.</exception>
    /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
    int IList.IndexOf(object item)
    {
        return IsCompatibleObject(item) ? IndexOf((T) item) : -1;
    }

    /// <summary>Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from the specified index to the last element.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item" /> within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from <paramref name="index" /> to the last element, if found; otherwise, -1.</returns>
    public int IndexOf(T item, int index)
    {
        if (index > Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than or equal to the size of the collection.");
        return Array.IndexOf(Items, item, index, Size - index);
    }

    /// <summary>Searches for the specified object and returns the zero-based index of the first occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that starts at the specified index and contains the specified number of elements.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="index">The zero-based starting index of the search. 0 (zero) is valid in an empty list.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// <paramref name="count" /> is less than 0
    /// or
    /// <paramref name="index" /> and <paramref name="count" /> do not specify a valid section in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.
    /// </exception>
    /// <returns>The zero-based index of the first occurrence of <paramref name="item" /> within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that starts at <paramref name="index" /> and contains <paramref name="count" /> number of elements, if found; otherwise, -1.</returns>
    public int IndexOf(T item, int index, int count)
    {
        if (index > Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than or equal to the size of the collection.");
        if (count < 0 || index > Size - count)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive and count must refer to a location within the string/array/collection.");
        return Array.IndexOf(Items, item, index, count);
    }

    /// <summary>Inserts an element into the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert. The value can be <see langword="null" /> for reference types.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.
    /// <para/>
    /// or
    /// <para/>
    /// <paramref name="index" /> is greater than <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</exception>
    public void Insert(int index, T item)
    {
        if ((uint) index > (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be within the bounds of the List.");
        if (Size == Items.Length)
            GrowForInsertion(index);
        else if (index < Size)
            Array.Copy(Items, index, Items, index + 1, Size - index);
        Items[index] = item;
        ++Size;
        ++Version;
    }

    /// <summary>Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="T:System.Collections.IList" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />.</exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="item" /> is of a type that is not assignable to the <see cref="T:System.Collections.IList" />.</exception>
    void IList.Insert(int index, object item)
    {
        if (default(T) == null && item == null) throw new ArgumentNullException(nameof(item));
        try
        {
            Insert(index, (T) item);
        }
        catch (InvalidCastException)
        {
            throw new ArgumentException($"The value '{item.GetType()}' is not of type '{typeof(T)}' and cannot be used in this generic collection.");
        }
    }

    /// <summary>Inserts the elements of a collection into the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> at the specified index.</summary>
    /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
    /// <param name="collection">The collection whose elements should be inserted into the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The collection itself cannot be <see langword="null" />, but it can contain elements that are <see langword="null" />, if type is a reference type.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="collection" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.
    /// <para/>
    /// or
    /// <para/>
    /// <paramref name="index" /> is greater than <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</exception>
    public void InsertRange(int index, IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));
        if ((uint) index > (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than or equal to the size of the collection.");
        if (collection is ICollection<T> objs)
        {
            var count = objs.Count;
            if (count <= 0)
                return;
            if (Items.Length - Size < count)
                GrowForInsertion(index, count);
            else if (index < Size)
                Array.Copy(Items, index, Items, index + count, Size - index);
            if (Equals(this, objs))
            {
                Array.Copy(Items, 0, Items, index, index);
                Array.Copy(Items, index + count, Items, index * 2, Size - index);
            }
            else
                objs.CopyTo(Items, index);

            Size += count;
            ++Version;
        }
        else
        {
            foreach (var obj in collection)
                Insert(index++, obj);
        }
    }

    /// <summary>Searches for the specified object and returns the zero-based index of the last occurrence within the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <returns>The zero-based index of the last occurrence of <paramref name="item" /> within the entire the <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item)
    {
        return Size == 0 ? -1 : LastIndexOf(item, Size - 1, Size);
    }

    /// <summary>Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from the first element to the specified index.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    /// <returns>The zero-based index of the last occurrence of <paramref name="item" /> within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that extends from the first element to <paramref name="index" />, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item, int index)
    {
        if (index >= Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
        return LastIndexOf(item, index, index + 1);
    }

    /// <summary>Searches for the specified object and returns the zero-based index of the last occurrence within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that contains the specified number of elements and ends at the specified index.</summary>
    /// <param name="item">The object to locate in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <param name="index">The zero-based starting index of the backward search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is outside the range of valid indexes for the <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// <paramref name="count" /> is less than 0
    /// or
    /// <paramref name="index" /> and <paramref name="count" /> do not specify a valid section in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.
    /// </exception>
    /// <returns>The zero-based index of the last occurrence of <paramref name="item" /> within the range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> that contains <paramref name="count" /> number of elements and ends at <paramref name="index" />, if found; otherwise, -1.</returns>
    public int LastIndexOf(T item, int index, int count)
    {
        if (Count != 0 && index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (Count != 0 && count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size == 0)
            return -1;
        if (index >= Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Must be less than or equal to the size of the collection.");
        if (count > index + 1)
            throw new ArgumentOutOfRangeException(nameof(count), "Must be less than or equal to the size of the collection.");
        return Array.LastIndexOf(Items, item, index, count);
    }

    /// <summary>Removes the first occurrence of a specific object from the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="item">The object to remove from the <see cref="T:Gubbins.Collections.ExpandArray{T}" />. The value can be <see langword="null" /> for reference types.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="item" /> is successfully removed; otherwise, <see langword="false" />.  This method also returns <see langword="false" /> if <paramref name="item" /> was not found in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }

#nullable disable
    /// <summary>Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.</summary>
    /// <param name="item">The object to remove from the <see cref="T:System.Collections.IList" />.</param>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="item" /> is of a type that is not assignable to the <see cref="T:System.Collections.IList" />.</exception>
    void IList.Remove(object item)
    {
        if (!IsCompatibleObject(item))
            return;
        Remove((T) item);
    }

#nullable enable
    /// <summary>Removes all the elements that match the conditions defined by the specified predicate.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions of the elements to remove.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>The number of elements removed from the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public int RemoveAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        var index1 = 0;
        while (index1 < Size &&
               !match(Items[index1]))
            ++index1;
        if (index1 >= Size)
            return 0;
        var index2 = index1 + 1;
        while (index2 < Size)
        {
            while (index2 < Size &&
                   match(Items[index2]))
                ++index2;
            if (index2 < Size)
                Items[index1++] = Items[index2++];
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            Array.Clear(Items, index1, Size - index1);
        var num = Size - index1;
        Size = index1;
        ++Version;
        return num;
    }

    /// <summary>Removes the element at the specified index of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0.
    /// <para/>
    /// or
    /// <para/>
    /// <paramref name="index" /> is equal to or greater than <see cref="P:Gubbins.Collections.ExpandArray{T}.Count" />.</exception>
    public void RemoveAt(int index)
    {
        if ((uint) index >= (uint) Size)
            throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
        --Size;
        if (index < Size)
            Array.Copy(Items, index + 1, Items, index, Size - index);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            Items[Size] = default!;
        ++Version;
    }

    /// <summary>Removes a range of elements from the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    /// <param name="index">The zero-based starting index of the range of elements to remove.</param>
    /// <param name="count">The number of elements to remove.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not denote a valid range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    public void RemoveRange(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        if (count <= 0)
            return;
        Size -= count;
        if (index < Size)
            Array.Copy(Items, index + count, Items, index, Size - index);
        ++Version;
        if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            return;
        Array.Clear(Items, Size, count);
    }

    /// <summary>Reverses the order of the elements in the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    public void Reverse() => Reverse(0, Count);

    /// <summary>Reverses the order of the elements in the specified range.</summary>
    /// <param name="index">The zero-based starting index of the range to reverse.</param>
    /// <param name="count">The number of elements in the range to reverse.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not denote a valid range of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</exception>
    public void Reverse(int index, int count)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        if (count > 1)
            Array.Reverse(Items, index, count);
        ++Version;
    }

    /// <summary>Sorts the elements in the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" /> using the default comparer.</summary>
    /// <exception cref="T:System.InvalidOperationException">The default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find an implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    public void Sort() => Sort(0, Count, null);

    /// <summary>Sorts the elements in the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" /> using the specified comparer.</summary>
    /// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing elements, or <see langword="null" /> to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.</param>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is <see langword="null" />, and the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    /// <exception cref="T:System.ArgumentException">The implementation of <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not return 0 when comparing an item with itself.</exception>
    public void Sort(IComparer<T>? comparer) => Sort(0, Count, comparer);

    /// <summary>Sorts the elements in a range of elements in <see cref="T:Gubbins.Collections.ExpandArray{T}" /> using the specified comparer.</summary>
    /// <param name="index">The zero-based starting index of the range to sort.</param>
    /// <param name="count">The length of the range to sort.</param>
    /// <param name="comparer">The <see cref="T:System.Collections.Generic.IComparer`1" /> implementation to use when comparing elements, or <see langword="null" /> to use the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" />.</param>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="index" /> is less than 0
    /// or
    /// <paramref name="count" /> is less than 0.
    /// </exception>
    /// <exception cref="T:System.ArgumentException">
    /// <paramref name="index" /> and <paramref name="count" /> do not specify a valid range in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />
    /// or
    /// The implementation of <paramref name="comparer" /> caused an error during the sort. For example, <paramref name="comparer" /> might not return 0 when comparing an item with itself.
    /// </exception>
    /// <exception cref="T:System.InvalidOperationException">
    /// <paramref name="comparer" /> is <see langword="null" />, and the default comparer <see cref="P:System.Collections.Generic.Comparer`1.Default" /> cannot find implementation of the <see cref="T:System.IComparable`1" /> generic interface or the <see cref="T:System.IComparable" /> interface for type.</exception>
    public void Sort(int index, int count, IComparer<T>? comparer)
    {
        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Non-negative number required.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
        if (Size - index < count)
            throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection.");
        if (count > 1)
            Array.Sort(Items, index, count, comparer);
        ++Version;
    }

    /// <summary>Sorts the elements in the entire <see cref="T:Gubbins.Collections.ExpandArray{T}" /> using the specified <see cref="T:System.Comparison`1" />.</summary>
    /// <param name="comparison">The <see cref="T:System.Comparison`1" /> to use when comparing elements.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="comparison" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentException">The implementation of <paramref name="comparison" /> caused an error during the sort. For example, <paramref name="comparison" /> might not return 0 when comparing an item with itself.</exception>
    public void Sort(Comparison<T> comparison)
    {
        if (comparison == null)
            throw new ArgumentNullException(nameof(comparison));
        if (Size > 1)
        {
            ArraySort<T>.Sort(new Span<T>(Items, 0, Size), comparison);
        }

        ++Version;
    }

    /// <summary>Copies the elements of the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> to a new array.</summary>
    /// <returns>An array containing copies of the elements of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</returns>
    public T[] ToArray()
    {
        if (Size == 0)
            return s_EmptyArray;
        var destinationArray = new T[Size];
        Array.Copy(Items, destinationArray, Size);
        return destinationArray;
    }

    /// <summary>Sets the capacity to the actual number of elements in the <see cref="T:Gubbins.Collections.ExpandArray{T}" />, if that number is less than a threshold value.</summary>
    public void TrimExcess()
    {
        if (Size >= (int) (Items.Length * 0.9))
            return;
        Capacity = Size;
    }

    /// <summary>Determines whether every element in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> matches the conditions defined by the specified predicate.</summary>
    /// <param name="match">The <see cref="T:System.Predicate`1" /> delegate that defines the conditions to check against the elements.</param>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="match" /> is <see langword="null" />.</exception>
    /// <returns>
    /// <see langword="true" /> if every element in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> matches the conditions defined by the specified predicate; otherwise, <see langword="false" />. If the expand array has no elements, the return value is <see langword="true" />.</returns>
    public bool TrueForAll(Predicate<T> match)
    {
        if (match == null)
            throw new ArgumentNullException(nameof(match));
        for (var index = 0; index < Size; ++index)
        {
            if (!match(Items[index]))
                return false;
        }

        return true;
    }

    /// <summary>Enumerates the elements of a <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ExpandArray<T> m_ExpandArray;
        private int m_Index;
        private readonly int m_Version;

        internal Enumerator(ExpandArray<T> expandArray)
        {
            m_ExpandArray = expandArray;
            m_Index       = 0;
            m_Version     = expandArray.Version;
        }

        /// <summary>Releases all resources used by the <see cref="T:Gubbins.Collections.ExpandArray{T}.Enumerator" />.</summary>
        public void Dispose() { }

        /// <summary>Advances the enumerator to the next element of the <see cref="T:Gubbins.Collections.ExpandArray{T}" />.</summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        /// <returns>
        /// <see langword="true" /> if the enumerator was successfully advanced to the next element; <see langword="false" /> if the enumerator has passed the end of the collection.</returns>
        public bool MoveNext()
        {
            var array = m_ExpandArray;
            if (m_Version != array.Version || (uint) m_Index >= (uint) array.Size)
                return MoveNextRare();
            ++m_Index;
            return true;
        }

        private bool MoveNextRare()
        {
            if (m_Version != m_ExpandArray.Version)
                throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
            m_Index   = m_ExpandArray.Size + 1;
            return false;
        }

        public ref T Current => ref m_ExpandArray.Items[m_Index - 1];

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        /// <returns>The element in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> at the current position of the enumerator.</returns>
        T IEnumerator<T>.Current => Current!;

        /// <summary>Gets the element at the current position of the enumerator.</summary>
        /// <exception cref="T:System.InvalidOperationException">The enumerator is positioned before the first element of the collection or after the last element.</exception>
        /// <returns>The element in the <see cref="T:Gubbins.Collections.ExpandArray{T}" /> at the current position of the enumerator.</returns>
        object? IEnumerator.Current
        {
            get
            {
                if (m_Index == 0 || m_Index == m_ExpandArray.Size + 1)
                    throw new InvalidOperationException("Enumeration has either not started or has already finished.");
                return Current;
            }
        }

        /// <summary>Sets the enumerator to its initial position, which is before the first element in the collection.</summary>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created.</exception>
        void IEnumerator.Reset()
        {
            if (m_Version != m_ExpandArray.Version)
                throw new InvalidOperationException("Collection was modified after the enumerator was instantiated.");
            m_Index   = 0;
        }
    }
}