namespace Gubbins.Pipeline;

/// <summary>
/// Provides extension methods for building pipelines in a fluent manner, allowing for the chaining
/// of phases to create complex processing pipelines with ease. These extensions enable developers to
/// construct pipelines by simply providing functions that define the transformation from one phase to
/// the next, without needing to explicitly implement the <see cref="IPhase{TIn, TOut}"/> interface for each step.
/// This promotes a more concise and readable way to build pipelines, making it easier to understand the flow of data
/// through the various transformations.
/// </summary>
public static class PipelineBuilderExtensions
{
    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent, TNext>(this PipelineBuilder<TInitial, TCurrent> builder, Func<TCurrent, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2)> builder, Func<TCurrent1, TCurrent2, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TCurrent4, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3, TCurrent4)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Adds a new phase to the pipeline using a simple function that transforms the output of the current phase into the input of the next phase.
    /// </summary>
    /// <param name="builder">The current pipeline builder to which the new phase will be added.</param>
    /// <param name="onPhase">A function that takes the output of the current phase and produces the input for the next phase.</param>
    /// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
    /// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="onPhase"/>.</typeparam>
    /// <returns>A new <see cref="PipelineBuilder{TInitial, TNext}"/> that includes the newly added phase.</returns>
    public static PipelineBuilder<TInitial, TNext> Next<TInitial, TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TCurrent8, TNext>(this PipelineBuilder<TInitial, (TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TCurrent8)> builder, Func<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TCurrent8, TNext> onPhase)
    {
        var phase = new ImplicitPhase<TCurrent1, TCurrent2, TCurrent3, TCurrent4, TCurrent5, TCurrent6, TCurrent7, TCurrent8, TNext>(onPhase);
        return builder.Next(phase);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn, TOut> : IPhase<TIn, TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start(TIn input) => m_Func(input);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TOut> : IPhase<(TIn1, TIn2), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2) input) => m_Func(input.Item1, input.Item2);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TOut> : IPhase<(TIn1, TIn2, TIn3), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3) input) => m_Func(input.Item1, input.Item2, input.Item3);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TIn4, TOut> : IPhase<(TIn1, TIn2, TIn3, TIn4), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TIn4, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TIn4, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3, TIn4) input) => m_Func(input.Item1, input.Item2, input.Item3, input.Item4);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> : IPhase<(TIn1, TIn2, TIn3, TIn4, TIn5), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3, TIn4, TIn5) input) => m_Func(input.Item1, input.Item2, input.Item3, input.Item4, input.Item5);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> : IPhase<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3, TIn4, TIn5, TIn6) input) => m_Func(input.Item1, input.Item2, input.Item3, input.Item4, input.Item5, input.Item6);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut> : IPhase<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7) input) => m_Func(input.Item1, input.Item2, input.Item3, input.Item4, input.Item5, input.Item6, input.Item7);
    }

    /// <summary>
    /// Represents a phase in the processing pipeline that is defined by a simple function, allowing for a concise way to create phases
    /// without needing to implement the <see cref="IPhase{TIn, TOut}"/> interface explicitly.
    /// </summary>
    /// <typeparam name="TIn">The type of the input to the phase, which will be transformed by the function provided in the constructor.</typeparam>
    /// <typeparam name="TOut">The type of the output from the phase, which will be the result of applying the function to the input.</typeparam>
    private sealed class ImplicitPhase<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut> : IPhase<(TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8), TOut>
    {
        /// <summary>
        /// The function that defines the transformation from the input of type <see cref="TIn"/> to the output of type <see cref="TOut"/>.
        /// </summary>
        private readonly Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut> m_Func;

        /// <param name="func">The function that will be used to transform the input to the output for this phase.</param>
        public ImplicitPhase(Func<TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8, TOut> func) => m_Func = func;

        /// <summary>
        /// Starts the phase by applying the function to the given input, producing the output for this phase.
        /// </summary>
        /// <param name="input">The input to be processed by this phase, which will be transformed by the function provided in the constructor.</param>
        /// <returns>The output produced by applying the function to the input, which will be the result of this phase in the pipeline.</returns>
        public TOut Start((TIn1, TIn2, TIn3, TIn4, TIn5, TIn6, TIn7, TIn8) input) => m_Func(input.Item1, input.Item2, input.Item3, input.Item4, input.Item5, input.Item6, input.Item7, input.Item8);
    }
}

/// <summary>
/// Represents a builder for constructing a processing pipeline in a fluent manner, allowing for the chaining of phases to create
/// complex pipelines with ease. This struct provides methods to add new phases to the pipeline and ultimately build the final
/// <see cref="IPipeline{TInitial, TCurrent}"/> instance that can be executed to process data through the defined transformations.
/// </summary>
/// <typeparam name="TInitial">The type of the initial input to the pipeline.</typeparam>
/// <typeparam name="TCurrent">The type of the output from the current phase, which will be the input to the next phase.</typeparam>
public readonly struct PipelineBuilder<TInitial, TCurrent>
{
    /// <summary>
    /// The underlying pipeline that is being built, which consists of the phases added so far.
    /// This field is used to keep track of the current state of the pipeline as new phases are added.
    /// </summary>
    private readonly IPipeline<TInitial, TCurrent> m_Pipeline;

    /// <summary>
    /// The count of phases that have been added to the pipeline so far. This is used for informational purposes
    /// and can be accessed through the <see cref="IPipeline{TIn, TOut}.Count"/> property of the built pipeline.
    /// </summary>
    private readonly int m_Count;

    /// <param name="pipeline">The initial pipeline to start building from, which can be an identity pipeline or a previously built pipeline.</param>
    /// <param name="count">The initial count of phases in the pipeline, which is typically 0 for a new pipeline or the count from a previously built pipeline.</param>
    internal PipelineBuilder(IPipeline<TInitial, TCurrent> pipeline, int count)
    {
        m_Pipeline = pipeline;
        m_Count    = count;
    }

    /// <summary>
    /// Adds a new phase to the pipeline using the provided <see cref="IPhase{TCurrent, TNext}"/> instance,
    /// which defines the transformation from the current phase's output to the next phase's output.
    /// </summary>
    /// <param name="phase">The phase to be added to the pipeline, which must implement the <see cref="IPhase{TCurrent, TNext}"/> interface.</param>
    /// <typeparam name="TNext">The type of the output from the next phase, which will be the result of the transformation defined by <paramref name="phase"/>.</typeparam>
    /// <returns></returns>
    public PipelineBuilder<TInitial, TNext> Next<TNext>(IPhase<TCurrent, TNext> phase)
    {
        var step = new PipelineStep<TInitial, TCurrent, TNext>(m_Pipeline, phase);
        return new PipelineBuilder<TInitial, TNext>(step, m_Count + 1);
    }

    /// <summary>
    /// Builds the final <see cref="IPipeline{TInitial, TCurrent}"/> instance that represents the complete
    /// processing pipeline with all the phases added so far.
    /// </summary>
    /// <returns>The built <see cref="IPipeline{TInitial, TCurrent}"/> instance that can be executed to process data through the defined transformations.</returns>
    public IPipeline<TInitial, TCurrent> Build() => m_Pipeline;
}

/// <summary>
/// Represents a single step in the processing pipeline, where an input of type <see cref="TIn"/> is transformed into an output
/// of type <see cref="TOut"/> through a specific phase defined by the <see cref="IPhase{TMid, TOut}"/> interface.
/// </summary>
/// <param name="previous">The previous pipeline that produces an output of type <see cref="TMid"/>, which will be the input to the current phase.</param>
/// <param name="phase">The phase that defines the transformation from the output of the previous pipeline to the output of the current step.</param>
/// <typeparam name="TIn">The type of the initial input to the pipeline, which will be processed through the previous pipeline and then transformed by the current phase.</typeparam>
/// <typeparam name="TMid">The type of the intermediate output produced by the previous pipeline, which will be the input to the current phase.</typeparam>
/// <typeparam name="TOut">The type of the final output produced by the current phase, which will be the result of processing the initial input through the previous pipeline and then transforming it with the current phase.</typeparam>
public sealed class PipelineStep<TIn, TMid, TOut> : IPipeline<TIn, TOut>
{
    /// <summary>
    /// The previous pipeline that produces an output of type <see cref="TMid"/>, which will be the input to the current phase.
    /// This allows for the chaining of multiple phases in the pipeline, where each phase takes the output of the previous phase as its input and produces a new output that can be further processed by subsequent phases.
    /// </summary>
    private readonly IPipeline<TIn, TMid> m_Previous;

    /// <summary>
    /// The phase that defines the transformation from the output of the previous pipeline to the output of the current step.
    /// This phase is responsible for taking the intermediate output produced by the previous pipeline and transforming it into the final output for this step in the pipeline.
    /// </summary>
    private readonly IPhase<TMid, TOut> m_Phase;

    /// <inheritdoc/>
    public int Count => m_Previous.Count + 1;

    /// <inheritdoc/>
    public TOut Start(TIn input) => m_Phase.Start(m_Previous.Start(input));

    /// <param name="previous">The previous pipeline that produces an output of type <see cref="TMid"/>, which will be the input to the current phase.</param>
    /// <param name="phase">The phase that defines the transformation from the output of the previous pipeline to the output of the current step.</param>
    internal PipelineStep(IPipeline<TIn, TMid> previous, IPhase<TMid, TOut> phase)
    {
        m_Previous = previous;
        m_Phase    = phase;
    }
}