
namespace Gubbins.Graphic;

/// <summary>
/// Runs one compute pipeline on an OpenGL context: a GLSL program bound to two shader storage buffers and dispatched, then
/// waited for with <c>glFinish</c>.
/// </summary>
/// <remarks>
/// OpenGL has no bytecode this layer loads, so the kernel is GLSL source. The buffers are the same persistent mappings the
/// bridge hands out, which is what makes the dispatch read the block the CPU wrote.
/// </remarks>
internal sealed unsafe class GlCompute : IDisposable
{
    private const uint ComputeShader = 0x91B9;
    private const uint ShaderStorageBuffer = 0x90D2;
    private const uint CompileStatus = 0x8B81;
    private const uint LinkStatus = 0x8B82;
    private const uint AllBarrierBits = 0xFFFFFFFF;
    private const uint Texture2D = 0x0DE1;
    private const uint Texture0 = 0x84C0;

    private readonly GlEntryPoints m_Api;
    private uint m_Program;
    private bool m_Disposed;

    /// <param name="api">The entry points of the current context.</param>
    internal GlCompute(GlEntryPoints api)
    {
        m_Api = api;
    }

    /// <summary>
    /// Compiles and links a compute program.
    /// </summary>
    /// <param name="source">The GLSL source.</param>
    /// <returns><see langword="true"/> if the program linked.</returns>
    internal bool SetPipeline(string source)
    {
        if (m_Disposed || source.Length == 0)
        {
            return false;
        }

        var shader = m_Api.CreateShader(ComputeShader);
        var text = System.Runtime.InteropServices.Marshal.StringToHGlobalAnsi(source);
        var pointer = (byte*)text;
        var length = source.Length;
        m_Api.ShaderSource(shader, 1, &pointer, &length);
        System.Runtime.InteropServices.Marshal.FreeHGlobal(text);
        m_Api.CompileShader(shader);

        var status = 0;
        m_Api.GetShaderiv(shader, CompileStatus, &status);

        if (status == 0)
        {
            var log = stackalloc byte[1024];
            var written = 0;
            m_Api.GetShaderInfoLog(shader, 1024, &written, log);
            return false;
        }

        var program = m_Api.CreateProgram();
        m_Api.AttachShader(program, shader);
        m_Api.LinkProgram(program);
        m_Api.GetProgramiv(program, LinkStatus, &status);

        if (status == 0)
        {
            return false;
        }

        m_Program = program;
        return true;
    }

    /// <summary>
    /// Runs the program over two buffers.
    /// </summary>
    /// <param name="sourceBuffer">The buffer bound to binding 0.</param>
    /// <param name="sourceOffset">The offset of the input inside it.</param>
    /// <param name="sourceSize">The size of the input.</param>
    /// <param name="destinationBuffer">The buffer bound to binding 1.</param>
    /// <param name="destinationOffset">The offset of the output inside it.</param>
    /// <param name="destinationSize">The size of the output.</param>
    /// <param name="parametersBuffer">The buffer bound after the buffers, which carries the structure a kernel takes, or zero when it takes none.</param>
    /// <param name="parametersOffset">The offset of that structure inside it.</param>
    /// <param name="parametersSize">The size of that structure.</param>
    /// <param name="lengthsBuffer">The buffer bound after the buffers, or zero to reuse the input.</param>
    /// <param name="lengthsOffset">The offset of the lengths inside it.</param>
    /// <param name="lengthsSize">The size of the lengths.</param>
    /// <param name="groups">The number of thread groups on the x axis.</param>
    /// <returns><see langword="true"/> if the dispatch completed.</returns>
    /// <remarks>
    /// The blocks of one segment share a buffer, so every binding is a range and never the whole buffer: binding the
    /// whole buffer would make every block read and write the words at the start of the segment instead of its own.
    /// </remarks>
    internal bool Dispatch(uint sourceBuffer, ulong sourceOffset, ulong sourceSize, uint destinationBuffer, ulong destinationOffset, ulong destinationSize, uint parametersBuffer, ulong parametersOffset, ulong parametersSize, uint lengthsBuffer, ulong lengthsOffset, ulong lengthsSize, ReadOnlySpan<uint> textures, ReadOnlySpan<uint> samplers, uint groups, bool wait)
    {
        if (m_Disposed || m_Program == 0 || sourceBuffer == 0 || destinationBuffer == 0 || groups == 0)
        {
            return false;
        }

        m_Api.UseProgram(m_Program);
        m_Api.BindBufferRange(ShaderStorageBuffer, 0, sourceBuffer, (nint)sourceOffset, (nint)sourceSize);
        m_Api.BindBufferRange(ShaderStorageBuffer, 1, destinationBuffer, (nint)destinationOffset, (nint)destinationSize);

        // The block of the structure a kernel takes is bound after the buffers, and the counts after it when it is there.
        if (parametersBuffer != 0)
        {
            m_Api.BindBufferRange(ShaderStorageBuffer, 2, parametersBuffer, (nint)parametersOffset, (nint)parametersSize);

            if (lengthsBuffer != 0)
            {
                m_Api.BindBufferRange(ShaderStorageBuffer, 3, lengthsBuffer, (nint)lengthsOffset, (nint)lengthsSize);
            }
            else
            {
                m_Api.BindBufferRange(ShaderStorageBuffer, 3, sourceBuffer, (nint)sourceOffset, (nint)sourceSize);
            }
        }
        else if (lengthsBuffer == 0)
        {
            m_Api.BindBufferRange(ShaderStorageBuffer, 2, sourceBuffer, (nint)sourceOffset, (nint)sourceSize);
        }
        else
        {
            m_Api.BindBufferRange(ShaderStorageBuffer, 2, lengthsBuffer, (nint)lengthsOffset, (nint)lengthsSize);
        }

        for (var index = 0; index < textures.Length; index++)
        {
            if (textures[index] != 0)
            {
                m_Api.ActiveTexture(Texture0 + (uint)index);
                m_Api.BindTexture(Texture2D, textures[index]);
                m_Api.BindSampler((uint)index, samplers[index]);
            }
        }

        m_Api.DispatchCompute(groups, 1, 1);
        m_Api.MemoryBarrier(AllBarrierBits);

        // The wait is a finish rather than a fence: the fence object is only worth its extra state once the layer has to
        // overlap work, and a wrong sync object silently reads stale memory.
        m_Api.Finish();
        return true;
    }

    /// <summary>
    /// Waits for the dispatches that were recorded without waiting, which this backend runs immediately.
    /// </summary>
    /// <returns>Always <see langword="true"/>.</returns>
    internal bool WaitPending() => true;

    /// <summary>
    /// Releases the program.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        _ = m_Program;
    }
}

