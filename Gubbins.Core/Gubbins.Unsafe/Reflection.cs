using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gubbins.Unsafe;

internal static class Reflection
{
    /// <summary>
    /// Loads an assembly by its name. This method is used as the assembly resolver callback for type resolution,
    /// allowing the system to load assemblies on demand when resolving types that may not be currently loaded in the AppDomain.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load, provided as an AssemblyName object.</param>
    /// <returns>The loaded Assembly object if the assembly is successfully loaded; otherwise, null if the assembly cannot be found or loaded.</returns>
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
            var pool = ListPool<Type>.Default;
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
            var argStrings = ListPool<string>.Default.Spawn();
            ParseGenericArguments(argsString, argStrings);

            // Recursively parse each argument
            var argTypes = argStrings.Select(ParseFriendlyTypeName).ToArray();
            ListPool<string>.Default.Recycle(argStrings);

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

        // Parses a string of generic arguments, correctly handling nested generics, and adds each argument to the provided list.
        static void ParseGenericArguments(string argsString, List<string> args)
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

        // Maps common system types to their C# alias names for improved type resolution when friendly names are used.
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

/// <summary>
/// Caches reflection metadata and resolved <see cref="ValuableMember"/> instances for fast repeated access.
/// </summary>
internal static class ReflectionCache
{
    /// <summary>
    /// Stores per-type checker instances.
    /// </summary>
    private static readonly Dictionary<Type, TypeChecker> s_CheckerMaps = new();

    /// <summary>
    /// Stores cached reflected members by declaring type and member name.
    /// </summary>
    private static readonly Dictionary<Type, Dictionary<string, ValuableMember>> s_CacheMaps = new();

    /// <summary>
    /// Default binding flags used when resolving instance fields and properties.
    /// </summary>
    private const BindingFlags DEFAULT_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Gets a cached <see cref="TypeChecker"/> for the specified type, creating one if needed.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The cached or newly created <see cref="TypeChecker"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TypeChecker CheckType(this Type type)
    {
        if (s_CheckerMaps.TryGetValue(type, out var checker))
            return checker;
        checker = new TypeChecker(type);
        s_CheckerMaps.Add(type, checker);
        return checker;
    }

    /// <summary>
    /// Gets a cached reflected member by name, checking fields first and then properties.
    /// </summary>
    /// <param name="type">The declaring type.</param>
    /// <param name="name">The member name.</param>
    /// <returns>The cached <see cref="ValuableMember"/>, or <see langword="null"/> if not found.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValuableMember? GetValuableMember(this Type type, string name)
    {
        var result = GetField(type, name, DEFAULT_BINDING_FLAGS);
        return result ?? GetProperty(type, name, DEFAULT_BINDING_FLAGS);
    }

    /// <summary>
    /// Gets a cached property wrapper for the specified type and name, creating and caching it if necessary.
    /// </summary>
    /// <param name="type">The declaring type.</param>
    /// <param name="name">The property name.</param>
    /// <param name="flags">The binding flags used for lookup.</param>
    /// <returns>The cached property wrapper, or <see langword="null"/> if the property does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValuableMember? GetProperty(Type type, string name, BindingFlags flags)
    {
        if (!s_CacheMaps.TryGetValue(type, out var memberMaps))
        {
            memberMaps = new Dictionary<string, ValuableMember>();
            s_CacheMaps.Add(type, memberMaps);
        }

        if (memberMaps.TryGetValue(name, out var unsafeProperty))
            return unsafeProperty;

        var propertyInfo = type.GetProperty(name, flags);
        if (propertyInfo == null)
            return null;
        unsafeProperty = new ValuableMember(propertyInfo);
        memberMaps.Add(name, unsafeProperty);

        return unsafeProperty;
    }

    /// <summary>
    /// Gets a cached field wrapper for the specified type and name, creating and caching it if necessary.
    /// </summary>
    /// <param name="type">The declaring type.</param>
    /// <param name="name">The field name.</param>
    /// <param name="flags">The binding flags used for lookup.</param>
    /// <returns>The cached field wrapper, or <see langword="null"/> if the field does not exist.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ValuableMember? GetField(Type type, string name, BindingFlags flags)
    {
        if (!s_CacheMaps.TryGetValue(type, out var memberMaps))
        {
            memberMaps = new Dictionary<string, ValuableMember>();
            s_CacheMaps.Add(type, memberMaps);
        }

        if (memberMaps.TryGetValue(name, out var unsafeField))
            return unsafeField;

        var fieldInfo = type.GetField(name, flags);
        if (fieldInfo == null)
            return null;
        unsafeField = new ValuableMember(fieldInfo);
        memberMaps.Add(name, unsafeField);

        return unsafeField;
    }
}

/// <summary>
/// Provides helpers for creating strongly typed delegates from reflected property accessors.
/// </summary>
public static class ReflectionExtensions
{
    /// <summary>
    /// The open generic type name used for generated <see cref="Func{TResult}"/>-style delegates.
    /// </summary>
    private const string SYSTEM_FUNC_NAME = "System.Func";

    /// <summary>
    /// The open generic type name used for generated <see cref="Action"/>-style delegates.
    /// </summary>
    private const string SYSTEM_ACTION_NAME = "System.Action";

    /// <summary>
    /// Creates a delegate for the property's set accessor.
    /// </summary>
    /// <param name="property">The property whose setter should be converted to a delegate.</param>
    /// <returns>The created delegate, or <see langword="null"/> if no compatible delegate type can be resolved.</returns>
    public static Delegate? CreateSetDelegate(this PropertyInfo property)
    {
        return CreateGetSetDelegate(property.SetMethod);
    }

    /// <summary>
    /// Creates a delegate for the property's get accessor.
    /// </summary>
    /// <param name="property">The property whose getter should be converted to a delegate.</param>
    /// <returns>The created delegate, or <see langword="null"/> if no compatible delegate type can be resolved.</returns>
    public static Delegate? CreateGetDelegate(this PropertyInfo property)
    {
        return CreateGetSetDelegate(property.GetMethod);
    }

    /// <summary>
    /// Creates a delegate matching the supplied accessor signature, including special handling for
    /// instance methods declared on struct and class sources.
    /// </summary>
    /// <param name="method">The reflected accessor method.</param>
    /// <returns>The created delegate, or <see langword="null"/> if the delegate type cannot be found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="method"/> contains open generic parameters.</exception>
    private static Delegate? CreateGetSetDelegate(MethodInfo method)
    {
        var sourceType = method.DeclaringType;

        if (method.ContainsGenericParameters)
            throw new ArgumentException("Can't not create delegate from the method with generic parameters.");

        var sourceTypeChecker = sourceType!.CheckType();
        var isValueType = sourceType is {IsValueType: true};
        var isVoidReturn = method.ReturnType == typeof(void);
        var isStatic = sourceTypeChecker.IsStatic || method.IsStatic;
        var genericTypes = ListPool<Type>.Default.Spawn();
        string typeName;

        //Add source type
        if (!isStatic)
        {
            genericTypes.Add(sourceType!);
            if (!isValueType)
                typeName = isVoidReturn ? MethodWithoutReturnValue.CLASS_METHOD_NAME : MethodWithReturnValue.CLASS_METHOD_NAME;
            else
                typeName = isVoidReturn ? MethodWithoutReturnValue.STRUCT_METHOD_NAME : MethodWithReturnValue.STRUCT_METHOD_NAME;
        }
        else
        {
            typeName = isVoidReturn ? SYSTEM_ACTION_NAME : SYSTEM_FUNC_NAME;
        }

        //Add parameters
        genericTypes.AddRange(method.GetParameters().Select(static parameter => parameter.ParameterType));

        //Add return
        if (!isVoidReturn)
            genericTypes.Add(method.ReturnType);

        var delegateType = Reflection.GetType(typeName, genericTypes.ToArray());
        ListPool<Type>.Default.Recycle(genericTypes);
        return delegateType != null ? method.CreateDelegate(delegateType) : null;
    }
}

file class ListPool<T>
{
    public static readonly ListPool<T> Default = new();

    public readonly ConcurrentQueue<List<T>> m_Queue = new();

    public List<T> Spawn() => m_Queue.TryDequeue(out var list) ? list : [];

    public void Recycle(List<T> list)
    {
        list.Clear();
        m_Queue.Enqueue(list);
    }
}