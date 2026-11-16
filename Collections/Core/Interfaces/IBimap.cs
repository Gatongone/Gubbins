namespace Gubbins.Collections;

/// <summary>
/// A read-only view of a bijective (one-to-one) map between forward and reverse keys.
///
/// Every forward maps to exactly one reverse and vice versa. Lookups in either direction
/// are O(1). Implementations are immutable; use <see cref="IBimap{TForward, TReverse}" />
/// for a mutable variant.
/// </summary>
/// <typeparam name="TForward">The type of the forward keys.</typeparam>
/// <typeparam name="TReverse">The type of the reverse values.</typeparam>
public interface IReadOnlyBimap<TForward, TReverse> : IReadOnlyCollection<KeyValuePair<TForward, TReverse>>
{
    /// <summary>Gets the forward keys in this bimap.</summary>
    IEnumerable<TForward> Forwards { get; }

    /// <summary>Gets the reverse values in this bimap.</summary>
    IEnumerable<TReverse> Reverses { get; }

    /// <summary>Returns the reverse associated with the given forward.</summary>
    /// <param name="forward">The forward whose reverse is looked up.</param>
    /// <returns>The reverse paired with <paramref name="forward" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the forward is not present.</exception>
    TReverse GetReverse(TForward forward);

    /// <summary>Returns the forward associated with the given reverse.</summary>
    /// <param name="reverse">The reverse whose forward is looked up.</param>
    /// <returns>The forward paired with <paramref name="reverse" />.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the reverse is not present.</exception>
    TForward GetForward(TReverse reverse);

    /// <summary>Determines whether the given forward exists in the bimap.</summary>
    /// <param name="forward">The forward to locate.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    bool ContainsForward(TForward forward);

    /// <summary>Determines whether the given reverse exists in the bimap.</summary>
    /// <param name="reverse">The reverse to locate.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    bool ContainsReverse(TReverse reverse);

    /// <summary>Attempts to retrieve the reverse associated with the given forward.</summary>
    /// <param name="forward">The forward to look up.</param>
    /// <param name="reverse">When this method returns, contains the paired reverse if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the forward is present; otherwise <c>false</c>.</returns>
    bool TryGetReverse(TForward forward, out TReverse reverse);

    /// <summary>Attempts to retrieve the forward associated with the given reverse.</summary>
    /// <param name="reverse">The reverse to look up.</param>
    /// <param name="forward">When this method returns, contains the paired forward if found; otherwise the default value.</param>
    /// <returns><c>true</c> if the reverse is present; otherwise <c>false</c>.</returns>
    bool TryGetForward(TReverse reverse, out TForward forward);
}

/// <summary>
/// A mutable bijective (one-to-one) map between forward and reverse keys.
///
/// Extends <see cref="IReadOnlyBimap{TForward, TReverse}" /> with write operations that
/// maintain the one-to-one invariant: no forward or reverse may appear more than once.
/// </summary>
/// <typeparam name="TForward">The type of the forward keys.</typeparam>
/// <typeparam name="TReverse">The type of the reverse values.</typeparam>
public interface IBimap<TForward, TReverse> : IReadOnlyBimap<TForward, TReverse>, ICollection<KeyValuePair<TForward, TReverse>>
{
    /// <summary>Adds a forward/reverse pair, ensuring neither key is already present.</summary>
    /// <param name="forward">The forward key to add.</param>
    /// <param name="reverse">The reverse value to add.</param>
    /// <exception cref="ArgumentException">Thrown when the forward or reverse already exists.</exception>
    void Add(TForward forward, TReverse reverse);

    /// <summary>Attempts to add a forward/reverse pair without throwing on duplicates.</summary>
    /// <param name="forward">The forward key to add.</param>
    /// <param name="reverse">The reverse value to add.</param>
    /// <returns><c>true</c> if the pair was added; <c>false</c> if either key already exists.</returns>
    bool TryAdd(TForward forward, TReverse reverse);

    /// <summary>Removes the pair whose forward matches the given forward.</summary>
    /// <param name="forward">The forward of the pair to remove.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    bool RemoveByForward(TForward forward);

    /// <summary>Removes the pair whose reverse matches the given reverse.</summary>
    /// <param name="reverse">The reverse of the pair to remove.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    bool RemoveByReverse(TReverse reverse);

    /// <summary>Removes the pair whose forward matches the given forward, returning its reverse.</summary>
    /// <param name="forward">The forward of the pair to remove.</param>
    /// <param name="reverse">When this method returns, contains the removed reverse if found; otherwise the default value.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    bool RemoveByForward(TForward forward, out TReverse reverse);

    /// <summary>Removes the pair whose reverse matches the given reverse, returning its forward.</summary>
    /// <param name="reverse">The reverse of the pair to remove.</param>
    /// <param name="forward">When this method returns, contains the removed forward if found; otherwise the default value.</param>
    /// <returns><c>true</c> if a pair was removed; otherwise <c>false</c>.</returns>
    bool RemoveByReverse(TReverse reverse, out TForward forward);

    /// <summary>Removes all forward/reverse pairs from the bimap.</summary>
    void Clear();

    /// <summary>Returns a compact, read-only view of this bimap.</summary>
    /// <returns>A read-only snapshot of this bimap.</returns>
    IReadOnlyBimap<TForward, TReverse> AsReadOnly();
}
