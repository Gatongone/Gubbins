using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable CS8603 // Possible null reference return.

namespace Gubbins.Unsafe;

[SuppressMessage("ReSharper", "UnusedType.Local")]
public partial class ValuableMember
{
    private const string STRUCT_SOURCE_SET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(ValuableMember)}+StructSourceSetWrapper";
    private const string CLASS_SOURCE_SET_WRAPPER_NAME  = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(ValuableMember)}+ClassSourceSetWrapper";
    private const string STRUCT_SOURCE_GET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(ValuableMember)}+StructSourceGetWrapper";
    private const string CLASS_SOURCE_GET_WRAPPER_NAME  = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(ValuableMember)}+ClassSourceGetWrapper";

    /// <summary>
    /// A wrapper for the get method of a property when the source type is a class.
    /// This wrapper is used to handle cases where the source type is a class,
    /// and we need to manage boxing and unboxing of value types when getting the property value.
    /// The actual delegate for getting the property value is stored in <see cref="m_GetMethod"/>,
    /// and this wrapper provides an additional layer to handle the specific call shapes for class sources.
    /// </summary>
    private readonly object m_StructValueGetWrapper;

    /// <summary>
    /// The actual delegate that was created from the property's get method.
    /// This delegate is used to invoke the get method directly when retrieving the property value,
    /// and the wrapper is used to handle cases where the source type is a class,
    /// and we need to manage the boxing and unboxing of value types when getting the property value.
    /// </summary>
    private readonly Delegate m_GetMethod;

    /// <summary>
    /// A wrapper for the set method of a property when the source type is a struct.
    /// This wrapper is used to handle cases where the source type is a struct,
    /// and we need to manage boxing and unboxing of value types when setting the property value.
    /// The actual delegate for setting the property value is stored in <see cref="m_SetMethod"/>,
    /// and this wrapper provides an additional layer to handle the specific call shapes for struct sources.
    /// </summary>
    private readonly object m_StructValueSetWrapper;

    /// <summary>
    /// The actual delegate that was created from the property's set method.
    /// This delegate is used to invoke the set method directly when setting the property value,
    /// and the wrapper is used to handle cases where the source type is a struct,
    /// and we need to manage boxing and unboxing of value types.
    /// </summary>
    private readonly Delegate m_SetMethod;

    /// <summary>
    /// A readonly Type that represents the type of the member. This is used to determine how to read or write the member's value and to provide type information for the member.
    /// </summary>
    private readonly TypeCode m_TypeCode;

    /// <summary>
    /// A readonly boolean that indicates whether the member's type is an enum.
    /// This is determined by checking the IsEnum property of the member's type and is used to handle enum types differently when getting or setting their values, especially in terms of boxing and unboxing.
    /// </summary>
    private readonly bool m_IsEnum;

    /// <summary>
    /// Initializes property accessor delegates and source wrappers used by fast get/set paths.
    /// </summary>
    /// <param name="propertyInfo">The reflected property metadata.</param>
    internal ValuableMember(PropertyInfo propertyInfo)
    {
        m_StructValueGetWrapper = null!;
        m_StructValueSetWrapper = null!;
        MemberType              = propertyInfo.PropertyType;
        m_IsField               = false;
        var checker = MemberType.CheckType();
        m_IsValueType = checker.IsValueType;

        var propertyType = propertyInfo.PropertyType;
        var sourceType = propertyInfo.DeclaringType;
        var getMethod = propertyInfo.GetMethod;
        var setMethod = propertyInfo.SetMethod;
        var propertyChecker = propertyType.CheckType();
        m_TypeCode = Type.GetTypeCode(propertyType);
        m_IsEnum   = propertyChecker.IsEnum;

        // For properties, we create delegates for the get and set methods, and also create wrapper instances that can handle boxing and unboxing for value types.
        // This allows us to efficiently get and set property values even when the source type is a struct.
        if (setMethod != null)
        {
            m_SetMethod = propertyInfo.CreateSetDelegate()!;
            if (sourceType != null)
            {
                // If the source type is a value type, we need to create a wrapper that can handle the boxing and unboxing of the value when setting the property value.
                // This is necessary because when we have a struct as the source, we need to pass it by reference to the set method,
                // and if the property type is also a value type, we need to ensure that we are correctly handling the value semantics.
                var isSourceValueType = sourceType.CheckType().IsValueType;
                var setWrapperType = isSourceValueType
                    ? Reflection.GetType(STRUCT_SOURCE_SET_WRAPPER_NAME, sourceType, propertyType)
                    : Reflection.GetType(CLASS_SOURCE_SET_WRAPPER_NAME, sourceType, propertyType);
                m_StructValueSetWrapper = Activator.CreateInstance(setWrapperType!, m_SetMethod);
            }
            else
            {
                // If there is no source type (which can happen for static properties),
                // we still need to create a wrapper for the set method, but it will be a class source wrapper since there is no struct source to consider.
                var setWrapperType = Reflection.GetType(CLASS_SOURCE_SET_WRAPPER_NAME, propertyType);
                m_StructValueSetWrapper = Activator.CreateInstance(setWrapperType!, m_SetMethod);
            }
        }

        // Similarly, for the get method, we create a delegate and a wrapper instance to handle the retrieval of the property value,
        // especially when dealing with struct sources and value type properties.
        if (getMethod != null)
        {
            m_GetMethod = propertyInfo.CreateGetDelegate()!;
            if (sourceType != null)
            {
                // Similar to the set method, if the source type is a value type, we need to create a wrapper that can handle
                // the boxing and unboxing of the value when getting the property value.
                var isSourceValueType = sourceType.CheckType().IsValueType;
                var getWrapperType = isSourceValueType
                    ? Reflection.GetType(STRUCT_SOURCE_GET_WRAPPER_NAME, sourceType, propertyType)
                    : Reflection.GetType(CLASS_SOURCE_GET_WRAPPER_NAME, sourceType, propertyType);
                m_StructValueGetWrapper = Activator.CreateInstance(getWrapperType!, m_GetMethod);
            }
            else
            {
                var getWrapperType = Reflection.GetType(CLASS_SOURCE_GET_WRAPPER_NAME, propertyInfo.PropertyType);
                m_StructValueGetWrapper = Activator.CreateInstance(getWrapperType!, m_GetMethod);
            }
        }
    }

    /// <summary>
    /// Gets a value-type property from a struct source using a strongly typed delegate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TResult GetWithStructSource<TSource, TResult>(ref TSource source) where TSource : struct where TResult : struct
        => ((MethodWithReturnValue.StructMethod<TSource, TResult>) m_GetMethod)(ref source);

    /// <summary>
    /// Gets a property value from a struct source and returns it as <see cref="object"/>.
    /// Uses typed fast paths for common primitives and wrapper fallback for other types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object GetWithStructSource<TSource>(ref TSource source) where TSource : struct => m_IsEnum
        ? ((IBoxingGetWrapper<TSource>) m_StructValueGetWrapper).Get(ref source)
        : m_TypeCode switch
        {
            TypeCode.Boolean  => ((MethodWithReturnValue.StructMethod<TSource, bool>) m_GetMethod)(ref source),
            TypeCode.Byte     => ((MethodWithReturnValue.StructMethod<TSource, byte>) m_GetMethod)(ref source),
            TypeCode.Char     => ((MethodWithReturnValue.StructMethod<TSource, char>) m_GetMethod)(ref source),
            TypeCode.DateTime => ((MethodWithReturnValue.StructMethod<TSource, DateTime>) m_GetMethod)(ref source),
            TypeCode.Decimal  => ((MethodWithReturnValue.StructMethod<TSource, decimal>) m_GetMethod)(ref source),
            TypeCode.Double   => ((MethodWithReturnValue.StructMethod<TSource, double>) m_GetMethod)(ref source),
            TypeCode.Int16    => ((MethodWithReturnValue.StructMethod<TSource, short>) m_GetMethod)(ref source),
            TypeCode.Int32    => ((MethodWithReturnValue.StructMethod<TSource, int>) m_GetMethod)(ref source),
            TypeCode.Int64    => ((MethodWithReturnValue.StructMethod<TSource, long>) m_GetMethod)(ref source),
            TypeCode.SByte    => ((MethodWithReturnValue.StructMethod<TSource, sbyte>) m_GetMethod)(ref source),
            TypeCode.Single   => ((MethodWithReturnValue.StructMethod<TSource, float>) m_GetMethod)(ref source),
            TypeCode.UInt16   => ((MethodWithReturnValue.StructMethod<TSource, ushort>) m_GetMethod)(ref source),
            TypeCode.UInt32   => ((MethodWithReturnValue.StructMethod<TSource, uint>) m_GetMethod)(ref source),
            TypeCode.UInt64   => ((MethodWithReturnValue.StructMethod<TSource, ulong>) m_GetMethod)(ref source),
            _                 => ((IBoxingGetWrapper<TSource>) m_StructValueGetWrapper).Get(ref source)
        };

    /// <summary>
    /// Gets a value-type property from a class source using a strongly typed delegate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TResult GetWithClassSource<TSource, TResult>(TSource source) where TSource : class where TResult : struct
    {
        return ((MethodWithReturnValue.ClassMethod<TSource, TResult>) m_GetMethod)(source);
    }

    /// <summary>
    /// Gets a property value from a class source and returns it as <see cref="object"/>.
    /// Uses typed fast paths for common primitives and wrapper fallback for other types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private object GetWithClassSource<TSource>(TSource source) where TSource : class => m_IsEnum
        ? ((IBoxingGetWrapper<TSource>) m_StructValueGetWrapper).Get(source)
        : m_TypeCode switch
        {
            TypeCode.Boolean  => ((MethodWithReturnValue.ClassMethod<TSource, bool>) m_GetMethod)(source),
            TypeCode.Byte     => ((MethodWithReturnValue.ClassMethod<TSource, byte>) m_GetMethod)(source),
            TypeCode.Char     => ((MethodWithReturnValue.ClassMethod<TSource, char>) m_GetMethod)(source),
            TypeCode.DateTime => ((MethodWithReturnValue.ClassMethod<TSource, DateTime>) m_GetMethod)(source),
            TypeCode.Decimal  => ((MethodWithReturnValue.ClassMethod<TSource, decimal>) m_GetMethod)(source),
            TypeCode.Double   => ((MethodWithReturnValue.ClassMethod<TSource, double>) m_GetMethod)(source),
            TypeCode.Int16    => ((MethodWithReturnValue.ClassMethod<TSource, short>) m_GetMethod)(source),
            TypeCode.Int32    => ((MethodWithReturnValue.ClassMethod<TSource, int>) m_GetMethod)(source),
            TypeCode.Int64    => ((MethodWithReturnValue.ClassMethod<TSource, long>) m_GetMethod)(source),
            TypeCode.SByte    => ((MethodWithReturnValue.ClassMethod<TSource, sbyte>) m_GetMethod)(source),
            TypeCode.Single   => ((MethodWithReturnValue.ClassMethod<TSource, float>) m_GetMethod)(source),
            TypeCode.UInt16   => ((MethodWithReturnValue.ClassMethod<TSource, ushort>) m_GetMethod)(source),
            TypeCode.UInt32   => ((MethodWithReturnValue.ClassMethod<TSource, uint>) m_GetMethod)(source),
            TypeCode.UInt64   => ((MethodWithReturnValue.ClassMethod<TSource, ulong>) m_GetMethod)(source),
            _                 => ((IBoxingGetWrapper<TSource>) m_StructValueGetWrapper).Get(source)
        };

    /// <summary>
    /// Sets a value-type property on a struct source using a strongly typed delegate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithStructSource<TSource, TValue>(ref TSource source, ref TValue value) where TSource : struct where TValue : struct
    {
        ((MethodWithoutReturnValue.StructMethod<TSource, TValue>) m_SetMethod)(ref source, value);
    }

    /// <summary>
    /// Sets a property on a struct source from a boxed value.
    /// Uses typed fast paths for common primitives and wrapper fallback for other types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithStructSource<TSource>(ref TSource source, object? value) where TSource : struct
    {
        if (m_IsEnum)
        {
            ((IUnboxingSetWrapper<TSource>) m_StructValueSetWrapper).Set(ref source, value);
            return;
        }

        switch (m_TypeCode)
        {
            case TypeCode.Boolean:
                ((MethodWithoutReturnValue.StructMethod<TSource, bool>) m_SetMethod)(ref source, (bool) value!);
                break;
            case TypeCode.Byte:
                ((MethodWithoutReturnValue.StructMethod<TSource, byte>) m_SetMethod)(ref source, (byte) value!);
                break;
            case TypeCode.Char:
                ((MethodWithoutReturnValue.StructMethod<TSource, char>) m_SetMethod)(ref source, (char) value!);
                break;
            case TypeCode.DateTime:
                ((MethodWithoutReturnValue.StructMethod<TSource, DateTime>) m_SetMethod)(ref source, (DateTime) value!);
                break;
            case TypeCode.Decimal:
                ((MethodWithoutReturnValue.StructMethod<TSource, decimal>) m_SetMethod)(ref source, (decimal) value!);
                break;
            case TypeCode.Double:
                ((MethodWithoutReturnValue.StructMethod<TSource, double>) m_SetMethod)(ref source, (double) value!);
                break;
            case TypeCode.Int16:
                ((MethodWithoutReturnValue.StructMethod<TSource, short>) m_SetMethod)(ref source, (short) value!);
                break;
            case TypeCode.Int32:
                ((MethodWithoutReturnValue.StructMethod<TSource, int>) m_SetMethod)(ref source, (int) value!);
                break;
            case TypeCode.Int64:
                ((MethodWithoutReturnValue.StructMethod<TSource, long>) m_SetMethod)(ref source, (long) value!);
                break;
            case TypeCode.SByte:
                ((MethodWithoutReturnValue.StructMethod<TSource, sbyte>) m_SetMethod)(ref source, (sbyte) value!);
                break;
            case TypeCode.Single:
                ((MethodWithoutReturnValue.StructMethod<TSource, float>) m_SetMethod)(ref source, (float) value!);
                break;
            case TypeCode.UInt16:
                ((MethodWithoutReturnValue.StructMethod<TSource, ushort>) m_SetMethod)(ref source, (ushort) value!);
                break;
            case TypeCode.UInt32:
                ((MethodWithoutReturnValue.StructMethod<TSource, uint>) m_SetMethod)(ref source, (uint) value!);
                break;
            case TypeCode.UInt64:
                ((MethodWithoutReturnValue.StructMethod<TSource, ulong>) m_SetMethod)(ref source, (ulong) value!);
                break;
            default:
                ((IUnboxingSetWrapper<TSource>) m_StructValueSetWrapper).Set(ref source, value);
                break;
        }
    }

    /// <summary>
    /// Sets a value-type property on a class source using a strongly typed delegate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithClassSource<TSource, TValue>(TSource source, ref TValue value) where TSource : class where TValue : struct
    {
        ((MethodWithoutReturnValue.ClassMethod<TSource, TValue>) m_SetMethod)(source, value);
    }

    /// <summary>
    /// Sets a property on a class source from a boxed value.
    /// Uses typed fast paths for common primitives and wrapper fallback for other types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithClassSource<TSource>(TSource source, object? value) where TSource : class
    {
        if (m_IsEnum)
        {
            ((IUnboxingSetWrapper<TSource>) m_StructValueSetWrapper).Set(source, value);
            return;
        }

        switch (m_TypeCode)
        {
            case TypeCode.Boolean:
                ((MethodWithoutReturnValue.ClassMethod<TSource, bool>) m_SetMethod)(source, (bool) value!);
                break;
            case TypeCode.Byte:
                ((MethodWithoutReturnValue.ClassMethod<TSource, byte>) m_SetMethod)(source, (byte) value!);
                break;
            case TypeCode.Char:
                ((MethodWithoutReturnValue.ClassMethod<TSource, char>) m_SetMethod)(source, (char) value!);
                break;
            case TypeCode.DateTime:
                ((MethodWithoutReturnValue.ClassMethod<TSource, DateTime>) m_SetMethod)(source, (DateTime) value!);
                break;
            case TypeCode.Decimal:
                ((MethodWithoutReturnValue.ClassMethod<TSource, decimal>) m_SetMethod)(source, (decimal) value!);
                break;
            case TypeCode.Double:
                ((MethodWithoutReturnValue.ClassMethod<TSource, double>) m_SetMethod)(source, (double) value!);
                break;
            case TypeCode.Int16:
                ((MethodWithoutReturnValue.ClassMethod<TSource, short>) m_SetMethod)(source, (short) value!);
                break;
            case TypeCode.Int32:
                ((MethodWithoutReturnValue.ClassMethod<TSource, int>) m_SetMethod)(source, (int) value!);
                break;
            case TypeCode.Int64:
                ((MethodWithoutReturnValue.ClassMethod<TSource, long>) m_SetMethod)(source, (long) value!);
                break;
            case TypeCode.SByte:
                ((MethodWithoutReturnValue.ClassMethod<TSource, sbyte>) m_SetMethod)(source, (sbyte) value!);
                break;
            case TypeCode.Single:
                ((MethodWithoutReturnValue.ClassMethod<TSource, float>) m_SetMethod)(source, (float) value!);
                break;
            case TypeCode.UInt16:
                ((MethodWithoutReturnValue.ClassMethod<TSource, ushort>) m_SetMethod)(source, (ushort) value!);
                break;
            case TypeCode.UInt32:
                ((MethodWithoutReturnValue.ClassMethod<TSource, uint>) m_SetMethod)(source, (uint) value!);
                break;
            case TypeCode.UInt64:
                ((MethodWithoutReturnValue.ClassMethod<TSource, ulong>) m_SetMethod)(source, (ulong) value!);
                break;
            default:
                ((IUnboxingSetWrapper<TSource>) m_StructValueSetWrapper).Set(source, value);
                break;
        }
    }

    /// <summary>
    /// Defines boxed get operations for both by-ref and by-value source call shapes.
    /// </summary>
    private interface IBoxingGetWrapper<TSource>
    {
        /// <summary>Gets from a by-ref source.</summary>
        public object Get(ref TSource source);

        /// <summary>Gets from a by-value source.</summary>
        public object Get(TSource source);
    }

    /// <summary>
    /// Defines unboxed set operations for both by-ref and by-value source call shapes.
    /// </summary>
    private interface IUnboxingSetWrapper<TSource>
    {
        /// <summary>Sets on a by-ref source.</summary>
        public void Set(ref TSource source, object? value);

        /// <summary>Sets on a by-value source.</summary>
        public void Set(TSource source, object? value);
    }

    /// <summary>
    /// Wrapper for class-source property setters.
    /// </summary>
    private class ClassSourceSetWrapper<TSource, TResult>(Delegate getDel) : IUnboxingSetWrapper<TSource> where TSource : class
    {
        /// <summary>
        /// Strongly typed delegate used to set the property value on a class source.
        /// </summary>
        private readonly MethodWithoutReturnValue.ClassMethod<TSource, TResult> m_GetDel = (MethodWithoutReturnValue.ClassMethod<TSource, TResult>) getDel;

        /// <inheritdoc/>
        public void Set(ref TSource source, object? value) => throw new NotImplementedException();

        /// <inheritdoc/>
        public void Set(TSource source, object? value) => m_GetDel(source, (TResult) value!);
    }

    /// <summary>
    /// Wrapper for struct-source property setters.
    /// </summary>
    private class StructSourceSetWrapper<TSource, TValue>(Delegate getDel) : IUnboxingSetWrapper<TSource> where TSource : struct
    {
        /// <summary>
        /// Strongly typed delegate used to set the property value on a struct source.
        /// </summary>
        private readonly MethodWithoutReturnValue.StructMethod<TSource, TValue> m_GetDel = (MethodWithoutReturnValue.StructMethod<TSource, TValue>) getDel;

        /// <inheritdoc/>
        public void Set(ref TSource source, object? value) => m_GetDel(ref source, (TValue) value!);

        /// <inheritdoc/>
        public void Set(TSource source, object? value) => throw new NotImplementedException();
    }

    /// <summary>
    /// Represents a wrapper for the get method of a property when the source type is a class.
    /// </summary>
    private class ClassSourceGetWrapper<TSource, TResult>(Delegate getDel) : IBoxingGetWrapper<TSource> where TSource : class
    {
        /// <summary>
        /// This delegate is used to get the value of the property from the class source type
        /// </summary>
        private readonly MethodWithReturnValue.ClassMethod<TSource, TResult> m_GetDel = (MethodWithReturnValue.ClassMethod<TSource, TResult>) getDel;

        /// <inheritdoc/>
        public object Get(ref TSource source) => throw new NotImplementedException();

        /// <inheritdoc/>
        public object Get(TSource source) => m_GetDel(source);
    }

    /// <summary>
    /// Represents a wrapper for the get method of a property when the source type is a struct.
    /// </summary>
    private class StructSourceGetWrapper<TSource, TResult>(Delegate getDel) : IBoxingGetWrapper<TSource> where TSource : struct
    {
        /// <summary>
        /// This delegate is used to get the value of the property from the struct source type.
        /// </summary>
        private readonly MethodWithReturnValue.StructMethod<TSource, TResult> m_GetDel = (MethodWithReturnValue.StructMethod<TSource, TResult>) getDel;

        /// <inheritdoc/>
        public object Get(ref TSource source) => m_GetDel(ref source);

        /// <inheritdoc/>
        public object Get(TSource source) => throw new NotImplementedException();
    }
}