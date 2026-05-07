using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Gubbins.Enhance;

[SuppressMessage("ReSharper", "UnusedType.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public struct TypeChecker
{
    private const TypeAttributes ATTRIB_FOR_STATIC_CLASS = TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

    private static readonly HashSet<Type?> s_UnmanagedTypeCache = [];
    public readonly         bool           IsClass;
    public readonly         bool           IsValueType;
    public readonly         bool           IsManagedType;
    public readonly         bool           IsPrimitive;
    public readonly         bool           IsPointer;
    public readonly         bool           IsEnum;
    public readonly         bool           IsAbstract;
    public readonly         bool           IsInterface;
    public readonly         bool           IsStatic;
    public readonly         bool           IsNumberType;
    public readonly         bool           IsGeneric;

    public TypeChecker(Type type)
    {
        IsClass       = type.IsClass;
        IsValueType   = type.IsValueType;
        IsPrimitive   = type.IsPrimitive;
        IsPointer     = type.IsPointer;
        IsEnum        = type.IsEnum;
        IsAbstract    = type.IsAbstract;
        IsInterface   = type.IsInterface;
        IsManagedType = !IsPrimitive && !IsEnum && !IsPointer && IsValueType && !CheckIsTypeUnmanaged(type);
        IsStatic      = (ATTRIB_FOR_STATIC_CLASS & type.Attributes) == type.Attributes || (ATTRIB_FOR_STATIC_CLASS & type.Attributes) == ATTRIB_FOR_STATIC_CLASS;
        IsGeneric     = type.IsGenericType;
        IsNumberType  = IsPrimitive && (
            type == typeof(byte) ||
            type == typeof(sbyte) ||
            type == typeof(int) ||
            type == typeof(uint) ||
            type == typeof(long) ||
            type == typeof(ulong) ||
            type == typeof(double) ||
            type == typeof(float) ||
            type == typeof(decimal));
    }

    private static bool CheckIsNewable(Type type)
    {
        var ctor = type.GetConstructor([]);
        return ctor != null && ctor.IsPublic;
    }

    private static bool CheckIsTypeUnmanaged(Type type)
    {
        if (s_UnmanagedTypeCache.Contains(type) || type.IsPrimitive || type.IsPointer || type.IsEnum)
            return true;
        if (!type.IsValueType)
            return false;
        var isUnmanaged = !type.GetFields().Any(f => f.FieldType != type && !CheckIsTypeUnmanaged(f.FieldType));

        if (isUnmanaged)
            s_UnmanagedTypeCache.Add(type);
        return isUnmanaged;
    }
}