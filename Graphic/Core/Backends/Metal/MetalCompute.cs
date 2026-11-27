
namespace Gubbins.Graphic.Metal;

/// <summary>
/// Runs one compute pipeline on a Metal device: a metallib loaded from disk, one function, and the dispatches that are
/// recorded either into a command buffer of this object's own or into one a batch owns.
/// </summary>
/// <remarks>
/// Metal loads its shaders from a file rather than from bytes, so this is the one backend whose kernel is named by a path.
/// The device and the queue belong to the bridge; the library, the function and the pipeline state are this object's.
/// <para>
/// The recording and the submission are separate here for the same reason the batch exists: <see cref="Record"/> binds the
/// pipeline and the arguments into an encoder without closing it, so one command buffer can carry the dispatches of
/// several kernels, while <see cref="Dispatch"/> is the single dispatch shape that owns its command buffer end to end.
/// </para>
/// </remarks>
internal sealed unsafe class MetalCompute : IDisposable
{
    private readonly nint m_Device;
    private readonly nint m_Queue;
    private nint m_Library;
    private nint m_Function;
    private nint m_Pipeline;
    private string m_Error = string.Empty;
    private readonly List<nint> m_Pending = [];
    private bool m_Disposed;

    /// <param name="device">The <c>MTLDevice</c> to run on.</param>
    /// <param name="queue">The queue to submit to.</param>
    internal MetalCompute(nint device, nint queue)
    {
        m_Device = device;
        m_Queue = queue;
    }

    /// <summary>
    /// Gets the text Metal gave for the last pipeline that was refused, which is empty when the last one loaded.
    /// </summary>
    internal string LastError => m_Error;

    /// <summary>
    /// Loads a kernel from a metallib.
    /// </summary>
    /// <param name="path">The file the library is loaded from.</param>
    /// <param name="function">The kernel function to run.</param>
    /// <returns><see langword="true"/> if the pipeline state was created.</returns>
    internal bool SetPipeline(string path, string function)
    {
        if (m_Disposed || path.Length == 0 || function.Length == 0)
        {
            return false;
        }

        var library = MetalApi.NewLibraryWithFile(m_Device, path, out var error);

        // Only the library is owned here; the error text is a managed copy.
        m_Error = error;

        return SetPipeline(library, function);
    }

    /// <summary>
    /// Creates the pipeline state from a library.
    /// </summary>
    /// <param name="library">The <c>MTLLibrary</c> that holds the kernel.</param>
    /// <param name="function">The kernel function to run.</param>
    /// <returns><see langword="true"/> if the pipeline state was created.</returns>
    private bool SetPipeline(nint library, string function)
    {
        if (m_Disposed || library == 0 || function.Length == 0)
        {
            return false;
        }

        m_Library = library;

        var entry = MetalApi.NewFunction(library, function);

        if (entry == 0)
        {
            return false;
        }

        m_Function = entry;

        var pipeline = MetalApi.NewComputePipelineState(m_Device, entry, out var error);
        m_Error = error;

        if (pipeline == 0)
        {
            return false;
        }

        m_Pipeline = pipeline;
        return true;
    }

    /// <summary>
    /// Compiles a kernel from Metal Shading Language source.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="function">The kernel function to run.</param>
    /// <returns><see langword="true"/> if the pipeline state was created.</returns>
    internal bool SetPipelineFromSource(string source, string function)
    {
        if (m_Disposed || source.Length == 0 || function.Length == 0)
        {
            return false;
        }

        var library = MetalApi.NewLibrary(m_Device, source, out var error);
        m_Error = error;

        return SetPipeline(library, function);
    }

    /// <summary>
    /// Gets the command queue this pipeline submits to, which a batched command buffer has to match.
    /// </summary>
    internal nint Queue => m_Queue;

    /// <summary>
    /// Creates a command buffer on the queue, with one compute encoder open on it.
    /// </summary>
    /// <param name="commandBuffer">When the method returns, the <c>MTLCommandBuffer</c>, retained.</param>
    /// <param name="encoder">When the method returns, the <c>MTLComputeCommandEncoder</c>, retained.</param>
    /// <returns><see langword="true"/> if both were created.</returns>
    /// <remarks>
    /// Metal hands both out autoreleased, so they are retained to outlive the pool this method uses, and the caller releases
    /// them once it has ended the encoding and committed or discarded the buffer.
    /// </remarks>
    internal bool BeginCommandBuffer(out nint commandBuffer, out nint encoder)
    {
        commandBuffer = 0;
        encoder = 0;

        if (m_Disposed)
        {
            return false;
        }

        var pool = MetalApi.PushAutoreleasePool();

        try
        {
            var created = MetalApi.Send(m_Queue, "commandBuffer");

            if (created == 0)
            {
                return false;
            }

            var coding = MetalApi.ComputeCommandEncoder(created);

            if (coding == 0)
            {
                return false;
            }

            MetalApi.Retain(created);
            MetalApi.Retain(coding);

            commandBuffer = created;
            encoder = coding;
            return true;
        }
        finally
        {
            MetalApi.PopAutoreleasePool(pool);
        }
    }

    /// <summary>
    /// Records the loaded pipeline over two buffers into an encoder that is already open.
    /// </summary>
    /// <param name="encoder">The <c>MTLComputeCommandEncoder</c> to record into; it is left open.</param>
    /// <param name="sourceBuffer">The <c>MTLBuffer</c> the shader reads.</param>
    /// <param name="sourceOffset">The offset of the input inside it.</param>
    /// <param name="destinationBuffer">The <c>MTLBuffer</c> the shader writes.</param>
    /// <param name="destinationOffset">The offset of the output inside it.</param>
    /// <param name="parametersBuffer">The <c>MTLBuffer</c> that carries the structure the kernel takes, or zero when it takes none.</param>
    /// <param name="parametersOffset">The offset of that structure inside it.</param>
    /// <param name="lengthsBuffer">The <c>MTLBuffer</c> the lengths are read from, or zero to bind the input again.</param>
    /// <param name="lengthsOffset">The offset of the lengths inside it.</param>
    /// <param name="textures">The <c>MTLTexture</c> handles to bind, in declaration order.</param>
    /// <param name="samplers">The <c>MTLSamplerState</c> handles to bind, in declaration order.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="threadsPerGroup">The threads in a group.</param>
    /// <returns><see langword="true"/> if the dispatch was recorded.</returns>
    /// <remarks>
    /// The pipeline state is set on every record, so one encoder can carry dispatches of different kernels: that is what
    /// makes a batched command buffer a switch of state rather than a new encoder.
    /// </remarks>
    internal bool Record(nint encoder, nint sourceBuffer, nuint sourceOffset, nint destinationBuffer, nuint destinationOffset, nint parametersBuffer, nuint parametersOffset, nint lengthsBuffer, nuint lengthsOffset, ReadOnlySpan<nint> textures, ReadOnlySpan<nint> samplers, uint groups, uint threadsPerGroup)
    {
        if (m_Disposed || m_Pipeline == 0 || encoder == 0 || sourceBuffer == 0 || destinationBuffer == 0 || groups == 0 || threadsPerGroup == 0)
        {
            return false;
        }

        MetalApi.SetComputePipelineState(encoder, m_Pipeline);
        MetalApi.SetBuffer(encoder, sourceBuffer, sourceOffset, 0);
        MetalApi.SetBuffer(encoder, destinationBuffer, destinationOffset, 1);

        // The block of the structure a kernel takes is bound after the buffers, and the counts after it when it is there.
        if (parametersBuffer != 0)
        {
            MetalApi.SetBuffer(encoder, parametersBuffer, parametersOffset, 2);

            if (lengthsBuffer != 0)
            {
                MetalApi.SetBuffer(encoder, lengthsBuffer, lengthsOffset, 3);
            }
        }
        else if (lengthsBuffer != 0)
        {
            MetalApi.SetBuffer(encoder, lengthsBuffer, lengthsOffset, 2);
        }

        for (var index = 0; index < textures.Length; index++)
        {
            if (textures[index] != 0)
            {
                MetalApi.SetTexture(encoder, textures[index], (nuint)index);
            }
        }

        for (var index = 0; index < samplers.Length; index++)
        {
            if (samplers[index] != 0)
            {
                MetalApi.SetSamplerState(encoder, samplers[index], (nuint)index);
            }
        }

        var threadgroups = new MetalSize { Width = groups, Height = 1, Depth = 1 };
        var threads = new MetalSize { Width = threadsPerGroup, Height = 1, Depth = 1 };
        MetalApi.DispatchThreadgroups(encoder, threadgroups, threads);

        return true;
    }

    /// <summary>
    /// Runs the loaded pipeline over two buffers.
    /// </summary>
    /// <param name="sourceBuffer">The <c>MTLBuffer</c> the shader reads.</param>
    /// <param name="sourceOffset">The offset of the input inside it.</param>
    /// <param name="destinationBuffer">The <c>MTLBuffer</c> the shader writes.</param>
    /// <param name="destinationOffset">The offset of the output inside it.</param>
    /// <param name="parametersBuffer">The <c>MTLBuffer</c> that carries the structure the kernel takes, or zero when it takes none.</param>
    /// <param name="parametersOffset">The offset of that structure inside it.</param>
    /// <param name="lengthsBuffer">The <c>MTLBuffer</c> the lengths are read from, or zero to bind the input again.</param>
    /// <param name="lengthsOffset">The offset of the lengths inside it.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <param name="threadsPerGroup">The threads in a group.</param>
    /// <returns><see langword="true"/> if the dispatch ran to completion.</returns>
    /// <remarks>
    /// This is the single dispatch shape: one command buffer, one encoder, one submission. It keeps working exactly as it
    /// did after the recording path was split out, so a caller that has no batch pays only for the pool and the handles.
    /// </remarks>
    internal bool Dispatch(nint sourceBuffer, nuint sourceOffset, nint destinationBuffer, nuint destinationOffset, nint parametersBuffer, nuint parametersOffset, nint lengthsBuffer, nuint lengthsOffset, ReadOnlySpan<nint> textures, ReadOnlySpan<nint> samplers, uint groups, uint threadsPerGroup, bool wait)
    {
        if (m_Disposed || m_Pipeline == 0 || sourceBuffer == 0 || destinationBuffer == 0 || groups == 0 || threadsPerGroup == 0)
        {
            return false;
        }

        if (!BeginCommandBuffer(out var commandBuffer, out var encoder))
        {
            return false;
        }

        if (!Record(encoder, sourceBuffer, sourceOffset, destinationBuffer, destinationOffset, parametersBuffer, parametersOffset, lengthsBuffer, lengthsOffset, textures, samplers, groups, threadsPerGroup))
        {
            MetalApi.SendVoid(encoder, "endEncoding");
            MetalApi.Release(encoder);
            MetalApi.Release(commandBuffer);
            return false;
        }

        // The encoder has to be closed before the buffer is committed, and the wait is what makes the write visible.
        MetalApi.SendVoid(encoder, "endEncoding");
        MetalApi.Release(encoder);
        MetalApi.SendVoid(commandBuffer, "commit");

        if (wait)
        {
            MetalApi.SendVoid(commandBuffer, "waitUntilCompleted");
            MetalApi.Release(commandBuffer);
        }
        else
        {
            m_Pending.Add(commandBuffer);
        }

        return true;
    }

    /// <summary>
    /// Waits for the dispatches that were recorded without waiting.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    internal bool WaitPending()
    {
        foreach (var commandBuffer in m_Pending)
        {
            MetalApi.Send(commandBuffer, "waitUntilCompleted");
            MetalApi.Release(commandBuffer);
        }

        m_Pending.Clear();
        return true;
    }

    /// <summary>
    /// Releases the pipeline state, the function and the library.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        WaitPending();

        if (m_Pipeline != 0)
        {
            MetalApi.Release(m_Pipeline);
        }

        if (m_Function != 0)
        {
            MetalApi.Release(m_Function);
        }

        if (m_Library != 0)
        {
            MetalApi.Release(m_Library);
        }
    }
}

