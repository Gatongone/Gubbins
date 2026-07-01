namespace Gubbins.Entities;

/// <summary>
/// Implemented by transform components so the per-frame scatter can write their values and axis flags
/// into a <see cref="TransformSnapshot"/> through a constrained generic call, avoiding both per-type
/// dispatch and boxing.
/// </summary>
/// <remarks>
/// Godot has no Burst/Jobs, so the scatter runs single-threaded on the main thread (see
/// <see cref="EntityAdapter3D.SyncTransforms"/>). This is the managed equivalent of the Unity
/// <c>ScatterJob&lt;T&gt;</c> path; only the interface it dispatched through is retained here.
/// </remarks>
internal interface ITransformComponent
{
    /// <summary>Merges this component's value(s) and axis flags into <paramref name="snapshot"/>.</summary>
    void Write(ref TransformSnapshot snapshot);
}
