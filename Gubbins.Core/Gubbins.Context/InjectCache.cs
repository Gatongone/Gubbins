using System.Reflection;
using System.Runtime.CompilerServices;
using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// The field offset or property method delegate with InjectAttribute cache.
/// </summary>
internal static class InjectCache
{
    /// <summary>
    /// Default inject binding flags.
    /// </summary>
    private const BindingFlags MEMBER_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>
    /// Inject method binding flags. Declared-only so the base-type recursion in the resolver invokes each method exactly once.
    /// </summary>
    private const BindingFlags METHOD_BINDING_FLAGS = MEMBER_BINDING_FLAGS | BindingFlags.DeclaredOnly;

    /// <summary>
    /// InjectMember cache.
    /// </summary>
    private static readonly Dictionary<Type?, InjectMember[]> s_Cache = new();

    /// <summary>
    /// Inject method cache, keyed by declaring type.
    /// </summary>
    private static readonly Dictionary<Type, InjectMethod[]> s_MethodCache = new();

    /// <summary>
    /// Inject constructor cache. A null value means the type has no usable constructor.
    /// </summary>
    private static readonly Dictionary<Type, InjectConstructor?> s_ConstructorCache = new();

    /// <summary>
    /// Get <see cref="InjectMember"/> with <inheritdoc cref="InjectAttribute"/>.
    /// </summary>
    /// <param name="type">The type need to be resolved.</param>
    /// <returns>ValuableMembers of <c>type</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static InjectMember[] GetInjectMembers(Type type)
    {
        if (type == null)
        {
            throw new NullReferenceException("Got a null type.");
        }
        // Try get result from cache.
        if (s_Cache.TryGetValue(type, out var results))
        {
            return results;
        }

        var pool = Pool<List<InjectMember>>.Default;
        var valuableMembers = pool.Spawn();
        // Append all properties and fields with InjectAttribute into cache.
        AppendFields(type, valuableMembers);
        AppendProperties(type, valuableMembers);
        results = valuableMembers.ToArray();
        pool.Recycle(valuableMembers);
        s_Cache.Add(type, results);
        return results;
    }

    /// <summary>
    /// Get the <see cref="InjectMethod"/>s declared on <paramref name="type"/> (not inherited) that carry an <see cref="InjectAttribute"/>.
    /// </summary>
    /// <param name="type">The type whose declared methods are scanned.</param>
    /// <returns>The inject methods declared on <paramref name="type"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static InjectMethod[] GetInjectMethods(Type type)
    {
        if (s_MethodCache.TryGetValue(type, out var results))
        {
            return results;
        }

        var list = new List<InjectMethod>();
        foreach (var method in type.GetMethods(METHOD_BINDING_FLAGS))
        {
            if (method.GetCustomAttribute<InjectAttribute>() is null)
            {
                continue;
            }

            list.Add(new InjectMethod(method, BuildParameters(method.GetParameters())));
        }

        results = list.ToArray();
        s_MethodCache.Add(type, results);
        return results;
    }

    /// <summary>
    /// Get the constructor used for constructor injection: the one marked with <see cref="InjectAttribute"/>,
    /// otherwise the public constructor with the most parameters.
    /// </summary>
    /// <param name="type">The type to construct.</param>
    /// <returns>The chosen constructor with its parameters, or <see langword="null"/> when none is available.</returns>
    /// <exception cref="ArgumentException">Thrown when more than one constructor is marked with <see cref="InjectAttribute"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static InjectConstructor? GetInjectConstructor(Type type)
    {
        if (s_ConstructorCache.TryGetValue(type, out var cached))
        {
            return cached;
        }

        // Prefer an [Inject]-marked constructor (public or non-public).
        ConstructorInfo? chosen = null;
        foreach (var constructor in type.GetConstructors(MEMBER_BINDING_FLAGS))
        {
            if (constructor.GetCustomAttribute<InjectAttribute>() is null)
            {
                continue;
            }

            if (chosen != null)
            {
                throw new ArgumentException($"Multiple [Inject] constructors found on type: \"{type}\".");
            }

            chosen = constructor;
        }

        // Otherwise pick the greediest public constructor.
        if (chosen == null)
        {
            foreach (var constructor in type.GetConstructors())
            {
                if (chosen == null || constructor.GetParameters().Length > chosen.GetParameters().Length)
                {
                    chosen = constructor;
                }
            }
        }

        var result = chosen == null ? null : new InjectConstructor(chosen, BuildParameters(chosen.GetParameters()));
        s_ConstructorCache.Add(type, result);
        return result;
    }

    /// <summary>
    /// Build the inject parameter descriptors for a method or constructor, reading the optional per-parameter key.
    /// </summary>
    /// <param name="parameters">The reflected parameters.</param>
    /// <returns>The inject parameter descriptors.</returns>
    private static InjectParameter[] BuildParameters(ParameterInfo[] parameters)
    {
        if (parameters.Length == 0)
        {
            return [];
        }

        var result = new InjectParameter[parameters.Length];
        for (var index = 0; index < parameters.Length; index++)
        {
            var key = parameters[index].GetCustomAttribute<InjectAttribute>()?.Key;
            result[index] = new InjectParameter(parameters[index].ParameterType, key);
        }

        return result;
    }

    /// <summary>
    /// Append a <see cref="ValuableMember"/> that property with <inheritdoc cref="InjectAttribute"/> to cache.
    /// </summary>
    /// <param name="type">The type need to be resolved.</param>
    /// <param name="cache">Cache of the <see cref="InjectMember"/>.</param>
    /// <exception cref="ArgumentException">Throw when the property type is value type or it doesn't have a set method.</exception>
    private static void AppendProperties(Type type, List<InjectMember> cache)
    {
        foreach (var property in type.GetProperties(MEMBER_BINDING_FLAGS))
        {
            // Try get InjectAttribute.
            if (property.GetCustomAttributes().FirstOrDefault(static attribute => attribute is InjectAttribute) is not InjectAttribute injectAttribute)
            {
                continue;
            }

            // Type validation.
            if (property.SetMethod == null)
            {
                throw new ArgumentException("The Inject property does not have a set method.");
            }

            if (property.PropertyType.CheckType().IsValueType)
            {
                throw new ArgumentException("The Inject member can only be reference type.");
            }

            // Field accessing is faster.
            var field = type.GetField($"<{property.Name}>k__BackingField", MEMBER_BINDING_FLAGS);

            // Append to cache.
            cache.Add(new InjectMember(injectAttribute.Key, field == null ? new ValuableMember(property) : new ValuableMember(field)));
        }
    }

    /// <summary>
    /// Append a <see cref="ValuableMember"/> that field with <inheritdoc cref="InjectAttribute"/> to cache.
    /// </summary>
    /// <param name="type">The type need to be resolved.</param>
    /// <param name="cache">Cache of the <see cref="InjectMember"/>.</param>
    /// <exception cref="ArgumentException">Throw when the field type is value type.</exception>
    private static void AppendFields(Type type, List<InjectMember> cache)
    {
        foreach (var field in type.GetFields(MEMBER_BINDING_FLAGS))
        {
            // Try get InjectAttribute.
            if (field.GetCustomAttributes().FirstOrDefault(static attribute => attribute is InjectAttribute) is not InjectAttribute injectAttribute)
            {
                continue;
            }

            // Type validation.
            if (field.FieldType.CheckType().IsValueType)
            {
                throw new ArgumentException("The Inject member can only be reference type.");
            }

            // Append to cache.
            cache.Add(new InjectMember(injectAttribute.Key, new ValuableMember(field)));
        }
    }

    internal readonly struct InjectMember(string? key, ValuableMember member)
    {
        public readonly string?        Key    = key;
        public readonly ValuableMember Member = member;
    }

    /// <summary>
    /// A single injectable parameter of a constructor or method: its type and an optional resolution key.
    /// </summary>
    internal readonly struct InjectParameter(Type type, string? key)
    {
        public readonly Type    Type = type;
        public readonly string? Key  = key;
    }

    /// <summary>
    /// A method marked with <see cref="InjectAttribute"/>, invoked after construction with resolved parameters.
    /// </summary>
    internal readonly struct InjectMethod(MethodInfo method, InjectParameter[] parameters)
    {
        public readonly MethodInfo        Method     = method;
        public readonly InjectParameter[] Parameters = parameters;
    }

    /// <summary>
    /// The constructor chosen for constructor injection together with its parameters.
    /// </summary>
    internal sealed class InjectConstructor(ConstructorInfo constructor, InjectParameter[] parameters)
    {
        public readonly ConstructorInfo   Constructor = constructor;
        public readonly InjectParameter[] Parameters  = parameters;
    }
}