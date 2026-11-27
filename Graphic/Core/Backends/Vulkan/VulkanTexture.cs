namespace Gubbins.Graphic;

/// <summary>
/// A native Vulkan texture behind <see cref="GpuTexture2D"/>.
/// </summary>
/// <remarks>
/// <para>
/// A texture the layer creates owns a <c>VkImage</c>, the memory bound to it and a <c>VkImageView</c>, plus a command pool,
/// one command buffer and a fence so an upload or a readback can be recorded and waited for on the queue the bridge
/// borrows from the engine. Staging is a buffer created for the one transfer and released when it has completed.
/// </para>
/// <para>
/// A texture the engine created is wrapped: the view and the transfer commands are this layer's, but the image and its
/// memory stay with the engine, so disposing the wrapper never destroys them. The caller passes the layout the engine left
/// the image in, because every barrier this layer records transitions from the layout it was told about.
/// </para>
/// <para>
/// Between transfers the image stays in <c>VK_IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL</c>, which is the layout a sampled
/// image binding expects, and the current layout is reported with the image view through
/// <see cref="GetDescriptorInfo"/>. Dispose the texture before the provider, which owns the device.
/// </para>
/// </remarks>
internal sealed unsafe class VulkanTexture : IGpuTexture
{
    private readonly VulkanUnifiedProvider m_Provider;
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly int m_Width;
    private readonly int m_Height;
    private readonly int m_BytesPerPixel;
    private readonly uint m_NativeFormat;
    private readonly bool m_OwnsImage;
    private nint m_Image;
    private nint m_Memory;
    private nint m_View;
    private nint m_CommandPool;
    private nint m_CommandBuffer;
    private nint m_Fence;
    private int m_Layout = VulkanApi.IMAGE_LAYOUT_UNDEFINED;
    private bool m_Disposed;

    /// <summary>
    /// Creates a texture, its memory and its view.
    /// </summary>
    /// <param name="provider">The bridge that owns the device and chooses the memory types.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <exception cref="VulkanException">Thrown when the image, its memory, the view or the transfer commands cannot be created.</exception>
    internal VulkanTexture(VulkanUnifiedProvider provider, int width, int height, GpuTextureFormat format)
    {
        m_Provider = provider;
        m_Device = provider.Device;
        m_Queue = provider.Queue;
        m_Width = width;
        m_Height = height;
        m_OwnsImage = true;
        (m_NativeFormat, m_BytesPerPixel) = DescribeFormat(format);

        try
        {
            EnsureFormatIsSupported(provider.PhysicalDevice, m_NativeFormat);
            CreateImage(provider, width, height);
            CreateView(width, height);
            CreateTransferCommands(provider.QueueFamily);
        }
        catch
        {
            ReleaseNative();
            throw;
        }
    }

    /// <summary>
    /// Wraps a <c>VkImage</c> the engine owns, creating only the view and the transfer commands.
    /// </summary>
    /// <param name="provider">The bridge that owns the device and submits the transfers.</param>
    /// <param name="image">The <c>VkImage</c>. The caller keeps ownership of it and of its memory.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format of the image.</param>
    /// <param name="layout">The <c>VkImageLayout</c> the engine left the image in.</param>
    /// <param name="ownsImage">Whether disposing the wrapper also destroys the image; the engine path passes <see langword="false"/>.</param>
    /// <exception cref="VulkanException">Thrown when the view or the transfer commands cannot be created.</exception>
    internal VulkanTexture(VulkanUnifiedProvider provider, nint image, int width, int height, GpuTextureFormat format, int layout, bool ownsImage = false)
    {
        m_Provider = provider;
        m_Device = provider.Device;
        m_Queue = provider.Queue;
        m_Width = width;
        m_Height = height;
        m_Image = image;
        m_Layout = layout;
        m_OwnsImage = ownsImage;
        (m_NativeFormat, m_BytesPerPixel) = DescribeFormat(format);

        try
        {
            EnsureFormatIsSupported(provider.PhysicalDevice, m_NativeFormat);
            CreateView(width, height);
            CreateTransferCommands(provider.QueueFamily);
        }
        catch
        {
            ReleaseNative();
            throw;
        }
    }

    /// <summary>
    /// Gets the <c>VkImage</c>.
    /// </summary>
    public nint Image => m_Image;

    /// <summary>
    /// Gets the <c>VkImageView</c> that samples the image.
    /// </summary>
    public nint ImageView => m_View;

    /// <summary>
    /// Gets the <c>VkFormat</c> of the image.
    /// </summary>
    public uint NativeFormat => m_NativeFormat;

    /// <summary>
    /// Gets the current <c>VkImageLayout</c> of the image.
    /// </summary>
    public int Layout => m_Layout;

    /// <summary>
    /// Gets the number of bytes one pixel occupies.
    /// </summary>
    public int BytesPerPixel => m_BytesPerPixel;

    /// <inheritdoc/>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        ThrowIfDisposed();

        var rowPitch = StagingRowPitch(bytesPerRow, pixels.Length);
        var size = (ulong)rowPitch * (ulong)m_Height;
        var staging = CreateStagingBuffer(size, out var memory, out var host, out var coherent);

        try
        {
            var tight = m_Width * m_BytesPerPixel;

            for (var row = 0; row < m_Height; row++)
            {
                pixels.Slice(row * bytesPerRow, tight).CopyTo(new Span<byte>(host + (row * rowPitch), tight));
            }

            // A non-coherent mapping needs the write published before the transfer engine is allowed to read it; a coherent
            // one is visible to the device once the submission is made.
            if (!coherent)
            {
                Flush(memory);
            }

            Transfer(staging, (uint)(rowPitch / m_BytesPerPixel), toImage: true);
        }
        finally
        {
            ReleaseStaging(staging, memory, host);
        }
    }

    /// <inheritdoc/>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        ThrowIfDisposed();

        var rowPitch = StagingRowPitch(bytesPerRow, pixels.Length);
        var size = (ulong)rowPitch * (ulong)m_Height;
        var staging = CreateStagingBuffer(size, out var memory, out var host, out var coherent);

        try
        {
            Transfer(staging, (uint)(rowPitch / m_BytesPerPixel), toImage: false);

            if (!coherent)
            {
                Invalidate(memory);
            }

            var tight = m_Width * m_BytesPerPixel;

            for (var row = 0; row < m_Height; row++)
            {
                new ReadOnlySpan<byte>(host + (row * rowPitch), tight).CopyTo(pixels.Slice(row * bytesPerRow, tight));
            }
        }
        finally
        {
            ReleaseStaging(staging, memory, host);
        }
    }

    /// <summary>
    /// Builds the descriptor information a sampled image binding needs, including the layout the image is in.
    /// </summary>
    /// <param name="sampler">The sampler to bind with the image.</param>
    /// <returns>The descriptor information.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the texture has been disposed.</exception>
    internal VkDescriptorImageInfo GetDescriptorInfo(VulkanSamplerState sampler)
    {
        ThrowIfDisposed();

        return new VkDescriptorImageInfo
        {
            Sampler = sampler.Sampler,
            ImageView = m_View,
            ImageLayout = m_Layout
        };
    }

    /// <summary>
    /// Releases the view and the transfer commands, plus the image and its memory when this wrapper created them.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // A submitted transfer may still be reading the image, so the device has to drain before anything is destroyed.
        VulkanApi.DeviceWaitIdle(m_Device);
        ReleaseNative();
    }

    /// <summary>
    /// Rejects a format the physical device cannot transfer and sample, instead of failing at the first draw.
    /// </summary>
    /// <param name="physicalDevice">The <c>VkPhysicalDevice</c> to ask.</param>
    /// <param name="format">The <c>VkFormat</c> to check.</param>
    /// <exception cref="VulkanException">Thrown when the optimal tiling features lack what the texture needs.</exception>
    private static void EnsureFormatIsSupported(nint physicalDevice, uint format)
    {
        const uint required =
            VulkanApi.FORMAT_FEATURE_SAMPLED_IMAGE |
            VulkanApi.FORMAT_FEATURE_TRANSFER_SOURCE |
            VulkanApi.FORMAT_FEATURE_TRANSFER_DESTINATION;

        VkFormatProperties properties;
        VulkanApi.GetPhysicalDeviceFormatProperties(physicalDevice, format, &properties);

        if ((properties.OptimalTilingFeatures & required) != required)
        {
            throw new VulkanException(
                $"The device does not support format {format} as a transferred, sampled image " +
                $"(optimal tiling features 0x{properties.OptimalTilingFeatures:X}); required 0x{required:X}.");
        }
    }

    /// <summary>
    /// Maps a pixel format to the <c>VkFormat</c> and the bytes per pixel a transfer uses.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <param name="nativeFormat">When the method returns, the <c>VkFormat</c>.</param>
    /// <param name="bytesPerPixel">When the method returns, the bytes in one pixel.</param>
    /// <returns><see langword="true"/> when the backend has a format for it; otherwise, <see langword="false"/>.</returns>
    internal static bool TryDescribeFormat(GpuTextureFormat format, out uint nativeFormat, out int bytesPerPixel)
    {
        (nativeFormat, bytesPerPixel) = format switch
        {
            GpuTextureFormat.R8Unorm => (9u, 1),       // VK_FORMAT_R8_UNORM
            GpuTextureFormat.Rgba8Unorm => (37u, 4),  // VK_FORMAT_R8G8B8A8_UNORM
            GpuTextureFormat.Bgra8Unorm => (44u, 4),  // VK_FORMAT_B8G8R8A8_UNORM
            GpuTextureFormat.R32Uint => (98u, 4),     // VK_FORMAT_R32_UINT
            GpuTextureFormat.R32Float => (100u, 4),   // VK_FORMAT_R32_SFLOAT
            _ => (0u, 0)
        };

        return nativeFormat != 0;
    }

    /// <summary>
    /// Maps a pixel format to the <c>VkFormat</c> and the bytes per pixel a transfer uses.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <returns>The <c>VkFormat</c> and the bytes in one pixel.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the backend has no format for it.</exception>
    private static (uint NativeFormat, int BytesPerPixel) DescribeFormat(GpuTextureFormat format)
        => TryDescribeFormat(format, out var nativeFormat, out var bytesPerPixel)
            ? (nativeFormat, bytesPerPixel)
            : throw new ArgumentOutOfRangeException(nameof(format), format, "Vulkan has no format for it.");

    /// <summary>
    /// Creates the image and chooses and binds its memory.
    /// </summary>
    /// <param name="provider">The bridge that owns the device.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    private void CreateImage(VulkanUnifiedProvider provider, int width, int height)
    {
        var createInfo = new VkImageCreateInfo
        {
            StructureType = VulkanApi.IMAGE_CREATE_INFO_TYPE,
            ImageType = VulkanApi.IMAGE_TYPE_2D,
            Format = m_NativeFormat,
            Extent = new VkExtent3D { Width = (uint)width, Height = (uint)height, Depth = 1 },
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = VulkanApi.SAMPLE_COUNT_1,
            Tiling = VulkanApi.IMAGE_TILING_OPTIMAL,
            Usage = VulkanApi.IMAGE_USAGE_TRANSFER_SOURCE | VulkanApi.IMAGE_USAGE_TRANSFER_DESTINATION | VulkanApi.IMAGE_USAGE_SAMPLED,
            SharingMode = VulkanApi.SHARING_MODE_EXCLUSIVE,
            InitialLayout = VulkanApi.IMAGE_LAYOUT_UNDEFINED
        };

        nint image = 0;
        var imageResult = VulkanApi.CreateImage(m_Device, &createInfo, 0, &image);

        if (imageResult != VulkanApi.SUCCESS || image == 0)
        {
            throw new VulkanException($"vkCreateImage failed for a {width}x{height} texture.", imageResult);
        }

        m_Image = image;

        VkMemoryRequirements requirements;
        VulkanApi.GetImageMemoryRequirements(m_Device, image, &requirements);

        var memoryType = provider.SelectMemoryType(requirements.MemoryTypeBits, UnifiedMemoryUsage.Device);

        if (memoryType < 0)
        {
            throw new VulkanException(
                $"No device local memory type can serve a {width}x{height} texture (compatible type bits 0x{requirements.MemoryTypeBits:X}).");
        }

        var allocateInfo = new VkMemoryAllocateInfo
        {
            StructureType = VulkanApi.MEMORY_ALLOCATE_INFO_TYPE,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = (uint)memoryType
        };

        nint memory = 0;
        var allocateResult = VulkanApi.AllocateMemory(m_Device, &allocateInfo, 0, &memory);

        if (allocateResult != VulkanApi.SUCCESS || memory == 0)
        {
            ThrowAllocationFailure($"vkAllocateMemory failed for a {requirements.Size} byte texture", allocateResult);
        }

        var bindResult = VulkanApi.BindImageMemory(m_Device, image, memory, 0);

        if (bindResult != VulkanApi.SUCCESS)
        {
            VulkanApi.FreeMemory(m_Device, memory, 0);
            throw new VulkanException($"vkBindImageMemory failed with {bindResult}.", bindResult);
        }

        m_Memory = memory;
    }

    /// <summary>
    /// Creates the view over the image's one mip and layer.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    private void CreateView(int width, int height)
    {
        var viewInfo = new VkImageViewCreateInfo
        {
            StructureType = VulkanApi.IMAGE_VIEW_CREATE_INFO_TYPE,
            Image = m_Image,
            ViewType = VulkanApi.IMAGE_VIEW_TYPE_2D,
            Format = m_NativeFormat,
            Components = new VkComponentMapping
            {
                Red = VulkanApi.COMPONENT_SWIZZLE_IDENTITY,
                Green = VulkanApi.COMPONENT_SWIZZLE_IDENTITY,
                Blue = VulkanApi.COMPONENT_SWIZZLE_IDENTITY,
                Alpha = VulkanApi.COMPONENT_SWIZZLE_IDENTITY
            },
            SubresourceRange = new VkImageSubresourceRange
            {
                AspectMask = VulkanApi.IMAGE_ASPECT_COLOR,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        nint view = 0;
        var viewResult = VulkanApi.CreateImageView(m_Device, &viewInfo, 0, &view);

        if (viewResult != VulkanApi.SUCCESS || view == 0)
        {
            throw new VulkanException($"vkCreateImageView failed for a {width}x{height} texture.", viewResult);
        }

        m_View = view;
    }

    /// <summary>
    /// Creates the command pool, the command buffer and the fence a transfer is recorded and waited for with.
    /// </summary>
    /// <param name="queueFamily">The family of the queue the transfer is submitted to.</param>
    private void CreateTransferCommands(uint queueFamily)
    {
        var poolInfo = new VkCommandPoolCreateInfo
        {
            StructureType = VulkanApi.COMMAND_POOL_CREATE_INFO_TYPE,
            Flags = VulkanApi.COMMAND_POOL_CREATE_RESET_COMMAND_BUFFER,
            QueueFamilyIndex = queueFamily
        };

        nint pool = 0;
        var poolResult = VulkanApi.CreateCommandPool(m_Device, &poolInfo, 0, &pool);

        if (poolResult != VulkanApi.SUCCESS || pool == 0)
        {
            throw new VulkanException($"vkCreateCommandPool failed for the texture transfer pool.", poolResult);
        }

        m_CommandPool = pool;

        var allocateInfo = new VkCommandBufferAllocateInfo
        {
            StructureType = VulkanApi.COMMAND_BUFFER_ALLOCATE_INFO_TYPE,
            CommandPool = pool,
            Level = VulkanApi.COMMAND_BUFFER_LEVEL_PRIMARY,
            CommandBufferCount = 1
        };

        nint commandBuffer = 0;
        var allocateResult = VulkanApi.AllocateCommandBuffers(m_Device, &allocateInfo, &commandBuffer);

        if (allocateResult != VulkanApi.SUCCESS || commandBuffer == 0)
        {
            throw new VulkanException("vkAllocateCommandBuffers failed for the texture transfer buffer.", allocateResult);
        }

        m_CommandBuffer = commandBuffer;

        var fenceInfo = new VkFenceCreateInfo { StructureType = VulkanApi.FENCE_CREATE_INFO_TYPE };
        nint fence = 0;
        var fenceResult = VulkanApi.CreateFence(m_Device, &fenceInfo, 0, &fence);

        if (fenceResult != VulkanApi.SUCCESS || fence == 0)
        {
            throw new VulkanException("vkCreateFence failed for the texture transfer fence.", fenceResult);
        }

        m_Fence = fence;
    }

    /// <summary>
    /// Records one transfer between the image and a staging buffer, submits it and waits for it.
    /// </summary>
    /// <param name="stagingBuffer">The <c>VkBuffer</c> with the texels, or the buffer the texels are copied into.</param>
    /// <param name="bufferRowLength">The source or destination row length, in texels.</param>
    /// <param name="toImage">Whether the copy goes from the buffer into the image.</param>
    private void Transfer(nint stagingBuffer, uint bufferRowLength, bool toImage)
    {
        var beginInfo = new VkCommandBufferBeginInfo { StructureType = VulkanApi.COMMAND_BUFFER_BEGIN_INFO_TYPE };

        if (VulkanApi.BeginCommandBuffer(m_CommandBuffer, &beginInfo) != VulkanApi.SUCCESS)
        {
            throw new VulkanException("vkBeginCommandBuffer failed for a texture transfer.");
        }

        var layout = m_Layout;
        var submitted = false;
        var completed = false;

        try
        {
            var region = new VkBufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = bufferRowLength,
                BufferImageHeight = 0,
                ImageSubresource = new VkImageSubresourceLayers
                {
                    AspectMask = VulkanApi.IMAGE_ASPECT_COLOR,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                ImageOffset = default,
                ImageExtent = new VkExtent3D
                {
                    Width = (uint)m_Width,
                    Height = (uint)m_Height,
                    Depth = 1
                }
            };

            if (toImage)
            {
                // The staging buffer was written by the host, so the dependency starts at the host stage. An image that has
                // never been used has no earlier access to wait for, but the host write still has to be made available.
                var sourceStage = layout == VulkanApi.IMAGE_LAYOUT_UNDEFINED
                    ? VulkanApi.PIPELINE_STAGE_HOST
                    : VulkanApi.PIPELINE_STAGE_HOST | VulkanApi.PIPELINE_STAGE_ALL_COMMANDS;

                Transition(
                    ref layout,
                    VulkanApi.IMAGE_LAYOUT_TRANSFER_DESTINATION_OPTIMAL,
                    sourceStage,
                    VulkanApi.ACCESS_HOST_WRITE,
                    VulkanApi.PIPELINE_STAGE_TRANSFER,
                    VulkanApi.ACCESS_TRANSFER_READ | VulkanApi.ACCESS_TRANSFER_WRITE);

                VulkanApi.CmdCopyBufferToImage(
                    m_CommandBuffer,
                    stagingBuffer,
                    m_Image,
                    VulkanApi.IMAGE_LAYOUT_TRANSFER_DESTINATION_OPTIMAL,
                    1,
                    &region);

                Transition(
                    ref layout,
                    VulkanApi.IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                    VulkanApi.PIPELINE_STAGE_TRANSFER,
                    VulkanApi.ACCESS_TRANSFER_WRITE,
                    VulkanApi.PIPELINE_STAGE_FRAGMENT_SHADER | VulkanApi.PIPELINE_STAGE_COMPUTE_SHADER,
                    VulkanApi.ACCESS_SHADER_READ);
            }
            else
            {
                var sourceStage = layout == VulkanApi.IMAGE_LAYOUT_UNDEFINED
                    ? VulkanApi.PIPELINE_STAGE_TOP_OF_PIPE
                    : VulkanApi.PIPELINE_STAGE_ALL_COMMANDS;

                Transition(
                    ref layout,
                    VulkanApi.IMAGE_LAYOUT_TRANSFER_SOURCE_OPTIMAL,
                    sourceStage,
                    0,
                    VulkanApi.PIPELINE_STAGE_TRANSFER,
                    VulkanApi.ACCESS_TRANSFER_READ);

                VulkanApi.CmdCopyImageToBuffer(
                    m_CommandBuffer,
                    m_Image,
                    VulkanApi.IMAGE_LAYOUT_TRANSFER_SOURCE_OPTIMAL,
                    stagingBuffer,
                    1,
                    &region);

                // The transfer wrote the staging buffer, so publishing that write to the host is its own dependency.
                var barrier = new VkMemoryBarrier
                {
                    StructureType = VulkanApi.MEMORY_BARRIER_TYPE,
                    SourceAccessMask = VulkanApi.ACCESS_TRANSFER_WRITE,
                    DestinationAccessMask = VulkanApi.ACCESS_HOST_READ
                };

                VulkanApi.CmdPipelineBarrier(
                    m_CommandBuffer,
                    VulkanApi.PIPELINE_STAGE_TRANSFER,
                    VulkanApi.PIPELINE_STAGE_HOST,
                    0,
                    1,
                    &barrier,
                    0,
                    0,
                    0,
                    0);

                Transition(
                    ref layout,
                    VulkanApi.IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL,
                    VulkanApi.PIPELINE_STAGE_TRANSFER,
                    VulkanApi.ACCESS_TRANSFER_READ,
                    VulkanApi.PIPELINE_STAGE_FRAGMENT_SHADER | VulkanApi.PIPELINE_STAGE_COMPUTE_SHADER,
                    VulkanApi.ACCESS_SHADER_READ);
            }

            if (VulkanApi.EndCommandBuffer(m_CommandBuffer) != VulkanApi.SUCCESS)
            {
                throw new VulkanException("vkEndCommandBuffer failed for a texture transfer.");
            }

            var commandBuffer = m_CommandBuffer;
            var submitInfo = new VkSubmitInfo
            {
                StructureType = VulkanApi.SUBMIT_INFO_TYPE,
                CommandBufferCount = 1,
                CommandBuffers = (nint)(&commandBuffer)
            };

            if (VulkanApi.QueueSubmit(m_Queue, 1, &submitInfo, m_Fence) != VulkanApi.SUCCESS)
            {
                throw new VulkanException("vkQueueSubmit failed for a texture transfer.");
            }

            submitted = true;

            var fence = m_Fence;

            if (VulkanApi.WaitForFences(m_Device, 1, &fence, 1, VulkanApi.INFINITE_TIMEOUT) != VulkanApi.SUCCESS)
            {
                throw new VulkanException("vkWaitForFences failed for a texture transfer.");
            }

            VulkanApi.ResetFences(m_Device, 1, &fence);
            m_Layout = layout;
            completed = true;
        }
        finally
        {
            if (!completed)
            {
                // A failed recording or submission leaves the buffer recording and the fence possibly signaled, so the
                // device is drained and the pool is reset to a state the next transfer can record from.
                VulkanApi.DeviceWaitIdle(m_Device);

                if (submitted)
                {
                    var fence = m_Fence;
                    VulkanApi.ResetFences(m_Device, 1, &fence);
                }

                VulkanApi.ResetCommandPool(m_Device, m_CommandPool, 0);
            }
        }
    }

    /// <summary>
    /// Records a layout transition of the whole image.
    /// </summary>
    /// <param name="layout">The tracked layout, updated when the transition is recorded.</param>
    /// <param name="newLayout">The layout to move to.</param>
    /// <param name="sourceStage">The stage that has to finish first.</param>
    /// <param name="sourceAccess">The accesses that have to be made available.</param>
    /// <param name="destinationStage">The stage that waits for the transition.</param>
    /// <param name="destinationAccess">The accesses the destination stage performs.</param>
    private void Transition(ref int layout, int newLayout, uint sourceStage, uint sourceAccess, uint destinationStage, uint destinationAccess)
    {
        var barrier = new VkImageMemoryBarrier
        {
            StructureType = VulkanApi.IMAGE_MEMORY_BARRIER_TYPE,
            SourceAccessMask = sourceAccess,
            DestinationAccessMask = destinationAccess,
            OldLayout = layout,
            NewLayout = newLayout,
            SourceQueueFamilyIndex = VulkanApi.QUEUE_FAMILY_IGNORED,
            DestinationQueueFamilyIndex = VulkanApi.QUEUE_FAMILY_IGNORED,
            Image = m_Image,
            SubresourceRange = new VkImageSubresourceRange
            {
                AspectMask = VulkanApi.IMAGE_ASPECT_COLOR,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };

        VulkanApi.CmdPipelineBarrier(
            m_CommandBuffer,
            sourceStage,
            destinationStage,
            0,
            0,
            null,
            0,
            0,
            1,
            (nint)(&barrier));

        layout = newLayout;
    }

    /// <summary>
    /// Creates a host visible buffer sized for one transfer and maps it.
    /// </summary>
    /// <param name="size">The buffer size, in bytes.</param>
    /// <param name="memory">When the method returns, the <c>VkDeviceMemory</c> bound to the buffer.</param>
    /// <param name="host">When the method returns, the mapping.</param>
    /// <param name="coherent">When the method returns, whether the mapping is coherent and needs no cache maintenance.</param>
    /// <returns>The <c>VkBuffer</c>.</returns>
    private nint CreateStagingBuffer(ulong size, out nint memory, out byte* host, out bool coherent)
    {
        memory = 0;
        host = null;
        coherent = false;

        var bufferInfo = new VkBufferCreateInfo
        {
            StructureType = VulkanApi.BUFFER_CREATE_INFO_TYPE,
            Size = size,
            Usage = VulkanApi.BUFFER_USAGE_TRANSFER_SOURCE | VulkanApi.BUFFER_USAGE_TRANSFER_DESTINATION,
            SharingMode = VulkanApi.SHARING_MODE_EXCLUSIVE
        };

        nint buffer = 0;
        var bufferResult = VulkanApi.CreateBuffer(m_Device, &bufferInfo, 0, &buffer);

        if (bufferResult != VulkanApi.SUCCESS || buffer == 0)
        {
            ThrowAllocationFailure($"vkCreateBuffer failed for a {size} byte texture staging buffer", bufferResult);
        }

        VkMemoryRequirements requirements;
        VulkanApi.GetBufferMemoryRequirements(m_Device, buffer, &requirements);

        var memoryType = m_Provider.SelectMemoryType(requirements.MemoryTypeBits, UnifiedMemoryUsage.General);

        if (memoryType < 0)
        {
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException(
                $"No host visible memory type can serve a {size} byte texture staging buffer (compatible type bits 0x{requirements.MemoryTypeBits:X}).");
        }

        var flags = m_Provider.GetMemoryTypeFlags(memoryType);
        coherent = (flags & VulkanMemoryTypeInfo.HOST_COHERENT) != 0;

        var allocateInfo = new VkMemoryAllocateInfo
        {
            StructureType = VulkanApi.MEMORY_ALLOCATE_INFO_TYPE,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = (uint)memoryType
        };

        nint deviceMemory = 0;
        var allocateResult = VulkanApi.AllocateMemory(m_Device, &allocateInfo, 0, &deviceMemory);

        if (allocateResult != VulkanApi.SUCCESS || deviceMemory == 0)
        {
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            ThrowAllocationFailure($"vkAllocateMemory failed for a {requirements.Size} byte texture staging buffer", allocateResult);
        }

        var bindResult = VulkanApi.BindBufferMemory(m_Device, buffer, deviceMemory, 0);

        if (bindResult != VulkanApi.SUCCESS)
        {
            VulkanApi.FreeMemory(m_Device, deviceMemory, 0);
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException($"vkBindBufferMemory failed for a texture staging buffer with {bindResult}.", bindResult);
        }

        void* mapped;
        var mapResult = VulkanApi.MapMemory(m_Device, deviceMemory, 0, VulkanApi.WHOLE_SIZE, 0, &mapped);

        if (mapResult != VulkanApi.SUCCESS || mapped == null)
        {
            VulkanApi.FreeMemory(m_Device, deviceMemory, 0);
            VulkanApi.DestroyBuffer(m_Device, buffer, 0);
            throw new VulkanException($"vkMapMemory failed for a {size} byte texture staging buffer.", mapResult);
        }

        memory = deviceMemory;
        host = (byte*)mapped;
        return buffer;
    }

    /// <summary>
    /// Unmaps and destroys a staging buffer.
    /// </summary>
    /// <param name="buffer">The <c>VkBuffer</c>.</param>
    /// <param name="memory">The <c>VkDeviceMemory</c> bound to it.</param>
    /// <param name="host">The mapping, or <see langword="null"/> when it was never made.</param>
    private void ReleaseStaging(nint buffer, nint memory, byte* host)
    {
        if (host != null)
        {
            VulkanApi.UnmapMemory(m_Device, memory);
        }

        VulkanApi.DestroyBuffer(m_Device, buffer, 0);
        VulkanApi.FreeMemory(m_Device, memory, 0);
    }

    /// <summary>
    /// Publishes host writes in a mapped staging buffer to the device.
    /// </summary>
    /// <param name="memory">The <c>VkDeviceMemory</c> to flush.</param>
    private void Flush(nint memory)
    {
        var range = new VkMappedMemoryRange
        {
            StructureType = VulkanApi.MAPPED_MEMORY_RANGE_TYPE,
            Memory = memory,
            Offset = 0,
            Size = VulkanApi.WHOLE_SIZE
        };

        var result = VulkanApi.FlushMappedMemoryRanges(m_Device, 1, &range);

        if (result != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkFlushMappedMemoryRanges failed for a texture staging buffer with {result}.", result);
        }
    }

    /// <summary>
    /// Makes device writes in a mapped staging buffer visible to the host.
    /// </summary>
    /// <param name="memory">The <c>VkDeviceMemory</c> to invalidate.</param>
    private void Invalidate(nint memory)
    {
        var range = new VkMappedMemoryRange
        {
            StructureType = VulkanApi.MAPPED_MEMORY_RANGE_TYPE,
            Memory = memory,
            Offset = 0,
            Size = VulkanApi.WHOLE_SIZE
        };

        var result = VulkanApi.InvalidateMappedMemoryRanges(m_Device, 1, &range);

        if (result != VulkanApi.SUCCESS)
        {
            throw new VulkanException($"vkInvalidateMappedMemoryRanges failed for a texture staging buffer with {result}.", result);
        }
    }

    /// <summary>
    /// Computes the row pitch a staging buffer needs for the rows the caller left in a span.
    /// </summary>
    /// <param name="bytesPerRow">The number of bytes in one caller row.</param>
    /// <param name="length">The number of bytes in the caller span.</param>
    /// <returns>The row pitch, which is at least the image row and a whole number of texels.</returns>
    private int StagingRowPitch(int bytesPerRow, int length)
    {
        var tight = m_Width * m_BytesPerPixel;

        if (bytesPerRow < tight)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bytesPerRow),
                bytesPerRow,
                $"A {m_Width} pixel row of this format is {tight} bytes.");
        }

        var required = (long)bytesPerRow * m_Height;

        if (length < required)
        {
            throw new ArgumentException(
                $"The span holds {length} bytes, but {m_Height} rows of {bytesPerRow} bytes need {required}.",
                nameof(bytesPerRow));
        }

        // bufferRowLength is in texels, so the staging row has to end on a texel boundary even when the caller's row does
        // not; the extra bytes at the end of the row are padding the image copy never reads.
        var remainder = bytesPerRow % m_BytesPerPixel;

        return remainder == 0 ? bytesPerRow : bytesPerRow + m_BytesPerPixel - remainder;
    }

    /// <summary>
    /// Releases every native object this texture still owns.
    /// </summary>
    private void ReleaseNative()
    {
        if (m_View != 0)
        {
            VulkanApi.DestroyImageView(m_Device, m_View, 0);
            m_View = 0;
        }

        if (m_Image != 0)
        {
            // The engine keeps the image it lent this wrapper, so only an image this layer created is destroyed.
            if (m_OwnsImage)
            {
                VulkanApi.DestroyImage(m_Device, m_Image, 0);
            }

            m_Image = 0;
        }

        if (m_Memory != 0)
        {
            VulkanApi.FreeMemory(m_Device, m_Memory, 0);
            m_Memory = 0;
        }

        if (m_Fence != 0)
        {
            VulkanApi.DestroyFence(m_Device, m_Fence, 0);
            m_Fence = 0;
        }

        if (m_CommandPool != 0)
        {
            VulkanApi.DestroyCommandPool(m_Device, m_CommandPool, 0);
            m_CommandPool = 0;
        }

        m_CommandBuffer = 0;
    }

    /// <summary>
    /// Reports a failed allocation as <see cref="OutOfMemoryException"/>, like the bridge does.
    /// </summary>
    /// <param name="what">The call that failed.</param>
    /// <param name="result">The <c>VkResult</c> the call returned.</param>
    private static void ThrowAllocationFailure(string what, int result)
    {
        if (result is -1 or -2)
        {
            throw new OutOfMemoryException($"{what} (VkResult {result}).");
        }

        throw new VulkanException($"{what}.", result);
    }

    /// <summary>
    /// Throws when the texture has already been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the texture has been disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(VulkanTexture));
        }
    }
}
