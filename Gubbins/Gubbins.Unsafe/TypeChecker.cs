using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Gubbins.Unsafe;

/// <summary>
/// A struct that checks various properties of a type and caches the results for efficient access.
/// It provides information about whether the type is a class, value type, primitive, pointer, enum, abstract, interface, static, number type, or generic.
/// The struct also includes a method to check if a type is unmanaged and caches the results to optimize performance.
/// This can be useful for scenarios where type information needs to be accessed frequently without incurring the overhead of reflection each time.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public struct TypeChecker
{
    /// <summary>
    /// A constant that represents the combination of TypeAttributes for a static class.
    /// </summary>
    private const TypeAttributes ATTRIB_FOR_STATIC_CLASS = TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

    /// <summary>
    /// A static HashSet that caches types that have been determined to be unmanaged.
    /// </summary>
    private static readonly HashSet<Type?> s_UnmanagedTypeCache = [];

    /// <summary>
    /// A readonly boolean that indicates whether the type is a class.
    /// </summary>
    public readonly bool IsClass;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a value type.
    /// </summary>
    public readonly bool IsValueType;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a managed type, which is determined by checking if it is not a primitive, enum, or pointer, and is a value type that is not unmanaged.
    /// </summary>
    public readonly bool IsManagedType;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a primitive type, such as int, float, etc.
    /// </summary>
    public readonly bool IsPrimitive;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a pointer type.
    /// </summary>
    public readonly bool IsPointer;

    /// <summary>
    /// A readonly boolean that indicates whether the type is an enum type.
    /// </summary>
    public readonly bool IsEnum;

    /// <summary>
    /// A readonly boolean that indicates whether the type is an abstract class.
    /// </summary>
    public readonly bool IsAbstract;

    /// <summary>
    /// A readonly boolean that indicates whether the type is an interface.
    /// </summary>
    public readonly bool IsInterface;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a static class, which is determined by checking
    /// if the type's attributes match the combination of TypeAttributes for a static class.
    /// </summary>
    public readonly bool IsStatic;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a number type, which is determined by checking
    /// if it is a primitive type and matches one of the common numeric types (byte, sbyte, int, uint, long, ulong, double, float, decimal).
    /// </summary>
    public readonly bool IsNumberType;

    /// <summary>
    /// A readonly boolean that indicates whether the type is a generic type.
    /// </summary>
    public readonly bool IsGeneric;

    /// <summary>
    /// Initializes a new instance of the TypeChecker struct by checking various properties of
    /// the provided type and caching the results in readonly fields for efficient access.
    /// </summary>
    /// <param name="type"></param>
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
        IsNumberType = IsPrimitive && (
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

    /// <summary>
    /// Checks if the provided type is newable, which means it has a public parameterless constructor.
    /// </summary>
    /// <param name="type">The type to check for newability. </param>
    /// <returns>True if the type is newable; otherwise, false.</returns>
    private static bool CheckIsNewable(Type type)
    {
        var ctor = type.GetConstructor([]);
        return ctor != null && ctor.IsPublic;
    }

    /// <summary>
    /// Checks if the provided type is unmanaged, which means it is a value type that does not contain any reference type fields.
    /// </summary>
    /// <param name="type">The type to check for being unmanaged. </param>
    /// <returns>True if the type is unmanaged; otherwise, false.</returns>
    private static bool CheckIsTypeUnmanaged(Type type)
    {
        if (s_UnmanagedTypeCache.Contains(type) || type.IsPrimitive || type.IsPointer || type.IsEnum)
            return true;
        if (!type.IsValueType)
            return false;

        // A type is considered unmanaged if it is a value type and all of its fields are either of the same type (to allow for recursive structs) or are unmanaged types.
        var isUnmanaged = !type.GetFields().Any(f => f.FieldType != type && !CheckIsTypeUnmanaged(f.FieldType));

        if (isUnmanaged)
            s_UnmanagedTypeCache.Add(type);
        return isUnmanaged;
    }
}