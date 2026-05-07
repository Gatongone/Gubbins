using System.Reflection;
using System.Runtime.CompilerServices;

namespace Gubbins.Enhance;

internal static class ReflectionCache
{
    private static readonly Dictionary<Type, TypeChecker> s_CheckerMaps = new();
    private static readonly Dictionary<Type, Dictionary<string, ValuableMember>> s_CacheMaps = new();
    private const BindingFlags DEFAULT_BINDING_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TypeChecker CheckType(this Type type)
    {
        if (s_CheckerMaps.TryGetValue(type, out var checker))
            return checker;
        checker = new TypeChecker(type);
        s_CheckerMaps.Add(type, checker);
        return checker;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValuableMember? GetValuableMember(this Type type, string name)
    {
        var result = GetField(type, name, DEFAULT_BINDING_FLAGS);
        return result ?? GetProperty(type, name, DEFAULT_BINDING_FLAGS);
    }

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