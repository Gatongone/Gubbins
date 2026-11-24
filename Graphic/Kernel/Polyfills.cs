#if NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Linq;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// The marker the compiler needs to emit an init only setter, which .NET Standard 2.0 does not ship.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

namespace Gubbins.Graphic.Kernels
{
    /// <summary>
    /// The members a later framework has and the one the generator is built for does not.
    /// </summary>
    internal static class NetStandard20
    {
        /// <summary>
        /// Reports whether a string holds another, with the comparison the caller asked for.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="other">The string to find.</param>
        /// <param name="comparison">How to compare.</param>
        /// <returns><see langword="true"/> when it is there.</returns>
        internal static bool Contains(this string value, string other, StringComparison comparison)
            => value.IndexOf(other, comparison) >= 0;

        /// <summary>
        /// Reports whether a string holds a character.
        /// </summary>
        /// <param name="value">The string to search.</param>
        /// <param name="other">The character to find.</param>
        /// <returns><see langword="true"/> when it is there.</returns>
        internal static bool Contains(this string value, char other) => value.IndexOf(other) >= 0;

        /// <summary>
        /// Collects a sequence into a set.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The sequence.</param>
        /// <param name="comparer">How to compare the elements, or <see langword="null"/> for the default.</param>
        /// <returns>The set.</returns>
        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
            => new(source, comparer);
    }
}
#endif
