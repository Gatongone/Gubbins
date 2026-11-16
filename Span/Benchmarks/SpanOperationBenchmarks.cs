using BenchmarkDotNet.Attributes;
using Gubbins.Span;

namespace Gubbins.Span.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class SpanOperationBenchmarks
{
    /// <summary>
    /// Span length in elements. The smallest value is above the parallel threshold (1024) and SIMD width;
    /// the largest is large enough for memory bandwidth and scheduling costs to be amortized.
    /// </summary>
    [Params(16_384, 4_194_304)]
    public int Length { get; set; }

    private int[] m_Source = null!;
    private int[] m_Destination = null!;

    private ISpanNumberOperation<int> m_Serial = null!;
    private ISpanNumberOperation<int> m_Simd = null!;
    private ISpanNumberOperation<int> m_Parallel = null!;
    private ISpanNumberOperation<int> m_ParallelSimd = null!;

    [GlobalSetup]
    public void Setup()
    {
        m_Source = new int[Length];
        m_Destination = new int[Length];
        for (var i = 0; i < Length; i++)
        {
            m_Source[i] = i;
        }

        m_Serial = GetOperation(IterationMode.Serial);
        m_Simd = GetOperation(IterationMode.Simd);
        m_Parallel = GetOperation(IterationMode.Parallel);
        m_ParallelSimd = GetOperation(IterationMode.Parallel | IterationMode.Simd);
    }

    private static ISpanNumberOperation<int> GetOperation(IterationMode mask) =>
        SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(mask)!;

    [Benchmark(Baseline = true)]
    public void Serial() => m_Serial.Add(m_Source, 1, m_Destination);

    [Benchmark]
    public void Simd() => m_Simd.Add(m_Source, 1, m_Destination);

    [Benchmark]
    public void Parallel() => m_Parallel.Add(m_Source, 1, m_Destination);

    [Benchmark]
    public void ParallelSimd() => m_ParallelSimd.Add(m_Source, 1, m_Destination);
}