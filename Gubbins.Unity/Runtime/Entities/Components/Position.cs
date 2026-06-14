using System;

namespace Gubbins.Entities
{
    /// <summary>
    /// Represents the position of an entity in local space along the X-axis, defined by its X coordinate.
    /// </summary>
    [Serializable]
    public struct PositionX : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the position of an entity in local space along the Y-axis, defined by its Y coordinate.
    /// </summary>
    [Serializable]
    public struct PositionY : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the position of an entity in local space along the Z-axis, defined by its Z coordinate.
    /// </summary>
    [Serializable]
    public struct PositionZ : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the position of an entity in local space, defined by its X and Y coordinates.
    /// </summary>
    [Serializable]
    public struct PositionXY : IComponent
    {
        public float X;
        public float Y;
    }

    /// <summary>
    /// Represents the position of an entity in local space, defined by its X and Z coordinates.
    /// </summary>
    [Serializable]
    public struct PositionXZ : IComponent
    {
        public float X;
        public float Z;
    }

    /// <summary>
    /// Represents the position of an entity in local space, defined by its Y and Z coordinates.
    /// </summary>
    [Serializable]
    public struct PositionYZ : IComponent
    {
        public float Y;
        public float Z;
    }

    /// <summary>
    /// Represents the position of an entity in local space, defined by its X, Y, and Z coordinates.
    /// </summary>
    [Serializable]
    public struct Position : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}