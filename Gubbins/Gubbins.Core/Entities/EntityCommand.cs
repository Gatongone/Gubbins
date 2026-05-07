using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Enhance;

namespace Gubbins.Entities;

/// <summary>
/// Provides methods for getting entity query handles for unmanaged entities.
/// </summary>
public static class EntityCommandInsertExtensions
{

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1>(this IEntityCommand command, T1 component1) where T1 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var rg1 = new Range(0, size1);
        var types = FixedArrayPool<Type>.Instance.Rent(1);

        types[0] = typeof(T1);

        Span<byte> data = stackalloc byte[size1];

        MemoryMarshal.Write(data[rg1], ref component1);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2>(this IEntityCommand command, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var types = FixedArrayPool<Type>.Instance.Rent(2);

        types[0] = typeof(T1);
        types[1] = typeof(T2);

        Span<byte> data = stackalloc byte[size1 + size2];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3>(this IEntityCommand command, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var types = FixedArrayPool<Type>.Instance.Rent(3);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);

        Span<byte> data = stackalloc byte[size1 + size2 + size3];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var types = FixedArrayPool<Type>.Instance.Rent(4);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var types = FixedArrayPool<Type>.Instance.Rent(5);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var types = FixedArrayPool<Type>.Instance.Rent(6);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var types = FixedArrayPool<Type>.Instance.Rent(7);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var types = FixedArrayPool<Type>.Instance.Rent(8);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var types = FixedArrayPool<Type>.Instance.Rent(9);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var types = FixedArrayPool<Type>.Instance.Rent(10);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var types = FixedArrayPool<Type>.Instance.Rent(11);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var size12 = (int) Native.GetStackSize(typeof(T12));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var rg12 = new Range(rg11.End, rg11.End.Value + size12);
        var types = FixedArrayPool<Type>.Instance.Rent(12);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);
        types[11] = typeof(T12);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11 + size12];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);
        MemoryMarshal.Write(data[rg12], ref component12);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var size12 = (int) Native.GetStackSize(typeof(T12));
        var size13 = (int) Native.GetStackSize(typeof(T13));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var rg12 = new Range(rg11.End, rg11.End.Value + size12);
        var rg13 = new Range(rg12.End, rg12.End.Value + size13);
        var types = FixedArrayPool<Type>.Instance.Rent(13);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);
        types[11] = typeof(T12);
        types[12] = typeof(T13);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11 + size12 + size13];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);
        MemoryMarshal.Write(data[rg12], ref component12);
        MemoryMarshal.Write(data[rg13], ref component13);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var size12 = (int) Native.GetStackSize(typeof(T12));
        var size13 = (int) Native.GetStackSize(typeof(T13));
        var size14 = (int) Native.GetStackSize(typeof(T14));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var rg12 = new Range(rg11.End, rg11.End.Value + size12);
        var rg13 = new Range(rg12.End, rg12.End.Value + size13);
        var rg14 = new Range(rg13.End, rg13.End.Value + size14);
        var types = FixedArrayPool<Type>.Instance.Rent(14);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);
        types[11] = typeof(T12);
        types[12] = typeof(T13);
        types[13] = typeof(T14);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11 + size12 + size13 + size14];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);
        MemoryMarshal.Write(data[rg12], ref component12);
        MemoryMarshal.Write(data[rg13], ref component13);
        MemoryMarshal.Write(data[rg14], ref component14);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var size12 = (int) Native.GetStackSize(typeof(T12));
        var size13 = (int) Native.GetStackSize(typeof(T13));
        var size14 = (int) Native.GetStackSize(typeof(T14));
        var size15 = (int) Native.GetStackSize(typeof(T15));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var rg12 = new Range(rg11.End, rg11.End.Value + size12);
        var rg13 = new Range(rg12.End, rg12.End.Value + size13);
        var rg14 = new Range(rg13.End, rg13.End.Value + size14);
        var rg15 = new Range(rg14.End, rg14.End.Value + size15);
        var types = FixedArrayPool<Type>.Instance.Rent(15);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);
        types[11] = typeof(T12);
        types[12] = typeof(T13);
        types[13] = typeof(T14);
        types[14] = typeof(T15);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11 + size12 + size13 + size14 + size15];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);
        MemoryMarshal.Write(data[rg12], ref component12);
        MemoryMarshal.Write(data[rg13], ref component13);
        MemoryMarshal.Write(data[rg14], ref component14);
        MemoryMarshal.Write(data[rg15], ref component15);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }

    /// <summary>
    /// Insert an entity with the specified components.
    /// </summary>
    public static Entity Insert<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this IEntityCommand command, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        var size1 = (int) Native.GetStackSize(typeof(T1));
        var size2 = (int) Native.GetStackSize(typeof(T2));
        var size3 = (int) Native.GetStackSize(typeof(T3));
        var size4 = (int) Native.GetStackSize(typeof(T4));
        var size5 = (int) Native.GetStackSize(typeof(T5));
        var size6 = (int) Native.GetStackSize(typeof(T6));
        var size7 = (int) Native.GetStackSize(typeof(T7));
        var size8 = (int) Native.GetStackSize(typeof(T8));
        var size9 = (int) Native.GetStackSize(typeof(T9));
        var size10 = (int) Native.GetStackSize(typeof(T10));
        var size11 = (int) Native.GetStackSize(typeof(T11));
        var size12 = (int) Native.GetStackSize(typeof(T12));
        var size13 = (int) Native.GetStackSize(typeof(T13));
        var size14 = (int) Native.GetStackSize(typeof(T14));
        var size15 = (int) Native.GetStackSize(typeof(T15));
        var size16 = (int) Native.GetStackSize(typeof(T16));
        var rg1 = new Range(0, size1);
        var rg2 = new Range(rg1.End, rg1.End.Value + size2);
        var rg3 = new Range(rg2.End, rg2.End.Value + size3);
        var rg4 = new Range(rg3.End, rg3.End.Value + size4);
        var rg5 = new Range(rg4.End, rg4.End.Value + size5);
        var rg6 = new Range(rg5.End, rg5.End.Value + size6);
        var rg7 = new Range(rg6.End, rg6.End.Value + size7);
        var rg8 = new Range(rg7.End, rg7.End.Value + size8);
        var rg9 = new Range(rg8.End, rg8.End.Value + size9);
        var rg10 = new Range(rg9.End, rg9.End.Value + size10);
        var rg11 = new Range(rg10.End, rg10.End.Value + size11);
        var rg12 = new Range(rg11.End, rg11.End.Value + size12);
        var rg13 = new Range(rg12.End, rg12.End.Value + size13);
        var rg14 = new Range(rg13.End, rg13.End.Value + size14);
        var rg15 = new Range(rg14.End, rg14.End.Value + size15);
        var rg16 = new Range(rg15.End, rg15.End.Value + size16);
        var types = FixedArrayPool<Type>.Instance.Rent(16);

        types[0] = typeof(T1);
        types[1] = typeof(T2);
        types[2] = typeof(T3);
        types[3] = typeof(T4);
        types[4] = typeof(T5);
        types[5] = typeof(T6);
        types[6] = typeof(T7);
        types[7] = typeof(T8);
        types[8] = typeof(T9);
        types[9] = typeof(T10);
        types[10] = typeof(T11);
        types[11] = typeof(T12);
        types[12] = typeof(T13);
        types[13] = typeof(T14);
        types[14] = typeof(T15);
        types[15] = typeof(T16);

        Span<byte> data = stackalloc byte[size1 + size2 + size3 + size4 + size5 + size6 + size7 + size8 + size9 + size10 + size11 + size12 + size13 + size14 + size15 + size16];

        MemoryMarshal.Write(data[rg1], ref component1);
        MemoryMarshal.Write(data[rg2], ref component2);
        MemoryMarshal.Write(data[rg3], ref component3);
        MemoryMarshal.Write(data[rg4], ref component4);
        MemoryMarshal.Write(data[rg5], ref component5);
        MemoryMarshal.Write(data[rg6], ref component6);
        MemoryMarshal.Write(data[rg7], ref component7);
        MemoryMarshal.Write(data[rg8], ref component8);
        MemoryMarshal.Write(data[rg9], ref component9);
        MemoryMarshal.Write(data[rg10], ref component10);
        MemoryMarshal.Write(data[rg11], ref component11);
        MemoryMarshal.Write(data[rg12], ref component12);
        MemoryMarshal.Write(data[rg13], ref component13);
        MemoryMarshal.Write(data[rg14], ref component14);
        MemoryMarshal.Write(data[rg15], ref component15);
        MemoryMarshal.Write(data[rg16], ref component16);

        var result = command.Insert(data, types);
        FixedArrayPool<Type>.Instance.Return(types);
        return result;
    }
}

/// <summary>
/// Provides methods for update entities' components.
/// </summary>
public static class EntityCommandUpdateExtensions
{

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1>(this IEntityCommand command, Entity entity, T1 component1) where T1 : unmanaged
    {
        command.Update(entity.Index, component1);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2>(this IEntityCommand command, Entity entity, T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
        command.Update(entity.Index, component12);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
        command.Update(entity.Index, component12);
        command.Update(entity.Index, component13);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
        command.Update(entity.Index, component12);
        command.Update(entity.Index, component13);
        command.Update(entity.Index, component14);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
        command.Update(entity.Index, component12);
        command.Update(entity.Index, component13);
        command.Update(entity.Index, component14);
        command.Update(entity.Index, component15);
    }

    /// <summary>
    /// Updates the components data of a specific entity for a given component type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Update<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this IEntityCommand command, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        command.Update(entity.Index, component1);
        command.Update(entity.Index, component2);
        command.Update(entity.Index, component3);
        command.Update(entity.Index, component4);
        command.Update(entity.Index, component5);
        command.Update(entity.Index, component6);
        command.Update(entity.Index, component7);
        command.Update(entity.Index, component8);
        command.Update(entity.Index, component9);
        command.Update(entity.Index, component10);
        command.Update(entity.Index, component11);
        command.Update(entity.Index, component12);
        command.Update(entity.Index, component13);
        command.Update(entity.Index, component14);
        command.Update(entity.Index, component15);
        command.Update(entity.Index, component16);
    }
}