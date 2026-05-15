using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Unsafe;

/// <summary>
/// Provides common unsafe pointer operation.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed unsafe class Native
{
    private const int POINTER_VACANCIES = 8;

    /// <summary>
    /// Unsafe operation base on different environment.
    /// </summary>
    internal static Memory Operation = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr Allocate(int size, int align = 8) => Operation.Allocate(size, align);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(IntPtr ptr) => Operation.Free(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAlignment<T>() => GetAlignment((int) GetStackSize<T>());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAlignment(int size)
    {
        if ((size & 7) == 0)
        {
            return 8;
        }
        if ((size & 3) == 0)
        {
            return 4;
        }
        return (size & 1) == 0 ? 2 : 1;
    }

    /// <summary>
    /// Cast a struct to another struct.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo Cast<TFrom, TTo>(ref TFrom src) where TFrom : struct where TTo : struct
    {
        fixed (TFrom* pSource = &src)
        {
            return ref *(TTo*) pSource;
        }
    }

    /// <summary>
    /// Cast a object to another struct.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo Cast<TTo>(object src) => ref *(TTo*) &src;

    /// <summary>
    /// Cast type for each bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo BitCast<TFrom, TTo>(TFrom source) where TFrom : struct where TTo : struct => Cast<byte, TTo>(ref Cast<TFrom, byte>(ref source));

    /// <summary>
    /// Get field offset of structure or class.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetFieldOffset(FieldInfo fieldInfo) => Operation.GetFieldOffset(fieldInfo);

    /// <summary>
    /// Make the object as pinning reference.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void* AsReference<T>(ref T value)
    {
        if (typeof(T).CheckType().IsValueType)
        {
            fixed (void* ptr = &value)
            {
                return ptr;
            }
        }

        fixed (void* ptr = &value)
        {
            return (void**) ptr;
        }
    }

    /// <summary>
    /// Get the address of the array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(T[] value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Get the address of the array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(Span<T> value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Get the address of the array.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(ReadOnlySpan<T> value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Get the address of the structure.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetAddress<T>(ref T structure) where T : struct
    {
        fixed (void* ptr = &structure)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Get the address of the object.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetAddress(object instance) => *(void**) &instance;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T AddByteOffset<T>(ref T source, int byteOffset) where T : struct
    {
        fixed (void* ptr = &source)
        {
            var bytePtr = (byte*) ptr;
            var a = bytePtr + byteOffset;
            return ref AsRef<T>(a);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValuableMember? GetValuableMember(Type sourceType, string name)
    {
        var typeChecker = sourceType.CheckType();
        if (typeChecker.IsInterface || typeChecker.IsAbstract || typeChecker.IsStatic)
            throw new ArgumentException("Can't get the valuable member from a abstract or static class.");
        return sourceType.GetValuableMember(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetStackSize(Type type) => Operation.GetStackSize(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetStackSize<T>() => GetStackSize(typeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyMemory(void* source, void* destination, uint offset) => Operation.CopyMemory(source, destination, offset);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(void* address, Type sourceType, uint offset)
    {
        var valueObj = FormatterServices.GetUninitializedObject(sourceType);
        var add = GetAddress(valueObj);
        var handlePtr = (IntPtr*) add;
        CopyMemory(address, handlePtr + POINTER_VACANCIES, offset);
        return valueObj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Unbox(object source) => (byte*) GetAddress(source) + IntPtr.Size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(void* ptr) where T : struct => *(T*) ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TResult AsRef<TResult>(void* ptr) => ref *(TResult*) ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TResult AsRef<TFrom, TResult>(Span<TFrom> span)
    {
        fixed (void* p = span)
        {
            return ref AsRef<TResult>(p);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetValue(void* source, Type type)
    {
        var typeCode = Type.GetTypeCode(type);
        var isValueType = type.CheckType().IsValueType;
        var size = GetStackSize(type);
        return GetValueInternal(source, typeCode, isValueType, size, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object GetValueInternal(void* source, TypeCode typeCode, bool isValueType, uint size, Type type) => typeCode switch
    {
        TypeCode.Boolean  => *(bool*) source,
        TypeCode.Byte     => *(byte*) source,
        TypeCode.Char     => *(char*) source,
        TypeCode.DateTime => *(DateTime*) source,
        TypeCode.Decimal  => *(decimal*) source,
        TypeCode.Double   => *(double*) source,
        TypeCode.Int16    => *(short*) source,
        TypeCode.Int32    => *(int*) source,
        TypeCode.Int64    => *(long*) source,
        TypeCode.SByte    => *(sbyte*) source,
        TypeCode.Single   => *(float*) source,
        TypeCode.UInt16   => *(ushort*) source,
        TypeCode.UInt32   => *(uint*) source,
        TypeCode.UInt64   => *(ulong*) source,
        // _                 => isValueType ? Box(source, type, size) : *(object*) source
        _ => throw new InvalidOperationException($"Unsupported type code: {typeCode}.")
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue<TValue>(void* source, ref TValue value) where TValue : struct => *(TValue*) source = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue(void* source, Type type, object value)
    {
        var typeCode = Type.GetTypeCode(type);
        var isValueType = type.CheckType().IsValueType;
        var size = GetStackSize(type);
        SetValueInternal(source, typeCode, isValueType, size, value);
    }

    internal static void SetValueInternal<TValue>(void* source, TValue value) => *(TValue*) source = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void SetValueInternal(void* source, TypeCode typeCode, bool isValueType, uint size, object value)
    {
        switch (typeCode)
        {
            case TypeCode.Boolean:
                *(bool*) source = (bool) value;
                break;
            case TypeCode.Byte:
                *(byte*) source = (byte) value;
                break;
            case TypeCode.Char:
                *(char*) source = (char) value;
                break;
            case TypeCode.DateTime:
                *(DateTime*) source = (DateTime) value;
                break;
            case TypeCode.Decimal:
                *(decimal*) source = (decimal) value;
                break;
            case TypeCode.Double:
                *(double*) source = (double) value;
                break;
            case TypeCode.Int16:
                *(short*) source = (short) value;
                break;
            case TypeCode.Int32:
                *(int*) source = (int) value;
                break;
            case TypeCode.Int64:
                *(long*) source = (long) value;
                break;
            case TypeCode.SByte:
                *(sbyte*) source = (sbyte) value;
                break;
            case TypeCode.Single:
                *(float*) source = (float) value;
                break;
            case TypeCode.UInt16:
                *(ushort*) source = (ushort) value;
                break;
            case TypeCode.UInt32:
                *(uint*) source = (uint) value;
                break;
            case TypeCode.UInt64:
                *(ulong*) source = (ulong) value;
                break;
            default:
                if (isValueType)
                    CopyMemory(Unbox(value), source, size);
                else
                    Operation.SetObjectAddress(value, source);
                break;
        }
    }
}