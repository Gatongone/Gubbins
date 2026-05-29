using System;

namespace Gubbins.Span.Tests;

[TestFixture]
public sealed class ParallelNumberReductionTests
{
    private static ISpanNumberOperations<int> CreateOperation()
    {
        var genericType = typeof(ISpanNumberOperations<int>).Assembly
            .GetType("Gubbins.Span.ParallelNumberOperations`1", throwOnError: true)!;
        var closedType = genericType.MakeGenericType(typeof(int));
        return (ISpanNumberOperations<int>)Activator.CreateInstance(closedType, nonPublic: true)!;
    }

    [Test]
    public void GetMax_ShouldReduceAcrossAllPartitions()
    {
        var op = CreateOperation();
        var values = CreateUnevenData();
        var expected = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] > expected)
            {
                expected = values[i];
            }
        }

        var actual = op.GetMax(values);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetMin_ShouldReduceAcrossAllPartitions()
    {
        var op = CreateOperation();
        var values = CreateUnevenData();
        var expected = values[0];
        for (var i = 1; i < values.Length; i++)
        {
            if (values[i] < expected)
            {
                expected = values[i];
            }
        }

        var actual = op.GetMin(values);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void GetMaxAndGetMin_ShouldHandleSmallSpans()
    {
        var op = CreateOperation();
        Span<int> values = stackalloc[] { 7, -3, 5 };

        Assert.That(op.GetMax(values), Is.EqualTo(7));
        Assert.That(op.GetMin(values), Is.EqualTo(-3));
    }

    [Test]
    public void GetMaxAndGetMin_ShouldThrowForEmptySpan()
    {
        var op = CreateOperation();
        var empty = Array.Empty<int>();

        Assert.That(() => op.GetMax(empty), Throws.ArgumentException);
        Assert.That(() => op.GetMin(empty), Throws.ArgumentException);
    }

    private static int[] CreateUnevenData()
    {
        var length = Environment.ProcessorCount * 3 + 5;
        var values = new int[length];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i % 2 == 0 ? -i * 11 : i * 7 - 100;
        }

        values[values.Length / 3] = int.MaxValue - 17;
        values[^2] = int.MinValue + 29;
        return values;
    }
}
