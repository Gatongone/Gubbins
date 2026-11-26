
using System.Runtime.InteropServices;

#pragma warning disable CS8500

namespace Gubbins.Graphic;

/// <summary>
/// An OpenGL implementation of <see cref="IUnifiedProvider"/>: a segment is one buffer with a persistent, coherent
/// mapping, so the address the CPU writes is the address the shader reads.
/// </summary>
/// <remarks>
/// The context has to be current on the calling thread, which is the rule OpenGL has always had. Nothing here flushes or
/// invalidates: a coherent mapping needs no maintenance, which is why the reported kind is
/// <see cref="UnifiedMemoryKind.SharedCoherent"/>.
/// </remarks>
internal sealed unsafe class GlUnifiedProvider : IUnifiedProvider
{
    private const uint ArrayBuffer = 0x8892;
    private const uint DynamicStorageBit = 0x0100;
    private const uint MapPersistentBit = 0x0040;
    private const uint MapCoherentBit = 0x0080;
    private const uint MapReadBit = 0x0001;
    private const uint MapWriteBit = 0x0002;

    /// <summary>
    /// The bookkeeping of one segment: the buffer, its mapping and its size.
    /// </summary>
    private struct Arena
    {
        public uint Buffer;
        public byte* Host;
        public nuint Size;
    }

    private readonly GlEntryPoints m_Api;
    private GCHandle m_Self;
    private bool m_Disposed;

    /// <param name="api">The entry points of the current context.</param>
    internal GlUnifiedProvider(GlEntryPoints api)
    {
        m_Api = api;
    }

    /// <inheritdoc/>
    public nuint Alignment => 256;

    /// <inheritdoc/>
    public nuint MaxSegmentSize => 0;

    /// <inheritdoc/>
    public nuint WorkingSetLimit => 0;

    /// <inheritdoc/>
    public UnifiedProvider GetProvider() => ProviderAdapter<GlUnifiedProvider>.Create(this, ref m_Self);

    /// <summary>
    /// Creates a two dimensional texture on the current context.
    /// </summary>
    /// <param name="width">The width, in pixels.</param>
    /// <param name="height">The height, in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <returns>The texture, to be disposed when it is no longer read.</returns>
    public GpuTexture2D CreateTexture(int width, int height, GpuTextureFormat format)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GlUnifiedProvider));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "A texture is at least one pixel wide and high.");
        }

        return new GpuTexture2D(new GlTexture(m_Api, width, height, format), width, height, format);
    }

    /// <summary>
    /// Creates a sampler object on the current context.
    /// </summary>
    /// <param name="sampler">The sampler description.</param>
    /// <returns>The sampler, to be disposed when it is no longer bound.</returns>
    public GpuSamplerState CreateSampler(GpuSampler sampler)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GlUnifiedProvider));
        }

        return new GpuSamplerState(new GlSamplerState(m_Api, sampler));
    }

    /// <inheritdoc/>
    public void* Alloc(nuint size, nuint alignment, UnifiedMemoryUsage usage, out void* deviceHandle, out ulong deviceAddress, out UnifiedMemoryKind kind)
    {
        var arena = (Arena*)NativeInterop.AllocZeroed((nuint)sizeof(Arena));

        if (arena == null)
        {
            throw new OutOfMemoryException($"Unable to allocate {sizeof(Arena)} bytes of bridge bookkeeping.");
        }

        var buffer = 0u;
        m_Api.GenBuffers(1, &buffer);
        m_Api.BindBuffer(ArrayBuffer, buffer);

        // A segment the device alone uses is stored without a mapping: a buffer the CPU maps for itself is host memory
        // the device reaches across the bus, which is the whole difference between the two kinds of block.
        var device = usage == UnifiedMemoryUsage.Device;

        m_Api.BufferStorage(ArrayBuffer, size, null, device
            ? DynamicStorageBit
            : DynamicStorageBit | MapPersistentBit | MapCoherentBit | MapReadBit | MapWriteBit);

        var host = device
            ? null
            : (byte*)m_Api.MapBufferRange(buffer, 0, size, MapReadBit | MapWriteBit | MapPersistentBit | MapCoherentBit);

        if (!device && host == null)
        {
            m_Api.DeleteBuffers(1, &buffer);
            NativeInterop.Free(arena);
            throw new OutOfMemoryException($"Mapping a {size} byte segment failed.");
        }

        arena->Buffer = buffer;
        arena->Host = host;
        arena->Size = size;

        deviceHandle = arena;
        deviceAddress = 0;
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

        var buffer = arena->Buffer;

        if (arena->Host != null)
        {
            m_Api.UnmapBuffer(buffer);
        }

        m_Api.DeleteBuffers(1, &buffer);
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

        var sourceHost = sourceArena->Host + source.Offset + sourceOffset;
        var destinationHost = destinationArena->Host + destination.Offset + destinationOffset;

        if (sourceArena->Host != null && destinationArena->Host != null)
        {
            // Both segments are mapped, so the copy is a plain move in the address space the shader reads as well.
            Buffer.MemoryCopy(sourceHost, destinationHost, (long)size, (long)size);
            return true;
        }

        if (sourceArena->Host != null)
        {
            m_Api.BufferSubData(destinationArena->Buffer, (ulong)(destinationHost - destinationArena->Host), (nint)size, sourceHost);
            return true;
        }

        if (destinationArena->Host != null)
        {
            m_Api.GetBufferSubData(sourceArena->Buffer, (ulong)(sourceHost - sourceArena->Host), (nint)size, destinationHost);
            return true;
        }

        // Neither segment is mapped, so the device copies one to the other.
        m_Api.CopyBufferSubData(sourceArena->Buffer, destinationArena->Buffer, (ulong)(sourceHost - sourceArena->Host), (ulong)(destinationHost - destinationArena->Host), (nint)size);
        return true;
    }

    /// <inheritdoc/>
    public bool Submit() => true;

    /// <inheritdoc/>
    public bool Wait()
    {
        m_Api.Finish();
        return true;
    }

    /// <inheritdoc/>
    public bool SetResidency(in UnifiedMemoryBlock block, UnifiedMemoryResidency residency) => false;

    /// <summary>
    /// Gets the buffer and offset a block lives at, for a shader storage binding.
    /// </summary>
    /// <param name="block">The block to describe.</param>
    /// <param name="buffer">When the method returns, the buffer name.</param>
    /// <param name="offset">When the method returns, the offset inside it.</param>
    /// <returns><see langword="true"/> if the block belongs to this bridge.</returns>
    internal bool TryGetBuffer(in UnifiedMemoryBlock block, out uint buffer, out ulong offset)
    {
        var arena = (Arena*)block.DeviceHandle;

        if (arena == null)
        {
            buffer = 0;
            offset = 0;
            return false;
        }

        buffer = arena->Buffer;
        offset = (ulong)block.Offset;
        return true;
    }

    /// <summary>
    /// Releases every segment this bridge still owns.
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
/// An OpenGL device this library created and owns: the context, and the bridge that allocates on it.
/// </summary>
internal sealed unsafe class GlDevice : IDisposable
{
    private GlDevice(GlContext context)
    {
        Context = context;
        Provider = new GlUnifiedProvider(context.EntryPoints);

        // A core profile binds array buffer state through a vertex array object, and buffer storage needs the binding.
        var api = context.EntryPoints;
        var vao = 0u;
        api.GenVertexArrays(1, &vao);
        api.BindVertexArray(vao);
    }

    /// <summary>
    /// Gets the context the bridge runs on.
    /// </summary>
    internal GlContext Context { get; }

    /// <summary>
    /// Gets the bridge that allocates on this context.
    /// </summary>
    internal GlUnifiedProvider Provider { get; }

    /// <summary>
    /// Gets the entry points of the context.
    /// </summary>
    internal GlEntryPoints EntryPoints => Context.EntryPoints;

    /// <summary>
    /// Creates a device.
    /// </summary>
    /// <param name="created">When the method returns, the device, or <see langword="null"/> when creation failed.</param>
    /// <returns><see langword="true"/> if a 4.5 core context was created.</returns>
    internal static bool TryCreate(out GlDevice? created)
    {
        created = null;

        if (!NativeInterop.IsWindows || !GlContext.TryCreate(out var context))
        {
            return false;
        }

        created = new GlDevice(context!);
        return true;
    }

    /// <summary>
    /// Releases the bridge and the context.
    /// </summary>
    public void Dispose()
    {
        Provider.Dispose();
        Context.Dispose();
    }
}

