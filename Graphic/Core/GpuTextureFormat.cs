namespace Gubbins.Graphic;

/// <summary>
/// The pixel format of a texture.
/// </summary>
/// <remarks>
/// The values are the <c>MTLPixelFormat</c> values of the formats the Metal bridge creates today. The other backends map
/// the same members to their own formats as their texture path is added.
/// </remarks>
public enum GpuTextureFormat : uint
{
    /// <summary>
    /// Four channels, 8 bits each, unsigned normalized.
    /// </summary>
    Rgba8Unorm = 70,

    /// <summary>
    /// Four channels, 8 bits each, unsigned normalized, with the red and blue channels swapped.
    /// </summary>
    Bgra8Unorm = 80,

    /// <summary>
    /// One unsigned 32 bit integer channel.
    /// </summary>
    R32Uint = 53,

    /// <summary>
    /// One 32 bit floating point channel.
    /// </summary>
    R32Float = 55
}

/// <summary>
/// What a texture is created for.
/// </summary>
[Flags]
public enum GpuTextureUsage : uint
{
    /// <summary>
    /// The device reads it as a sampled image.
    /// </summary>
    Sampled = 1,

    /// <summary>
    /// The device reads and writes it as a storage image.
    /// </summary>
    Storage = 2,

    /// <summary>
    /// The device renders into it.
    /// </summary>
    RenderTarget = 4
}
