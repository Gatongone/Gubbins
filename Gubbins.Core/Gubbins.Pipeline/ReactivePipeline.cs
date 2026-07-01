using Gubbins.Context;
using Gubbins.Enhance;

namespace Gubbins.Pipeline;

/// <summary>
/// Represents a reactive pipeline that listens for events and invokes corresponding event handlers.
/// </summary>
/// <param name="context">The context used for dependency resolution and event listener registration.</param>
public class ReactivePipeline(IContext context) : IPipeline
{
    /// <summary>
    /// The list of event listeners that are registered with this pipeline.Each listener is responsible
    /// for handling events and invoking the corresponding methods when those events are published.
    /// </summary>
    private readonly List<IEventListener> m_EventListeners = [];

    /// <inheritdoc/>
    public PipeLineState State { get; private set; }

    /// <summary>
    /// Adds an event listener to the pipeline, allowing it to listen for events and invoke the corresponding
    /// event handler methods when those events are broadcasted.
    /// </summary>
    /// <param name="listener">The event listener to be added to the pipeline.</param>
    public void AddListener(IEventListener listener) => m_EventListeners.Add(listener);

    /// <summary>
    /// Adds multiple event listeners to the pipeline, allowing them to listen for events and invoke the corresponding
    /// event handler methods when those events are broadcasted.
    /// </summary>
    /// <param name="listener">The event listeners to be added to the pipeline.</param>
    public void AddListeners(params IEnumerable<IEventListener> listeners) => m_EventListeners.AddRange(listeners);

    /// <summary>
    /// Starts the reactive pipeline, registering all event listeners and allowing them to listen for events.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the pipeline is already running when this method is called.
    /// </exception>
    public void Start()
    {
        if (m_EventListeners.Count == 0)
        {
            State = PipeLineState.Completed;
            return;
        }

        if (State == PipeLineState.Running)
        {
            throw new InvalidOperationException("Pipeline is already running.");
        }

        try
        {
            State = PipeLineState.Running;
            foreach (var listener in m_EventListeners)
            {
                listener.Listen(context, context);
            }
        }
        catch
        {
            State = PipeLineState.Failed;
            throw;
        }
    }

    /// <summary>
    /// Stops the reactive pipeline, unregistering all event listeners and preventing themfrom receiving further events.
    /// </summary>
    public void Stop()
    {
        if (State != PipeLineState.Running)
        {
            return;
        }

        foreach (var listener in m_EventListeners)
        {
            listener.Clear(context);
        }

        State = PipeLineState.Completed;
    }

    /// <inheritdoc cref="Start()"/>
    Unit IPhase<Unit, Unit>.Start(Unit input)
    {
        Start();
        return input;
    }
}