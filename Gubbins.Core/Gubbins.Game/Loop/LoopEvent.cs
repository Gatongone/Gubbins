using Gubbins.Events;

namespace Gubbins.Game;

/// <summary>
/// Engine-agnostic base class for game loop phase events.
/// Publishes deltaTime to subscribers during the engine's loop phase.
/// </summary>
public class LoopEvent : IEventSubscriable<float>
{
    public static ILoopPhaseRegistrar Registrar { get; set; } = NullRegistrar.Instance;

    private readonly LoopPhase m_Phase;
    private readonly Dictionary<IEventHandler<float>, Action<float>> m_HandlerMap = new();

    internal LoopEvent(LoopPhase phase)
    {
        m_Phase = phase;
    }

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<float> handler)
    {
        Action<float> action;
        if (handler is ActionEventHandler<float> actionHandler)
        {
            action = actionHandler.Invocation;
        }
        else
        {
            action = handler.Handle;
        }

        m_HandlerMap.Add(handler, action);
        Registrar.Register(m_Phase, action);
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<float> handler)
    {
        if (!m_HandlerMap.Remove(handler, out var action)) return false;
        Registrar.Unregister(m_Phase, action);
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        foreach (var action in m_HandlerMap.Values)
        {
            Registrar.Unregister(m_Phase, action);
        }

        m_HandlerMap.Clear();
    }

    private sealed class NullRegistrar : ILoopPhaseRegistrar
    {
        public static readonly NullRegistrar Instance = new();
        public void Register(LoopPhase phase, Action<float> handler) { }
        public void Unregister(LoopPhase phase, Action<float> handler) { }
    }
}
