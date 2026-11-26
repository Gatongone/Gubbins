namespace Gubbins.Graphic;

/// <summary>
/// How a sampled texture chooses a pixel.
/// </summary>
public enum GpuTextureFilter : byte
{
    /// <summary>
    /// The nearest pixel.
    /// </summary>
    Nearest = 0,

    /// <summary>
    /// A blend of the four surrounding pixels.
    /// </summary>
    Linear = 1
}

/// <summary>
/// How a sampled texture handles a coordinate outside the image.
/// </summary>
public enum GpuAddressMode : byte
{
    /// <summary>
    /// The edge pixel is repeated.
    /// </summary>
    Clamp = 0,

    /// <summary>
    /// The image is tiled.
    /// </summary>
    Repeat = 1,

    /// <summary>
    /// The image is tiled and mirrored.
    /// </summary>
    Mirror = 2
}

/// <summary>
/// The description of a sampler the device creates.
/// </summary>
public readonly struct GpuSampler
{
    /// <param name="filter">How a coordinate between pixels is resolved.</param>
    /// <param name="address">How a coordinate outside the image is resolved.</param>
    public GpuSampler(GpuTextureFilter filter = GpuTextureFilter.Nearest, GpuAddressMode address = GpuAddressMode.Clamp)
    {
        Filter = filter;
        Address = address;
    }

    /// <summary>
    /// Gets how a coordinate between pixels is resolved.
    /// </summary>
    public GpuTextureFilter Filter { get; }

    /// <summary>
    /// Gets how a coordinate outside the image is resolved.
    /// </summary>
    public GpuAddressMode Address { get; }
}
