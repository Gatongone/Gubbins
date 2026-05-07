using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Gubbins.Enhance;

/// <summary>
/// Provides utility methods and constants for hash table operations, including prime number generation,
/// fast modulo operations, and serialization support for hash-based data structures.
/// </summary>
internal static partial class Hash
{
    /// <summary>
    /// Thread-safe storage for serialization information associated with objects.
    /// Lazily initialized to avoid unnecessary memory allocation.
    /// </summary>
    private static ConditionalWeakTable<object, SerializationInfo>? s_SerializationInfoTable;

    /// <summary>
    /// The threshold for hash collisions before considering rehashing or resizing operations.
    /// Used to maintain optimal hash table performance by detecting excessive collision scenarios.
    /// </summary>
    public const int HASH_COLLISION_THRESHOLD = 100;

    /// <summary>
    /// The maximum prime number smaller than Array.MaxArrayLength (2147483587).
    /// This value represents the largest safe prime that can be used for hash table sizing
    /// without risking array length overflow exceptions.
    /// </summary>
    public const int MAX_PRIME_ARRAY_LENGTH = 0x7FEFFFFD;

    /// <summary>
    /// A prime number commonly used in hash function calculations and collision resolution.
    /// This specific prime (101) is chosen for its mathematical properties that help
    /// distribute hash values uniformly and reduce clustering.
    /// </summary>
    public const int HASH_PRIME = 101;

    /// <summary>
    /// Gets a read-only span containing precomputed prime numbers suitable for hash table sizing.
    /// These primes are ordered from smallest to largest and are used to determine optimal
    /// hash table capacities during resize operations.
    /// </summary>
    /// <value>
    /// A <see cref="ReadOnlySpan{T}"/> of integers containing prime numbers from 3 to 7199369.
    /// </value>
    public static ReadOnlySpan<int> Primes => s_Primes.AsSpan();

    // Table of prime numbers to use as hash table sizes.
    // A typical resize algorithm would pick the smallest prime number in this array
    // that is larger than twice the previous capacity.
    // Suppose our Hashtable currently has capacity x and enough elements are added
    // such that a resize needs to occur. Resizing first computes 2x then finds the
    // first prime in the table greater than 2x, i.e. if primes are ordered
    // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
    // Doubling is important for preserving the asymptotic complexity of the
    // hashtable operations such as add.  Having a prime guarantees that double
    // hashing does not lead to infinite loops.  IE, your hash function will be
    // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
    // We prefer the low computation costs of higher prime numbers over the increased
    // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
    private static readonly int[] s_Primes =
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

    /// <summary>
    /// Determines whether the specified unsigned integer is a prime number using an optimized trial division algorithm.
    /// </summary>
    /// <param name="n">The unsigned integer to test for primality.</param>
    /// <returns>
    /// <c>true</c> if the specified number is prime; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsPrime(uint n) // Use uint for positive ints only
    {
        if (n < 2) return false;
        if (n == 2 || n == 3) return true;
        if ((n & 1) == 0 || n % 3 == 0) return false; // Bitwise even + %3
        uint i = 5;
        while (i * i <= n)
        {
            if (n % i == 0 || n % (i + 2) == 0) return false;
            i += 6;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified integer is a prime number.
    /// </summary>
    /// <param name="n">The integer to test for primality.</param>
    /// <returns>
    /// <c>true</c> if the specified number is non-negative and prime; otherwise, <c>false</c>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrime(int n) => n >= 0 && IsPrime((uint) n);

    /// <summary>
    /// Gets the smallest prime number that is greater than or equal to the specified minimum value.
    /// First searches the precomputed prime table, then calculates primes dynamically if needed.
    /// </summary>
    /// <param name="min">The minimum value for the prime number to return.</param>
    /// <returns>
    /// The smallest prime number greater than or equal to <paramref name="min"/>.
    /// If no suitable prime is found, returns <paramref name="min"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="min"/> is negative, indicating a hashtable capacity overflow.
    /// </exception>
    public static int GetPrime(int min)
    {
        if (min < 0)
            throw new ArgumentException("Hashtable's capacity overflowed and went negative. Check load factor, capacity and the current size of the table.");
        var primes = Primes;
        for (var index = 0; index < primes.Length; ++index)
        {
            var prime = primes[index];
            if (prime >= min)
                return prime;
        }

        for (var candidate = min | 1; candidate < int.MaxValue; candidate += 2)
        {
            if (IsPrime(candidate) && (candidate - 1) % 101 != 0)
                return candidate;
        }

        return min;
    }

    /// <summary>
    /// Calculates an expanded prime number suitable for hashtable resizing by finding the smallest prime 
    /// greater than or equal to twice the old size, with special handling for large sizes.
    /// </summary>
    /// <param name="oldSize">The current size of the hashtable to expand.</param>
    /// <returns>
    /// A prime number suitable for the new hashtable size. For very large sizes, returns the maximum 
    /// prime array length (2147483587) if it's larger than the old size; otherwise returns the smallest 
    /// prime greater than or equal to twice the old size.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExpandPrime(int oldSize)
    {
        var min = 2 * oldSize;
        return (uint) min > 2147483587U && 2147483587 > oldSize ? 2147483587 : GetPrime(min);
    }

    /// <summary>
    /// Computes a fast modulo multiplier for the specified divisor, used in optimized modulo operations.
    /// </summary>
    /// <param name="divisor">The divisor for which to compute the multiplier.</param>
    /// <returns>
    /// A precomputed multiplier value that can be used with <see cref="FastMod"/> for efficient modulo operations.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetFastModMultiplier(uint divisor) => ulong.MaxValue / divisor + 1UL;

    /// <summary>
    /// Performs a fast modulo operation using a precomputed multiplier, avoiding expensive division operations.
    /// </summary>
    /// <param name="value">The value to compute the modulo for.</param>
    /// <param name="divisor">The divisor for the modulo operation.</param>
    /// <param name="multiplier">The precomputed multiplier obtained from <see cref="GetFastModMultiplier"/>.</param>
    /// <returns>
    /// The result of <paramref name="value"/> modulo <paramref name="divisor"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint FastMod(uint value, uint divisor, ulong multiplier)
    {
        return (uint) (((multiplier * value >> 32 /*0x20*/) + 1UL) * divisor >> 32 /*0x20*/);
    }

    /// <summary>
    /// Gets a thread-safe conditional weak table for storing serialization information associated with objects.
    /// The table is lazily initialized on first access.
    /// </summary>
    /// <value>
    /// A <see cref="ConditionalWeakTable{TKey, TValue}"/> that maps objects to their serialization information.
    /// The table allows garbage collection of keys when they are no longer referenced elsewhere.
    /// </value>
    public static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (s_SerializationInfoTable == null)
                Interlocked.CompareExchange(ref s_SerializationInfoTable, new ConditionalWeakTable<object, SerializationInfo>(), null);
            return s_SerializationInfoTable;
        }
    }
}