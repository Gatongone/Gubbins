using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// Turns the supported subset of C# into the model every backend of this layer emits from.
/// </summary>
/// <remarks>
/// <para>
/// The subset is a static method whose first parameter is the thread index followed by buffers. The translation happens in
/// C#: the kernel, the helpers it calls and the structures it works with are lowered into <see cref="KernelProgram"/>,
/// and the emitters of this layer write the language each backend loads from it. Nothing is handed to a shading language
/// compiler, so the pipeline has no tool to install beyond the platform's own.
/// </para>
/// <para>
/// Two things are not written the way they are used. A buffer's <c>Length</c> is not readable from the device on every
/// backend, so the caller passes the element counts and the body reads them through the block that carries them. A call
/// to one of the maths functions the table maps becomes the operation with that meaning, which is the whole reason the
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
/// Anything outside the subset is refused here rather than emitted and rejected later by a backend, with the construct
/// named in the message: an object, an array, a string, <c>checked</c>, an exception, a lock, a lambda, a cast to a type
/// the device does not name, an element type no D3D12 profile has. This is a translator for a deliberately small language,
/// not a compiler.
/// </para>
/// </remarks>
public static partial class KernelTranspiler
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
    /// The name of the buffer the structure a kernel takes as one value is passed in.
    /// </summary>
    public const string PARAMETERS_BUFFER = "__gubbinsParams";

    /// <summary>
    /// The name the kernel is compiled under, which is the entry point every backend looks the kernel up by.
    /// </summary>
    public const string ENTRY_POINT = "gubbins_main";

    /// <summary>
    /// The method a kernel slices a span with.
    /// </summary>
    public const string SLICE = "Slice";

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
    /// The element types a buffer can hold, which are the ones every profile of D3D12 has.
    /// </summary>
    /// <remarks>
    /// A narrower type is not a portability problem this layer can paper over: D3D12 has no eight or sixteen bit type
    /// without a compiler switch, while the other backends do, so a buffer of one of them would work everywhere but there.
    /// </remarks>
    private static readonly System.Text.RegularExpressions.Regex ELEMENT_TYPE =
        new(@"^(int|uint|float)([234])?$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// Translates one C# kernel to the model every backend reads.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <param name="entryName">The kernel to translate, or <see langword="null"/> to take the only one there is.</param>
    /// <returns>The kernel, with the bindings the backends expect.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the source is outside the supported subset.</exception>
    /// <remarks>
    /// One program serves all five backends, because the model describes what the kernel computes and not how a language
    /// spells it: each backend writes the artifact it loads out of the same translation.
    /// </remarks>
    public static KernelProgram Transpile(string source, string? entryName = null)
    {
        var (method, tree) = Locate(source, entryName);

        return KernelLowering.Build(method, tree, Threads(method), null);
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
    internal static bool IsKernel(MethodDeclarationSyntax method)
    {
        if (!method.Modifiers.Any(SyntaxKind.StaticKeyword) || method.ParameterList.Parameters.Count == 0)
        {
            return false;
        }

        var type = TypeName(method.ParameterList.Parameters[0].Type);

        return type == INDEX_TYPE || type == SPAN_INDEX_TYPE;
    }

    /// <summary>
    /// Reads the type name a declaration writes, without its namespace.
    /// </summary>
    /// <param name="type">The declared type.</param>
    /// <returns>The type name, or an empty string when there is none.</returns>
    internal static string TypeName(TypeSyntax? type) => type switch
    {
        null => string.Empty,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        ArrayTypeSyntax array => TypeName(array.ElementType) + "[]",
        _ => type.ToString()
    };

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
    /// Refuses the constructs the subset does not cover, so that an unsupported kernel fails at the source line instead of
    /// turning into a program that means something else.
    /// </summary>
    /// <param name="method">The kernel or helper.</param>
    /// <param name="tree">The tree it was parsed from.</param>
    /// <param name="semantic">Whether the compiler resolved the calls, which is checked by symbol instead of by name.</param>
    /// <exception cref="KernelTranslationException">Thrown when the body contains one.</exception>
    internal static void RejectUnsupported(MethodDeclarationSyntax method, SyntaxTree tree, bool semantic = false)
    {
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

        if (method.DescendantNodes().OfType<BinaryExpressionSyntax>().FirstOrDefault(binary => binary.IsKind(SyntaxKind.AsExpression) || binary.IsKind(SyntaxKind.IsExpression)) is { } reference)
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
            if (literal.IsKind(SyntaxKind.StringLiteralExpression) || literal.IsKind(SyntaxKind.CharacterLiteralExpression))
            {
                throw new KernelTranslationException("The device has no strings or characters.");
            }
        }

        // A source text has nothing but its names to go on, so the calls it makes are the ones the file declares. A kernel
        // the compiler resolved is checked by the symbol each call resolves to, which is what lets a helper come from
        // another file and what refuses a call to something the device cannot run.
        if (!semantic)
        {
            var methods = tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<StructDeclarationSyntax>()
                .SelectMany(structure => structure.Members.OfType<MethodDeclarationSyntax>())
                .Select(method => method.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);

            var functions = tree
                .GetCompilationUnitRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(candidate => !IsKernel(candidate))
                .Select(candidate => candidate.Identifier.ValueText)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var name = CallName(invocation.Expression);
                var qualified = Qualified(invocation.Expression);

                if (Intrinsic(qualified) || name == SLICE)
                {
                    continue;
                }

                if (invocation.Expression is MemberAccessExpressionSyntax member && methods.Contains(member.Name.Identifier.ValueText))
                {
                    continue;
                }

                if (functions.Contains(name))
                {
                    continue;
                }

                if (qualified is BARRIER || name is BARRIER || (invocation.Expression is GenericNameSyntax generic && generic.Identifier.ValueText == SHARED))
                {
                    continue;
                }

                if (qualified == INTERLOCKED)
                {
                    if (invocation.Parent is not ExpressionStatementSyntax)
                    {
                        throw new KernelTranslationException($"'{INTERLOCKED}' is called as a statement: the value an atomic add returns is not part of the subset.");
                    }

                    continue;
                }

                throw new KernelTranslationException($"A kernel body supports operators, indexing, control flow and the maths calls the subset maps; the call to '{invocation.Expression}' is not supported.");
            }
        }

        foreach (var lambda in method.DescendantNodes().OfType<LambdaExpressionSyntax>())
        {
            throw new KernelTranslationException($"A kernel body cannot contain a lambda ('{lambda}'): it has no representation on the device.");
        }
    }

    /// <summary>
    /// Reports whether a name is one of the intrinsics the subset maps.
    /// </summary>
    /// <param name="name">The name a call is made through.</param>
    /// <returns><see langword="true"/> when the device has the operation.</returns>
    private static bool Intrinsic(string name) => name.StartsWith("Math.", StringComparison.Ordinal) ||
                                                  name.StartsWith("MathF.", StringComparison.Ordinal) ||
                                                  name.StartsWith("GpuWave.", StringComparison.Ordinal) ||
                                                  name.StartsWith("System.Math.", StringComparison.Ordinal) ||
                                                  name.StartsWith("System.MathF.", StringComparison.Ordinal) ||
                                                  name.StartsWith("System.Threading.Interlocked", StringComparison.Ordinal);

    /// <summary>
    /// Reads the name a call is made through, without its namespace.
    /// </summary>
    /// <param name="expression">The expression the call is made through.</param>
    /// <returns>The name, as the intrinsics table spells it.</returns>
    private static string Qualified(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => Qualified(access.Expression) + "." + access.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => expression.ToString()
    };

    private static string CallName(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
        GenericNameSyntax generic => generic.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => expression.ToString()
    };

    /// <summary>
    /// Refuses a construct the device cannot express.
    /// </summary>
    /// <typeparam name="T">The kind of syntax the subset does not cover.</typeparam>
    /// <param name="method">The kernel or helper.</param>
    /// <param name="message">What to say about it.</param>
    /// <exception cref="KernelTranslationException">Thrown when the body contains it.</exception>
    private static void Reject<T>(MethodDeclarationSyntax method, string message)
        where T : SyntaxNode
    {
        if (method.DescendantNodes().OfType<T>().Any())
        {
            throw new KernelTranslationException(message);
        }
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
