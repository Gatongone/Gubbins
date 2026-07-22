using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Unsafe;

/// <summary>
/// Provides low-level memory and pointer helpers for unsafe operations.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed unsafe class Native
{
    /// <summary>
    /// Runtime-specific memory backend used by this facade.
    /// </summary>
    internal static Memory Operation = new();

    /// <summary>
    /// Allocates an unmanaged aligned memory block.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <param name="align">The requested byte alignment.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr Alloc(int size) => Operation.Alloc(size);

    /// <summary>
    /// Allocates an unmanaged aligned memory block.
    /// </summary>
    /// <param name="size">The number of bytes to allocate.</param>
    /// <param name="align">The requested byte alignment.</param>
    /// <returns>A pointer to the allocated memory block.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IntPtr AlignedAlloc(int size, int align) => Operation.AlignedAlloc(size, align);

    /// <summary>
    /// Frees a previously allocated unmanaged aligned memory block.
    /// </summary>
    /// <param name="ptr">The pointer returned by <see cref="AlignedAlloc"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AlignedFree(IntPtr ptr) => Operation.AlignedFree(ptr);

    /// <summary>
    /// Frees a previously allocated unmanaged memory block.
    /// </summary>
    /// <param name="ptr">The pointer returned by <see cref="AlignedAlloc"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Free(IntPtr ptr) => Operation.Free(ptr);

    /// <summary>
    /// Gets the natural alignment for the unmanaged representation of <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The value type to inspect.</typeparam>
    /// <returns>The alignment in bytes (1, 2, 4, or 8).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAlignment<T>() => GetAlignment((int) GetStackSize<T>());

    /// <summary>
    /// Gets an alignment value based on a byte size.
    /// </summary>
    /// <param name="size">The byte size to evaluate.</param>
    /// <returns>The alignment in bytes (1, 2, 4, or 8).</returns>
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
    /// Reinterprets a struct reference as a reference to another struct type.
    /// </summary>
    /// <typeparam name="TFrom">The source struct type.</typeparam>
    /// <typeparam name="TTo">The destination struct type.</typeparam>
    /// <param name="src">The source value reference.</param>
    /// <returns>A reference to the same memory interpreted as <typeparamref name="TTo"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo Cast<TFrom, TTo>(ref TFrom src) where TFrom : struct where TTo : struct
    {
        fixed (TFrom* pSource = &src)
        {
            return ref *(TTo*) pSource;
        }
    }

    /// <summary>
    /// Reinterprets an object reference as a struct reference.
    /// </summary>
    /// <typeparam name="TTo">The target struct type.</typeparam>
    /// <param name="src">The source object reference.</param>
    /// <returns>A reference to memory interpreted as <typeparamref name="TTo"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TTo Cast<TTo>(object src) => ref *(TTo*) &src;

    /// <summary>
    /// Reinterprets the bits of one struct as another struct type.
    /// </summary>
    /// <typeparam name="TFrom">The source struct type.</typeparam>
    /// <typeparam name="TTo">The destination struct type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <returns>A value of <typeparamref name="TTo"/> backed by the same bits.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TTo BitCast<TFrom, TTo>(TFrom source) where TFrom : struct where TTo : struct => Cast<byte, TTo>(ref Cast<TFrom, byte>(ref source));

    /// <summary>
    /// Gets the byte offset of a field within its declaring type.
    /// </summary>
    /// <param name="fieldInfo">The field metadata.</param>
    /// <returns>The field offset in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetFieldOffset(FieldInfo fieldInfo) => Operation.GetFieldOffset(fieldInfo);

    /// <summary>
    /// Returns a pinned pointer to a value or object reference slot.
    /// </summary>
    /// <typeparam name="T">The value type or reference type.</typeparam>
    /// <param name="value">The value to pin for the duration of the call.</param>
    /// <returns>A pointer to the value data or object reference slot.</returns>
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
    /// Gets a pointer to the first element in an array.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="value">The source array.</param>
    /// <returns>A pointer to the first element, or a null pointer for an empty array.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(T[] value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Gets a pointer to the first element in a span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="value">The source span.</param>
    /// <returns>A pointer to the first element, or a null pointer for an empty span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(Span<T> value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Gets a pointer to the first element in a read-only span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="value">The source span.</param>
    /// <returns>A pointer to the first element, or a null pointer for an empty span.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetFirstElementAddress<T>(ReadOnlySpan<T> value)
    {
        fixed (void* ptr = value)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Gets the address of a struct value.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    /// <param name="structure">The struct value.</param>
    /// <returns>A pointer to the struct.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetAddress<T>(ref T structure) where T : struct
    {
        fixed (void* ptr = &structure)
        {
            return ptr;
        }
    }

    /// <summary>
    /// Gets the object reference address.
    /// </summary>
    /// <param name="instance">The source object instance.</param>
    /// <returns>A pointer to the object header.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* GetAddress(object instance) => *(void**) &instance;

    /// <summary>
    /// Adds a byte offset to a reference and reinterprets the result as the same type.
    /// </summary>
    /// <typeparam name="T">The struct type.</typeparam>
    /// <param name="source">The source reference.</param>
    /// <param name="byteOffset">The offset in bytes.</param>
    /// <returns>A reference at the shifted address.</returns>
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

    /// <summary>
    /// Finds a field or property that can be read/written by name.
    /// </summary>
    /// <param name="sourceType">The type that declares the member.</param>
    /// <param name="name">The target member name.</param>
    /// <returns>The matching valuable member, if found.</returns>
    /// <exception cref="ArgumentException">Thrown when the source type is interface, abstract, or static.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValuableMember? GetValuableMember(Type sourceType, string name)
    {
        var typeChecker = sourceType.CheckType();
        if (typeChecker.IsInterface || typeChecker.IsAbstract || typeChecker.IsStatic)
            throw new ArgumentException("Can't get the valuable member from a abstract or static class.");
        return sourceType.GetValuableMember(name);
    }

    /// <summary>
    /// Gets the unmanaged stack size for a type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The size in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetStackSize(Type type) => Operation.GetStackSize(type);

    /// <summary>
    /// Gets the unmanaged stack size for <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to inspect.</typeparam>
    /// <returns>The size in bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetStackSize<T>() => GetStackSize(typeof(T));

    /// <summary>
    /// Copies bytes from one memory location to another.
    /// </summary>
    /// <param name="source">The source pointer.</param>
    /// <param name="destination">The destination pointer.</param>
    /// <param name="offset">The number of bytes to copy.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CopyMemory(void* source, void* destination, uint offset) => Operation.CopyMemory(source, destination, offset);

    /// <summary>
    /// Boxes a value from unmanaged memory into a managed object.
    /// </summary>
    /// <param name="address">The source unmanaged address.</param>
    /// <param name="sourceType">The value type to construct.</param>
    /// <param name="offset">The number of bytes to copy.</param>
    /// <returns>A boxed value of <paramref name="sourceType"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Box(void* address, Type sourceType, uint offset)
    {
#if NET5_0_OR_GREATER
        var valueObj = RuntimeHelpers.GetUninitializedObject(sourceType);
#else
        var valueObj = FormatterServices.GetUninitializedObject(sourceType);
#endif
        CopyMemory(address, Unbox(valueObj), offset);
        return valueObj;
    }

    /// <summary>
    /// Gets a pointer to the raw data payload of a boxed object.
    /// </summary>
    /// <param name="source">The boxed object.</param>
    /// <returns>A pointer to the object data payload.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Unbox(object source) => (byte*) GetAddress(source) + IntPtr.Size;

    /// <summary>
    /// Reads a struct value from an unmanaged pointer.
    /// </summary>
    /// <typeparam name="T">The struct type to read.</typeparam>
    /// <param name="ptr">The source pointer.</param>
    /// <returns>The read value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T GetValue<T>(void* ptr) where T : struct => *(T*) ptr;

    /// <summary>
    /// Reinterprets an unmanaged pointer as a managed reference.
    /// </summary>
    /// <typeparam name="TResult">The destination reference type.</typeparam>
    /// <param name="ptr">The source pointer.</param>
    /// <returns>A managed reference bound to <paramref name="ptr"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TResult AsRef<TResult>(void* ptr) => ref *(TResult*) ptr;

    /// <summary>
    /// Reinterprets the first element of a span as a different reference type.
    /// </summary>
    /// <typeparam name="TFrom">The source element type.</typeparam>
    /// <typeparam name="TResult">The destination reference type.</typeparam>
    /// <param name="span">The source span.</param>
    /// <returns>A managed reference bound to the span data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TResult AsRef<TFrom, TResult>(Span<TFrom> span)
    {
        fixed (void* p = span)
        {
            return ref AsRef<TResult>(p);
        }
    }

    /// <summary>
    /// Reads a value from unmanaged memory using runtime type metadata.
    /// </summary>
    /// <param name="source">The source pointer.</param>
    /// <param name="type">The value type to read.</param>
    /// <returns>A boxed value or object reference read from <paramref name="source"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object GetValue(void* source, Type type)
    {
        var typeCode = Type.GetTypeCode(type);
        var isValueType = type.CheckType().IsValueType;
        var size = GetStackSize(type);
        return GetValueInternal(source, typeCode, isValueType, size, type);
    }

    /// <summary>
    /// Internal typed read dispatch for unmanaged memory.
    /// </summary>
    /// <param name="source">The source pointer.</param>
    /// <param name="typeCode">The primitive type code.</param>
    /// <param name="isValueType">Whether the target type is a value type.</param>
    /// <param name="size">The value size in bytes.</param>
    /// <param name="type">The full target type metadata.</param>
    /// <returns>A boxed primitive, boxed struct, or object reference.</returns>
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
        _                 => isValueType ? Box(source, type, size) : *(object*) source
    };

    /// <summary>
    /// Writes a struct value to unmanaged memory.
    /// </summary>
    /// <typeparam name="TValue">The struct type to write.</typeparam>
    /// <param name="source">The destination pointer.</param>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue<TValue>(void* source, ref TValue value) where TValue : struct => *(TValue*) source = value;

    /// <summary>
    /// Writes an object value to unmanaged memory using runtime type metadata.
    /// </summary>
    /// <param name="source">The destination pointer.</param>
    /// <param name="value">The value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetValue(void* source, object value)
    {
        var type = value.GetType();
        var typeCode = Type.GetTypeCode(type);
        var isValueType = type.CheckType().IsValueType;
        var size = GetStackSize(type);
        SetValueInternal(source, typeCode, isValueType, size, value);
    }

    /// <summary>
    /// Internal generic write helper for unmanaged memory.
    /// </summary>
    /// <typeparam name="TValue">The value type to write.</typeparam>
    /// <param name="source">The destination pointer.</param>
    /// <param name="value">The value to write.</param>
    internal static void SetValueInternal<TValue>(void* source, TValue value) => *(TValue*) source = value;

    /// <summary>
    /// Internal typed write dispatch for unmanaged memory.
    /// </summary>
    /// <param name="source">The destination pointer.</param>
    /// <param name="typeCode">The primitive type code.</param>
    /// <param name="isValueType">Whether the source value is a value type.</param>
    /// <param name="size">The value size in bytes.</param>
    /// <param name="value">The value to write.</param>
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