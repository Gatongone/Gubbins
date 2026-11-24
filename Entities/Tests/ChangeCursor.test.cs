namespace Gubbins.Entities.Tests;

[TestFixture]
public class ChangeCursorTests
{
    private const int CHUNK_SIZE = 128;

    private record struct CompA(int Value);

    private static readonly ComponentFilter filter = new ComponentFilter().Include<CompA>();

    /// <summary>
    /// Creates a repository whose 128-byte chunks hold ten <see cref="CompA"/> entities, so twenty inserts fill two
    /// chunks and a write can target one of them.
    /// </summary>
    private static EntityRepository CreateRepository(out List<Entity> entities)
    {
        var repository = new EntityRepository(TestAllocator.Shared, CHUNK_SIZE);
        entities = new List<Entity>(20);
        for (var i = 0; i < 20; i++)
        {
            entities.Add(repository.Insert(new CompA(i)));
        }

        return repository;
    }

    [Test]
    public void Changed_ShouldYieldOnlyWhatWasWrittenSinceTheLastCall()
    {
        using var repository = CreateRepository(out var entities);
        IEntityQuery query = repository;
        var cursor = new ChangeCursor();

        {

            var first = query.Changed(cursor, filter);
            Assert.That(first.Count, Is.EqualTo(2), "The first call sees both chunks that already exist.");
        }

        {

            var second = query.Changed(cursor, filter);
            Assert.That(second.Count, Is.EqualTo(0), "Nothing was written since, so nothing is yielded.");
        }

        // No NextVersion, no captured version: the write itself moves the clock past the cursor.
        repository.Update(entities[15], new CompA(150));

        var third = query.Changed(cursor, filter);
        using var values = third.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(third.Count, Is.EqualTo(1), "Only the chunk the update touched is yielded.");
            Assert.That(values.Count, Is.EqualTo(10));
        });
    }

    [Test]
    public void Changed_ShouldSeeAWriteThatHappensAfterTheRead()
    {
        using var repository = CreateRepository(out var entities);
        IEntityQuery query = repository;
        var cursor = new ChangeCursor();

        query.Changed(cursor, filter);
        {
            // The first read only establishes the cursor; the interesting write happens next.
        }

        repository.Update(entities[0], new CompA(1));

        var chunks = query.Changed(cursor, filter);
        using var bodies = chunks.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(chunks.Count, Is.EqualTo(1), "Only the chunk the write touched is yielded.");
            Assert.That(bodies.Batch.GetIndexes(), Does.Contain(entities[0].Index));
        });
    }

    [Test]
    public void Reset_ShouldYieldEverythingAgain()
    {
        using var repository = CreateRepository(out _);
        IEntityQuery query = repository;
        var cursor = new ChangeCursor();

        query.Changed(cursor, filter);

        {

            var empty = query.Changed(cursor, filter);
            Assert.That(empty.Count, Is.EqualTo(0));
        }

        cursor.Reset();

        var everything = query.Changed(cursor, filter);
        Assert.That(everything.Count, Is.EqualTo(2));
    }

    [Test]
    public void Changed_OnTheCommandRole_ShouldYieldTheWritableView()
    {
        using var repository = CreateRepository(out var entities);
        IEntityCommand command = repository;
        IEntityQuery query = repository;
        var writer = new ChangeCursor();
        var reader = new ChangeCursor();

        query.Changed(reader, filter);

        // The declared type is the contract: the command side yields the writable view.
        Chunks chunks = command.Changed(writer, filter);
        using (var values = chunks.GetLayout<CompA>())
        {
            for (var i = 0; i < values.Elements.Count; i++)
            {
                values.Elements[i].Value += 1;
            }
        }

        var seenByReader = query.Changed(reader, filter);
        using var readValues = seenByReader.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(seenByReader.Count, Is.EqualTo(2), "Both chunks were written by the command view.");
            Assert.That(readValues.Count, Is.EqualTo(20));
        });

        var seenByWriter = command.Changed(writer, filter);
        Assert.That(seenByWriter.Count, Is.EqualTo(2), "A cursor observes a write at least once, including its own.");

        Assert.That(entities.Count, Is.EqualTo(20));
    }

    [Test]
    public void Changed_ShouldFilterDisabledEntities()
    {
        using var repository = CreateRepository(out var entities);
        IEntityQuery query = repository;
        var cursor = new ChangeCursor();

        query.Changed(cursor, filter);

        repository.Disable(entities[0]);

        var chunks = query.Changed(cursor, filter);
        using var values = chunks.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(chunks.Count, Is.EqualTo(1), "Only the chunk the move touched is yielded.");
            Assert.That(values.Count, Is.EqualTo(9), "The disabled entity left that chunk for the disabled archetype.");
        });
    }
}
