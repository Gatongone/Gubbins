#if GUBBINS_ENABLED
using Godot;
using Gubbins.Context;
using Gubbins.Events;
using Gubbins.Pipeline;

namespace Gubbins.Entities;

/// <summary>
/// A per-slot snapshot of an entity's transform components, gathered on the main thread and consumed by
/// <see cref="EntityAdapter3D.SyncTransforms"/>. <see cref="Flags"/> records which individual axes are
/// present — including axis-subset variants like <c>PositionZ</c> — so absent axes leave the
/// corresponding component of the bound transform untouched.
/// </summary>
internal struct TransformSnapshot
{
    internal const ushort POS_X = 1 << 0;
    internal const ushort POS_Y = 1 << 1;
    internal const ushort POS_Z = 1 << 2;
    internal const ushort ROT_X = 1 << 3;
    internal const ushort ROT_Y = 1 << 4;
    internal const ushort ROT_Z = 1 << 5;
    internal const ushort SCL_X = 1 << 6;
    internal const ushort SCL_Y = 1 << 7;
    internal const ushort SCL_Z = 1 << 8;

    // Whole-quaternion orientation; mutually exclusive with the Euler rotation axes above.
    internal const ushort HAS_ORIENTATION = 1 << 9;

    // Channel masks: set when any axis of the channel is present.
    internal const ushort HAS_POSITION = POS_X | POS_Y | POS_Z;
    internal const ushort HAS_ROTATION = ROT_X | ROT_Y | ROT_Z;
    internal const ushort HAS_SCALE    = SCL_X | SCL_Y | SCL_Z;

    public Vector3    Position;
    public Vector3    Euler;
    public Quaternion Orientation;
    public Vector3    Scale;
    public ushort     Flags;
}

/// <summary>
/// Writes the gathered <see cref="TransformSnapshot"/> values back onto a bound <see cref="Node3D"/>.
/// Each channel merges only the axes it owns onto the live transform, so an entity that carries just
/// <c>PositionZ</c> moves along Z while its X/Y stay whatever the node held.
/// </summary>
internal static class TransformWriteback
{
    public static void Apply(Node3D node, in TransformSnapshot snapshot)
    {
        var flags = snapshot.Flags;

        if ((flags & TransformSnapshot.HAS_POSITION) != 0)
        {
            var position = node.Position;
            if ((flags & TransformSnapshot.POS_X) != 0) position.X = snapshot.Position.X;
            if ((flags & TransformSnapshot.POS_Y) != 0) position.Y = snapshot.Position.Y;
            if ((flags & TransformSnapshot.POS_Z) != 0) position.Z = snapshot.Position.Z;
            node.Position = position;
        }

        // A quaternion orientation is written verbatim and takes precedence; otherwise the Euler axes are
        // merged onto the node's current rotation. Godot's RotationDegrees is already local Euler in
        // degrees, so — unlike Unity — no Quaternion.Euler round-trip is needed.
        if ((flags & TransformSnapshot.HAS_ORIENTATION) != 0)
        {
            node.Quaternion = snapshot.Orientation;
        }
        else if ((flags & TransformSnapshot.HAS_ROTATION) != 0)
        {
            var euler = node.RotationDegrees;
            if ((flags & TransformSnapshot.ROT_X) != 0) euler.X = snapshot.Euler.X;
            if ((flags & TransformSnapshot.ROT_Y) != 0) euler.Y = snapshot.Euler.Y;
            if ((flags & TransformSnapshot.ROT_Z) != 0) euler.Z = snapshot.Euler.Z;
            node.RotationDegrees = euler;
        }

        if ((flags & TransformSnapshot.HAS_SCALE) != 0)
        {
            var scale = node.Scale;
            if ((flags & TransformSnapshot.SCL_X) != 0) scale.X = snapshot.Scale.X;
            if ((flags & TransformSnapshot.SCL_Y) != 0) scale.Y = snapshot.Scale.Y;
            if ((flags & TransformSnapshot.SCL_Z) != 0) scale.Z = snapshot.Scale.Z;
            node.Scale = scale;
        }
    }

    /// <summary>
    /// 2D writeback: merges the snapshot onto a <see cref="Node2D"/>. Only the channels with a 2D meaning
    /// apply — position X/Y, the single rotation angle taken from the Z Euler channel, and scale X/Y. A
    /// component's Z position, its X/Y Euler axes and the quaternion <see cref="TransformSnapshot.Orientation"/>
    /// have no 2D analogue and are ignored.
    /// </summary>
    public static void Apply(Node2D node, in TransformSnapshot snapshot)
    {
        var flags = snapshot.Flags;

        if ((flags & TransformSnapshot.HAS_POSITION) != 0)
        {
            var position = node.Position;
            if ((flags & TransformSnapshot.POS_X) != 0) position.X = snapshot.Position.X;
            if ((flags & TransformSnapshot.POS_Y) != 0) position.Y = snapshot.Position.Y;
            node.Position = position;
        }

        // 2D has a single rotation angle in the screen plane, which maps to the Z Euler channel (set by
        // Rotation and RotationZ). Whole-quaternion orientation and the X/Y Euler axes are ignored.
        if ((flags & TransformSnapshot.ROT_Z) != 0)
        {
            node.RotationDegrees = snapshot.Euler.Z;
        }

        if ((flags & TransformSnapshot.HAS_SCALE) != 0)
        {
            var scale = node.Scale;
            if ((flags & TransformSnapshot.SCL_X) != 0) scale.X = snapshot.Scale.X;
            if ((flags & TransformSnapshot.SCL_Y) != 0) scale.Y = snapshot.Scale.Y;
            node.Scale = scale;
        }
    }
}

/// <summary>
/// Runtime driver that pumps <see cref="EntityAdapter3D.SyncTransforms"/> once per frame during the
/// Postprocess phase, after gameplay/ECS systems have mutated component data. Add it to a
/// <c>GamePipeline</c>/<c>ScenePipeline</c> listener list so the pipeline's context subscribes it.
/// </summary>
/// <remarks>
/// The <see cref="IEventListener"/> implementation (Listen/Clear) is emitted by the Gubbins source
/// generator from the <see cref="EventAttribute"/> below; this type only has to be <c>partial</c>.
/// </remarks>
public sealed partial class EntityTransformSync : IEventListener
{
    [Event(typeof(LoopEvents.Postprocess))]
    private void OnPostprocess(double delta) => EntityTransformSystem.SyncTransforms();
}
#endif
