using Godot;
using Godot.Collections;
using Gubbins.Context;
using Gubbins.Enhance;

namespace Gubbins.Pipeline;

/// <summary>
/// Represents a game pipeline that manages the execution of event listeners within a specified context.
/// The GamePipeline can be configured to automatically start when the application starts or can be started manually.
/// It maintains the state of the pipeline and ensures that event listeners are registered and executed in the correct order.
/// </summary>
[GlobalClass, Tool]
public partial class GamePipeline : Godot.Resource, IPipeline
{
    /// <summary>
    /// Indicates whether the GamePipeline should automatically start when the application starts.
    /// If set to true, the Start method will be called during the OnEnable phase, allowing the pipeline to begin execution immediately. If set to false,
    /// the Start method must be called manually to initiate the pipeline.
    /// </summary>
    [Export] private bool AutoStart;

    /// <summary>
    /// The context reference for the GamePipeline. This context will be used to register the event listeners defined in the pipeline.
    /// </summary>
    [Export] private SerializedReference<IContext> Context;

    /// <summary>
    /// The list of event listeners to register with the context.
    /// These listeners will be executed in the order they are defined in the array.
    /// </summary>
    [Export] private Array<SerializedReference<IEventListener>> Listeners;

    /// <summary>
    /// A list of instantiated event listeners that have been registered with the context.
    /// </summary>
    private readonly List<IEventListener> m_ListenerInstances = [];

    /// <inheritdoc/>
    public PipeLineState State { get; private set; }

    /// <inheritdoc/>
    public override void _Notification(int what)
    {
        if (what == NotificationPostinitialize && AutoStart)
        {
            Start();
        }
    }

    /// <summary>
    /// Starts the GamePipeline by registering the event listeners with the context and transitioning the pipeline state to Running.
    /// If the pipeline has already been started, an <see cref="InvalidOperationException"/> is thrown to prevent multiple invocations of the Start method.
    /// If there are no listeners to register, the pipeline state is set to Completed immediately.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the Start method is called more than once during its lifecycle.</exception>
    public void Start()
    {
        if (State == PipeLineState.Running)
        {
            throw new InvalidOperationException("The GamePipeline has already been started. Please ensure that the Start method is called only once during the application lifecycle.");
        }

        if (Listeners.Count == 0)
        {
            State = PipeLineState.Completed;
            return;
        }

        var context = Context.Value;
        if (context == null)
        {
            context = Context.Value = ApplicationContext.Global;
            GD.PushWarning("The context reference for the GamePipeline is not set. Defaulting to the global application context. Please assign a valid context reference to ensure proper functionality.");
        }

        try
        {
            if (State == PipeLineState.NotStarted)
            {
                State = PipeLineState.Running;
                RegisterListeners(context);
            }
            else
            {
                State = PipeLineState.Running;
                foreach (var listener in m_ListenerInstances)
                {
                    listener.Listen(context, context);
                }
            }
        }
        catch
        {
            State = PipeLineState.Failed;
            throw;
        }
    }

    /// <summary>
    /// Registers the event listeners defined in the GamePipeline with the provided context.
    /// </summary>
    private void RegisterListeners(IContext context)
    {
        foreach (var listener in Listeners)
        {
            var targetType = listener.ExpectedType;
            // Try inject by ctor first.
            if (targetType != null && InjectCache.GetInjectConstructor(targetType) != null)
            {
                var item = context.InjectByCtor(targetType) as IEventListener;
                listener.Value = item;
                if (item != null)
                {
                    item.Listen(context, context);
                    m_ListenerInstances.Add(item);
                }
            }
            else
            {
                var item = listener.Value;
                if (item != null)
                {
                    context.Inject(item);
                    item.Listen(context, context);
                    m_ListenerInstances.Add(item);
                }
            }
        }
    }

    /// <inheritdoc/>
    Unit IPhase<Unit, Unit>.Start(Unit input)
    {
        Start();
        return input;
    }

    /// <summary>
    /// Stops the GamePipeline by clearing the registered event listeners from the context and transitioning the pipeline state to Completed.
    /// </summary>
    public void Stop()
    {
        if (State != PipeLineState.Running)
        {
            return;
        }

        var context = Context.Value;
        if (context != null)
        {
            foreach (var listener in m_ListenerInstances)
            {
                listener?.Clear(context);
            }
        }

        State = PipeLineState.Completed;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        Stop();
        base.Dispose(disposing);
    }
}