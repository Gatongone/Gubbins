
using Gubbins.Graphic.D3D12;
using Gubbins.Graphic;
using Gubbins.Graphic.Metal;

namespace Gubbins.Graphic;

/// <summary>
/// A compute pipeline loaded on a <see cref="GpuDevice"/>, with the two block bindings this layer provides: an input the
/// shader reads and an output it writes.
/// </summary>
/// <remarks>
/// The caller never names a graphics API: the kernel dispatches through whichever backend <see cref="GpuDevice.TryCreate"/>
/// selected, and the output block is written by the device, so it is marked with
/// <see cref="GpuMemory.MarkWrittenByGpu"/> and is only readable after a synchronization point.
/// </remarks>
public sealed class GpuKernel : IDisposable
{
    private readonly GpuDevice m_Device;
    private readonly D3D12Compute? m_D3D12;
    private readonly VulkanCompute? m_Vulkan;
    private readonly MetalCompute? m_Metal;
    private readonly GlCompute? m_OpenGL;
    private readonly CudaCompute? m_Cuda;
    /// <summary>
    /// The name the kernel compiler gives every kernel, which is how each backend finds it in the artifact.
    /// </summary>
    public const string KernelEntryPoint = "gubbins_main";

    private bool m_Disposed;

    /// <summary>
    /// The size of the buffer that carries the lengths of the inputs.
    /// </summary>
    private const ulong LengthsSize = 16;

    /// <param name="device">The device the kernel was loaded on.</param>
    /// <param name="d3d12">The D3D12 pipeline, when that is the backend in use.</param>
    /// <param name="vulkan">The Vulkan pipeline, when that is the backend in use.</param>
    private GpuKernel(GpuDevice device, D3D12Compute? d3d12, VulkanCompute? vulkan, MetalCompute? metal, GlCompute? openGL, CudaCompute? cuda)
    {
        m_Device = device;
        m_D3D12 = d3d12;
        m_Vulkan = vulkan;
        m_Metal = metal;
        m_OpenGL = openGL;
        m_Cuda = cuda;
    }

    /// <summary>
    /// Loads a kernel from bytecode.
    /// </summary>
    /// <param name="device">The device to load it on.</param>
    /// <param name="bytecode">The compiled shader for that device's backend: DXIL on D3D12, SPIR-V on Vulkan.</param>
    /// <returns>The kernel, or <see langword="null"/> when the backend has no compute path yet or refused the pipeline.</returns>
    public static GpuKernel? Load(GpuDevice device, ReadOnlySpan<byte> bytecode)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (device.Backend is D3D12Device d3d12)
        {
            var compute = new D3D12Compute(d3d12.Handle);

            if (!compute.SetPipeline(bytecode))
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, compute, null, null, null, null);
        }

        if (device.Backend is VulkanDevice vulkan)
        {
            var compute = new VulkanCompute(vulkan.Handle, vulkan.Queue, vulkan.QueueFamily);

            if (!compute.SetPipeline(bytecode))
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, null, compute, null, null, null);
        }

        return null;
    }

    /// <summary>
    /// Loads a kernel from a directory that holds it for every backend.
    /// </summary>
    /// <param name="device">The device to load it on.</param>
    /// <param name="directory">The directory that holds the artifacts.</param>
    /// <param name="name">The kernel name, which is the file name without its extension.</param>
    /// <param name="entryPoint">The function to run, for the backends that resolve one by name.</param>
    /// <returns>The kernel, or <see langword="null"/> when the artifact for this backend is missing or refused.</returns>
    /// <remarks>
    /// The artifact that is loaded is chosen from the backend the device selected: <c>dxil</c> for D3D12, <c>spv</c> for
    /// Vulkan, <c>comp</c> for OpenGL, <c>metallib</c> or the <c>metal</c> source it was built from for Metal, and
    /// <c>ptx</c> for CUDA. A caller that ships them all has one kernel to maintain and no branch of its own.
    /// </remarks>
    public static GpuKernel? LoadFromDirectory(GpuDevice device, string directory, string name, string entryPoint = KernelEntryPoint)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        string extension = device.Backend switch
        {
            D3D12Device => ".dxil",
            VulkanDevice => ".spv",
            GlDevice => ".comp",
            MetalDevice => ".metallib",
            CudaDevice => ".ptx",
            _ => string.Empty
        };

        if (extension.Length == 0)
        {
            return null;
        }

        var path = Path.Combine(directory, name + extension);

        // A metalib is what a build step on the platform produces; the source is what any other machine produces, and
        // Metal compiles it when the pipeline is created.
        if (!File.Exists(path) && device.Backend is MetalDevice)
        {
            path = Path.Combine(directory, name + ".metal");
        }

        return File.Exists(path) ? LoadFromFile(device, path, entryPoint) : null;
    }

    /// <summary>
    /// Loads a kernel from a file, which is the only way Metal takes a shader.
    /// </summary>
    /// <param name="device">The device to load it on.</param>
    /// <param name="path">The compiled shader on disk: a metallib on Metal, DXIL or SPIR-V elsewhere.</param>
    /// <param name="function">The kernel function to run; Metal resolves it by name.</param>
    /// <returns>The kernel, or <see langword="null"/> when the backend refused it.</returns>
    public static GpuKernel? LoadFromFile(GpuDevice device, string path, string function)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        if (device.Backend is CudaDevice cuda)
        {
            _ = cuda;
            var compute = new CudaCompute();

            if (!compute.SetPipeline(File.ReadAllText(path), function))
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, null, null, null, null, compute);
        }

        if (device.Backend is GlDevice openGL)
        {
            var compute = new GlCompute(openGL.EntryPoints);

            if (!compute.SetPipeline(File.ReadAllText(path)))
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, null, null, null, compute, null);
        }

        if (device.Backend is MetalDevice metal)
        {
            var compute = new MetalCompute(metal.Handle, metal.Provider.CommandQueue);
            var compiled = path.EndsWith(".metal", StringComparison.OrdinalIgnoreCase)
                ? compute.SetPipelineFromSource(File.ReadAllText(path), function)
                : compute.SetPipeline(path, function);

            if (!compiled)
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, null, null, compute, null, null);
        }

        return Load(device, File.ReadAllBytes(path));
    }

    /// <summary>
    /// Dispatches the kernel over two blocks.
    /// </summary>
    /// <param name="source">The block the shader reads.</param>
    /// <param name="destination">The block the shader writes.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="lengths">
    /// The block that holds one element count per buffer, for a kernel that reads <c>Span.Length</c>; <see langword="null"/>
    /// when the kernel does not.
    /// </param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    public bool TryDispatch(GpuBlock source, GpuBlock destination, uint groups, GpuBlock? lengths = null)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (source is null || destination is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(destination));
        }

        if (m_Cuda is not null && m_Device.Backend is CudaDevice cuda)
        {
            if (!cuda.Provider.TryGetPointer(source.Storage, out var sourcePointer) ||
                !cuda.Provider.TryGetPointer(destination.Storage, out var destinationPointer))
            {
                return false;
            }

            var lengthsPointer = 0ul;
            var lengthsLength = 0ul;

            if (lengths is not null && cuda.Provider.TryGetPointer(lengths.Storage, out var availableLengths))
            {
                lengthsPointer = availableLengths;
                lengthsLength = (ulong)lengths.Storage.Length;
            }

            return m_Cuda.Dispatch(sourcePointer, (ulong)source.Storage.Length, destinationPointer, (ulong)destination.Storage.Length, lengthsPointer, lengthsLength, groups, 64);
        }

        if (m_OpenGL is not null && m_Device.Backend is GlDevice openGL)
        {
            if (!openGL.Provider.TryGetBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
                !openGL.Provider.TryGetBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
            {
                return false;
            }

            var lengthsBuffer = 0u;
            var lengthsOffset = 0ul;

            if (lengths is not null && openGL.Provider.TryGetBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
            {
                lengthsBuffer = availableLengths;
                lengthsOffset = availableLengthsOffset;
            }

            return m_OpenGL.Dispatch(sourceBuffer, sourceOffset, (ulong)source.Length, destinationBuffer, destinationOffset, (ulong)destination.Length, lengthsBuffer, lengthsOffset, LengthsSize, groups);
        }

        if (m_Metal is not null && m_Device.Backend is MetalDevice metal)
        {
            var metalProvider = metal.Provider;

            if (!metalProvider.TryGetBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
                !metalProvider.TryGetBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
            {
                return false;
            }

            nint lengthsBuffer = 0;
            var lengthsOffset = 0ul;

            if (lengths is not null && metalProvider.TryGetBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
            {
                lengthsBuffer = availableLengths;
                lengthsOffset = availableLengthsOffset;
            }

            return m_Metal.Dispatch(sourceBuffer, (nuint)sourceOffset, destinationBuffer, (nuint)destinationOffset, lengthsBuffer, (nuint)lengthsOffset, groups, 64);
        }

        if (m_Vulkan is not null && m_Device.Backend is VulkanDevice vulkan)
        {
            var provider = vulkan.Provider;

            if (!provider.TryGetVulkanBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
                !provider.TryGetVulkanBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
            {
                return false;
            }

            nint lengthsBuffer = 0;
            var lengthsOffset = 0ul;

            if (lengths is not null && provider.TryGetVulkanBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
            {
                lengthsBuffer = availableLengths;
                lengthsOffset = availableLengthsOffset;
            }

            return m_Vulkan.Dispatch(sourceBuffer, sourceOffset, (ulong)source.Length, destinationBuffer, destinationOffset, (ulong)destination.Length, lengthsBuffer, lengthsOffset, groups);
        }

        if (m_D3D12 is null || m_Device.Backend is not D3D12Device d3d12)
        {
            return false;
        }

        var d3d12Provider = d3d12.Provider;

        if (!d3d12Provider.TryGetResource(source.Storage, out var sourceResource, out _) ||
            !d3d12Provider.TryGetResource(destination.Storage, out var destinationResource, out _))
        {
            return false;
        }

        var sourceAddress = D3D12Api.GetVirtualAddress(sourceResource) + (ulong)source.Storage.Offset;
        var destinationAddress = D3D12Api.GetVirtualAddress(destinationResource) + (ulong)destination.Storage.Offset;

        var lengthsAddress = 0ul;

        if (lengths is not null && d3d12Provider.TryGetResource(lengths.Storage, out var lengthsResource, out _))
        {
            lengthsAddress = D3D12Api.GetVirtualAddress(lengthsResource) + (ulong)lengths.Storage.Offset;
        }

        return m_D3D12.Dispatch(sourceResource, sourceAddress, destinationResource, destinationAddress, lengthsAddress, groups);
    }

    /// <summary>
    /// Releases the pipeline this kernel owns.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        m_D3D12?.Dispose();
        m_Vulkan?.Dispose();
        m_Metal?.Dispose();
        m_OpenGL?.Dispose();
        m_Cuda?.Dispose();
    }
}

