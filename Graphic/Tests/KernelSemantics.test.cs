using Gubbins.Graphic.Kernels;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// The translator reading a kernel the compiler resolved: the helper it calls and the structure it holds are the ones the
/// central processor runs, not a second declaration the kernel carries beside them.
/// </summary>
/// <remarks>
/// The source is compiled here with the Graphic assembly as a reference, which is the same view the source generator
/// gets: the attributes, the intrinsics and the spans are symbols, so a call either resolves to something the subset maps
/// or the translation refuses it by name.
/// </remarks>
public sealed class KernelSemanticsTests
{
    /// <summary>
    /// A kernel, the helper it calls and the structure both processors use, written as one piece of C#.
    /// </summary>
    private const string Source = """
        using System;
        using Gubbins.Graphic;
        using static Gubbins.Graphic.GpuIntrinsics;

        [GpuStruct]
        public struct Position
        {
            public float X;
            public float Y;
            public float Z;

            public Position(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }

        public static class Motion
        {
            private const float Retention = 0.98f;

            [GpuCompatible]
            public static float Damping(float value) => value * Retention;

            [GpuKernel]
            [GpuThreads(64)]
            public static void Integrate(int index, ReadOnlySpan<Position> source, Span<Position> target)
            {
                var position = source[index];
                target[index] = new Position(Damping(position.X), Damping(position.Y), Damping(position.Z));
            }
        }
        """;

    [Test]
    public void Kernel_TranslatesTheHelperAndStructureTheCodeBaseDeclares()
    {
        var (compilation, kernel) = CompileSource(Source);
        var slang = KernelTranspiler.Transpile(compilation, kernel);

        Assert.That(slang, Does.Contain("struct Position"), "the structure is the one the source declares");
        Assert.That(slang, Does.Contain("float Damping(float value)"), "the helper is a function of the program");
        Assert.That(slang, Does.Contain("0.98"), "the constant the helper reads is folded to its value");
        Assert.That(slang, Does.Not.Contain("Retention"), "the name of the constant has no meaning on the device");
        Assert.That(slang, Does.Contain("RWStructuredBuffer<Position> source"), "the buffer holds the structure itself");
        Assert.That(slang, Does.Contain("target[index]= Position(Damping(position.X)"), "the body calls the helper and builds the structure");
        Assert.That(slang, Does.Contain("[numthreads(64, 1, 1)]"), "the group size is the one the attribute declares");
    }

    [Test]
    public void Kernel_ReadsTheGroupSizeFromTheSymbol()
    {
        var (_, kernel) = CompileSource(Source);

        Assert.That(KernelTranspiler.ThreadsPerGroup(kernel), Is.EqualTo(64));
    }

    [Test]
    public void Kernel_RefusesACallThatIsNotDeclaredForTheDevice()
    {
        var (compilation, kernel) = CompileSource(Source.Replace("[GpuCompatible]", string.Empty));

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(compilation, kernel));

        Assert.That(refusal!.Message, Does.Contain("GpuCompatible"), "the refusal names the mark the call is missing");
        Assert.That(refusal.Message, Does.Contain("Damping"), "the refusal names the call that was reached");
    }

    [Test]
    public void Kernel_RefusesAValueThatIsNotDeclaredForTheDevice()
    {
        var (compilation, kernel) = CompileSource(Source.Replace("[GpuStruct]", string.Empty));

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(compilation, kernel));

        Assert.That(refusal!.Message, Does.Contain("GpuStruct"), "the refusal names the mark the structure is missing");
        Assert.That(refusal.Message, Does.Contain("Position"), "the refusal names the value that has no layout");
    }

    [Test]
    public void Kernel_RefusesOneThatWasNotMarked()
    {
        var (compilation, kernel) = CompileSource(Source.Replace("[GpuKernel]", string.Empty));

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(compilation, kernel));

        Assert.That(refusal!.Message, Does.Contain("GpuKernel"), "the method has to say that it is one");
    }

    [Test]
    public void Kernel_ReachesAHelperAndAStructureDeclaredInAnotherFile()
    {
        const string Kernel = """
            using System;
            using Gubbins.Graphic;
            using static Gubbins.Graphic.GpuIntrinsics;

            public static class Motion
            {
                [GpuKernel]
                public static void Integrate(int index, ReadOnlySpan<Position> source, Span<Position> target)
                {
                    var position = source[index];
                    target[index] = new Position(Damping.Apply(position.X), position.Y, position.Z);
                }
            }
            """;

        const string Shared = """
            using Gubbins.Graphic;

            [GpuStruct]
            public struct Position
            {
                public float X;
                public float Y;
                public float Z;

                public Position(float x, float y, float z)
                {
                    X = x;
                    Y = y;
                    Z = z;
                }
            }

            public static class Damping
            {
                private const float Retention = 0.98f;

                [GpuCompatible]
                public static float Apply(float value) => value * Retention;
            }
            """;

        var (compilation, kernel) = CompileProject("Integrate", Kernel, Shared);
        var slang = KernelTranspiler.Transpile(compilation, kernel);

        Assert.That(slang, Does.Contain("struct Position"), "the structure comes from the file that declares it");
        Assert.That(slang, Does.Contain("float Apply(float value)"), "the helper comes from the file that declares it");
        Assert.That(slang, Does.Contain("Apply(position.X)"), "a qualified call is emitted as the free function it becomes");
        Assert.That(slang, Does.Contain("0.98"), "the constant of that file is folded too");
    }

    [Test]
    public void Kernel_MapsTheCallsOnlyTheDeviceHasMeaningFor()
    {
        const string Grouped = """
            using System;
            using Gubbins.Graphic;
            using static Gubbins.Graphic.GpuIntrinsics;

            public static class Grouped
            {
                [GpuKernel]
                public static void Touch(int index, ReadOnlySpan<uint> source, Span<uint> target)
                {
                    var lane = index % 64;
                    var tile = Shared<uint>(64);
                    tile[lane] = source[index];
                    Barrier();
                    target[index] = GpuWave.Sum(tile[lane]) + (uint)MathF.Abs((float)source[index]) + (uint)source.Length;
                }
            }
            """;

        var (compilation, kernel) = CompileSource(Grouped, "Touch");
        var slang = KernelTranspiler.Transpile(compilation, kernel);

        Assert.That(slang, Does.Contain("groupshared uint tile[64];"), "the shared array is the memory of the group");
        Assert.That(slang, Does.Contain("GroupMemoryBarrierWithGroupSync()"), "the barrier becomes the one every backend has");
        Assert.That(slang, Does.Contain("WaveActiveSum(tile[lane])"), "the wave call becomes the wave intrinsic");
        Assert.That(slang, Does.Contain("abs("), "the maths call becomes the intrinsic with that meaning");
        Assert.That(slang, Does.Contain("__length_source"), "a length read is the block the caller passes");
    }

    [Test]
    public void Kernel_MapsASliceAndAnAtomicAdd()
    {
        const string Window = """
            using System;
            using System.Threading;
            using Gubbins.Graphic;
            using static Gubbins.Graphic.GpuIntrinsics;

            public static class Window
            {
                [GpuKernel]
                public static void Sum(int index, ReadOnlySpan<uint> source, Span<uint> target)
                {
                    var inner = source.Slice(1);
                    target[index] = inner[0] + (uint)inner.Length;
                    Interlocked.Add(ref target[index], 1);
                }
            }
            """;

        var (compilation, kernel) = CompileSource(Window, "Sum");
        var slang = KernelTranspiler.Transpile(compilation, kernel);

        Assert.That(slang, Does.Contain("source[(0) + (1)]"), "a slice is the buffer and the offset it starts at");
        Assert.That(slang, Does.Contain("(__length_source - (1))"), "the length of a slice runs from its offset to the end");
        Assert.That(slang, Does.Not.Contain(".Slice"), "no span is left in the program");
        Assert.That(slang, Does.Contain("InterlockedAdd(target[index], 1)"), "the atomic add is the one the device has");
    }

    /// <summary>
    /// Compiles a source with the Graphic assembly as a reference and finds the kernel in it.
    /// </summary>
    /// <param name="source">The source to compile.</param>
    /// <returns>The compilation and the kernel method.</returns>
    private static (Compilation Compilation, IMethodSymbol Kernel) CompileSource(string source, string kernelName = "Integrate")
        => CompileProject(kernelName, source);

    /// <summary>
    /// Compiles the sources as one project with the Graphic assembly as a reference and finds the kernel in them.
    /// </summary>
    /// <param name="kernelName">The method that is the kernel.</param>
    /// <param name="sources">The sources to compile.</param>
    /// <returns>The compilation and the kernel method.</returns>
    private static (Compilation Compilation, IMethodSymbol Kernel) CompileProject(string kernelName, params string[] sources)
    {
        var references = ((string) AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                          .Split(Path.PathSeparator)
                          .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                          .Select(path => (MetadataReference) MetadataReference.CreateFromFile(path))
                          .ToList();

        references.Add(MetadataReference.CreateFromFile(typeof(GpuKernelAttribute).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "Gubbins.Graphic.KernelSemantics.Tests",
            sources.Select(source => CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();

        Assert.That(diagnostics, Is.Empty, "the kernel has to be valid C# before it can be translated");

        var kernel = Types(compilation.Assembly.GlobalNamespace)
            .SelectMany(type => type.GetMembers().OfType<IMethodSymbol>())
            .First(method => method.Name == kernelName);

        return (compilation, kernel);
    }

    /// <summary>
    /// Walks the types of a namespace or a type, including the ones nested in it.
    /// </summary>
    /// <param name="container">The namespace or type to walk.</param>
    /// <returns>The types it holds.</returns>
    private static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol container)
    {
        foreach (var member in container.GetMembers())
        {
            switch (member)
            {
                case INamespaceSymbol space:
                    foreach (var type in Types(space))
                    {
                        yield return type;
                    }

                    break;

                case INamedTypeSymbol type:
                    yield return type;

                    foreach (var nested in Types(type))
                    {
                        yield return nested;
                    }

                    break;
            }
        }
    }
}
