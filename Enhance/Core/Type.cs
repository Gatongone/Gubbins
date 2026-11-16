namespace Gubbins.Enhance;

/// <summary>
/// Extensions for <see cref="System.Type"/>.
/// </summary>
public static class TypeExtensions
{
    private static readonly Dictionary<Type, string> s_TypeAliases = new()
    {
        [typeof(bool)] = "bool",
        [typeof(byte)] = "byte",
        [typeof(sbyte)] = "sbyte",
        [typeof(short)] = "short",
        [typeof(ushort)] = "ushort",
        [typeof(int)] = "int",
        [typeof(uint)] = "uint",
        [typeof(long)] = "long",
        [typeof(ulong)] = "ulong",
        [typeof(float)] = "float",
        [typeof(double)] = "double",
        [typeof(decimal)] = "decimal",
        [typeof(char)] = "char",
        [typeof(string)] = "string",
        [typeof(object)] = "object",
        [typeof(void)] = "void"
    };

    /// <summary>
    /// Gets the canonical, namespace-qualified display name for a type.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>A C#-style type name with nested types separated by <c>.</c> and generic arguments in angle brackets.</returns>
    public static string GetDisplayName(this Type type)
    {
        if (s_TypeAliases.TryGetValue(type, out var alias)) return alias;

        if (type.IsArray) return $"{type.GetElementType()!.GetDisplayName()}[]";

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var name = definition.FullName ?? definition.Name;
            var arity = name.IndexOf('`');
            if (arity >= 0) name = name[..arity];
            name = name.Replace('+', '.');

            var arguments = type.GetGenericArguments().Select(GetDisplayName);
            return $"{name}<{string.Join(", ", arguments)}>";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }
}
