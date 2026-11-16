using Gubbins.Graphic;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace Gubbins.Graphic;

/// <summary>
/// A D3D12 device this library created and owns: the device, the copy queue and the command allocator a bridge needs,
/// all made together and released together.
/// </summary>
/// <remarks>
/// <para>
/// An engine that already has a device should hand its own objects to <see cref="D3D12UnifiedProvider"/> instead. This
/// type is for the other case, where nothing above this library speaks D3D12: a caller asks for a device, gets one that
/// works, and never has to name an API.
/// </para>
/// <para>
/// The bridge is disposed first and the device last, which is the order D3D12 requires: a queue may not outlive the
/// device it was created on, and the bridge holds a command list and a fence created on this device.
/// </para>
/// </remarks>
internal sealed unsafe class D3D12Device : IDisposable
{
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private readonly nint m_Allocator;
    private bool m_Disposed;

    private D3D12Device(nint device, nint queue, nint allocator)
    {
        m_Device = device;
        m_Queue = queue;
        m_Allocator = allocator;
        Provider = new D3D12UnifiedProvider(device, queue, allocator);
    }

    /// <summary>
    /// Gets the bridge that allocates on this device.
    /// </summary>
    public D3D12UnifiedProvider Provider { get; }

    /// <summary>
    /// Gets the <c>ID3D12Device</c> handle, for a caller that needs it for its own resources.
    /// </summary>
    public nint Handle => m_Device;

    /// <summary>
    /// Gets a value indicating whether the D3D12 runtime is present on this machine.
    /// </summary>
    public static bool IsAvailable => D3D12Api.IsAvailable;

    /// <summary>
    /// Creates a device on the adapter the system picks.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(out D3D12Device? created) => TryCreate(0, out created);

    /// <summary>
    /// Creates a device on one adapter.
    /// </summary>
    /// <param name="adapter">The <c>IDXGIAdapter</c> to create it on, or <see langword="0"/> for the system default.</param>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(nint adapter, out D3D12Device? created)
    {
        created = null;

        if (!D3D12Api.IsAvailable || !D3D12Api.TryCreateDevice(adapter, out var device, out _) || device == 0)
        {
            return false;
        }

        // A bridge submits copies, so it needs a copy queue and a copy allocator rather than the graphics ones.
        var queueDesc = new D3D12CommandQueueDesc { Type = D3D12Api.COMMAND_LIST_TYPE_COPY, NodeMask = 1 };

        if (D3D12Api.CreateCommandQueue(device, &queueDesc, out var queue) < 0 || queue == 0)
        {
            D3D12Api.Release(device);
            return false;
        }

        if (D3D12Api.CreateCommandAllocator(device, D3D12Api.COMMAND_LIST_TYPE_COPY, out var allocator) < 0 || allocator == 0)
        {
            D3D12Api.Release(queue);
            D3D12Api.Release(device);
            return false;
        }

        created = new D3D12Device(device, queue, allocator);
        return true;
    }

    /// <summary>
    /// Releases the bridge, then the allocator, the queue and the device it was created on.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        Provider.Dispose();
        D3D12Api.Release(m_Allocator);
        D3D12Api.Release(m_Queue);
        D3D12Api.Release(m_Device);
    }
}

