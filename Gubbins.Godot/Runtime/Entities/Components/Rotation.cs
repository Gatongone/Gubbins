using Godot;

namespace Gubbins.Entities;

/// <summary>
/// Represents the rotation of an entity around the X-axis in local space, defined by its X Euler angle (degrees).
/// </summary>
public struct RotationX : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.X =  Value;
        snapshot.Flags   |= TransformSnapshot.ROT_X;
    }
}

/// <summary>
/// Represents the rotation of an entity around the Y-axis in local space, defined by its Y Euler angle (degrees).
/// </summary>
public struct RotationY : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.Y =  Value;
        snapshot.Flags   |= TransformSnapshot.ROT_Y;
    }
}

/// <summary>
/// Represents the rotation of an entity around the Z-axis in local space, defined by its Z Euler angle (degrees).
/// </summary>
public struct RotationZ : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.Z =  Value;
        snapshot.Flags   |= TransformSnapshot.ROT_Z;
    }
}

/// <summary>
/// Represents the rotation of an entity in local space, defined by its X and Y Euler angles (degrees).
/// </summary>
public struct RotationXY : IComponent, ITransformComponent
{
    public float X;
    public float Y;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.X =  X;
        snapshot.Euler.Y =  Y;
        snapshot.Flags   |= TransformSnapshot.ROT_X | TransformSnapshot.ROT_Y;
    }
}

/// <summary>
/// Represents the rotation of an entity in local space, defined by its X and Z Euler angles (degrees).
/// </summary>
public struct RotationXZ : IComponent, ITransformComponent
{
    public float X;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.X =  X;
        snapshot.Euler.Z =  Z;
        snapshot.Flags   |= TransformSnapshot.ROT_X | TransformSnapshot.ROT_Z;
    }
}

/// <summary>
/// Represents the rotation of an entity in local space, defined by its Y and Z Euler angles (degrees).
/// </summary>
public struct RotationYZ : IComponent, ITransformComponent
{
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler.Y =  Y;
        snapshot.Euler.Z =  Z;
        snapshot.Flags   |= TransformSnapshot.ROT_Y | TransformSnapshot.ROT_Z;
    }
}

/// <summary>
/// Represents the rotation of an entity in local space, defined by its Euler angles (X, Y, Z in degrees).
/// </summary>
public struct Rotation : IComponent, ITransformComponent
{
    public float X;
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Euler =  new Vector3(X, Y, Z);
        snapshot.Flags |= TransformSnapshot.HAS_ROTATION;
    }
}

/// <summary>
/// Represents the full orientation of an entity in local space as a quaternion (X, Y, Z, W).
/// Unlike <see cref="Rotation"/> and its axis subsets, this is written to the transform verbatim with
/// no Euler conversion, so it composes and interpolates without gimbal-lock artifacts. Use it for
/// arbitrary 3D orientation; use the Euler variants for designer-authored or single-axis spin.
/// </summary>
public struct Orientation : IComponent, ITransformComponent
{
    public float X;
    public float Y;
    public float Z;
    public float W;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Orientation =  new Quaternion(X, Y, Z, W);
        snapshot.Flags       |= TransformSnapshot.HAS_ORIENTATION;
    }
}
