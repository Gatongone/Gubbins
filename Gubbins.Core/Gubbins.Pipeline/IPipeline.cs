using Gubbins.Enhance;

/// <summary>
/// Represents a processing phase that does not require any input and does not produce any output, effectively serving as a placeholder or a no-operation phase in a pipeline.
/// </summary>
public interface IPhase : IPhase<Unit, Unit>;

/// <summary>
/// Represents a single phase in a processing pipeline, where an input of type <see cref="TIn"/> is transformed into an output of type <see cref="TOut"/>.
/// </summary>
/// <typeparam name="TIn">The type of the input to the phase.</typeparam>
/// <typeparam name="TOut">The type of the output from the phase.</typeparam>
public interface IPhase<in TIn, out TOut>
{
    /// <summary>
    /// Starts the phase by processing the given input and producing an output.
    /// </summary>
    /// <param name="input">The input to be processed by this phase.</param>
    /// <returns>The output produced by processing the input.</returns>
    TOut Start(TIn input);
}

/// <summary>
/// Represents a processing pipeline that consists of multiple phases, where an input of type <see cref="TIn"/> is transformed into an output of type <see cref="TOut"/> through a series of steps.
/// </summary>
/// <typeparam name="TIn">The type of the input to the pipeline.</typeparam>
/// <typeparam name="TOut">The type of the output from the pipeline.</typeparam>
public interface IPipeline<in TIn, out TOut> : IPhase<TIn, TOut>
{
    PipeLineState State { get; }
}

/// <summary>
/// Represents a processing pipeline that does not require any input and does not produce any output, effectively serving as a placeholder or a no-operation pipeline in a processing sequence.
/// </summary>
public interface IPipeline : IPipeline<Unit, Unit>;

public enum PipeLineState
{
    NotStarted,
    Running,
    Completed,
    Failed
}