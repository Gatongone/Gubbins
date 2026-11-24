namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Writes one program as the Metal shading language source Metal compiles.
/// </summary>
/// <remarks>
/// The kernel is written as a compute function of Metal: the buffers are arguments bound at the index every backend binds
/// them at, the thread position is <c>thread_position_in_grid</c>, and the memory a group shares is declared inside the
/// function because Metal has no module scope for it. A device compiles the source when it creates the pipeline, so the
/// artifact is the source and a <c>metallib</c> is only an artifact of a build that has the platform toolchain.
/// </remarks>
public sealed class KernelMetal : KernelTextEmitter
{
    /// <summary>
    /// The name of the value that carries the thread position into the entry point.
    /// </summary>
    private const string THREAD_ID = "__gubbinsThreadId";

    /// <summary>
    /// The name of the local that holds the thread index the body reads.
    /// </summary>
    private const string INDEX = "__gubbinsIndex";

    /// <inheritdoc/>
    protected override string Header(KernelProgram program) => "#include <metal_stdlib>" + Environment.NewLine + "using namespace metal;";

    /// <inheritdoc/>
    protected override string Type(KernelType type) => type switch
    {
        ScalarType scalar => scalar.TypeName,
        VectorType vector => vector.TypeName,
        StructType structure => structure.Name,
        ArrayType array => Type(array.Element),
        _ => "void"
    };

    /// <inheritdoc/>
    protected override void Bindings(KernelProgram program, System.Text.StringBuilder text)
    {
        // The buffers of a Metal kernel are the arguments of the function, so they are written with its signature.
    }

    /// <inheritdoc/>
    protected override string Buffer(KernelProgram program, KernelBuffer buffer) => string.Empty;

    /// <inheritdoc/>
    protected override string Lengths(KernelProgram program) => string.Empty;

    /// <inheritdoc/>
    protected override IReadOnlyList<string> EntrySignature(KernelProgram program)
    {
        var arguments = new List<string>();

        foreach (var buffer in program.Buffers)
        {
            arguments.Add($"{(buffer.ReadOnly ? "const " : string.Empty)}device {Type(buffer.ElementType)}* {buffer.Name} [[buffer({buffer.Binding})]]");
        }

        if (program.Parameters is not null)
        {
            arguments.Add($"constant {Type(program.Parameters.ElementType)}* {program.Parameters.Name} [[buffer({program.Parameters.Binding})]]");
        }

        arguments.Add($"device uint* {KernelTranspiler.LENGTHS_BUFFER} [[buffer({LengthsBinding(program)})]]");
        arguments.Add($"uint3 {THREAD_ID} [[thread_position_in_grid]]");

        var lines = new List<string> { $"kernel void {KernelTranspiler.ENTRY_POINT}(" };

        for (var index = 0; index < arguments.Count; index++)
        {
            lines.Add(INDENT + arguments[index] + (index == arguments.Count - 1 ? ")" : ","));
        }

        return lines;
    }

    /// <inheritdoc/>
    protected override string EntryPrologue(KernelProgram program) => $"int {INDEX} = int({THREAD_ID}.x);";

    /// <inheritdoc/>
    protected override bool SharedInEntry => true;

    /// <inheritdoc/>
    protected override string Shared(KernelSharedArray array) => $"threadgroup {Type(array.Element)} {array.Name}[{array.Length}];";

    /// <inheritdoc/>
    protected override string Parameter(KernelFunctionParameter parameter) => parameter.Mode switch
    {
        KernelParameterMode.InOut => $"thread {Type(parameter.Type)}& {parameter.Name}",
        KernelParameterMode.Out => $"thread {Type(parameter.Type)}& {parameter.Name}",
        _ => $"{Type(parameter.Type)} {parameter.Name}"
    };

    /// <inheritdoc/>
    protected override string Intrinsic(KernelIntrinsic intrinsic, IReadOnlyList<string> arguments)
    {
        var name = intrinsic switch
        {
            KernelIntrinsic.Abs => "abs",
            KernelIntrinsic.Min => "min",
            KernelIntrinsic.Max => "max",
            KernelIntrinsic.Sqrt => "sqrt",
            KernelIntrinsic.Floor => "floor",
            KernelIntrinsic.Ceiling => "ceil",
            KernelIntrinsic.Pow => "pow",
            KernelIntrinsic.Clamp => "clamp",
            KernelIntrinsic.Sin => "sin",
            KernelIntrinsic.Cos => "cos",
            KernelIntrinsic.WaveSum => "simd_sum",
            KernelIntrinsic.WaveMin => "simd_min",
            KernelIntrinsic.WaveMax => "simd_max",
            KernelIntrinsic.WaveProduct => "simd_product",
            KernelIntrinsic.WavePrefixSum => "simd_prefix_exclusive_sum",
            KernelIntrinsic.WaveAny => "simd_any",
            _ => "simd_all"
        };

        return name + "(" + string.Join(", ", arguments) + ")";
    }

    /// <inheritdoc/>
    protected override string Barrier() => "threadgroup_barrier(mem_flags::mem_threadgroup);";

    /// <inheritdoc/>
    protected override string AtomicAdd(string target, string value) => $"atomic_fetch_add_explicit((device atomic_uint*)&({target}), {value}, memory_order_relaxed);";

    /// <inheritdoc/>
    protected override string ThreadIndex() => INDEX;

    /// <inheritdoc/>
    protected override string LengthsElement(int index) => $"{KernelTranspiler.LENGTHS_BUFFER}[{index}]";
}
