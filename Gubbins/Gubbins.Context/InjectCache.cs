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
    /// InjectMember cache.
    /// </summary>
    private static readonly Dictionary<Type?, InjectMember[]> s_Cache = new();

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
            var field = type.GetField($"<{property}>k__BackingField", MEMBER_BINDING_FLAGS);

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
}