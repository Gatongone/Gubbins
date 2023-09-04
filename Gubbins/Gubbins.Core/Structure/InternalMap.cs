/*
 * Copyright ©2022 Gatongone
 * Author: Gatongone
 * Email: gatongone@gmail.com
 * Created On: 2023/09/03-04:55:30
 * Github: https://github.com/Gatongone
 * Description: Static type map.
 */
namespace Gubbins.Structure;

/// <summary>
/// A static class that provides a generic mapping functionality for storing and retrieving values based on type keys.
/// </summary>
public static class InternalMap
{
    /// <summary>
    /// A nested generic class that stores the mapped value for a specific type key.
    /// </summary>
    /// <typeparam name="TTypeKey">The type of the key.</typeparam>
    /// <typeparam name="TTypeValue">The type of the value.</typeparam>
    private static class TypeStore<TTypeKey, TTypeValue> where TTypeValue : new()
    {
        internal static TTypeValue Value = new();
    }

    /// <summary>
    /// Retrieves the mapped value based on the provided type key.
    /// </summary>
    /// <typeparam name="TTypeKey">The type of the key.</typeparam>
    /// <typeparam name="TTypeValue">The type of the value.</typeparam>
    /// <returns>The mapped value.</returns>
    public static TTypeValue Get<TTypeKey, TTypeValue>() where TTypeValue : new()
        => TypeStore<TTypeKey, TTypeValue>.Value;

    /// <summary>
    /// Sets the mapped value for the provided type key.
    /// </summary>
    /// <typeparam name="TTypeKey">The type of the key.</typeparam>
    /// <typeparam name="TTypeValue">The type of the value.</typeparam>
    /// <param name="value">The value to be set.</param>
    public static void Set<TTypeKey, TTypeValue>(TTypeValue value) where TTypeValue : new()
        => TypeStore<TTypeKey, TTypeValue>.Value = value;
}