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
        var repository = new EntityRepository();
        var target = repository.Insert(new CompB {Value = 1});
        var chunks = repository.Search(new ComponentFilter().Include<CompA>());
        var entities = chunks.GetComponents<Entity>();
        Assert.That(entities.Batch.GetIndexes(), Is.Not.EquivalentTo(new[] {target.Index}));
    }

    [Test]
    public void Include_ChainedCalls_ShouldAccumulateAllRequiredComponents()
    {
        var repository = new EntityRepository();

        var abc = repository.Insert(new CompA {Value = 1}, new CompB {Value = 2}, new CompC {Value = 3});
        repository.Insert(new CompA {Value = 4}, new CompC {Value = 5});

        var filter = new ComponentFilter().Include<CompA>().Include<CompB>().Include<CompC>();
        using var chunks = repository.Search(filter);
        using var entities = chunks.GetComponents<Entity>();
        Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {abc.Index}));
    }

    [Test]
    public void Exclude_ChainedCalls_ShouldAccumulateAllExcludedComponents()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());

        var filter = new ComponentFilter().Include<CompA, CompB, CompC>().Exclude<CompD, CompE>();
        using var chunks = repository.Search(filter);
        using var entities = chunks.GetComponents<Entity>();
        Assert.That(entities.Elements.Count, Is.EqualTo(1));
        Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
    }

    [Test]
    public void Include_OrderShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompC());

        var context1 = new ComponentFilter().Include<CompB>().Include<CompC>();
        var context2 = new ComponentFilter().Include<CompC>().Include<CompB>();

        using var chunks1 = repository.Search(context1);
        using var chunks2 = repository.Search(context2);
        using var result1 = chunks1.GetComponents<Entity>();
        using var result2 = chunks2.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Include_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA());

        var single = new ComponentFilter().Include<CompA, CompB>();
        var duplicate = new ComponentFilter().Include<CompA, CompB>().Include<CompB>();

        using var chunksSingle = repository.Search(single);
        using var chunksDuplicate = repository.Search(duplicate);
        using var entitiesSingle = chunksSingle.GetComponents<Entity>();
        using var entitiesDuplicate = chunksDuplicate.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(entitiesSingle.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(entitiesDuplicate.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Exclude_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC());

        var single = new ComponentFilter().Include<CompA, CompB>().Exclude<CompC>();
        var duplicate = new ComponentFilter().Include<CompA, CompB>().Exclude<CompC>().Exclude<CompC>();

        using var chunksSingle = repository.Search(single);
        using var chunksDuplicate = repository.Search(duplicate);
        using var entitiesSingle = chunksSingle.GetComponents<Entity>();
        using var entitiesDuplicate = chunksDuplicate.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(entitiesSingle.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(entitiesDuplicate.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void IncludeExclude_AlternatingChainPermutation_ShouldMatchSameEntities()
    {
        var repository = new EntityRepository();

        var target1 = repository.Insert(new CompA(), new CompB(), new CompC());
        var target2 = repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var filter1 = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var filter2 = new ComponentFilter().Include<CompA>().Include<CompC>().Include<CompB>().Exclude<CompD>();

        using var chunks1 = repository.Search(filter1);
        using var chunks2 = repository.Search(filter2);
        using var result1 = chunks1.GetComponents<Entity>();
        using var result2 = chunks2.GetComponents<Entity>();

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
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var single = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var duplicate = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompD>();

        using var singleChunks = repository.Search(single);
        using var duplicateChunks = repository.Search(duplicate);
        using var singleResult = singleChunks.GetComponents<Entity>();
        using var duplicateResult = duplicateChunks.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(duplicateResult.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void IncludeExclude_EquivalentContexts_ShouldPreserveMatchDeterminism()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompD());
        repository.Insert(new CompA(), new CompC(), new CompE());

        var context1 = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompE>();
        var context2 = new ComponentFilter().Include<CompA>().Include<CompC>().Include<CompB>().Exclude<CompE>().Exclude<CompD>();

        using var chunks1 = repository.Search(context1);
        using var chunks2 = repository.Search(context2);
        using var result1 = chunks1.GetComponents<Entity>();
        using var result2 = chunks2.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }
}