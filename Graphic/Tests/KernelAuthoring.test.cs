using System.Reflection;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// The kernel as the compiler sees it: an ordinary method that the central processor can call, a helper it shares with
/// that method, and a structure both processors address, with the device only calls refused where they are reached.
/// </summary>
/// <remarks>
/// These tests need no device: they are the half of the promise that the same declaration runs on the central processor
/// as it is written, which is what makes a kernel part of the code base instead of a second language beside it.
/// </remarks>
/// <summary>
/// A structure the simulation writes and the kernel reads, declared once for both.
/// </summary>
[GpuStruct]
public struct Position
{
        /// <summary>The position on the x axis.</summary>
        public float X;

        /// <summary>The position on the y axis.</summary>
        public float Y;

        /// <summary>The position on the z axis.</summary>
        public float Z;

        /// <param name="x">The position on the x axis.</param>
        /// <param name="y">The position on the y axis.</param>
        /// <param name="z">The position on the z axis.</param>
        public Position(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

/// <summary>
/// The kernel and the helper it calls, which are ordinary C# methods.
/// </summary>
public sealed partial class KernelAuthoringTests
{
    public static partial class Motion
    {
        /// <summary>
        /// The fraction of a value a step keeps.
        /// </summary>
        private const float Retention = 0.98f;

        /// <summary>
        /// Damps one component of a position, which the kernel calls for each of them.
        /// </summary>
        /// <param name="value">The component to damp.</param>
        /// <returns>The damped component.</returns>
        [GpuCompatible]
        public static float Damping(float value) => value * Retention;

        /// <summary>
        /// Damps one position, which is the step the simulation runs and the device runs.
        /// </summary>
        /// <param name="index">The position to step.</param>
        /// <param name="source">The positions to read.</param>
        /// <param name="target">The positions to write.</param>
        [GpuKernel]
        [GpuThreads(64)]
        public static void Integrate(int index, ReadOnlySpan<Position> source, Span<Position> target)
        {
            var position = source[index];
            target[index] = new Position(Damping(position.X), Damping(position.Y), Damping(position.Z));
        }
    }

    [Test]
    public void Kernel_GeneratesATypedEntryPointBesideTheMethod()
    {
        var methods = typeof(Motion).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "Run")
            .ToList();

        Assert.That(methods, Is.Not.Empty, "the generator adds Run to the type that declares the kernel");
        Assert.That(
            methods.Any(method => method.GetParameters()[1].ParameterType.IsGenericType &&
                                  method.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(GpuBuffer<>)),
            Is.True,
            "one overload takes the buffers of this layer, which is the one the typed call uses");
        Assert.That(
            methods.Any(method => method.GetParameters()[1].ParameterType == typeof(ReadOnlySpan<Position>)),
            Is.True,
            "one overload takes spans, so a caller with an array does not build a buffer by hand");
    }

    [Test]
    public void Kernel_KeepsTheTranslatedProgramForTheArtifactBuild()
    {
        var registry = typeof(Motion).Assembly.GetType("Gubbins.Graphic.Generated.__GubbinsKernels");

        Assert.That(registry, Is.Not.Null, "the generator adds the registry the artifact build reads");
        Assert.That(registry!.GetNestedType("Kernel0", BindingFlags.NonPublic), Is.Not.Null, "the registry holds one entry per kernel");

        var entry = registry.GetNestedType("Kernel0", BindingFlags.NonPublic)!;

        Assert.That((string) entry.GetField("Name", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!, Does.EndWith(".Integrate"), "the artifact is named after the kernel");
        Assert.That((string) entry.GetField("Slang", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!, Does.Contain("void gubbins_main"), "the translated program is a constant, so a tool reads it without running the program");
        Assert.That((int) entry.GetField("Threads", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!, Is.EqualTo(64), "the group size travels with the program");
    }

    [Test]
    public void Kernel_RunsThroughTheGeneratedEntryPointOnTheDevice()
    {
        if (!Gpu.TryCreate(out var gpu) || gpu is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        using (gpu)
        {
            var source = new[] { new Position(100f, 200f, 300f), new Position(1f, 2f, 3f) };
            var target = new Position[source.Length];

            try
            {
                Motion.Run(gpu, source, target);
            }
            catch (GpuExecutionException)
            {
                Assert.Ignore("The kernel artifact was not built beside this test.");
                return;
            }

            Assert.That(target[0].X, Is.EqualTo(98f).Within(0.001f), "the generated entry point dispatched the kernel and the result came back");
            Assert.That(target[1].Z, Is.EqualTo(2.94f).Within(0.001f), "every element went through the helper the two processors share");
        }
    }

    [Test]
    public void Kernel_IsOrdinaryCSharpTheCentralProcessorRuns()
    {
        var source = new[] { new Position(100f, 200f, 300f), new Position(1f, 2f, 3f) };
        var target = new Position[source.Length];

        for (var index = 0; index < source.Length; index++)
        {
            Motion.Integrate(index, source, target);
        }

        Assert.That(target[0].X, Is.EqualTo(98f).Within(0.0001f), "the kernel runs as the method it is written as");
        Assert.That(target[0].Y, Is.EqualTo(196f).Within(0.0001f), "the helper it calls is the same helper");
        Assert.That(target[0].Z, Is.EqualTo(294f).Within(0.0001f), "every component goes through the one declaration");
        Assert.That(target[1].X, Is.EqualTo(0.98f).Within(0.0001f), "the second index reads the second position");
    }

    [Test]
    public void Kernel_DeclaresTheGroupSizeTheDeviceIsDispatchedWith()
    {
        var method = typeof(Motion).GetMethod(nameof(Motion.Integrate))!;
        var threads = method.GetCustomAttribute<GpuThreadsAttribute>();

        Assert.That(threads, Is.Not.Null, "the declaration carries the group size");
        Assert.That(threads!.Threads, Is.EqualTo(64), "the group the kernel was written against");
        Assert.That(method.GetCustomAttribute<GpuKernelAttribute>(), Is.Not.Null, "the method is marked as a kernel");
        Assert.That(typeof(Position).GetCustomAttribute<GpuStructAttribute>(), Is.Not.Null, "the structure is one the device holds");
        Assert.That(typeof(Motion).GetMethod(nameof(Motion.Damping))!.GetCustomAttribute<GpuCompatibleAttribute>(), Is.Not.Null, "the helper may be followed by the translator");
    }

    [Test]
    public void GpuThreads_RefusesAGroupADeviceCannotRun()
    {
        Assert.That(() => new GpuThreadsAttribute(0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new GpuThreadsAttribute(2048), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(new GpuThreadsAttribute(1024).Threads, Is.EqualTo(1024), "the largest group a backend promises is allowed");
    }

    [Test]
    public void GpuIntrinsics_RefuseGroupMemoryOnTheCentralProcessor()
    {
        var shared = Assert.Throws<NotSupportedException>(() => GpuIntrinsics.Shared<uint>(64));
        var barrier = Assert.Throws<NotSupportedException>(() => GpuIntrinsics.Barrier());

        Assert.That(shared!.Message, Does.Contain("group"), "the refusal names what the call means");
        Assert.That(barrier!.Message, Does.Contain("group"), "the barrier has no group to wait for");
    }

    [Test]
    public void GpuWave_RefusesWaveOperationsOnTheCentralProcessor()
    {
        Assert.That(() => GpuWave.Sum(1), Throws.TypeOf<NotSupportedException>().With.Message.Contains("wave"));
        Assert.That(() => GpuWave.PrefixSum(1u), Throws.TypeOf<NotSupportedException>().With.Message.Contains("wave"));
        Assert.That(() => GpuWave.Any(true), Throws.TypeOf<NotSupportedException>().With.Message.Contains("wave"));
    }

    [Test]
    public void GpuIntrinsics_ReportTheGroupSizeTheTranslatorDispatchesWith()
    {
        Assert.That(GpuIntrinsics.DefaultThreadsPerGroup, Is.EqualTo(64), "the default group size is one number, not a literal in three places");
    }
}
