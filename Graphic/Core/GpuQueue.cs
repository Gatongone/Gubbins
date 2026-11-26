namespace Gubbins.Graphic;

/// <summary>
/// A group of kernel dispatches that are recorded without waiting and waited for once.
/// </summary>
/// <remarks>
/// The backends submit a dispatch when it is recorded, so <see cref="Wait"/> is the point where the processor waits for
/// all of them. A queue is the shape a frame uses: record the frame's dispatches, then wait once before reading a result.
/// </remarks>
public sealed class GpuQueue : IDisposable
{
    private readonly List<GpuKernel> m_Pending = [];
    private readonly List<(GpuMemory Memory, GpuBlock Block)> m_Retained = [];
    private bool m_Disposed;

    /// <summary>
    /// Keeps a block alive until the dispatches that read it have been waited for.
    /// </summary>
    /// <param name="memory">The memory the block belongs to.</param>
    /// <param name="block">The block to release after the wait.</param>
    internal void Retain(GpuMemory memory, GpuBlock block) => m_Retained.Add((memory, block));

    /// <summary>
    /// Records one dispatch without waiting for it.
    /// </summary>
    /// <param name="kernel">The kernel to run.</param>
    /// <param name="source">The block the kernel reads.</param>
    /// <param name="destination">The block the kernel writes.</param>
    /// <param name="groups">The number of groups on the x axis.</param>
    /// <param name="lengths">The block that carries the element counts, or <see langword="null"/>.</param>
    /// <param name="parameters">The block that carries the structure the kernel takes, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the dispatch was recorded.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the queue was disposed.</exception>
    public bool Dispatch(GpuKernel kernel, GpuBlock source, GpuBlock destination, uint groups, GpuBlock? lengths = null, GpuBlock? parameters = null)
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuQueue));
        }

        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        if (!kernel.TryDispatch(source, destination, groups, lengths, parameters, wait: false))
        {
            return false;
        }

        if (!m_Pending.Contains(kernel))
        {
            m_Pending.Add(kernel);
        }

        return true;
    }

    /// <summary>
    /// Waits for every dispatch recorded since the last wait, which is the barrier between two groups of work.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the queue was disposed.</exception>
    public bool Barrier() => Wait();

    /// <summary>
    /// Waits for every dispatch recorded since the last wait.
    /// </summary>
    /// <returns><see langword="true"/> when every submission has completed.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the queue was disposed.</exception>
    public bool Wait()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(GpuQueue));
        }

        foreach (var kernel in m_Pending)
        {
            if (!kernel.Wait())
            {
                return false;
            }
        }

        m_Pending.Clear();

        foreach (var (memory, block) in m_Retained)
        {
            memory.Release(block);
        }

        m_Retained.Clear();
        return true;
    }

    /// <summary>
    /// Waits for the outstanding dispatches.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        Wait();
        m_Disposed = true;
    }
}
