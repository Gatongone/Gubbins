using System.Runtime.InteropServices;
using Gubbins.Graphic.Kernels;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// The kernel pipeline: what a C# kernel becomes, what the subset refuses, and that the artifacts it produces run on the
/// device this machine has.
/// </summary>
/// <remarks>
/// The tests that only read the translation need nothing but the translator. The ones that compile and run need
/// <c>slangc</c> and a device, and are ignored on a machine that has neither rather than reporting a failure that is not
/// the pipeline's.
/// </remarks>
public sealed class KernelTests
{
    /// <summary>
    /// A kernel that adds a group up through the shared memory of the group, which is the construct the barrier exists for.
    /// </summary>
    private const string Reduction = @"
using System;

public static unsafe class Reduce
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var lane = index % 64;
        var tile = Shared<uint>(64);

        tile[lane] = source[index];
        Barrier();

        tile[lane] = tile[lane] + tile[lane + 32];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 16];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 8];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 4];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 2];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 1];
        Barrier();

        if (lane == 0)
        {
            destination[index / 64] = tile[0];
        }
    }
}
";

    /// <summary>
    /// A kernel that reads the element count of its input, which is the construct the lengths block exists for.
    /// </summary>
    private const string Kernel = @"
using System;

public static unsafe class Scale
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var bounded = Math.Min(source.Length, 4096);
        destination[index] = source[index] + (uint)bounded;
    }
}
";

    [Test]
    public void Kernel_TranslatesIntoTheShapeEveryBackendBinds()
    {
        var slang = KernelTranspiler.Transpile(Kernel);

        Assert.That(slang, Does.Contain($"#define index (__gubbinsThreadId.x)"), "the thread index is the name the body uses");
        Assert.That(slang, Does.Contain($"void {KernelTranspiler.ENTRY_POINT}("), "every backend looks the kernel up by one name");
        Assert.That(slang, Does.Contain("[[vk::binding(0, 0)]] RWStructuredBuffer<uint> source : register(u0);"));
        Assert.That(slang, Does.Contain("[[vk::binding(1, 0)]] RWStructuredBuffer<uint> destination : register(u1);"));
        Assert.That(slang, Does.Contain("[[vk::binding(2, 0)]] RWStructuredBuffer<uint> __gubbinsLengths : register(u2);"));
        Assert.That(slang, Does.Contain("int __length_source = int(__gubbinsLengths[0]);"), "a span's Length comes from the lengths block");
        Assert.That(slang, Does.Contain("min(__length_source, 4096)"), "the maths calls the subset maps become intrinsics");
    }

    [Test]
    public void Kernel_RefusesACallOutsideTheSubset()
    {
        const string Source = @"
using System;

public static unsafe class Tiled
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var copy = source.ToArray();
        destination[index] = copy[index];
    }
}
";

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(Source));

        Assert.That(refusal!.Message, Does.Contain("ToArray"));
    }

    [Test]
    public void Kernel_TurnsASliceIntoAWindowOnTheBuffer()
    {
        const string Source = @"
using System;

public static unsafe class Tiled
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var half = source.Length / 2;
        var first = source.Slice(0, half);
        var second = destination.Slice(half, half);
        second[index] = first[index] + 1;
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("source[index]"), "a read through a slice that starts at zero is the buffer itself");
        Assert.That(slang, Does.Contain("destination[(index) + (half)]"), "a write through a slice is a write of the buffer it came from");
        Assert.That(slang, Does.Not.Contain(".Slice("), "the slice itself has no representation on the device");
    }

    [Test]
    public void Kernel_MeasuresASliceThatRunsToTheEndFromTheBuffer()
    {
        const string Source = @"
using System;

public static unsafe class Tailed
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var tail = source.Slice(1);
        destination[index] = tail.Length;
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("(__length_source - (1))"), "a slice with no count runs to the end of the buffer");
    }

    [Test]
    public void Kernel_ResolvesASliceUsedInline()
    {
        const string Source = @"
using System;

public static unsafe class Inline
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = source.Slice(0, 1)[0];
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("source[0]"), "a slice of one element is the element itself");
        Assert.That(slang, Does.Not.Contain(".Slice("), "the slice itself has no representation on the device");
    }

    [Test]
    public void Kernel_ResolvesASliceOfASlice()
    {
        const string Source = @"
using System;

public static unsafe class Nested
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var inner = source.Slice(1);
        destination[index] = inner.Slice(2)[0] + (uint)inner.Length;
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("source[(0) + ((1) + (2))]"), "the offsets of a chain of slices add up");
        Assert.That(slang, Does.Contain("(__length_source - (1))"), "the length of a slice runs from its offset to the end of the buffer");
    }

    [Test]
    public void Kernel_ReadsTheGroupSizeFromTheAttribute()
    {
        const string Source = @"
using System;

public static unsafe class Grouped
{
    [GpuKernel]
    [GpuThreads(128)]
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = source[index] + 1;
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("[numthreads(128, 1, 1)]"), "the kernel says how many threads a group of it has");
        Assert.That(KernelTranspiler.ThreadsPerGroup(Source), Is.EqualTo(128), "a caller divides its element count by the same number");
        Assert.That(KernelTranspiler.ThreadsPerGroup(Kernel), Is.EqualTo(KernelTranspiler.THREADS_PER_GROUP), "a kernel that says nothing gets the default");
    }

    [Test]
    public void Kernel_RefusesAnAttributeMeantForTheHost()
    {
        const string Source = @"
using System;

public static unsafe class Marked
{
    [Serializable]
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = source[index] + 1;
    }
}
";

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(Source));

        Assert.That(refusal!.Message, Does.Contain("GpuThreads"));
    }
    [Test]
    public void Kernel_RefusesWhatTheDeviceCannotExpress()
    {
        const string Array = @"
using System;

public static unsafe class Scratch
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var values = new uint[4];
        values[0] = source[index];
        destination[index] = values[0];
    }
}
";

        const string Narrow = @"
using System;

public static unsafe class Narrow
{
    public static void Kernel(int index, ReadOnlySpan<byte> source, Span<uint> destination)
    {
        destination[index] = source[index];
    }
}
";

        Assert.That(() => KernelTranspiler.Transpile(Array), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("cannot create an array"));
        Assert.That(() => KernelTranspiler.Transpile(Narrow), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("element type every backend has"));
    }

    [Test]
    public void Kernel_RefusesTheSyntaxThatOnlyMeansSomethingOnTheHost()
    {
        const string Index = @"
using System;

public static unsafe class FromTheEnd
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = source[^1];
    }
}
";

        const string Pattern = @"
using System;

public static unsafe class Pattern
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        if (source[index] is 0)
        {
            destination[index] = 1;
        }
    }
}
";

        const string Wide = @"
using System;

public static unsafe class Wide
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = (uint)((double)source[index] / 2.0);
    }
}
";

        Assert.That(() => KernelTranspiler.Transpile(Index), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("counts forwards"));
        Assert.That(() => KernelTranspiler.Transpile(Pattern), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("pattern"));
        Assert.That(() => KernelTranspiler.Transpile(Wide), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("cast to 'double'"));
    }

    [Test]
    public void Kernel_RefusesALambda()
    {
        const string Source = @"
using System;

public static unsafe class Callback
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        Func<uint, uint> copy = value => value;
        destination[index] = copy(source[index]);
    }
}
";

        Assert.That(() => KernelTranspiler.Transpile(Source), Throws.TypeOf<KernelTranslationException>());
    }

    [Test]
    public void Kernel_CompilesEveryArtifactTheBackendsLoad()
    {
        var directory = Directory.CreateTempSubdirectory("gubbins-kernel-").FullName;

        try
        {
            var artifacts = Compile(directory);
            var names = artifacts.Select(Path.GetFileName).ToList();

            // The targets every machine with the compiler can produce: the translation itself, the two cross platform
            // shader artifacts, and the source Metal compiles itself.
            foreach (var extension in new[] { ".slang", ".spv", ".comp", ".metal" })
            {
                Assert.That(names, Does.Contain("Scale" + extension), $"the {extension} artifact is one of the targets");
            }

            // The remaining three come from a downstream compiler, which is a Windows driver kit, a CUDA runtime or the
            // platform toolchain. A machine without one produces the rest rather than failing the build.
            if (names.Contains("Scale.ptx"))
            {
                Assert.That(File.ReadAllText(Path.Combine(directory, "Scale.ptx")), Does.Contain("SLANG_globalParams"), "CUDA reads its buffers from the constant the runtime fills");
            }

            if (OperatingSystem.IsWindows())
            {
                Assert.That(names, Does.Contain("Scale.dxil"), "a Windows build machine produces the D3D12 artifact");
                Assert.That(names, Does.Contain("Scale.ptx"), "a Windows build machine produces the CUDA artifact");
            }

            if (OperatingSystem.IsMacOS())
            {
                Assert.That(names, Does.Contain("Scale.metallib"), "a macOS build machine turns the source into the library Metal loads");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Kernel_RunsOnTheDeviceThisMachineHas()
    {
        const int Count = 4096;

        if (!GpuDevice.TryCreate(out var device) || device is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-kernel-").FullName;

        try
        {
            Compile(directory);

            using var memory = device.CreateMemory();
            using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scale");

            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate(Count * sizeof(uint), GpuAccess.Shared);
            var output = memory.Allocate(Count * sizeof(uint), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, Count / KernelTranspiler.THREADS_PER_GROUP, lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());
            var expected = (uint)Math.Min(Count, 4096);

            for (var index = 0; index < Count; index++)
            {
                Assert.That(result[index], Is.EqualTo(values[index] + expected), $"the kernel wrote element {index}");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Kernel_RunsASlicedWindowOnTheDeviceTheMachineHas()
    {
        const int Count = 4096;
        const string Tiled = @"
using System;

public static unsafe class Tiled
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var half = source.Length / 2;
        var first = source.Slice(0, half);
        var second = destination.Slice(half, half);
        second[index] = first[index] + 1;
    }
}
";

        if (!GpuDevice.TryCreate(out var device) || device is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-tiled-").FullName;

        try
        {
            try
            {
                KernelCompiler.Compile(Tiled, directory, "Tiled");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found"))
            {
                Assert.Ignore("This machine has no slangc.");
                return;
            }

            using var memory = device.CreateMemory();
            using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Tiled");

            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var half = Count / 2;
            var input = memory.Allocate(Count * sizeof(uint), GpuAccess.Shared);
            var output = memory.Allocate(Count * sizeof(uint), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(half / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var index = 0; index < half; index++)
            {
                Assert.That(result[half + index], Is.EqualTo(values[index] + 1), $"the kernel wrote element {half + index}");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Kernel_TranslatesASharedArrayAndItsBarrier()
    {
        const string Source = @"
using System;

public static unsafe class Reduce
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var lane = index % 64;
        var tile = Shared<uint>(64);

        tile[lane] = source[index];
        Barrier();

        if (lane == 0)
        {
            destination[index / 64] = tile[0] + tile.Length;
        }
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("groupshared uint tile[64];"), "the array every thread of the group addresses is a module declaration");
        Assert.That(slang, Does.Contain("GroupMemoryBarrierWithGroupSync();"), "the barrier becomes the one call every backend has");
        Assert.That(slang, Does.Contain("tile[0] + 64"), "the length of a shared array is the number it was declared with");
        Assert.That(slang, Does.Not.Contain("Shared<"), "the declaration has no representation on the device");
    }

    [Test]
    public void Kernel_RefusesASharedArraySizedByAnythingButALiteral()
    {
        const string Source = @"
using System;

public static unsafe class Reduce
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var size = source.Length;
        var tile = Shared<uint>(size);
        tile[index % 64] = source[index];
        destination[index] = tile[0];
    }
}
";

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(Source));

        Assert.That(refusal!.Message, Does.Contain("literal"));
    }

    [Test]
    public void Kernel_ReducesAGroupOnTheDeviceTheMachineHas()
    {
        const int Count = 65536;
        const string Reduction = @"
using System;

public static unsafe class Reduce
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var lane = index % 64;
        var tile = Shared<uint>(64);

        tile[lane] = source[index];
        Barrier();

        tile[lane] = tile[lane] + tile[lane + 32];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 16];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 8];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 4];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 2];
        Barrier();
        tile[lane] = tile[lane] + tile[lane + 1];
        Barrier();

        if (lane == 0)
        {
            destination[index / 64] = tile[0];
        }
    }
}
";

        if (!GpuDevice.TryCreate(out var device) || device is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-reduce-").FullName;

        try
        {
            try
            {
                KernelCompiler.Compile(Reduction, directory, "Reduce");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found"))
            {
                Assert.Ignore("This machine has no slangc.");
                return;
            }

            using var memory = device.CreateMemory();
            using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Reduce");

            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var groups = Count / KernelTranspiler.THREADS_PER_GROUP;
            var input = memory.Allocate(Count * sizeof(uint), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(groups * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)(index % 5 + 1);
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)groups, lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var group = 0; group < groups; group++)
            {
                var expected = 0u;

                for (var lane = 0; lane < KernelTranspiler.THREADS_PER_GROUP; lane++)
                {
                    expected += values[group * KernelTranspiler.THREADS_PER_GROUP + lane];
                }

                Assert.That(result[group], Is.EqualTo(expected), $"the group {group} added its values up");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Kernel_TranslatesAVectorElementType()
    {
        const string Source = @"
using System;

public static unsafe class Square
{
    public static void Kernel(int index, ReadOnlySpan<float4> source, Span<float4> destination)
    {
        var value = source[index];
        destination[index] = value * value;
    }
}
";

        var slang = KernelTranspiler.Transpile(Source);

        Assert.That(slang, Does.Contain("RWStructuredBuffer<float4> source"), "an element is whatever type the language knows");
        Assert.That(slang, Does.Contain("value * value"), "the arithmetic on a vector is the language's own");
    }

    [Test]
    public void Kernel_RefusesTheResultOfAnAtomicAdd()
    {
        const string Source = @"
using System;

public static unsafe class Histogram
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var previous = Interlocked.Add(ref destination[index % 16], 1);
        destination[0] = previous;
    }
}
";

        var refusal = Assert.Throws<KernelTranslationException>(() => KernelTranspiler.Transpile(Source));

        Assert.That(refusal!.Message, Does.Contain("atomic add"));
    }

    [Test]
    public void Kernel_SquaresAVectorOnTheDeviceTheMachineHas()
    {
        const int Count = 8192;
        const string Squaring = @"
using System;

public static unsafe class Square
{
    public static void Kernel(int index, ReadOnlySpan<float4> source, Span<float4> destination)
    {
        var value = source[index];
        destination[index] = value * value;
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Squaring, "Square"))
        {
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Square"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var elements = Count * 4;
            var input = memory.Allocate((nuint)(elements * sizeof(float)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(elements * sizeof(float)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new float[elements];

            for (var index = 0; index < elements; index++)
            {
                values[index] = index % 7 * 1.5f;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

            for (var index = 0; index < elements; index++)
            {
                Assert.That(result[index], Is.EqualTo(values[index] * values[index]).Within(1e-4f * MathF.Max(1f, values[index] * values[index])), $"the kernel squared element {index}");
            }
        }

        Directory.Delete(directory, true);
    }

    [Test]
    public void Kernel_CountsIntoBucketsFromEveryThread()
    {
        const int Count = 65536;
        const int Buckets = 16;
        const string Counting = @"
using System;

public static unsafe class Histogram
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var bucket = index % 16;
        Interlocked.Add(ref destination[bucket], 1);
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Counting, "Histogram"))
        {
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Histogram"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Buckets * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(output, MemoryMarshal.AsBytes(new uint[Buckets].AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Buckets, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var bucket = 0; bucket < Buckets; bucket++)
            {
                Assert.That(result[bucket], Is.EqualTo(Count / Buckets), $"bucket {bucket} counted every thread that landed in it");
            }
        }

        Directory.Delete(directory, true);
    }

    [Test]
    public void Kernel_RunsOnEveryBackendTheMachineHas()
    {
        const int Count = 4096;

        var directory = Directory.CreateTempSubdirectory("gubbins-backends-").FullName;

        try
        {
            try
            {
                KernelCompiler.Compile(Kernel, directory, "Scale");
                KernelCompiler.Compile(Reduction, directory, "Reduce");
            }
            catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found"))
            {
                Assert.Ignore("This machine has no slangc.");
                return;
            }

            var ran = 0;

            foreach (var name in new[] { "D3D12", "Vulkan", "OpenGL", "CUDA", "Metal" })
            {
                using var device = Create(name);

                if (device is null)
                {
                    continue;
                }

                ran++;
                VerifyScale(device, directory, name, Count);
                VerifyReduction(device, directory, name, Count);
            }

            if (ran == 0)
            {
                Assert.Ignore("This machine has no backend the layer can create.");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Creates one backend's device, whether or not this machine has it.
    /// </summary>
    /// <param name="name">The backend to create.</param>
    /// <returns>The device, or <see langword="null"/> when the backend is not there.</returns>
    private static GpuDevice? Create(string name)
    {
        try
        {
            switch (name)
            {
                case "D3D12" when OperatingSystem.IsWindows() && D3D12Device.TryCreate(out var d3d12):
                    return new GpuDevice(d3d12, d3d12!.Provider.GetProvider(), d3d12.Provider.MaxSegmentSize, d3d12.Provider.IsUnifiedMemory, d3d12.Provider.WorkingSetLimit, "D3D12");

                case "Vulkan" when VulkanDevice.TryCreate(out var vulkan):
                    return new GpuDevice(vulkan, vulkan!.Provider.GetProvider(), vulkan.Provider.MaxSegmentSize, vulkan.Provider.IsUnifiedMemory, vulkan.Provider.WorkingSetLimit, "Vulkan");

                case "OpenGL" when GlDevice.TryCreate(out var openGL):
                    return new GpuDevice(openGL, openGL!.Provider.GetProvider(), openGL.Provider.MaxSegmentSize, false, openGL.Provider.WorkingSetLimit, "OpenGL");

                case "CUDA" when CudaDevice.IsAvailable && CudaDevice.TryCreate(out var cuda):
                    return new GpuDevice(cuda, cuda!.Provider.GetProvider(), cuda.Provider.MaxSegmentSize, false, cuda.Provider.WorkingSetLimit, "CUDA");

                case "Metal" when OperatingSystem.IsMacOS() && MetalDevice.TryCreate(out var metal):
                    return new GpuDevice(metal, metal!.Provider.GetProvider(), metal.Provider.MaxSegmentSize, metal.Provider.IsUnifiedMemory, metal.Provider.WorkingSetLimit, "Metal");

                default:
                    return null;
            }
        }
        catch (Exception)
        {
            // A backend whose runtime is not installed reports it by refusing to load, which is the backend not being
            // there rather than a failure of the layer.
            return null;
        }
    }

    /// <summary>
    /// Runs the kernel that reads its input's length on one backend.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    /// <param name="count">The number of elements to work on.</param>
    private static void VerifyScale(GpuDevice device, string directory, string name, int count)
    {
        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scale");

        Assert.That(kernel, Is.Not.Null, $"{name}: the artifact the backend needs was loaded");

        var input = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Shared);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var values = new uint[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = (uint)index;
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { (uint)count, (uint)count, 0, 0 }.AsSpan()));

        Assert.That(kernel!.TryDispatch(input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the dispatch ran");
        memory.MarkWrittenByGpu(output);
        memory.Synchronize();

        var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());
        var addend = (uint)Math.Min(count, 4096);

        for (var index = 0; index < count; index++)
        {
            Assert.That(result[index], Is.EqualTo(values[index] + addend), $"{name}: element {index}");
        }
    }

    /// <summary>
    /// Runs the kernel that synchronizes a group through shared memory on one backend.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    /// <param name="count">The number of elements to add up.</param>
    private static void VerifyReduction(GpuDevice device, string directory, string name, int count)
    {
        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Reduce");

        Assert.That(kernel, Is.Not.Null, $"{name}: the artifact the backend needs was loaded");

        var groups = count / KernelTranspiler.THREADS_PER_GROUP;
        var input = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(groups * sizeof(uint)), GpuAccess.Shared);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var values = new uint[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = (uint)(index % 5 + 1);
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { (uint)count, (uint)count, 0, 0 }.AsSpan()));

        Assert.That(kernel!.TryDispatch(input, output, (uint)groups, lengths), Is.True, $"{name}: the dispatch ran");
        memory.MarkWrittenByGpu(output);
        memory.Synchronize();

        var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

        for (var group = 0; group < groups; group++)
        {
            var expected = 0u;

            for (var lane = 0; lane < KernelTranspiler.THREADS_PER_GROUP; lane++)
            {
                expected += values[group * KernelTranspiler.THREADS_PER_GROUP + lane];
            }

            Assert.That(result[group], Is.EqualTo(expected), $"{name}: the group {group} added its values up");
        }
    }

    /// <summary>
    /// Compiles a kernel for the device this machine has, skipping the test when either is missing.
    /// </summary>
    /// <param name="device">When the method returns, the device to run on.</param>
    /// <param name="directory">When the method returns, the directory the artifacts were written to.</param>
    /// <param name="source">The kernel to compile.</param>
    /// <param name="name">The kernel name.</param>
    /// <returns><see langword="true"/> when there is both a device and an artifact to load.</returns>
    private static bool TryUseDevice(out GpuDevice device, out string directory, string source, string name)
    {
        device = null!;
        directory = string.Empty;

        if (!GpuDevice.TryCreate(out var created) || created is null)
        {
            Assert.Ignore("This machine has no device the layer can use.");
            return false;
        }

        device = created;
        directory = Directory.CreateTempSubdirectory("gubbins-kernel-").FullName;

        try
        {
            KernelCompiler.Compile(source, directory, name);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found"))
        {
            Assert.Ignore("This machine has no slangc.");
            Directory.Delete(directory, true);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Compiles the kernel the tests share, skipping the test when the compiler is not installed.
    /// </summary>
    /// <param name="directory">The directory the artifacts are written to.</param>
    /// <returns>The artifacts that were written.</returns>
    private static IReadOnlyList<string> Compile(string directory)
    {
        try
        {
            return KernelCompiler.Compile(Kernel, directory, "Scale");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("slangc was not found"))
        {
            Assert.Ignore("This machine has no slangc.");
            return Array.Empty<string>();
        }
    }
}
