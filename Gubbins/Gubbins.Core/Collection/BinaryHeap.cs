using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gubbins.Collection;

/// <summary>
/// Represents a binary heap data structure that implements a priority queue.
/// </summary>
/// <typeparam name="T">The type of elements in the heap.</typeparam>
public class BinaryHeap<T> : IPriorityQueue<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    private readonly List<T> m_List;

    private readonly IComparer<T> m_Comparer;

    /// <summary>
    /// Gets the root element of the heap (the element with the highest priority).
    /// </summary>
    public T Root => m_List[0];

    /// <summary>
    /// Gets the number of elements in the heap.
    /// </summary>
    public int Count => m_List.Count;

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with the default comparer.
    /// </summary>
    public BinaryHeap() : this(Comparer<T>.Default) { }

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with the specified comparer.
    /// </summary>
    /// <param name="comparer">The comparer to use for ordering elements in the heap.</param>
    public BinaryHeap(IComparer<T> comparer)
    {
        m_Comparer = comparer;
        m_List     = new List<T>();
    }

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with the specified elements and default comparer.
    /// </summary>
    /// <param name="elements">The elements to initialize the heap with.</param>
    public BinaryHeap(IEnumerable<T> elements) : this(elements, Comparer<T>.Default) { }

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with the specified elements and comparer.
    /// </summary>
    /// <param name="elements">The elements to initialize the heap with.</param>
    /// <param name="comparer">The comparer to use for ordering elements in the heap.</param>
    public BinaryHeap(IEnumerable<T> elements, IComparer<T> comparer)
    {
        m_Comparer = comparer;
        m_List     = new List<T>(elements);
        Heapify();
    }

    /// <summary>
    /// Returns the root element of the heap without removing it.
    /// </summary>
    /// <returns>The root element of the heap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peak() => m_List[0];

    /// <summary>
    /// Adds an item to the heap and maintains the heap property.
    /// </summary>
    /// <param name="item">The item to add to the heap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        m_List.Add(item);
        ShiftUp(m_List.Count - 1);
    }

    /// <summary>
    /// Removes and returns the root element from the heap.
    /// </summary>
    /// <returns>The root element that was removed from the heap.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Extract()
    {
        var count = m_List.Count;
        if (count <= 0) throw new InvalidOperationException("The heap is empty.");

        var result = m_List[0];
        m_List[0] = m_List[count - 1];
        m_List.RemoveAt(count - 1);
        ShiftDown(0);
        return result;
    }

    /// <summary>
    /// Removes all elements from the heap.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_List.Clear();

    /// <summary>
    /// Determines whether the heap contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate in the heap.</param>
    /// <returns>true if the item is found in the heap; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item) => m_List.Contains(item);

    /// <summary>
    /// Returns an enumerator that iterates through the heap.
    /// </summary>
    /// <returns>An enumerator for the heap.</returns>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Converts the internal list into a valid heap structure.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Heapify() => Heapify(m_List, m_Comparer);

    /// <summary>
    /// Moves an element up the heap to maintain the heap property.
    /// </summary>
    /// <param name="index">The index of the element to shift up.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ShiftUp(int index) => ShiftUp(m_List, index, m_Comparer);

    /// <summary>
    /// Moves an element down the heap to maintain the heap property.
    /// </summary>
    /// <param name="index">The index of the element to shift down.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ShiftDown(int index) => ShiftDown(m_List, index, m_Comparer);

    /// <summary>
    /// Converts the specified list into a valid heap structure using the default comparer.
    /// </summary>
    /// <param name="list">The list to convert into a heap.</param>
    public static void Heapify(IList<T> list) => Heapify(list, Comparer<T>.Default);

    /// <summary>
    /// Converts the specified list into a valid heap structure using the specified comparer.
    /// </summary>
    /// <param name="list">The list to convert into a heap.</param>
    /// <param name="comparer">The comparer to use for ordering elements.</param>
    public static void Heapify(IList<T> list, IComparer<T> comparer)
    {
        var index = (list.Count - 1) / 2;
        while (index >= 0)
        {
            ShiftDown(list, index--, comparer);
        }
    }

    /// <summary>
    /// Moves an element up the heap to maintain the heap property using the default comparer.
    /// </summary>
    /// <param name="list">The list representing the heap.</param>
    /// <param name="index">The index of the element to shift up.</param>
    public static void ShiftUp(IList<T> list, int index) => ShiftUp(list, index, Comparer<T>.Default);

    /// <summary>
    /// Moves an element up the heap to maintain the heap property using the specified comparer.
    /// </summary>
    /// <param name="list">The list representing the heap.</param>
    /// <param name="index">The index of the element to shift up.</param>
    /// <param name="comparer">The comparer to use for ordering elements.</param>
    public static void ShiftUp(IList<T> list, int index, IComparer<T> comparer)
    {
        while (index > 0)
        {
            var parentIndex = GetParentIndex(index);
            if (comparer.Compare(list[index], list[parentIndex]) > 0)
                (list[index], list[parentIndex]) = (list[parentIndex], list[index]);
            else break;

            index = parentIndex;
        }
    }

    /// <summary>
    /// Moves an element down the heap to maintain the heap property using the default comparer.
    /// </summary>
    /// <param name="list">The list representing the heap.</param>
    /// <param name="index">The index of the element to shift down.</param>
    public static void ShiftDown(IList<T> list, int index) => ShiftDown(list, index, Comparer<T>.Default);

    /// <summary>
    /// Moves an element down the heap to maintain the heap property using the specified comparer.
    /// </summary>
    /// <param name="list">The list representing the heap.</param>
    /// <param name="index">The index of the element to shift down.</param>
    /// <param name="comparer">The comparer to use for ordering elements.</param>
    public static void ShiftDown(IList<T> list, int index, IComparer<T> comparer)
    {
        var count = list.Count;
        while (index < count)
        {
            int resultIndex;
            var leftIndex = GetLeftChildIndex(index);
            var rightIndex = GetRightChildIndex(index);

            if (rightIndex >= count)
            {
                if (leftIndex >= count)
                    break;
                resultIndex = leftIndex;
            }
            else
            {
                resultIndex = comparer.Compare(list[leftIndex], list[rightIndex]) < 0 ? leftIndex : rightIndex;
            }

            if (comparer.Compare(list[index], list[resultIndex]) > 0)
                (list[index], list[resultIndex]) = (list[resultIndex], list[index]);
            else
                break;

            index = resultIndex;
        }
    }

    /// <summary>
    /// Gets the parent index of the specified child index in the heap.
    /// </summary>
    /// <param name="index">The child index.</param>
    /// <returns>The parent index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetParentIndex(int index) => (index - 1) / 2;

    /// <summary>
    /// Gets the left child index of the specified parent index in the heap.
    /// </summary>
    /// <param name="index">The parent index.</param>
    /// <returns>The left child index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetLeftChildIndex(int index) => 2 * index + 1;

    /// <summary>
    /// Gets the right child index of the specified parent index in the heap.
    /// </summary>
    /// <param name="index">The parent index.</param>
    /// <returns>The right child index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetRightChildIndex(int index) => 2 * index + 2;

    /// <summary>
    /// Provides an enumerator for iterating through the elements of the binary heap.
    /// </summary>
    public struct Enumerator : IEnumerator<T>
    {
        private          int           m_CurrentIndex;
        private readonly BinaryHeap<T> m_Heap;

        /// <summary>
        /// Initializes a new instance of the Enumerator struct.
        /// </summary>
        /// <param name="heap">The binary heap to enumerate.</param>
        public Enumerator(BinaryHeap<T> heap) => m_Heap = heap;

        /// <summary>
        /// Advances the enumerator to the next element of the heap.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            m_CurrentIndex++;
            return m_CurrentIndex < m_Heap.m_List.Count;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_CurrentIndex = 0;

        /// <summary>
        /// Gets the element in the heap at the current position of the enumerator.
        /// </summary>
        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Heap.m_List[m_CurrentIndex];
        }

        object IEnumerator.Current => m_Heap.m_List[m_CurrentIndex]!;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Reset();
    }
}