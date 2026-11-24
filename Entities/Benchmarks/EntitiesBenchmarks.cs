using BenchmarkDotNet.Attributes;
using Gubbins.Async;
using Gubbins.Entities;
using Gubbins.Span;
using Gubbins.Unsafe;

namespace Gubbins.Entities.Benchmarks;

[MemoryDiagnoser]
[MaxIterationCount(25)]
public class EntitiesForeachBenchmarks
{
    private AllocatorHandle m_Handle;
    private EntityRepository m_Repository;
    private static readonly ComponentFilter s_Filter = new ComponentFilter().Include<int>();

    [Params(16 * 1024, 256 * 1024, 1024 * 1024, 4 * 1024 * 1024)]
    public int ChunkSize { get; set; }

    [GlobalSetup]
    public void Init()
    {
        m_Handle = AllocatorHandle.Create(new ArenaAllocator(ManagedAllocator.Global));
        m_Repository = new EntityRepository(m_Handle.Allocator, ChunkSize);
        var allocator = m_Handle.Allocator;
        allocator.Alloc<int>(1000000, 8);
        allocator.Reset();
        for (var i = 0; i < 1024 * 100; i++)
        {
            m_Repository.Insert(1, 1.2f);
            m_Repository.Insert('a', 1, 1.2f);
            m_Repository.Insert(DateTime.Now, 1, 1.2f);
        }
    }

    [GlobalCleanup]
    public void Cleanup() => m_Handle.Dispose();

    [Benchmark(Baseline = true)]
    public void Foreach()
    {
        var chunks = m_Repository.Command(s_Filter);
        chunks.ForEach(static (in Entity _, ref int a) => { a += 1; });
    }

    [Benchmark]
    public async Asynchronous ForeachAsync()
    {
        var chunks = m_Repository.Command(s_Filter);
        await chunks.ForEachAsync(static (in Entity _, ref int a) => { a += 1; });
    }

    [Benchmark]
    public void ForeachElementsWithForEach()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        var elements = num.Elements;
        foreach (ref var element in elements)
        {
            element += 1;
        }
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithSerial()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        var segments = num.Segments;
        var operation = SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(IterationMode.Serial)!;
        for (var i = 0; i < segments.Count; i++)
        {
            var span = segments[i];
            operation.Add(span, 1, span);
        }
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithSimd()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        var segments = num.Segments;
        var operation = SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(IterationMode.Simd)!;
        for (var i = 0; i < segments.Count; i++)
        {
            var span = segments[i];
            operation.Add(span, 1, span);
        }
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithParallel()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        var segments = num.Segments;
        var operation = SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(IterationMode.Parallel)!;
        for (var i = 0; i < segments.Count; i++)
        {
            var span = segments[i];
            operation.Add(span, 1, span);
        }
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithParallelSimd()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        var segments = num.Segments;
        var operation = SpanOperations.GetOperation<int, ISpanNumberOperation<int>>(IterationMode.Parallel | IterationMode.Simd)!;
        for (var i = 0; i < segments.Count; i++)
        {
            var span = segments[i];
            operation.Add(span, 1, span);
        }
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithChunkedSerial()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        num.Add(IterationMode.Serial, 1);
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithChunkedSimd()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        num.Add(IterationMode.Simd, 1);
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithChunkedParallelSimd()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        num.Add(IterationMode.Parallel | IterationMode.Simd, 1);
    }

    [Benchmark]
    public void ForeachSegmentsSpanWithChunkedParallel()
    {
        var chunks = m_Repository.Command(s_Filter);
        using var num = chunks.GetLayout<int>();
        num.Add(IterationMode.Parallel, 1);
    }
}