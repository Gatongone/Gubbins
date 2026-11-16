namespace Gubbins.Pipeline;

/// <summary>
/// A pipeline that simply returns the input as the output, without performing any transformations.
/// This can be useful as a default or placeholder pipeline when no processing is needed, or as a base case for building more complex pipelines.
/// </summary>
/// <typeparam name="T">The type of the input and output of the pipeline.</typeparam>
public class IdentityPipeline<T> : IPipeline<T, T>
{
    /// <summary>
    /// Gets the number of phases in the pipeline, which is always 0 since this pipeline does not contain any processing phases and simply returns the input as the output.
    /// </summary>
    public int Count => 0;

    /// <inheritdoc/>
    /// <remarks>
    /// Since this pipeline does not perform any processing, the Start method simply returns the input as the output, effectively acting as an identity function.
    /// This means that whatever input is provided to the Start method will be returned unchanged,
    /// making it a useful component in scenarios where a no-operation pipeline is needed or when building more complex pipelines that may require a default behavior.
    /// </remarks>
    public T Start(T input) => input;

    /// <summary>
    /// Gets the singleton instance of the <see cref="IdentityPipeline{T}"/>. This instance can be used whenever a pipeline that performs no processing is needed, providing a convenient and efficient way to represent a no-operation pipeline in various contexts.
    /// </summary>
    public static readonly IdentityPipeline<T> Default = new();

    /// <summary>
    /// Gets the current state of the pipeline, which is always <see cref="PipeLineState.Completed"/> since this pipeline does not perform any processing and immediately returns the input as output.
    /// </summary>
    public PipeLineState State => PipeLineState.Completed;
}