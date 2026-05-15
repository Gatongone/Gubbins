namespace Gubbins.Unsafe;

/// <summary>
/// Defines delegate shapes for instance methods that return a value.
/// </summary>
internal static class MethodWithReturnValue
{
    /// <summary>
    /// Fully qualified nested delegate type name for class-source methods.
    /// </summary>
    public const string CLASS_METHOD_NAME  = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(MethodWithReturnValue)}+ClassMethod";

    /// <summary>
    /// Fully qualified nested delegate type name for struct-source methods.
    /// </summary>
    public const string STRUCT_METHOD_NAME = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(MethodWithReturnValue)}+StructMethod";

    /// <summary>
    /// Represents an instance method on a struct source that returns a value.
    /// </summary>
    /// <typeparam name="TSource">The struct instance type.</typeparam>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="source">The source instance passed by reference.</param>
    /// <returns>The returned value.</returns>
    public delegate TResult StructMethod<TSource, out TResult>(ref TSource source) where TSource : struct;

    /// <summary>
    /// Represents an instance method on a class source that returns a value.
    /// </summary>
    /// <typeparam name="TSource">The class instance type.</typeparam>
    /// <typeparam name="TResult">The return type.</typeparam>
    /// <param name="source">The source instance.</param>
    /// <returns>The returned value.</returns>
    public delegate TResult ClassMethod<in TSource, out TResult>(TSource source) where TSource : class;
}

/// <summary>
/// Defines delegate shapes for instance methods that do not return a value.
/// </summary>
internal static class MethodWithoutReturnValue
{
    /// <summary>
    /// Fully qualified nested delegate type name for class-source methods.
    /// </summary>
    public const string CLASS_METHOD_NAME  = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(MethodWithoutReturnValue)}+ClassMethod";

    /// <summary>
    /// Fully qualified nested delegate type name for struct-source methods.
    /// </summary>
    public const string STRUCT_METHOD_NAME = $"{nameof(Gubbins)}.{nameof(Gubbins.Unsafe)}.{nameof(MethodWithoutReturnValue)}+StructMethod";

    /// <summary>
    /// Represents an instance method on a struct source that accepts a parameter and returns no value.
    /// </summary>
    /// <typeparam name="TSource">The struct instance type.</typeparam>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <param name="source">The source instance passed by reference.</param>
    /// <param name="param">The method parameter.</param>
    public delegate void StructMethod<TSource, in TParam>(ref TSource source, TParam param) where TSource : struct;

    /// <summary>
    /// Represents an instance method on a class source that accepts a parameter and returns no value.
    /// </summary>
    /// <typeparam name="TSource">The class instance type.</typeparam>
    /// <typeparam name="TParam">The parameter type.</typeparam>
    /// <param name="source">The source instance.</param>
    /// <param name="param">The method parameter.</param>
    public delegate void ClassMethod<in TSource, in TParam>(TSource source, TParam param) where TSource : class;
}