namespace Gubbins.Graphic;

/// <summary>
/// Marks the method of a class as the entry point of a kernel, so that the same method can be compiled for the device
/// instead of only run on the central processor.
/// </summary>
/// <remarks>
/// <para>
/// The method is ordinary C#: it compiles, it can be called from any other method, and it runs as it is written when the
/// central processor calls it. What the attribute adds is that the translator will also accept it as the body of a
/// device kernel, which is why it is restricted to the subset the shading language can express: the first parameter is
/// the thread index, the buffers are declared as <see cref="ReadOnlySpan{T}"/> or <see cref="Span{T}"/>, an optional last
/// parameter is a structure marked with <see cref="GpuStructAttribute"/> that the caller fills in for every dispatch, and
/// everything the body calls is either an intrinsic of <see cref="GpuIntrinsics"/> or a method marked with
/// <see cref="GpuCompatibleAttribute"/>.
/// </para>
/// <para>
/// A method is the entry point of exactly one kernel. The class it is declared in does not matter to the device, which
/// is why it does not have to derive from anything or be partial; the source generator that reads this attribute is
/// what produces the typed entry point beside it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public static partial class Scale
/// {
///     [GpuKernel]
///     public static void Add(int index, ReadOnlySpan&lt;uint&gt; source, Span&lt;uint&gt; target)
///     {
///         target[index] = source[index] + 1;
///     }
/// }
/// </code>
/// <para>
/// A kernel that takes a structure as one value reads it like any other value, and the caller fills it in for every
/// dispatch, so the same artifact runs with different settings without being translated again:
/// </para>
/// <code>
/// [GpuStruct]
/// public struct Step
/// {
///     public float Damping;
///     public float Offset;
/// }
///
/// public static partial class Motion
/// {
///     [GpuKernel]
///     public static void Integrate(int index, ReadOnlySpan&lt;float&gt; source, Span&lt;float&gt; target, in Step step)
///     {
///         target[index] = source[index] * step.Damping + step.Offset;
///     }
/// }
///
/// // Motion.RunIntegrate(gpu, source, target, new Step { Damping = 0.5f, Offset = 1f });
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GpuKernelAttribute : Attribute
{
}

/// <summary>
/// Declares how many threads a group of the kernel has, which is the size the device is dispatched with and the number
/// the body can divide an element count by.
/// </summary>
/// <remarks>
/// <para>
/// A kernel that says nothing is dispatched with <see cref="GpuIntrinsics.DefaultThreadsPerGroup"/> threads per group.
/// The value has to be one the device can run: at most 1024, which is the largest group any of the backends promises.
/// </para>
/// <para>
/// The number is part of the kernel, not of the dispatch: a caller that divides an element count by anything else will
/// leave the last group short or run past the end, and the two numbers have to agree with the one the body was written
/// against.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [GpuKernel]
/// [GpuThreads(128)]
/// public static void Add(int index, ReadOnlySpan&lt;uint&gt; source, Span&lt;uint&gt; target)
/// {
///     target[index] = source[index] + 1;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GpuThreadsAttribute : Attribute
{
    /// <summary>
    /// The largest group a backend is required to run.
    /// </summary>
    private const int MaximumThreads = 1024;

    /// <param name="threads">The number of threads in a group.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is not a size a device can dispatch.</exception>
    public GpuThreadsAttribute(int threads)
    {
        if (threads < 1 || threads > MaximumThreads)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threads),
                threads,
                $"A group has between 1 and {MaximumThreads} threads.");
        }

        Threads = threads;
    }

    /// <summary>
    /// Gets the number of threads in a group.
    /// </summary>
    public int Threads { get; }
}

/// <summary>
/// Marks a structure as one the device can hold, so that the same declaration the central processor uses is the one the
/// kernel reads and writes.
/// </summary>
/// <remarks>
/// <para>
/// A marked structure is laid out like the memory the device addresses: its fields are the values it declares, in the
/// order it declares them, and every one of them is a type the shading language has. The translator emits the matching
/// structure for the device and checks the layout, so a field that cannot cross over is refused where it is declared
/// rather than producing a kernel that reads the wrong bytes.
/// </para>
/// <para>
/// The point of the attribute is that there is one declaration: a component the simulation writes and the kernel reads
/// is not copied into a second, device shaped structure, and a field added to it is a field the kernel sees.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class GpuStructAttribute : Attribute
{
}

/// <summary>
/// Marks a method as one a kernel may call, so that the same helper serves both processors instead of being written twice.
/// </summary>
/// <remarks>
/// <para>
/// A marked method is an ordinary method: the central processor calls it as it is written, and the translator follows the
/// call from the kernel and emits the body beside the kernel for the device. The method has to be expressible in the
/// subset the kernel is, which is why the mark is explicit: everything the kernel reaches has to be checked before it is
/// translated, and an unmarked method is one the translator will not follow.
/// </para>
/// <para>
/// The method may be declared in any class of that compilation: the translator follows the call by what the compiler
/// resolved, so a helper of another class, or one that calls a helper of a third, is part of the kernel. Two classes may
/// even declare a method of the same name, in which case the name the program emits is qualified by the class that
/// declares it, and a call written without a class still reaches the one in the kernel's own class.
/// </para>
/// <para>
/// The method has to be declared in the same compilation as the kernel, or in a source file both projects compile. The
/// translator reads the body of a method, not its signature, so a helper from a referenced assembly cannot be followed
/// into even when the call itself is valid C#.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GpuCompatibleAttribute : Attribute
{
}
