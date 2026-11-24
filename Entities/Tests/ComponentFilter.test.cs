namespace Gubbins.Entities.Tests;

[TestFixture]
public class ComponentFilterSemanticsTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    private record struct CompC(int Value);

    private record struct CompD;

    private record struct CompE;
    
    [Test]
    public void SimpleTest()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompB {Value = 1});
        var chunks = repository.Command(new ComponentFilter().Include<CompA>());
        var entities = chunks.GetLayout<Entity>();
        Assert.That(entities.Batch.GetIndexes(), Is.Not.EquivalentTo(new[] {target.Index}));
    }

    [Test]
    public void Include_ChainedCalls_ShouldAccumulateAllRequiredComponents()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var abc = repository.Insert(new CompA {Value = 1}, new CompB {Value = 2}, new CompC {Value = 3});
        repository.Insert(new CompA {Value = 4}, new CompC {Value = 5});

        var filter = new ComponentFilter().Include<CompA>().Include<CompB>().Include<CompC>();
        var chunks = repository.Command(filter);
        using var entities = chunks.GetLayout<Entity>();
        Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {abc.Index}));
    }

    [Test]
    public void Exclude_ChainedCalls_ShouldAccumulateAllExcludedComponents()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());

        var filter = new ComponentFilter().Include<CompA, CompB, CompC>().Exclude<CompD, CompE>();
        var chunks = repository.Command(filter);
        using var entities = chunks.GetLayout<Entity>();
        Assert.That(entities.Elements.Count, Is.EqualTo(1));
        Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
    }

    [Test]
    public void Include_OrderShouldNotChangeMatchedEntities()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompC());

        var context1 = new ComponentFilter().Include<CompB>().Include<CompC>();
        var context2 = new ComponentFilter().Include<CompC>().Include<CompB>();

        var chunks1 = repository.Command(context1);
        var chunks2 = repository.Command(context2);
        using var result1 = chunks1.GetLayout<Entity>();
        using var result2 = chunks2.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Include_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA());

        var single = new ComponentFilter().Include<CompA, CompB>();
        var duplicate = new ComponentFilter().Include<CompA, CompB>().Include<CompB>();

        var chunksSingle = repository.Command(single);
        var chunksDuplicate = repository.Command(duplicate);
        using var entitiesSingle = chunksSingle.GetLayout<Entity>();
        using var entitiesDuplicate = chunksDuplicate.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(entitiesSingle.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(entitiesDuplicate.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Exclude_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC());

        var single = new ComponentFilter().Include<CompA, CompB>().Exclude<CompC>();
        var duplicate = new ComponentFilter().Include<CompA, CompB>().Exclude<CompC>().Exclude<CompC>();

        var chunksSingle = repository.Command(single);
        var chunksDuplicate = repository.Command(duplicate);
        using var entitiesSingle = chunksSingle.GetLayout<Entity>();
        using var entitiesDuplicate = chunksDuplicate.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(entitiesSingle.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(entitiesDuplicate.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void IncludeExclude_AlternatingChainPermutation_ShouldMatchSameEntities()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target1 = repository.Insert(new CompA(), new CompB(), new CompC());
        var target2 = repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var filter1 = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var filter2 = new ComponentFilter().Include<CompA>().Include<CompC>().Include<CompB>().Exclude<CompD>();

        var chunks1 = repository.Command(filter1);
        var chunks2 = repository.Command(filter2);
        using var result1 = chunks1.GetLayout<Entity>();
        using var result2 = chunks2.GetLayout<Entity>();

        var expected = new[] {target1.Index, target2.Index};

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batch.GetIndexes(), Is.EquivalentTo(expected));
            Assert.That(result2.Batch.GetIndexes(), Is.EquivalentTo(expected));
        });
    }

    [Test]
    public void IncludeExclude_AlternatingChainWithDuplicateExclude_ShouldMatchSingleExclude()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var single = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var duplicate = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompD>();

        var singleChunks = repository.Command(single);
        var duplicateChunks = repository.Command(duplicate);
        using var singleResult = singleChunks.GetLayout<Entity>();
        using var duplicateResult = duplicateChunks.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(duplicateResult.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void IncludeExclude_EquivalentContexts_ShouldPreserveMatchDeterminism()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompD());
        repository.Insert(new CompA(), new CompC(), new CompE());

        var context1 = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompE>();
        var context2 = new ComponentFilter().Include<CompA>().Include<CompC>().Include<CompB>().Exclude<CompE>().Exclude<CompD>();

        var chunks1 = repository.Command(context1);
        var chunks2 = repository.Command(context2);
        using var result1 = chunks1.GetLayout<Entity>();
        using var result2 = chunks2.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }
}