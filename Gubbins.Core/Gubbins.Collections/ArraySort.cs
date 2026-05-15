using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Unsafe;

namespace Gubbins.Collections;

/// <summary>
/// Provides high-performance sorting algorithms for arrays and spans of type T.
/// Implements an introspective sort algorithm that combines quicksort, heapsort, and insertion sort
/// to achieve optimal performance across different data sizes and patterns.
/// </summary>
/// <typeparam name="T">The type of elements to be sorted.</typeparam>
internal static class ArraySort<T>
{
    // This is the threshold where Introspective sort switches to Insertion sort.
    // Empirically, 16 seems to speed up most cases without slowing down others, at least for integers.
    // Large value types may benefit from a smaller number.
    internal const int INTROSORT_SIZE_THRESHOLD = 16;

    private static ReadOnlySpan<byte> Log2DeBruijn => // 32
    [
        00, 09, 01, 10, 13, 21, 02, 29,
        11, 14, 16, 18, 22, 25, 03, 30,
        08, 12, 20, 28, 15, 17, 24, 07,
        19, 27, 23, 06, 26, 05, 04, 31
    ];

    /// <summary>
    /// Sorts the elements in a span using an introspective sort algorithm with a custom comparison function.
    /// The algorithm automatically chooses the most appropriate sorting method based on the data size and recursion depth.
    /// </summary>
    /// <param name="keys">The span of elements to sort. The sorting is performed in-place.</param>
    /// <param name="comparison">A comparison function that defines the sort order. Must return a negative value if the first parameter is less than the second, zero if they are equal, and a positive value if the first parameter is greater than the second.</param>
    /// <exception cref="ArgumentException">Thrown when the comparison function returns inconsistent results.</exception>
    /// <exception cref="InvalidOperationException">Thrown when an unexpected error occurs during the comparison of elements.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Sort(Span<T> keys, Comparison<T> comparison)
    {
        if (keys.Length <= 1)
        {
            return;
        }

        try
        {
#if NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IntroSort(keys, 2 * (System.Numerics.BitOperations.Log2((uint) keys.Length) + 1), comparison);
#else
            IntroSort(keys, 2 * (Log2((uint) keys.Length) + 1), comparison);
#endif
        }
        catch (IndexOutOfRangeException)
        {
            throw new ArgumentException($"Unable to sort because the IComparer.Compare() method returns inconsistent results. Either a value does not compare equal to itself, or one value repeatedly compared to another value yields different results. IComparer: '{comparison.GetType()}'.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to compare two elements in the array.", ex);
        }
    }

    /// <summary>
    /// Performs an introspective sort on the specified span of elements.
    /// Uses quicksort with a depth limit, switching to heapsort when the recursion depth exceeds the limit,
    /// and insertion sort for small partitions.
    /// </summary>
    /// <param name="keys">The span of elements to sort.</param>
    /// <param name="depthLimit">The maximum recursion depth before switching to heapsort to avoid worst-case O(n²) performance.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void IntroSort(Span<T> keys, int depthLimit, Comparison<T> comparer)
    {
        var partitionSize = keys.Length;
        while (partitionSize > 1)
        {
            if (partitionSize <= INTROSORT_SIZE_THRESHOLD)
            {
                switch (partitionSize)
                {
                    case 2:
                        SwapIfGreater(keys, comparer, 0, 1);
                        return;
                    case 3:
                        SwapIfGreater(keys, comparer, 0, 1);
                        SwapIfGreater(keys, comparer, 0, 2);
                        SwapIfGreater(keys, comparer, 1, 2);
                        return;
                    default:
                        InsertionSort(keys.Slice(0, partitionSize), comparer);
                        return;
                }
            }

            if (depthLimit == 0)
            {
                HeapSort(keys[..partitionSize], comparer);
                return;
            }

            depthLimit--;

            var p = PickPivotAndPartition(keys.Slice(0, partitionSize), comparer);

            // Note we've already partitioned around the pivot and do not have to move the pivot again.
            IntroSort(keys[(p + 1)..partitionSize], depthLimit, comparer);
            partitionSize = p;
        }
    }

    /// <summary>
    /// Sorts the elements in a span using the heapsort algorithm.
    /// Provides O(n log n) worst-case performance with in-place sorting.
    /// </summary>
    /// <param name="keys">The span of elements to sort.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    private static void HeapSort(Span<T> keys, Comparison<T> comparer)
    {
        var n = keys.Length;
        for (var i = n >> 1; i >= 1; i--)
        {
            DownHeap(keys, i, n, comparer);
        }

        for (var i = n; i > 1; i--)
        {
            (keys[0], keys[i - 1]) = (keys[i - 1], keys[0]);
            DownHeap(keys, 1, i - 1, comparer);
        }
    }

    /// <summary>
    /// Sorts the elements in a span using the insertion sort algorithm.
    /// Provides optimal performance for small arrays and nearly sorted data.
    /// </summary>
    /// <param name="keys">The span of elements to sort.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    private static void InsertionSort(Span<T> keys, Comparison<T> comparer)
    {
        for (var i = 0; i < keys.Length - 1; i++)
        {
            var t = keys[i + 1];

            var j = i;
            while (j >= 0 &&
                comparer(t, keys[j]) < 0)
            {
                keys[j + 1] = keys[j];
                j--;
            }

            keys[j + 1] = t;
        }
    }

    /// <summary>
    /// Maintains the heap property by moving an element down the heap until it finds its correct position.
    /// Used as a helper method for the heapsort algorithm.
    /// </summary>
    /// <param name="keys">The span representing the heap.</param>
    /// <param name="i">The 1-based index of the element to move down the heap.</param>
    /// <param name="n">The size of the heap.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    private static void DownHeap(Span<T> keys, int i, int n, Comparison<T> comparer)
    {
        var d = keys[i - 1];
        while (i <= n >> 1)
        {
            var child = 2 * i;
            if (child < n && comparer(keys[child - 1], keys[child]) < 0)
            {
                child++;
            }

            if (!(comparer(d, keys[child - 1]) < 0))
                break;

            keys[i - 1] = keys[child - 1];
            i           = child;
        }

        keys[i - 1] = d;
    }

    /// <summary>
    /// Swaps two elements in a span if the first element is greater than the second element.
    /// Used as an optimization to avoid unnecessary swaps when elements are already in correct order.
    /// </summary>
    /// <param name="keys">The span containing the elements to potentially swap.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    /// <param name="i">The index of the first element.</param>
    /// <param name="j">The index of the second element.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SwapIfGreater(Span<T> keys, Comparison<T> comparer, int i, int j)
    {
        if (comparer(keys[i], keys[j]) <= 0) return;
        (keys[i], keys[j]) = (keys[j], keys[i]);
    }

    /// <summary>
    /// Selects a pivot element using the median-of-three method and partitions the span around it.
    /// Elements less than the pivot are moved to the left, and elements greater than the pivot are moved to the right.
    /// </summary>
    /// <param name="keys">The span of elements to partition.</param>
    /// <param name="comparer">The comparison function used to determine the relative order of elements.</param>
    /// <returns>The final index position of the pivot element after partitioning.</returns>
    private static int PickPivotAndPartition(Span<T> keys, Comparison<T> comparer)
    {
        var hi = keys.Length - 1;

        // Compute median-of-three.  But also partition them, since we've done the comparison.
        var middle = hi >> 1;

        // Sort lo, mid and hi appropriately, then pick mid as the pivot.
        SwapIfGreater(keys, comparer, 0, middle);  // swap the low with the mid point
        SwapIfGreater(keys, comparer, 0, hi);      // swap the low with the high
        SwapIfGreater(keys, comparer, middle, hi); // swap the middle with the high

        var pivot = keys[middle];
        (keys[middle], keys[hi - 1]) = (keys[hi - 1], keys[middle]);
        int left = 0, right = hi - 1; // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

        while (left < right)
        {
            while (comparer(keys[++left], pivot) < 0) { }

            while (comparer(pivot, keys[--right]) < 0) { }

            if (left >= right)
                break;

            (keys[left], keys[right]) = (keys[right], keys[left]);
        }

        // Put pivot in the right location.
        if (left != hi - 1)
        {
            (keys[left], keys[hi - 1]) = (keys[hi - 1], keys[left]);
        }

        return left;
    }

    /// <summary>
    /// Calculates the base-2 logarithm of a 32-bit unsigned integer using a de Bruijn sequence lookup table.
    /// This method provides a fast, branch-free implementation that returns the floor of log₂(value).
    /// </summary>
    /// <param name="value">The unsigned integer value for which to calculate the logarithm. A value of 0 returns 0 by convention.</param>
    /// <returns>The integer floor of the base-2 logarithm of the input value. Returns 0 for input value 0.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Log2(uint value)
    {
        // The 0->0 contract is fulfilled by setting the LSB to 1.
        // Log(1) is 0, and setting the LSB for values > 1 does not change the log2 result.
        value |= 1;

        // Returns the integer (floor) log of the specified value, base 2.
        // Note that by convention, input value 0 returns 0 since Log(0) is undefined.
        // Does not directly use any hardware intrinsics, nor does it incur branching.
        value |= value >> 01;
        value |= value >> 02;
        value |= value >> 04;
        value |= value >> 08;
        value |= value >> 16;

        // uint.MaxValue >> 27 is always in range [0 - 31] so we use Unsafe.AddByteOffset to avoid bounds check
        return Native.AddByteOffset(
            // Using deBruijn sequence, k=2, n=5 (2^5=32) : 0b_0000_0111_1100_0100_1010_1100_1101_1101u
            ref MemoryMarshal.GetReference(Log2DeBruijn),
            // uint|long -> IntPtr cast on 32-bit platforms does expensive overflow checks not needed here
            (int) ((value * 0x07C4ACDDu) >> 27));
    }
}