namespace Gubbins.Entities
{
    /// <summary>
    /// Represents the local scale of an entity along the X-axis in world space.
    /// </summary>
    public struct ScaleX : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the local scale of an entity along the Y-axis in world space.
    /// </summary>
    public struct ScaleY : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the local scale of an entity along the Z-axis in world space.
    /// </summary>
    public struct ScaleZ : IComponent
    {
        public float Value;
    }

    /// <summary>
    /// Represents the local scale of an entity in world space, with separate components for the X and Y axes.
    /// </summary>
    public struct ScaleXY : IComponent
    {
        public float X;
        public float Y;
    }

    /// <summary>
    /// Represents the local scale of an entity in world space, with separate components for the X and Z axes.
    /// </summary>
    public struct ScaleXZ : IComponent
    {
        public float X;
        public float Z;
    }

    /// <summary>
    /// Represents the local scale of an entity in world space, with separate components for the Y and Z axes.
    /// </summary>
    public struct ScaleYZ : IComponent
    {
        public float Y;
        public float Z;
    }

    /// <summary>
    /// Represents the local scale of an entity in world space, with separate components for each axis (X, Y, Z).
    /// </summary>
    public struct Scale : IComponent
    {
        public float X;
        public float Y;
        public float Z;
    }
}