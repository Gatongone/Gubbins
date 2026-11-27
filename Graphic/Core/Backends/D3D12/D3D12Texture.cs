namespace Gubbins.Graphic.D3D12;

/// <summary>
/// A native D3D12 texture behind <see cref="GpuTexture2D"/>.
/// </summary>
/// <remarks>
/// The texture owns a copy queue of its own, so an upload or a readback can be recorded and waited for without touching the
/// compute queue a kernel runs on. The resource stays in <c>COPY_SOURCE</c> after a transfer, which is the state a copy can
/// read it from.
/// </remarks>
internal sealed unsafe class D3D12Texture : IGpuTexture
{
    private const int HeapTypeDefault = 1;
    private const int HeapTypeUpload = 2;
    private const int HeapTypeReadback = 3;
    private const int StateCopyDestination = 0x400;
    private const int StateCopySource = 0x800;
    private const int StateGenericRead = 0xAC;
    private const int ResourceDimensionBuffer = 1;
    private const int ResourceDimensionTexture2D = 3;
    private const uint AllSubresources = 0xFFFFFFFF;
    private const uint CopyTypeSubresource = 0;
    private const uint CopyTypeFootprint = 1;
    private const uint RowPitchAlignment = 256;

    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nint m_Allocator;
    private readonly nint m_List;
    private readonly nint m_Fence;
    private readonly nint m_Resource;
    private readonly nint m_SrvHeap;
    private readonly D3D12GpuDescriptorHandle m_Srv;
    private readonly uint m_Format;
    private readonly int m_Width;
    private readonly int m_Height;
    private readonly int m_BytesPerPixel;
    private readonly bool m_OwnsResource;
    private ulong m_FenceValue;
    private int m_State;
    private bool m_Disposed;

    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <exception cref="D3D12Exception">Thrown when a resource or a queue cannot be created.</exception>
    internal D3D12Texture(nint device, int width, int height, GpuTextureFormat format)
        : this(device, CreateResource(device, width, height, format), width, height, format, StateCopyDestination, true)
    {
    }

    /// <summary>
    /// Wraps a resource the engine owns.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="resource">The <c>ID3D12Resource</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="initialState">The state the resource is in.</param>
    /// <param name="ownsResource">Whether disposing this view releases the resource.</param>
    /// <exception cref="D3D12Exception">Thrown when a queue cannot be created.</exception>
    internal D3D12Texture(nint device, nint resource, int width, int height, GpuTextureFormat format, int initialState, bool ownsResource = false)
    {
        m_Device = device;
        m_Width = width;
        m_Height = height;
        (m_Format, m_BytesPerPixel) = DescribeFormat(format);
        m_Resource = resource;
        m_OwnsResource = ownsResource;
        m_State = initialState;

        nint queue = 0;
        nint allocator = 0;
        nint list = 0;
        nint fence = 0;
        nint srvHeap = 0;

        try
        {
            var queueDescription = new D3D12CommandQueueDesc { Type = D3D12Api.COMMAND_LIST_TYPE_COPY, NodeMask = 1 };
            if (D3D12Api.CreateCommandQueue(device, &queueDescription, out queue) < 0 || queue == 0)
            {
                throw new D3D12Exception("CreateCommandQueue failed for the texture copy queue.");
            }

            if (D3D12Api.CreateCommandAllocator(device, D3D12Api.COMMAND_LIST_TYPE_COPY, out allocator) < 0 || allocator == 0)
            {
                throw new D3D12Exception("CreateCommandAllocator failed for the texture copy allocator.");
            }

            if (D3D12Api.CreateCommandList(device, allocator, D3D12Api.COMMAND_LIST_TYPE_COPY, out list) < 0 || list == 0)
            {
                throw new D3D12Exception("CreateCommandList failed for the texture copy list.");
            }

            if (D3D12Api.CloseCommandList(list) < 0)
            {
                throw new D3D12Exception("Closing the initial texture copy list failed.");
            }

            if (D3D12Api.CreateFence(device, out fence) < 0 || fence == 0)
            {
                throw new D3D12Exception("CreateFence failed for the texture copy fence.");
            }

            var heapDescription = new D3D12DescriptorHeapDesc { Type = 0, NumDescriptors = 1, Flags = 1, NodeMask = 0 };

            if (D3D12Api.CreateDescriptorHeap(device, &heapDescription, out srvHeap) < 0 || srvHeap == 0)
            {
                throw new D3D12Exception("CreateDescriptorHeap failed for the texture shader resource view.");
            }

            var view = new D3D12ShaderResourceViewDesc
            {
                Format = m_Format,
                ViewDimension = 4,
                Shader4ComponentMapping = 0x1688,
                MostDetailedMip = 0,
                MipLevels = 1,
                PlaneSlice = 0,
                ResourceMinLodClamp = 0
            };

            D3D12Api.CreateShaderResourceView(device, resource, &view, D3D12Api.GetCpuDescriptorHandle(srvHeap));

            m_Queue = queue;
            m_Allocator = allocator;
            m_List = list;
            m_Fence = fence;
            m_SrvHeap = srvHeap;
            m_Srv = D3D12Api.GetGpuDescriptorHandle(srvHeap);
        }
        catch
        {
            D3D12Api.Release(srvHeap);
            D3D12Api.Release(fence);
            D3D12Api.Release(list);
            D3D12Api.Release(allocator);
            D3D12Api.Release(queue);

            if (ownsResource)
            {
                D3D12Api.Release(resource);
            }

            throw;
        }
    }

    /// <summary>
    /// Creates the committed texture the creating constructor owns.
    /// </summary>
    /// <param name="device">The <c>ID3D12Device</c>.</param>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <returns>The <c>ID3D12Resource</c>.</returns>
    /// <exception cref="D3D12Exception">Thrown when the device cannot create the resource.</exception>
    private static nint CreateResource(nint device, int width, int height, GpuTextureFormat format)
    {
        var (dxgiFormat, _) = DescribeFormat(format);

        var properties = new D3D12HeapProperties { Type = HeapTypeDefault, CreationNodeMask = 1, VisibleNodeMask = 1 };
        var description = new D3D12ResourceDesc
        {
            Dimension = ResourceDimensionTexture2D,
            Width = (ulong)width,
            Height = (uint)height,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = dxgiFormat,
            SampleCount = 1,
            SampleQuality = 0,
            Layout = 0,
            Flags = 0
        };

        var textureResult = D3D12Api.CreateCommittedResource(device, &properties, 0, &description, StateCopyDestination, out var resource);

        if (textureResult < 0 || resource == 0)
        {
            throw new D3D12Exception($"CreateCommittedResource failed for a {width}x{height} texture.", textureResult);
        }

        return resource;
    }

    /// <summary>
    /// Maps a pixel format to the DXGI format and the bytes per pixel a transfer uses.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <param name="dxgiFormat">When the method returns, the DXGI format.</param>
    /// <param name="bytesPerPixel">When the method returns, the bytes in one pixel.</param>
    /// <returns><see langword="true"/> when the backend has a format for it; otherwise, <see langword="false"/>.</returns>
    internal static bool TryDescribeFormat(GpuTextureFormat format, out uint dxgiFormat, out int bytesPerPixel)
    {
        (dxgiFormat, bytesPerPixel) = format switch
        {
            GpuTextureFormat.R8Unorm => (61u, 1),
            GpuTextureFormat.Rgba8Unorm => (28u, 4),
            GpuTextureFormat.Bgra8Unorm => (87u, 4),
            GpuTextureFormat.R32Uint => (42u, 4),
            GpuTextureFormat.R32Float => (41u, 4),
            _ => (0u, 0)
        };

        return dxgiFormat != 0;
    }

    /// <summary>
    /// Maps a pixel format to the DXGI format and the bytes per pixel a transfer uses.
    /// </summary>
    /// <param name="format">The pixel format.</param>
    /// <returns>The DXGI format and the bytes in one pixel.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the backend has no format for it.</exception>
    private static (uint Format, int BytesPerPixel) DescribeFormat(GpuTextureFormat format)
        => TryDescribeFormat(format, out var dxgiFormat, out var bytesPerPixel)
            ? (dxgiFormat, bytesPerPixel)
            : throw new ArgumentOutOfRangeException(nameof(format), format, "D3D12 has no format for it.");

    /// <summary>
    /// Gets the <c>ID3D12Resource</c>.
    /// </summary>
    public nint Resource => m_Resource;

    /// <summary>
    /// Gets the DXGI format of the resource.
    /// </summary>
    public uint Format => m_Format;

    /// <summary>
    /// Gets the row pitch the resource is copied with.
    /// </summary>
    public uint RowPitch => (uint)(((m_Width * m_BytesPerPixel) + RowPitchAlignment - 1) & ~(RowPitchAlignment - 1));

    /// <summary>
    /// Gets the descriptor heap that holds the shader resource view.
    /// </summary>
    public nint SrvHeap => m_SrvHeap;

    /// <summary>
    /// Gets the GPU descriptor of the shader resource view.
    /// </summary>
    public D3D12GpuDescriptorHandle Srv => m_Srv;

    /// <summary>
    /// Gets or sets the state the resource is in.
    /// </summary>
    internal int State
    {
        get => m_State;
        set => m_State = value;
    }

    /// <inheritdoc/>
    public void Upload(ReadOnlySpan<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12Texture));
        }

        var rowPitch = RowPitch;
        var size = (ulong)(rowPitch * (uint)m_Height);
        var (buffer, heap) = CreateBuffer(size, HeapTypeUpload, StateGenericRead, out var mapped);

        try
        {
            for (var row = 0; row < m_Height; row++)
            {
                var source = pixels.Slice(row * bytesPerRow, bytesPerRow);
                var destination = new Span<byte>((byte*)mapped + (row * rowPitch), (int)rowPitch);
                destination.Clear();
                source.CopyTo(destination);
            }
        }
        finally
        {
            D3D12Api.Unmap(buffer, null);
        }

        try
        {
            Execute(list =>
            {
                Transition(list, StateCopyDestination);

                var destination = new D3D12TextureCopyLocation
                {
                    Resource = m_Resource,
                    Type = CopyTypeSubresource,
                    SubresourceIndex = 0
                };

                var source = new D3D12TextureCopyLocation
                {
                    Resource = buffer,
                    Type = CopyTypeFootprint,
                    Offset = 0,
                    Footprint = new D3D12SubresourceFootprint
                    {
                        Format = m_Format,
                        Width = (uint)m_Width,
                        Height = (uint)m_Height,
                        Depth = 1,
                        RowPitch = rowPitch
                    }
                };

                D3D12Api.CopyTextureRegion(list, &destination, 0, 0, 0, &source, null);
            });
        }
        finally
        {
            D3D12Api.Release(buffer);
            D3D12Api.Release(heap);
        }
    }

    /// <inheritdoc/>
    public void Readback(Span<byte> pixels, int bytesPerRow)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(D3D12Texture));
        }

        var rowPitch = RowPitch;
        var size = (ulong)(rowPitch * (uint)m_Height);
        var (buffer, heap) = CreateBuffer(size, HeapTypeReadback, StateCopyDestination, out var mapped);

        try
        {
            Execute(list =>
            {
                Transition(list, StateCopySource);

                var destination = new D3D12TextureCopyLocation
                {
                    Resource = buffer,
                    Type = CopyTypeFootprint,
                    Offset = 0,
                    Footprint = new D3D12SubresourceFootprint
                    {
                        Format = m_Format,
                        Width = (uint)m_Width,
                        Height = (uint)m_Height,
                        Depth = 1,
                        RowPitch = rowPitch
                    }
                };

                var source = new D3D12TextureCopyLocation
                {
                    Resource = m_Resource,
                    Type = CopyTypeSubresource,
                    SubresourceIndex = 0
                };

                D3D12Api.CopyTextureRegion(list, &destination, 0, 0, 0, &source, null);
            });

            for (var row = 0; row < m_Height; row++)
            {
                var source = new ReadOnlySpan<byte>((byte*)mapped + (row * rowPitch), (int)rowPitch);
                source.Slice(0, bytesPerRow).CopyTo(pixels.Slice(row * bytesPerRow, bytesPerRow));
            }
        }
        finally
        {
            D3D12Api.Unmap(buffer, null);
            D3D12Api.Release(buffer);
            D3D12Api.Release(heap);
        }
    }

    /// <summary>
    /// Releases the view: the descriptor heap, the copy queue and the fence, plus the resource when this view owns it.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        D3D12Api.Release(m_SrvHeap);

        if (m_OwnsResource)
        {
            D3D12Api.Release(m_Resource);
        }

        D3D12Api.Release(m_Fence);
        D3D12Api.Release(m_List);
        D3D12Api.Release(m_Allocator);
        D3D12Api.Release(m_Queue);
    }

    /// <summary>
    /// Creates an upload or readback buffer and maps it.
    /// </summary>
    /// <param name="size">The size, in bytes.</param>
    /// <param name="heapType">The heap type.</param>
    /// <param name="state">The initial state.</param>
    /// <param name="mapped">When the method returns, the mapping.</param>
    /// <returns>The resource.</returns>
    private (nint Buffer, nint Heap) CreateBuffer(ulong size, int heapType, int state, out void* mapped)
    {
        var properties = new D3D12HeapProperties { Type = heapType, CreationNodeMask = 1, VisibleNodeMask = 1 };
        var heapDescription = new D3D12HeapDesc
        {
            SizeInBytes = (size + 0xFFFFul) & ~0xFFFFul,
            Properties = properties,
            Alignment = 0,
            Flags = 0
        };

        var heapResult = D3D12Api.CreateHeap(m_Device, &heapDescription, out var heap);

        if (heapResult < 0 || heap == 0)
        {
            throw new D3D12Exception($"CreateHeap failed for a {size} byte texture transfer buffer.", heapResult);
        }

        var description = new D3D12ResourceDesc
        {
            Dimension = ResourceDimensionBuffer,
            Width = size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            Format = 0,
            SampleCount = 1,
            SampleQuality = 0,
            Layout = 1,
            Flags = 0
        };

        if (D3D12Api.CreatePlacedResource(m_Device, heap, 0, &description, state, out var buffer) < 0 || buffer == 0)
        {
            D3D12Api.Release(heap);
            throw new D3D12Exception($"CreatePlacedResource failed for a {size} byte texture transfer buffer.");
        }

        if (D3D12Api.Map(buffer, null, out mapped) < 0 || mapped == null)
        {
            D3D12Api.Release(buffer);
            D3D12Api.Release(heap);
            throw new D3D12Exception($"Mapping a {size} byte texture transfer buffer failed.");
        }

        return (buffer, heap);
    }

    /// <summary>
    /// Records one transfer and waits for it.
    /// </summary>
    /// <param name="record">The commands to record.</param>
    private void Execute(Action<nint> record)
    {
        if (D3D12Api.ResetCommandAllocator(m_Allocator) < 0 || D3D12Api.ResetCommandList(m_List, m_Allocator) < 0)
        {
            throw new D3D12Exception("Resetting the texture copy list failed.");
        }

        record(m_List);

        if (D3D12Api.CloseCommandList(m_List) < 0)
        {
            throw new D3D12Exception("Closing the texture copy list failed.");
        }

        D3D12Api.ExecuteCommandLists(m_Queue, m_List);
        m_FenceValue++;

        if (D3D12Api.Signal(m_Queue, m_Fence, m_FenceValue) < 0 || !D3D12Api.WaitForFence(m_Fence, m_FenceValue))
        {
            throw new D3D12Exception("Waiting for the texture copy failed.");
        }
    }

    /// <summary>
    /// Records a transition of the texture to a state.
    /// </summary>
    /// <param name="list">The command list.</param>
    /// <param name="state">The state to move to.</param>
    private void Transition(nint list, int state)
    {
        if (m_State == state)
        {
            return;
        }

        var barrier = new D3D12ResourceBarrier
        {
            Type = D3D12Api.RESOURCE_BARRIER_TYPE_TRANSITION,
            Resource = m_Resource,
            Subresource = AllSubresources,
            StateBefore = m_State,
            StateAfter = state
        };

        D3D12Api.ResourceBarrier(list, &barrier);
        m_State = state;
    }
}
