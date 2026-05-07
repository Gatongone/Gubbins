using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

#pragma warning disable CS8603 // Possible null reference return.

namespace Gubbins.Enhance;

[SuppressMessage("ReSharper", "UnusedType.Local")]
public partial class ValuableMember
{
    private const string STRUCT_SOURCE_SET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(ValuableMember)}+StructSourceSetWrapper";
    private const string CLASS_SOURCE_SET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(ValuableMember)}+ClassSourceSetWrapper";
    private const string STRUCT_SOURCE_GET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(ValuableMember)}+StructSourceGetWrapper";
    private const string CLASS_SOURCE_GET_WRAPPER_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(ValuableMember)}+ClassSourceGetWrapper";

    private readonly object m_StructValueGetWrapper;
    private readonly Delegate m_GetMethod;

    private readonly object m_StructValueSetWrapper;
    private readonly Delegate m_SetMethod;

    private readonly TypeCode m_TypeCode;
    private readonly bool m_IsEnum;

    internal ValuableMember(PropertyInfo propertyInfo)
    {
        m_StructValueGetWrapper = null!;
        m_StructValueSetWrapper = null!;
        MemberType = propertyInfo.PropertyType;
        m_IsField = false;
        var checker = MemberType.CheckType();
        m_IsValueType = checker.IsValueType;

        var propertyType = propertyInfo.PropertyType;
        var sourceType = propertyInfo.DeclaringType;
        var getMethod = propertyInfo.GetMethod;
        var setMethod = propertyInfo.SetMethod;
        var propertyChecker = propertyType.CheckType();
        m_TypeCode  = Type.GetTypeCode(propertyType);
        m_IsEnum    = propertyChecker.IsEnum;

        if (setMethod != null)
        {
            m_SetMethod = propertyInfo.CreateSetDelegate()!;
            if (sourceType != null)
            {
                var isSourceValueType = sourceType.CheckType().IsValueType;
                var setWrapperType = isSourceValueType
                    ? Reflection.GetType(STRUCT_SOURCE_SET_WRAPPER_NAME, sourceType, propertyType)
                    : Reflection.GetType(CLASS_SOURCE_SET_WRAPPER_NAME, sourceType, propertyType);
                m_StructValueSetWrapper = Activator.CreateInstance(setWrapperType!, m_SetMethod);
            }
            else
            {
                var setWrapperType = Reflection.GetType(CLASS_SOURCE_SET_WRAPPER_NAME, propertyType);
                m_StructValueSetWrapper = Activator.CreateInstance(setWrapperType!, m_SetMethod);
            }
        }

        if (getMethod != null)
        {
            m_GetMethod = propertyInfo.CreateGetDelegate()!;
            if (sourceType != null)
            {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TResult GetWithStructSource<TSource, TResult>(ref TSource source) where TSource : struct where TResult : struct
        => ((MethodWithReturnValue.StructMethod<TSource, TResult>) m_GetMethod)(ref source);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TResult GetWithClassSource<TSource, TResult>(TSource source) where TSource : class where TResult : struct
    {
        return ((MethodWithReturnValue.ClassMethod<TSource, TResult>) m_GetMethod)(source);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithStructSource<TSource, TValue>(ref TSource source, ref TValue value) where TSource : struct where TValue : struct
    {
        ((MethodWithoutReturnValue.StructMethod<TSource, TValue>) m_SetMethod)(ref source, value);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWithClassSource<TSource, TValue>(TSource source, ref TValue value) where TSource : class where TValue : struct
    {
        ((MethodWithoutReturnValue.ClassMethod<TSource, TValue>) m_SetMethod)(source, value);
    }

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

    private interface IBoxingGetWrapper<TSource>
    {
        public object Get(ref TSource source);
        public object Get(TSource source);
    }

    private interface IUnboxingSetWrapper<TSource>
    {
        public void Set(ref TSource source, object? value);
        public void Set(TSource source, object? value);
    }

    private class ClassSourceSetWrapper<TSource, TResult> : IUnboxingSetWrapper<TSource> where TSource : class
    {
        private readonly MethodWithoutReturnValue.ClassMethod<TSource, TResult> m_GetDel;
        public ClassSourceSetWrapper(Delegate getDel) => m_GetDel = (MethodWithoutReturnValue.ClassMethod<TSource, TResult>) getDel;

        public void Set(ref TSource source, object? value) => throw new NotImplementedException();

        public void Set(TSource source, object? value) => m_GetDel(source, (TResult) value!);
    }

    private class StructSourceSetWrapper<TSource, TValue>(Delegate getDel) : IUnboxingSetWrapper<TSource> where TSource : struct
    {
        private readonly MethodWithoutReturnValue.StructMethod<TSource, TValue> m_GetDel = (MethodWithoutReturnValue.StructMethod<TSource, TValue>) getDel;
        public void Set(ref TSource source, object? value) => m_GetDel(ref source, (TValue) value!);
        public void Set(TSource source, object? value) => throw new NotImplementedException();
    }

    private class ClassSourceGetWrapper<TSource, TResult>(Delegate getDel) : IBoxingGetWrapper<TSource> where TSource : class
    {
        private readonly MethodWithReturnValue.ClassMethod<TSource, TResult> m_GetDel = (MethodWithReturnValue.ClassMethod<TSource, TResult>) getDel;
        public object Get(ref TSource source) => throw new NotImplementedException();
        public object Get(TSource source) => m_GetDel(source);
    }

    private class StructSourceGetWrapper<TSource, TResult>(Delegate getDel) : IBoxingGetWrapper<TSource> where TSource : struct
    {
        private readonly MethodWithReturnValue.StructMethod<TSource, TResult> m_GetDel = (MethodWithReturnValue.StructMethod<TSource, TResult>) getDel;
        public object Get(ref TSource source) => m_GetDel(ref source);
        public object Get(TSource source) => throw new NotImplementedException();
    }
}