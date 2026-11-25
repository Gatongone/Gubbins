namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Writes one program as the GLSL OpenGL compiles.
/// </summary>
/// <remarks>
/// <para>
/// The kernel is written as a compute shader of OpenGL 4.6: the buffers are blocks of standard layout bound at the index
/// every backend binds them at, the thread position is <c>gl_GlobalInvocationID</c>, and the memory a group shares is
/// declared beside the function rather than inside it.
/// </para>
/// <para>
/// The calls over the lanes of a wave come from the subgroup extensions the compute stage of 4.6 has, which are required
/// only by a kernel that makes one.
/// </para>
/// </remarks>
public sealed class KernelGlsl : KernelTextEmitter
{
    /// <summary>
    /// The name of the local that holds the thread index the body reads.
    /// </summary>
    private const string INDEX = "gubbins_index";

    /// <summary>
    /// The prefix of the block name of a buffer.
    /// </summary>
    private const string BUFFER_BLOCK = "gubbins_buffer";

    /// <summary>
    /// The block name the lengths are read through.
    /// </summary>
    private const string LENGTHS_BLOCK = "gubbins_lengths";

    /// <summary>
    /// The name of the array the lengths are read through.
    /// </summary>
    private const string LENGTHS = "gubbins_lengths_data";

    /// <inheritdoc/>
    protected override string Header(KernelProgram program)
    {
        var text = new System.Text.StringBuilder();
        text.AppendLine("#version 460");

        if (UsesWave(program))
        {
            text.AppendLine("#extension GL_KHR_shader_subgroup_arithmetic : require");
            text.AppendLine("#extension GL_KHR_shader_subgroup_ballot : require");
        }

        text.AppendLine($"layout(local_size_x = {program.ThreadsPerGroup}, local_size_y = 1, local_size_z = 1) in;");

        return text.ToString().TrimEnd();
    }

    /// <inheritdoc/>
    protected override string Type(KernelType type) => type switch
    {
        ScalarType scalar => scalar.TypeName,
        VectorType vector => vector.TypeName.Replace("float2", "vec2").Replace("float3", "vec3").Replace("float4", "vec4")
            .Replace("int2", "ivec2").Replace("int3", "ivec3").Replace("int4", "ivec4")
            .Replace("uint2", "uvec2").Replace("uint3", "uvec3").Replace("uint4", "uvec4"),
        StructType structure => Identifier(structure.Name),
        ArrayType array => Type(array.Element),
        _ => "void"
    };

    /// <inheritdoc/>
    /// <remarks>
    /// Names containing <c>__</c>, starting with <c>gl_</c>, or starting with an underscore are reserved by OpenGL, so a
    /// name the program declares is written under a prefix that keeps it out of those.
    /// </remarks>
    protected override string Identifier(string name)
    {
        if (!name.Contains("__") &&
            !name.StartsWith("gl_", StringComparison.Ordinal) &&
            !name.StartsWith("_", StringComparison.Ordinal))
        {
            return name;
        }

        return "gubbins_" + name.TrimStart('_').Replace("__", "_");
    }

    /// <inheritdoc/>
    protected override string Buffer(KernelProgram program, KernelBuffer buffer)
        => $"layout(std430, binding = {buffer.Binding}) {(buffer.ReadOnly ? "readonly " : string.Empty)}buffer {BUFFER_BLOCK}{buffer.Binding} {{ {Type(buffer.ElementType)} {Identifier(buffer.Name)}[]; }};";

    /// <inheritdoc/>
    protected override string Lengths(KernelProgram program)
        => $"layout(std430, binding = {LengthsBinding(program)}) buffer {LENGTHS_BLOCK} {{ uint {LENGTHS}[]; }};";

    /// <inheritdoc/>
    protected override IReadOnlyList<string> EntrySignature(KernelProgram program) => new[]
    {
        "void main()"
    };

    /// <inheritdoc/>
    protected override string EntryPrologue(KernelProgram program) => $"int {INDEX} = int(gl_GlobalInvocationID.x);";

    /// <inheritdoc/>
    protected override string Shared(KernelSharedArray array) => $"shared {Type(array.Element)} {Identifier(array.Name)}[{array.Length}];";

    /// <inheritdoc/>
    protected override string Parameter(KernelFunctionParameter parameter) => parameter.Mode switch
    {
        KernelParameterMode.InOut => $"inout {Type(parameter.Type)} {Identifier(parameter.Name)}",
        KernelParameterMode.Out => $"out {Type(parameter.Type)} {Identifier(parameter.Name)}",
        _ => $"{Type(parameter.Type)} {Identifier(parameter.Name)}"
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
            KernelIntrinsic.WaveSum => "subgroupAdd",
            KernelIntrinsic.WaveMin => "subgroupMin",
            KernelIntrinsic.WaveMax => "subgroupMax",
            KernelIntrinsic.WaveProduct => "subgroupMul",
            KernelIntrinsic.WavePrefixSum => "subgroupExclusiveAdd",
            KernelIntrinsic.WaveAny => "subgroupAny",
            _ => "subgroupAll"
        };

        return name + "(" + string.Join(", ", arguments) + ")";
    }

    /// <inheritdoc/>
    protected override string Barrier() => "barrier();";

    /// <inheritdoc/>
    protected override string AtomicAdd(string target, string value) => $"atomicAdd({target}, {value});";

    /// <inheritdoc/>
    protected override string ThreadIndex() => INDEX;

    /// <inheritdoc/>
    protected override string LengthsElement(int index) => $"{LENGTHS}[{index}]";

    /// <summary>
    /// Reports whether a program makes a call over the lanes of a wave.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool UsesWave(KernelProgram program) => KernelTextEmitter.Wave(program);
}
