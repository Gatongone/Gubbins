namespace Gubbins.Pipeline;

/// <summary>
/// A pipeline that simply returns the input as the output, without performing any transformations.
/// This can be useful as a default or placeholder pipeline when no processing is needed, or as a base case for building more complex pipelines.
/// </summary>
/// <typeparam name="T">The type of the input and output of the pipeline.</typeparam>
public class IdentityPipeline<T> : IPipeline<T, T>
{
    public int Count => 0;
    public T Start(T input) => input;
    public static readonly IdentityPipeline<T> Default = new();
}