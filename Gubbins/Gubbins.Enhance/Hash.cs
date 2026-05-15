using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Gubbins.Enhance;

/// <summary>
/// Provides utility methods and constants for hash table operations, including prime number generation,
/// fast modulo operations, and serialization support for hash-based data structures.
/// </summary>
public static partial class Hash
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

static partial class Hash
{
    /// <summary>
    /// Precomputed lookup table for CRC-8 checksum calculations.
    /// Contains 256 entries representing all possible CRC-8 values for single-byte inputs.
    /// This table enables fast CRC-8 computation using table lookup instead of polynomial division.
    /// </summary>
    private static readonly byte[] s_Crc8Table =
    [
        0x00, 0x5E, 0xBC, 0xE2, 0x61, 0x3F, 0xDD, 0x83,
        0xC2, 0x9C, 0x7E, 0x20, 0xA3, 0xFD, 0x1F, 0x41,
        0x9D, 0xC3, 0x21, 0x7F, 0xFC, 0xA2, 0x40, 0x1E,
        0x5F, 0x01, 0xE3, 0xBD, 0x3E, 0x60, 0x82, 0xDC,
        0x23, 0x7D, 0x9F, 0xC1, 0x42, 0x1C, 0xFE, 0xA0,
        0xE1, 0xBF, 0x5D, 0x03, 0x80, 0xDE, 0x3C, 0x62,
        0xBE, 0xE0, 0x02, 0x5C, 0xDF, 0x81, 0x63, 0x3D,
        0x7C, 0x22, 0xC0, 0x9E, 0x1D, 0x43, 0xA1, 0xFF,
        0x46, 0x18, 0xFA, 0xA4, 0x27, 0x79, 0x9B, 0xC5,
        0x84, 0xDA, 0x38, 0x66, 0xE5, 0xBB, 0x59, 0x07,
        0xDB, 0x85, 0x67, 0x39, 0xBA, 0xE4, 0x06, 0x58,
        0x19, 0x47, 0xA5, 0xFB, 0x78, 0x26, 0xC4, 0x9A,
        0x65, 0x3B, 0xD9, 0x87, 0x04, 0x5A, 0xB8, 0xE6,
        0xA7, 0xF9, 0x1B, 0x45, 0xC6, 0x98, 0x7A, 0x24,
        0xF8, 0xA6, 0x44, 0x1A, 0x99, 0xC7, 0x25, 0x7B,
        0x3A, 0x64, 0x86, 0xD8, 0x5B, 0x05, 0xE7, 0xB9,
        0x8C, 0xD2, 0x30, 0x6E, 0xED, 0xB3, 0x51, 0x0F,
        0x4E, 0x10, 0xF2, 0xAC, 0x2F, 0x71, 0x93, 0xCD,
        0x11, 0x4F, 0xAD, 0xF3, 0x70, 0x2E, 0xCC, 0x92,
        0xD3, 0x8D, 0x6F, 0x31, 0xB2, 0xEC, 0x0E, 0x50,
        0xAF, 0xF1, 0x13, 0x4D, 0xCE, 0x90, 0x72, 0x2C,
        0x6D, 0x33, 0xD1, 0x8F, 0x0C, 0x52, 0xB0, 0xEE,
        0x32, 0x6C, 0x8E, 0xD0, 0x53, 0x0D, 0xEF, 0xB1,
        0xF0, 0xAE, 0x4C, 0x12, 0x91, 0xCF, 0x2D, 0x73,
        0xCA, 0x94, 0x76, 0x28, 0xAB, 0xF5, 0x17, 0x49,
        0x08, 0x56, 0xB4, 0xEA, 0x69, 0x37, 0xD5, 0x8B,
        0x57, 0x09, 0xEB, 0xB5, 0x36, 0x68, 0x8A, 0xD4,
        0x95, 0xCB, 0x29, 0x77, 0xF4, 0xAA, 0x48, 0x16,
        0xE9, 0xB7, 0x55, 0x0B, 0x88, 0xD6, 0x34, 0x6A,
        0x2B, 0x75, 0x97, 0xC9, 0x4A, 0x14, 0xF6, 0xA8,
        0x74, 0x2A, 0xC8, 0x96, 0x15, 0x4B, 0xA9, 0xF7,
        0xB6, 0xE8, 0x0A, 0x54, 0xD7, 0x89, 0x6B, 0x35
    ];

    /// <summary>
    /// Precomputed lookup table for CRC-16 checksum calculations.
    /// Contains 256 entries representing all possible CRC-16 values for single-byte inputs.
    /// This table enables fast CRC-16 computation using table lookup instead of polynomial division.
    /// </summary>
    private static readonly ushort[] s_CRC16Table =
    [
        0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241, 0xC601, 0x06C0, 0x0780,
        0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440, 0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1,
        0xCE81, 0x0E40, 0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841, 0xD801,
        0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40, 0x1E00, 0xDEC1, 0xDF81, 0x1F40,
        0xDD01, 0x1DC0, 0x1C80, 0xDC41, 0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680,
        0xD641, 0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040, 0xF001, 0x30C0,
        0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240, 0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501,
        0x35C0, 0x3480, 0xF441, 0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
        0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840, 0x2800, 0xE8C1, 0xE981,
        0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41, 0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1,
        0xEC81, 0x2C40, 0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640, 0x2200,
        0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041, 0xA001, 0x60C0, 0x6180, 0xA141,
        0x6300, 0xA3C1, 0xA281, 0x6240, 0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480,
        0xA441, 0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41, 0xAA01, 0x6AC0,
        0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840, 0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01,
        0x7BC0, 0x7A80, 0xBA41, 0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
        0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640, 0x7200, 0xB2C1, 0xB381,
        0x7340, 0xB101, 0x71C0, 0x7080, 0xB041, 0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0,
        0x5280, 0x9241, 0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440, 0x9C01,
        0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40, 0x5A00, 0x9AC1, 0x9B81, 0x5B40,
        0x9901, 0x59C0, 0x5880, 0x9841, 0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81,
        0x4A40, 0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41, 0x4400, 0x84C1,
        0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641, 0x8201, 0x42C0, 0x4380, 0x8341, 0x4100,
        0x81C1, 0x8081, 0x4040
    ];

    /// <summary>
    /// Precomputed lookup table for CRC-32 checksum calculations.
    /// Contains 256 entries representing all possible CRC-32 values for single-byte inputs.
    /// This table enables fast CRC-32 computation using table lookup instead of polynomial division.
    /// </summary>
    private static readonly uint[] s_CRC32Table =
    [
        0x00000000, 0x04c11db7, 0x09823b6e, 0x0d4326d9, 0x130476dc, 0x17c56b6b, 0x1a864db2, 0x1e475005,
        0x2608edb8, 0x22c9f00f, 0x2f8ad6d6, 0x2b4bcb61, 0x350c9b64, 0x31cd86d3, 0x3c8ea00a, 0x384fbdbd,
        0x4c11db70, 0x48d0c6c7, 0x4593e01e, 0x4152fda9, 0x5f15adac, 0x5bd4b01b, 0x569796c2, 0x52568b75,
        0x6a1936c8, 0x6ed82b7f, 0x639b0da6, 0x675a1011, 0x791d4014, 0x7ddc5da3, 0x709f7b7a, 0x745e66cd,
        0x9823b6e0, 0x9ce2ab57, 0x91a18d8e, 0x95609039, 0x8b27c03c, 0x8fe6dd8b, 0x82a5fb52, 0x8664e6e5,
        0xbe2b5b58, 0xbaea46ef, 0xb7a96036, 0xb3687d81, 0xad2f2d84, 0xa9ee3033, 0xa4ad16ea, 0xa06c0b5d,
        0xd4326d90, 0xd0f37027, 0xddb056fe, 0xd9714b49, 0xc7361b4c, 0xc3f706fb, 0xceb42022, 0xca753d95,
        0xf23a8028, 0xf6fb9d9f, 0xfbb8bb46, 0xff79a6f1, 0xe13ef6f4, 0xe5ffeb43, 0xe8bccd9a, 0xec7dd02d,
        0x34867077, 0x30476dc0, 0x3d044b19, 0x39c556ae, 0x278206ab, 0x23431b1c, 0x2e003dc5, 0x2ac12072,
        0x128e9dcf, 0x164f8078, 0x1b0ca6a1, 0x1fcdbb16, 0x018aeb13, 0x054bf6a4, 0x0808d07d, 0x0cc9cdca,
        0x7897ab07, 0x7c56b6b0, 0x71159069, 0x75d48dde, 0x6b93dddb, 0x6f52c06c, 0x6211e6b5, 0x66d0fb02,
        0x5e9f46bf, 0x5a5e5b08, 0x571d7dd1, 0x53dc6066, 0x4d9b3063, 0x495a2dd4, 0x44190b0d, 0x40d816ba,
        0xaca5c697, 0xa864db20, 0xa527fdf9, 0xa1e6e04e, 0xbfa1b04b, 0xbb60adfc, 0xb6238b25, 0xb2e29692,
        0x8aad2b2f, 0x8e6c3698, 0x832f1041, 0x87ee0df6, 0x99a95df3, 0x9d684044, 0x902b669d, 0x94ea7b2a,
        0xe0b41de7, 0xe4750050, 0xe9362689, 0xedf73b3e, 0xf3b06b3b, 0xf771768c, 0xfa325055, 0xfef34de2,
        0xc6bcf05f, 0xc27dede8, 0xcf3ecb31, 0xcbffd686, 0xd5b88683, 0xd1799b34, 0xdc3abded, 0xd8fba05a,
        0x690ce0ee, 0x6dcdfd59, 0x608edb80, 0x644fc637, 0x7a089632, 0x7ec98b85, 0x738aad5c, 0x774bb0eb,
        0x4f040d56, 0x4bc510e1, 0x46863638, 0x42472b8f, 0x5c007b8a, 0x58c1663d, 0x558240e4, 0x51435d53,
        0x251d3b9e, 0x21dc2629, 0x2c9f00f0, 0x285e1d47, 0x36194d42, 0x32d850f5, 0x3f9b762c, 0x3b5a6b9b,
        0x0315d626, 0x07d4cb91, 0x0a97ed48, 0x0e56f0ff, 0x1011a0fa, 0x14d0bd4d, 0x19939b94, 0x1d528623,
        0xf12f560e, 0xf5ee4bb9, 0xf8ad6d60, 0xfc6c70d7, 0xe22b20d2, 0xe6ea3d65, 0xeba91bbc, 0xef68060b,
        0xd727bbb6, 0xd3e6a601, 0xdea580d8, 0xda649d6f, 0xc423cd6a, 0xc0e2d0dd, 0xcda1f604, 0xc960ebb3,
        0xbd3e8d7e, 0xb9ff90c9, 0xb4bcb610, 0xb07daba7, 0xae3afba2, 0xaafbe615, 0xa7b8c0cc, 0xa379dd7b,
        0x9b3660c6, 0x9ff77d71, 0x92b45ba8, 0x9675461f, 0x8832161a, 0x8cf30bad, 0x81b02d74, 0x857130c3,
        0x5d8a9099, 0x594b8d2e, 0x5408abf7, 0x50c9b640, 0x4e8ee645, 0x4a4ffbf2, 0x470cdd2b, 0x43cdc09c,
        0x7b827d21, 0x7f436096, 0x7200464f, 0x76c15bf8, 0x68860bfd, 0x6c47164a, 0x61043093, 0x65c52d24,
        0x119b4be9, 0x155a565e, 0x18197087, 0x1cd86d30, 0x029f3d35, 0x065e2082, 0x0b1d065b, 0x0fdc1bec,
        0x3793a651, 0x3352bbe6, 0x3e119d3f, 0x3ad08088, 0x2497d08d, 0x2056cd3a, 0x2d15ebe3, 0x29d4f654,
        0xc5a92679, 0xc1683bce, 0xcc2b1d17, 0xc8ea00a0, 0xd6ad50a5, 0xd26c4d12, 0xdf2f6bcb, 0xdbee767c,
        0xe3a1cbc1, 0xe760d676, 0xea23f0af, 0xeee2ed18, 0xf0a5bd1d, 0xf464a0aa, 0xf9278673, 0xfde69bc4,
        0x89b8fd09, 0x8d79e0be, 0x803ac667, 0x84fbdbd0, 0x9abc8bd5, 0x9e7d9662, 0x933eb0bb, 0x97ffad0c,
        0xafb010b1, 0xab710d06, 0xa6322bdf, 0xa2f33668, 0xbcb4666d, 0xb8757bda, 0xb5365d03, 0xb1f740b4
    ];

    /// <summary>
    /// Calculates the CRC-8 checksum for the specified byte data using a lookup table.
    /// </summary>
    /// <param name="data">The byte data to calculate the CRC-8 checksum for.</param>
    /// <returns>An 8-bit CRC checksum value.</returns>
    public static byte CalculateCRC8(ReadOnlySpan<byte> data)
    {
        byte crc = 0;

        for (var i = 0; i < data.Length; i++)
        {
            crc = s_Crc8Table[crc ^ data[i]];
        }

        return crc;
    }

    /// <summary>
    /// Calculates the CRC-8 checksum for the specified character data by converting it to bytes.
    /// </summary>
    /// <param name="data">The character data to calculate the CRC-8 checksum for.</param>
    /// <returns>An 8-bit CRC checksum value cast to uint.</returns>
    public static uint CalculateCRC8(ReadOnlySpan<char> data) => CalculateCRC8(MemoryMarshal.Cast<char, byte>(data));

    /// <summary>
    /// Calculates the CRC-16 checksum for the specified byte data using a lookup table.
    /// </summary>
    /// <param name="data">The byte data to calculate the CRC-16 checksum for.</param>
    /// <returns>A 16-bit CRC checksum value.</returns>
    public static ushort CalculateCRC16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0;
        for (var i = 0; i < data.Length; ++i)
        {
            var index = (byte) (crc >> 8 ^ data[i]);
            crc = (ushort) (s_CRC16Table[index] ^ (crc << 8));
        }

        return crc;
    }

    /// <summary>
    /// Calculates the CRC-16 checksum for the specified character data by converting it to bytes.
    /// </summary>
    /// <param name="data">The character data to calculate the CRC-16 checksum for.</param>
    /// <returns>A 16-bit CRC checksum value.</returns>
    public static ushort CalculateCRC16(ReadOnlySpan<char> data) => CalculateCRC8(MemoryMarshal.Cast<char, byte>(data));

    /// <summary>
    /// Calculates the CRC-32 checksum for the specified byte data using a lookup table.
    /// </summary>
    /// <param name="data">The byte data to calculate the CRC-32 checksum for.</param>
    /// <returns>A 32-bit CRC checksum value.</returns>
    public static uint CalculateCRC32(ReadOnlySpan<byte> data)
    {
        uint crc = 0;
        for (var i = 0; i < data.Length; i++)
        {
            crc = (crc << 8) ^ s_CRC32Table[(crc >> 24) ^ data[i]];
        }

        return crc;
    }

    /// <summary>
    /// Calculates the CRC-32 checksum for the specified character data by converting it to bytes.
    /// </summary>
    /// <param name="data">The character data to calculate the CRC-32 checksum for.</param>
    /// <returns>A 32-bit CRC checksum value.</returns>
    public static uint CalculateCRC32(ReadOnlySpan<char> data) => CalculateCRC32(MemoryMarshal.Cast<char, byte>(data));
}