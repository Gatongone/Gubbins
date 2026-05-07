namespace Gubbins.Enhance;

internal static class MethodWithReturnValue
{
    public const string CLASS_METHOD_NAME  = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(MethodWithReturnValue)}+ClassMethod";
    public const string STRUCT_METHOD_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(MethodWithReturnValue)}+StructMethod";

    public delegate TResult StructMethod<TSource, out TResult>(ref TSource source) where TSource : struct;
    public delegate TResult ClassMethod<in TSource, out TResult>(TSource source) where TSource : class;
}

internal static class MethodWithoutReturnValue
{
    public const string CLASS_METHOD_NAME  = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(MethodWithoutReturnValue)}+ClassMethod";
    public const string STRUCT_METHOD_NAME = $"{nameof(Gubbins)}.{nameof(Enhance)}.{nameof(MethodWithoutReturnValue)}+StructMethod";

    public delegate void StructMethod<TSource, in TParam>(ref TSource source, TParam param) where TSource : struct;
    public delegate void ClassMethod<in TSource, in TParam>(TSource source, TParam param) where TSource : class;
}