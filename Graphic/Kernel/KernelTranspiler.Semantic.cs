using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Graphic.Kernels;

/// <summary>
/// The half of the translator that reads a kernel the compiler has resolved, rather than a text that happens to parse as
/// one.
/// </summary>
/// <remarks>
/// <para>
/// A kernel is ordinary C#, so the compiler has already decided what every name in it means: which <c>Span&lt;T&gt;</c> a
/// parameter is, which method a call reaches, which constant a field holds, and which structure a value is built from. The
/// translator reads those answers from a <see cref="Compilation"/> instead of guessing from the spelling, which is what
/// lets a kernel call a helper declared in another file and hold a structure the central processor also uses.
/// </para>
/// <para>
/// The boundary is the same as it has always been, moved from names to symbols: a helper is followed only when it carries
/// <c>[GpuCompatible]</c>, a structure is emitted only when it carries <c>[GpuStruct]</c>, and a call that reaches
/// anything else is refused with the name the compiler resolved. A helper has to be declared in source the compilation
/// contains, because the translator reads its body and a referenced assembly only has its signature.
/// </para>
/// </remarks>
public static partial class KernelTranspiler
{
    /// <summary>
    /// Translates one kernel the compiler resolved.
    /// </summary>
    /// <param name="compilation">The compilation the kernel was resolved in, which is what the helper calls are read from.</param>
    /// <param name="kernel">The method marked <c>[GpuKernel]</c>.</param>
    /// <returns>The kernel, with the bindings the backends expect.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the kernel, a helper or a structure is outside the subset.</exception>
    public static KernelProgram Transpile(Compilation compilation, IMethodSymbol kernel)
    {
        if (compilation is null)
        {
            throw new ArgumentNullException(nameof(compilation));
        }

        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        var semantics = KernelSemantics.Create(compilation, kernel);

        return KernelLowering.Build(semantics.Declaration, semantics.Declaration.SyntaxTree, semantics.Threads, semantics);
    }

    /// <summary>
    /// Reads the group size of a kernel the compiler resolved.
    /// </summary>
    /// <param name="kernel">The method marked <c>[GpuKernel]</c>.</param>
    /// <returns>The number of threads in a group, which is what a dispatch divides its element count by.</returns>
    /// <exception cref="KernelTranslationException">Thrown when the attribute does not declare a size a device can run.</exception>
    public static int ThreadsPerGroup(IMethodSymbol kernel)
    {
        if (kernel is null)
        {
            throw new ArgumentNullException(nameof(kernel));
        }

        var threads = THREADS_PER_GROUP;

        foreach (var attribute in kernel.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "Gubbins.Graphic.GpuThreadsAttribute")
            {
                continue;
            }

            if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not int count || count < 1 || count > 1024)
            {
                throw new KernelTranslationException("[GpuThreads(count)] takes the number of threads in a group as a literal a device can run.");
            }

            threads = count;
        }

        return threads;
    }

    /// <summary>
    /// Everything a kernel the compiler resolved reaches: its parameters, the helpers marked for the device, and the
    /// structures both processors use.
    /// </summary>
    /// <remarks>
    /// The discovery is a walk of the symbols the compiler resolved, starting at the kernel: every call inside the kernel
    /// and inside each helper it reaches is looked at once, and a call either maps to an intrinsic, belongs to a
    /// <c>[GpuStruct]</c>, or names a helper that must carry <c>[GpuCompatible]</c>. The walk is what makes a helper in
    /// another file reachable, and it is what refuses one that was never marked.
    /// </remarks>
    internal sealed class KernelSemantics
    {
        /// <summary>
        /// The structures reached so far, by symbol.
        /// </summary>
        private readonly Dictionary<INamedTypeSymbol, StructDeclaration> m_Structs = new(SymbolEqualityComparer.Default);

        /// <summary>
        /// The structures whose fields have not been walked yet.
        /// </summary>
        private readonly Queue<INamedTypeSymbol> m_Pending = new();

        /// <summary>
        /// The semantic model of every syntax tree the walk touched, built once.
        /// </summary>
        private readonly Dictionary<SyntaxTree, SemanticModel> m_Models = new();

        private readonly INamedTypeSymbol m_GpuStruct;
        private readonly INamedTypeSymbol m_GpuCompatible;

        private KernelSemantics(Compilation compilation, IMethodSymbol kernel, MethodDeclarationSyntax declaration, int threads, INamedTypeSymbol gpuStruct, INamedTypeSymbol gpuCompatible)
        {
            Compilation = compilation;
            Kernel = kernel;
            Declaration = declaration;
            Threads = threads;
            m_GpuStruct = gpuStruct;
            m_GpuCompatible = gpuCompatible;
        }

        /// <summary>
        /// Gets the compilation the kernel was resolved in.
        /// </summary>
        internal Compilation Compilation { get; }

        /// <summary>
        /// Gets the method marked as the kernel.
        /// </summary>
        internal IMethodSymbol Kernel { get; }

        /// <summary>
        /// Gets the declaration of that method.
        /// </summary>
        internal MethodDeclarationSyntax Declaration { get; }

        /// <summary>
        /// Gets the number of threads in a group.
        /// </summary>
        internal int Threads { get; }

        /// <summary>
        /// Gets the helpers the kernel reaches, in the order they were reached.
        /// </summary>
        internal IReadOnlyList<HelperMethod> Helpers { get; private set; } = Array.Empty<HelperMethod>();

        /// <summary>
        /// Gets the structures the kernel works with, in an order where a structure comes before the one that holds it.
        /// </summary>
        internal IReadOnlyList<StructDeclaration> Structs { get; private set; } = Array.Empty<StructDeclaration>();

        /// <summary>
        /// Resolves a kernel and everything it reaches.
        /// </summary>
        /// <param name="compilation">The compilation the kernel was resolved in.</param>
        /// <param name="kernel">The method marked <c>[GpuKernel]</c>.</param>
        /// <returns>The resolved kernel.</returns>
        /// <exception cref="KernelTranslationException">Thrown when the kernel is outside the subset.</exception>
        internal static KernelSemantics Create(Compilation compilation, IMethodSymbol kernel)
        {
            var gpuKernel = compilation.GetTypeByMetadataName("Gubbins.Graphic.GpuKernelAttribute")
                ?? throw new KernelTranslationException("The kernel vocabulary was not found: the compilation does not reference Gubbins.Graphic.");

            var gpuThreads = compilation.GetTypeByMetadataName("Gubbins.Graphic.GpuThreadsAttribute");
            var gpuStruct = compilation.GetTypeByMetadataName("Gubbins.Graphic.GpuStructAttribute")
                ?? throw new KernelTranslationException("The kernel vocabulary was not found: the compilation does not reference Gubbins.Graphic.");

            var gpuCompatible = compilation.GetTypeByMetadataName("Gubbins.Graphic.GpuCompatibleAttribute")
                ?? throw new KernelTranslationException("The kernel vocabulary was not found: the compilation does not reference Gubbins.Graphic.");

            if (kernel.MethodKind != MethodKind.Ordinary || !kernel.IsStatic)
            {
                throw new KernelTranslationException($"'{kernel.ToDisplayString()}' is not a kernel: a kernel is a static method.");
            }

            if (!kernel.ReturnsVoid)
            {
                throw new KernelTranslationException($"'{kernel.ToDisplayString()}' returns a value: a kernel writes its result to a buffer.");
            }

            if (!HasAttribute(kernel, gpuKernel))
            {
                throw new KernelTranslationException($"'{kernel.ToDisplayString()}' is not marked [GpuKernel], so there is no kernel to translate.");
            }

            var threads = THREADS_PER_GROUP;

            foreach (var attribute in kernel.GetAttributes())
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, gpuKernel))
                {
                    continue;
                }

                if (gpuThreads is not null && SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, gpuThreads))
                {
                    if (attribute.ConstructorArguments.Length != 1 || attribute.ConstructorArguments[0].Value is not int count || count < 1 || count > 1024)
                    {
                        throw new KernelTranslationException($"[{THREADS_ATTRIBUTE}(count)] takes the number of threads in a group as a literal.");
                    }

                    threads = count;
                    continue;
                }

                throw new KernelTranslationException($"The attribute '{attribute.AttributeClass?.Name}' means nothing to the device. A kernel declares at most [{KERNEL_ATTRIBUTE}] and [{THREADS_ATTRIBUTE}(count)].");
            }

            var reference = kernel.DeclaringSyntaxReferences.FirstOrDefault()
                ?? throw new KernelTranslationException($"'{kernel.ToDisplayString()}' was not declared in source, so its body cannot be read.");

            var declaration = reference.GetSyntax() as MethodDeclarationSyntax
                ?? throw new KernelTranslationException($"'{kernel.ToDisplayString()}' is not declared as a method.");

            var semantics = new KernelSemantics(compilation, kernel, declaration, threads, gpuStruct, gpuCompatible);
            semantics.Discover();

            return semantics;
        }

        /// <summary>
        /// Walks the kernel and the helpers it reaches, then the structures they use.
        /// </summary>
        private void Discover()
        {
            RejectUnsupported(Declaration, Declaration.SyntaxTree, semantic: true);

            // The parameter a kernel takes as one value is a structure the device holds, and it is walked first so that it
            // is declared before the body that reads it.
            foreach (var parameter in Kernel.Parameters)
            {
                RequireStruct(parameter.Type, $"parameter '{parameter.Name}'");
            }

            var pending = new Queue<IMethodSymbol>();
            var seen = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default) { Kernel };

            pending.Enqueue(Kernel);

            while (pending.Count > 0)
            {
                var method = pending.Dequeue();
                var syntax = SyntaxOf(method);
                var model = ModelFor(syntax.SyntaxTree);

                if (!SymbolEqualityComparer.Default.Equals(method, Kernel))
                {
                    RejectUnsupported(syntax, syntax.SyntaxTree, semantic: true);
                }

                foreach (var node in syntax.DescendantNodes())
                {
                    switch (node)
                    {
                        case InvocationExpressionSyntax invocation:
                            Reach(model.GetSymbolInfo(invocation).Symbol as IMethodSymbol, invocation, pending, seen);
                            break;

                        case ObjectCreationExpressionSyntax creation:
                            RequireStruct(model.GetTypeInfo(creation).Type, "a value the kernel builds");
                            break;

                        case VariableDeclaratorSyntax declarator when model.GetDeclaredSymbol(declarator) is ILocalSymbol local:
                            RequireStruct(local.Type, "a local of the kernel");
                            break;
                    }
                }
            }

            // The structures are closed over their fields, which is what makes a structure that holds another one emit
            // both of them.
            while (m_Pending.Count > 0)
            {
                var structure = m_Pending.Dequeue();

                foreach (var field in structure.GetMembers().OfType<IFieldSymbol>())
                {
                    if (field.IsStatic || field.AssociatedSymbol is not null)
                    {
                        continue;
                    }

                    RequireStruct(field.Type, $"a field of '{structure.Name}'");
                }
            }

            OrderStructures();
        }

        /// <summary>
        /// Resolves one call the kernel or a helper makes, following it when it is a helper.
        /// </summary>
        /// <param name="symbol">The method the call resolved to, or <see langword="null"/> when it did not resolve.</param>
        /// <param name="invocation">The call.</param>
        /// <param name="pending">The methods whose bodies are still to be walked.</param>
        /// <param name="seen">The methods already followed.</param>
        /// <returns><see langword="true"/> when the call is part of the subset; <see langword="false"/> when it is an intrinsic.</returns>
        /// <exception cref="KernelTranslationException">Thrown when the call reaches something the device cannot run.</exception>
        private bool Reach(IMethodSymbol? symbol, InvocationExpressionSyntax invocation, Queue<IMethodSymbol> pending, HashSet<IMethodSymbol> seen)
        {
            if (symbol is null)
            {
                throw new KernelTranslationException($"'{invocation}' is not a call the compiler could resolve, so the device has nothing to run.");
            }

            if (symbol.ReducedFrom is not null)
            {
                throw new KernelTranslationException($"'{invocation}' is an extension method: a kernel calls a helper the compiler can follow, not one that is written as an extension.");
            }

            if (IsIntrinsic(symbol) || IsShared(symbol) || IsBarrier(symbol) || (symbol.Name == SLICE && symbol.ContainingType is { } sliceOwner && IsSpan(sliceOwner)))
            {
                return false;
            }

            if (symbol.ContainingType is { TypeKind: TypeKind.Struct } owner && HasAttribute(owner, m_GpuStruct))
            {
                RequireStruct(owner, "a call of the kernel");
                return false;
            }

            if (!HasAttribute(symbol, m_GpuCompatible))
            {
                throw new KernelTranslationException($"'{symbol.ToDisplayString()}' is called from the kernel but is not marked [GpuCompatible]: a helper the device runs is declared for both processors.");
            }

            if (!symbol.IsStatic || symbol.TypeParameters.Length > 0)
            {
                throw new KernelTranslationException($"'{symbol.ToDisplayString()}' is not a plain static method: a helper of a kernel is static and not generic.");
            }

            if (symbol.Parameters.Any(parameter => IsSpan(parameter.Type)) || IsSpan(symbol.ReturnType))
            {
                throw new KernelTranslationException($"'{symbol.ToDisplayString()}' takes or returns a span: a helper of a kernel works on values, and the buffers are read by the kernel itself.");
            }

            foreach (var parameter in symbol.Parameters)
            {
                RequireStruct(parameter.Type, $"parameter '{parameter.Name}' of '{symbol.Name}'");
            }

            RequireStruct(symbol.ReturnType, $"the result of '{symbol.Name}'");

            var helpers = Helpers as List<HelperMethod> ?? new List<HelperMethod>();
            Helpers = helpers;

            if (symbol.DeclaringSyntaxReferences.Length == 0)
            {
                throw new KernelTranslationException($"'{symbol.ToDisplayString()}' is declared in an assembly this compilation does not hold the source of. A helper has to be compiled with the kernel, or linked into it as source.");
            }

            if (seen.Add(symbol))
            {
                helpers.Add(new HelperMethod(symbol, SyntaxOf(symbol)));
                pending.Enqueue(symbol);
            }

            return true;
        }

        /// <summary>
        /// Requires a type to be one the device has, and records the structure when it is one of the kernel's.
        /// </summary>
        /// <param name="type">The type a kernel value has.</param>
        /// <param name="use">What uses it, for the message.</param>
        /// <exception cref="KernelTranslationException">Thrown when the type has no representation on the device.</exception>
        private void RequireStruct(ITypeSymbol? type, string use)
        {
            if (type is null)
            {
                return;
            }

            if (type is IArrayTypeSymbol array)
            {
                RequireStruct(array.ElementType, use);
                return;
            }

            if (type is not INamedTypeSymbol named)
            {
                throw new KernelTranslationException($"'{type.ToDisplayString()}' is used by {use} and has no representation on the device.");
            }

            if (IsSpan(named))
            {
                RequireStruct(named.TypeArguments[0], use);
                return;
            }

            if (IsSupportedScalar(named) || IsVector(named))
            {
                return;
            }

            if (named.OriginalDefinition.SpecialType != SpecialType.None)
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' is used by {use} and has no representation on the device: the device holds int, uint, float and the structures declared for both processors.");
            }

            if (named.TypeKind != TypeKind.Struct)
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' is used by {use} and is not a structure: the device holds values, not objects.");
            }

            if (named.ContainingType is not null)
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' is nested inside another type: a structure of the device is declared at the top of a namespace.");
            }

            if (!HasAttribute(named, m_GpuStruct))
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' is used by {use} but is not marked [GpuStruct]: a value the device holds is a structure declared for both processors.");
            }

            if (!named.IsUnmanagedType)
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' holds a reference, so the device cannot lay it out.");
            }

            if (m_Structs.ContainsKey(named))
            {
                return;
            }

            var references = named.DeclaringSyntaxReferences;

            if (references.Length != 1)
            {
                throw new KernelTranslationException($"'{named.ToDisplayString()}' is declared in more than one place: a structure of a kernel is declared once.");
            }

            var syntax = references[0].GetSyntax() as StructDeclarationSyntax
                ?? throw new KernelTranslationException($"'{named.ToDisplayString()}' is not declared as a structure.");

            m_Structs.Add(named, new StructDeclaration(named, syntax));
            m_Pending.Enqueue(named);
        }

        /// <summary>
        /// Orders the structures so that one comes after every structure it holds.
        /// </summary>
        /// <exception cref="KernelTranslationException">Thrown when two structures hold each other.</exception>
        private void OrderStructures()
        {
            var ordered = new List<StructDeclaration>();
            var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            void Visit(INamedTypeSymbol structure)
            {
                if (visited.Contains(structure))
                {
                    return;
                }

                if (!visiting.Add(structure))
                {
                    throw new KernelTranslationException($"'{structure.Name}' holds itself: a structure of a kernel is a value, so it cannot contain its own type.");
                }

                foreach (var field in structure.GetMembers().OfType<IFieldSymbol>())
                {
                    if (field.IsStatic || field.Type is not INamedTypeSymbol fieldType || !m_Structs.ContainsKey(fieldType))
                    {
                        continue;
                    }

                    Visit(fieldType);
                }

                visiting.Remove(structure);
                visited.Add(structure);
                ordered.Add(m_Structs[structure]);
            }

            foreach (var structure in m_Structs.Keys)
            {
                Visit(structure);
            }

            Structs = ordered;
        }

        /// <summary>
        /// Reports whether a type is one of the numbers the device computes with.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><see langword="true"/> when it is.</returns>
        private static bool IsSupportedScalar(INamedTypeSymbol type)
            => type.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Single or SpecialType.System_Boolean;

        /// <summary>
        /// Reports whether a type is one of the vectors a shading language spells out.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><see langword="true"/> when it is.</returns>
        private static bool IsVector(INamedTypeSymbol type)
            => type.Name is "int2" or "int3" or "int4" or "uint2" or "uint3" or "uint4" or "float2" or "float3" or "float4";

        /// <summary>
        /// Reports whether a type is a span, which is how a kernel declares a buffer.
        /// </summary>
        /// <param name="type">The type to test.</param>
        /// <returns><see langword="true"/> when it is <c>Span&lt;T&gt;</c> or <c>ReadOnlySpan&lt;T&gt;</c>.</returns>
        private static bool IsSpan(ITypeSymbol type)
            => type is INamedTypeSymbol { Arity: 1 } named &&
               named.ContainingNamespace?.ToDisplayString() == "System" &&
               (named.Name == SPAN_TYPE || named.Name == READ_ONLY_SPAN_TYPE);

        /// <summary>
        /// Reports whether a method is one the intrinsics table maps.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns><see langword="true"/> when it maps to an intrinsic.</returns>
        private static bool IsIntrinsic(IMethodSymbol method)
            => (method.ContainingType?.ToDisplayString() == "System.Threading.Interlocked" && method.Name == "Add") ||
               (method.ContainingType?.Name is { } owner && INTRINSIC_NAMES.Contains($"{owner}.{method.Name}"));

        /// <summary>
        /// Reports whether a method is the call that declares a group shared array.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns><see langword="true"/> when it is <c>GpuIntrinsics.Shared</c>.</returns>
        private static bool IsShared(IMethodSymbol method)
            => method.Name == SHARED && method.ContainingType?.ToDisplayString() == "Gubbins.Graphic.GpuIntrinsics";

        /// <summary>
        /// Reports whether a method is the call that synchronizes a group.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns><see langword="true"/> when it is <c>GpuIntrinsics.Barrier</c>.</returns>
        private static bool IsBarrier(IMethodSymbol method)
            => method.Name == BARRIER && method.ContainingType?.ToDisplayString() == "Gubbins.Graphic.GpuIntrinsics";

        /// <summary>
        /// Reports whether a symbol carries an attribute.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="attribute">The attribute type.</param>
        /// <returns><see langword="true"/> when it does.</returns>
        private static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attribute)
            => symbol.GetAttributes().Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, attribute));

        /// <summary>
        /// Finds the declaration of a method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>Its declaration.</returns>
        private static MethodDeclarationSyntax SyntaxOf(IMethodSymbol method)
            => method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as MethodDeclarationSyntax
               ?? throw new KernelTranslationException($"'{method.ToDisplayString()}' has no declaration this compilation can read.");

        /// <summary>
        /// Gets the semantic model of a tree, building it once.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <returns>The model.</returns>
        internal SemanticModel ModelFor(SyntaxTree tree)
        {
            if (!m_Models.TryGetValue(tree, out var model))
            {
                model = Compilation.GetSemanticModel(tree);
                m_Models.Add(tree, model);
            }

            return model;
        }

        /// <summary>
        /// Reads the constant a name stands for, when the compiler resolved it.
        /// </summary>
        /// <param name="node">The name or member access.</param>
        /// <param name="value">When the method returns, the value the name stands for.</param>
        /// <returns><see langword="true"/> when the name is a constant.</returns>
        internal bool TryConstantValue(SyntaxNode node, out object value)
        {
            value = null!;

            if (ModelFor(node.SyntaxTree).GetSymbolInfo(node).Symbol is not IFieldSymbol { IsConst: true, HasConstantValue: true } field)
            {
                return false;
            }

            value = field.ConstantValue!;
            return true;
        }

        /// <summary>
        /// One method the kernel calls, with the declaration its body is read from.
        /// </summary>
        /// <param name="Symbol">The method.</param>
        /// <param name="Syntax">Its declaration.</param>
        internal readonly record struct HelperMethod(IMethodSymbol Symbol, MethodDeclarationSyntax Syntax);

        /// <summary>
        /// One structure the kernel holds, with the declaration it is read from.
        /// </summary>
        /// <param name="Symbol">The structure.</param>
        /// <param name="Syntax">Its declaration.</param>
        internal readonly record struct StructDeclaration(INamedTypeSymbol Symbol, StructDeclarationSyntax Syntax);
    }

    /// <summary>
    /// The names of the methods the intrinsics table maps, as the compiler resolves them.
    /// </summary>
    private static readonly HashSet<string> INTRINSIC_NAMES = new(StringComparer.Ordinal)
    {
        "Math.Abs", "MathF.Abs", "Math.Min", "MathF.Min", "Math.Max", "MathF.Max", "Math.Sqrt", "MathF.Sqrt",
        "Math.Floor", "MathF.Floor", "Math.Ceiling", "MathF.Ceiling", "Math.Pow", "MathF.Pow", "Math.Clamp",
        "MathF.Clamp", "Math.Sin", "MathF.Sin", "Math.Cos", "MathF.Cos", "GpuWave.Sum", "GpuWave.Min",
        "GpuWave.Max", "GpuWave.Product", "GpuWave.PrefixSum", "GpuWave.Any", "GpuWave.All"
    };
}
