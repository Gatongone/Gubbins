namespace Gubbins.Game;

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