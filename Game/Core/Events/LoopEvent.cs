using Gubbins.Events;

namespace Gubbins.Game;

/// <summary>
/// Abstraction that decouples <see cref="LoopEvent"/> from engine-specific loop APIs.
/// Each engine provides its own implementation and sets <see cref="LoopEvent.Registrar"/> at startup.
/// </summary>
public interface ILoopPhaseRegistrar
{
    void Register(LoopPhase phase, Action<float> handler);
    void Unregister(LoopPhase phase, Action<float> handler);
}

/// <summary>
/// Identifies a phase in the engine game loop. Each engine maps these values to its native loop phases.
/// </summary>
public enum LoopPhase
{
    /// <summary>Early processing (Unity: EarlyUpdate, Godot: Preprocess).</summary>
    Early,

    /// <summary>Main frame processing (Unity: Update, Godot: Process).</summary>
    Frame,

    /// <summary>Lately processing (Unity: PreLateUpdate, Godot: Postprocess).</summary>
    Lately,

    /// <summary>Physics / fixed-timestep processing (Unity: FixedUpdate, Godot: Physics).</summary>
    Physics
}

/// <summary>
/// Engine-agnostic base class for game loop phase events.
/// Publishes deltaTime to subscribers during the engine's loop phase.
/// </summary>
public class LoopEvent : IEventSubscribable<float>
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

/// <summary>
/// Provides engine-agnostic strongly-typed event wrappers for game loop phases.
/// Use with <c>[Event(typeof(LoopEvents.Frame))]</c> in business code
/// without depending on Unity or Godot assemblies.
/// </summary>
public static class LoopEvents
{
    public class Early() : LoopEvent(LoopPhase.Early);

    public class Frame() : LoopEvent(LoopPhase.Frame);

    public class Lately() : LoopEvent(LoopPhase.Lately);

    public class Physics() : LoopEvent(LoopPhase.Physics);
}