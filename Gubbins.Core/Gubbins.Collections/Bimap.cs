using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Enhance;

namespace Gubbins.Collections;

/// <summary>
/// A minimal perfect hash function over a forward set of type <typeparamref name="T" />.
///
/// The caller supplies a hasher <c>Func&lt;T, uint, uint&gt;</c> returning the raw 32-bit
/// hash of a forward for a given seed. Forwards are hashed in place through a
/// <see cref="ReadOnlySpan{T}" /> view of their own memory, so no intermediate
/// <c>byte[]</c> is ever allocated.
/// </summary>
/// <typeparam name="T">The type of the elements being hashed.</typeparam>
internal sealed class MPHF<T>
{
    /// <summary>The seed used to hash elements into their initial buckets.</summary>
    private const uint INIT_SEED = 0;

    /// <summary>Masks a hash to its lower 28 bits, keeping values positive and bounded before the modulo.</summary>
    private const int SIGN_MASK = 0xfffffff;

    /// <summary>Number of elements grouped into each bucket during construction.</summary>
    private readonly int m_ForwardsPerBucket;

    /// <summary>Ratio of elements to bins; at most 1.0.</summary>
    private readonly double m_LoadFactor;

    /// <summary>Upper bound (exclusive) on the displacement values tried per bucket.</summary>
    private readonly int m_MaxSeed;

    /// <summary>Returns the raw 32-bit hash of an element for a given seed.</summary>
    private readonly Func<T, uint, uint> m_Hash;

    /// <summary>The per-bucket displacement values produced by construction.</summary>
    private uint[] m_DisplacementArray = [];

    /// <summary>The number of bins (output slots) available to the hash function.</summary>
    private int m_NumBins;

    /// <summary>The number of buckets elements are partitioned into.</summary>
    private int m_NumBuckets;

    /// <summary>The number of elements covered by the hash function.</summary>
    private int m_NumForwards;

    /// <summary>
    /// Initializes a minimal perfect hash function with the supplied hasher and tuning
    /// parameters. Construction is deferred until <see cref="Construct" /> is called.
    /// </summary>
    /// <param name="hash">Returns the raw 32-bit hash of <typeparamref name="T" /> for a given seed.</param>
    /// <param name="forwardsPerBucket">Number of elements grouped into each bucket during construction.</param>
    /// <param name="maxSeed">Upper bound (exclusive) on the displacement values tried per bucket.</param>
    /// <param name="loadFactor">Ratio of elements to bins; must be at most 1.0.</param>
    public MPHF(Func<T, uint, uint> hash, int forwardsPerBucket = 4, int maxSeed = int.MaxValue, double loadFactor = 1.0)
    {
        if (loadFactor > 1.0)
            throw new ArgumentException("Load factor should be <= 1.0");

        m_Hash              = hash ?? throw new ArgumentNullException(nameof(hash));
        m_ForwardsPerBucket = forwardsPerBucket;
        m_MaxSeed           = maxSeed;
        m_LoadFactor        = loadFactor;
    }

    /// <summary>
    /// Builds the displacement array that makes the hash function minimal (injective) over
    /// the supplied elements. Each element maps to a distinct index in <c>[0, n)</c>.
    /// </summary>
    /// <param name="forwards">The elements to make hashable in O(1).</param>
    /// <returns>The per-bucket displacement array used by <see cref="Hash" />.</returns>
    /// <exception cref="Exception">Thrown when no valid displacement can be found before <c>maxSeed</c>.</exception>
    public IEnumerable<uint> Construct(IReadOnlyList<T> forwards)
    {
        m_NumForwards       = forwards.Count;
        m_NumBins           = (int) (m_NumForwards / m_LoadFactor);
        m_NumBuckets        = Math.Max(1, m_NumForwards / m_ForwardsPerBucket);
        m_DisplacementArray = new uint[m_NumBuckets];

        var occupied = new bool[m_NumBins];
        var buckets = CreateBuckets(forwards);

        foreach (var bucket in buckets)
        {
            if (bucket.Count == 0) continue;

            var bucketIndex = GetBucketIndex(forwards[bucket[0]]);
            var placed = new int[bucket.Count];
            var success = false;

            for (var displacement = INIT_SEED + 1; !success && displacement < m_MaxSeed; displacement++)
            {
                // A displacement is valid only if every forward in the bucket lands in a bin
                // that is distinct within the bucket and not yet occupied by an earlier
                // bucket. One pass over the bucket; the occupied set is a plain bool[]
                // checked in O(1) (no re-scan of all bins, no stale state between tries).
                var seen = new HashSet<int>(bucket.Count);
                var n = 0;
                var ok = true;

                foreach (var idx in bucket)
                {
                    var bin = Phi(forwards[idx], displacement);
                    if (!seen.Add(bin) || occupied[bin])
                    {
                        ok = false;
                        break;
                    }

                    placed[n++] = bin;
                }

                if (!ok)
                    continue;

                success                          = true;
                m_DisplacementArray[bucketIndex] = displacement;
                for (var i = 0; i < n; i++)
                    occupied[placed[i]] = true;
            }

            if (!success)
                throw new Exception("Cannot construct perfect hash function");
        }

        return m_DisplacementArray;
    }

    /// <summary>
    /// Returns the unique index in <c>[0, n)</c> assigned to <paramref name="forward" />.
    /// Must only be called after <see cref="Construct" /> or <see cref="Import" />.
    /// </summary>
    /// <param name="forward">The element to hash.</param>
    /// <returns>A unique non-negative index for this element.</returns>
    public uint Hash(T forward)
    {
        var displacement = m_DisplacementArray[GetBucketIndex(forward)];
        return (uint) Phi(forward, displacement);
    }

    /// <summary>
    /// Restores a previously constructed hash function from a displacement array, avoiding
    /// the cost of re-construction.
    /// </summary>
    /// <param name="forwardSize">The number of elements covered by the hash function.</param>
    /// <param name="displacementArray">The displacement array produced by <see cref="Construct" />.</param>
    /// <param name="loadFactor">The load factor used during construction.</param>
    public void Import(int forwardSize, IEnumerable<uint> displacementArray, double loadFactor = 1.0)
    {
        m_NumForwards       = forwardSize;
        m_NumBins           = (int) (m_NumForwards / loadFactor);
        m_DisplacementArray = displacementArray.ToArray();
        m_NumBuckets        = m_DisplacementArray.Length;
    }

    /// <summary>
    /// Partitions the elements into buckets, returning them ordered by descending size so
    /// the hardest buckets are placed first during construction.
    /// </summary>
    /// <param name="forwards">The elements to partition.</param>
    /// <returns>The buckets, each holding indices into <paramref name="forwards" />, largest first.</returns>
    private IEnumerable<List<int>> CreateBuckets(IReadOnlyList<T> forwards)
    {
        var buckets = new List<int>[m_NumBuckets];
        for (var i = 0; i < m_NumBuckets; i++)
            buckets[i] = [];

        for (var i = 0; i < forwards.Count; i++)
            buckets[GetBucketIndex(forwards[i])].Add(i);

        return buckets.OrderByDescending(bucket => bucket.Count);
    }

    /// <summary>Maps an element to the index of the bucket it belongs to.</summary>
    /// <param name="forward">The element to bucket.</param>
    /// <returns>The bucket index in <c>[0, m_NumBuckets)</c>.</returns>
    private int GetBucketIndex(T forward) => (int) ((m_Hash(forward, INIT_SEED) & SIGN_MASK) % (uint) m_NumBuckets);

    /// <summary>Maps an element to a bin using the given displacement value.</summary>
    /// <param name="forward">The element to place.</param>
    /// <param name="displacement">The displacement of the element's bucket.</param>
    /// <returns>The bin index in <c>[0, m_NumBins)</c>.</returns>
    private int Phi(T forward, uint displacement) => (int) ((m_Hash(forward, displacement) & SIGN_MASK) % (uint) m_NumBins);
}

/// <summary>
/// A bijective (one-to-one) map backed by two minimal perfect hash functions.
///
/// Unlike a double hash table (a <c>Dictionary&lt;TForward,TReverse&gt;</c> plus a
/// <c>Dictionary&lt;TReverse,TForward&gt;</c>), this stores no buckets/entry chains:
/// space is O(n) — exactly <see cref="Count" /> forwards and <see cref="Count" />
/// reverses in two dense arrays, plus two small displacement arrays.
///
/// Both <see cref="GetReverse" /> and <see cref="GetForward" /> are O(1): the forward is
/// hashed to a unique index in <c>[0, n)</c> by its MPHF and the reverse is read
/// from that slot (and symmetrically for the reverse direction).
/// </summary>
/// <typeparam name="TForward">The type of the forward keys.</typeparam>
/// <typeparam name="TReverse">The type of the reverse values.</typeparam>
public class ReadOnlyBimap<TForward, TReverse> : IReadOnlyBimap<TForward, TReverse> where TForward : notnull where TReverse : notnull
{
    /// <summary>Maps each forward to a unique index in <c>[0, n)</c>.</summary>
    private readonly MPHF<TForward> m_ForwardMph;

    /// <summary>Maps each reverse to a unique index in <c>[0, n)</c>.</summary>
    private readonly MPHF<TReverse> m_ReverseMph;

    /// <summary>The dense forward table, indexed by the reverse MPHF.</summary>
    private readonly TForward[] m_ForwardTable;

    /// <summary>The dense reverse table, indexed by the forward MPHF.</summary>
    private readonly TReverse[] m_ReverseTable;

    /// <summary>Compares forwards for equality when verifying membership.</summary>
    private readonly IEqualityComparer<TForward> m_ForwardComparer;

    /// <summary>Compares reverses for equality when verifying membership.</summary>
    private readonly IEqualityComparer<TReverse> m_ReverseComparer;

    /// <summary>The forwards in this bimap, in unspecified order.</summary>
    IEnumerable<TForward> IReadOnlyBimap<TForward, TReverse>.Forwards => m_ForwardTable;

    /// <summary>The reverses in this bimap, in unspecified order.</summary>
    IEnumerable<TReverse> IReadOnlyBimap<TForward, TReverse>.Reverses => m_ReverseTable;

    /// <summary>The forwards in this bimap, in unspecified order.</summary>
    public ReadOnlySpan<TForward> Forwards => m_ForwardTable;

    /// <summary>The reverses in this bimap, in unspecified order.</summary>
    public ReadOnlySpan<TReverse> Reverses => m_ReverseTable;

    /// <summary>The number of forward/reverse pairs in the bimap.</summary>
    public int Count { get; }

    /// <summary>
    /// Builds a bimap from the given pairs. Forwards and reverses must each be distinct.
    /// </summary>
    /// <param name="pairs">The forward/reverse pairs. Forwards and reverses must both be unique.</param>
    /// <param name="forwardHasher">
    /// Hashes a forward to a raw 32-bit reverse for a seed. When <c>null</c>, a built-in
    /// zero-allocation hasher for common types (primitives, <c>string</c>, <c>Guid</c>,
    /// <c>decimal</c>) is used.
    /// </param>
    /// <param name="reverseHasher">Same as <paramref name="forwardHasher" />, but for reverses.</param>
    /// <param name="forwardComparer">Compares forwards for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <param name="reverseComparer">Compares reverses for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <exception cref="ArgumentException">Thrown when a forward or reverse is duplicated.</exception>
    public ReadOnlyBimap(
        IEnumerable<KeyValuePair<TForward, TReverse>> pairs,
        Func<TForward, uint, uint>? forwardHasher = null,
        Func<TReverse, uint, uint>? reverseHasher = null,
        IEqualityComparer<TForward>? forwardComparer = null,
        IEqualityComparer<TReverse>? reverseComparer = null)
    {
        var items = pairs as IReadOnlyList<KeyValuePair<TForward, TReverse>> ?? pairs.ToList();
        m_ForwardComparer = forwardComparer ?? EqualityComparer<TForward>.Default;
        m_ReverseComparer = reverseComparer ?? EqualityComparer<TReverse>.Default;
        Count             = items.Count;

        var hashForward = forwardHasher ?? DefaultHash;
        var hashReverse = reverseHasher ?? DefaultHash;

        EnsureBijective(items);

        var forwards = new TForward[Count];
        var reverses = new TReverse[Count];
        for (var i = 0; i < Count; i++)
        {
            forwards[i] = items[i].Key;
            reverses[i] = items[i].Value;
        }

        m_ForwardMph = new MPHF<TForward>(hashForward);
        m_ForwardMph.Construct(forwards);
        m_ReverseMph = new MPHF<TReverse>(hashReverse);
        m_ReverseMph.Construct(reverses);

        m_ForwardTable = new TForward[Count];
        m_ReverseTable = new TReverse[Count];

        for (var i = 0; i < Count; i++)
        {
            m_ReverseTable[(int) m_ForwardMph.Hash(forwards[i])] = reverses[i];
            m_ForwardTable[(int) m_ReverseMph.Hash(reverses[i])] = forwards[i];
        }
    }

    /// <summary>
    /// Builds a read-only bimap from an existing <see cref="Bitmap{TForward, TReverse}" />,
    /// inheriting its comparers.
    /// </summary>
    /// <param name="bitmap">The source bimap whose pairs are copied.</param>
    /// <param name="forwardHasher">Custom forward hasher; defaults to the built-in hasher.</param>
    /// <param name="reverseHasher">Custom reverse hasher; defaults to the built-in hasher.</param>
    public ReadOnlyBimap(Bitmap<TForward, TReverse> bitmap, Func<TForward, uint, uint>? forwardHasher = null, Func<TReverse, uint, uint>? reverseHasher = null)
    {
        m_ForwardComparer = bitmap.ForwardComparer;
        m_ReverseComparer = bitmap.ReverseComparer;

        Count = bitmap.Count;

        var hashForward = forwardHasher ?? DefaultHash;
        var hashReverse = reverseHasher ?? DefaultHash;

        var forwards = new TForward[Count];
        var reverses = new TReverse[Count];
        var index = 0;

        foreach (var kv in bitmap)
        {
            forwards[index] = kv.Key;
            reverses[index] = kv.Value;
            index++;
        }

        m_ForwardMph = new MPHF<TForward>(hashForward);
        m_ForwardMph.Construct(forwards);
        m_ReverseMph = new MPHF<TReverse>(hashReverse);
        m_ReverseMph.Construct(reverses);

        m_ForwardTable = new TForward[Count];
        m_ReverseTable = new TReverse[Count];

        for (var i = 0; i < Count; i++)
        {
            m_ReverseTable[(int) m_ForwardMph.Hash(forwards[i])] = reverses[i];
            m_ForwardTable[(int) m_ReverseMph.Hash(reverses[i])] = forwards[i];
        }
    }

    /// <summary>Returns the reverse associated with <paramref name="forward" /> in O(1).</summary>
    /// <remarks>
    /// Valid only for forwards present in the map; a forward that was never inserted hashes
    /// to an arbitrary slot (an inherent MPHF trade-off — no forward is stored for verification).
    /// </remarks>
    /// <param name="forward">The forward whose reverse is looked up.</param>
    /// <returns>The reverse paired with <paramref name="forward" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TReverse GetReverse(TForward forward) => m_ReverseTable[(int) m_ForwardMph.Hash(forward)];

    /// <summary>Returns the forward associated with <paramref name="reverse" /> in O(1).</summary>
    /// <remarks>Valid only for reverses present in the map (see <see cref="GetReverse" />).</remarks>
    /// <param name="reverse">The reverse whose forward is looked up.</param>
    /// <returns>The forward paired with <paramref name="reverse" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TForward GetForward(TReverse reverse) => m_ForwardTable[(int) m_ReverseMph.Hash(reverse)];

    /// <summary>Determines whether <paramref name="forward" /> exists in the bimap.</summary>
    /// <param name="forward">The forward to locate.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsForward(TForward forward)
    {
        var index = (int) m_ForwardMph.Hash(forward);
        return m_ForwardComparer.Equals(m_ForwardTable[(int) m_ReverseMph.Hash(m_ReverseTable[index])], forward);
    }

    /// <summary>Determines whether <paramref name="reverse" /> exists in the bimap.</summary>
    /// <param name="reverse">The reverse to locate.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsReverse(TReverse reverse)
    {
        var index = (int) m_ReverseMph.Hash(reverse);
        return m_ReverseComparer.Equals(m_ReverseTable[(int) m_ForwardMph.Hash(m_ForwardTable[index])], reverse);
    }

    /// <summary>
    /// Attempts to retrieve the reverse associated with <paramref name="forward" />, verifying
    /// the forward is present before returning.
    /// </summary>
    /// <param name="forward">The forward to look up.</param>
    /// <param name="reverse">When this method returns, contains the paired reverse if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    public bool TryGetReverse(TForward forward, out TReverse reverse)
    {
        var index = (int) m_ForwardMph.Hash(forward);
        if (m_ForwardComparer.Equals(m_ForwardTable[(int) m_ReverseMph.Hash(m_ReverseTable[index])], forward))
        {
            reverse = m_ReverseTable[index];
            return true;
        }

        reverse = default!;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the forward associated with <paramref name="reverse" />, verifying
    /// the reverse is present before returning.
    /// </summary>
    /// <param name="reverse">The reverse to look up.</param>
    /// <param name="forward">When this method returns, contains the paired forward if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    public bool TryGetForward(TReverse reverse, out TForward forward)
    {
        var index = (int) m_ReverseMph.Hash(reverse);
        if (m_ReverseComparer.Equals(m_ReverseTable[(int) m_ForwardMph.Hash(m_ForwardTable[index])], reverse))
        {
            forward = m_ForwardTable[index];
            return true;
        }

        forward = default!;
        return false;
    }

    /// <summary>Verifies that the forwards and reverses of the given pairs are each distinct.</summary>
    /// <param name="items">The pairs to validate.</param>
    /// <exception cref="ArgumentException">Thrown when a forward or reverse is duplicated.</exception>
    private void EnsureBijective(IReadOnlyList<KeyValuePair<TForward, TReverse>> items)
    {
        var forwards = new HashSet<TForward>(m_ForwardComparer);
        var reverses = new HashSet<TReverse>(m_ReverseComparer);

        foreach (var item in items)
        {
            if (!forwards.Add(item.Key))
                throw new ArgumentException("Forwards must be distinct.", nameof(items));
            if (!reverses.Add(item.Value))
                throw new ArgumentException("Reverses must be distinct for a bimap.", nameof(items));
        }
    }

    /// <summary>
    /// The built-in zero-allocation hasher used when no custom hasher is supplied. Supports
    /// common primitives, <c>string</c>, <c>Guid</c>, and <c>decimal</c>.
    /// </summary>
    /// <typeparam name="T">The type of the element being hashed.</typeparam>
    /// <param name="reverse">The element to hash.</param>
    /// <param name="seed">The seed to feed into the hash.</param>
    /// <returns>The raw 32-bit hash of <paramref name="reverse" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reverse" /> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <typeparamref name="T" /> has no built-in hasher.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DefaultHash<T>(T reverse, uint seed)
    {
        if (reverse is null)
            throw new ArgumentNullException(nameof(reverse));

        return reverse switch
        {
            byte v    => HashStruct(v, seed),
            sbyte v   => HashStruct(v, seed),
            short v   => HashStruct(v, seed),
            ushort v  => HashStruct(v, seed),
            int v     => HashStruct(v, seed),
            uint v    => HashStruct(v, seed),
            long v    => HashStruct(v, seed),
            ulong v   => HashStruct(v, seed),
            float v   => HashStruct(v, seed),
            double v  => HashStruct(v, seed),
            decimal v => HashStruct(v, seed),
            char v    => HashStruct(v, seed),
            bool v    => HashStruct(v, seed),
            Guid v    => HashStruct(v, seed),
            string v  => HashString(v, seed),
            _ => throw new NotSupportedException(
                $"No default hasher for type '{typeof(T)}'. Pass forwardHasher/reverseHasher to the constructor.")
        };
    }

    /// <summary>Hashes a string by Murmur-hashing its UTF-16 bytes in place.</summary>
    /// <param name="reverse">The string to hash.</param>
    /// <param name="seed">The seed to feed into the hash.</param>
    /// <returns>The raw 32-bit hash of <paramref name="reverse" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashString(string reverse, uint seed)
    {
        var bytes = MemoryMarshal.AsBytes(reverse.AsSpan());
        return Hash.Murmur(ref bytes, seed);
    }

    /// <summary>Hashes an unmanaged struct by Murmur-hashing its raw bytes in place.</summary>
    /// <typeparam name="T">The unmanaged type being hashed.</typeparam>
    /// <param name="reverse">The value to hash.</param>
    /// <param name="seed">The seed to feed into the hash.</param>
    /// <returns>The raw 32-bit hash of <paramref name="reverse" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint HashStruct<T>(T reverse, uint seed) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref reverse, 1));
        return Hash.Murmur(ref bytes, seed);
    }

    /// <summary>Returns an enumerator that iterates over the forward/reverse pairs.</summary>
    /// <returns>An enumerator for the bimap.</returns>
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<KeyValuePair<TForward, TReverse>> IEnumerable<KeyValuePair<TForward, TReverse>>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Enumerates the forward/reverse pairs of a <see cref="ReadOnlyBimap{TForward, TReverse}" />.</summary>
    public struct Enumerator(ReadOnlyBimap<TForward, TReverse> bimap) : IEnumerator<KeyValuePair<TForward, TReverse>>
    {
        /// <summary>The current position, or -1 before the first call to <see cref="MoveNext" />.</summary>
        private int m_Index = -1;

        /// <summary>Gets the pair at the current enumerator position.</summary>
        public KeyValuePair<TForward, TReverse> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var forward = bimap.m_ForwardTable[m_Index];
                var reverse = bimap.GetReverse(forward);
                return new(forward, reverse);
            }
        }

        /// <summary>Gets the pair at the current enumerator position.</summary>
        object IEnumerator.Current => Current;

        /// <summary>Advances the enumerator to the next pair.</summary>
        /// <returns><c>true</c> if advanced to a valid position; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++m_Index < bimap.Count;

        /// <summary>Resets the enumerator to its initial position.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => m_Index = -1;

        /// <summary>Releases all resources used by the enumerator.</summary>
        public void Dispose() { }
    }
}

/// <summary>
/// A mutable bijective (one-to-one) map backed by two <see cref="Dictionary{TKey, TValue}" />
/// instances, one in each direction.
///
/// Provides O(1) lookup in both directions and enforces that no forward or reverse appears
/// more than once. Use <see cref="AsReadOnly" /> to convert to a compact, allocation-free
/// <see cref="ReadOnlyBimap{TForward, TReverse}" /> once the map is final.
/// </summary>
/// <typeparam name="TForward">The type of the forward keys.</typeparam>
/// <typeparam name="TReverse">The type of the reverse values.</typeparam>
public class Bitmap<TForward, TReverse> : IBimap<TForward, TReverse> where TForward : notnull where TReverse : notnull
{
    /// <summary>Maps each forward to its paired reverse.</summary>
    private readonly Dictionary<TForward, TReverse> m_Forward;

    /// <summary>Maps each reverse to its paired forward.</summary>
    private readonly Dictionary<TReverse, TForward> m_Reverse;

    /// <summary>The number of forward/reverse pairs in the bimap.</summary>
    public int Count => m_Forward.Count;

    /// <summary>Always <c>false</c>; a bimap is mutable.</summary>
    public bool IsReadOnly => false;

    /// <summary>The forwards in this bimap.</summary>
    public Dictionary<TForward, TReverse>.KeyCollection Forwards => m_Forward.Keys;

    /// <summary>The reverses in this bimap.</summary>
    public Dictionary<TReverse, TForward>.KeyCollection Reverses => m_Reverse.Keys;

    /// <summary>The forwards in this bimap.</summary>
    IEnumerable<TForward> IReadOnlyBimap<TForward, TReverse>.Forwards => m_Forward.Keys;

    /// <summary>The reverses in this bimap.</summary>
    IEnumerable<TReverse> IReadOnlyBimap<TForward, TReverse>.Reverses => m_Reverse.Keys;

    /// <summary>The comparer used to compare forwards for equality.</summary>
    internal IEqualityComparer<TForward> ForwardComparer => m_Forward.Comparer;

    /// <summary>The comparer used to compare reverses for equality.</summary>
    internal IEqualityComparer<TReverse> ReverseComparer => m_Reverse.Comparer;

    /// <summary>Initializes a bimap from the given pairs using the specified capacity.</summary>
    /// <param name="pairs">The forward/reverse pairs to add.</param>
    /// <param name="capacity">The initial capacity for the underlying dictionaries.</param>
    /// <param name="forwardComparer">Compares forwards for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <param name="reverseComparer">Compares reverses for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public Bitmap(IEnumerable<KeyValuePair<TForward, TReverse>> pairs, int capacity, IEqualityComparer<TForward>? forwardComparer = null, IEqualityComparer<TReverse>? reverseComparer = null)
    {
        m_Forward = new Dictionary<TForward, TReverse>(capacity, forwardComparer ?? EqualityComparer<TForward>.Default);
        m_Reverse = new Dictionary<TReverse, TForward>(capacity, reverseComparer ?? EqualityComparer<TReverse>.Default);
        AddRange(pairs);
    }

    /// <summary>Initializes an empty bimap.</summary>
    /// <param name="forwardComparer">Compares forwards for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <param name="reverseComparer">Compares reverses for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public Bitmap(IEqualityComparer<TForward>? forwardComparer = null, IEqualityComparer<TReverse>? reverseComparer = null)
    {
        m_Forward = new Dictionary<TForward, TReverse>(forwardComparer ?? EqualityComparer<TForward>.Default);
        m_Reverse = new Dictionary<TReverse, TForward>(reverseComparer ?? EqualityComparer<TReverse>.Default);
    }

    /// <summary>Initializes a bimap from the given pairs.</summary>
    /// <param name="pairs">The forward/reverse pairs to add.</param>
    /// <param name="forwardComparer">Compares forwards for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <param name="reverseComparer">Compares reverses for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public Bitmap(IEnumerable<KeyValuePair<TForward, TReverse>> pairs, IEqualityComparer<TForward>? forwardComparer = null, IEqualityComparer<TReverse>? reverseComparer = null) : this(pairs, pairs.Count(), forwardComparer, reverseComparer) { }

    /// <summary>Initializes an empty bimap with the specified capacity.</summary>
    /// <param name="capacity">The initial capacity for the underlying dictionaries.</param>
    /// <param name="forwardComparer">Compares forwards for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <param name="reverseComparer">Compares reverses for equality. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    public Bitmap(int capacity, IEqualityComparer<TForward>? forwardComparer = null, IEqualityComparer<TReverse>? reverseComparer = null) : this(Enumerable.Empty<KeyValuePair<TForward, TReverse>>(), capacity, forwardComparer, reverseComparer) { }

    /// <summary>Initializes a bimap by copying another, including its comparers.</summary>
    /// <param name="other">The bimap to copy.</param>
    public Bitmap(Bitmap<TForward, TReverse> other) : this(other.m_Forward, other.Count, other.m_Forward.Comparer, other.m_Reverse.Comparer) { }

    /// <summary>Adds each pair in <paramref name="pairs" /> to the bimap.</summary>
    /// <param name="pairs">The forward/reverse pairs to add.</param>
    /// <exception cref="ArgumentException">Thrown when a forward or reverse is duplicated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(IEnumerable<KeyValuePair<TForward, TReverse>> pairs)
    {
        foreach (var kv in pairs)
        {
            Add(kv.Key, kv.Value);
        }
    }

    /// <summary>Removes the pair that matches the given forward and reverse.</summary>
    /// <param name="item">The pair to remove; both key and value must match.</param>
    /// <returns><c>true</c> if the pair was removed; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(KeyValuePair<TForward, TReverse> item)
    {
        if (!m_Forward.TryGetValue(item.Key, out var reverse) || !EqualityComparer<TReverse>.Default.Equals(reverse, item.Value)) return false;
        m_Forward.Remove(item.Key);
        m_Reverse.Remove(item.Value);
        return true;
    }

    /// <summary>Returns the reverse associated with <paramref name="forward" />.</summary>
    /// <param name="forward">The forward whose reverse is looked up.</param>
    /// <returns>The reverse paired with <paramref name="forward" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the forward is not present.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TReverse GetReverse(TForward forward) => m_Forward[forward];

    /// <summary>Returns the forward associated with <paramref name="reverse" />.</summary>
    /// <param name="reverse">The reverse whose forward is looked up.</param>
    /// <returns>The forward paired with <paramref name="reverse" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the reverse is not present.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TForward GetForward(TReverse reverse) => m_Reverse[reverse];

    /// <summary>Determines whether <paramref name="forward" /> exists in the bimap.</summary>
    /// <param name="forward">The forward to locate.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsForward(TForward forward) => m_Forward.ContainsKey(forward);

    /// <summary>Determines whether <paramref name="reverse" /> exists in the bimap.</summary>
    /// <param name="reverse">The reverse to locate.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsReverse(TReverse reverse) => m_Reverse.ContainsKey(reverse);

    /// <summary>Attempts to retrieve the reverse associated with <paramref name="forward" />.</summary>
    /// <param name="forward">The forward to look up.</param>
    /// <param name="reverse">When this method returns, contains the paired reverse if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetReverse(TForward forward, out TReverse reverse) => m_Forward.TryGetValue(forward, out reverse);

    /// <summary>Attempts to retrieve the forward associated with <paramref name="reverse" />.</summary>
    /// <param name="reverse">The reverse to look up.</param>
    /// <param name="forward">When this method returns, contains the paired forward if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetForward(TReverse reverse, out TForward forward) => m_Reverse.TryGetValue(reverse, out forward);

    /// <summary>Adds a forward/reverse pair, ensuring neither key is already present.</summary>
    /// <param name="forward">The forward key to add.</param>
    /// <param name="reverse">The reverse value to add.</param>
    /// <exception cref="ArgumentException">Thrown when the forward or reverse already exists.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TForward forward, TReverse reverse)
    {
        if (!m_Forward.TryAdd(forward, reverse))
        {
            throw new ArgumentException("An item with the same forward already exists in the bimap.");
        }

        if (!m_Reverse.TryAdd(reverse, forward))
        {
            m_Forward.Remove(forward);
            throw new ArgumentException("An item with the same reverse already exists in the bimap.");
        }
    }

    /// <summary>Attempts to add a forward/reverse pair without throwing on duplicates.</summary>
    /// <param name="forward">The forward key to add.</param>
    /// <param name="reverse">The reverse value to add.</param>
    /// <returns><c>true</c> if the pair was added; <c>false</c> if either key already exists.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdd(TForward forward, TReverse reverse)
    {
        if (!m_Forward.TryAdd(forward, reverse)) return false;
        if (!m_Reverse.TryAdd(reverse, forward))
        {
            m_Forward.Remove(forward);
            return false;
        }

        return true;
    }

    /// <summary>Removes the pair whose forward matches <paramref name="forward" />.</summary>
    /// <param name="forward">The forward of the pair to remove.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveByForward(TForward forward)
    {
        if (!m_Forward.Remove(forward, out var reverse)) return false;
        if (!m_Reverse.Remove(reverse))
        {
            throw new InvalidOperationException("Inconsistent state: forward was removed but reverse was not found.");
        }

        return true;
    }

    /// <summary>Removes the pair whose reverse matches <paramref name="reverse" />.</summary>
    /// <param name="reverse">The reverse of the pair to remove.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveByReverse(TReverse reverse)
    {
        if (!m_Reverse.Remove(reverse, out var forward)) return false;
        if (!m_Forward.Remove(forward))
        {
            throw new InvalidOperationException("Inconsistent state: reverse was removed but forward was not found.");
        }

        return true;
    }

    /// <summary>Removes the pair whose forward matches <paramref name="forward" />, returning its reverse.</summary>
    /// <param name="forward">The forward of the pair to remove.</param>
    /// <param name="reverse">When this method returns, contains the removed reverse if found; otherwise the default value.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveByForward(TForward forward, out TReverse reverse)
    {
        if (!m_Forward.Remove(forward, out reverse)) return false;
        if (!m_Reverse.Remove(reverse))
        {
            throw new InvalidOperationException("Inconsistent state: forward was removed but reverse was not found.");
        }

        return true;
    }

    /// <summary>Removes the pair whose reverse matches <paramref name="reverse" />, returning its forward.</summary>
    /// <param name="reverse">The reverse of the pair to remove.</param>
    /// <param name="forward">When this method returns, contains the removed forward if found; otherwise the default value.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveByReverse(TReverse reverse, out TForward forward)
    {
        if (!m_Reverse.Remove(reverse, out forward)) return false;
        if (!m_Forward.Remove(forward))
        {
            throw new InvalidOperationException("Inconsistent state: reverse was removed but forward was not found.");
        }

        return true;
    }

    /// <summary>Adds a forward/reverse pair.</summary>
    /// <param name="item">The pair to add.</param>
    /// <exception cref="ArgumentException">Thrown when the forward or reverse already exists.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(KeyValuePair<TForward, TReverse> item) => Add(item.Key, item.Value);

    /// <summary>Removes all forward/reverse pairs from the bimap.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_Forward.Clear();
        m_Reverse.Clear();
    }

    /// <summary>Determines whether the bimap contains the given pair.</summary>
    /// <param name="item">The pair to locate; both key and value must match.</param>
    /// <returns><c>true</c> if the pair is present; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(KeyValuePair<TForward, TReverse> item) => m_Forward.Contains(item);

    /// <summary>Copies the pairs to a compatible array starting at the given index.</summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based index at which copying begins.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(KeyValuePair<TForward, TReverse>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TForward, TReverse>>) m_Forward).CopyTo(array, arrayIndex);

    /// <summary>Returns a compact, read-only view of this bimap backed by minimal perfect hash functions.</summary>
    /// <returns>A <see cref="ReadOnlyBimap{TForward, TReverse}" /> snapshot of this bimap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyBimap<TForward, TReverse> AsReadOnly() => new(this);

    /// <inheritdoc/>
    IReadOnlyBimap<TForward, TReverse> IBimap<TForward, TReverse>.AsReadOnly() => AsReadOnly();

    /// <summary>Returns an enumerator over the forward/reverse pairs.</summary>
    /// <returns>An enumerator for the bimap.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Dictionary<TForward, TReverse>.Enumerator GetEnumerator() => m_Forward.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<KeyValuePair<TForward, TReverse>> IEnumerable<KeyValuePair<TForward, TReverse>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}