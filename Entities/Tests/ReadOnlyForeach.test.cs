namespace Gubbins.Entities.Tests;

[TestFixture]
public class ReadOnlyForeachTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    [Test]
    public void ForEach_ShouldVisitEveryElement()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        repository.Insert(new CompA(2));
        repository.Insert(new CompA(3));

        var sum = 0;
        var chunks = repository.Query(new ComponentFilter().Include<CompA>());
        chunks.ForEach((in Entity entity, in CompA a) => sum += a.Value);

        Assert.That(sum, Is.EqualTo(6));
    }

    [Test]
    public void ForEach_WithoutComponents_ShouldVisitEveryEntity()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var a = repository.Insert(new CompA(1));
        var b = repository.Insert(new CompB(2));

        var indexes = new List<int>();
        var chunks = repository.Query(new ComponentFilter());
        chunks.ForEach((in Entity entity) => indexes.Add(entity.Index));

        Assert.That(indexes, Is.EquivalentTo(new[] {a.Index, b.Index}));
    }

    [Test]
    public void ForEach_ShouldGiveTheActionReadOnlySpans()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        for (var i = 0; i < 5; i++)
        {
            repository.Insert(new CompA(i), new CompB(i * 10));
        }

        var sumA = 0;
        var sumB = 0;
        var seen = 0;
        var chunks = repository.Query(new ComponentFilter().Include<CompA>().Include<CompB>());
        chunks.ForEach(
            (ReadOnlySpan<Entity> entities, ReadOnlySpan<CompA> valuesA, ReadOnlySpan<CompB> valuesB) =>
            {
                seen += entities.Length;
                for (var i = 0; i < valuesA.Length; i++)
                {
                    sumA += valuesA[i].Value;
                    sumB += valuesB[i].Value;
                }
            });

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EqualTo(5));
            Assert.That(sumA, Is.EqualTo(10));
            Assert.That(sumB, Is.EqualTo(100));
        });
    }

    [Test]
    public void ForEach_ShouldNotStampTheChunksItVisits()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        var filter = new ComponentFilter().Include<CompA>();
        var before = repository.Version;

        {

            var chunks = repository.Query(filter);
            chunks.ForEach((in Entity entity, in CompA a) => _ = a.Value);
        }

        Assert.That(
            repository.Query(filter, before).Count,
            Is.EqualTo(0),
            "A read-only view must not make a chunk hot.");
        {
            var writable = repository.Command(filter);
            writable.ForEach((in Entity entity, ref CompA a) => a.Value++);
        }

        Assert.That(repository.Query(filter, before).Count, Is.EqualTo(1), "Write iteration must make its chunks hot.");
    }

    [Test]
    public void Query_ShouldComposeWithTheOtherFilters()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        var other = repository.Insert(new CompA(2));
        repository.Disable(target);

        var indexes = new List<int>();
        var chunks = repository.Query(new ComponentFilter().Include<CompA>());
        chunks.IncludeDisabled().ForEach((in Entity entity) => indexes.Add(entity.Index));

        Assert.That(indexes, Is.EquivalentTo(new[] {target.Index, other.Index}));

        var hot = new List<int>();
        var before = repository.Version;
        chunks.IncludeDisabled().ChangedSince(before).ForEach((in Entity entity) => hot.Add(entity.Index));
        Assert.That(hot, Is.Empty, "Nothing was written after the captured version.");
    }

    [Test]
    public void GetLayout_ShouldExposeTheSameValues()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(7));
        repository.Insert(new CompA(8));

        var chunks = repository.Query(new ComponentFilter().Include<CompA>());
        using var values = chunks.GetLayout<CompA>();

        ref readonly var first = ref values.Elements[0];
        var firstValue = first.Value;
        var total = 0;
        foreach (ref readonly var value in values.Elements)
        {
            total += value.Value;
        }

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.EqualTo(2));
            Assert.That(values.Segments.Count, Is.EqualTo(1));
            Assert.That(values.Segments[0].Length, Is.EqualTo(2));
            Assert.That(values.Addresses.Count, Is.EqualTo(1));
            Assert.That(firstValue, Is.EqualTo(7));
            Assert.That(total, Is.EqualTo(15));
        });
    }

    [Test]
    public void GetLayout_ShouldNotStampTheChunks()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        var filter = new ComponentFilter().Include<CompA>();
        var before = repository.Version;

        var chunks = repository.Query(filter);
        using (var values = chunks.GetLayout<CompA>())
        {
            Assert.That(values.Count, Is.EqualTo(1));
        }

        Assert.That(repository.Query(filter, before).Count, Is.EqualTo(0));
    }
}
