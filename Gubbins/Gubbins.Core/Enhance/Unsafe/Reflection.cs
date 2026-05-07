using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gubbins.Enhance;

internal static class Reflection
{
    internal static Assembly? LoadAssemblyResolver(AssemblyName assemblyName) => Assembly.Load(assemblyName);

    /// <summary>
    /// Get the type from the domain assemblies.
    /// </summary>
    /// <param name="assembly">Assembly from assemblyResolver.</param>
    /// <param name="typeName">Type name.</param>
    /// <param name="ignoreCase">Ignore case when searching for type.</param>
    /// <returns>Type from the domain assemblies or null if not found.</returns>
    /// <exception cref="ArgumentNullException">AssemblyName cannot be null or empty.</exception>
    /// <exception cref="TypeLoadException">Assembly was not found.</exception>
    internal static Type? DomainTypeResolver(Assembly? assembly, string typeName, bool ignoreCase)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentNullException(nameof(typeName));
        }

        typeName = typeName.Trim();

        // If it contains '<', assume it's the friendly generic format
        if (typeName.Contains('<'))
        {
            return ParseFriendlyTypeName(typeName);
        }

        // Fall back to the original cross-assembly resolution for standard formats
        Type result;
        if (assembly == null)
        {
            var pool = Pool<List<Type>>.Default;
            var genericArgs = pool.Spawn();
            foreach (var assem in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (TryGetType(assem, typeName, ignoreCase, out result))
                {
                    return result;
                }
            }

            pool.Recycle(genericArgs);
            return null;
        }
        TryGetType(assembly, typeName, ignoreCase, out result);
        return result;

        static bool TryGetType(Assembly assembly, string typeName, bool ignoreCase, out Type result)
        {
            result = assembly.GetType(typeName, ignoreCase);
            return result != null;
        }

        static Type? ParseFriendlyTypeName(string typeName)
        {
            var genericStart = typeName.IndexOf('<');
            if (genericStart == -1)
            {
                // Non-generic type
                if (TryGetTypeAlias(typeName, out var aliasedName))
                {
                    typeName = aliasedName;
                }

                // Recursive call but without '<' so it uses the standard path
                return Type.GetType(typeName, LoadAssemblyResolver, DomainTypeResolver);
            }

            var baseName = typeName.Substring(0, genericStart).Trim();
            var argsString = typeName.Substring(genericStart + 1, typeName.Length - genericStart - 2).Trim();

            // Parse the generic arguments, handling nesting
            var argStrings = Pool<List<string>>.Default.Spawn();
            ParseGenericArguments(argsString, argStrings);

            // Recursively parse each argument
            var argTypes = argStrings.Select(ParseFriendlyTypeName).ToArray();
            Pool<List<string>>.Default.Recycle(argStrings);

            // Construct the open generic type name
            var openGenericName = $"{baseName}`{argTypes.Length}";

            // Get the open generic type using the standard GetTypeEx
            var openType = Type.GetType(typeName, LoadAssemblyResolver, DomainTypeResolver);
            if (openType == null)
            {
                throw new TypeLoadException($"Could not load type '{openGenericName}'");
            }

            // Construct the closed generic type
            return openType.MakeGenericType(argTypes);
        }

        static void ParseGenericArguments(string argsString,  List<string> args)
        {
            var start = 0;
            var depth = 0;

            for (var i = 0; i < argsString.Length; i++)
            {
                var c = argsString[i];
                switch (c)
                {
                    case '<':
                        depth++;
                        break;
                    case '>':
                        depth--;
                        break;
                    case ',' when depth == 0:
                    {
                        var arg = argsString.Substring(start, i - start).Trim();
                        if (!string.IsNullOrEmpty(arg))
                        {
                            args.Add(arg);
                        }

                        start = i + 1;
                        break;
                    }
                }
            }

            var lastArg = argsString[start..].Trim();
            if (!string.IsNullOrEmpty(lastArg))
            {
                args.Add(lastArg);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryGetTypeAlias(string typeName, out string aliasedName)
        {
            switch (typeName)
            {
                case "System.Boolean":
                    aliasedName = "bool";
                    return true;
                case "System.Int16":
                    aliasedName = "short";
                    return true;
                case "System.Int32":
                    aliasedName = "int";
                    return true;
                case "System.Int64":
                    aliasedName = "long";
                    return true;
                case "System.UInt16":
                    aliasedName = "ushort";
                    return true;
                case "System.UInt32":
                    aliasedName = "uint";
                    return true;
                case "System.UInt64":
                    aliasedName = "ulong";
                    return true;
                case "System.Single":
                    aliasedName = "float";
                    return true;
                case "System.Double":
                    aliasedName = "double";
                    return true;
                case "System.Decimal":
                    aliasedName = "decimal";
                    return true;
                case "System.String":
                    aliasedName = "string";
                    return true;
                case "System.Object":
                    aliasedName = "object";
                    return true;
                case "System.Byte":
                    aliasedName = "byte";
                    return true;
                case "System.SByte":
                    aliasedName = "sbyte";
                    return true;
                case "System.Char":
                    aliasedName = "char";
                    return true;
                case "System.Void":
                    aliasedName = "void";
                    return true;
                default:
                    aliasedName = string.Empty;
                    return false;
            }
        }
    }

    /// <summary>
    /// Get generic type from a type name with generic arguments.
    /// </summary>
    /// <param name="typeName">Type name.</param>
    /// <param name="genericParameters">Generic arguments.</param>
    /// <returns>The generic type.</returns>
    internal static Type? GetType(string typeName, params Type[] genericParameters)
    {
        if (genericParameters.Length == 0)
            return Type.GetType(typeName);

        if (genericParameters.Length > 0)
        {
            var nameBuilder = new StringBuilder();
            var genericStrs = string.Join(",", genericParameters.Select(genericParameter => $"[{genericParameter.AssemblyQualifiedName}]").ToArray());
            nameBuilder.Append($"{typeName}`{genericParameters.Length}[");
            nameBuilder.Append(genericStrs);
            nameBuilder.Append(']');
            return Type.GetType(nameBuilder.ToString());
        }

        return Type.GetType(typeName);
    }
}