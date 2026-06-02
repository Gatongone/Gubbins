using System;

namespace Gubbins.Entities
{
    /// <summary>
    /// Represents the rotation of an entity around the X-axis in world space, defined by its X Euler angle.
    /// </summary>
    [Serializable]
    public struct RotationX : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the rotation of an entity around the Y-axis in world space, defined by its Y Euler angle.
    /// </summary>
    [Serializable]
    public struct RotationY : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the rotation of an entity around the Z-axis in world space, defined by its Z Euler angle.
    /// </summary>
    [Serializable]
    public struct RotationZ : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the rotation of an entity in world space, defined by its X and Y Euler angles.
    /// </summary>
    [Serializable]
    public struct RotationXY : IComponent
    {
        public float X;
        public float Y;
    }

    /// <summary>
    /// Represents the rotation of an entity in world space, defined by its X and Z Euler angles.
    /// </summary>
    [Serializable]
    public struct RotationXZ : IComponent
    {
        public float X;
        public float Z;
    }

    /// <summary>
    /// Represents the rotation of an entity in world space, defined by its Y and Z Euler angles.
    /// </summary>
    [Serializable]
    public struct RotationYZ : IComponent
    {
        public float Y;
        public float Z;
    }

    /// <summary>
    /// Represents the rotation of an entity in world space, defined by its Euler angles (X, Y, Z).
    /// </summary>
    [Serializable]
    public struct Rotation : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}