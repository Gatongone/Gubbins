using System.Diagnostics.CodeAnalysis;
using Gubbins.Context;

/// <summary>
/// Extensions for <see cref="INotMultitonBindingDecorator"/>.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class BindingDecoratorExtensions
{
    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

    /// <summary>
    /// <inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="INotMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static INotMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this INotMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

    /// <summary>
    /// <inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IMultitonBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IMultitonBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this IMultitonBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

    /// <summary>
    /// <inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/>
    /// </summary>
    /// <returns><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></returns>
    /// <exception cref="ArgumentException"><inheritdoc cref="IBindingDecorator.BindTo(System.Type[])"/></exception>
    public static IBindingDecorator BindTo<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this IBindingDecorator decorator)
        => decorator.BindTo(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));
}