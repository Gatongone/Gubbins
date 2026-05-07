using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Gubbins.Enhance;

[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed unsafe partial class ValuableMember
{
    private readonly bool m_IsValueType;
    public readonly Type MemberType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource, TResult>(ref TSource source, out TResult result) where TSource : struct where TResult : struct
    {
        result = m_IsField
            ? Native.GetValue<TResult>((byte*) Native.GetAddress(ref source) + m_Offset)
            : GetWithStructSource<TSource, TResult>(ref source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource>(ref TSource source, out object result) where TSource : struct
    {
        result = m_IsField
            ? Native.GetValueInternal((byte*) Native.GetAddress(ref source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, MemberType)
            : GetWithStructSource(ref source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource, TResult>(TSource source, out TResult result) where TSource : class where TResult : struct
    {
        result = m_IsField
            ? Native.GetValue<TResult>((byte*) Native.GetAddress(source) + m_Offset)
            : GetWithClassSource<TSource, TResult>(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource>(TSource source, out object result) where TSource : class
    {
        result = m_IsField
            ? Native.GetValueInternal((byte*) Native.GetAddress(source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, MemberType)
            : GetWithClassSource(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource, TValue>(ref TSource source, ref TValue value) where TSource : struct where TValue : struct
    {
        if (m_IsField)
            Native.SetValue((byte*) Native.GetAddress(ref source) + m_Offset, ref value);
        else
            SetWithStructSource(ref source, ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource>(ref TSource source, object value) where TSource : struct
    {
        if (m_IsField)
            Native.SetValueInternal((byte*) Native.GetAddress(ref source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, value);
        else
            SetWithStructSource(ref source, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource, TValue>(TSource source, ref TValue value) where TSource : class where TValue : struct
    {
        if (m_IsField)
            Native.SetValue((byte*) Native.GetAddress(source) + m_Offset, ref value);
        else
            SetWithClassSource(source, ref value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource>(TSource source, object value) where TSource : class
    {
        if (m_IsField)
            Native.SetValueInternal((byte*) Native.GetAddress(source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, value);
        else
            SetWithClassSource(source, value);
    }
}