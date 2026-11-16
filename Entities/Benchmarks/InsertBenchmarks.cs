using BenchmarkDotNet.Attributes;
using Gubbins.Unsafe;

namespace Gubbins.Entities.Benchmarks;

/// <summary>
/// Measures what the <see cref="Components"/> payload contract costs on the insert path, against the typed
/// overloads that build the payload for you.
/// </summary>
/// <remarks>
/// Every method inserts <see cref="Count"/> entities into a repository created fresh for the iteration, so chunk
/// filling and the archetype lookup are both exercised. The <c>ManyArchetypes</c> cases first seed the repository
/// with 28 archetypes to expose the linear scan in <c>RequireArcheType</c>.
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class EntitiesInsertBenchmarks
{
    private AllocatorHandle m_Handle;
    private EntityRepository m_Repository;
    private A m_A;
    private B m_B;

    [Params(200_000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Init()
    {
        m_Handle = AllocatorHandle.Create(new ArenaAllocator(ManagedAllocator.Global));
        var allocator = m_Handle.Allocator;
        allocator.Alloc<int>(1 << 20, 8);
        allocator.Reset();
        m_A = new A { Value = 1 };
        m_B = new B { Value = 2.5f };
    }

    [GlobalCleanup]
    public void Cleanup() => m_Handle.Dispose();

    [IterationSetup]
    public void IterationSetup() => m_Repository = new EntityRepository(m_Handle.Allocator);

    [Benchmark(Baseline = true)]
    public void TypedInsert()
    {
        for (var i = 0; i < Count; i++)
        {
            m_Repository.Insert(1, 2.5f);
        }
    }

    [Benchmark]
    public void TypedInsertAll() => m_Repository.InsertAll(Count, 1, 2.5f);

    [Benchmark]
    public void ComponentsInsertPerCall()
    {
        var a = m_A;
        var b = m_B;
        for (var i = 0; i < Count; i++)
        {
            m_Repository.Insert(Components.Create(in a, in b));
        }
    }

    [Benchmark]
    public void ComponentsInsertHoisted()
    {
        var a = m_A;
        var b = m_B;
        var components = Components.Create(in a, in b);
        for (var i = 0; i < Count; i++)
        {
            m_Repository.Insert(components);
        }
    }

    [Benchmark]
    public void ComponentsInsertAll()
    {
        var a = m_A;
        var b = m_B;
        m_Repository.InsertAll(Count, Components.Create(in a, in b));
    }

    [Benchmark]
    public void TypedInsertManyArchetypes()
    {
        SeedArchetypes(m_Repository);
        for (var i = 0; i < Count; i++)
        {
            m_Repository.Insert(1, 2.5f);
        }
    }

    [Benchmark]
    public void ComponentsInsertManyArchetypes()
    {
        SeedArchetypes(m_Repository);
        var a = m_A;
        var b = m_B;
        for (var i = 0; i < Count; i++)
        {
            m_Repository.Insert(Components.Create(in a, in b));
        }
    }

    /// <summary>Adds 28 archetypes (every pair of A..H) so the archetype lookup has to scan past them.</summary>
    private static void SeedArchetypes(EntityRepository repository)
    {
        repository.Insert(new A(), new B());
        repository.Insert(new A(), new C());
        repository.Insert(new A(), new D());
        repository.Insert(new A(), new E());
        repository.Insert(new A(), new F());
        repository.Insert(new A(), new G());
        repository.Insert(new A(), new H());
        repository.Insert(new B(), new C());
        repository.Insert(new B(), new D());
        repository.Insert(new B(), new E());
        repository.Insert(new B(), new F());
        repository.Insert(new B(), new G());
        repository.Insert(new B(), new H());
        repository.Insert(new C(), new D());
        repository.Insert(new C(), new E());
        repository.Insert(new C(), new F());
        repository.Insert(new C(), new G());
        repository.Insert(new C(), new H());
        repository.Insert(new D(), new E());
        repository.Insert(new D(), new F());
        repository.Insert(new D(), new G());
        repository.Insert(new D(), new H());
        repository.Insert(new E(), new F());
        repository.Insert(new E(), new G());
        repository.Insert(new E(), new H());
        repository.Insert(new F(), new G());
        repository.Insert(new F(), new H());
        repository.Insert(new G(), new H());
    }

    public struct A
    {
        public int Value;
    }

    public struct B
    {
        public float Value;
    }

    public struct C
    {
        public float Value;
    }

    public struct D
    {
        public float Value;
    }

    public struct E
    {
        public float Value;
    }

    public struct F
    {
        public float Value;
    }

    public struct G
    {
        public float Value;
    }

    public struct H
    {
        public float Value;
    }
}
