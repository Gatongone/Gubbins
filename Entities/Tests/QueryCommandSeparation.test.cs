namespace Gubbins.Entities.Tests;

[TestFixture]
public class QueryCommandSeparationTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    [Test]
    public void QueryRole_ShouldHandOutOnlyTheReadOnlyView()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        var filter = new ComponentFilter().Include<CompA>();

        // The declared type is the contract: the query role cannot reach a writable Chunks at all.
        IEntityQuery query = repository;

        // Capture the version the insert stamped, then move the clock so a view that declares write access becomes
        // distinguishable from one that does not.
        var before = repository.Version;

        ReadOnlyChunks chunks = query.Query(filter);
        using var values = chunks.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(values.Count, Is.EqualTo(1));
            Assert.That(
                repository.Query(filter, before).Count,
                Is.EqualTo(0),
                "The query role must not mark the chunk as written.");
        });
    }

    [Test]
    public void CommandRole_ShouldHandOutTheWritableViewAndStampIt()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        repository.Insert(new CompA(1));
        var filter = new ComponentFilter().Include<CompA>();

        // The declared type is the contract: the command role reaches the writable view.
        IEntityCommand command = repository;

        var before = repository.Version;

        Chunks chunks = command.Command(filter);
        using (var values = chunks.GetLayout<CompA>())
        {
            values.Batch.GetElement(0).Value++;
        }

        var check = repository.Query(filter);
        using var checkedValues = check.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(
                repository.Query(filter, before).Count,
                Is.EqualTo(1),
                "The command role's view marks the chunk as written.");
            Assert.That(checkedValues.Elements[0].Value, Is.EqualTo(2));
        });
    }

    [Test]
    public void CommandRole_ShouldMutateStructureWhileQueryRoleObserves()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        IEntityCommand command = repository;
        IEntityQuery query = repository;
        var filter = new ComponentFilter().Include<CompA>();

        var entity = command.Insert(new CompA(1));
        command.Update(entity.Index, new CompA(2));
        command.SetEnabled(entity, false);

        var hidden = query.Query(filter);
        using (var hiddenValues = hidden.GetLayout<CompA>())
        {
            Assert.That(hiddenValues.Count, Is.Zero, "A disabled entity is invisible to the query role.");
        }

        command.SetEnabled(entity, true);
        command.AddComponent(entity, new CompB(3));

        var chunks = query.Query(new ComponentFilter().Include<CompA>().Include<CompB>());
        using var values = chunks.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(query.Contains(entity), Is.True);
            Assert.That(query.Count, Is.EqualTo(1));
            Assert.That(values.Count, Is.EqualTo(1));
            Assert.That(values.Elements[0].Value, Is.EqualTo(2));
        });
    }
}
