using System.Collections.Concurrent;

namespace Gubbins.Enhance;

/// <summary>
/// Represents a fixed-size array pool that allows renting and returning arrays of a specific length.
/// This implementation uses a concurrent dictionary to manage the pool of arrays,
/// ensuring thread safety when renting and returning arrays.
/// The pool is designed to provide efficient reuse of arrays, minimizing memory allocations
/// and improving performance in scenarios where arrays of the same length are frequently needed.
/// </summary>
/// <typeparam name="T"></typeparam>
public class FixedArrayPool<T> : System.Buffers.ArrayPool<T>
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="FixedArrayPool{T}"/> class.
    /// </summary>
    public static readonly FixedArrayPool<T> Instance = new();

    /// <summary>
    /// A concurrent dictionary that maps array lengths to arrays of type <typeparamref name="T"/>.
    /// </summary>
    private readonly ConcurrentDictionary<int, T[]> m_Pool = new();

    /// <inheritdoc />
    public override T[] Rent(int length)
    {
        if (m_Pool.IsEmpty || !m_Pool.TryGetValue(length, out var array))
        {
            array = new T[length];
        }

        return array;
    }

    /// <inheritdoc />
    public override void Return(T[] array, bool clearArray = false)
    {
        for (var index = 0; index < array.Length; index++)
        {
            array[index] = default!;
        }

        m_Pool[array.Length] = array;
    }
}