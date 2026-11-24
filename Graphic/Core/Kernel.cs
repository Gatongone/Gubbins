using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// A kernel loaded on a <see cref="Gpu"/>, with the dispatch a caller makes over typed buffers.
/// </summary>
/// <remarks>
/// <para>
/// The type is what the generated entry point of a <c>[GpuKernel]</c> method dispatches through. It knows the artifact it
/// was loaded from, how many threads a group of it has, and how to turn the two buffers of a call into the bindings the
/// backend expects, including the block that carries their element counts.
/// </para>
/// <para>
/// <see cref="Run"/> waits for the dispatch to finish, which is what makes the destination readable when it returns.
/// <see cref="Dispatch"/> records the same work without waiting, for a caller that submits a frame's worth of kernels and
/// synchronizes once.
/// </para>
/// </remarks>
public sealed class Kernel
{
    /// <summary>
    /// The size of the block that carries the element counts of the buffers.
    /// </summary>
    private const int LengthsSize = 16;

    private readonly Gpu m_Gpu;
    private readonly GpuKernel m_Program;
    private bool m_Disposed;

    /// <param name="gpu">The layer the kernel runs on.</param>
    /// <param name="name">The kernel name, which is the file name of its artifact.</param>
    /// <param name="program">The pipeline that was loaded for this device.</param>
    /// <param name="threadsPerGroup">The threads in a group the kernel was written for.</param>
    internal Kernel(Gpu gpu, string name, GpuKernel program, int threadsPerGroup)
    {
        if (threadsPerGroup < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threadsPerGroup), threadsPerGroup, "A group has at least one thread.");
        }

        m_Gpu = gpu;
        m_Program = program;
        Name = name;
        ThreadsPerGroup = threadsPerGroup;
    }

    /// <summary>
    /// Gets the name the kernel was loaded under.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the number of threads in a group of the kernel.
    /// </summary>
    public int ThreadsPerGroup { get; }

    /// <summary>
    /// Runs the kernel over two buffers and waits for it to finish.
    /// </summary>
    /// <param name="source">The buffer the kernel reads.</param>
    /// <param name="target">The buffer the kernel writes.</param>
    /// <exception cref="GpuExecutionException">Thrown when the dispatch did not run.</exception>
    public void Run(GpuBuffer source, GpuBuffer target)
    {
        Dispatch(source, target);
        m_Gpu.Synchronize();
    }

    /// <summary>
    /// Runs the kernel over two buffers, over a number of groups the caller chooses, and waits for it to finish.
    /// </summary>
    /// <param name="source">The buffer the kernel reads.</param>
    /// <param name="target">The buffer the kernel writes.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <exception cref="GpuExecutionException">Thrown when the dispatch did not run.</exception>
    public void Run(GpuBuffer source, GpuBuffer target, uint groups)
    {
        Dispatch(source, target, groups);
        m_Gpu.Synchronize();
    }

    /// <summary>
    /// Records the kernel over two buffers without waiting, for a caller that synchronizes once per frame.
    /// </summary>
    /// <param name="source">The buffer the kernel reads.</param>
    /// <param name="target">The buffer the kernel writes.</param>
    /// <exception cref="GpuExecutionException">Thrown when the dispatch did not run.</exception>
    public void Dispatch(GpuBuffer source, GpuBuffer target) => Dispatch(source, target, Groups(source));

    /// <summary>
    /// Records the kernel over two buffers, over a number of groups the caller chooses, without waiting.
    /// </summary>
    /// <param name="source">The buffer the kernel reads.</param>
    /// <param name="target">The buffer the kernel writes.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <exception cref="GpuExecutionException">Thrown when the dispatch did not run.</exception>
    public void Dispatch(GpuBuffer source, GpuBuffer target, uint groups)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(Kernel));
        }

        if (source is null || target is null)
        {
            throw new ArgumentNullException(source is null ? nameof(source) : nameof(target));
        }

        if (groups == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groups), groups, "A dispatch runs at least one group.");
        }

        var memory = m_Gpu.Memory;
        var lengths = memory.Allocate(LengthsSize, GpuAccess.Shared);

        try
        {
            var counts = new uint[] { (uint) source.Length, (uint) target.Length, 0, 0 };
            memory.Upload(lengths, MemoryMarshal.AsBytes(counts.AsSpan()));

            if (!m_Program.TryDispatch(source.Block, target.Block, groups, lengths))
            {
                throw new GpuExecutionException($"The kernel '{Name}' did not run.");
            }

            target.MarkWrittenByGpu();
        }
        finally
        {
            memory.Release(lengths);
        }
    }

    /// <summary>
    /// Releases the pipeline.
    /// </summary>
    internal void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        m_Program.Dispose();
    }

    /// <summary>
    /// Computes the number of groups an element count covers.
    /// </summary>
    /// <param name="source">The buffer the kernel reads.</param>
    /// <returns>The number of groups on the x axis.</returns>
    private uint Groups(GpuBuffer source)
    {
        var groups = ((long) source.Length + ThreadsPerGroup - 1) / ThreadsPerGroup;

        return groups <= 0 ? 0 : (uint) groups;
    }
}
