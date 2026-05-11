namespace Gubbins.Entities.Tests;

[TestFixture]
public class EntityQueryContextSemanticsTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    private record struct CompC(int Value);

    private record struct CompD;

    private record struct CompE;

    [Test]
    public void Include_ChainedCalls_ShouldAccumulateAllRequiredComponents()
    {
        var repository = new EntityRepository();

        var abc = repository.Insert(new CompA {Value = 1}, new CompB {Value = 2}, new CompC {Value = 3});
        repository.Insert(new CompA {Value = 4}, new CompC {Value = 5});

        var context = new EntityQueryContext<CompA>().Include<CompB>().Include<CompC>();
        using var result = repository.GetQueryHandle(context).Query();

        Assert.That(result.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {abc.Index}));
    }

    [Test]
    public void Exclude_ChainedCalls_ShouldAccumulateAllExcludedComponents()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());

        var context = new EntityQueryContext<CompA, CompB, CompC>().Exclude<CompD>().Exclude<CompE>();
        using var result = repository.GetQueryHandle(context).Query();

        Assert.That(result.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
    }

    [Test]
    public void Include_MultiCallAndSingleCall_ShouldProduceSameHash()
    {
        var chained = new EntityQueryContext<CompA>().Include<CompB>().Include<CompC>();
        var grouped = new EntityQueryContext<CompA>().Include<CompB, CompC>();

        Assert.That(chained.Hash, Is.EqualTo(grouped.Hash));
    }

    [Test]
    public void Exclude_ShouldNotChangeHash()
    {
        var baseContext = new EntityQueryContext<CompA, CompB>();
        var excluded = baseContext.Exclude<CompC, CompD>();

        Assert.That(excluded.Hash, Is.EqualTo(baseContext.Hash));
    }

    [Test]
    public void Include_OrderShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompC());

        var context1 = new EntityQueryContext<CompA>().Include<CompB>().Include<CompC>();
        var context2 = new EntityQueryContext<CompA>().Include<CompC>().Include<CompB>();

        using var result1 = repository.GetQueryHandle(context1).Query();
        using var result2 = repository.GetQueryHandle(context2).Query();

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Include_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA());

        var single = new EntityQueryContext<CompA>().Include<CompB>();
        var duplicate = new EntityQueryContext<CompA>().Include<CompB>().Include<CompB>();

        using var singleResult = repository.GetQueryHandle(single).Query();
        using var duplicateResult = repository.GetQueryHandle(duplicate).Query();

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(duplicateResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Exclude_DuplicateCalls_ShouldNotChangeMatchedEntities()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC());

        var single = new EntityQueryContext<CompA, CompB>().Exclude<CompC>();
        var duplicate = new EntityQueryContext<CompA, CompB>().Exclude<CompC>().Exclude<CompC>();

        using var singleResult = repository.GetQueryHandle(single).Query();
        using var duplicateResult = repository.GetQueryHandle(duplicate).Query();

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(duplicateResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void GenericBaseContext_AndChainedInclude_ShouldMatchEquivalentGenericContext()
    {
        var typed = new EntityQueryContext<CompA, CompB>();
        var chained = new EntityQueryContext<CompA>().Include<CompB>();

        Assert.That(chained.Hash, Is.EqualTo(typed.Hash));
    }

    [Test]
    public void IncludeExclude_AlternatingChainPermutation_ShouldMatchSameEntities()
    {
        var repository = new EntityRepository();

        var target1 = repository.Insert(new CompA(), new CompB(), new CompC());
        var target2 = repository.Insert(new CompA(), new CompB(), new CompC(), new CompE());
        repository.Insert(new CompA(), new CompB());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var context1 = new EntityQueryContext<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var context2 = new EntityQueryContext<CompA>().Include<CompC>().Include<CompB>().Exclude<CompD>();

        using var result1 = repository.GetQueryHandle(context1).Query();
        using var result2 = repository.GetQueryHandle(context2).Query();

        var expected = new[] {target1.Index, target2.Index};

        Assert.Multiple(() =>
        {
            Assert.That(result1.Batches.Item1.GetIndexes(), Is.EquivalentTo(expected));
            Assert.That(result2.Batches.Item1.GetIndexes(), Is.EquivalentTo(expected));
        });
    }

    [Test]
    public void IncludeExclude_AlternatingChainWithDuplicateExclude_ShouldMatchSingleExclude()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompC(), new CompD());

        var single = new EntityQueryContext<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>();
        var duplicate = new EntityQueryContext<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompD>();

        using var singleResult = repository.GetQueryHandle(single).Query();
        using var duplicateResult = repository.GetQueryHandle(duplicate).Query();

        Assert.Multiple(() =>
        {
            Assert.That(singleResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(duplicateResult.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    [Test]
    public void Include_OrderPermutation_ShouldProduceDeterministicHash()
    {
        var context1 = new EntityQueryContext<CompA>().Include<CompB>().Include<CompC>();
        var context2 = new EntityQueryContext<CompA>().Include<CompC>().Include<CompB>();

        Assert.That(context1.Hash, Is.EqualTo(context2.Hash));
    }

    [Test]
    public void IncludeExclude_EquivalentContexts_ShouldPreserveHashAndMatchDeterminism()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA(), new CompB(), new CompC());
        repository.Insert(new CompA(), new CompB(), new CompD());
        repository.Insert(new CompA(), new CompC(), new CompE());

        var context1 = new EntityQueryContext<CompA>().Include<CompB>().Exclude<CompD>().Include<CompC>().Exclude<CompE>();
        var context2 = new EntityQueryContext<CompA>().Include<CompC>().Include<CompB>().Exclude<CompE>().Exclude<CompD>();

        using var result1 = repository.GetQueryHandle(context1).Query();
        using var result2 = repository.GetQueryHandle(context2).Query();

        Assert.Multiple(() =>
        {
            Assert.That(context1.Hash, Is.EqualTo(context2.Hash));
            Assert.That(result1.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(result2.Batches.Item1.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }
}