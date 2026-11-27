using System.Runtime.InteropServices;
using Gubbins.Graphic.Kernels;

namespace Gubbins.Graphic.Tests;

/// <summary>
/// The kernel pipeline: what a C# kernel becomes, what the subset refuses, and that the artifacts it produces run on the
/// device this machine has.
/// </summary>
/// <remarks>
/// The tests that only read the translation need nothing but the translator. The ones that compile and run need a device,
/// and are ignored on a machine that has none rather than reporting a failure that is not the pipeline's.
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
        var hlsl = Hlsl(Kernel);

        Assert.That(hlsl, Does.Contain("int __gubbinsIndex = int(__gubbinsThreadId.x);"), "the thread index is the name the body uses");
        Assert.That(hlsl, Does.Contain($"void {KernelTranspiler.ENTRY_POINT}("), "every backend looks the kernel up by one name");
        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<uint> source : register(u0);"));
        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<uint> destination : register(u1);"));
        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<uint> __gubbinsLengths : register(u2);"));
        Assert.That(hlsl, Does.Contain("int __length_source = int(__gubbinsLengths[0]);"), "a span's Length comes from the lengths block");
        Assert.That(hlsl, Does.Contain("min(__length_source, 4096)"), "the maths calls the subset maps become intrinsics");
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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("source[__gubbinsIndex]"), "a read through a slice that starts at zero is the buffer itself");
        Assert.That(hlsl, Does.Contain("destination[__gubbinsIndex + half]"), "a write through a slice is a write of the buffer it came from");
        Assert.That(hlsl, Does.Not.Contain(".Slice("), "the slice itself has no representation on the device");
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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("(__length_source - 1)"), "a slice with no count runs to the end of the buffer");
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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("source[0]"), "a slice of one element is the element itself");
        Assert.That(hlsl, Does.Not.Contain(".Slice("), "the slice itself has no representation on the device");
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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("source[0 + (1 + 2)]"), "the offsets of a chain of slices add up");
        Assert.That(hlsl, Does.Contain("(__length_source - 1)"), "the length of a slice runs from its offset to the end of the buffer");
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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("[numthreads(128, 1, 1)]"), "the kernel says how many threads a group of it has");
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
        const string Unknown = @"
using System;

public static unsafe class Builds
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var point = new Vec2(1, 2);
        destination[index] = point.X;
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

        Assert.That(() => KernelTranspiler.Transpile(Unknown), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("structure the kernel declares"));
        Assert.That(() => KernelTranspiler.Transpile(Narrow), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("element type every backend has"));
    }
    [Test]
    public void Kernel_RefusesTheSyntaxThatOnlyMeansSomethingOnTheHost()
    {
        const string Relational = @"
using System;

public static unsafe class Relational
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        if (source[index] is > 4)
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

        Assert.That(() => KernelTranspiler.Transpile(Relational), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("switch over constants"));
        Assert.That(() => KernelTranspiler.Transpile(Wide), Throws.TypeOf<KernelTranslationException>().With.Message.Contains("cast to 'double'"));
    }
    [Test]
    public void Kernel_ReadsFromTheEndAndThroughRanges()
    {
        const string Source = @"
using System;

public static unsafe class Windows
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var tail = source[2..4];
        var rest = source[1..];
        var all = source[..];

        destination[index] = source[^1] + tail[0] + (uint)tail.Length + (uint)rest.Length + (uint)all.Length;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("__length_source - 1"), "an index from the end is the length less the index");
        Assert.That(hlsl, Does.Contain("(4 - 2)"), "a range runs from its start to its end");
        Assert.That(hlsl, Does.Contain("(__length_source - 1)"), "a range with no end runs to the end of the buffer");
        Assert.That(hlsl, Does.Not.Contain(".."), "the ranges themselves have no representation on the device");
    }
    [Test]
    public void Kernel_DeclaresAStructAsAValueTypeOfTheDevice()
    {
        const string Source = @"
using System;

public struct Vec2
{
    public uint X;
    public uint Y;

    public Vec2(uint x, uint y)
    {
        X = x;
        Y = y;
    }

    public static Vec2 operator +(Vec2 left, Vec2 right) => new Vec2(left.X + right.X, left.Y + right.Y);

    public uint Sum()
    {
        return X + Y;
    }

    public void Shift(uint amount)
    {
        X += amount;
    }
}

public static unsafe class Vectors
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var left = new Vec2(source[index], 1);
        var total = left + new Vec2(2, 3);
        total.Shift(4);
        destination[index] = total.Sum();
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("struct Vec2"), "the struct is a type of the device");
        Assert.That(hlsl, Does.Contain("Vec2_ctor(uint x, uint y)"), "a constructor is the way a value is built");
        Assert.That(hlsl, Does.Contain("Vec2_op_Addition(Vec2 left, Vec2 right)"), "an operator is a function of the module");
        Assert.That(hlsl, Does.Contain("void Vec2_Shift(inout Vec2 self"), "a method that writes a field mutates the value");
        Assert.That(hlsl, Does.Contain("Vec2_ctor(source[__gubbinsIndex], 1)"), "a value is built without an allocation");
    }

    [Test]
    public void Kernel_DeclaresPropertiesStaticMethodsAndHelpers()
    {
        const string Source = @"
using System;

public struct Inner
{
    public uint Value;
}

public struct Outer
{
    public Inner Item;
    public uint Scale;

    public Outer(uint value, uint scale)
    {
        Item.Value = value;
        Scale = scale;
    }

    public uint Total => Item.Value * Scale;

    public static uint Twice(uint value) => value * 2;

    public uint Read()
    {
        return Total;
    }
}

public struct Holder<T>
{
    public T Value;

    public T Get()
    {
        return Value;
    }
}

public static unsafe class Values
{
    private static uint Helper(uint value)
    {
        return value + 7;
    }

    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var outer = new Outer(Helper(source[index]), 3);
        var inner = new Inner();
        inner.Value = outer.Total;
        var holder = new Holder<uint>();
        holder.Value = Outer.Twice(inner.Value);
        destination[index] = holder.Get() + outer.Read();
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("struct Inner"), "a struct that a struct holds is declared before it");
        Assert.That(hlsl.IndexOf("struct Inner", StringComparison.Ordinal), Is.LessThan(hlsl.IndexOf("struct Outer", StringComparison.Ordinal)), "the order follows what holds what");
        Assert.That(hlsl, Does.Contain("uint Outer_Total(Outer self)"), "a read only property is a function that reads the value it is called on");
        Assert.That(hlsl, Does.Contain("self.Item.Value * self.Scale"), "the body of the property reads the fields of the value");
        Assert.That(hlsl, Does.Contain("uint Outer_Twice(uint value)"), "a static method stays static");
        Assert.That(hlsl, Does.Contain("struct Holder_uint"), "a generic struct is instantiated with the arguments the kernel uses it with");
        Assert.That(hlsl, Does.Contain("uint Helper(uint value)"), "a helper method of the kernel's class is a function of the module");
        Assert.That(hlsl, Does.Contain("Holder_uint holder;"), "a value with no arguments is declared, which is what a generic one needs");
    }

    [Test]
    public void Kernel_TakesAStructureAsOneValue()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

[GpuStruct]
public struct Step
{
    public float Damping;
    public float Offset;
}

public static partial class Motion
{
    [GpuKernel]
    public static void Scale(int index, ReadOnlySpan<float> source, Span<float> destination, in Step step)
    {
        destination[index] = source[index] * step.Damping + step.Offset;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<Step> __gubbinsParams : register(u2);"), "the structure is a block of one element, bound after the buffers");
        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<uint> __gubbinsLengths : register(u3);"), "the counts are bound after it, because the binding is taken");
        Assert.That(hlsl, Does.Contain("Step step = __gubbinsParams[0];"), "the entry point copies it into a local");
        Assert.That(hlsl, Does.Contain("source[__gubbinsIndex] * step.Damping + step.Offset"), "the body reads the structure like any value");
    }

    [Test]
    public void Kernel_RunsWithTheStructureItTakesOnTheDeviceTheMachineHas()
    {
        const int Count = 256;
        const string Source = @"
using System;
using Gubbins.Graphic;

[GpuStruct]
public struct Step
{
    public float Damping;
    public float Offset;
}

public static partial class Motion
{
    [GpuKernel]
    public static void Scale(int index, ReadOnlySpan<float> source, Span<float> destination, in Step step)
    {
        destination[index] = source[index] * step.Damping + step.Offset;
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Source, "Scaled"))
        {
            return;
        }

        try
        {
            using (device)
            using (var memory = device.CreateMemory())
            using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scaled"))
            {
                Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

                var values = new float[Count];

                for (var index = 0; index < Count; index++)
                {
                    values[index] = index;
                }

                var input = memory.Allocate((nuint)(Count * sizeof(float)), GpuAccess.Shared);
                var output = memory.Allocate((nuint)(Count * sizeof(float)), GpuAccess.Shared);
                var lengths = memory.Allocate(16, GpuAccess.Shared);
                var parameters = memory.Allocate((nuint)(2 * sizeof(float)), GpuAccess.Shared);

                memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
                memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

                foreach (var (damping, offset) in new[] { (2f, 1f), (5f, -3f) })
                {
                    memory.Upload(parameters, MemoryMarshal.AsBytes(new[] { damping, offset }.AsSpan()));

                    Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths, parameters), Is.True, $"the dispatch with damping {damping} ran");
                    memory.MarkWrittenByGpu(output);
                    memory.Synchronize();

                    var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

                    for (var index = 0; index < Count; index++)
                    {
                        Assert.That(result[index], Is.EqualTo(values[index] * damping + offset).Within(0.001f), $"element {index} with damping {damping} and offset {offset}");
                    }
                }
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Kernel_ResolvesAHelperDeclaredInAnotherClass()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

public static class Maths
{
    [GpuCompatible]
    public static uint Twice(uint value) => Scale(value * 3);
}

public static class Scale
{
    [GpuCompatible]
    public static uint Scale(uint value) => value * 2;
}

public static partial class Kernels
{
    [GpuKernel]
    public static void Add(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = Maths.Twice(source[index]);
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("uint Twice(uint value)"), "the helper of another class is a function of the program");
        Assert.That(hlsl, Does.Contain("uint Scale(uint value)"), "the helper that helper calls is one too");
        Assert.That(hlsl, Does.Contain("return Scale(value * 3u);"), "a qualified call is emitted as the free function it becomes");
        Assert.That(hlsl, Does.Not.Contain("Maths.Twice"), "the name the call was qualified with has no meaning on the device");
    }

    [Test]
    public void Kernel_TellsApartTwoHelpersOfTheSameNameInDifferentClasses()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

public static class First
{
    [GpuCompatible]
    public static uint Bump(uint value) => value + 1;
}

public static class Second
{
    [GpuCompatible]
    public static uint Bump(uint value) => value + 100;
}

public static partial class Kernels
{
    [GpuKernel]
    public static void Add(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = First.Bump(source[index]) + Second.Bump(source[index]);
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("uint First_Bump(uint value)"), "a name two classes share is qualified by the class that declares it");
        Assert.That(hlsl, Does.Contain("uint Second_Bump(uint value)"), "the other class keeps the same rule");
        Assert.That(hlsl, Does.Contain("return value + 1u;"), "the first helper is the body of the first class");
        Assert.That(hlsl, Does.Contain("return value + 100u;"), "the second helper is the body of the second class");
        Assert.That(hlsl, Does.Contain("First_Bump(source[__gubbinsIndex]) + Second_Bump(source[__gubbinsIndex])"), "each call reaches the helper of the class it names");
    }

    [Test]
    public void Kernel_WalksACollectionWithForeach()
    {
        const string Source = @"
using System;

public static unsafe class Walks
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var total = 0u;

        foreach (var value in source.Slice(1, 2))
        {
            total = total + value;
        }

        foreach (uint other in source)
        {
            total = total + other;
        }

        destination[index] = total;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("for (int __gubbinsLoop0 = 0; __gubbinsLoop0 < 2; __gubbinsLoop0 = __gubbinsLoop0 + 1)"), "a loop over a slice counts the elements of the slice");
        Assert.That(hlsl, Does.Contain("uint value = source[__gubbinsLoop0 + 1];"), "the element of a slice is the buffer at the offset");
        Assert.That(hlsl, Does.Contain("for (int __gubbinsLoop1 = 0; __gubbinsLoop1 < __length_source; __gubbinsLoop1 = __gubbinsLoop1 + 1)"), "a loop over a buffer counts its elements");
        Assert.That(hlsl, Does.Contain("uint other = source[__gubbinsLoop1];"), "the element of a buffer is its element");
    }

    [Test]
    public void Kernel_SumsAForeachOnTheDeviceTheMachineHas()
    {
        const int Count = 4096;
        const string Source = @"
using System;

public static unsafe class Walks
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var total = 0u;

        foreach (var value in source.Slice(0, 16))
        {
            total = total + value;
        }

        destination[index] = total;
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Source, "Walks"))
        {
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Walks"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];
            var expected = 0u;

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)(index % 7);

                if (index < 16)
                {
                    expected += values[index];
                }
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var index = 0; index < Count; index++)
            {
                Assert.That(result[index], Is.EqualTo(expected), $"the loop added the first elements up at element {index}");
            }
        }

        Directory.Delete(directory, true);
    }

    [Test]
    public void Kernel_WritesThroughRefAndOutParameters()
    {
        const string Source = @"
using System;

public static unsafe class Writes
{
    private static void Bump(ref uint value)
    {
        value = value + 1;
    }

    private static void Split(uint value, out uint high, out uint low)
    {
        high = value / 2;
        low = value - (value / 2);
    }

    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var value = source[index];
        Bump(ref value);
        uint high;
        uint low;
        Split(value, out high, out low);
        destination[index] = high + low + value;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("void Bump(inout uint value)"), "a parameter a helper writes back to is inout");
        Assert.That(hlsl, Does.Contain("void Split(uint value, out uint high, out uint low)"), "a parameter a helper only writes is out");
        Assert.That(hlsl, Does.Contain("Bump(value);"), "the caller passes the variable itself");
        Assert.That(hlsl, Does.Contain("Split(value, high, low);"), "the caller passes the variables the call writes back to");
    }

    [Test]
    public void Kernel_DeclaresTheVariablesACallWritesBackTo()
    {
        const string Source = @"
using System;

public static unsafe class Writes
{
    private static void Split(uint value, out uint high, out uint low)
    {
        high = value / 2;
        low = value - (value / 2);
    }

    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        Split(source[index], out var high, out var low);
        destination[index] = high + low;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("uint high;"), "the variable the call writes back to is declared with the type the call takes");
        Assert.That(hlsl, Does.Contain("Split(source[__gubbinsIndex], high, low);"), "the call passes the variables themselves");
    }

    [Test]
    public void Kernel_BranchesWithASwitchExpression()
    {
        const string Source = @"
using System;

public static unsafe class Grades
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var grade = source[index] switch
        {
            0 => 100u,
            1 => 50u,
            _ => 0u,
        };

        if (source[index] is 2)
        {
            grade = grade + 1;
        }

        destination[index] = grade;
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("== 0u ? 100u"), "a switch expression is a chain of comparisons");
        Assert.That(hlsl, Does.Contain("== 2u"), "a constant pattern is an equality");
        Assert.That(hlsl, Does.Not.Contain("switch"), "the device branches with the comparisons");
    }

    [Test]
    public void Kernel_RunsASwitchExpressionOnTheDeviceTheMachineHas()
    {
        const int Count = 4096;
        const string Source = @"
using System;

public static unsafe class Grades
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = (source[index] % 4) switch
        {
            0 => 7u,
            1 => 11u,
            2 => 13u,
            _ => 17u,
        };
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Source, "Grades"))
        {
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Grades"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];
            var expected = new uint[] { 7, 11, 13, 17 };

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var index = 0; index < Count; index++)
            {
                Assert.That(result[index], Is.EqualTo(expected[values[index] % 4]), $"the arm of element {index} was taken");
            }
        }

        Directory.Delete(directory, true);
    }
    [Test]
    public void Kernel_DeclaresAFixedSizeLocalArray()
    {
        const string Source = @"
using System;

public static unsafe class Scratch
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var values = new uint[4] { 1, 2, 3, 4 };
        destination[index] = values[2] + source[index];
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("uint values[4];"), "the array is sized where it is declared");
        Assert.That(hlsl, Does.Contain("values[2] = 3u;"), "the values it starts with are written into it one element at a time");
    }

    [Test]
    public void Kernel_MapsTheWaveCalls()
    {
        const string Source = @"
using System;

public static unsafe class Waves
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = GpuWave.Sum(source[index]) + GpuWave.Max(source[index]);
    }
}
";

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("WaveActiveSum("), "a wave sum is the intrinsic every backend has");
        Assert.That(hlsl, Does.Contain("WaveActiveMax("));
    }

    [Test]
    public void Kernel_RunsAValueTypeAndAnArrayOnTheDeviceTheMachineHas()
    {
        const int Count = 4096;
        const string Source = @"
using System;

public struct Pair
{
    public uint Left;
    public uint Right;

    public Pair(uint left, uint right)
    {
        Left = left;
        Right = right;
    }

    public static Pair operator +(Pair a, Pair b) => new Pair(a.Left + b.Left, a.Right + b.Right);

    public void Scale(uint factor)
    {
        Left *= factor;
    }

    public uint Total()
    {
        return Left + Right;
    }
}

public static unsafe class Values
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        var values = new uint[3] { 1, 2, 3 };
        var total = new Pair(source[index], values[1]) + new Pair(2, 3);
        total.Scale(2);
        destination[index] = total.Total();
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Source, "Values"))
        {
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Values"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

            for (var index = 0; index < Count; index++)
            {
                // Pair(source, 2) + Pair(2, 3) then Left *= 2, so (source + 2) * 2 + (2 + 3).
                var expected = (values[index] + 2) * 2 + 5;

                Assert.That(result[index], Is.EqualTo(expected), $"the kernel wrote element {index}");
            }
        }

        Directory.Delete(directory, true);
    }

    [Test]
    public void Kernel_SubscribesAGroupThroughTheWaveOnTheDeviceTheMachineHas()
    {
        const int Count = 4096;
        const string Source = @"
using System;

public static unsafe class Waves
{
    public static void Kernel(int index, ReadOnlySpan<uint> source, Span<uint> destination)
    {
        destination[index] = GpuWave.Sum(1);
    }
}
";

        if (!TryUseDevice(out var device, out var directory, Source, "Waves"))
        {
            return;
        }

        // D3D12 compiles the artifact with the system compiler's cs_5_1 profile, which has no wave intrinsics: the kernel
        // loads on every backend that has the operation in its language, so a D3D12 machine skips rather than fails.
        if (device.Backend is D3D12Device)
        {
            device.Dispose();
            Directory.Delete(directory, true);
            Assert.Ignore("D3D12 compiles kernels with cs_5_1, which has no wave intrinsics.");
            return;
        }

        using (device)
        using (var memory = device.CreateMemory())
        using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Waves"))
        {
            Assert.That(kernel, Is.Not.Null, "the artifact the backend needs was loaded");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);

            memory.Upload(input, MemoryMarshal.AsBytes(new uint[Count].AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True);

            memory.MarkWrittenByGpu(output);
            memory.Synchronize();

            var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());
            var wave = result[0];

            Assert.That(wave, Is.GreaterThan(0u), "a wave has threads in it");
            Assert.That(KernelTranspiler.THREADS_PER_GROUP % wave, Is.Zero, "a group holds a whole number of waves");

            for (var index = 0; index < Count; index++)
            {
                Assert.That(result[index], Is.EqualTo(wave), $"the wave a thread belongs to added its ones up at element {index}");
            }
        }

        Directory.Delete(directory, true);
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

            // The artifacts every machine produces: the HLSL D3D12 compiles where it loads, the SPIR-V Vulkan loads, and
            // the sources OpenGL and Metal compile themselves.
            foreach (var extension in new[] { ".hlsl", ".spv", ".comp", ".metal" })
            {
                Assert.That(names, Does.Contain("Scale" + extension), $"the {extension} artifact is one of the targets");
            }

            // The text CUDA compiles is written by this layer, so a machine that has it produces it. The test checks the
            // artifact when it is there.
            if (names.Contains("Scale.ptx"))
            {
                Assert.That(new FileInfo(Path.Combine(directory, "Scale.ptx")).Length, Is.GreaterThan(0), "the CUDA artifact is the PTX text the driver compiles");
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
            KernelCompiler.Compile(Tiled, directory, "Tiled");

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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("groupshared uint tile[64];"), "the array every thread of the group addresses is a module declaration");
        Assert.That(hlsl, Does.Contain("GroupMemoryBarrierWithGroupSync();"), "the barrier becomes the one call every backend has");
        Assert.That(hlsl, Does.Contain("tile[0] + 64u"), "the length of a shared array is the number it was declared with");
        Assert.That(hlsl, Does.Not.Contain("Shared<"), "the declaration has no representation on the device");
    }

    [Test]
    public void Kernel_AvoidsTheNamesMetalReserves()
    {
        const string Source = @"
using System;

public static unsafe class Reserved
{
    public static void Kernel(int index, ReadOnlySpan<uint> device, Span<uint> destination)
    {
        var half = 2u;
        var tile = Shared<uint>(64);

        tile[index % 64] = device[index] + half;
        Barrier();
        destination[index] = tile[index % 64];
    }
}
";

        var metal = Metal(Source);

        // A buffer named device is renamed, while the address-space qualifier the emitter writes keeps its meaning: only
        // the names the program declares are touched.
        Assert.That(metal, Does.Contain("const device uint* device_ [[buffer(0)]]"), "a buffer named device is renamed but the qualifier is not");
        Assert.That(metal, Does.Contain("device_[__gubbinsIndex]"), "every use of the renamed buffer follows the declaration");
        Assert.That(metal, Does.Contain("int half_ = 2u;"), "a local named after the Metal scalar type is renamed");

        // Metal has no module scope for group memory, so it is a local of the kernel and has to follow the signature.
        var signature = metal.IndexOf("kernel void gubbins_main(", StringComparison.Ordinal);

        Assert.That(metal.IndexOf("threadgroup uint tile[64];", StringComparison.Ordinal), Is.GreaterThan(signature), "the group memory is declared inside the kernel");
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
            KernelCompiler.Compile(Reduction, directory, "Reduce");

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

        var hlsl = Hlsl(Source);

        Assert.That(hlsl, Does.Contain("RWStructuredBuffer<float4> source"), "an element is whatever type the language knows");
        Assert.That(hlsl, Does.Contain("value * value"), "the arithmetic on a vector is the language's own");
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
        const string Structure = @"
using System;
using Gubbins.Graphic;

[GpuStruct]
public struct Step
{
    public float Damping;
    public float Offset;
}

public static partial class Motion
{
    [GpuKernel]
    public static void Scale(int index, ReadOnlySpan<float> source, Span<float> destination, in Step step)
    {
        destination[index] = source[index] * step.Damping + step.Offset;
    }
}
";
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

        var directory = Directory.CreateTempSubdirectory("gubbins-backends-").FullName;

        try
        {
            KernelCompiler.Compile(Kernel, directory, "Scale");
            KernelCompiler.Compile(Reduction, directory, "Reduce");
            KernelCompiler.Compile(Structure, directory, "Scaled");
            KernelCompiler.Compile(Counting, directory, "Histogram");
            KernelCompiler.Compile(Squaring, directory, "Square");

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
                VerifyScaleOnDeviceMemory(device, directory, name, Count);
                VerifyQueue(device, directory, name, Count);
                VerifyReduction(device, directory, name, Count);

                // The PTX the CUDA backend loads has no parameter block, so a kernel that takes a structure is not one it
                // can express.
                if (name != "CUDA")
                {
                    VerifyStructure(device, directory, name);
                }

                VerifyAtomic(device, directory, name);
                VerifyVector(device, directory, name);
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
    /// Runs the scale kernel through a queue, which records without waiting and waits once.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    /// <param name="count">The number of elements to work on.</param>
    private static void VerifyQueue(GpuDevice device, string directory, string name, int count)
    {
        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scale");
        using var queue = new GpuQueue();

        Assert.That(kernel, Is.Not.Null, $"{name}: the kernel for the queue was loaded");

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

        Assert.That(queue.Dispatch(kernel!, input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the queue recorded the dispatch");
        Assert.That(queue.Wait(), Is.True, $"{name}: the queue waited for the dispatch");

        memory.MarkWrittenByGpu(output);
        Assert.That(memory.Synchronize(), Is.True, $"{name}: the queued output was synchronized");

        var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());
        var addend = (uint)Math.Min(count, 4096);

        for (var index = 0; index < count; index++)
        {
            Assert.That(result[index], Is.EqualTo(values[index] + addend), $"{name}: queued element {index}");
        }
    }

    /// <summary>
    /// Runs the scale kernel with an output that lives in device-only memory on one backend.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    /// <param name="count">The number of elements to work on.</param>
    private static void VerifyScaleOnDeviceMemory(GpuDevice device, string directory, string name, int count)
    {
        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scale");

        Assert.That(kernel, Is.Not.Null, $"{name}: the artifact for the device-only output was loaded");

        var input = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Sync);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var values = new uint[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = (uint)index;
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { (uint)count, (uint)count, 0, 0 }.AsSpan()));

        Assert.That(kernel!.TryDispatch(input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the dispatch over device-only output ran");
        memory.MarkWrittenByGpu(output);
        Assert.That(memory.Synchronize(), Is.True, $"{name}: the device-only output was copied back");

        var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());
        var addend = (uint)Math.Min(count, 4096);

        for (var index = 0; index < count; index++)
        {
            Assert.That(result[index], Is.EqualTo(values[index] + addend), $"{name}: device-only element {index}");
        }
    }

    /// <summary>
    /// Runs the kernel that takes a structure on one backend, which binds the structure after the buffers.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    private static void VerifyStructure(GpuDevice device, string directory, string name)
    {
        const int count = 256;

        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scaled");

        Assert.That(kernel, Is.Not.Null, $"{name}: the structure kernel was loaded");

        var input = memory.Allocate((nuint)(count * sizeof(float)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(count * sizeof(float)), GpuAccess.Shared);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var parameters = memory.Allocate((nuint)(2 * sizeof(float)), GpuAccess.Shared);
        var values = new float[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = index;
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { count, count, 0, 0 }.AsSpan()));

        foreach (var (damping, offset) in new[] { (2f, 1f), (5f, -3f) })
        {
            memory.Upload(parameters, MemoryMarshal.AsBytes(new[] { damping, offset }.AsSpan()));

            Assert.That(kernel!.TryDispatch(input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths, parameters), Is.True, $"{name}: the structure dispatch ran");

            memory.MarkWrittenByGpu(output);
            Assert.That(memory.Synchronize(), Is.True, $"{name}: the structure output was copied back");

            var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

            for (var index = 0; index < count; index++)
            {
                Assert.That(result[index], Is.EqualTo(values[index] * damping + offset).Within(0.001f), $"{name}: structure element {index}");
            }
        }
    }

    /// <summary>
    /// Runs the kernel that counts into buckets with an atomic add on one backend.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    private static void VerifyAtomic(GpuDevice device, string directory, string name)
    {
        const int count = 65536;
        const int buckets = 16;

        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Histogram");

        Assert.That(kernel, Is.Not.Null, $"{name}: the atomic kernel was loaded");

        var input = memory.Allocate((nuint)(count * sizeof(uint)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(buckets * sizeof(uint)), GpuAccess.Shared);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var values = new uint[count];

        for (var index = 0; index < count; index++)
        {
            values[index] = (uint)index;
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(output, MemoryMarshal.AsBytes(new uint[buckets].AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { count, buckets, 0, 0 }.AsSpan()));

        Assert.That(kernel!.TryDispatch(input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the atomic dispatch ran");

        memory.MarkWrittenByGpu(output);
        Assert.That(memory.Synchronize(), Is.True, $"{name}: the atomic output was copied back");

        var result = MemoryMarshal.Cast<byte, uint>(output.CpuView());

        for (var bucket = 0; bucket < buckets; bucket++)
        {
            Assert.That(result[bucket], Is.EqualTo(count / buckets), $"{name}: bucket {bucket}");
        }
    }

    /// <summary>
    /// Runs the kernel whose element is a vector on one backend.
    /// </summary>
    /// <param name="device">The device to run on.</param>
    /// <param name="directory">The directory the artifacts are in.</param>
    /// <param name="name">The backend, for the assertions.</param>
    private static void VerifyVector(GpuDevice device, string directory, string name)
    {
        const int count = 8192;
        var elements = count * 4;

        using var memory = device.CreateMemory();
        using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Square");

        Assert.That(kernel, Is.Not.Null, $"{name}: the vector kernel was loaded");

        var input = memory.Allocate((nuint)(elements * sizeof(float)), GpuAccess.Shared);
        var output = memory.Allocate((nuint)(elements * sizeof(float)), GpuAccess.Shared);
        var lengths = memory.Allocate(16, GpuAccess.Shared);
        var values = new float[elements];

        for (var index = 0; index < elements; index++)
        {
            values[index] = index % 7 * 1.5f;
        }

        memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
        memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { count, count, 0, 0 }.AsSpan()));

        Assert.That(kernel!.TryDispatch(input, output, (uint)(count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the vector dispatch ran");

        memory.MarkWrittenByGpu(output);
        Assert.That(memory.Synchronize(), Is.True, $"{name}: the vector output was copied back");

        var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

        for (var index = 0; index < elements; index++)
        {
            var expected = values[index] * values[index];
            Assert.That(result[index], Is.EqualTo(expected).Within(1e-4f * MathF.Max(1f, expected)), $"{name}: vector element {index}");
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
    /// Emits the HLSL of the kernel a source declares, which is the artifact D3D12 compiles when it loads it.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <returns>The HLSL the program is written as.</returns>
    private static string Hlsl(string source) => new KernelHlsl().Emit(KernelTranspiler.Transpile(source));

    /// <summary>
    /// Emits the Metal source of the kernel a source declares, which is the artifact a Metal device compiles when it creates
    /// the pipeline.
    /// </summary>
    /// <param name="source">The C# source that declares the kernel.</param>
    /// <returns>The Metal source the program is written as.</returns>
    private static string Metal(string source) => new KernelMetal().Emit(KernelTranspiler.Transpile(source));

    [Test]
    public unsafe void GpuDevice_RunsOnAnEngineOwnedMetalDevice()
    {
        if (!OperatingSystem.IsMacOS() || !MetalDevice.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Metal device.");
            return;
        }

        using (owner)
        using (var device = GpuDevice.FromMetal(owner.Provider))
        {
            VerifyEngineOwnedDevice(device, "Metal", (memory, source) =>
            {
                Assert.That(owner.Provider.TryImport((nint)source.Storage.DeviceHandle, source.Storage.Offset, source.Length, out var imported), Is.True, "the engine buffer is imported");
                return memory.Wrap(imported, GpuAccess.Shared);
            });
        }
    }

    [Test]
    public unsafe void GpuDevice_RunsOnAnEngineOwnedD3D12Device()
    {
        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        using (owner)
        using (var device = GpuDevice.FromD3D12(owner.Provider))
        {
            VerifyEngineOwnedDevice(device, "D3D12", (memory, source) =>
            {
                Assert.That(owner.Provider.TryGetResource(source.Storage, out var resource, out _), Is.True, "the engine resource is readable");
                Assert.That(owner.Provider.TryImport(resource, source.Storage.Offset, source.Length, (nint)source.Storage.SegmentHost, UnifiedMemoryKind.SharedCoherent, out var imported), Is.True, "the engine resource is imported");
                return memory.Wrap(imported, GpuAccess.Shared);
            });
        }
    }

    [Test]
    public unsafe void GpuDevice_RunsOnAnEngineOwnedVulkanDevice()
    {
        VulkanDevice? owner;

        try
        {
            if (!VulkanDevice.TryCreate(out owner) || owner is null)
            {
                Assert.Ignore("This machine has no Vulkan device.");
                return;
            }
        }
        catch (Exception)
        {
            Assert.Ignore("This machine has no Vulkan loader.");
            return;
        }

        using (owner)
        using (var device = GpuDevice.FromVulkan(owner.Provider))
        {
            VerifyEngineOwnedDevice(device, "Vulkan", (memory, source) =>
            {
                Assert.That(owner.Provider.TryGetVulkanBuffer(source.Storage, out var buffer, out _), Is.True, "the engine buffer is readable");
                Assert.That(owner.Provider.TryImport(buffer, source.Storage.Offset, source.Length, (nint)source.Storage.SegmentHost, UnifiedMemoryKind.SharedCoherent, out var imported), Is.True, "the engine buffer is imported");
                return memory.Wrap(imported, GpuAccess.Shared);
            });
        }
    }

    [Test]
    public void Gpu_CreatesOnAnEngineOwnedD3D12Device()
    {
        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        using (owner)
        using (var gpu = Gpu.Create(GpuDevice.FromD3D12(owner.Provider)))
        {
            Assert.That(gpu.Description, Is.EqualTo("D3D12"), "the layer uses the device the engine handed it");

            using var buffer = gpu.Upload(new uint[] { 1, 2, 3, 4 });

            Assert.That(buffer.CpuSpan.ToArray(), Is.EqualTo(new uint[] { 1, 2, 3, 4 }), "the layer allocates through the engine device");
        }
    }

    [Test]
    public void MetalTexture_KernelSamplesHardwareTexture()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

public static class Textured
{
    public static void Kernel(int index, ReadOnlySpan<float2> source, Span<float4> destination, GpuTexture2D texture, GpuSamplerState sampler)
    {
        destination[index] = texture.Sample(sampler, source[index]);
    }
}
";

        if (!OperatingSystem.IsMacOS() || !MetalDevice.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no Metal device.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-texture-").FullName;

        try
        {
            KernelCompiler.Compile(Source, directory, "Textured");

            using (owner)
            using (var device = GpuDevice.FromMetal(owner.Provider))
            using (var memory = device.CreateMemory())
            using (var texture = owner.Provider.CreateTexture(2, 2, GpuTextureFormat.Rgba8Unorm))
            using (var sampler = owner.Provider.CreateSampler(new GpuSampler(GpuTextureFilter.Nearest, GpuAddressMode.Clamp)))
            using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "Textured"))
            {
                Assert.That(kernel, Is.Not.Null, "the texture kernel loads on Metal");

                texture.Upload(new byte[]
                {
                    255, 0, 0, 255, 0, 255, 0, 255,
                    0, 0, 255, 255, 255, 255, 255, 255
                }, 8);

                var input = memory.Allocate(8, GpuAccess.Shared);
                var output = memory.Allocate(16, GpuAccess.Shared);
                var lengths = memory.Allocate(16, GpuAccess.Shared);

                memory.Upload(input, MemoryMarshal.AsBytes(new float[] { 0.25f, 0.25f }.AsSpan()));
                memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { 1, 1, 0, 0 }.AsSpan()));

                Assert.That(kernel!.TryDispatch(input, output, lengths, null, texture, sampler, 1), Is.True, "the hardware texture dispatch ran");

                memory.MarkWrittenByGpu(output);
                Assert.That(memory.Synchronize(), Is.True, "the texture output was synchronized");

                var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

                Assert.That(result[0], Is.EqualTo(1f).Within(0.001f), "the red channel came from the sampled texel");
                Assert.That(result[1], Is.EqualTo(0f).Within(0.001f), "the green channel came from the sampled texel");
                Assert.That(result[2], Is.EqualTo(0f).Within(0.001f), "the blue channel came from the sampled texel");
                Assert.That(result[3], Is.EqualTo(1f).Within(0.001f), "the alpha channel came from the sampled texel");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GlTexture_KernelSamplesHardwareTexture()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

public static class GlTextured
{
    public static void Kernel(int index, ReadOnlySpan<float2> source, Span<float4> destination, GpuTexture2D texture, GpuSamplerState sampler)
    {
        destination[index] = texture.Sample(sampler, source[index]);
    }
}
";

        if (!GlDevice.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no OpenGL context.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-gltexture-").FullName;

        try
        {
            KernelCompiler.Compile(Source, directory, "GlTextured");

            using (var device = new GpuDevice(owner, owner.Provider.GetProvider(), 0, false, 0, "OpenGL"))
            using (var memory = device.CreateMemory())
            using (var texture = owner.Provider.CreateTexture(2, 2, GpuTextureFormat.Rgba8Unorm))
            using (var sampler = owner.Provider.CreateSampler(new GpuSampler(GpuTextureFilter.Nearest, GpuAddressMode.Clamp)))
            using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "GlTextured"))
            {
                Assert.That(kernel, Is.Not.Null, "the texture kernel loads on OpenGL");

                texture.Upload(new byte[]
                {
                    255, 0, 0, 255, 0, 255, 0, 255,
                    0, 0, 255, 255, 255, 255, 255, 255
                }, 8);

                var input = memory.Allocate(8, GpuAccess.Shared);
                var output = memory.Allocate(16, GpuAccess.Shared);
                var lengths = memory.Allocate(16, GpuAccess.Shared);

                memory.Upload(input, MemoryMarshal.AsBytes(new float[] { 0.25f, 0.25f }.AsSpan()));
                memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { 1, 1, 0, 0 }.AsSpan()));

                Assert.That(kernel!.TryDispatchOpenGl(input, output, lengths, null, texture, sampler, 1), Is.True, "the hardware texture dispatch ran");

                memory.MarkWrittenByGpu(output);
                Assert.That(memory.Synchronize(), Is.True, "the texture output was synchronized");

                var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

                Assert.That(result[0], Is.EqualTo(1f).Within(0.001f), "the red channel came from the sampled texel");
                Assert.That(result[1], Is.EqualTo(0f).Within(0.001f), "the green channel came from the sampled texel");
                Assert.That(result[2], Is.EqualTo(0f).Within(0.001f), "the blue channel came from the sampled texel");
                Assert.That(result[3], Is.EqualTo(1f).Within(0.001f), "the alpha channel came from the sampled texel");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void D3D12Texture_KernelSamplesHardwareTexture()
    {
        const string Source = @"
using System;
using Gubbins.Graphic;

public static class D3D12Textured
{
    public static void Kernel(int index, ReadOnlySpan<float2> source, Span<float4> destination, GpuTexture2D texture, GpuSamplerState sampler)
    {
        destination[index] = texture.Sample(sampler, source[index]);
    }
}
";

        if (!OperatingSystem.IsWindows() || !D3D12Device.TryCreate(out var owner) || owner is null)
        {
            Assert.Ignore("This machine has no D3D12 device.");
            return;
        }

        var directory = Directory.CreateTempSubdirectory("gubbins-d3d12texture-").FullName;

        try
        {
            KernelCompiler.Compile(Source, directory, "D3D12Textured");

            using (owner)
            using (var device = GpuDevice.FromD3D12(owner.Provider))
            using (var memory = device.CreateMemory())
            using (var texture = owner.Provider.CreateTexture(2, 2, GpuTextureFormat.Rgba8Unorm))
            using (var sampler = owner.Provider.CreateSampler(new GpuSampler(GpuTextureFilter.Nearest, GpuAddressMode.Clamp)))
            using (var kernel = GpuKernel.LoadFromDirectory(device, directory, "D3D12Textured"))
            {
                Assert.That(kernel, Is.Not.Null, "the texture kernel loads on D3D12");

                texture.Upload(new byte[]
                {
                    255, 0, 0, 255, 0, 255, 0, 255,
                    0, 0, 255, 255, 255, 255, 255, 255
                }, 8);

                var input = memory.Allocate(8, GpuAccess.Shared);
                var output = memory.Allocate(16, GpuAccess.Shared);
                var lengths = memory.Allocate(16, GpuAccess.Shared);

                memory.Upload(input, MemoryMarshal.AsBytes(new float[] { 0.25f, 0.25f }.AsSpan()));
                memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { 1, 1, 0, 0 }.AsSpan()));

                Assert.That(kernel!.TryDispatchD3D12(input, output, lengths, null, texture, sampler, 1), Is.True, "the hardware texture dispatch ran");

                memory.MarkWrittenByGpu(output);
                Assert.That(memory.Synchronize(), Is.True, "the texture output was synchronized");

                var result = MemoryMarshal.Cast<byte, float>(output.CpuView());

                Assert.That(result[0], Is.EqualTo(1f).Within(0.001f), "the red channel came from the sampled texel");
                Assert.That(result[1], Is.EqualTo(0f).Within(0.001f), "the green channel came from the sampled texel");
                Assert.That(result[2], Is.EqualTo(0f).Within(0.001f), "the blue channel came from the sampled texel");
                Assert.That(result[3], Is.EqualTo(1f).Within(0.001f), "the alpha channel came from the sampled texel");
            }
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Runs a kernel on an engine-owned device, with one of the buffers wrapped from a resource the engine owns.
    /// </summary>
    /// <param name="device">The engine-owned device.</param>
    /// <param name="name">The backend, for the assertions.</param>
    /// <param name="import">Wraps one block as an imported block.</param>
    private static void VerifyEngineOwnedDevice(GpuDevice device, string name, Func<GpuMemory, GpuBlock, GpuBlock> import)
    {
        const int Count = 4096;

        var directory = Directory.CreateTempSubdirectory("gubbins-engine-").FullName;

        try
        {
            KernelCompiler.Compile(Kernel, directory, "Scale");

            using var memory = device.CreateMemory();
            using var kernel = GpuKernel.LoadFromDirectory(device, directory, "Scale");

            Assert.That(kernel, Is.Not.Null, $"{name}: the kernel loads on the engine device");

            var input = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var output = memory.Allocate((nuint)(Count * sizeof(uint)), GpuAccess.Shared);
            var lengths = memory.Allocate(16, GpuAccess.Shared);
            var values = new uint[Count];

            for (var index = 0; index < Count; index++)
            {
                values[index] = (uint)index;
            }

            memory.Upload(input, MemoryMarshal.AsBytes(values.AsSpan()));
            memory.Upload(lengths, MemoryMarshal.AsBytes(new uint[] { Count, Count, 0, 0 }.AsSpan()));

            var imported = import(memory, output);

            Assert.That(kernel!.TryDispatch(input, imported, (uint)(Count / KernelTranspiler.THREADS_PER_GROUP), lengths), Is.True, $"{name}: the dispatch over the imported block ran");

            memory.MarkWrittenByGpu(imported);
            Assert.That(memory.Synchronize(), Is.True, $"{name}: the imported block was synchronized");

            var result = MemoryMarshal.Cast<byte, uint>(imported.CpuView());
            var addend = (uint)Math.Min(Count, 4096);

            for (var index = 0; index < Count; index++)
            {
                Assert.That(result[index], Is.EqualTo(values[index] + addend), $"{name}: imported element {index}");
            }

            Assert.That(memory.Release(imported), Is.True, $"{name}: the imported wrapper is released");
            Assert.That(memory.Release(output), Is.True, $"{name}: the owning block is released");
        }
        finally
        {
            Directory.Delete(directory, true);
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

        KernelCompiler.Compile(source, directory, name);

        return true;
    }

    /// <summary>
    /// Compiles the kernel the tests share.
    /// </summary>
    /// <param name="directory">The directory the artifacts are written to.</param>
    /// <returns>The artifacts that were written.</returns>
    private static IReadOnlyList<string> Compile(string directory)
        => KernelCompiler.Compile(Kernel, directory, "Scale");
}
