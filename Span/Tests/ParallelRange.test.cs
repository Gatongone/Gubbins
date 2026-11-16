namespace Gubbins.Span.Tests;

[TestFixture]
public sealed class ParallelRangeTests
{
    [Test]
    public void For_RepeatedDispatch_ShouldProcessEveryIndexExactlyOnce()
    {
        var counters = new int[1_000];

        foreach (var workers in new[] { 1, 2, 3, 4, 6, 9, 12 })
        {
            for (var iteration = 0; iteration < 5_000; iteration++)
            {
                Array.Clear(counters, 0, counters.Length);

                ParallelRange.For(counters.Length, workers, counters, static (start, length, state) =>
                {
                    for (var i = start; i < start + length; i++)
                    {
                        Interlocked.Increment(ref state[i]);
                    }
                });

                for (var i = 0; i < counters.Length; i++)
                {
                    Assert.That(counters[i], Is.EqualTo(1), $"workers={workers}, index={i}, iteration={iteration}");
                }
            }
        }
    }

    [Test]
    public void For_PartialRanges_ShouldCoverContiguousSlices()
    {
        const int length = 10_003;
        var coverage = new int[length];

        foreach (var workers in new[] { 2, 3, 4, 6, 9, 12 })
        {
            Array.Clear(coverage, 0, coverage.Length);

            ParallelRange.For(length, workers, coverage, static (start, count, state) =>
            {
                for (var i = start; i < start + count; i++)
                {
                    Interlocked.Increment(ref state[i]);
                }
            });

            for (var i = 0; i < coverage.Length; i++)
            {
                Assert.That(coverage[i], Is.EqualTo(1), $"workers={workers}, index={i}");
            }
        }
    }
}
