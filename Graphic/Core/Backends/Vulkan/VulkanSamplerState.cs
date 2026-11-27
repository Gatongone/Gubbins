namespace Gubbins.Graphic;

/// <summary>
/// A native <c>VkSampler</c> behind <see cref="GpuSamplerState"/>.
/// </summary>
/// <remarks>
/// The sampler is a standalone Vulkan object rather than a descriptor heap entry, so it is created and destroyed on its
/// own and the kernel binding step pairs it with a <see cref="VulkanTexture"/> through
/// <see cref="VulkanTexture.GetDescriptorInfo"/>.
/// </remarks>
internal sealed unsafe class VulkanSamplerState : IGpuSamplerState
{
    private readonly nint m_Device;
    private readonly nint m_Sampler;
    private bool m_Disposed;

    /// <param name="device">The <c>VkDevice</c>.</param>
    /// <param name="sampler">The sampler description.</param>
    /// <exception cref="VulkanException">Thrown when the sampler cannot be created.</exception>
    internal VulkanSamplerState(nint device, GpuSampler sampler, bool anisotropySupported = false)
    {
        m_Device = device;
        Description = sampler;

        var filter = sampler.Filter == GpuTextureFilter.Linear ? VulkanApi.FILTER_LINEAR : VulkanApi.FILTER_NEAREST;

        var address = sampler.Address switch
        {
            GpuAddressMode.Repeat => VulkanApi.SAMPLER_ADDRESS_MODE_REPEAT,
            GpuAddressMode.Mirror => VulkanApi.SAMPLER_ADDRESS_MODE_MIRRORED_REPEAT,
            _ => VulkanApi.SAMPLER_ADDRESS_MODE_CLAMP_TO_EDGE
        };

        var createInfo = new VkSamplerCreateInfo
        {
            StructureType = VulkanApi.SAMPLER_CREATE_INFO_TYPE,
            MagFilter = filter,
            MinFilter = filter,
            MipmapMode = VulkanApi.SAMPLER_MIPMAP_MODE_NEAREST,
            AddressModeU = address,
            AddressModeV = address,
            AddressModeW = address,
            MipLodBias = sampler.MipLodBias,
            AnisotropyEnable = anisotropySupported && sampler.MaxAnisotropy > 1 ? 1u : 0u,
            MaxAnisotropy = anisotropySupported ? sampler.MaxAnisotropy : 1,
            CompareEnable = 0,
            CompareOp = VulkanApi.COMPARE_OP_ALWAYS,
            MinLod = sampler.MinLod,
            MaxLod = sampler.MaxLod,
            BorderColor = VulkanApi.BORDER_COLOR_FLOAT_TRANSPARENT_BLACK,
            UnnormalizedCoordinates = 0
        };

        nint handle = 0;
        var result = VulkanApi.CreateSampler(device, &createInfo, 0, &handle);

        if (result != VulkanApi.SUCCESS || handle == 0)
        {
            throw new VulkanException($"vkCreateSampler failed with {result}.", result);
        }

        m_Sampler = handle;
    }

    /// <summary>
    /// Gets the <c>VkSampler</c>.
    /// </summary>
    public nint Sampler => m_Sampler;

    /// <summary>
    /// Gets the description the sampler was created from.
    /// </summary>
    public GpuSampler Description { get; }

    /// <summary>
    /// Releases the sampler.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        VulkanApi.DestroySampler(m_Device, m_Sampler, 0);
    }
}
