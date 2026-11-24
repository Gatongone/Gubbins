using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Lowers one kernel to the model every backend reads.
/// </summary>
/// <remarks>
/// <para>
/// The body of a kernel is ordinary C#, and the compiler has already decided what its names mean; this reads the syntax
/// of the kernel, of the helpers it calls and of the structures it works with, and resolves every expression into a
/// typed value of <see cref="KernelExpression"/>. What a shading language spells differently is resolved here rather than
/// by an emitter: a span becomes the buffer and the offset it reads, a range and a <c>foreach</c> become the loop they
/// mean, a <c>switch</c> becomes the comparisons it is, and a method of a structure becomes a function of the program.
/// </para>
/// <para>
/// A kernel the compiler resolved arrives with <see cref="KernelSemantics"/>, which answers what a name stands for beyond
/// the syntax: a constant is folded to its value, and the structures and helpers that are emitted are the ones the walk
/// reached. A source text without a compilation has nothing but its declarations to go on, so everything it declares is
/// emitted and a constant keeps the name it was folded from.
/// </para>
/// </remarks>
internal sealed partial class KernelLowering
{
    /// <summary>
    /// The prefix of the local that carries the element count of a buffer.
    /// </summary>
    internal const string LENGTH_PREFIX = "__length_";

    /// <summary>
    /// The name a method that writes the value it is called on takes for the device.
    /// </summary>
    private const string SELF = "self";

    private readonly MethodDeclarationSyntax m_Kernel;
    private readonly string m_KernelOwner;
    private readonly SyntaxTree m_Tree;
    private readonly int m_Threads;
    private readonly KernelTranspiler.KernelSemantics? m_Semantics;

    /// <summary>
    /// The structures the source declares, by name.
    /// </summary>
    private readonly Dictionary<string, StructDeclarationSyntax> m_Declared = new(StringComparer.Ordinal);

    /// <summary>
    /// The helpers of the program, which are the methods the kernel and the helpers it reaches call.
    /// </summary>
    private readonly List<HelperInfo> m_Helpers = new();

    /// <summary>
    /// The helpers by the name the source declares them with, which is how a call that names no type is resolved.
    /// </summary>
    private readonly Dictionary<string, List<HelperInfo>> m_HelpersByName = new(StringComparer.Ordinal);

    /// <summary>
    /// The helpers by the type that declares them and their name, which is how a call that names a type is resolved.
    /// </summary>
    private readonly Dictionary<string, List<HelperInfo>> m_HelpersByOwner = new(StringComparer.Ordinal);

    /// <summary>
    /// The helpers by the symbol the compiler resolved, which is how a call of a kernel the compiler read is resolved.
    /// </summary>
    private readonly Dictionary<IMethodSymbol, HelperInfo> m_HelpersBySymbol = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// The structures already declared for the device, by the name that carries their arguments.
    /// </summary>
    private readonly Dictionary<string, KernelStruct> m_Structures = new(StringComparer.Ordinal);

    /// <summary>
    /// The functions of the program, by the name they are emitted under.
    /// </summary>
    private readonly Dictionary<string, KernelFunction> m_Functions = new(StringComparer.Ordinal);

    /// <summary>
    /// The functions of the program, in the order they are emitted: helpers first, the kernel last.
    /// </summary>
    private readonly List<KernelFunction> m_Ordered = new();

    /// <summary>
    /// The locals of the function being lowered, by name.
    /// </summary>
    private readonly Dictionary<string, KernelType> m_Locals = new(StringComparer.Ordinal);

    /// <summary>
    /// The spans the function being lowered reads a buffer through, by the name the body uses.
    /// </summary>
    private readonly Dictionary<string, SpanView> m_Views = new(StringComparer.Ordinal);

    /// <summary>
    /// The local arrays of the function being lowered, by name.
    /// </summary>
    private readonly Dictionary<string, ArrayInfo> m_Arrays = new(StringComparer.Ordinal);

    /// <summary>
    /// The arrays the function being lowered shares with its group.
    /// </summary>
    private readonly List<KernelSharedArray> m_Shared = new();

    /// <summary>
    /// The buffers of the kernel, in the order it declares them.
    /// </summary>
    private readonly List<KernelBuffer> m_Buffers = new();

    /// <summary>
    /// The parameter of the kernel that carries the thread index, when it is being lowered.
    /// </summary>
    private string? m_IndexParameter;

    /// <summary>
    /// The parameter of the kernel that is a structure taken as one value, when it declares one.
    /// </summary>
    private string? m_ParameterName;

    /// <summary>
    /// The block that carries that structure, or <see langword="null"/> when the kernel takes none.
    /// </summary>
    private KernelBuffer? m_Parameters;

    /// <summary>
    /// The structures a method of a structure is being lowered for, which is what an unqualified field name reads.
    /// </summary>
    private StructType? m_Self;

    /// <summary>
    /// The arrays the body that was lowered last shares with its group.
    /// </summary>
    private IReadOnlyList<KernelSharedArray> BodyShared { get; set; } = System.Array.Empty<KernelSharedArray>();

    private int m_Loops;

    private KernelLowering(MethodDeclarationSyntax kernel, SyntaxTree tree, int threads, KernelTranspiler.KernelSemantics? semantics)
    {
        m_Kernel = kernel;
        m_KernelOwner = OwnerOf(kernel);
        m_Tree = tree;
        m_Threads = threads;
        m_Semantics = semantics;
    }

    /// <summary>
    /// Lowers the kernel <paramref name="source"/> declares.
    /// </summary>
    /// <param name="kernel">The kernel.</param>
    /// <param name="tree">The tree it was parsed from.</param>
    /// <param name="threads">The number of threads in a group of it.</param>
    /// <param name="semantics">What the compiler resolved, or <see langword="null"/> when the source is a text.</param>
    /// <returns>The kernel, as every backend reads it.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the supported subset.</exception>
    internal static KernelProgram Build(MethodDeclarationSyntax kernel, SyntaxTree tree, int threads, KernelTranspiler.KernelSemantics? semantics)
    {
        var lowering = new KernelLowering(kernel, tree, threads, semantics);
        return lowering.Build();
    }

    /// <summary>
    /// Reads the declarations the kernel works with and lowers the functions of the program.
    /// </summary>
    /// <returns>The program.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the supported subset.</exception>
    private KernelProgram Build()
    {
        Declare();

        var body = Entry();
        var entry = Function(KernelTranspiler.ENTRY_POINT, VoidType.Void, Array.Empty<KernelFunctionParameter>(), body, m_Shared, true);

        return new KernelProgram(m_Kernel.Identifier.ValueText, m_Threads, m_Buffers, m_Structures.Values.ToList(), m_Ordered, entry, m_Parameters);
    }

    /// <summary>
    /// Reads the structures and the helpers the kernel may call, then declares the structures the device always holds.
    /// </summary>
    /// <exception cref="KernelTranslationException">Thrown when a declaration has no representation on the device.</exception>
    private void Declare()
    {
        var structures = m_Semantics is null
            ? m_Tree.GetCompilationUnitRoot().DescendantNodes().OfType<StructDeclarationSyntax>().ToList()
            : m_Semantics.Structs.Select(structure => structure.Syntax).ToList();

        foreach (var structure in structures)
        {
            m_Declared[structure.Identifier.ValueText] = structure;
        }

        var helpers = m_Semantics is null
            ? m_Tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(method => !KernelTranspiler.IsKernel(method) && method.Parent is not StructDeclarationSyntax).ToList()
            : m_Semantics.Helpers.Select(helper => helper.Syntax).ToList();

        DeclareHelpers(helpers);

        // A structure the source declares without type parameters is one the device holds whether the kernel reaches it
        // or not, which is what keeps the order of the declarations the order of the source. A generic one is declared
        // when the kernel says what it holds, because there is nothing to declare until then.
        foreach (var structure in InDependencyOrder(m_Declared.Values.ToList()))
        {
            if (structure.TypeParameterList is null || structure.TypeParameterList.Parameters.Count == 0)
            {
                Materialize(new StructType(structure.Identifier.ValueText, Array.Empty<KernelType>()));
            }
        }
    }

    /// <summary>
    /// Lowers the body of the kernel, with the locals that carry the element counts of its buffers in front of it.
    /// </summary>
    /// <returns>The body of the entry point.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the supported subset.</exception>
    private KernelBlock Entry()
    {
        if (m_Semantics is null)
        {
            KernelTranspiler.RejectUnsupported(m_Kernel, m_Tree);
        }

        m_Locals.Clear();
        m_Views.Clear();
        m_Arrays.Clear();
        m_Shared.Clear();
        m_Loops = 0;

        ReadParameters();

        var statements = new List<KernelStatement>();

        for (var index = 0; index < m_Buffers.Count; index++)
        {
            statements.Add(new KernelDeclaration(
                LENGTH_PREFIX + m_Buffers[index].Name,
                ScalarType.Int,
                new KernelConvert(ScalarType.Int, new KernelLengthsElement(index))));
        }

        if (m_Parameters is not null && m_ParameterName is not null)
        {
            // The structure the kernel takes is one element of the block the caller fills in, copied into a local so that
            // the body reads it like any other value.
            statements.Add(new KernelDeclaration(
                m_ParameterName,
                m_Parameters.ElementType,
                new KernelBufferElement(m_Parameters.Name, new KernelLiteral(0, ScalarType.Int), m_Parameters.ElementType)));
        }

        Lower(m_Kernel.Body, statements);

        return new KernelBlock(statements);
    }

    /// <summary>
    /// Reads the parameters of the kernel as the buffers every backend binds.
    /// </summary>
    /// <exception cref="KernelTranslationException">Thrown when a parameter is not one the device has.</exception>
    private void ReadParameters()
    {
        m_Buffers.Clear();
        m_IndexParameter = null;

        m_ParameterName = null;
        m_Parameters = null;

        foreach (var parameter in m_Kernel.ParameterList.Parameters)
        {
            var name = parameter.Identifier.ValueText;
            var type = KernelTranspiler.TypeName(parameter.Type);

            if (type == KernelTranspiler.INDEX_TYPE || type == KernelTranspiler.SPAN_INDEX_TYPE)
            {
                if (parameter.Modifiers.Count > 0)
                {
                    throw new KernelTranslationException($"Parameter '{name}' is passed by reference: a kernel reads and writes the buffers it is given.");
                }

                if (m_IndexParameter is not null)
                {
                    throw new KernelTranslationException("The kernel declares more than one thread index parameter.");
                }

                m_IndexParameter = name;
                m_Locals[name] = ScalarType.Int;
                continue;
            }

            if (parameter.Type is not GenericNameSyntax generic)
            {
                // The one parameter that is not a buffer is the structure the kernel takes as one value, which the caller
                // fills in and the device reads as a block of one element.
                ReadParameter(parameter);
                continue;
            }

            if (parameter.Modifiers.Count > 0)
            {
                throw new KernelTranslationException($"Parameter '{name}' is passed by reference: a kernel reads and writes the buffers it is given.");
            }

            var element = generic.TypeArgumentList.Arguments.FirstOrDefault()
                          ?? throw new KernelTranslationException($"Parameter '{name}': only an int index, ReadOnlySpan<T>, Span<T>, GpuIndex, GpuReadOnlyBuffer<T> or GpuBuffer<T> are supported.");

            if (!IsElementName(KernelTranspiler.TypeName(element)) && !m_Declared.ContainsKey(KernelTranspiler.TypeName(element)))
            {
                throw new KernelTranslationException($"Parameter '{name}': '{element}' is not an element type every backend has. Use int, uint or float, optionally with 2, 3 or 4 components, or a structure the kernel declares.");
            }

            var buffer = new KernelBuffer(name, TypeOf(element), m_Buffers.Count, type == KernelTranspiler.READ_ONLY_BUFFER_TYPE || type == KernelTranspiler.READ_ONLY_SPAN_TYPE);
            m_Buffers.Add(buffer);
        }

        if (!m_Buffers.Any(buffer => !buffer.ReadOnly))
        {
            throw new KernelTranslationException("The kernel writes nothing: a writable buffer parameter is required.");
        }

        if (m_IndexParameter is null)
        {
            throw new KernelTranslationException("The kernel has no thread index parameter.");
        }
    }

    /// <summary>
    /// Reads the parameter a kernel takes as one value, which is a structure both processors declare.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <exception cref="KernelTranslationException">Thrown when it is not a structure the device has, or when it is not the last parameter.</exception>
    private void ReadParameter(ParameterSyntax parameter)
    {
        var name = parameter.Identifier.ValueText;

        if (m_ParameterName is not null)
        {
            throw new KernelTranslationException($"Parameter '{name}': a kernel takes one structure as a value, and it is the last parameter.");
        }

        foreach (var modifier in parameter.Modifiers)
        {
            if (!modifier.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InKeyword))
            {
                throw new KernelTranslationException($"Parameter '{name}' is passed by reference: the structure a kernel takes is read, and a result is written to a buffer.");
            }
        }

        var type = TypeOf(parameter.Type!);

        if (type is not StructType structure)
        {
            throw new KernelTranslationException($"Parameter '{name}': only an int index, ReadOnlySpan<T>, Span<T>, GpuIndex, GpuReadOnlyBuffer<T>, GpuBuffer<T> or a structure declared [GpuStruct] are supported.");
        }

        if (parameter != m_Kernel.ParameterList.Parameters[m_Kernel.ParameterList.Parameters.Count - 1])
        {
            throw new KernelTranslationException($"Parameter '{name}': the structure a kernel takes as a value is the last parameter, after the buffers.");
        }

        m_ParameterName = name;
        m_Parameters = new KernelBuffer(KernelTranspiler.PARAMETERS_BUFFER, structure, m_Buffers.Count, true);
        m_Locals[name] = structure;
    }

    /// <summary>
    /// Registers one function of the program.
    /// </summary>
    /// <param name="name">The name it is emitted under.</param>
    /// <param name="returnType">What it returns.</param>
    /// <param name="parameters">What it takes.</param>
    /// <param name="body">What it runs.</param>
    /// <param name="shared">The arrays its group shares.</param>
    /// <param name="entryPoint">Whether it is the kernel a dispatch runs.</param>
    /// <returns>The function.</returns>
    private KernelFunction Function(string name, KernelType returnType, IReadOnlyList<KernelFunctionParameter> parameters, KernelBlock body, IReadOnlyList<KernelSharedArray> shared, bool entryPoint)
    {
        var function = new KernelFunction(name, returnType, parameters, body, shared, entryPoint);
        m_Ordered.Add(function);
        m_Functions[name] = function;

        return function;
    }

    /// <summary>
    /// Reads the helpers a kernel may call, which may be declared in any class of the compilation, and gives each a name
    /// the program can tell apart.
    /// </summary>
    /// <param name="helpers">The helpers, in the order the walk reached them.</param>
    /// <exception cref="KernelTranslationException">Thrown when a helper is generic or two helpers have no name the program can tell apart.</exception>
    private void DeclareHelpers(List<MethodDeclarationSyntax> helpers)
    {
        foreach (var helper in helpers)
        {
            if (helper.TypeParameterList is not null)
            {
                throw new KernelTranslationException($"'{helper.Identifier.ValueText}' is generic: a helper of a kernel is a plain function.");
            }

            // The compiler refuses an instance helper of a kernel that was resolved by symbol, and a source text has to be
            // refused the same way: there is no value to call it on the device.
            if (!helper.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword))
            {
                throw new KernelTranslationException($"'{helper.Identifier.ValueText}' is not static: a helper of a kernel is a static method, so that the device can call it without a value.");
            }
        }

        var declared = helpers.Select(helper => new HelperInfo(helper, helper.Identifier.ValueText, OwnerOf(helper))).ToList();
        var shared = declared.GroupBy(info => info.Simple, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        // Two classes may declare a method of the same name, and the device has one namespace: a name two helpers share is
        // qualified by the class that declares it, and a name only one has stays as it is written.
        foreach (var info in declared)
        {
            info.Emitted = shared[info.Simple] > 1 ? info.Owner + "_" + info.Simple : info.Simple;
        }

        foreach (var group in declared.GroupBy(info => info.Emitted, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                throw new KernelTranslationException($"'{group.Key}' is declared {group.Count()} times by the classes the kernel reaches: a helper of a kernel is a function with one body.");
            }
        }

        m_Helpers.AddRange(declared);

        foreach (var info in declared)
        {
            Add(m_HelpersByName, info.Simple, info);
            Add(m_HelpersByOwner, info.Owner + "." + info.Simple, info);
        }

        if (m_Semantics is null)
        {
            return;
        }

        foreach (var helper in m_Semantics.Helpers)
        {
            foreach (var info in declared)
            {
                if (ReferenceEquals(info.Syntax, helper.Syntax))
                {
                    m_HelpersBySymbol[helper.Symbol] = info;
                }
            }
        }
    }

    /// <summary>
    /// Adds a helper to one of the lists a call is resolved against.
    /// </summary>
    /// <param name="index">The index to add it to.</param>
    /// <param name="key">The key it is added under.</param>
    /// <param name="info">The helper.</param>
    private static void Add(Dictionary<string, List<HelperInfo>> index, string key, HelperInfo info)
    {
        if (!index.TryGetValue(key, out var helpers))
        {
            helpers = new List<HelperInfo>();
            index[key] = helpers;
        }

        helpers.Add(info);
    }

    /// <summary>
    /// Reads the type a declaration is nested in, which is the class a helper is declared in.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <returns>The name of the type.</returns>
    private static string OwnerOf(SyntaxNode declaration)
    {
        for (var node = declaration.Parent; node is not null; node = node.Parent)
        {
            if (node is BaseTypeDeclarationSyntax type)
            {
                return type.Identifier.ValueText;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Lowers the body of a helper of the kernel.
    /// </summary>
    /// <param name="helper">The helper.</param>
    /// <returns>The function the call reaches.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the helper is outside the supported subset.</exception>
    private KernelFunction Helper(HelperInfo helper)
    {
        var name = helper.Emitted;

        if (m_Functions.TryGetValue(name, out var known))
        {
            return known;
        }

        if (m_Semantics is null)
        {
            KernelTranspiler.RejectUnsupported(helper.Syntax, m_Tree);
        }

        var parameters = new List<KernelFunctionParameter>();

        foreach (var parameter in helper.Syntax.ParameterList.Parameters)
        {
            parameters.Add(Parameter(parameter));
        }

        var returnType = TypeOf(helper.Syntax.ReturnType);
        var body = Body(helper.Syntax.Body, helper.Syntax.ExpressionBody?.Expression, parameters, null);

        return Function(name, returnType, parameters, body, BodyShared, false);
    }

    /// <summary>
    /// Lowers the body of a method of a structure.
    /// </summary>
    /// <param name="structure">The structure the method belongs to, instantiated.</param>
    /// <param name="member">The method.</param>
    /// <param name="declaration">The declaration of the structure.</param>
    /// <param name="receiver">Whether the call passes the value it was called on.</param>
    /// <param name="writeBack">Whether the method writes the value it was called on.</param>
    /// <returns>The function the call reaches.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the method is outside the supported subset.</exception>
    private KernelFunction Member(StructType structure, MemberDeclarationSyntax member, StructDeclarationSyntax declaration, bool receiver, bool writeBack)
    {
        var memberName = member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            ConstructorDeclarationSyntax => "ctor",
            OperatorDeclarationSyntax operation => OperatorName(operation.OperatorToken.Kind()),
            _ => throw new KernelTranslationException($"A member of '{structure.Name}' has no representation on the device.")
        };

        var name = FunctionName(structure, memberName);

        if (m_Functions.TryGetValue(name, out var known))
        {
            return known;
        }

        var substitutions = Substitutions(declaration, structure);
        var parameters = new List<KernelFunctionParameter>();

        if (receiver)
        {
            parameters.Add(new KernelFunctionParameter(SELF, structure, writeBack ? KernelParameterMode.InOut : KernelParameterMode.In));
        }

        KernelType returnType = VoidType.Void;
        var body = KernelBlock.Empty;

        m_Self = receiver ? structure : null;

        try
        {
            switch (member)
            {
                case MethodDeclarationSyntax method:
                {
                    if (method.TypeParameterList is not null)
                    {
                        throw new KernelTranslationException($"'{method.Identifier.ValueText}' of '{structure.Name}' is generic: a method of a structure is a plain function.");
                    }

                    returnType = TypeOf(method.ReturnType, substitutions);

                    foreach (var parameter in method.ParameterList.Parameters)
                    {
                        parameters.Add(Parameter(parameter, substitutions));
                    }

                    body = Body(method.Body, method.ExpressionBody?.Expression, parameters, structure);
                    break;
                }

                case PropertyDeclarationSyntax property:
                {
                    returnType = TypeOf(property.Type, substitutions);

                    if (property.ExpressionBody is not null)
                    {
                        body = Body(null, property.ExpressionBody.Expression, parameters, structure);
                        break;
                    }

                    var accessors = property.AccessorList?.Accessors;

                    if (accessors is null || accessors.Value.Count != 1 || !accessors.Value[0].IsKind(SyntaxKind.GetAccessorDeclaration))
                    {
                        throw new KernelTranslationException($"A property of '{structure.Name}' is read only: the device reads a value, it does not run a setter.");
                    }

                    var getter = accessors.Value[0];
                    body = Body(getter.Body, getter.ExpressionBody?.Expression, parameters, structure);
                    break;
                }

                case ConstructorDeclarationSyntax constructor:
                {
                    returnType = structure;

                    foreach (var parameter in constructor.ParameterList.Parameters)
                    {
                        parameters.Add(Parameter(parameter, substitutions));
                    }

                    body = Constructor(constructor, parameters, structure);
                    break;
                }

                case OperatorDeclarationSyntax operation:
                {
                    returnType = TypeOf(operation.ReturnType, substitutions);

                    foreach (var parameter in operation.ParameterList.Parameters)
                    {
                        parameters.Add(Parameter(parameter, substitutions));
                    }

                    body = Body(operation.Body, operation.ExpressionBody?.Expression, parameters, null);
                    break;
                }

                default:
                    throw new KernelTranslationException($"A member of '{structure.Name}' has no representation on the device: a struct holds fields, a constructor, read only properties and methods.");
            }
        }
        finally
        {
            m_Self = null;
        }

        return Function(name, returnType, parameters, body, BodyShared, false);
    }

    /// <summary>
    /// Lowers the body of a constructor, which builds the value it is called for and returns it.
    /// </summary>
    /// <param name="constructor">The constructor.</param>
    /// <param name="parameters">The parameters of the function.</param>
    /// <param name="structure">The structure being built.</param>
    /// <returns>The body.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the constructor is outside the supported subset.</exception>
    private KernelBlock Constructor(ConstructorDeclarationSyntax constructor, IReadOnlyList<KernelFunctionParameter> parameters, StructType structure)
    {
        var locals = new Dictionary<string, KernelType>(m_Locals, StringComparer.Ordinal);
        var statements = new List<KernelStatement> { new KernelDeclaration(SELF, structure, null) };

        m_Self = structure;
        m_Locals.Clear();

        foreach (var parameter in parameters)
        {
            m_Locals[parameter.Name] = parameter.Type;
        }

        try
        {
            Lower(constructor.Body, statements);
        }
        finally
        {
            m_Self = null;
            m_Locals.Clear();

            foreach (var local in locals)
            {
                m_Locals[local.Key] = local.Value;
            }
        }

        statements.Add(new KernelReturn(new KernelLocal(SELF, structure)));

        return new KernelBlock(statements);
    }

    /// <summary>
    /// Lowers the body of a member, which is a block of statements or the expression it is written as.
    /// </summary>
    /// <param name="block">The block, when the member is written as one.</param>
    /// <param name="expression">The expression, when the member is written as one.</param>
    /// <param name="parameters">The parameters of the function.</param>
    /// <param name="self">The structure the member belongs to, or <see langword="null"/> when it is static.</param>
    /// <returns>The body.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the member is outside the supported subset.</exception>
    private KernelBlock Body(BlockSyntax? block, ExpressionSyntax? expression, IReadOnlyList<KernelFunctionParameter> parameters, StructType? self)
    {
        var locals = new Dictionary<string, KernelType>(m_Locals, StringComparer.Ordinal);
        var views = new Dictionary<string, SpanView>(m_Views, StringComparer.Ordinal);
        var arrays = new Dictionary<string, ArrayInfo>(m_Arrays, StringComparer.Ordinal);
        var shared = new List<KernelSharedArray>(m_Shared);
        var previous = m_Self;
        var loops = m_Loops;
        var statements = new List<KernelStatement>();

        m_Locals.Clear();
        m_Views.Clear();
        m_Arrays.Clear();
        m_Shared.Clear();
        m_Loops = 0;
        m_Self = self;

        foreach (var parameter in parameters)
        {
            m_Locals[parameter.Name] = parameter.Type;
        }

        try
        {
            if (expression is not null)
            {
                statements.Add(new KernelReturn(Expression(expression)));
            }
            else if (block is not null)
            {
                Lower(block, statements);
            }
        }
        finally
        {
            BodyShared = m_Shared.ToList();

            m_Self = previous;
            m_Locals.Clear();
            m_Views.Clear();
            m_Arrays.Clear();
            m_Shared.Clear();
            m_Shared.AddRange(shared);
            m_Loops = loops;

            foreach (var local in locals)
            {
                m_Locals[local.Key] = local.Value;
            }

            foreach (var view in views)
            {
                m_Views[view.Key] = view.Value;
            }

            foreach (var array in arrays)
            {
                m_Arrays[array.Key] = array.Value;
            }
        }

        return new KernelBlock(statements);
    }

    /// <summary>
    /// Reads one parameter of a function.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <param name="substitutions">What the type parameters of a generic structure are, or <see langword="null"/>.</param>
    /// <returns>The parameter of the device.</returns>
    /// <exception cref="KernelTranslationException">Thrown when it is passed in a way the device does not have.</exception>
    private KernelFunctionParameter Parameter(ParameterSyntax parameter, IReadOnlyDictionary<string, KernelType>? substitutions = null)
    {
        var mode = KernelParameterMode.In;

        foreach (var modifier in parameter.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.RefKeyword))
            {
                mode = KernelParameterMode.InOut;
            }
            else if (modifier.IsKind(SyntaxKind.OutKeyword))
            {
                mode = KernelParameterMode.Out;
            }
            else if (!modifier.IsKind(SyntaxKind.InKeyword))
            {
                throw new KernelTranslationException($"Parameter '{parameter.Identifier.ValueText}' has a modifier the device has no meaning for.");
            }
        }

        return new KernelFunctionParameter(parameter.Identifier.ValueText, TypeOf(parameter.Type, substitutions), mode);
    }

    /// <summary>
    /// Lowers a block of statements.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when a statement is outside the supported subset.</exception>
    private void Lower(BlockSyntax? block, List<KernelStatement> statements)
    {
        if (block is null)
        {
            return;
        }

        foreach (var statement in block.Statements)
        {
            Lower(statement, statements);
        }
    }

    /// <summary>
    /// Lowers one statement.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when the statement is outside the supported subset.</exception>
    private void Lower(StatementSyntax statement, List<KernelStatement> statements)
    {
        switch (statement)
        {
            case BlockSyntax block:
            {
                var inner = new List<KernelStatement>();
                Lower(block, inner);
                statements.Add(new KernelBlock(inner));
                return;
            }

            case LocalDeclarationStatementSyntax declaration:
                Declare(declaration, statements);
                return;

            case ExpressionStatementSyntax expression:
                Lower(expression.Expression, statements);
                return;

            case IfStatementSyntax branch:
                statements.Add(new KernelIf(Expression(branch.Condition), BlockOf(branch.Statement, false), branch.Else is null ? null : BlockOf(branch.Else.Statement, false)));
                return;

            case WhileStatementSyntax loop:
                statements.Add(new KernelWhile(Expression(loop.Condition), BlockOf(loop.Statement, true)));
                return;

            case DoStatementSyntax loop:
                statements.Add(new KernelDoWhile(BlockOf(loop.Statement, true), Expression(loop.Condition)));
                return;

            case ForStatementSyntax loop:
                statements.Add(For(loop));
                return;

            case ForEachStatementSyntax loop:
                statements.Add(ForEach(loop));
                return;

            case SwitchStatementSyntax branch:
                statements.Add(Branch(branch));
                return;

            case ReturnStatementSyntax returned:
                statements.Add(new KernelReturn(returned.Expression is null ? null : Expression(returned.Expression)));
                return;

            case BreakStatementSyntax:
                statements.Add(KernelBreak.Value);
                return;

            case ContinueStatementSyntax:
                statements.Add(KernelContinue.Value);
                return;

            case EmptyStatementSyntax:
                return;

            default:
                throw new KernelTranslationException($"'{statement.Kind()}' has no representation on the device.");
        }
    }

    /// <summary>
    /// Lowers a statement that a branch or a loop runs, as a block.
    /// </summary>
    /// <param name="statement">The statement.</param>
    /// <param name="save">Whether the locals it declares are forgotten after it, which is what a loop body does.</param>
    /// <returns>The block.</returns>
    private KernelBlock BlockOf(StatementSyntax statement, bool save)
    {
        var statements = new List<KernelStatement>();
        var locals = save ? new Dictionary<string, KernelType>(m_Locals, StringComparer.Ordinal) : null;
        var views = save ? new Dictionary<string, SpanView>(m_Views, StringComparer.Ordinal) : null;
        var arrays = save ? new Dictionary<string, ArrayInfo>(m_Arrays, StringComparer.Ordinal) : null;

        Lower(statement, statements);

        if (save)
        {
            m_Locals.Clear();
            m_Views.Clear();
            m_Arrays.Clear();

            foreach (var local in locals!)
            {
                m_Locals[local.Key] = local.Value;
            }

            foreach (var view in views!)
            {
                m_Views[view.Key] = view.Value;
            }

            foreach (var array in arrays!)
            {
                m_Arrays[array.Key] = array.Value;
            }
        }

        return statements.Count == 1 && statements[0] is KernelBlock block ? block : new KernelBlock(statements);
    }

    /// <summary>
    /// Lowers a <c>for</c> loop, which the device has as it is written.
    /// </summary>
    /// <param name="loop">The loop.</param>
    /// <returns>The statement.</returns>
    private KernelStatement For(ForStatementSyntax loop)
    {
        KernelStatement? run = null;

        if (loop.Declaration is not null)
        {
            var declarations = new List<KernelStatement>();
            Declare(loop.Declaration, declarations);
            run = run is null && declarations.Count == 1 ? declarations[0] : new KernelBlock(declarations);
        }
        else if (loop.Initializers.Count > 0)
        {
            var assignments = new List<KernelStatement>();

            foreach (var initializer in loop.Initializers)
            {
                Lower(initializer, assignments);
            }

            run = assignments.Count == 1 ? assignments[0] : new KernelBlock(assignments);
        }

        KernelStatement? next = null;

        if (loop.Incrementors.Count > 0)
        {
            var increments = new List<KernelStatement>();

            foreach (var increment in loop.Incrementors)
            {
                Lower(increment, increments);
            }

            next = increments.Count == 1 ? increments[0] : new KernelBlock(increments);
        }

        var condition = loop.Condition is null ? null : Expression(loop.Condition);
        var body = BlockOf(loop.Statement, true);

        return new KernelFor(run, condition, next, body);
    }

    /// <summary>
    /// Declares a local array and the values it starts with, which the device writes into it one element at a time.
    /// </summary>
    /// <param name="local">The array.</param>
    /// <param name="elements">The values it starts with, or <see langword="null"/> when it starts undefined.</param>
    /// <param name="statements">The statements it adds to.</param>
    private void DeclareArray(ArrayInfo local, IReadOnlyList<ExpressionSyntax>? elements, List<KernelStatement> statements)
    {
        m_Arrays[local.Name] = local;
        statements.Add(new KernelDeclaration(local.Name, new ArrayType(local.Element, local.Length), null));

        if (elements is null)
        {
            return;
        }

        if (elements.Count != local.Length)
        {
            throw new KernelTranslationException($"'{local.Name}' holds {local.Length} elements and starts with {elements.Count}.");
        }

        for (var index = 0; index < elements.Count; index++)
        {
            var element = Expression(elements[index], local.Element);
            var target = new KernelArrayElement(local.Name, new KernelLiteral(index, ScalarType.Int), local.Element);

            statements.Add(new KernelAssignment(target, Coerce(element, local.Element)));
        }
    }

    /// <summary>
    /// Lowers a <c>foreach</c> loop into the counted loop it means over a buffer, a slice or an array.
    /// </summary>
    /// <param name="loop">The loop.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the collection is not one the device walks.</exception>
    private KernelStatement ForEach(ForEachStatementSyntax loop)
    {
        if (!TryResolve(loop.Expression, out var collection))
        {
            throw new KernelTranslationException($"'{loop.Expression}' is not a buffer, a slice of one or a local array, which are the collections a kernel can walk.");
        }

        var name = $"__gubbinsLoop{m_Loops++}";
        var index = new KernelLocal(name, ScalarType.Int);
        var element = new KernelDeclaration(
            loop.Identifier.ValueText,
            collection.Element,
            Element(collection, index));

        m_Locals[name] = ScalarType.Int;
        m_Locals[loop.Identifier.ValueText] = collection.Element;

        var body = new List<KernelStatement> { element };
        body.AddRange(BlockOf(loop.Statement, true).Statements);

        m_Locals.Remove(name);
        m_Locals.Remove(loop.Identifier.ValueText);

        return new KernelFor(
            new KernelDeclaration(name, ScalarType.Int, new KernelLiteral(0, ScalarType.Int)),
            new KernelBinary(KernelBinaryOperator.Less, index, collection.Count, ScalarType.Bool),
            new KernelAssignment(index, new KernelBinary(KernelBinaryOperator.Add, index, new KernelLiteral(1, ScalarType.Int), ScalarType.Int)),
            new KernelBlock(body));
    }

    /// <summary>
    /// Lowers a <c>switch</c> statement into the comparisons it is, because not every backend has the statement and all of
    /// them have the comparisons.
    /// </summary>
    /// <param name="branch">The statement.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="KernelTranslationException">Thrown when a section falls through into the next one.</exception>
    private KernelStatement Branch(SwitchStatementSyntax branch)
    {
        var subject = Expression(branch.Expression);
        var otherwise = KernelBlock.Empty;

        for (var index = branch.Sections.Count - 1; index >= 0; index--)
        {
            var section = branch.Sections[index];
            var statements = new List<KernelStatement>();
            foreach (var statement in section.Statements)
            {
                Lower(statement, statements);
            }

            if (statements.Count > 0 && statements[statements.Count - 1] is KernelBreak)
            {
                statements.RemoveAt(statements.Count - 1);
            }
            else if (section.Labels.Any(label => !label.IsKind(SyntaxKind.DefaultSwitchLabel)))
            {
                throw new KernelTranslationException("A section of a switch falls through into the next one, which the device does not have: end it with break.");
            }

            var block = new KernelBlock(statements);

            if (section.Labels.Any(label => label.IsKind(SyntaxKind.DefaultSwitchLabel)))
            {
                otherwise = block;
                continue;
            }

            foreach (var label in section.Labels.Reverse())
            {
                var value = label is CaseSwitchLabelSyntax constant
                    ? Expression(constant.Value)
                    : throw new KernelTranslationException($"'{label}' has no representation on the device: a switch compares with constants.");

                otherwise = new KernelBlock(new KernelStatement[]
                {
                    new KernelIf(
                        new KernelBinary(KernelBinaryOperator.Equal, subject, value, ScalarType.Bool),
                        block,
                        otherwise)
                });
            }
        }

        return otherwise.Statements.Count == 1 && otherwise.Statements[0] is KernelIf single
            ? single
            : new KernelIf(new KernelLiteral(true, ScalarType.Bool), otherwise, null);
    }

    /// <summary>
    /// Lowers a local declaration, which may declare a span, an array every thread of the group addresses, a local array or
    /// an ordinary value.
    /// </summary>
    /// <param name="declaration">The statement.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when the declaration is outside the supported subset.</exception>
    private void Declare(LocalDeclarationStatementSyntax declaration, List<KernelStatement> statements)
        => Declare(declaration.Declaration, statements);

    /// <summary>
    /// Lowers the variables of one declaration.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="statements">The statements it adds to.</param>
    /// <exception cref="KernelTranslationException">Thrown when the declaration is outside the supported subset.</exception>
    private void Declare(VariableDeclarationSyntax declaration, List<KernelStatement> statements)
    {
        foreach (var variable in declaration.Variables)
        {
            var name = variable.Identifier.ValueText;
            var initializer = variable.Initializer?.Value;

            if (initializer is ArrayCreationExpressionSyntax arrayCreation)
            {
                DeclareArray(LocalArray(variable, arrayCreation.Type.ElementType, initializer), arrayCreation.Initializer?.Expressions.ToList(), statements);
                continue;
            }

            if (initializer is ImplicitArrayCreationExpressionSyntax implicitCreation && declaration.Type is ArrayTypeSyntax declared)
            {
                DeclareArray(LocalArray(variable, declared.ElementType, initializer), implicitCreation.Initializer.Expressions.ToList(), statements);
                continue;
            }

            if (initializer is InvocationExpressionSyntax sharedCall && IsShared(sharedCall, out var sharedDeclaration))
            {
                m_Shared.Add(new KernelSharedArray(name, sharedDeclaration.Element, sharedDeclaration.Length));
                continue;
            }

            if (initializer is not null && initializer is not ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0 })
            {
                if (TryResolve(initializer, out var view))
                {
                    m_Views[name] = view with { };
                    continue;
                }
            }

            var type = declaration.Type.IsVar ? null : TypeOf(declaration.Type);
            KernelExpression? value = null;

            if (initializer is ObjectCreationExpressionSyntax blank && (blank.ArgumentList is null || blank.ArgumentList.Arguments.Count == 0))
            {
                // A value with no arguments is declared, and the fields the constructor would write are what the body
                // writes when it needs them.
                var created = TypeOf(blank.Type);

                if (created is not StructType)
                {
                    throw new KernelTranslationException($"A value of '{blank.Type}' with no arguments is declared as a local, which the device only does for a structure of the kernel.");
                }

                type = created;
            }
            else if (initializer is not null)
            {
                value = Expression(initializer, type);
            }

            type ??= value?.ResultType ?? throw new KernelTranslationException($"'{name}' has no type: a local the device declares is declared with its type, or with a value that has one.");

            if (value?.ResultType is not null && !KernelType.Same(value.ResultType, type))
            {
                value = Coerce(value, type);
            }

            m_Locals[name] = type;
            statements.Add(new KernelDeclaration(name, type, value));
        }
    }

    /// <summary>
    /// Reads the size and element type of a local array.
    /// </summary>
    /// <param name="variable">The variable.</param>
    /// <param name="element">The element type the declaration wrote.</param>
    /// <param name="initializer">The value it starts with.</param>
    /// <returns>The array.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the size is not a literal or the array is not one the device has.</exception>
    private ArrayInfo LocalArray(VariableDeclaratorSyntax variable, TypeSyntax element, ExpressionSyntax initializer)
    {
        switch (initializer)
        {
            case ImplicitArrayCreationExpressionSyntax withoutType:
                return new ArrayInfo(variable.Identifier.ValueText, TypeOf(element), withoutType.Initializer.Expressions.Count);

            case ArrayCreationExpressionSyntax creation:
            {
                if (creation.Type.RankSpecifiers.FirstOrDefault()?.Sizes.FirstOrDefault() is not { } size ||
                    !int.TryParse(size.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) ||
                    length <= 0)
                {
                    throw new KernelTranslationException("A local array takes its size as a literal: the device sizes it at compile time.");
                }

                return new ArrayInfo(variable.Identifier.ValueText, TypeOf(creation.Type.ElementType), length);
            }

            default:
                throw new KernelTranslationException("A local array is declared with an array creation.");
        }
    }

    /// <summary>
    /// Reads the type a declaration writes.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <param name="substitutions">What the type parameters of a generic structure are, or <see langword="null"/>.</param>
    /// <returns>The type of the device.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the type has no representation on the device.</exception>
    private KernelType TypeOf(TypeSyntax type, IReadOnlyDictionary<string, KernelType>? substitutions = null)
    {
        switch (type)
        {
            case PredefinedTypeSyntax predefined:
                return predefined.Keyword.Kind() switch
                {
                    SyntaxKind.IntKeyword => ScalarType.Int,
                    SyntaxKind.UIntKeyword => ScalarType.UInt,
                    SyntaxKind.FloatKeyword => ScalarType.Float,
                    SyntaxKind.BoolKeyword => ScalarType.Bool,
                    SyntaxKind.VoidKeyword => VoidType.Void,
                    _ => throw new KernelTranslationException($"'{predefined}' has no representation on the device: a kernel computes with int, uint and float.")
                };

            case ArrayTypeSyntax array:
                throw new KernelTranslationException($"'{array}' is a type of a local array, which is declared with the array it creates.");

            case GenericNameSyntax generic:
                return Generic(generic, substitutions);

            case IdentifierNameSyntax identifier:
                return Named(identifier.Identifier.ValueText, Array.Empty<KernelType>(), substitutions);

            case QualifiedNameSyntax qualified:
                return TypeOf(qualified.Right, substitutions);

            default:
                throw new KernelTranslationException($"'{type}' has no representation on the device.");
        }
    }

    /// <summary>
    /// Reads a type written with its arguments.
    /// </summary>
    /// <param name="generic">The type.</param>
    /// <param name="substitutions">What the type parameters of a generic structure are, or <see langword="null"/>.</param>
    /// <returns>The type of the device.</returns>
    private KernelType Generic(GenericNameSyntax generic, IReadOnlyDictionary<string, KernelType>? substitutions)
    {
        if (generic.Identifier.ValueText == KernelTranspiler.SHARED || generic.Identifier.ValueText == KernelTranspiler.SLICE)
        {
            throw new KernelTranslationException($"'{generic}' is not a type of the device.");
        }

        var arguments = generic.TypeArgumentList.Arguments.Select(argument => TypeOf(argument, substitutions)).ToList();

        return Named(generic.Identifier.ValueText, arguments, substitutions);
    }

    /// <summary>
    /// Reads a named type, which is a scalar a shading language spells out, a vector, or one of the kernel's structures.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="arguments">The type arguments, when it was written with them.</param>
    /// <param name="substitutions">What the type parameters of a generic structure are, or <see langword="null"/>.</param>
    /// <returns>The type of the device.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the name has no representation on the device.</exception>
    private KernelType Named(string name, IReadOnlyList<KernelType> arguments, IReadOnlyDictionary<string, KernelType>? substitutions)
    {
        if (arguments.Count == 0 && substitutions is not null && substitutions.TryGetValue(name, out var substituted))
        {
            return substituted;
        }

        if (Vector(name) is { } vector)
        {
            return vector;
        }

        if (!m_Declared.TryGetValue(name, out var declaration))
        {
            throw new KernelTranslationException($"'{name}' has no representation on the device: it is not int, uint, float, a vector of them, or a structure the kernel declares.");
        }

        var parameters = declaration.TypeParameterList?.Parameters.Count ?? 0;

        if (parameters != arguments.Count)
        {
            if (parameters == 0)
            {
                return Materialize(new StructType(name, Array.Empty<KernelType>()));
            }

            throw new KernelTranslationException($"'{name}' takes {parameters} type arguments and was written with {arguments.Count}.");
        }

        return Materialize(new StructType(arguments.Count == 0 ? name : name + "_" + string.Join("_", arguments.Select(Mangle)), arguments));
    }

    /// <summary>
    /// Reports whether a name is one of the numbers a buffer can hold.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><see langword="true"/> when it is a scalar or a vector of them.</returns>
    private static bool IsElementName(string name) => name is "int" or "uint" or "float" || Vector(name) is not null;

    /// <summary>
    /// Reads a vector type by the name a shading language spells it with.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The vector, or <see langword="null"/> when the name is not one.</returns>
    private static VectorType? Vector(string name)
    {
        if (name.Length < 2)
        {
            return null;
        }

        var components = name[name.Length - 1] - '0';

        if (components is < 2 or > 4)
        {
            return null;
        }

        return name.Substring(0, name.Length - 1) switch
        {
            "int" => new VectorType(KernelScalar.Int, components),
            "uint" => new VectorType(KernelScalar.UInt, components),
            "float" => new VectorType(KernelScalar.Float, components),
            _ => null
        };
    }

    /// <summary>
    /// Declares a structure for the device, with the fields the arguments instantiate it with.
    /// </summary>
    /// <param name="structure">The structure, with its arguments.</param>
    /// <returns>The structure.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the structure holds something the device cannot express.</exception>
    private StructType Materialize(StructType structure)
    {
        if (m_Structures.ContainsKey(structure.Name))
        {
            return structure;
        }

        var name = TemplateOf(structure);
        var declaration = m_Declared[name];
        var substitutions = Substitutions(declaration, structure);
        var fields = new List<KernelField>();

        // The fields are declared before the structure is registered, so a structure that holds itself is refused before
        // anything is emitted for it.
        if (declaration.BaseList is not null)
        {
            throw new KernelTranslationException($"'{name}' cannot inherit: a struct of a kernel is a value with fields and methods.");
        }

        foreach (var member in declaration.Members.OfType<FieldDeclarationSyntax>())
        {
            if (member.Declaration.Variables.Any(variable => variable.Initializer is not null))
            {
                throw new KernelTranslationException($"A field of '{name}' cannot start with a value: give the struct a constructor instead.");
            }

            var type = TypeOf(member.Declaration.Type, substitutions);

            foreach (var variable in member.Declaration.Variables)
            {
                fields.Add(new KernelField(variable.Identifier.ValueText, type));
            }
        }

        m_Structures[structure.Name] = new KernelStruct(structure.Name, fields);

        foreach (var field in fields)
        {
            if (field.Type is StructType held)
            {
                Materialize(held);
            }
        }

        // A structure the source declares without type parameters has every member the device can call, so that the
        // translation of a kernel does not depend on which of them it happens to reach. One instantiated with arguments
        // gets the members the kernel calls, because the body has to be read against the arguments it holds.
        if (structure.Arguments.Count == 0)
        {
            foreach (var member in declaration.Members)
            {
                switch (member)
                {
                    case MethodDeclarationSyntax method:
                        Member(structure, method, declaration, !method.Modifiers.Any(SyntaxKind.StaticKeyword), !method.Modifiers.Any(SyntaxKind.StaticKeyword) && Mutates(method));
                        break;

                    case PropertyDeclarationSyntax property:
                        Member(structure, property, declaration, true, false);
                        break;

                    case ConstructorDeclarationSyntax constructor:
                        Member(structure, constructor, declaration, false, false);
                        break;

                    case OperatorDeclarationSyntax operation:
                        Member(structure, operation, declaration, false, false);
                        break;
                }
            }
        }

        return structure;
    }

    /// <summary>
    /// Reads the type arguments a structure was instantiated with.
    /// </summary>
    /// <param name="declaration">The declaration of the structure.</param>
    /// <param name="structure">The structure, with its arguments.</param>
    /// <returns>The arguments, by the name of the parameter they take.</returns>
    private static Dictionary<string, KernelType> Substitutions(StructDeclarationSyntax declaration, StructType structure)
    {
        var substitutions = new Dictionary<string, KernelType>(StringComparer.Ordinal);
        var parameters = declaration.TypeParameterList?.Parameters;

        if (parameters is null)
        {
            return substitutions;
        }

        for (var index = 0; index < parameters.Value.Count && index < structure.Arguments.Count; index++)
        {
            substitutions[parameters.Value[index].Identifier.ValueText] = structure.Arguments[index];
        }

        return substitutions;
    }

    /// <summary>
    /// Reads the name a type is written with inside another name.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The name, with its arguments.</returns>
    private static string Mangle(KernelType type) => type switch
    {
        VectorType vector => vector.TypeName,
        ScalarType scalar => scalar.TypeName,
        StructType structure => structure.Name,
        ArrayType array => Mangle(array.Element) + "_" + array.Length,
        _ => type.TypeName
    };

    /// <summary>
    /// Reads the name a member of a structure is emitted under.
    /// </summary>
    /// <param name="structure">The structure, with its arguments.</param>
    /// <param name="member">The name of the member.</param>
    /// <returns>The name of the function.</returns>
    private static string FunctionName(StructType structure, string member) => structure.Name + "_" + member;

    /// <summary>
    /// Reads the name C# gives an operator, which is the name the function is emitted under.
    /// </summary>
    /// <param name="kind">The token of the operator.</param>
    /// <returns>The name.</returns>
    private static string OperatorName(SyntaxKind kind) => kind switch
    {
        SyntaxKind.PlusToken => "op_Addition",
        SyntaxKind.MinusToken => "op_Subtraction",
        SyntaxKind.AsteriskToken => "op_Multiply",
        SyntaxKind.SlashToken => "op_Division",
        SyntaxKind.PercentToken => "op_Modulus",
        SyntaxKind.EqualsEqualsToken => "op_Equality",
        SyntaxKind.ExclamationEqualsToken => "op_Inequality",
        SyntaxKind.LessThanToken => "op_LessThan",
        SyntaxKind.LessThanEqualsToken => "op_LessThanOrEqual",
        SyntaxKind.GreaterThanToken => "op_GreaterThan",
        SyntaxKind.GreaterThanEqualsToken => "op_GreaterThanOrEqual",
        SyntaxKind.AmpersandToken => "op_BitwiseAnd",
        SyntaxKind.BarToken => "op_BitwiseOr",
        SyntaxKind.CaretToken => "op_ExclusiveOr",
        _ => "op_" + kind
    };

    /// <summary>
    /// Reads the name of the operator a binary expression is.
    /// </summary>
    /// <param name="kind">The token of the operator.</param>
    /// <returns>The name C# gives it, or <see langword="null"/> when it is not an operator a structure can declare.</returns>
    private static string? OperatorNameOf(SyntaxKind kind) => kind switch
    {
        SyntaxKind.PlusToken => "op_Addition",
        SyntaxKind.MinusToken => "op_Subtraction",
        SyntaxKind.AsteriskToken => "op_Multiply",
        SyntaxKind.SlashToken => "op_Division",
        SyntaxKind.PercentToken => "op_Modulus",
        SyntaxKind.EqualsEqualsToken => "op_Equality",
        SyntaxKind.ExclamationEqualsToken => "op_Inequality",
        SyntaxKind.LessThanToken => "op_LessThan",
        SyntaxKind.LessThanEqualsToken => "op_LessThanOrEqual",
        SyntaxKind.GreaterThanToken => "op_GreaterThan",
        SyntaxKind.GreaterThanEqualsToken => "op_GreaterThanOrEqual",
        SyntaxKind.AmpersandToken => "op_BitwiseAnd",
        SyntaxKind.BarToken => "op_BitwiseOr",
        SyntaxKind.CaretToken => "op_ExclusiveOr",
        _ => null
    };

    /// <summary>
    /// Reports whether a method writes a field of the value it is called on, which is what the receiver is passed back to
    /// the caller for.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> when the body assigns to a member.</returns>
    private static bool Mutates(SyntaxNode method)
    {
        foreach (var node in method.DescendantNodes())
        {
            ExpressionSyntax? target = node switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left,
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression) => prefix.Operand,
                PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression) => postfix.Operand,
                _ => null
            };

            if (target is IdentifierNameSyntax || target is MemberAccessExpressionSyntax)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Orders the structures of a source so that a structure comes after every structure it holds.
    /// </summary>
    /// <param name="structures">The structures, in the order the source declares them.</param>
    /// <returns>The same structures, in an order the shading languages accept.</returns>
    /// <exception cref="KernelTranslationException">Thrown when two structures hold each other.</exception>
    private List<StructDeclarationSyntax> InDependencyOrder(List<StructDeclarationSyntax> structures)
    {
        var byName = new Dictionary<string, StructDeclarationSyntax>(StringComparer.Ordinal);

        foreach (var structure in structures)
        {
            byName[structure.Identifier.ValueText] = structure;
        }

        var ordered = new List<StructDeclarationSyntax>();
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        void Visit(StructDeclarationSyntax structure)
        {
            var name = structure.Identifier.ValueText;

            if (visited.Contains(name))
            {
                return;
            }

            if (!visiting.Add(name))
            {
                throw new KernelTranslationException($"'{name}' holds itself: a struct of a kernel is a value, so it cannot contain its own type.");
            }

            foreach (var field in structure.Members.OfType<FieldDeclarationSyntax>())
            {
                var type = KernelTranspiler.TypeName(field.Declaration.Type);

                if (byName.TryGetValue(type, out var dependency))
                {
                    Visit(dependency);
                }
            }

            visiting.Remove(name);
            visited.Add(name);
            ordered.Add(structure);
        }

        foreach (var structure in structures)
        {
            Visit(structure);
        }

        return ordered;
    }

    /// <summary>
    /// One helper of the program: the declaration it is lowered from, the class that declares it, and the name the
    /// program emits it under, which is qualified when two classes declare a method of the same name.
    /// </summary>
    private sealed class HelperInfo
    {
        internal HelperInfo(MethodDeclarationSyntax syntax, string simple, string owner)
        {
            Syntax = syntax;
            Simple = simple;
            Owner = owner;
        }

        /// <summary>
        /// Gets the declaration of the helper.
        /// </summary>
        internal MethodDeclarationSyntax Syntax { get; }

        /// <summary>
        /// Gets the name the source declares it with.
        /// </summary>
        internal string Simple { get; }

        /// <summary>
        /// Gets the name of the class that declares it.
        /// </summary>
        internal string Owner { get; }

        /// <summary>
        /// Gets or sets the name the program emits it under.
        /// </summary>
        internal string Emitted { get; set; } = string.Empty;
    }

    /// <summary>
    /// One span of a buffer, as the buffer and the numbers the device reads it through.
    /// </summary>
    /// <param name="Buffer">The buffer the span is a window on.</param>
    /// <param name="Offset">The element the window starts at.</param>
    /// <param name="Count">The number of elements it holds.</param>
    /// <param name="Element">The type of one element.</param>
    private sealed record SpanView(string Buffer, KernelExpression Offset, KernelExpression Count, KernelType Element);

    /// <summary>
    /// One local array, as the declaration the device holds.
    /// </summary>
    /// <param name="Name">The name the body reads it by.</param>
    /// <param name="Element">The type of one element.</param>
    /// <param name="Length">The number of elements.</param>
    private sealed record ArrayInfo(string Name, KernelType Element, int Length);

    /// <summary>
    /// A shared array the body declared, before it is added to the function.
    /// </summary>
    /// <param name="Element">The type of one element.</param>
    /// <param name="Length">The number of elements.</param>
    private sealed record SharedDeclaration(KernelType Element, int Length);

    /// <summary>
    /// Reads an element of a span, a buffer or an array.
    /// </summary>
    /// <param name="collection">The collection.</param>
    /// <param name="index">The element index.</param>
    /// <returns>The element.</returns>
    private static KernelExpression Element(SpanView collection, KernelExpression index)
    {
        var element = collection.Offset is KernelLiteral { Value: 0 } ? index : new KernelBinary(KernelBinaryOperator.Add, index, collection.Offset, ScalarType.Int);

        return new KernelBufferElement(collection.Buffer, element, collection.Element);
    }
}
