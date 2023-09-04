/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/09/03-04:43:55
 * Github: https://github.com/Gatongone
 * Description: Internal singleton cache.
 */

namespace Gubbins.Structure;

/// <summary>
/// Represents an internal class that provides a generic implementation of a singleton pattern.
/// </summary>
internal static class InternalSingleton
{
    /// <summary>
    /// A nested generic class that stores the instance of the singleton.
    /// </summary>
    /// <typeparam name="TTypeValue">The type of the singleton instance.</typeparam>
    private static class TypeStore<TTypeValue> where TTypeValue : new()
    {
        internal static readonly TTypeValue Value = new();
    }

    /// <summary>
    /// Retrieves the instance of the singleton based on the provided type.
    /// </summary>
    /// <typeparam name="TTypeValue">The type of the singleton instance.</typeparam>
    /// <returns>The instance of the singleton.</returns>
    internal static TTypeValue InstanceOf<TTypeValue>() where TTypeValue : new()
        => TypeStore<TTypeValue>.Value;
}