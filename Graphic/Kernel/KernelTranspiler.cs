using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Turns the supported subset of C# into Slang, so that one kernel written as C# is compiled once by <c>slangc</c> into
/// the artifacts every backend loads.
/// </summary>
/// <remarks>
/// <para>
/// The subset is a static method whose first parameter is the thread index followed by buffers: the C# and the Slang
/// expression grammars agree on arithmetic, indexing, casts and control flow, so the body is carried over as written and
/// only the declarations and the index access are translated.
/// </para>
/// <para>
/// Two things are not written the way they are used. A buffer's <c>Length</c> is not readable from the device on every
/// backend, so the caller passes the element counts and the body reads them through the buffer that carries them. A call
/// to one of the maths functions the table maps becomes the intrinsic with that meaning, which is the whole reason the
/// table exists: the names differ between C# and the shading languages while the operations do not.
/// </para>
/// <para>
/// What is inside: a static method whose first parameter is the thread index, buffers declared as <c>ReadOnlySpan&lt;T&gt;</c>
/// or <c>Span&lt;T&gt;</c> over an element type every backend has, locals of every shape the device has (a fixed size array,
/// a slice, a range, an index from the end), arithmetic and bit operations, casts to those element types, <c>if</c>/<c>else</c>,
/// <c>switch</c> statements and expressions over constants, <c>for</c>, <c>while</c>, <c>do</c>, <c>foreach</c> over a buffer,
/// a slice or an array, <c>is</c> against a constant, helper methods with <c>ref</c> and <c>out</c> parameters, value structs
/// with fields, a constructor, read only properties, instance and static methods, operators and type parameters, the maths
/// calls the table above maps, the wave calls beside them, an array the group shares with <c>Shared&lt;T&gt;(count)</c> and
/// <c>Barrier()</c> to synchronize it, and <c>Interlocked.Add</c> over a buffer element used as a statement.
/// </para>
/// <para>
/// Anything outside the subset is refused here rather than emitted and rejected later by the compiler, with the construct
/// named in the message: an object, an array, <c>foreach</c>, a string, <c>checked</c>, an exception, a lock, a lambda, a
/// cast to a type the shading language does not name, an element type no D3D12 profile has. This is a translator for a
/// deliberately small language, not a compiler.
/// </para>
/// </remarks>
public static class KernelTranspiler
{
    /// <summary>
    /// The type name a kernel declares its thread index with.
    /// </summary>
    public const string INDEX_TYPE = "GpuIndex";

    /// <summary>
    /// The type name a kernel declares its thread index with, as an ordinary C# parameter.
    /// </summary>
    public const string SPAN_INDEX_TYPE = "int";

    /// <summary>
    /// The type name a kernel declares a read-only buffer with.
    /// </summary>
    public const string READ_ONLY_BUFFER_TYPE = "GpuReadOnlyBuffer";

    /// <summary>
    /// The read-only span a kernel declares a buffer with.
    /// </summary>
    public const string READ_ONLY_SPAN_TYPE = "ReadOnlySpan";

    /// <summary>
    /// The type name a kernel declares a writable buffer with.
    /// </summary>
    public const string WRITABLE_BUFFER_TYPE = "GpuBuffer";

    /// <summary>
    /// The span a kernel declares a writable buffer with.
    /// </summary>
    public const string SPAN_TYPE = "Span";

    /// <summary>
    /// The number of threads in a group a kernel is dispatched with when it does not say otherwise.
    /// </summary>
    public const int THREADS_PER_GROUP = 64;

    /// <summary>
    /// The attribute a kernel declares its group size with.
    /// </summary>
    public const string THREADS_ATTRIBUTE = "GpuThreads";

    /// <summary>
    /// The attribute that marks a method as a kernel, which says nothing the signature does not already say.
    /// </summary>
    public const string KERNEL_ATTRIBUTE = "GpuKernel";

    /// <summary>
    /// The name of the buffer the buffer lengths are passed in.
    /// </summary>
    public const string LENGTHS_BUFFER = "__gubbinsLengths";

    /// <summary>
    /// The name the kernel is compiled under, which is the entry point every backend looks the kernel up by.
    /// </summary>
    public const string ENTRY_POINT = "gubbins_main";

    /// <summary>
    /// The method a kernel slices a span with.
    /// </summary>
    private const string SLICE = "Slice";

    /// <summary>
    /// The text a slice leaves behind when it is not the value of a local.
    /// </summary>
    private const string SLICE_METHOD = ".Slice(";

    /// <summary>
    /// The text a range leaves behind when it is used where the device needs a value.
    /// </summary>
    private const string RANGE = "..";

    /// <summary>
    /// The offset of a span that starts at the beginning of its buffer.
    /// </summary>
    private const string ZERO = "0";

    /// <summary>
    /// The element type an array creation without a type takes, which the declared type of the variable supplies.
    /// </summary>
    private const string ELEMENT_OF_IMPLICIT = "";

    /// <summary>
    /// The method a kernel declares an array the whole group addresses with.
    /// </summary>
    public const string SHARED = "Shared";

    /// <summary>
    /// The call that makes a group wait for its own writes.
    /// </summary>
    public const string BARRIER = "Barrier";

    /// <summary>
    /// The call that adds to a value several threads write at once.
    /// </summary>
    public const string INTERLOCKED = "Interlocked.Add";

    /// <summary>
    /// The element types a buffer can hold, which are the ones every profile of the shading language has.
    /// </summary>
    /// <remarks>
    /// A narrower type is not a portability problem this layer can paper over: D3D12 has no eight or sixteen bit type
    /// without a compiler switch, while the other backends do, so a buffer of one of them would work everywhere but there.
    /// </remarks>
    private static readonly System.Text.RegularExpressions.Regex ELEMENT_TYPE =
        new(@"^(int|uint|float)([234])?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// The calls a kernel body may make, and the intrinsic each one becomes.
    /// </summary>
    /// <remarks>
    /// The list holds only what means the same thing on every backend: rounding modes and the shapes the maths libraries
    /// disagree about are left out, so a kernel that needs them is refused instead of quietly computing something else.
    /// </remarks>
    private static readonly Dictionary<string, string> INTRINSICS = new(StringComparer.Ordinal)
    {
        ["Math.Abs"] = "abs",
        ["MathF.Abs"] = "abs",
        ["Math.Min"] = "min",
        ["MathF.Min"] = "min",
        ["Math.Max"] = "max",
        ["MathF.Max"] = "max",
        ["Math.Sqrt"] = "sqrt",
        ["MathF.Sqrt"] = "sqrt",
        ["Math.Floor"] = "floor",
        ["MathF.Floor"] = "floor",
        ["Math.Ceiling"] = "ceil",
        ["MathF.Ceiling"] = "ceil",
        ["Math.Pow"] = "pow",
        ["MathF.Pow"] = "pow",
        ["Math.Clamp"] = "clamp",
        ["MathF.Clamp"] = "clamp",
        ["Math.Sin"] = "sin",
        ["MathF.Sin"] = "sin",
        ["Math.Cos"] = "cos",
        ["MathF.Cos"] = "cos",
        ["GpuWave.Sum"] = "WaveActiveSum",
        ["GpuWave.Min"] = "WaveActiveMin",
        ["GpuWave.Max"] = "WaveActiveMax",
        ["GpuWave.Product"] = "WaveActiveProduct",
        ["GpuWave.PrefixSum"] = "WavePrefixSum",
        ["GpuWave.Any"] = "WaveActiveAnyTrue",
        ["GpuWave.All"] = "WaveActiveAllTrue",
    };

    /// <summary>
    /// Translates one C# kernel to Slang.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <param name="entryName">The kernel to translate, or <see langword="null"/> to take the only one there is.</param>
    /// <returns>The Slang program, with the bindings the backends expect.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the source is outside the supported subset.</exception>
    /// <remarks>
    /// One program serves all five backends, because the bindings are annotated for each of them at once: D3D12, Vulkan,
    /// OpenGL, Metal and CUDA each read the artifact they can load out of the same translation.
    /// </remarks>
    public static string Transpile(string source, string? entryName = null)
    {
        var (method, tree) = Locate(source, entryName);

        return Emit(method, tree);
    }

    /// <summary>
    /// Finds the kernel a source declares.
    /// </summary>
    /// <param name="source">The C# source.</param>
    /// <param name="entryName">The kernel to take, or <see langword="null"/> to take the only one there is.</param>
    /// <returns>The kernel, and the tree it was read from.</returns>
    /// <exception cref="KernelTranslationException">Thrown when there is no kernel, or more than one and none was named.</exception>
    private static (MethodDeclarationSyntax Method, SyntaxTree Tree) Locate(string source, string? entryName)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var tree = CSharpSyntaxTree.ParseText(source);
        var methods = tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(IsKernel).ToList();

        if (methods.Count == 0)
        {
            throw new KernelTranslationException("No kernel was found: a kernel is a static method whose first parameter is the thread index.");
        }

        if (entryName is not null)
        {
            methods = methods.Where(method => method.Identifier.ValueText == entryName).ToList();

            if (methods.Count == 0)
            {
                throw new KernelTranslationException($"No kernel named '{entryName}' was found.");
            }
        }

        if (methods.Count > 1)
        {
            throw new KernelTranslationException($"{methods.Count} kernels were found; name the one to translate.");
        }

        return (methods[0], tree);
    }

    /// <summary>
    /// Reports whether a method declares a kernel.
    /// </summary>
    /// <param name="method">The method to test.</param>
    /// <returns><see langword="true"/> when its first parameter is the thread index.</returns>
    private static bool IsKernel(MethodDeclarationSyntax method)
    {
        if (!method.Modifiers.Any(SyntaxKind.StaticKeyword) || method.ParameterList.Parameters.Count == 0)
        {
            return false;
        }

        var type = TypeName(method.ParameterList.Parameters[0].Type);

        return type == INDEX_TYPE || type == SPAN_INDEX_TYPE;
    }

    /// <summary>
    /// Reads the type name a parameter declares, without its namespace.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The type name, or an empty string when there is none.</returns>
    private static string TypeName(TypeSyntax? type) => type switch
    {
        null => string.Empty,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => type.ToString()
    };

    /// <summary>
    /// Builds the Slang program for one kernel.
    /// </summary>
    /// <param name="method">The kernel.</param>
    /// <param name="tree">The tree the kernel was parsed from.</param>
    /// <returns>The Slang text.</returns>
    private static string Emit(MethodDeclarationSyntax method, SyntaxTree tree)
    {
        var parameters = ReadParameters(method, tree);
        var shared = new StringBuilder();
        var body = RewriteBody(method, parameters, shared, Structures(tree));
        var count = parameters.Count(parameter => parameter.Kind != KernelParameterKind.Index);
        var text = new StringBuilder();

        text.Append(StructureText(tree));
        text.Append(FunctionText(tree, method));

        // Every buffer is writable here because a backend that binds one address space for all resources cannot tell a
        // readable buffer from a writable one: Metal would place the two at the same index. The kernel the subset
        // accepts writes only the buffer it declares writable, so nothing is lost but the read-only cache hint.
        foreach (var parameter in parameters)
        {
            if (parameter.Kind == KernelParameterKind.Index)
            {
                continue;
            }

            text.AppendLine($"[[vk::binding({parameter.Binding}, 0)]] RWStructuredBuffer<{parameter.ElementType}> {parameter.Name} : register(u{parameter.Binding});");
        }

        if (count > 0)
        {
            text.AppendLine($"[[vk::binding({count}, 0)]] RWStructuredBuffer<uint> {LENGTHS_BUFFER} : register(u{count});");
        }

        text.Append(shared.ToString());

        var index = parameters.First(parameter => parameter.Kind == KernelParameterKind.Index);

        text.AppendLine();
        text.AppendLine($"#define {index.Name} (__gubbinsThreadId.x)");
        text.AppendLine();
        text.AppendLine("[shader(\"compute\")]");
        text.AppendLine($"[numthreads({Threads(method)}, 1, 1)]");
        text.AppendLine($"void {ENTRY_POINT}(uint3 __gubbinsThreadId : SV_DispatchThreadID)");
        text.AppendLine("{");
        text.Append(Prologue(parameters));
        text.AppendLine();
        text.AppendLine(body);
        text.AppendLine("}");

        return text.ToString();
    }

    /// <summary>
    /// Reads the number of threads a kernel asks its group to have.
    /// </summary>
    /// <param name="method">The kernel.</param>
    /// <returns>The number the kernel declares, or <see cref="THREADS_PER_GROUP"/> when it declares none.</returns>
    /// <exception cref="KernelTranslationException">Thrown when an attribute means nothing to the device.</exception>
    private static int Threads(MethodDeclarationSyntax method)
    {
        var threads = THREADS_PER_GROUP;

        foreach (var attribute in method.AttributeLists.SelectMany(list => list.Attributes))
        {
            var name = TypeName(attribute.Name);

            if (name == KERNEL_ATTRIBUTE)
            {
                continue;
            }

            if (name != THREADS_ATTRIBUTE)
            {
                throw new KernelTranslationException($"The attribute '{attribute}' means nothing to the device. A kernel declares at most [{KERNEL_ATTRIBUTE}] and [{THREADS_ATTRIBUTE}(count)].");
            }

            if (attribute.ArgumentList?.Arguments.Count != 1 || !int.TryParse(attribute.ArgumentList.Arguments[0].Expression.ToString(), out var count) || count <= 0)
            {
                throw new KernelTranslationException($"[{THREADS_ATTRIBUTE}(count)] takes the number of threads in a group as a literal.");
            }

            threads = count;
        }

        return threads;
    }

    /// <summary>
    /// Reads the number of threads in a group the kernel in <paramref name="source"/> is written for.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <param name="entryName">The kernel to read, or <see langword="null"/> to take the only one there is.</param>
    /// <returns>The number of threads in a group, which is what a dispatch divides its element count by.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the source is outside the supported subset.</exception>
    public static int ThreadsPerGroup(string source, string? entryName = null) => Threads(Locate(source, entryName).Method);

    /// <summary>
    /// Reads the names of the structs a source declares.
    /// </summary>
    /// <param name="tree">The tree to read.</param>
    /// <returns>The names, in the order the source declares them.</returns>
    private static IReadOnlyCollection<string> Structures(SyntaxTree tree) => tree
        .GetCompilationUnitRoot()
        .DescendantNodes()
        .OfType<StructDeclarationSyntax>()
        .Select(structure => structure.Identifier.ValueText)
        .ToList();

    /// <summary>
    /// Translates the structs a kernel works with, which the shading language has as value types of their own.
    /// </summary>
    /// <param name="tree">The tree the kernel was parsed from.</param>
    /// <returns>The declarations, in an order where a struct comes before the one that holds it.</returns>
    /// <exception cref="KernelTranslationException">Thrown when a struct holds something the device cannot express.</exception>
    private static string StructureText(SyntaxTree tree)
    {
        var text = new StringBuilder();
        var declarations = tree.GetCompilationUnitRoot().DescendantNodes().OfType<StructDeclarationSyntax>().ToList();
        var rewriter = new SliceRewriter(Array.Empty<KernelParameter>(), declarations.Select(structure => structure.Identifier.ValueText).ToList(), Callables(tree), Signatures(tree));

        foreach (var declared in InDependencyOrder(declarations))
        {
            // The members are rewritten the way the body is, so that a value built with 'new' is built the way the
            // language builds one.
            var structure = (StructDeclarationSyntax)rewriter.Visit(declared)!;
            var name = structure.Identifier.ValueText;

            if (structure.BaseList is not null)
            {
                throw new KernelTranslationException($"'{name}' cannot inherit: a struct of a kernel is a value with fields and methods.");
            }

            var parameters = structure.TypeParameterList is null
                ? string.Empty
                : $"<{string.Join(", ", structure.TypeParameterList.Parameters.Select(parameter => parameter.Identifier.ValueText))}>";

            text.AppendLine($"struct {name}{parameters}");
            text.AppendLine("{");

            // An operator is a function of the module and not a member of the type, which is the one place the two
            // languages put a declaration differently.
            var operators = new StringBuilder();

            foreach (var member in structure.Members)
            {
                if (member is OperatorDeclarationSyntax operation)
                {
                    operators.AppendLine($"{operation.ReturnType} operator{operation.OperatorToken.Text}({Parameters(operation.ParameterList)}){Body(operation.Body, operation.ExpressionBody, 0)}");
                    continue;
                }

                text.Append(StructureMember(name, member));
            }

            text.AppendLine("}");
            text.Append(operators.ToString());
            text.AppendLine();
        }

        return text.ToString();
    }

    /// <summary>
    /// Translates the methods of the kernel's own class that are not kernels, which are the helpers it calls.
    /// </summary>
    /// <param name="tree">The tree the kernel was parsed from.</param>
    /// <param name="kernel">The kernel itself, which is not a helper.</param>
    /// <returns>The helpers, as the shading language writes them.</returns>
    /// <exception cref="KernelTranslationException">Thrown when a helper is generic or takes a span.</exception>
    private static string FunctionText(SyntaxTree tree, MethodDeclarationSyntax kernel)
    {
        var text = new StringBuilder();

        foreach (var method in tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (method == kernel || !method.Modifiers.Any(SyntaxKind.StaticKeyword) || method.Parent is StructDeclarationSyntax)
            {
                continue;
            }

            if (method.TypeParameterList is not null)
            {
                throw new KernelTranslationException($"'{method.Identifier.ValueText}' is generic: a helper of a kernel is a plain function.");
            }

            text.AppendLine($"{method.ReturnType} {method.Identifier.ValueText}({Parameters(method.ParameterList)}){Body(method.Body, method.ExpressionBody, 0)}");
        }

        return text.ToString();
    }

    /// <summary>
    /// Reads the parameter types of every name a body may call.
    /// </summary>
    /// <param name="tree">The tree to read.</param>
    /// <returns>The types, by name, for the names the kernel declares.</returns>
    private static Dictionary<string, IReadOnlyList<ParameterSyntax>> Signatures(SyntaxTree tree)
    {
        var signatures = new Dictionary<string, IReadOnlyList<ParameterSyntax>>(StringComparer.Ordinal);

        foreach (var method in tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            if (IsKernel(method))
            {
                continue;
            }

            signatures[method.Identifier.ValueText] = method.ParameterList.Parameters;
        }

        return signatures;
    }

    /// <summary>
    /// Reads every name a body may call: the helpers of the kernel's class and the methods of its structs.
    /// </summary>
    /// <param name="tree">The tree to read.</param>
    /// <returns>The names.</returns>
    private static HashSet<string> Callables(SyntaxTree tree)
    {
        var names = Functions(tree);

        foreach (var structure in tree.GetCompilationUnitRoot().DescendantNodes().OfType<StructDeclarationSyntax>())
        {
            foreach (var method in structure.Members.OfType<MethodDeclarationSyntax>())
            {
                names.Add(method.Identifier.ValueText);
            }
        }

        return names;
    }

    /// <summary>
    /// Reads the names of the helpers a source declares beside the kernel.
    /// </summary>
    /// <param name="tree">The tree to read.</param>
    /// <returns>The names, which are the calls the subset allows by name.</returns>
    private static HashSet<string> Functions(SyntaxTree tree) => tree
        .GetCompilationUnitRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Where(method => !IsKernel(method))
        .Select(method => method.Identifier.ValueText)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Orders the structs of a source so that a struct comes after every struct it holds.
    /// </summary>
    /// <param name="structures">The structs, in the order the source declares them.</param>
    /// <returns>The same structs, in an order the shading language accepts.</returns>
    /// <exception cref="KernelTranslationException">Thrown when two structs hold each other.</exception>
    private static List<StructDeclarationSyntax> InDependencyOrder(List<StructDeclarationSyntax> structures)
    {
        var byName = structures.ToDictionary(structure => structure.Identifier.ValueText, StringComparer.Ordinal);
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
                var type = TypeName(field.Declaration.Type);

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
    /// Translates one member of a struct.
    /// </summary>
    /// <param name="structure">The name of the struct.</param>
    /// <param name="member">The member to translate.</param>
    /// <returns>The member, as the shading language writes it.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the member has no representation on the device.</exception>
    private static string StructureMember(string structure, MemberDeclarationSyntax member)
    {
        switch (member)
        {
            case FieldDeclarationSyntax field:
                if (field.Declaration.Variables.Any(variable => variable.Initializer is not null))
                {
                    throw new KernelTranslationException($"A field of '{structure}' cannot start with a value: give the struct a constructor instead.");
                }

                return $"    {field.Declaration.Type} {string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText))};{Environment.NewLine}";

            case ConstructorDeclarationSyntax constructor:
                return $"    __init({Parameters(constructor.ParameterList)}){Body(constructor.Body, constructor.ExpressionBody, 4)}";

            case PropertyDeclarationSyntax property:
                return StructureProperty(structure, property);

            case MethodDeclarationSyntax method:
                return StructureMethod(structure, method);

            default:
                throw new KernelTranslationException($"A member of '{structure}' has no representation on the device: a struct holds fields, a constructor, read only properties and methods.");
        }
    }

    /// <summary>
    /// Translates one read only property of a struct.
    /// </summary>
    /// <param name="structure">The name of the struct.</param>
    /// <param name="property">The property to translate.</param>
    /// <returns>The property, as the shading language writes it.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the property can be written.</exception>
    private static string StructureProperty(string structure, PropertyDeclarationSyntax property)
    {
        var name = $"{property.Type} {property.Identifier.ValueText}";

        if (property.ExpressionBody is not null)
        {
            return $"    property {name} {{ get {{ return {property.ExpressionBody.Expression}; }} }}{Environment.NewLine}";
        }

        var accessors = property.AccessorList?.Accessors;

        if (accessors is null || accessors.Value.Count != 1 || !accessors.Value[0].IsKind(SyntaxKind.GetAccessorDeclaration))
        {
            throw new KernelTranslationException($"A property of '{structure}' is read only: the device reads a value, it does not run a setter.");
        }

        var getter = accessors.Value[0];

        if (getter.ExpressionBody is not null)
        {
            return $"    property {name} {{ get {{ return {getter.ExpressionBody.Expression}; }} }}{Environment.NewLine}";
        }

        var lines = Indent(getter.Body!.Statements).Split('\n');
        var text = new StringBuilder($"    property {name}{Environment.NewLine}    {{{Environment.NewLine}        get{Environment.NewLine}        {{{Environment.NewLine}");

        foreach (var line in lines)
        {
            text.AppendLine("            " + line);
        }

        text.AppendLine("        }");
        text.AppendLine("    }");
        return text.ToString();
    }

    /// <summary>
    /// Translates one method of a struct.
    /// </summary>
    /// <param name="structure">The name of the struct.</param>
    /// <param name="method">The method to translate.</param>
    /// <returns>The method, as the shading language writes it.</returns>
    private static string StructureMethod(string structure, MethodDeclarationSyntax method)
    {
        var isStatic = method.Modifiers.Any(SyntaxKind.StaticKeyword);

        // A method that writes a field changes the value it was called on, which the language spells [mutating].
        var mutating = !isStatic && Mutates(method) ? "[mutating] " : string.Empty;
        var modifier = isStatic ? "static " : string.Empty;

        return $"    {mutating}{modifier}{method.ReturnType} {method.Identifier.ValueText}({Parameters(method.ParameterList)}){Body(method.Body, method.ExpressionBody, 4)}";
    }
    /// <summary>
    /// Reports whether a method writes a field of the value it was called on.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns><see langword="true"/> when the body assigns to a member.</returns>
    private static bool Mutates(MethodDeclarationSyntax method)
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
    /// Writes a body the way the shading language writes it, indented inside the struct.
    /// </summary>
    /// <param name="body">The block, when the member has one.</param>
    /// <param name="expression">The expression, when the member is written as one.</param>
    /// <param name="indent">The indentation of the opening brace, in spaces.</param>
    /// <returns>The block.</returns>
    private static string Body(BlockSyntax? body, ArrowExpressionClauseSyntax? expression, int indent)
    {
        var padding = new string(' ', indent);
        var text = new StringBuilder();
        text.AppendLine();
        text.AppendLine(padding + "{");

        if (expression is not null)
        {
            text.AppendLine(padding + "    return " + expression.Expression + ";");
        }
        else
        {
            foreach (var line in Indent(body!.Statements).Split('\n'))
            {
                text.AppendLine(padding + line);
            }
        }

        text.AppendLine(padding + "}");
        return text.ToString();
    }

    /// <summary>
    /// Writes a parameter list the way the shading language writes it.
    /// </summary>
    /// <param name="list">The parameters.</param>
    /// <returns>The parameters, separated by commas.</returns>
    private static string Parameters(ParameterListSyntax list) =>
        string.Join(", ", list.Parameters.Select(parameter => $"{Keyword(parameter.Modifiers.FirstOrDefault(token => token.IsKind(SyntaxKind.RefKeyword) || token.IsKind(SyntaxKind.OutKeyword) || token.IsKind(SyntaxKind.InKeyword)))}{parameter.Type} {parameter.Identifier.ValueText}"));

    /// <summary>
    /// Reads the keyword a parameter is passed with, as the shading language writes it.
    /// </summary>
    /// <param name="modifier">The modifier the source wrote, or <see langword="default"/> when it wrote none.</param>
    /// <returns>The keyword and its space, or an empty string.</returns>
    private static string Keyword(SyntaxToken modifier) => modifier.Kind() switch
    {
        SyntaxKind.RefKeyword => "inout ",
        SyntaxKind.OutKeyword => "out ",
        SyntaxKind.InKeyword => "in ",
        _ => string.Empty,
    };

    /// <param name="method">The kernel.</param>
    /// <param name="tree">The tree the kernel was parsed from.</param>
    /// <returns>The parameters, with the buffers numbered in the order they are declared.</returns>
    private static List<KernelParameter> ReadParameters(MethodDeclarationSyntax method, SyntaxTree tree)
    {
        RejectUnsupported(method, tree);

        var parameters = new List<KernelParameter>();

        foreach (var parameter in method.ParameterList.Parameters)
        {
            var name = parameter.Identifier.ValueText;
            var type = TypeName(parameter.Type);

            if (type == INDEX_TYPE || type == SPAN_INDEX_TYPE)
            {
                parameters.Add(new KernelParameter(name, KernelParameterKind.Index, string.Empty, -1));
                continue;
            }

            var element = (parameter.Type as GenericNameSyntax)?.TypeArgumentList.Arguments.FirstOrDefault()
                          ?? throw new KernelTranslationException($"Parameter '{name}': only an int index, ReadOnlySpan<T>, Span<T>, GpuIndex, GpuReadOnlyBuffer<T> or GpuBuffer<T> are supported.");

            if (!ELEMENT_TYPE.IsMatch(element.ToString()))
            {
                throw new KernelTranslationException($"Parameter '{name}': '{element}' is not an element type every backend has. Use int, uint or float, optionally with 2, 3 or 4 components.");
            }

            if (type == READ_ONLY_BUFFER_TYPE || type == READ_ONLY_SPAN_TYPE)
            {
                parameters.Add(new KernelParameter(name, KernelParameterKind.ReadOnlyBuffer, element.ToString(), parameters.Count(p => p.Kind != KernelParameterKind.Index)));
                continue;
            }

            if (type == WRITABLE_BUFFER_TYPE || type == SPAN_TYPE)
            {
                parameters.Add(new KernelParameter(name, KernelParameterKind.WritableBuffer, element.ToString(), parameters.Count(p => p.Kind != KernelParameterKind.Index)));
                continue;
            }

            throw new KernelTranslationException($"Parameter '{name}': only an int index, ReadOnlySpan<T>, Span<T>, GpuIndex, GpuReadOnlyBuffer<T> or GpuBuffer<T> are supported.");
        }

        if (!parameters.Any(parameter => parameter.Kind == KernelParameterKind.WritableBuffer))
        {
            throw new KernelTranslationException("The kernel writes nothing: a writable buffer parameter is required.");
        }

        if (!parameters.Any(parameter => parameter.Kind == KernelParameterKind.Index))
        {
            throw new KernelTranslationException("The kernel has no thread index parameter.");
        }

        return parameters;
    }

    /// <summary>
    /// Builds the locals that carry the buffer lengths, as the element counts a span reports on the host.
    /// </summary>
    /// <param name="parameters">The parameters of the kernel.</param>
    /// <returns>The declarations.</returns>
    private static string Prologue(IReadOnlyList<KernelParameter> parameters)
    {
        var prologue = new StringBuilder();
        var buffers = parameters.Where(parameter => parameter.Kind != KernelParameterKind.Index).ToList();

        for (var i = 0; i < buffers.Count; i++)
        {
            prologue.AppendLine($"    int __length_{buffers[i].Name} = int({LENGTHS_BUFFER}[{i}]);");
        }

        return prologue.ToString();
    }

    /// <summary>
    /// Carries the kernel body over, translating the constructs every target spells differently.
    /// </summary>
    /// <param name="method">The kernel.</param>
    /// <param name="parameters">The parameters of the kernel.</param>
    /// <param name="shared">Receives the declarations a shared array needs, which the prologue carries.</param>
    /// <param name="structures">The names of the structs the kernel declares, which are the types it may construct.</param>
    /// <returns>The body text.</returns>
    private static string RewriteBody(MethodDeclarationSyntax method, IReadOnlyList<KernelParameter> parameters, StringBuilder shared, IReadOnlyCollection<string> structures)
    {
        var rewriter = new SliceRewriter(parameters, structures, Callables(method.SyntaxTree), Signatures(method.SyntaxTree));
        var body = method.Body is null
            ? $"    {method.ExpressionBody!.Expression};"
            : Indent(((BlockSyntax)rewriter.Visit(method.Body)!).Statements);

        foreach (var parameter in parameters.Where(parameter => parameter.Kind != KernelParameterKind.Index))
        {
            body = System.Text.RegularExpressions.Regex.Replace(
                body,
                $@"\b{parameter.Name}\.Length\b",
                $"__length_{parameter.Name}");
        }

        foreach (var intrinsic in INTRINSICS)
        {
            var parts = intrinsic.Key.Split('.');
            var pattern = $@"(?<![\w.])(?:System\.)?{parts[0]}\.{parts[1]}\(";

            body = System.Text.RegularExpressions.Regex.Replace(body, pattern, $"{intrinsic.Value}(");
        }


        if (body.Contains(RANGE, StringComparison.Ordinal))
        {
            throw new KernelTranslationException("A range is read as an element or as a length: this kernel uses one where the device needs a value.");
        }

        if (body.Contains(SLICE_METHOD, StringComparison.Ordinal))
        {
            throw new KernelTranslationException("A span is read as an element or as a length; this kernel uses one where the device needs a value.");
        }

        // The call that makes a group wait for its own writes is the one synchronization the subset has, and every
        // backend has it under a different name.
        body = System.Text.RegularExpressions.Regex.Replace(body, $@"\b{BARRIER}\(\)", "GroupMemoryBarrierWithGroupSync()");
        body = System.Text.RegularExpressions.Regex.Replace(body, $@"(?:System\.Threading\.)?{INTERLOCKED.Replace(".", "\\.")}\(\s*ref\s+", "InterlockedAdd(");

        if (body.Contains("Interlocked", StringComparison.Ordinal) && !body.Contains("InterlockedAdd(", StringComparison.Ordinal))
        {
            throw new KernelTranslationException($"The subset maps '{INTERLOCKED}' and no other atomic operation.");
        }

        if (body.Contains(BARRIER + "(", StringComparison.Ordinal))
        {
            throw new KernelTranslationException("The barrier of a group takes no arguments.");
        }

        shared.Append(rewriter.Declarations());

        return body;
    }

    /// <summary>
    /// Rewrites a sliced span into a window over the buffer it came from, because no shading language has a span.
    /// </summary>
    /// <remarks>
    /// A slice becomes two locals the body reads instead: the offset the window starts at and, when the kernel named it,
    /// the count. Every element the body takes through the slice is then indexed off the buffer it came from, which keeps
    /// one translation describing all five backends.
    /// </remarks>
    private sealed class SliceRewriter : CSharpSyntaxRewriter
    {
        private readonly List<KernelParameter> m_Parameters;
        private readonly IReadOnlyCollection<string> m_Structures;
        private readonly IReadOnlyCollection<string> m_Callables;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<ParameterSyntax>> m_Signatures;
        private readonly Dictionary<string, SpanView> m_Views = new(StringComparer.Ordinal);
        private readonly Dictionary<string, SharedArray> m_Arrays = new(StringComparer.Ordinal);
        private readonly List<SharedArray> m_Shared = new();
        private int m_Loops;

        /// <param name="parameters">The parameters of the kernel, which are the buffers a slice can come from.</param>
        /// <param name="structures">The names of the structs the kernel declares.</param>
        /// <param name="callables">The names a body may call, whose arguments are written without a keyword.</param>
        /// <param name="signatures">The parameter types of those names, so that a call can declare what it writes.</param>
        internal SliceRewriter(
            IReadOnlyList<KernelParameter> parameters,
            IReadOnlyCollection<string> structures,
            IReadOnlyCollection<string>? callables = null,
            IReadOnlyDictionary<string, IReadOnlyList<ParameterSyntax>>? signatures = null)
        {
            m_Parameters = parameters.ToList();
            m_Structures = structures;
            m_Callables = callables ?? Array.Empty<string>();
            m_Signatures = signatures ?? new Dictionary<string, IReadOnlyList<ParameterSyntax>>(StringComparer.Ordinal);
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // A variable a call writes back to is declared before the call, with the type the call takes there.
            if (node.Expression is not InvocationExpressionSyntax invocation ||
                !invocation.ArgumentList.Arguments.Any(argument => argument.Expression is DeclarationExpressionSyntax))
            {
                return base.VisitExpressionStatement(node);
            }

            var name = invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => string.Empty,
            };

            if (!m_Signatures.TryGetValue(name, out var parameters))
            {
                throw new KernelTranslationException($"'{invocation.Expression}' is not a call the kernel declares, so the variables it writes back to have no type on the device.");
            }

            var declarations = new StringBuilder();
            var arguments = new List<string>();
            var index = 0;

            foreach (var argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is not DeclarationExpressionSyntax declaration ||
                    declaration.Designation is not SingleVariableDesignationSyntax designated)
                {
                    arguments.Add(argument.Expression.ToString());
                    index++;
                    continue;
                }

                if (index >= parameters.Count || parameters[index].Modifiers.All(modifier => !modifier.IsKind(SyntaxKind.OutKeyword) && !modifier.IsKind(SyntaxKind.RefKeyword)))
                {
                    throw new KernelTranslationException($"Argument {index + 1} of '{name}' declares a variable the call does not write back to.");
                }

                var type = declaration.Type?.ToString() ?? parameters[index].Type.ToString();

                if (type == "var")
                {
                    type = parameters[index].Type.ToString();
                }

                declarations.AppendLine($"{type} {designated.Identifier.ValueText};");
                arguments.Add(designated.Identifier.ValueText);
                index++;
            }

            declarations.AppendLine($"{invocation.Expression}({string.Join(", ", arguments)});");

            return SyntaxFactory.ParseStatement(declarations.ToString());
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            if (!TryResolveCollection(node.Expression, out var collection, out var type))
            {
                throw new KernelTranslationException($"'{node.Expression}' is not a buffer, a slice of one or a local array, which are the collections a kernel can walk.");
            }

            var body = (BlockSyntax)Visit(node.Statement)!;
            var loop = $"__gubbinsLoop{m_Loops++}";
            var run = new StringBuilder();

            run.AppendLine($"for (int {loop} = 0; {loop} < {collection.Length}; {loop}++)");
            run.AppendLine("{");
            run.AppendLine($"    {type} {node.Identifier.ValueText} = {collection.Element(loop)};");

            foreach (var line in Indent(body.Statements).Split('\n'))
            {
                run.AppendLine("    " + line);
            }

            run.AppendLine("}");

            return SyntaxFactory.ParseStatement(run.ToString());
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitSwitchExpression(SwitchExpressionSyntax node)
        {
            var subject = node.GoverningExpression.ToString();
            var arms = ((SwitchExpressionSyntax)base.VisitSwitchExpression(node)!).Arms;
            var fallback = arms.FirstOrDefault(arm => arm.Pattern is DiscardPatternSyntax);

            if (fallback is null)
            {
                throw new KernelTranslationException("A switch expression ends with a discard ('_') arm: the device has no exception to throw when nothing matches.");
            }

            var text = $"({fallback.Expression})";

            foreach (var arm in arms.Reverse())
            {
                if (arm == fallback)
                {
                    continue;
                }

                if (arm.Pattern is not ConstantPatternSyntax pattern || arm.WhenClause is not null)
                {
                    throw new KernelTranslationException($"A switch expression of a kernel compares with constants: '{arm.Pattern}' is not one.");
                }

                text = $"(({subject}) == ({pattern.Expression}) ? ({arm.Expression}) : {text})";
            }

            return SyntaxFactory.ParseExpression(text);
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitIsPatternExpression(IsPatternExpressionSyntax node)
        {
            var subject = node.Expression.ToString();

            if (node.Pattern is ConstantPatternSyntax { Expression: LiteralExpressionSyntax literal } && literal.IsKind(SyntaxKind.NullLiteralExpression))
            {
                throw new KernelTranslationException("A kernel has no nulls to test for: the buffers it is given are always there.");
            }

            if (node.Pattern is ConstantPatternSyntax constant)
            {
                return SyntaxFactory.ParseExpression($"(({subject}) == ({constant.Expression}))");
            }

            if (node.Pattern is UnaryPatternSyntax negation && negation.Pattern is ConstantPatternSyntax different)
            {
                return SyntaxFactory.ParseExpression($"(({subject}) != ({different.Expression}))");
            }

            throw new KernelTranslationException($"A kernel tests values with ==, != or a switch over constants: '{node.Pattern}' is not one.");
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
            var name = visited.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                _ => string.Empty,
            };

            if (!m_Callables.Contains(name) || visited.ArgumentList.Arguments.All(argument => argument.RefKindKeyword.IsKind(SyntaxKind.None)))
            {
                return visited;
            }

            var arguments = new List<string>();

            foreach (var argument in visited.ArgumentList.Arguments)
            {
                arguments.Add(argument.Expression.ToString());
            }

            return SyntaxFactory.ParseExpression($"{visited.Expression}({string.Join(", ", arguments)})");
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var type = TypeName(node.Type);

            if (!m_Structures.Contains(type) || node.Initializer is not null)
            {
                throw new KernelTranslationException($"'{node.Type}' is not a struct the kernel declares, so a value of it cannot be built on the device.");
            }

            if (node.ArgumentList is null || node.ArgumentList.Arguments.Count == 0)
            {
                throw new KernelTranslationException($"A value of '{node.Type}' with no arguments is declared as a local, not built inside an expression.");
            }

            var arguments = string.Join(", ", node.ArgumentList.Arguments.Select(argument => argument.Expression.ToString()));

            return SyntaxFactory.ParseExpression($"{node.Type}({arguments})");
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Declaration.Variables.Count == 1 && LocalArray(node, node.Declaration.Variables[0].Identifier.ValueText) is { } array)
            {
                if (ArrayOf(node, node.Declaration.Variables[0].Identifier.ValueText) is { } known)
                {
                    m_Arrays[known.Name] = known;
                }

                return array;
            }

            if (node.Declaration.Variables.Count == 1 &&
                node.Declaration.Variables[0].Initializer?.Value is ObjectCreationExpressionSyntax created &&
                created.Initializer is null &&
                (created.ArgumentList is null || created.ArgumentList.Arguments.Count == 0) &&
                m_Structures.Contains(TypeName(created.Type)))
            {
                return SyntaxFactory.ParseStatement($"{created.Type} {node.Declaration.Variables[0].Identifier.ValueText} = {{}};");
            }

            // A span kept in a local is the buffer and the two numbers the body reads it through.
            if (node.Declaration.Variables.Count == 1 &&
                node.Declaration.Variables[0].Initializer?.Value is { } value &&
                value is not IdentifierNameSyntax &&
                TryResolve(value, out var kept))
            {
                m_Views[node.Declaration.Variables[0].Identifier.ValueText] = kept;
                return null;
            }

            if (node.Declaration.Variables.Count != 1 || node.Declaration.Variables[0].Initializer?.Value is not InvocationExpressionSyntax invocation)
            {
                return base.VisitLocalDeclarationStatement(node);
            }

            var name = node.Declaration.Variables[0].Identifier.ValueText;

            if (IsShared(invocation, out var element, out var count))
            {
                m_Shared.Add(new SharedArray(name, element, count));
                return null;
            }

            if (TryResolve(invocation, out var view))
            {
                m_Views[name] = view;
                return null;
            }

            return base.VisitLocalDeclarationStatement(node);
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            var visited = (ElementAccessExpressionSyntax)base.VisitElementAccessExpression(node)!;

            if (visited.ArgumentList.Arguments.Count != 1 || !TryResolve(visited.Expression, out var view))
            {
                return visited;
            }

            var index = visited.ArgumentList.Arguments[0].Expression.ToString();

            if (visited.ArgumentList.Arguments[0].Expression is PrefixUnaryExpressionSyntax behind)
            {
                var length = view.Count ?? (view.Offset == ZERO
                    ? $"__length_{view.Buffer}"
                    : $"(__length_{view.Buffer} - ({view.Offset}))");

                var indexFromTheEnd = $"({length}) - ({behind.Operand})";

                return SyntaxFactory.ParseExpression(view.Offset == ZERO
                    ? $"{view.Buffer}[{indexFromTheEnd}]"
                    : $"{view.Buffer}[({indexFromTheEnd}) + ({view.Offset})]");
            }

            return SyntaxFactory.ParseExpression(view.Offset == ZERO
                ? $"{view.Buffer}[{index}]"
                : $"{view.Buffer}[({index}) + ({view.Offset})]");
        }


        /// <inheritdoc/>
        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name.Identifier.ValueText == "Length")
            {
                if (node.Expression is IdentifierNameSyntax identifier)
                {
                    var array = m_Shared.FirstOrDefault(candidate => candidate.Name == identifier.Identifier.ValueText);

                    if (array is not null)
                    {
                        return SyntaxFactory.ParseExpression(array.Count);
                    }
                }

                if (TryResolve(node.Expression, out var view))
                {
                    if (view.Count is not null)
                    {
                        return SyntaxFactory.ParseExpression($"({view.Count})");
                    }

                    return SyntaxFactory.ParseExpression(view.Offset == ZERO
                        ? $"__length_{view.Buffer}"
                        : $"(__length_{view.Buffer} - ({view.Offset}))");
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        /// <inheritdoc/>
        public override SyntaxNode? VisitMemberBindingExpression(MemberBindingExpressionSyntax node) => base.VisitMemberBindingExpression(node);

        /// <summary>
        /// Adds two offsets, leaving out the one that is zero so that a span of a whole buffer keeps its own spelling.
        /// </summary>
        /// <param name="left">The offset the span started at.</param>
        /// <param name="right">The offset the slice adds to it.</param>
        /// <returns>The expression for the sum.</returns>
        private static string Combine(string left, string right)
        {
            if (right == ZERO)
            {
                return left;
            }

            return left == ZERO ? right : $"({left}) + ({right})";
        }

        /// <summary>
        /// Rewrites a local array the way the shading language declares one: a size in the brackets and an initializer of
        /// elements, with no allocation behind either.
        /// </summary>
        /// <param name="node">The declaration the kernel wrote.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The statement to emit, or <see langword="null"/> when the declaration is not an array.</returns>
        /// <exception cref="KernelTranslationException">Thrown when the size is not a literal or the type is missing.</exception>
        private static StatementSyntax? LocalArray(LocalDeclarationStatementSyntax node, string name)
        {
            var initializer = node.Declaration.Variables[0].Initializer?.Value;
            string type;
            string size;
            string? elements;

            switch (initializer)
            {
                case ImplicitArrayCreationExpressionSyntax withoutType:
                    type = (node.Declaration.Type as ArrayTypeSyntax)?.ElementType.ToString()
                           ?? throw new KernelTranslationException("A local array written without a type takes the element type of the variable that holds it.");
                    size = withoutType.Initializer.Expressions.Count.ToString();
                    elements = string.Join(", ", withoutType.Initializer.Expressions);
                    break;

                case ArrayCreationExpressionSyntax creation:
                    type = creation.Type.ElementType.ToString();

                    if (creation.Type.RankSpecifiers.FirstOrDefault()?.Sizes.FirstOrDefault() is not { } length ||
                        !int.TryParse(length.ToString(), out var parsed) || parsed <= 0)
                    {
                        throw new KernelTranslationException("A local array takes its size as a literal: the device sizes it at compile time.");
                    }

                    size = parsed.ToString();
                    elements = creation.Initializer is null ? null : string.Join(", ", creation.Initializer.Expressions);
                    break;

                default:
                    return null;
            }

            var text = elements is null
                ? $"{type} {name}[{size}];"
                : $"{type} {name}[{size}] = {{ {elements} }};";

            return SyntaxFactory.ParseStatement(text);
        }
        /// <summary>
        /// Resolves a collection a loop can walk: a buffer, a slice of one, or a local array.
        /// </summary>
        /// <param name="expression">The expression the loop names.</param>
        /// <param name="collection">When the method returns, how to read it.</param>
        /// <param name="type">When the method returns, the type of its elements.</param>
        /// <returns><see langword="true"/> when the expression names a collection.</returns>
        private bool TryResolveCollection(ExpressionSyntax expression, out Walkable collection, out string type)
        {
            collection = default!;
            type = string.Empty;

            if (TryResolve(expression, out var view))
            {
                var parameter = m_Parameters.FirstOrDefault(candidate => candidate.Name == view.Buffer);

                if (parameter?.ElementType is null)
                {
                    return false;
                }

                var length = view.Count ?? (view.Offset == ZERO
                    ? $"__length_{view.Buffer}"
                    : $"(__length_{view.Buffer} - ({view.Offset}))");

                collection = new Walkable(view.Buffer, view.Offset, length);
                type = parameter.ElementType;
                return true;
            }

            if (expression is IdentifierNameSyntax identifier && m_Arrays.TryGetValue(identifier.Identifier.ValueText, out var array))
            {
                collection = new Walkable(array.Name, ZERO, array.Count);
                type = array.Element;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads the type and size of a local array declaration.
        /// </summary>
        /// <param name="node">The declaration.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The array, or <see langword="null"/> when the declaration is not an array.</returns>
        internal static SharedArray? ArrayOf(LocalDeclarationStatementSyntax node, string name)
        {
            var initializer = node.Declaration.Variables[0].Initializer?.Value;

            return initializer switch
            {
                ImplicitArrayCreationExpressionSyntax withoutType when node.Declaration.Type is ArrayTypeSyntax declared =>
                    new SharedArray(name, declared.ElementType.ToString(), withoutType.Initializer.Expressions.Count.ToString()),
                ArrayCreationExpressionSyntax creation when creation.Type.RankSpecifiers.FirstOrDefault()?.Sizes.FirstOrDefault() is { } size =>
                    new SharedArray(name, creation.Type.ElementType.ToString(), size.ToString()),
                _ => null,
            };
        }

        /// <summary>
        /// Builds the declarations the rewritten body reads.
        /// </summary>
        /// <returns>The declarations, one per shared array.</returns>
        internal string Declarations()
        {
            var declarations = new StringBuilder();

            foreach (var array in m_Shared)
            {
                declarations.AppendLine($"groupshared {array.Element} {array.Name}[{array.Count}];");
            }

            return declarations.ToString();
        }

        /// <summary>
        /// Reports whether a call declares an array every thread of the group addresses.
        /// </summary>
        /// <param name="invocation">The call.</param>
        /// <param name="element">When the method returns, the type of the elements.</param>
        /// <param name="count">When the method returns, the number of elements.</param>
        /// <returns><see langword="true"/> when the call declares a shared array.</returns>
        private static bool IsShared(InvocationExpressionSyntax invocation, out string element, out string count)
        {
            element = string.Empty;
            count = string.Empty;

            if (invocation.Expression is not GenericNameSyntax generic ||
                generic.Identifier.ValueText != SHARED ||
                generic.TypeArgumentList.Arguments.Count != 1 ||
                invocation.ArgumentList.Arguments.Count != 1)
            {
                return false;
            }

            count = invocation.ArgumentList.Arguments[0].Expression.ToString();

            if (!int.TryParse(count, out _))
            {
                throw new KernelTranslationException($"'{SHARED}<{generic.TypeArgumentList.Arguments[0]}>' takes the number of elements as a literal, which is what the device sizes the array with.");
            }

            element = generic.TypeArgumentList.Arguments[0].ToString();
            return true;
        }

        /// <summary>
        /// Resolves a span expression into the buffer it is a window on and the two numbers the device works with.
        /// </summary>
        /// <param name="expression">The expression to resolve.</param>
        /// <param name="view">When the method returns, the span it stands for.</param>
        /// <returns><see langword="true"/> when the expression names a buffer or a span of one.</returns>
        private bool TryResolve(ExpressionSyntax expression, out SpanView view)
        {
            view = default!;

            if (expression is IdentifierNameSyntax identifier)
            {
                var name = identifier.Identifier.ValueText;

                if (m_Views.TryGetValue(name, out var known))
                {
                    view = known;
                    return true;
                }

                if (m_Parameters.Any(parameter => parameter.Kind != KernelParameterKind.Index && parameter.Name == name))
                {
                    view = new SpanView(name, ZERO, null);
                    return true;
                }

                return false;
            }

            if (expression is ElementAccessExpressionSyntax ranged &&
                ranged.ArgumentList.Arguments.Count == 1 &&
                ranged.ArgumentList.Arguments[0].Expression is RangeExpressionSyntax range &&
                TryResolve(ranged.Expression, out var source))
            {
                var from = range.LeftOperand is null ? ZERO : range.LeftOperand.ToString();
                var window = Combine(source.Offset, from);
                var length = range.RightOperand is null
                    ? source.Count is null ? null : $"(({source.Count}) - ({from}))"
                    : $"(({range.RightOperand}) - ({from}))";

                view = new SpanView(source.Buffer, window, length);
                return true;
            }

            if (expression is not InvocationExpressionSyntax invocation ||
                invocation.Expression is not MemberAccessExpressionSyntax access ||
                access.Name.Identifier.ValueText != SLICE)
            {
                return false;
            }

            var arguments = invocation.ArgumentList.Arguments;

            if (arguments.Count is not (1 or 2))
            {
                throw new KernelTranslationException($"'{SLICE}' takes a start and an optional count; {arguments.Count} arguments were given.");
            }

            if (!TryResolve(access.Expression, out var inner))
            {
                throw new KernelTranslationException($"'{access.Expression}' is not one of the kernel's buffers, so a slice of it has no representation on the device.");
            }

            var start = arguments[0].Expression.ToString();
            var offset = Combine(inner.Offset, start);
            var count = arguments.Count == 2
                ? arguments[1].Expression.ToString()
                : inner.Count is null ? null : $"(({inner.Count}) - ({start}))";

            view = new SpanView(inner.Buffer, offset, count);
            return true;
        }
    }

    /// <summary>
    /// A collection a loop can walk, as the buffer and the two numbers the device reads it through.
    /// </summary>
    /// <param name="Buffer">The buffer or array the collection is.</param>
    /// <param name="Offset">The expression the collection starts at.</param>
    /// <param name="Length">The expression the collection is long.</param>
    private sealed record Walkable(string Buffer, string Offset, string Length)
    {
        /// <summary>
        /// Reads one element of the collection.
        /// </summary>
        /// <param name="index">The expression of the index.</param>
        /// <returns>The expression of the element.</returns>
        internal string Element(string index) =>
            Offset == ZERO ? $"{Buffer}[{index}]" : $"{Buffer}[({index}) + ({Offset})]";
    }

    /// <summary>
    /// One array every thread of the group addresses, as the declaration the kernel carries.
    /// </summary>
    /// <param name="Name">The name the kernel gave the array.</param>
    /// <param name="Element">The type of its elements.</param>
    /// <param name="Count">The number of elements it holds.</param>
    private sealed record SharedArray(string Name, string Element, string Count);

    /// <summary>
    /// One span of a buffer, as the buffer and the two numbers the device works with instead.
    /// </summary>
    /// <param name="Buffer">The buffer the span is a window on.</param>
    /// <param name="Offset">The expression the window starts at, measured in elements from the buffer.</param>
    /// <param name="Count">
    /// The expression the window is long, or <see langword="null"/> when it runs to the end of the buffer.
    /// </param>
    /// <remarks>
    /// A span has no representation on the device, but it never needs one: the subset cannot pass a span to a function,
    /// return one, or keep one anywhere but a local, so every use of one is either an element or a length, and both are the
    /// buffer with the two numbers folded in.
    /// </remarks>
    private sealed record SpanView(string Buffer, string Offset, string? Count);

    /// <summary>
    /// Reads the name a call is made through, without its namespace.
    /// </summary>
    /// <param name="expression">The expression the call is made through.</param>
    /// <returns>The name, as the intrinsics table spells it.</returns>
    private static string CallName(ExpressionSyntax expression)
    {
        var name = expression.ToString();

        return name.StartsWith("System.", StringComparison.Ordinal) ? name.Substring("System.".Length) : name;
    }

    /// <summary>
    /// Refuses a construct the device cannot express.
    /// </summary>
    /// <typeparam name="T">The kind of syntax the subset does not cover.</typeparam>
    /// <param name="method">The kernel.</param>
    /// <param name="message">What to say about it.</param>
    /// <exception cref="KernelTranslationException">Thrown when the kernel contains it.</exception>
    private static void Reject<T>(MethodDeclarationSyntax method, string message)
        where T : SyntaxNode
    {
        if (method.DescendantNodes().OfType<T>().Any())
        {
            throw new KernelTranslationException(message);
        }
    }

    /// <summary>
    /// Indents a statement list four spaces and normalizes its line endings.
    /// </summary>
    /// <param name="statements">The statements of a kernel body.</param>
    /// <returns>The body text.</returns>
    private static string Indent(SyntaxList<StatementSyntax> statements)
    {
        var lines = new List<string>();

        foreach (var statement in statements)
        {
            lines.AddRange(statement.ToFullString().Replace("\r\n", "\n").Split('\n'));
        }

        while (lines.Count > 0 && lines[0].Trim().Length == 0)
        {
            lines.RemoveAt(0);
        }

        while (lines.Count > 0 && lines[lines.Count - 1].Trim().Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        var common = lines
            .Where(line => line.Trim().Length != 0)
            .Select(line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();

        var text = new StringBuilder();

        foreach (var line in lines)
        {
            text.AppendLine(line.Length == 0 ? string.Empty : "    " + line.Substring(Math.Min(common, line.Length)));
        }

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>
    /// Refuses the constructs the subset does not cover, so that an unsupported kernel fails at the source line instead of
    /// turning into Slang that means something else.
    /// </summary>
    /// <param name="method">The kernel.</param>
    /// <param name="tree">The tree the kernel was parsed from.</param>
    private static void RejectUnsupported(MethodDeclarationSyntax method, SyntaxTree tree)
    {
        // Constructs whose meaning the device does not have, or has under different rules. Each one is refused here rather
        // than carried into the program the compiler would reject with a message about the generated text.

        Reject<InterpolatedStringExpressionSyntax>(method, "The device has no strings.");
        Reject<CheckedStatementSyntax>(method, "Arithmetic on the device does not trap, so checked has no meaning there.");
        Reject<TryStatementSyntax>(method, "The device has no exceptions.");
        Reject<ThrowStatementSyntax>(method, "The device has no exceptions.");
        Reject<UsingStatementSyntax>(method, "A kernel has nothing to release: the memory it works on belongs to the caller.");
        Reject<LockStatementSyntax>(method, "A kernel has no lock: threads of a group synchronize with Barrier().");
        Reject<AwaitExpressionSyntax>(method, "A kernel body is one straight pass; there is nothing to await.");
        Reject<YieldStatementSyntax>(method, "A kernel body is one straight pass; there is nothing to yield.");
        Reject<GotoStatementSyntax>(method, "A kernel body has no goto.");
        Reject<LabeledStatementSyntax>(method, "A kernel body has no goto, so it has no labels.");
        Reject<TypeOfExpressionSyntax>(method, "A kernel body cannot ask for a type: there are no types to reflect on.");
        Reject<SizeOfExpressionSyntax>(method, "A kernel body cannot take the size of a type.");
        Reject<AnonymousObjectCreationExpressionSyntax>(method, "A kernel cannot create an object.");
        Reject<UnsafeStatementSyntax>(method, "A kernel body cannot open an unsafe block: it already addresses the memory it was given.");
        Reject<DefaultExpressionSyntax>(method, "A kernel body writes the value it means: 'default' has no meaning on the device.");
        Reject<TupleExpressionSyntax>(method, "A kernel body has no tuples.");
        Reject<StackAllocArrayCreationExpressionSyntax>(method, "A kernel cannot take memory from the stack: the shared memory of a group is 'Shared<T>(count)'.");
        Reject<PointerTypeSyntax>(method, "A kernel body does not declare pointers: a buffer is a span the caller passes.");

        Reject<QueryExpressionSyntax>(method, "A kernel body has no queries.");
        Reject<ConditionalAccessExpressionSyntax>(method, "A kernel body has no null conditional access: the buffers it is given are always there.");
        Reject<ImplicitObjectCreationExpressionSyntax>(method, "A kernel cannot create an object.");
        Reject<LocalFunctionStatementSyntax>(method, "A kernel body is one straight pass; a local function has no representation on the device.");
        Reject<AnonymousMethodExpressionSyntax>(method, "A kernel body cannot contain an anonymous method.");

        if (method.DescendantNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault(binary => binary.IsKind(SyntaxKind.AsExpression)) is { } reference)
        {
            throw new KernelTranslationException($"'{reference}' casts between references: a kernel body has no objects to cast.");
        }

        foreach (var cast in method.DescendantNodes().OfType<CastExpressionSyntax>())
        {
            if (!ELEMENT_TYPE.IsMatch(TypeName(cast.Type) is { Length: > 0 } name ? name : cast.Type.ToString()))
            {
                throw new KernelTranslationException($"The cast to '{cast.Type}' has no equivalent on every backend. Cast to int, uint or float instead.");
            }
        }

        foreach (var literal in method.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralExpression) || literal.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CharacterLiteralExpression))
            {
                throw new KernelTranslationException("The device has no strings or characters.");
            }
        }

        // A method a struct of the kernel declares is a call the device has, as long as it is called on a value of that
        // struct, which the language checks.
        var methods = tree
            .GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<StructDeclarationSyntax>()
            .SelectMany(structure => structure.Members.OfType<MethodDeclarationSyntax>())
            .Select(method => method.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

        var functions = Functions(tree);

        foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (INTRINSICS.ContainsKey(CallName(invocation.Expression)))
            {
                continue;
            }

            if (invocation.Expression is MemberAccessExpressionSyntax member && methods.Contains(member.Name.Identifier.ValueText))
            {
                continue;
            }

            if (functions.Contains(CallName(invocation.Expression)))
            {
                continue;
            }

            if (invocation.Expression is MemberAccessExpressionSyntax access && access.Name.Identifier.ValueText == SLICE)
            {
                continue;
            }

            if (CallName(invocation.Expression) is BARRIER ||
                (invocation.Expression is GenericNameSyntax generic && generic.Identifier.ValueText == SHARED))
            {
                continue;
            }

            if (CallName(invocation.Expression) == INTERLOCKED)
            {
                if (invocation.Parent is not ExpressionStatementSyntax)
                {
                    throw new KernelTranslationException($"'{INTERLOCKED}' is called as a statement: the value an atomic add returns is not part of the subset.");
                }

                continue;
            }

            throw new KernelTranslationException($"A kernel body supports operators, indexing, control flow and the maths calls the subset maps; the call to '{invocation.Expression}' is not supported.");
        }

        foreach (var lambda in method.DescendantNodes().OfType<LambdaExpressionSyntax>())
        {
            throw new KernelTranslationException($"A kernel body cannot contain a lambda ('{lambda}'): it has no representation on the device.");
        }

        _ = tree;
    }
}

/// <summary>
/// Thrown when a kernel uses something the supported subset does not cover.
/// </summary>
public sealed class KernelTranslationException : Exception
{
    /// <param name="message">The message that describes the failure.</param>
    public KernelTranslationException(string message) : base(message) { }
}