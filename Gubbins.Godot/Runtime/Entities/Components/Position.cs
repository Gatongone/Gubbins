using System;
using Godot;

namespace Gubbins.Entities;

/// <summary>
/// Represents the position of an entity in local space along the X-axis, defined by its X coordinate.
/// </summary>
[Serializable]
public struct PositionX : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.X =  Value;
        snapshot.Flags      |= TransformSnapshot.POS_X;
    }
}

/// <summary>
/// Represents the position of an entity in local space along the Y-axis, defined by its Y coordinate.
/// </summary>
[Serializable]
public struct PositionY : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.Y =  Value;
        snapshot.Flags      |= TransformSnapshot.POS_Y;
    }
}

/// <summary>
/// Represents the position of an entity in local space along the Z-axis, defined by its Z coordinate.
/// </summary>
[Serializable]
public struct PositionZ : IComponent, ITransformComponent
{
    public float Value;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.Z =  Value;
        snapshot.Flags      |= TransformSnapshot.POS_Z;
    }
}

/// <summary>
/// Represents the position of an entity in local space, defined by its X and Y coordinates.
/// </summary>
[Serializable]
public struct PositionXY : IComponent, ITransformComponent
{
    public float X;
    public float Y;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.X =  X;
        snapshot.Position.Y =  Y;
        snapshot.Flags      |= TransformSnapshot.POS_X | TransformSnapshot.POS_Y;
    }
}

/// <summary>
/// Represents the position of an entity in local space, defined by its X and Z coordinates.
/// </summary>
[Serializable]
public struct PositionXZ : IComponent, ITransformComponent
{
    public float X;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.X =  X;
        snapshot.Position.Z =  Z;
        snapshot.Flags      |= TransformSnapshot.POS_X | TransformSnapshot.POS_Z;
    }
}

/// <summary>
/// Represents the position of an entity in local space, defined by its Y and Z coordinates.
/// </summary>
[Serializable]
public struct PositionYZ : IComponent, ITransformComponent
{
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position.Y =  Y;
        snapshot.Position.Z =  Z;
        snapshot.Flags      |= TransformSnapshot.POS_Y | TransformSnapshot.POS_Z;
    }
}

/// <summary>
/// Represents the position of an entity in local space, defined by its X, Y, and Z coordinates.
/// </summary>
[Serializable]
public struct Position : IComponent, ITransformComponent
{
    public float X;
    public float Y;
    public float Z;

    void ITransformComponent.Write(ref TransformSnapshot snapshot)
    {
        snapshot.Position =  new Vector3(X, Y, Z);
        snapshot.Flags    |= TransformSnapshot.HAS_POSITION;
    }
}
