using System;
using Godot;

namespace Gubbins.Entities;

/// <summary>
/// Represents the local scale of an entity along the X-axis in local space.
/// </summary>
[Serializable]
public struct ScaleX : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.X =  Value;
        snapshot.Flags   |= TransformSnapshot.SCL_X;
    }
}

/// <summary>
/// Represents the local scale of an entity along the Y-axis in local space.
/// </summary>
[Serializable]
public struct ScaleY : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.Y =  Value;
        snapshot.Flags   |= TransformSnapshot.SCL_Y;
    }
}

/// <summary>
/// Represents the local scale of an entity along the Z-axis in local space.
/// </summary>
[Serializable]
public struct ScaleZ : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.Z =  Value;
        snapshot.Flags   |= TransformSnapshot.SCL_Z;
    }
}

/// <summary>
/// Represents the local scale of an entity in local space, with separate components for the X and Y axes.
/// </summary>
[Serializable]
public struct ScaleXY : IComponent, ITransformComponent
{
    public float X;
    public float Y;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.X =  X;
        snapshot.Scale.Y =  Y;
        snapshot.Flags   |= TransformSnapshot.SCL_X | TransformSnapshot.SCL_Y;
    }
}

/// <summary>
/// Represents the local scale of an entity in local space, with separate components for the X and Z axes.
/// </summary>
[Serializable]
public struct ScaleXZ : IComponent, ITransformComponent
{
    public float X;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.X =  X;
        snapshot.Scale.Z =  Z;
        snapshot.Flags   |= TransformSnapshot.SCL_X | TransformSnapshot.SCL_Z;
    }
}

/// <summary>
/// Represents the local scale of an entity in local space, with separate components for the Y and Z axes.
/// </summary>
[Serializable]
public struct ScaleYZ : IComponent, ITransformComponent
{
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale.Y =  Y;
        snapshot.Scale.Z =  Z;
        snapshot.Flags   |= TransformSnapshot.SCL_Y | TransformSnapshot.SCL_Z;
    }
}

/// <summary>
/// Represents the local scale of an entity in local space, with separate components for each axis (X, Y, Z).
/// </summary>
[Serializable]
public struct Scale : IComponent, ITransformComponent
{
    public float X;
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Scale =  new Vector3(X, Y, Z);
        snapshot.Flags |= TransformSnapshot.HAS_SCALE;
    }
}
