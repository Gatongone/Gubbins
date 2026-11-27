
using System.ComponentModel;
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
/// <para>
/// This is the primitive layer. The surface a program uses is <see cref="Kernel"/>, which the typed entry point the source
/// generator writes beside a <c>[GpuKernel]</c> method calls; this type is hidden from completion for that reason.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
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
    /// <param name="bytecode">The compiled shader for that device's backend: SPIR-V on Vulkan.</param>
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
    /// The artifact that is loaded is chosen from the backend the device selected: <c>hlsl</c> for D3D12, which the
    /// system's own compiler turns into bytecode here, <c>spv</c> for Vulkan, <c>comp</c> for OpenGL, <c>metallib</c> or
    /// the <c>metal</c> source it was built from for Metal, and <c>ptx</c> for CUDA. A caller that ships them all has one
    /// kernel to maintain and no branch of its own.
    public static GpuKernel? LoadFromDirectory(GpuDevice device, string directory, string name, string entryPoint = KernelEntryPoint)
    {
        if (device is null)
        {
            throw new ArgumentNullException(nameof(device));
        }

        string extension = device.Backend switch
        {
            D3D12Device => ".hlsl",
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
    /// <param name="path">The shader on disk: a metallib or the source Metal compiles, HLSL for D3D12, and SPIR-V for Vulkan.</param>
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

        if (device.Backend is D3D12Device d3d12 && path.EndsWith(".hlsl", StringComparison.OrdinalIgnoreCase))
        {
            var compute = new D3D12Compute(d3d12.Handle);

            if (!compute.SetPipelineFromSource(File.ReadAllText(path), function))
            {
                compute.Dispose();
                return null;
            }

            return new GpuKernel(device, compute, null, null, null, null);
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
    /// <param name="parameters">
    /// The block that holds the structure the kernel takes as one value; <see langword="null"/> when the kernel takes none.
    /// </param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    public bool TryDispatch(GpuBlock source, GpuBlock destination, uint groups, GpuBlock? lengths = null, GpuBlock? parameters = null, bool wait = true)
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

            var parametersPointer = 0ul;

            if (parameters is not null && cuda.Provider.TryGetPointer(parameters.Storage, out var availableParameters))
            {
                parametersPointer = availableParameters;
            }

            return m_Cuda.Dispatch(sourcePointer, (ulong)source.Storage.Length, destinationPointer, (ulong)destination.Storage.Length, parametersPointer, lengthsPointer, lengthsLength, groups, 64, wait);
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

            var parametersBuffer = 0u;
            var parametersOffset = 0ul;
            var parametersSize = 0ul;

            if (parameters is not null && openGL.Provider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
            {
                parametersBuffer = availableParameters;
                parametersOffset = availableParametersOffset;
                parametersSize = (ulong)parameters.Storage.Length;
            }

            return m_OpenGL.Dispatch(sourceBuffer, sourceOffset, (ulong)source.Length, destinationBuffer, destinationOffset, (ulong)destination.Length, parametersBuffer, parametersOffset, parametersSize, lengthsBuffer, lengthsOffset, LengthsSize, ReadOnlySpan<uint>.Empty, ReadOnlySpan<uint>.Empty, groups, wait);
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

            nint parametersBuffer = 0;
            var parametersOffset = 0ul;

            if (parameters is not null && metalProvider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
            {
                parametersBuffer = availableParameters;
                parametersOffset = availableParametersOffset;
            }

            return m_Metal.Dispatch(sourceBuffer, (nuint)sourceOffset, destinationBuffer, (nuint)destinationOffset, parametersBuffer, (nuint)parametersOffset, lengthsBuffer, (nuint)lengthsOffset, ReadOnlySpan<nint>.Empty, ReadOnlySpan<nint>.Empty, groups, 64, wait);
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

            nint parametersBuffer = 0;
            var parametersOffset = 0ul;
            var parametersSize = 0ul;

            if (parameters is not null && provider.TryGetVulkanBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
            {
                parametersBuffer = availableParameters;
                parametersOffset = availableParametersOffset;
                parametersSize = (ulong)parameters.Storage.Length;
            }

            return m_Vulkan.Dispatch(sourceBuffer, sourceOffset, (ulong)source.Length, destinationBuffer, destinationOffset, (ulong)destination.Length, parametersBuffer, parametersOffset, parametersSize, lengthsBuffer, lengthsOffset, ReadOnlySpan<VkDescriptorImageInfo>.Empty, groups, wait);
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

        var parametersAddress = 0ul;

        if (parameters is not null && d3d12Provider.TryGetResource(parameters.Storage, out var parametersResource, out _))
        {
            parametersAddress = D3D12Api.GetVirtualAddress(parametersResource) + (ulong)parameters.Storage.Offset;
        }

        return m_D3D12.Dispatch(sourceResource, sourceAddress, destinationResource, destinationAddress, parametersAddress, lengthsAddress, groups, wait);
    }

    /// <summary>
    /// Runs a kernel that samples a texture, on the backend that can bind one.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">The sampler state.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public unsafe bool TryDispatch(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, GpuTexture2D texture, GpuSamplerState sampler, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (m_Metal is null || m_Device.Backend is not MetalDevice metal)
        {
            return false;
        }

        if (texture.Backend is not MetalTexture metalTexture || sampler.Backend is not MetalSamplerState metalSampler)
        {
            return false;
        }

        if (!metal.Provider.TryGetBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
            !metal.Provider.TryGetBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
        {
            return false;
        }

        nint lengthsBuffer = 0;
        var lengthsOffset = 0ul;

        if (lengths is not null && metal.Provider.TryGetBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
        {
            lengthsBuffer = availableLengths;
            lengthsOffset = availableLengthsOffset;
        }

        nint parametersBuffer = 0;
        var parametersOffset = 0ul;

        if (parameters is not null && metal.Provider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
        }

        var textures = stackalloc nint[1];
        var samplers = stackalloc nint[1];
        textures[0] = metalTexture.Handle;
        samplers[0] = metalSampler.Handle;

        return m_Metal.Dispatch(
            sourceBuffer,
            (nuint)sourceOffset,
            destinationBuffer,
            (nuint)destinationOffset,
            parametersBuffer,
            (nuint)parametersOffset,
            lengthsBuffer,
            (nuint)lengthsOffset,
            new ReadOnlySpan<nint>(textures, 1),
            new ReadOnlySpan<nint>(samplers, 1),
            groups,
            64,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples several textures.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public unsafe bool TryDispatchTextures(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, ReadOnlySpan<GpuTextureBinding> bindings, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (bindings.Length == 0 || m_Metal is null || m_Device.Backend is not MetalDevice metal)
        {
            return false;
        }

        if (!metal.Provider.TryGetBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
            !metal.Provider.TryGetBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
        {
            return false;
        }

        var textures = stackalloc nint[bindings.Length];
        var samplers = stackalloc nint[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            if (bindings[index].Texture.Backend is not MetalTexture texture ||
                bindings[index].Sampler.Backend is not MetalSamplerState sampler)
            {
                return false;
            }

            textures[index] = texture.Handle;
            samplers[index] = sampler.Handle;
        }

        nint lengthsBuffer = 0;
        var lengthsOffset = 0ul;

        if (lengths is not null && metal.Provider.TryGetBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
        {
            lengthsBuffer = availableLengths;
            lengthsOffset = availableLengthsOffset;
        }

        nint parametersBuffer = 0;
        var parametersOffset = 0ul;

        if (parameters is not null && metal.Provider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
        }

        return m_Metal.Dispatch(
            sourceBuffer,
            (nuint)sourceOffset,
            destinationBuffer,
            (nuint)destinationOffset,
            parametersBuffer,
            (nuint)parametersOffset,
            lengthsBuffer,
            (nuint)lengthsOffset,
            new ReadOnlySpan<nint>(textures, bindings.Length),
            new ReadOnlySpan<nint>(samplers, bindings.Length),
            groups,
            64,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples a texture on OpenGL.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">The sampler state.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public unsafe bool TryDispatchOpenGl(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, GpuTexture2D texture, GpuSamplerState sampler, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (m_OpenGL is null || m_Device.Backend is not GlDevice openGL)
        {
            return false;
        }

        if (texture.Backend is not GlTexture glTexture || sampler.Backend is not GlSamplerState glSampler)
        {
            return false;
        }

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

        var parametersBuffer = 0u;
        var parametersOffset = 0ul;
        var parametersSize = 0ul;

        if (parameters is not null && openGL.Provider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
            parametersSize = (ulong)parameters.Storage.Length;
        }

        var textures = stackalloc uint[1];
        var samplers = stackalloc uint[1];
        textures[0] = glTexture.Handle;
        samplers[0] = glSampler.Handle;

        return m_OpenGL.Dispatch(
            sourceBuffer,
            sourceOffset,
            (ulong)source.Length,
            destinationBuffer,
            destinationOffset,
            (ulong)destination.Length,
            parametersBuffer,
            parametersOffset,
            parametersSize,
            lengthsBuffer,
            lengthsOffset,
            LengthsSize,
            new ReadOnlySpan<uint>(textures, 1),
            new ReadOnlySpan<uint>(samplers, 1),
            groups,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples several textures on OpenGL.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public unsafe bool TryDispatchOpenGlTextures(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, ReadOnlySpan<GpuTextureBinding> bindings, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (bindings.Length == 0 || m_OpenGL is null || m_Device.Backend is not GlDevice openGL)
        {
            return false;
        }

        if (!openGL.Provider.TryGetBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
            !openGL.Provider.TryGetBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
        {
            return false;
        }

        var textures = stackalloc uint[bindings.Length];
        var samplers = stackalloc uint[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            if (bindings[index].Texture.Backend is not GlTexture texture || bindings[index].Sampler.Backend is not GlSamplerState sampler)
            {
                return false;
            }

            textures[index] = texture.Handle;
            samplers[index] = sampler.Handle;
        }

        var lengthsBuffer = 0u;
        var lengthsOffset = 0ul;

        if (lengths is not null && openGL.Provider.TryGetBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
        {
            lengthsBuffer = availableLengths;
            lengthsOffset = availableLengthsOffset;
        }

        var parametersBuffer = 0u;
        var parametersOffset = 0ul;
        var parametersSize = 0ul;

        if (parameters is not null && openGL.Provider.TryGetBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
            parametersSize = (ulong)parameters.Storage.Length;
        }

        return m_OpenGL.Dispatch(
            sourceBuffer,
            sourceOffset,
            (ulong)source.Length,
            destinationBuffer,
            destinationOffset,
            (ulong)destination.Length,
            parametersBuffer,
            parametersOffset,
            parametersSize,
            lengthsBuffer,
            lengthsOffset,
            LengthsSize,
            new ReadOnlySpan<uint>(textures, bindings.Length),
            new ReadOnlySpan<uint>(samplers, bindings.Length),
            groups,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples a texture on D3D12.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">The sampler state.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public bool TryDispatchD3D12(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, GpuTexture2D texture, GpuSamplerState sampler, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (m_D3D12 is null || m_Device.Backend is not D3D12Device)
        {
            return false;
        }

        if (texture.Backend is not D3D12Texture || sampler.Backend is not D3D12SamplerState)
        {
            return false;
        }

        // One texture is the same combined-heap path as several: the root signature tables are eight descriptors wide,
        // and the one binding is copied into their first slot.
        return TryDispatchD3D12Textures(source, destination, lengths, parameters, new[] { new GpuTextureBinding(texture, sampler) }, groups, wait);
    }

    /// <summary>
    /// Runs a kernel that samples several textures on D3D12.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    /// <remarks>
    /// The binding at index <c>i</c> is the texture the kernel declares at <c>t{i}</c> and the sampler it declares at
    /// <c>s{i}</c>. The descriptors are gathered into combined heaps by <c>D3D12Compute.Dispatch</c>, which owns them until
    /// the dispatch that recorded them has completed.
    /// </remarks>
    public unsafe bool TryDispatchD3D12Textures(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, ReadOnlySpan<GpuTextureBinding> bindings, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (bindings.Length == 0 || bindings.Length > D3D12Compute.MaxTextures || m_D3D12 is null || m_Device.Backend is not D3D12Device d3d12)
        {
            return false;
        }

        var provider = d3d12.Provider;

        if (!provider.TryGetResource(source.Storage, out var sourceResource, out _) ||
            !provider.TryGetResource(destination.Storage, out var destinationResource, out _))
        {
            return false;
        }

        var native = stackalloc D3D12TextureBinding[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            var binding = bindings[index];

            if (binding.Texture is null || binding.Sampler is null ||
                binding.Texture.Backend is not D3D12Texture texture ||
                binding.Sampler.Backend is not D3D12SamplerState sampler)
            {
                return false;
            }

            native[index] = new D3D12TextureBinding(texture, sampler);
        }

        var sourceAddress = D3D12Api.GetVirtualAddress(sourceResource) + (ulong)source.Storage.Offset;
        var destinationAddress = D3D12Api.GetVirtualAddress(destinationResource) + (ulong)destination.Storage.Offset;

        var lengthsAddress = 0ul;
        if (lengths is not null && provider.TryGetResource(lengths.Storage, out var lengthsResource, out _))
        {
            lengthsAddress = D3D12Api.GetVirtualAddress(lengthsResource) + (ulong)lengths.Storage.Offset;
        }

        var parametersAddress = 0ul;
        if (parameters is not null && provider.TryGetResource(parameters.Storage, out var parametersResource, out _))
        {
            parametersAddress = D3D12Api.GetVirtualAddress(parametersResource) + (ulong)parameters.Storage.Offset;
        }

        var dispatched = m_D3D12.Dispatch(
            sourceResource,
            sourceAddress,
            destinationResource,
            destinationAddress,
            parametersAddress,
            lengthsAddress,
            new ReadOnlySpan<D3D12TextureBinding>(native, bindings.Length),
            groups,
            wait);

        if (dispatched)
        {
            // The command list moved every texture it read into the compute shader's read state, which is where a later
            // upload or readback expects to find it.
            for (var index = 0; index < bindings.Length; index++)
            {
                ((D3D12Texture)bindings[index].Texture.Backend).State = D3D12Api.RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE;
            }
        }

        return dispatched;
    }

    /// <summary>
    /// Runs a kernel that samples a texture on Vulkan.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">The sampler state.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    /// <remarks>
    /// The texture has to be in the layout a sampled binding reads, which is the layout an upload or a readback leaves it
    /// in; one that has never been uploaded is refused instead of being bound in the undefined layout.
    /// </remarks>
    public unsafe bool TryDispatchVulkan(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, GpuTexture2D texture, GpuSamplerState sampler, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (source is null || destination is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(destination));
        }

        if (texture is null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        if (sampler is null)
        {
            throw new ArgumentNullException(nameof(sampler));
        }

        if (m_Vulkan is null || m_Device.Backend is not VulkanDevice vulkan)
        {
            return false;
        }

        if (texture.Backend is not VulkanTexture vulkanTexture || sampler.Backend is not VulkanSamplerState vulkanSampler)
        {
            return false;
        }

        // The upload leaves the image sampleable, and nothing in this layer moves it out of that layout afterwards. A
        // texture that has never been uploaded is still undefined, and a descriptor for it would read uninitialized texels.
        if (vulkanTexture.Layout != VulkanApi.IMAGE_LAYOUT_SHADER_READ_ONLY_OPTIMAL)
        {
            return false;
        }

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

        nint parametersBuffer = 0;
        var parametersOffset = 0ul;
        var parametersSize = 0ul;

        if (parameters is not null && provider.TryGetVulkanBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
            parametersSize = (ulong)parameters.Storage.Length;
        }

        var image = vulkanTexture.GetDescriptorInfo(vulkanSampler);

        var images = stackalloc VkDescriptorImageInfo[1];
        images[0] = image;

        return m_Vulkan.Dispatch(
            sourceBuffer,
            sourceOffset,
            (ulong)source.Length,
            destinationBuffer,
            destinationOffset,
            (ulong)destination.Length,
            parametersBuffer,
            parametersOffset,
            parametersSize,
            lengthsBuffer,
            lengthsOffset,
            new ReadOnlySpan<VkDescriptorImageInfo>(images, 1),
            groups,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples several textures on Vulkan.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    public unsafe bool TryDispatchVulkanTextures(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, ReadOnlySpan<GpuTextureBinding> bindings, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (bindings.Length == 0 || m_Vulkan is null || m_Device.Backend is not VulkanDevice vulkan)
        {
            return false;
        }

        if (!vulkan.Provider.TryGetVulkanBuffer(source.Storage, out var sourceBuffer, out var sourceOffset) ||
            !vulkan.Provider.TryGetVulkanBuffer(destination.Storage, out var destinationBuffer, out var destinationOffset))
        {
            return false;
        }

        var images = stackalloc VkDescriptorImageInfo[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            if (bindings[index].Texture.Backend is not VulkanTexture texture ||
                bindings[index].Sampler.Backend is not VulkanSamplerState sampler)
            {
                return false;
            }

            images[index] = texture.GetDescriptorInfo(sampler);
        }

        nint lengthsBuffer = 0;
        var lengthsOffset = 0ul;

        if (lengths is not null && vulkan.Provider.TryGetVulkanBuffer(lengths.Storage, out var availableLengths, out var availableLengthsOffset))
        {
            lengthsBuffer = availableLengths;
            lengthsOffset = availableLengthsOffset;
        }

        nint parametersBuffer = 0;
        var parametersOffset = 0ul;
        var parametersSize = 0ul;

        if (parameters is not null && vulkan.Provider.TryGetVulkanBuffer(parameters.Storage, out var availableParameters, out var availableParametersOffset))
        {
            parametersBuffer = availableParameters;
            parametersOffset = availableParametersOffset;
            parametersSize = (ulong)parameters.Storage.Length;
        }

        return m_Vulkan.Dispatch(
            sourceBuffer,
            sourceOffset,
            (ulong)source.Length,
            destinationBuffer,
            destinationOffset,
            (ulong)destination.Length,
            parametersBuffer,
            parametersOffset,
            parametersSize,
            lengthsBuffer,
            lengthsOffset,
            new ReadOnlySpan<VkDescriptorImageInfo>(images, bindings.Length),
            groups,
            wait);
    }

    /// <summary>
    /// Runs a kernel that samples a texture on CUDA.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="texture">The texture to sample.</param>
    /// <param name="sampler">
    /// The sampler state. CUDA has no sampler argument: the filter and the address modes live in the texture object, so
    /// the dispatch samples through the description the texture was created with and this value is only checked to be
    /// one of this backend's states.
    /// </param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    /// <remarks>
    /// The launch is the plain one with the handle of the <c>CUtexObject</c> added as the argument after the block of
    /// element counts, which is the order the PTX emits the parameters in.
    /// </remarks>
    public bool TryDispatchCuda(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, GpuTexture2D texture, GpuSamplerState sampler, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (source is null || destination is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(destination));
        }

        if (texture is null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        if (sampler is null)
        {
            throw new ArgumentNullException(nameof(sampler));
        }

        if (m_Cuda is null || m_Device.Backend is not CudaDevice cuda)
        {
            return false;
        }

        if (texture.Backend is not CudaTexture cudaTexture || sampler.Backend is not CudaSamplerState)
        {
            return false;
        }

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

        var parametersPointer = 0ul;

        if (parameters is not null && cuda.Provider.TryGetPointer(parameters.Storage, out var availableParameters))
        {
            parametersPointer = availableParameters;
        }

        return m_Cuda.Dispatch(
            sourcePointer,
            (ulong)source.Storage.Length,
            destinationPointer,
            (ulong)destination.Storage.Length,
            parametersPointer,
            lengthsPointer,
            lengthsLength,
            groups,
            64,
            wait,
            (ulong)cudaTexture.TextureObject);
    }

    /// <summary>
    /// Runs a kernel that samples several textures on CUDA.
    /// </summary>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <param name="bindings">The textures and samplers, in the order the kernel declares them.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="wait">Whether to wait for the dispatch.</param>
    /// <returns><see langword="true"/> if the dispatch ran.</returns>
    /// <remarks>
    /// The handle of each texture object is passed after the block of element counts, in binding order, which is the order
    /// the PTX declares the entry point's parameters in. CUDA keeps each sampler inside its texture object, so the sampler
    /// of a binding is only checked to be this backend's state and nothing of its own is passed to the launch.
    /// </remarks>
    public unsafe bool TryDispatchCudaTextures(GpuBlock source, GpuBlock destination, GpuBlock? lengths, GpuBlock? parameters, ReadOnlySpan<GpuTextureBinding> bindings, uint groups, bool wait = true)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (source is null || destination is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(destination));
        }

        if (bindings.Length == 0 || m_Cuda is null || m_Device.Backend is not CudaDevice cuda)
        {
            return false;
        }

        if (!cuda.Provider.TryGetPointer(source.Storage, out var sourcePointer) ||
            !cuda.Provider.TryGetPointer(destination.Storage, out var destinationPointer))
        {
            return false;
        }

        var textures = stackalloc ulong[bindings.Length];

        for (var index = 0; index < bindings.Length; index++)
        {
            if (bindings[index].Texture.Backend is not CudaTexture texture ||
                bindings[index].Sampler.Backend is not CudaSamplerState)
            {
                return false;
            }

            textures[index] = (ulong)texture.TextureObject;
        }

        var lengthsPointer = 0ul;
        var lengthsLength = 0ul;

        if (lengths is not null && cuda.Provider.TryGetPointer(lengths.Storage, out var availableLengths))
        {
            lengthsPointer = availableLengths;
            lengthsLength = (ulong)lengths.Storage.Length;
        }

        var parametersPointer = 0ul;

        if (parameters is not null && cuda.Provider.TryGetPointer(parameters.Storage, out var availableParameters))
        {
            parametersPointer = availableParameters;
        }

        return m_Cuda.Dispatch(
            sourcePointer,
            (ulong)source.Storage.Length,
            destinationPointer,
            (ulong)destination.Storage.Length,
            parametersPointer,
            lengthsPointer,
            lengthsLength,
            groups,
            64,
            wait,
            new ReadOnlySpan<ulong>(textures, bindings.Length));
    }

    /// <summary>
    /// Waits for the dispatches of this kernel that were recorded without waiting.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    public bool Wait()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuKernel));
        }

        if (m_Metal is not null)
        {
            return m_Metal.WaitPending();
        }

        if (m_Vulkan is not null)
        {
            return m_Vulkan.WaitPending();
        }

        if (m_D3D12 is not null)
        {
            return m_D3D12.WaitPending();
        }

        if (m_OpenGL is not null)
        {
            return m_OpenGL.WaitPending();
        }

        return m_Cuda is null || m_Cuda.WaitPending();
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

