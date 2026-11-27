namespace Gubbins.Graphic.Kernels;

using System.Text;

using System.Text;

/// <summary>
/// Writes one program as the HLSL D3D12 compiles, which is the language the subset is closest to.
/// </summary>
/// <remarks>
/// The kernel is written as the compute shader D3D12 loads: a group size, a dispatch thread id, one unordered access view
/// per buffer and the block that carries their counts after them. It is compiled where it is loaded, by the compiler
/// Windows ships, so the artifact is the source and no tool has to be installed beside it.
/// </remarks>
public sealed class KernelHlsl : KernelTextEmitter
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
    protected override string Type(KernelType type) => type switch
    {
        ScalarType scalar => scalar.TypeName,
        VectorType vector => vector.TypeName,
        StructType structure => structure.Name,
        ArrayType array => Type(array.Element),
        _ => "void"
    };

    /// <inheritdoc/>
    protected override string Buffer(KernelProgram program, KernelBuffer buffer)
        => $"RWStructuredBuffer<{Type(buffer.ElementType)}> {buffer.Name} : register(u{buffer.Binding});";

    /// <inheritdoc/>
    protected override string Lengths(KernelProgram program)
        => $"RWStructuredBuffer<uint> {KernelTranspiler.LENGTHS_BUFFER} : register(u{LengthsBinding(program)});";

    /// <inheritdoc/>
    protected override void Bindings(KernelProgram program, StringBuilder text)
    {
        base.Bindings(program, text);

        foreach (var texture in program.Textures ?? System.Array.Empty<KernelTexture>())
        {
            text.AppendLine($"Texture2D<float4> gubbins_tex_{Identifier(texture.Name)} : register(t{texture.Binding});");
        }

        foreach (var sampler in program.Samplers ?? System.Array.Empty<KernelSampler>())
        {
            text.AppendLine($"SamplerState gubbins_samp_{Identifier(sampler.Name)} : register(s{sampler.Binding});");
        }
    }

    /// <inheritdoc/>
    protected override bool SupportsTextures => true;

    /// <inheritdoc/>
    protected override string TextureSample(KernelTextureSample sample)
        => $"gubbins_tex_{Identifier(sample.Texture.Name)}.Sample(gubbins_samp_{Identifier(sample.Sampler.Name)}, {Value(sample.Coordinates)})";

    /// <inheritdoc/>
    protected override IReadOnlyList<string> EntrySignature(KernelProgram program) => new[]
    {
        $"[numthreads({program.ThreadsPerGroup}, 1, 1)]",
        $"void {KernelTranspiler.ENTRY_POINT}(uint3 {THREAD_ID} : SV_DispatchThreadID)"
    };

    /// <inheritdoc/>
    protected override string EntryPrologue(KernelProgram program) => $"int {INDEX} = int({THREAD_ID}.x);";

    /// <inheritdoc/>
    protected override string Shared(KernelSharedArray array) => $"groupshared {Type(array.Element)} {array.Name}[{array.Length}];";

    /// <inheritdoc/>
    protected override string Parameter(KernelFunctionParameter parameter) => parameter.Mode switch
    {
        KernelParameterMode.InOut => $"inout {Type(parameter.Type)} {parameter.Name}",
        KernelParameterMode.Out => $"out {Type(parameter.Type)} {parameter.Name}",
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
            KernelIntrinsic.WaveSum => "WaveActiveSum",
            KernelIntrinsic.WaveMin => "WaveActiveMin",
            KernelIntrinsic.WaveMax => "WaveActiveMax",
            KernelIntrinsic.WaveProduct => "WaveActiveProduct",
            KernelIntrinsic.WavePrefixSum => "WavePrefixSum",
            KernelIntrinsic.WaveAny => "WaveActiveAnyTrue",
            _ => "WaveActiveAllTrue"
        };

        return name + "(" + string.Join(", ", arguments) + ")";
    }

    /// <inheritdoc/>
    protected override string Barrier() => "GroupMemoryBarrierWithGroupSync();";

    /// <inheritdoc/>
    protected override string AtomicAdd(string target, string value) => $"InterlockedAdd({target}, {value});";

    /// <inheritdoc/>
    protected override string ThreadIndex() => INDEX;

    /// <inheritdoc/>
    protected override string LengthsElement(int index) => $"{KernelTranspiler.LENGTHS_BUFFER}[{index}]";
}
