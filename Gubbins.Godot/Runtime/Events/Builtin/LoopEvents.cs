using System;
using System.Collections.Generic;

#if GUBBINS_ENABLED
namespace Gubbins.Events;

/// <summary>
/// Provides strongly-typed wrappers for Godot window loop phase events.
/// </summary>
public static class LoopEvents
{
    /// <summary>
    /// Emitted during the Preprocess phase of the Godot window loop.
    /// </summary>
    public sealed class Preprocess : LoopEvent
    {
        public Preprocess() : base(GodotLoop.Kind.Preprocess) { }
    }

    /// <summary>
    /// Emitted during the Process phase of the Godot window loop.
    /// </summary>
    public sealed class Process : LoopEvent
    {
        public Process() : base(GodotLoop.Kind.Process) { }
    }

    /// <summary>
    /// Emitted during the Postprocess phase of the Godot window loop.
    /// </summary>
    public sealed class Postprocess : LoopEvent
    {
        public Postprocess() : base(GodotLoop.Kind.Postprocess) { }
    }

    /// <summary>
    /// Emitted during the Physics phase of the Godot window loop.
    /// </summary>
    public sealed class Physics : LoopEvent
    {
        public Physics() : base(GodotLoop.Kind.Physics) { }
    }
}

/// <summary>
/// Base class for WindowLoop event wrappers.
/// </summary>
public class LoopEvent : IEventSubscriable<double>
{
    private readonly GodotLoop.Kind                                    m_Kind;
    private readonly Dictionary<IEventHandler<double>, Action<double>> m_HandlerMap = new();

    internal LoopEvent(GodotLoop.Kind kind)
    {
        m_Kind = kind;
    }

    /// <inheritdoc/>
    public void Subscribe(IEventHandler<double> handler)
    {
        Action<double> action;
        if (handler is ActionEventHandler<double> actionHandler)
        {
            action = actionHandler.Invocation;
        }
        else
        {
            action = handler.Handle;
        }

        m_HandlerMap.Add(handler, action);
        GodotLoop.RegisterUpdate(m_Kind, action);
    }

    /// <inheritdoc/>
    public bool Unsubscribe(IEventHandler<double> handler)
    {
        if (!m_HandlerMap.Remove(handler, out var action)) return false;
        GodotLoop.UnregisterUpdate(m_Kind, action);
        return true;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        foreach (var wrap in m_HandlerMap.Values)
        {
            GodotLoop.UnregisterUpdate(m_Kind, wrap);
        }

        m_HandlerMap.Clear();
    }
}
#endif
