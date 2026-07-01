using Godot;

namespace Gubbins.Enhance;

/// <summary>
/// A utility class for resolving generic types and their implementations, particularly for use with serialized references in Godot.
/// </summary>
internal static class GenericTypeResolver
{
    /// <summary>
    /// A cache for storing previously resolved constrained implementations to improve performance on repeated lookups.
    /// </summary>
    private static readonly Dictionary<(Type expected, Type arg), List<Type>> s_ConstrainedCache = new();

    /// <summary>
    /// Get all implementations assignable to <paramref name="closedExpected"/>, closing single-parameter generic
    /// definitions with <paramref name="argument"/> so the dropdown offers concrete, type-matched spawners
    /// (e.g. <c>AutoSpawner&lt;Foo&gt;</c>) rather than open definitions requiring a manual type argument.
    /// </summary>
    internal static List<Type> GetConstrainedImplementations(Type closedExpected, Type argument)
    {
        if (s_ConstrainedCache.TryGetValue((closedExpected, argument), out var cached))
            return cached;

        var result = new List<Type>();
        foreach (var type in AssemblyCache.AllTypes)
        {
            if (type == null || type.IsAbstract)
                continue;

            if (type.IsGenericTypeDefinition)
            {
                var closed = TryMakeGeneric(type, argument);
                if (closed != null && IsInstantiableMatch(closed, closedExpected))
                    result.Add(closed);
            }
            else if (!type.ContainsGenericParameters && IsInstantiableMatch(type, closedExpected))
            {
                result.Add(type);
            }
        }

        s_ConstrainedCache[(closedExpected, argument)] = result;
        return result;
    }

    /// <summary>
    /// Extract the expected type T from <see cref="SerializedReference{T}"/> using reflection.
    /// </summary> 
    private static Type TryMakeGeneric(Type openGeneric, Type argument)
    {
        if (openGeneric == null || argument == null || !openGeneric.IsGenericTypeDefinition)
            return null;
        if (openGeneric.GetGenericArguments().Length != 1)
            return null;
        try
        {
            return openGeneric.MakeGenericType(argument);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Check if the type is assignable to the expected type and has a default constructor or is a GodotObject.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="expectedType".">The expected type to match against.</param>
    /// <returns></returns>
    private static bool IsInstantiableMatch(Type type, Type expectedType) => expectedType.IsAssignableFrom(type) && (ContainsDefaultConstructor(type) || typeof(GodotObject).IsAssignableFrom(type));

    /// <summary>
    /// Check if the type has a default constructor.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static bool ContainsDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;
}