namespace Gubbins.Graphic.Kernels;

/// <summary>
/// The kinds of storage a kernel parameter can be.
/// </summary>
public enum KernelParameterKind
{
    /// <summary>
    /// The thread position, which the kernel reads to know what to work on.
    /// </summary>
    Index = 0,

    /// <summary>
    /// A buffer the kernel reads and never writes.
    /// </summary>
    ReadOnlyBuffer = 1,

    /// <summary>
    /// A buffer the kernel writes.
    /// </summary>
    WritableBuffer = 2
}

/// <summary>
/// One parameter of a kernel entry point.
/// </summary>
/// <param name="Name">The name the kernel body uses.</param>
/// <param name="Kind">What the parameter is.</param>
/// <param name="ElementType">The element type of a buffer, or <see langword="null"/> for the index.</param>
/// <param name="Binding">The position of a buffer among the buffers the kernel declares, which every backend binds it at.</param>
public sealed record KernelParameter(string Name, KernelParameterKind Kind, string? ElementType, int Binding);