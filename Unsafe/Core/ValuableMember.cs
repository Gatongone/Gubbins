using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Gubbins.Unsafe;

[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed unsafe partial class ValuableMember
{
    /// <summary>
    /// A readonly boolean that indicates whether the member is a field. This is determined by checking if the member is a FieldInfo or a PropertyInfo with a backing field.
    /// </summary>
    private readonly bool m_IsValueType;

    /// <summary>
    /// A readonly TypeCode that represents the type code of the member's type. This is used for optimized handling of primitive types and can help in determining how to read or write the member's value efficiently.
    /// </summary>
    public readonly Type MemberType;

    /// <summary>
    /// Gets the value of the member from the source object and outputs it as an object.
    /// </summary>
    /// <param name="source">The source object from which to retrieve the member's value.</param>
    /// <param name="result"> An output parameter that will hold the retrieved value of the member after the method execution.</param>
    /// <typeparam name="TSource">The type of the source object from which to retrieve the member's value.</typeparam>
    /// <typeparam name="TResult">The type of the result that will hold the retrieved value of the member.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource, TResult>(ref TSource source, out TResult result) where TSource : struct where TResult : struct
    {
        result = m_IsField
            ? Native.GetValue<TResult>((byte*) Native.GetAddress(ref source) + m_Offset)
            : GetWithStructSource<TSource, TResult>(ref source);
    }

    /// <summary>
    /// Gets the value of the member from the source object and outputs it as an object.
    /// </summary>
    /// <param name="source">The source object from which to retrieve the member's value.</param>
    /// <param name="result"> An output parameter that will hold the retrieved value of the member after the method execution.</param>
    /// <typeparam name="TSource">The type of the source object from which to retrieve the member's value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource>(ref TSource source, out object result) where TSource : struct
    {
        result = m_IsField
            ? Native.GetValueInternal((byte*) Native.GetAddress(ref source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, MemberType)
            : GetWithStructSource(ref source);
    }

    /// <summary>
    /// Gets the value of the member from the source object and outputs it as a TResult.
    /// </summary>
    /// <param name="source">The source object from which to retrieve the member's value.</param>
    /// <param name="result"> An output parameter that will hold the retrieved value of the member after the method execution.</param>
    /// <typeparam name="TSource">The type of the source object from which to retrieve the member's value.</typeparam>
    /// <typeparam name="TResult">The type of the result that will hold the retrieved value of the member.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource, TResult>(TSource source, out TResult result) where TSource : class where TResult : struct
    {
        result = m_IsField
            ? Native.GetValue<TResult>((byte*) Native.GetAddress(source) + m_Offset)
            : GetWithClassSource<TSource, TResult>(source);
    }

    /// <summary>
    /// Gets the value of the member from the source object and outputs it as an object.
    /// </summary>
    /// <param name="source">The source object from which to retrieve the member's value.</param>
    /// <param name="result"> An output parameter that will hold the retrieved value of the member after the method execution.</param>
    /// <typeparam name="TSource">The type of the source object from which to retrieve the member's value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetValue<TSource>(TSource source, out object result) where TSource : class
    {
        result = m_IsField
            ? Native.GetValueInternal((byte*) Native.GetAddress(source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, MemberType)
            : GetWithClassSource(source);
    }

    /// <summary>
    /// Sets the value of the member on the source object using a reference to the value.
    /// </summary>
    /// <param name="source">The source object on which to set the member's value.</param>
    /// <param name="value">The value to set on the member, passed by reference.</param>
    /// <typeparam name="TSource">The type of the source object on which to set the member's value.</typeparam>
    /// <typeparam name="TValue">The type of the value to set on the member.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource, TValue>(ref TSource source, ref TValue value) where TSource : struct where TValue : struct
    {
        if (m_IsField)
            Native.SetValue((byte*) Native.GetAddress(ref source) + m_Offset, ref value);
        else
            SetWithStructSource(ref source, ref value);
    }

    /// <summary>
    /// Sets the value of the member on the source object using an object value.
    /// </summary>
    /// <param name="source">The source object on which to set the member's value.</param>
    /// <param name="value">The value to set on the member, passed as an object.</param>
    /// <typeparam name="TSource">The type of the source object on which to set the member's value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource>(ref TSource source, object value) where TSource : struct
    {
        if (m_IsField)
            Native.SetValueInternal((byte*) Native.GetAddress(ref source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, value);
        else
            SetWithStructSource(ref source, value);
    }

    /// <summary>
    /// Sets the value of the member on the source object using a reference to the value.
    /// </summary>
    /// <param name="source">The source object on which to set the member's value.</param>
    /// <param name="value">The value to set on the member, passed by reference.</param>
    /// <typeparam name="TSource">The type of the source object on which to set the member's value.</typeparam>
    /// <typeparam name="TValue">The type of the value to set on the member.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource, TValue>(TSource source, ref TValue value) where TSource : class where TValue : struct
    {
        if (m_IsField)
            Native.SetValue((byte*) Native.GetAddress(source) + m_Offset, ref value);
        else
            SetWithClassSource(source, ref value);
    }

    /// <summary>
    /// Sets the value of the member on the source object using an object value.
    /// </summary>
    /// <param name="source">The source object on which to set the member's value.</param>
    /// <param name="value">The value to set on the member, passed as an object.</param>
    /// <typeparam name="TSource">The type of the source object on which to set the member's value.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetValue<TSource>(TSource source, object value) where TSource : class
    {
        if (m_IsField)
            Native.SetValueInternal((byte*) Native.GetAddress(source) + m_Offset, m_TypeCode, m_IsValueType, m_Size, value);
        else
            SetWithClassSource(source, value);
    }
}