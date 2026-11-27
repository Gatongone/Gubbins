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
    /// <param name="minLod">The smallest mip level the sampler reads.</param>
    /// <param name="maxLod">The largest mip level the sampler reads.</param>
    /// <param name="mipLodBias">The bias added to a mip level before it is clamped.</param>
    /// <param name="maxAnisotropy">The largest anisotropy, where one is isotropic sampling.</param>
    public GpuSampler(
        GpuTextureFilter filter = GpuTextureFilter.Nearest,
        GpuAddressMode address = GpuAddressMode.Clamp,
        float minLod = 0,
        float maxLod = float.MaxValue,
        float mipLodBias = 0,
        uint maxAnisotropy = 1)
    {
        if (maxAnisotropy == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAnisotropy), maxAnisotropy, "Anisotropy is at least one.");
        }

        Filter = filter;
        Address = address;
        MinLod = minLod;
        MaxLod = maxLod;
        MipLodBias = mipLodBias;
        MaxAnisotropy = maxAnisotropy;
    }

    /// <summary>
    /// Gets how a coordinate between pixels is resolved.
    /// </summary>
    public GpuTextureFilter Filter { get; }

    /// <summary>
    /// Gets how a coordinate outside the image is resolved.
    /// </summary>
    public GpuAddressMode Address { get; }

    /// <summary>
    /// Gets the smallest mip level the sampler reads.
    /// </summary>
    public float MinLod { get; }

    /// <summary>
    /// Gets the largest mip level the sampler reads.
    /// </summary>
    public float MaxLod { get; }

    /// <summary>
    /// Gets the bias added to a mip level before it is clamped.
    /// </summary>
    public float MipLodBias { get; }

    /// <summary>
    /// Gets the largest anisotropy, where one is isotropic sampling.
    /// </summary>
    public uint MaxAnisotropy { get; }
}
