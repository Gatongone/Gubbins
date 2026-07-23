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