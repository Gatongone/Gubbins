namespace Gubbins.Entities.Tests;

[TestFixture]
public class ChangeTrackingTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    [Test]
    public void Command_WithoutChangeFilter_ShouldReturnEveryChunk()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        repository.Insert(new CompB(2));

        var chunks = repository.Query(new ComponentFilter().Include<CompA>());

        Assert.That(chunks.Count, Is.EqualTo(1));
    }

    [Test]
    public void Update_ShouldStampOnlyTheChunkItWrites()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        repository.Insert(new CompB(2));

        var before = repository.Version;
        repository.Update(target, new CompA(9));

        var written = repository.Query(new ComponentFilter().Include<CompA>(), before);
        var untouched = repository.Query(new ComponentFilter().Include<CompB>(), before);

        Assert.Multiple(() =>
        {
            Assert.That(written.Count, Is.EqualTo(1));
            Assert.That(untouched.Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void Insert_ShouldStampTheNewChunk()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var before = repository.Version;
        repository.Insert(new CompA(1));

        var chunks = repository.Query(new ComponentFilter().Include<CompA>(), before);

        Assert.That(chunks.Count, Is.EqualTo(1));
    }

    [Test]
    public void Delete_ShouldStampTheChunkItCompacts()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        repository.Insert(new CompA(2));

        var before = repository.Version;
        repository.Delete(target);

        var chunks = repository.Query(new ComponentFilter().Include<CompA>(), before);
        using var entities = chunks.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(chunks.Count, Is.EqualTo(1));
            Assert.That(entities.Elements.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReadOnlyQuery_ShouldNotStamp_WhileWriteQueryShould()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        var filter = new ComponentFilter().Include<CompA>();
        var before = repository.Version;

        {

            var readOnly = repository.Query(filter);
            using var components = readOnly.GetLayout<CompA>();
            Assert.That(components.Elements.Count, Is.EqualTo(1));
        }

        Assert.That(repository.Query(filter, before).Count, Is.EqualTo(0), "A read-only query must not make a chunk hot.");
        {
            var writable = repository.Command(filter);
            using var components = writable.GetLayout<CompA>();
            Assert.That(components.Elements.Count, Is.EqualTo(1));
        }

        Assert.That(repository.Query(filter, before).Count, Is.EqualTo(1), "A write query must make its chunks hot.");
    }

    [Test]
    public void ChangedSince_ShouldSkipColdChunksOfTheSameArchetype()
    {
        // A 128-byte chunk holds ten CompA entities, so twenty inserts fill two chunks of one archetype.
        using var repository = new EntityRepository(TestAllocator.Shared, 128);
        var filter = new ComponentFilter().Include<CompA>();
        var entities = new List<Entity>();
        for (var i = 0; i < 20; i++)
        {
            entities.Add(repository.Insert(new CompA(i)));
        }

        var before = repository.Version;

        // One entity of the second chunk changes; the first chunk stays cold.
        repository.Update(entities[15], new CompA(150));

        var all = repository.Query(filter);
        var hot = repository.Query(filter, before);
        using var values = hot.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(all.Count, Is.EqualTo(2));
            Assert.That(hot.Count, Is.EqualTo(1));
            Assert.That(values.Elements.Count, Is.EqualTo(10));
            Assert.That(values.Elements[5].Value, Is.EqualTo(150));
        });
    }

    [Test]
    public void NextVersion_ShouldMarkAnExplicitPointInTheTimeline()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var filter = new ComponentFilter().Include<CompA>();

        repository.Insert(new CompA(1));
        var mark = repository.NextVersion();

        {

            var nothingYet = repository.Query(filter, mark);
            Assert.That(nothingYet.Count, Is.EqualTo(0), "Nothing has been written after the mark.");
        }

        // Writes move the clock themselves, so they land after the mark without any further call.
        repository.Insert(new CompA(2));

        Assert.Multiple(() =>
        {
            Assert.That(repository.Version, Is.GreaterThan(mark));
            Assert.That(repository.Query(filter, mark).Count, Is.EqualTo(1));
        });
    }
}
