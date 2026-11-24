namespace Gubbins.Entities.Tests;

[TestFixture]
public sealed class ComponentsParallelReadOnlyTests
{
    private const int CHUNK_SIZE = 256;
    private const int ENTITY_COUNT = 1000;

    private static readonly ComponentFilter filter = new ComponentFilter().Include<int>();

    private static EntityRepository CreateRepository()
    {
        var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        for (var i = 0; i < ENTITY_COUNT; i++)
        {
            repository.Insert(1, 1.2f);
        }

        return repository;
    }

    private static int Sum(ReadOnlyChunkLayout<int> values)
    {
        var accumulator = new int[1];
        values.ApplyParallel(accumulator, static (ReadOnlySpan<int> span, int[] sink) =>
        {
            var local = 0;
            for (var i = 0; i < span.Length; i++)
            {
                local += span[i];
            }

            Interlocked.Add(ref sink[0], local);
        });

        return accumulator[0];
    }

    [Test]
    public void ApplyParallel_ShouldVisitEveryElement()
    {
        using var repository = CreateRepository();
        var chunks = repository.Query(filter);
        using var values = chunks.GetLayout<int>();

        Assert.That(Sum(values), Is.EqualTo(ENTITY_COUNT));
    }

    [Test]
    public void ApplyParallel_WithExplicitWorkerCount_ShouldVisitEveryElement()
    {
        using var repository = CreateRepository();
        var chunks = repository.Query(filter);
        using var values = chunks.GetLayout<int>();

        var accumulator = new int[1];
        values.ApplyParallel(accumulator, static (ReadOnlySpan<int> span, int[] sink) => Interlocked.Add(ref sink[0], span.Length), 4);

        Assert.That(accumulator[0], Is.EqualTo(ENTITY_COUNT));
    }

    [Test]
    public void ApplyParallel_ShouldNotStampTheChunksItWalks()
    {
        using var repository = CreateRepository();
        var before = repository.Version;

        var chunks = repository.Query(filter);
        using (var values = chunks.GetLayout<int>())
        {
            Assert.That(Sum(values), Is.EqualTo(ENTITY_COUNT));
        }

        Assert.Multiple(() =>
        {
            Assert.That(
                repository.Query(filter, before).Count,
                Is.EqualTo(0),
                "Read-only parallel iteration must not make a chunk hot.");
            Assert.That(repository.Query(filter).Count, Is.GreaterThan(1), "The fixture spans several chunks.");
        });
    }

    [Test]
    public void ApplyParallel_EmptyQuery_ShouldNotInvokeTheAction()
    {
        using var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        repository.Insert(1.2f);

        var invoked = 0;
        var chunks = repository.Query(filter);
        using var values = chunks.GetLayout<int>();
        values.ApplyParallel(0, (ReadOnlySpan<int> span, int state) => invoked += span.Length);

        Assert.That(invoked, Is.Zero);
    }
}
