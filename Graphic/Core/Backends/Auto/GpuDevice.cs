using System.ComponentModel;
using Gubbins.Unsafe;

namespace Gubbins.Graphic;

/// <summary>
/// A device this library created on the system's default GPU, with an allocator already built on it.
/// </summary>
/// <remarks>
/// <para>
/// Nothing in this type names an API: a caller asks for a device, gets one that works on this machine, and allocates
/// from <see cref="Allocator"/>. Which of the platform's APIs is behind it is a property of the machine, not of the
/// caller's code, and it can change between two machines without any calling code changing.
/// </para>
/// <para>
/// A host engine that already owns a device should keep constructing the bridge for its own API directly instead: this
/// type exists for the case where the allocator is the only thing that knows a device exists.
/// </para>
/// <para>
/// This is the primitive layer. The surface a program uses is <see cref="Gpu"/>, which owns a device, the memory
/// allocated on it and the kernels loaded for it; this type is hidden from completion for that reason.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class GpuDevice : IDisposable
{
    /// <summary>
    /// The size a segment is created with when the API does not want one of its own.
    /// </summary>
    private const nuint DEFAULT_SEGMENT_SIZE = 64 * 1024 * 1024;

    private readonly IDisposable m_Owner;
    private readonly UnifiedProvider m_Provider;
    private readonly nuint m_MaxSegmentSize;
    private bool m_Disposed;

    /// <param name="owner">The object that owns the device, released after the allocator.</param>
    /// <param name="provider">The provider the allocator allocates through.</param>
    /// <param name="segmentSize">The segment size the allocator grows by.</param>
    /// <param name="unifiedMemory">Whether the memory shared with the CPU is the machine's own DRAM.</param>
    /// <param name="budget">The working set the device reported, or <see langword="0"/> when it reported none.</param>
    /// <param name="description">What the device is, for a log line rather than for a decision.</param>
    internal GpuDevice(IDisposable owner, UnifiedProvider provider, nuint maxSegmentSize, bool unifiedMemory, nuint budget, string description)
    {
        m_Owner = owner;
        m_Provider = provider;
        m_MaxSegmentSize = maxSegmentSize;
        IsUnifiedMemory = unifiedMemory;
        Budget = budget;
        Description = description;
    }

    /// <summary>
    /// Creates a layer on a Metal device the engine owns.
    /// </summary>
    /// <param name="provider">The bridge the engine created around its own device.</param>
    /// <param name="ownsProvider">Whether disposing the layer also disposes the bridge.</param>
    /// <returns>The layer, to be disposed before the device.</returns>
    public static GpuDevice FromMetal(MetalUnifiedProvider provider, bool ownsProvider = false)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        return new GpuDevice(
            MetalDevice.FromProvider(provider, ownsProvider),
            provider.GetProvider(),
            provider.MaxSegmentSize,
            provider.IsUnifiedMemory,
            provider.WorkingSetLimit,
            "Metal");
    }

    /// <summary>
    /// Creates a layer on a D3D12 device the engine owns.
    /// </summary>
    /// <param name="provider">The bridge the engine created around its own device.</param>
    /// <param name="ownsProvider">Whether disposing the layer also disposes the bridge.</param>
    /// <returns>The layer, to be disposed before the device.</returns>
    public static GpuDevice FromD3D12(D3D12UnifiedProvider provider, bool ownsProvider = false)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        return new GpuDevice(
            D3D12Device.FromProvider(provider, ownsProvider),
            provider.GetProvider(),
            provider.MaxSegmentSize,
            provider.IsUnifiedMemory,
            provider.WorkingSetLimit,
            "D3D12");
    }

    /// <summary>
    /// Creates a layer on a Vulkan device the engine owns.
    /// </summary>
    /// <param name="provider">The bridge the engine created around its own device.</param>
    /// <param name="ownsProvider">Whether disposing the layer also disposes the bridge.</param>
    /// <returns>The layer, to be disposed before the device.</returns>
    public static GpuDevice FromVulkan(VulkanUnifiedProvider provider, bool ownsProvider = false)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        return new GpuDevice(
            VulkanDevice.FromProvider(provider, ownsProvider),
            provider.GetProvider(),
            provider.MaxSegmentSize,
            provider.IsUnifiedMemory,
            provider.WorkingSetLimit,
            "Vulkan");
    }

    /// <summary>
    /// Creates the layer that hands out blocks whose contents are stable between synchronization points.
    /// </summary>
    /// <param name="segmentSize">The size a segment grows by, or <see langword="0"/> for the default.</param>
    /// <returns>The layer, to be disposed before this device.</returns>
    /// <remarks>
    /// This is the surface a caller normally uses: it allocates blocks, marks them when a processor wrote them, and
    /// synchronizes once per frame, without naming a heap or an API.
    /// </remarks>
    internal object Backend => m_Owner;

    public GpuMemory CreateMemory(nuint segmentSize = 0)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuDevice));
        }

        return new GpuMemory(m_Provider, segmentSize == 0 ? SegmentSizeFor(m_MaxSegmentSize) : segmentSize);
    }

    /// <summary>
    /// Gets a value indicating whether the memory shared with the CPU is the DRAM the CPU itself runs from, rather than
    /// system memory the device reaches over an interconnect.
    /// </summary>
    public bool IsUnifiedMemory { get; }

    /// <summary>
    /// Gets the working set the device reported, or <see langword="0"/> when it reported none.
    /// </summary>
    public nuint Budget { get; }

    /// <summary>
    /// Gets a description of the device and the API behind it.
    /// </summary>
    /// <remarks>
    /// This is for logging. Nothing in this library branches on it, and a caller that branches on it has taken back the
    /// choice that <see cref="TryCreate"/> exists to make.
    /// </remarks>
    public string Description { get; }

    /// <summary>
    /// Creates a device on the platform's own API.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when no device could be created.</param>
    /// <returns><see langword="true"/> if a device was created; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// Only backends that can express one allocation both processors address are supported, because anything else would
    /// be a copy engine behind this API and the memory model would stop meaning what it says. That is why D3D11 is
    /// absent: it has no buffer that is both mappable and shader visible, so its shared memory is a staging copy.
    /// Metal, D3D12 and Vulkan each expose that allocation, and OpenGL 4.3 and WebGPU expose it through persistent
    /// mapping and mapped-at-creation buffers, so those two are the next candidates.
    /// </remarks>
    public static bool TryCreate(out GpuDevice? created)
    {
        created = null;

        // The platforms are checked in the order of what each one supports without a runtime that is not shipped with
        // the operating system: Metal on Apple, D3D12 on Windows. Vulkan is the answer everywhere else, and the fallback
        // for a Windows build where D3D12 is missing.
        if (NativeInterop.IsMacOS)
        {
            if (MetalDevice.TryCreate(out var metal))
            {
                created = new GpuDevice(
                    metal!,
                    metal!.Provider.GetProvider(),
                    metal.Provider.MaxSegmentSize,
                    metal.Provider.IsUnifiedMemory,
                    metal.Provider.WorkingSetLimit,
                    "Metal");
            }

            return created != null;
        }

        if (NativeInterop.IsWindows && D3D12Device.TryCreate(out var d3D12))
        {
            created = new GpuDevice(
                d3D12!,
                d3D12!.Provider.GetProvider(),
                d3D12.Provider.MaxSegmentSize,
                d3D12.Provider.IsUnifiedMemory,
                d3D12.Provider.WorkingSetLimit,
                "D3D12");

            return created != null;
        }

        if (GlDevice.TryCreate(out var openGL))
        {
            created = new GpuDevice(
                openGL!,
                openGL!.Provider.GetProvider(),
                openGL.Provider.MaxSegmentSize,
                false,
                openGL.Provider.WorkingSetLimit,
                "OpenGL");

            return true;
        }

        if (VulkanDevice.TryCreate(out var vulkan))
        {
            created = new GpuDevice(
                vulkan!,
                vulkan!.Provider.GetProvider(),
                vulkan.Provider.MaxSegmentSize,
                vulkan.Provider.IsUnifiedMemory,
                vulkan.Provider.WorkingSetLimit,
                "Vulkan");

            return true;
        }

        if (CudaDevice.TryCreate(out var cuda))
        {
            created = new GpuDevice(
                cuda!,
                cuda!.Provider.GetProvider(),
                cuda.Provider.MaxSegmentSize,
                false,
                cuda.Provider.WorkingSetLimit,
                "CUDA");

            return true;
        }

        return false;
    }

    /// <summary>
    /// Chooses the segment size a device should allocate in.
    /// </summary>
    /// <param name="maxSegmentSize">The largest segment the device can back, or <see langword="0"/> when it reported none.</param>
    /// <returns>The segment size to use.</returns>
    private static nuint SegmentSizeFor(nuint maxSegmentSize)
        => maxSegmentSize == 0 || maxSegmentSize > DEFAULT_SEGMENT_SIZE ? DEFAULT_SEGMENT_SIZE : maxSegmentSize;

    /// <summary>
    /// Releases the allocator and then the device it allocates on.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        // The bridge owns the segments, so releasing it returns every block: the allocator is bookkeeping over the
        // bridge and must not be used after this point.
        m_Owner.Dispose();
    }
}

