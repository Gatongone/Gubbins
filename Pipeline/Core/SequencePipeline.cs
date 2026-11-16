namespace Gubbins.Pipeline;

/// <summary>
/// Provides static methods for creating processing pipelines in a fluent manner, allowing developers to easily construct complex
/// pipelines by chaining together transformations defined through the <see cref="PipelineBuilder{TInitial, TCurrent}"/>.
/// This class serves as a convenient entry point for building pipelines without needing to directly interact with th
/// underlying pipeline implementation details, promoting a more intuitive and streamlined approach to pipeline construction.
/// </summary>
public class SequencePipeline
{
    /// <summary>
    /// Creates a new processing pipeline by using the provided function to build the pipeline through a fluent interface.
    /// </summary>
    /// <param name="build">A function that takes a <see cref="PipelineBuilder{TIn, TIn}"/> as input and returns a <see cref="PipelineBuilder{TIn, TOut}"/> after defining the transformations for the pipeline.</param>
    /// <typeparam name="TIn">The type of the input to the pipeline, which is also the initial type for the pipeline builder.</typeparam>
    /// <typeparam name="TOut">The type of the output from the pipeline, which is determined by the transformations defined in the provided function.</typeparam>
    /// <returns></returns>
    public static IPipeline<TIn, TOut> Create<TIn, TOut>(Func<PipelineBuilder<TIn, TIn>, PipelineBuilder<TIn, TOut>> build)
    {
        var builder = new PipelineBuilder<TIn, TIn>(IdentityPipeline<TIn>.Default, 0);
        var result = build(builder);
        return result.Build();
    }
}