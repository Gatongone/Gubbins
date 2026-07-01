using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gubbins.Unsafe;

[SuppressMessage("ReSharper", "ClassWithVirtualMembersNeverInherited.Global")]
public unsafe class Memory
{
#if !NATIVEAOT
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, uint> s_SizeOfMaps = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint DynamicSizeOf(Type type)
    {
        if (s_SizeOfMaps.TryGetValue(type, out var size))
            return size;

        var dynamicSizeOf = new System.Reflection.Emit.DynamicMethod("GetManagedSizeImpl", typeof(uint), null, true);
        var generator = dynamicSizeOf.GetILGenerator();

        generator.Emit(System.Reflection.Emit.OpCodes.Sizeof, type);
        generator.Emit(System.Reflection.Emit.OpCodes.Ret);

        var sizeOfFunc = (Func<uint>) dynamicSizeOf.CreateDelegate(typeof(Func<uint>));
        size = (uint) checked((int) sizeOfFunc());
        s_SizeOfMaps.TryAdd(type, size);
        return size;
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual IntPtr Allocate(int size, int align = 8) => Marshal.AllocHGlobal(size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void  Free(IntPtr ptr) => Marshal.FreeHGlobal(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual uint GetFieldOffset(FieldInfo fieldInfo)
    {
        var headOffset = fieldInfo.DeclaringType!.CheckType().IsValueType ? 0 : IntPtr.Size;
        return (uint) ((Marshal.ReadInt32(fieldInfo.FieldHandle.Value + (4 + IntPtr.Size)) & 0xFFFFFF) + headOffset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type
    public virtual T ConvertToStructure<T>(void* ptr) where T : struct => *(T*) ptr;
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void CopyMemory(void* source, void* destination, uint offset)
    {
#if NET5_0_OR_GREATER
        System.Runtime.CompilerServices.Unsafe.CopyBlock(destination, source, offset);
#else
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Memcpy((IntPtr) destination, (IntPtr) source, (UIntPtr) offset);
        }
        else
        {
            var src = new Span<byte>(source, (int) offset);
            var dst = new Span<byte>(destination, (int) offset);
            src.CopyTo(dst);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual void SetObjectAddress(object source, void* destination) => *(IntPtr*) destination = (IntPtr) Native.GetAddress(source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual uint GetStackSize(Type type)
    {
#if !NATIVEAOT
        return DynamicSizeOf(type);
#else
        var typeChecker = type.CheckType();
        if (!typeChecker.IsValueType)
            return (uint) IntPtr.Size;
        if (typeChecker.IsEnum)
            type = Enum.GetUnderlyingType(type);
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:  return sizeof(bool);
            case TypeCode.Byte:     return sizeof(byte);
            case TypeCode.Char:     return sizeof(char);
            case TypeCode.Decimal:  return sizeof(decimal);
            case TypeCode.Double:   return sizeof(double);
            case TypeCode.Int16:    return sizeof(short);
            case TypeCode.Int32:    return sizeof(int);
            case TypeCode.Int64:    return sizeof(long);
            case TypeCode.SByte:    return sizeof(sbyte);
            case TypeCode.Single:   return sizeof(float);
            case TypeCode.UInt16:   return sizeof(ushort);
            case TypeCode.UInt32:   return sizeof(uint);
            case TypeCode.UInt64:   return sizeof(ulong);
            case TypeCode.DateTime: return (uint) sizeof(DateTime);
            case TypeCode.String:   return (uint) IntPtr.Size;
            default:
                if (!typeChecker.IsManagedType) return (uint) Marshal.SizeOf(type);
                break;
        }
        throw new ArgumentException($"Invalid operation for managed struct. Type: {type}. ");
#endif
    }

    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
    private static extern IntPtr Memcpy(IntPtr dest, IntPtr src, UIntPtr count);
}