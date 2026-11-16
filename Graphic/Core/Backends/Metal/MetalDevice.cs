using Gubbins.Graphic;

namespace Gubbins.Graphic;

/// <summary>
/// A Metal device this library created and owns, so a caller that has no Metal code of its own can still get a working
/// bridge.
/// </summary>
/// <remarks>
/// An engine that already has an <c>MTLDevice</c> should hand it to <see cref="MetalUnifiedProvider"/> instead. The
/// bridge creates its own command queue and releases it on dispose, so this type only owns the device.
/// </remarks>
internal sealed class MetalDevice : IDisposable
{
    private readonly nint m_Device;
    private bool m_Disposed;

    private MetalDevice(nint device)
    {
        m_Device = device;
        Provider = new MetalUnifiedProvider(device);
    }

    /// <summary>
    /// Gets the bridge that allocates on this device.
    /// </summary>
    public MetalUnifiedProvider Provider { get; }

    /// <summary>
    /// Gets the <c>MTLDevice</c> handle, for a caller that needs it for its own resources.
    /// </summary>
    public nint Handle => m_Device;

    /// <summary>
    /// Gets a value indicating whether Metal is present on this machine.
    /// </summary>
    public static bool IsAvailable => MetalUnifiedProvider.IsAvailable;

    /// <summary>
    /// Creates a device on the GPU the system picks.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if the device was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(out MetalDevice? created)
    {
        created = null;

        if (!MetalUnifiedProvider.IsAvailable)
        {
            return false;
        }

        var device = MetalApi.CreateSystemDefaultDevice();
        if (device == 0)
        {
            return false;
        }

        created = new MetalDevice(device);
        return true;
    }

    /// <summary>
    /// Releases the bridge, then the device it was created on.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        Provider.Dispose();
        MetalApi.Release(m_Device);
    }
}

