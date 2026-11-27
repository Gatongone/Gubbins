
using System.Runtime.InteropServices;

#pragma warning disable CS8500

namespace Gubbins.Graphic;

/// <summary>
/// A CUDA implementation of <see cref="IUnifiedProvider"/>: a segment is one managed allocation, which the driver keeps
/// addressable from the CPU and the device at the same address.
/// </summary>
/// <remarks>
/// Managed memory is the reason CUDA is a backend at all: one allocation, one address, both processors. On a device with
/// its own memory the driver migrates pages on demand, so the synchronization point is what makes a write visible; the
/// reported kind says the two views are coherent, which the driver guarantees once the kernel or the copy has completed.
/// </remarks>
internal sealed unsafe class CudaUnifiedProvider : IUnifiedProvider
{
    /// <summary>
    /// The bookkeeping of one segment: the device pointer and its size.
    /// </summary>
    private struct Arena
    {
        public ulong Pointer;
        public byte* Host;
        public nuint Size;
    }

    private GCHandle m_Self;
    private bool m_Disposed;

    /// <inheritdoc/>
    public nuint Alignment => 256;

    /// <inheritdoc/>
    public nuint MaxSegmentSize => 0;

    /// <inheritdoc/>
    public nuint WorkingSetLimit => 0;

    /// <inheritdoc/>
    public UnifiedProvider GetProvider() => ProviderAdapter<CudaUnifiedProvider>.Create(this, ref m_Self);

    /// <summary>
    /// Creates a two dimensional texture on the device.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <returns>The texture, to be disposed when it is no longer sampled.</returns>
    /// <remarks>
    /// CUDA keeps the sampler inside the texture object, so this overload creates it with the default sampler;
    /// <see cref="CreateTexture(int, int, GpuTextureFormat, GpuSampler)"/> creates it with the one the caller wants. The
    /// device's context must be current on the calling thread, which it is for the thread that created the
    /// <see cref="CudaDevice"/>.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown when the bridge has been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the driver refuses the array or the texture object.</exception>
    public GpuTexture2D CreateTexture(int width, int height, GpuTextureFormat format)
        => CreateTexture(width, height, format, default);

    /// <summary>
    /// Creates a two dimensional texture whose texture object carries the sampler's filter and address modes.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <param name="sampler">The filter and the address modes the texture object is created with.</param>
    /// <returns>The texture, to be disposed when it is no longer sampled.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the bridge has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the extent is empty or the format is not mapped.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the driver refuses the array or the texture object.</exception>
    public GpuTexture2D CreateTexture(int width, int height, GpuTextureFormat format, GpuSampler sampler)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(CudaUnifiedProvider));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "A texture is at least one pixel wide and high.");
        }

        return new GpuTexture2D(new CudaTexture(width, height, format, sampler), width, height, format);
    }

    /// <inheritdoc/>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        ulong pointer = 0;
        byte* host = null;
        var device = usage == UnifiedMemoryUsage.Device;

        if (device)
        {
            CudaApi.Check(CudaApi.cuMemAlloc_v2(out pointer, size), $"cuMemAlloc({size})");
        }
        else
        {
            // Host memory the device is given an address for: the CPU writes the bytes the shader reads, with no copy
            // between them and nothing to flush.
            CudaApi.Check(CudaApi.cuMemHostAlloc(out var pageable, size, CudaApi.HostDeviceMap | CudaApi.HostPortable), $"cuMemHostAlloc({size})");
            CudaApi.Check(CudaApi.cuMemHostGetDevicePointer_v2(out pointer, (void*)pageable, 0), "cuMemHostGetDevicePointer");

            host = (byte*)pageable;
        }

        var arena = (Arena*)NativeInterop.AllocZeroed((nuint)sizeof(Arena));

        if (arena == null)
        {
            CudaApi.Check(host == null ? CudaApi.cuMemFree_v2(pointer) : CudaApi.cuMemFreeHost((nint)host), "cuMemFree");

            throw new OutOfMemoryException($"Unable to allocate {sizeof(Arena)} bytes of bridge bookkeeping.");
        }

        arena->Pointer = pointer;
        arena->Host = host;
        arena->Size = size;

        deviceHandle = arena;
        deviceAddress = pointer;
        kind = device ? UnifiedMemoryKind.Device : UnifiedMemoryKind.SharedCoherent;
        return host;
    }

    /// <inheritdoc/>
    public void Free(void* segment, nuint size, void* deviceHandle)
    {
        var arena = (Arena*)deviceHandle;

        if (arena == null)
        {
            return;
        }

        CudaApi.Check(arena->Host == null
            ? CudaApi.cuMemFree_v2(arena->Pointer)
            : CudaApi.cuMemFreeHost((nint)arena->Host), "cuMemFree");

        NativeInterop.Free(arena);
    }

    /// <inheritdoc/>
    public void Flush(void* ptr, nuint size) { }

    /// <inheritdoc/>
    public void Invalidate(void* ptr, nuint size) { }

    /// <inheritdoc/>
    public bool Copy(in UnifiedMemoryBlock destination, nuint destinationOffset, in UnifiedMemoryBlock source, nuint sourceOffset, nuint size, bool defer)
    {
        var destinationArena = (Arena*)destination.DeviceHandle;
        var sourceArena = (Arena*)source.DeviceHandle;

        if (destinationArena == null || sourceArena == null || size == 0)
        {
            return false;
        }

        CudaApi.cuCtxSynchronize();

        var sourcePointer = sourceArena->Pointer + (ulong)source.Offset + (ulong)sourceOffset;
        var destinationPointer = destinationArena->Pointer + (ulong)destination.Offset + (ulong)destinationOffset;

        if (sourceArena->Host != null && destinationArena->Host != null)
        {
            Buffer.MemoryCopy(sourceArena->Host + source.Offset + sourceOffset, destinationArena->Host + destination.Offset + destinationOffset, (long)size, (long)size);
            return true;
        }

        if (sourceArena->Host != null)
        {
            CudaApi.Check(CudaApi.cuMemcpyHtoD_v2(destinationPointer, sourceArena->Host + source.Offset + sourceOffset, size), "cuMemcpyHtoD");
            return true;
        }

        if (destinationArena->Host != null)
        {
            CudaApi.Check(CudaApi.cuMemcpyDtoH_v2(destinationArena->Host + destination.Offset + destinationOffset, sourcePointer, size), "cuMemcpyDtoH");
            return true;
        }

        CudaApi.Check(CudaApi.cuMemcpyDtoD_v2(destinationPointer, sourcePointer, size), "cuMemcpyDtoD");
        return true;
    }

    /// <inheritdoc/>
    public bool Submit() => true;

    /// <inheritdoc/>
    public bool Wait()
    {
        CudaApi.Check(CudaApi.cuCtxSynchronize(), "cuCtxSynchronize");
        return true;
    }

    /// <inheritdoc/>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Gets the device pointer of a block, for a kernel argument.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="pointer">When the method returns, the device pointer.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge.</returns>
    internal bool TryGetPointer(in UnifiedMemoryBlock block, out ulong pointer)
    {
        var arena = (Arena*)block.DeviceHandle;

        if (arena == null)
        {
            pointer = 0;
            return false;
        }

        pointer = arena->Pointer + (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Releases the handle that roots this bridge.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        if (m_Self.IsAllocated)
        {
            m_Self.Free();
        }
    }
}

/// <summary>
/// A CUDA device this library created and owns: the context and the bridge that allocates on it.
/// </summary>
internal sealed class CudaDevice : IDisposable
{
    private readonly nint m_Context;

    private CudaDevice(nint context)
    {
        m_Context = context;
        Provider = new CudaUnifiedProvider();
    }

    /// <summary>
    /// Gets the bridge that allocates on this device.
    /// </summary>
    internal CudaUnifiedProvider Provider { get; }

    /// <summary>
    /// Gets a value indicating whether the CUDA driver is present on this machine.
    /// </summary>
    internal static bool IsAvailable => NativeInterop.IsWindows || NativeInterop.IsLinux;

    /// <summary>
    /// Creates a context on the first device.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if a context was created.</returns>
    internal static bool TryCreate(out CudaDevice? created)
    {
        created = null;

        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            CudaApi.Check(CudaApi.cuInit(0), "cuInit");
            CudaApi.Check(CudaApi.cuDeviceGet(out var device, 0), "cuDeviceGet");
            CudaApi.Check(CudaApi.cuCtxCreate_v2(out var context, 0, device), "cuCtxCreate");
            created = new CudaDevice(context);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Releases the bridge and the context.
    /// </summary>
    public void Dispose()
    {
        Provider.Dispose();
        CudaApi.cuCtxDestroy_v2(m_Context);
    }
}

/// <summary>
/// Runs one CUDA kernel: a PTX module the driver compiles, launched over two device pointers.
/// </summary>
internal sealed unsafe class CudaCompute : IDisposable
{
    private nint m_Module;
    private nint m_Function;
    private ulong m_Parameters;
    private nuint m_ParameterSize;
    private bool m_Pending;
    private bool m_Disposed;


    /// <summary>
    /// Loads a kernel from PTX text.
    /// </summary>
    /// <param name="ptx">The PTX source.</param>
    /// <param name="function">The entry point to run.</param>
    /// <returns><see langword="true"/> if the module and the function were loaded.</returns>
    internal bool SetPipeline(string ptx, string function)
    {
        if (m_Disposed || ptx.Length == 0 || function.Length == 0)
        {
            return false;
        }

        var text = Marshal.StringToHGlobalAnsi(ptx);

        try
        {
            CudaApi.Check(CudaApi.cuModuleLoadData(out m_Module, (void*)text), "cuModuleLoadData");
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }

        try
        {
            CudaApi.Check(CudaApi.cuModuleGetFunction(out m_Function, m_Module, function), "cuModuleGetFunction");
        }
        catch (Exception)
        {
            return false;
        }

        // The compiler gives a kernel its buffers through a constant the host fills before the launch, the way the CUDA
        // runtime does; a hand written kernel that takes them as arguments has no such constant and is launched with them.
        var descriptor = System.Text.RegularExpressions.Regex.Match(ptx, @"\.const\s+\.align\s+\d+\s+\.b8\s+([A-Za-z_][A-Za-z0-9_]*)\[(\d+)\]");

        if (descriptor.Success && ulong.TryParse(descriptor.Groups[2].Value, out var size))
        {
            try
            {
                CudaApi.Check(CudaApi.cuModuleGetGlobal_v2(out m_Parameters, out _, m_Module, descriptor.Groups[1].Value), "cuModuleGetGlobal");
                m_ParameterSize = (nuint)size;
            }
            catch (Exception)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Runs the kernel over three blocks of device memory.
    /// </summary>
    /// <param name="source">The device pointer the kernel reads.</param>
    /// <param name="sourceSize">The size of that block, in bytes.</param>
    /// <param name="destination">The device pointer the kernel writes.</param>
    /// <param name="destinationSize">The size of that block, in bytes.</param>
    /// <param name="parameters">The device pointer the structure the kernel takes is read from, or zero when it takes none.</param>
    /// <param name="lengths">The device pointer the lengths are read from the block that carries them.</param>
    /// <param name="lengthsSize">The size of that block, in bytes.</param>
    /// <param name="groups">The number of thread blocks on the x axis.</param>
    /// <param name="threadsPerGroup">The threads in a block.</param>
    /// <returns><see langword="true"/> if the launch completed.</returns>
    internal bool Dispatch(ulong source, ulong sourceSize, ulong destination, ulong destinationSize, ulong parametersAddress, ulong lengths, ulong lengthsSize, uint groups, uint threadsPerGroup, bool wait)
    {
        if (m_Disposed || m_Function == 0 || groups == 0 || threadsPerGroup == 0)
        {
            return false;
        }

        if (m_Pending && !WaitPending())
        {
            return false;
        }

        // The list of arguments a kernel that takes its blocks as arguments is launched with, when it has no constant.
        void** arguments = null;

        if (m_Parameters != 0)
        {
            // A structured buffer is a pointer and a count, and the kernel is given the three of them through the
            // constant the compiler left for them. The count is the size of the block rather than its element count,
            // which bounds every element the kernel can address; the element count it reads comes from the lengths.
            var descriptors = stackalloc ulong[6];
            descriptors[0] = source;
            descriptors[1] = sourceSize;
            descriptors[2] = destination;
            descriptors[3] = destinationSize;
            descriptors[4] = lengths;
            descriptors[5] = lengthsSize;

            if (CudaApi.cuMemcpyHtoD_v2(m_Parameters, descriptors, m_ParameterSize) != 0)
            {
                return false;
            }
        }
        else
        {
            // A kernel whose PTX takes its buffers as arguments has no constant to fill, so the three parameters the
            // emitter writes are passed instead: the source, the destination and the block the counts live in.
            var list = stackalloc void*[3];
            list[0] = &source;
            list[1] = &destination;
            list[2] = &lengths;
            arguments = list;
        }

        if (CudaApi.cuLaunchKernel(m_Function, groups, 1, 1, threadsPerGroup, 1, 1, 0, 0, arguments, null) != 0)
        {
            return false;
        }

        if (!wait)
        {
            m_Pending = true;
            return true;
        }

        return CudaApi.cuCtxSynchronize() == 0;
    }

    /// <summary>
    /// Waits for the dispatches that were recorded without waiting.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    internal bool WaitPending()
    {
        if (!m_Pending)
        {
            return true;
        }

        if (CudaApi.cuCtxSynchronize() != 0)
        {
            return false;
        }

        m_Pending = false;
        return true;
    }
    /// <summary>
    /// Releases the module.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        WaitPending();

        if (m_Module != 0)
        {
            CudaApi.cuModuleUnload(m_Module);
        }
    }
}

