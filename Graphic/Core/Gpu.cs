using System.Runtime.InteropServices;

namespace Gubbins.Graphic;

/// <summary>
/// A device the layer created, with the memory and the kernels a caller works through: the surface a program uses when it
/// wants to run a kernel over its data without naming a graphics API.
/// </summary>
/// <remarks>
/// <para>
/// The type is the owner: it creates the device, hands out typed buffers allocated on it, loads the kernels that were
/// built for it, and releases all of them together. A caller does not see which backend was selected, which heap served a
/// buffer, or which queue a dispatch ran on; the same code runs on every machine the layer supports.
/// </para>
/// <para>
/// A kernel and the buffers it reads are declared once, in C#: the method carries <c>[GpuKernel]</c> and compiles as an
/// ordinary method, so the central processor can call it directly as the reference the device result is compared
/// against. The source generator produces the typed <c>Run</c> entry point beside it; this type is what that entry point
/// dispatches through.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var gpu = Gpu.Create();
/// using var input = gpu.Upload(new uint[] { 1, 2, 3 });
/// using var output = gpu.Allocate&lt;uint&gt;(input.Length);
///
/// gpu.LoadKernel("Scale").Run(input, output);
///
/// ReadOnlySpan&lt;uint&gt; result = output.CpuSpan;
/// </code>
/// </example>
public sealed class Gpu : IDisposable
{
    /// <summary>
    /// The directory the build writes the kernel artifacts to, beside the program.
    /// </summary>
    internal const string KernelFolder = "Kernels";

    private readonly GpuDevice m_Device;
    private readonly GpuMemory m_Memory;
    private readonly Dictionary<string, Kernel> m_Kernels = new(StringComparer.Ordinal);
    private bool m_Disposed;

    private Gpu(GpuDevice device)
    {
        m_Device = device;
        m_Memory = device.CreateMemory();
    }

    /// <summary>
    /// Creates the layer on the device this machine has.
    /// </summary>
    /// <returns>The layer, to be disposed when the program is done with it.</returns>
    /// <exception cref="GpuUnavailableException">Thrown when no device could be created.</exception>
    public static Gpu Create()
    {
        if (!GpuDevice.TryCreate(out var device) || device is null)
        {
            throw new GpuUnavailableException("This machine has no graphics device the layer can use.");
        }

        return new Gpu(device);
    }

    /// <summary>
    /// Creates the layer on the device this machine has, when there is one.
    /// </summary>
    /// <param name="gpu">When the method returns, the layer, or <see langword="null"/> when there is no device.</param>
    /// <returns><see langword="true"/> when a layer was created; otherwise, <see langword="false"/>.</returns>
    public static bool TryCreate(out Gpu? gpu)
    {
        gpu = null;

        if (!GpuDevice.TryCreate(out var device) || device is null)
        {
            return false;
        }

        gpu = new Gpu(device);
        return true;
    }

    /// <summary>
    /// Gets a description of the device and the API behind it, for a log line.
    /// </summary>
    public string Description => m_Device.Description;

    /// <summary>
    /// Gets a value indicating whether the memory shared with the central processor is the DRAM it runs from.
    /// </summary>
    public bool IsUnifiedMemory => m_Device.IsUnifiedMemory;

    /// <summary>
    /// Gets the directory the kernels of this program were built into.
    /// </summary>
    public static string KernelDirectory => Path.Combine(AppContext.BaseDirectory, KernelFolder);

    /// <summary>
    /// Gets the memory the buffers are allocated from, for a caller that works with the primitive layer.
    /// </summary>
    internal GpuMemory Memory => m_Memory;

    /// <summary>
    /// Allocates a buffer of elements the device and the central processor address.
    /// </summary>
    /// <typeparam name="T">The element type, which is an unmanaged value.</typeparam>
    /// <param name="count">The number of elements.</param>
    /// <param name="access">Whether the central processor writes the buffer directly, or the device owns it.</param>
    /// <returns>The buffer, to be disposed when it is no longer read.</returns>
    public GpuBuffer<T> Allocate<T>(int count, GpuAccess access = GpuAccess.Shared) where T : unmanaged
    {
        ThrowIfDisposed();

        return new GpuBuffer<T>(m_Memory, count, access);
    }

    /// <summary>
    /// Allocates a buffer and writes the values into it.
    /// </summary>
    /// <typeparam name="T">The element type, which is an unmanaged value.</typeparam>
    /// <param name="values">The values to upload.</param>
    /// <returns>The buffer, to be disposed when it is no longer read.</returns>
    public GpuBuffer<T> Upload<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        var buffer = Allocate<T>(values.Length);
        buffer.Upload(values);

        return buffer;
    }

    /// <summary>
    /// Loads the kernel of that name on this device, reusing the one already loaded.
    /// </summary>
    /// <param name="name">The kernel name, which is the file name of its artifact without the extension.</param>
    /// <param name="threadsPerGroup">
    /// The threads in a group the kernel was written for, which is the size the generated entry point knows.
    /// </param>
    /// <returns>The kernel, which this layer owns and releases with itself.</returns>
    /// <exception cref="GpuExecutionException">Thrown when the artifact for this device is missing or was refused.</exception>
    public Kernel LoadKernel(string name, int threadsPerGroup = GpuIntrinsics.DefaultThreadsPerGroup)
    {
        ThrowIfDisposed();

        if (m_Kernels.TryGetValue(name, out var cached))
        {
            return cached;
        }

        var program = GpuKernel.LoadFromDirectory(m_Device, KernelDirectory, name)
            ?? throw new GpuExecutionException($"The kernel '{name}' has no artifact for {Description} in '{KernelDirectory}'.");

        var kernel = new Kernel(this, name, program, threadsPerGroup);
        m_Kernels.Add(name, kernel);

        return kernel;
    }

    /// <summary>
    /// Loads the kernel of that name, when the artifact for this device is there.
    /// </summary>
    /// <param name="name">The kernel name, which is the file name of its artifact without the extension.</param>
    /// <param name="kernel">When the method returns, the kernel, or <see langword="null"/> when it is not there.</param>
    /// <param name="threadsPerGroup">The threads in a group the kernel was written for.</param>
    /// <returns><see langword="true"/> when the kernel is loaded; otherwise, <see langword="false"/>.</returns>
    public bool TryLoadKernel(string name, out Kernel? kernel, int threadsPerGroup = GpuIntrinsics.DefaultThreadsPerGroup)
    {
        kernel = null;

        if (m_Disposed)
        {
            return false;
        }

        if (m_Kernels.TryGetValue(name, out kernel))
        {
            return true;
        }

        var program = GpuKernel.LoadFromDirectory(m_Device, KernelDirectory, name);

        if (program is null)
        {
            return false;
        }

        kernel = new Kernel(this, name, program, threadsPerGroup);
        m_Kernels.Add(name, kernel);

        return true;
    }

    /// <summary>
    /// Makes every write recorded since the last synchronization visible to the other processor.
    /// </summary>
    /// <exception cref="GpuExecutionException">Thrown when a write could not be published.</exception>
    public void Synchronize()
    {
        ThrowIfDisposed();

        if (!m_Memory.Synchronize())
        {
            throw new GpuExecutionException("A write could not be published to the other processor.");
        }
    }

    /// <summary>
    /// Releases the kernels, the memory they work on, and the device.
    /// </summary>
    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        foreach (var kernel in m_Kernels.Values)
        {
            kernel.Dispose();
        }

        m_Kernels.Clear();
        m_Memory.Dispose();
        m_Device.Dispose();
    }

    /// <summary>
    /// Refuses to hand out memory of a disposed layer.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the layer was disposed.</exception>
    private void ThrowIfDisposed()
    {
        if (m_Disposed)
        {
            throw new ObjectDisposedException(nameof(Gpu));
        }
    }
}

/// <summary>
/// Thrown when the machine has no graphics device the layer can create.
/// </summary>
public sealed class GpuUnavailableException : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public GpuUnavailableException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when a kernel or a write could not be run on the device.
/// </summary>
public sealed class GpuExecutionException : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public GpuExecutionException(string message) : base(message)
    {
    }
}
