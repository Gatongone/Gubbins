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
        StructType structure => Identifier(structure.Name),
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
            arguments.Add($"{(buffer.ReadOnly ? "const " : string.Empty)}device {Type(buffer.ElementType)}* {Identifier(buffer.Name)} [[buffer({buffer.Binding})]]");
        }

        if (program.Parameters is not null)
        {
            arguments.Add($"constant {Type(program.Parameters.ElementType)}* {Identifier(program.Parameters.Name)} [[buffer({program.Parameters.Binding})]]");
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
    protected override string Shared(KernelSharedArray array) => $"threadgroup {Type(array.Element)} {Identifier(array.Name)}[{array.Length}];";

    /// <inheritdoc/>
    protected override string Parameter(KernelFunctionParameter parameter) => parameter.Mode switch
    {
        KernelParameterMode.InOut => $"thread {Type(parameter.Type)}& {Identifier(parameter.Name)}",
        KernelParameterMode.Out => $"thread {Type(parameter.Type)}& {Identifier(parameter.Name)}",
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

    /// <summary>
    /// The names this emitter maps for Metal, keyed by the name the kernel declares.
    /// </summary>
    private readonly Dictionary<string, string> m_Escaped = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    /// <remarks>
    /// Names the program declares are renamed when they are words Metal already means, so a C# local called <c>half</c>
    /// does not take the place of the Metal scalar type. Only the names the program declares are touched, so the qualifiers
    /// and the types the emitter writes keep their meaning.
    /// </remarks>
    protected override void Prepare(KernelProgram program)
    {
        m_Escaped.Clear();

        var names = new HashSet<string>(StringComparer.Ordinal);
        Collect(program, names);

        foreach (var name in names)
        {
            if (!RESERVED.Contains(name))
            {
                continue;
            }

            var escaped = name + "_";

            // A suffix is only enough when the name it makes is not one the program already uses itself.
            while (names.Contains(escaped) || RESERVED.Contains(escaped))
            {
                escaped += "_";
            }

            m_Escaped[name] = escaped;
        }
    }

    /// <inheritdoc/>
    protected override string Identifier(string name)
        => m_Escaped.TryGetValue(name, out var escaped) ? escaped : name;

    /// <summary>
    /// Gathers every name the program declares, wherever the language writes it.
    /// </summary>
    /// <param name="program">The kernel.</param>
    /// <param name="names">The set the names are added to.</param>
    private static void Collect(KernelProgram program, HashSet<string> names)
    {
        foreach (var structure in program.Structs)
        {
            names.Add(structure.Name);

            foreach (var field in structure.Fields)
            {
                names.Add(field.Name);
            }
        }

        foreach (var buffer in program.Buffers)
        {
            names.Add(buffer.Name);
        }

        if (program.Parameters is not null)
        {
            names.Add(program.Parameters.Name);
        }

        foreach (var function in program.Functions)
        {
            names.Add(function.Name);

            foreach (var parameter in function.Parameters)
            {
                names.Add(parameter.Name);
            }

            foreach (var shared in function.SharedArrays)
            {
                names.Add(shared.Name);
            }

            Collect(function.Body, names);
        }
    }

    /// <summary>
    /// Gathers the names a block declares, including the ones in a nested statement.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="names">The set the names are added to.</param>
    private static void Collect(KernelBlock block, HashSet<string> names)
    {
        foreach (var statement in block.Statements)
        {
            switch (statement)
            {
                case KernelDeclaration declaration:
                    names.Add(declaration.Name);
                    break;

                case KernelBlock nested:
                    Collect(nested, names);
                    break;

                case KernelIf branch:
                    Collect(branch.Then, names);

                    if (branch.Else is not null)
                    {
                        Collect(branch.Else, names);
                    }

                    break;

                case KernelWhile loop:
                    Collect(loop.Body, names);
                    break;

                case KernelDoWhile loop:
                    Collect(loop.Body, names);
                    break;

                case KernelFor loop:
                    if (loop.Run is KernelDeclaration run)
                    {
                        names.Add(run.Name);
                    }

                    if (loop.Next is KernelDeclaration next)
                    {
                        names.Add(next.Name);
                    }

                    Collect(loop.Body, names);
                    break;
            }
        }
    }

    /// <summary>
    /// The words Metal already means, which a C# name has to be kept out of.
    /// </summary>
    private static readonly HashSet<string> RESERVED = new(StringComparer.Ordinal)
    {
        "alignas", "alignof", "and", "and_eq", "asm", "atomic_bool", "atomic_float", "atomic_int", "atomic_uint",
        "auto", "bitand", "bitor", "bool", "bool2", "bool3", "bool4", "break", "case", "catch", "char", "char2",
        "char3", "char4", "char16_t", "char32_t", "class", "compl", "concept", "const", "const_cast", "consteval",
        "constexpr", "constinit", "continue", "co_await", "co_return", "co_yield", "decltype", "default", "delete",
        "device", "do", "double", "dynamic_cast", "else", "enum", "explicit", "export", "extern", "false", "float",
        "float2", "float3", "float4", "for", "fragment", "friend", "goto", "half", "half2", "half3", "half4", "if",
        "inline", "int", "int2", "int3", "int4", "kernel", "long", "long2", "long3", "long4", "matrix", "mutable",
        "namespace", "new", "noexcept", "not", "not_eq", "nullptr", "object_data", "operator", "or", "or_eq",
        "packed_float", "private", "protected", "public", "ray_data", "register", "reinterpret_cast", "requires",
        "return", "sampler", "short", "short2", "short3", "short4", "signed", "sizeof", "static", "static_assert",
        "static_cast", "struct", "switch", "template", "texture", "this", "thread", "thread_local", "threadgroup",
        "threadgroup_imageblock", "throw", "true", "try", "typedef", "typeid", "typename", "uchar", "uchar2", "uchar3",
        "uchar4", "uint", "uint2", "uint3", "uint4", "ulong", "ulong2", "ulong3", "ulong4", "union", "unsigned",
        "ushort", "ushort2", "ushort3", "ushort4", "using", "vec", "vertex", "virtual", "void", "volatile", "wchar_t",
        "while", "xor", "xor_eq"
    };
}
