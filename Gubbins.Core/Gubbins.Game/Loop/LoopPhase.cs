namespace Gubbins.Game;

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