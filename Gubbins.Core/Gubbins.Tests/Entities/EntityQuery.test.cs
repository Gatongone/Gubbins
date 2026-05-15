using System.Runtime.InteropServices;

namespace Gubbins.Entities.Tests;

[TestFixture]
public class EntityQueryTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    private record struct CompC(int Value);

    [StructLayout(LayoutKind.Sequential, Size = 4096)]
    private struct HugeComp
    {
        public byte Marker;
    }

    [Test]
    public void Query_WithSingleGenericContext_ShouldMatchAllArchetypesContainingComponent()
    {
        var repository = new EntityRepository();

        var onlyA1 = repository.Insert(new CompA {Value = 1});
        var onlyA2 = repository.Insert(new CompA {Value = 2});
        var withAB = repository.Insert(new CompA {Value = 3}, new CompB {Value = 30});

        var handle = repository.GetQueryHandle(new EntityQueryContext<CompA>());
        using var result = handle.Query();

        var entities = result.Batches.Item1;
        var values = result.Batches.Item2;

        Assert.Multiple(() =>
        {
            Assert.That(entities.ElementCount, Is.EqualTo(3));
            Assert.That(values.ElementCount, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(new[] {onlyA1.Index, onlyA2.Index, withAB.Index}, entities.GetIndexes());
        });
    }

    [Test]
    public void Query_WithIncludeExclude_ShouldFilterExpectedArchetype()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA {Value = 1}, new CompB {Value = 10});
        repository.Insert(new CompA {Value = 2}, new CompB {Value = 20}, new CompC {Value = 200});
        repository.Insert(new CompA {Value = 3});

        var context = new EntityQueryContext<CompA>().Include<CompB>().Exclude<CompC>();
        var handle = repository.GetQueryHandle(context);
        using var result = handle.Query();

        var entities = result.Batches.Item1;
        var valuesA = result.Batches.Item2;

        Assert.Multiple(() =>
        {
            Assert.That(entities.ElementCount, Is.EqualTo(1));
            Assert.That(valuesA.ElementCount, Is.EqualTo(1));
            Assert.That(entities.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(valuesA.GetElement(0).Value, Is.EqualTo(1));
        });
    }

    [Test]
    public void Query_ShouldAggregateAllChunksForSameArchetype()
    {
        var repository = new EntityRepository();
        var insertedIndexes = new List<int>();

        for (var i = 0; i < 10; i++)
        {
            var entity = repository.Insert(new HugeComp {Marker = (byte) i});
            insertedIndexes.Add(entity.Index);
        }

        var handle = repository.GetQueryHandle(new EntityQueryContext<HugeComp>());
        using var result = handle.Query();

        var entities = result.Batches.Item1;
        var values = result.Batches.Item2;

        Assert.Multiple(() =>
        {
            Assert.That(entities.ElementCount, Is.EqualTo(10));
            Assert.That(values.ElementCount, Is.EqualTo(10));
            Assert.That(entities.SegmentCount, Is.GreaterThan(1));
            CollectionAssert.AreEquivalent(insertedIndexes, entities.GetIndexes());
        });
    }

    [Test]
    public void Get_InvalidIndex_ShouldThrow()
    {
        var repository = new EntityRepository();
        repository.Insert(new CompA {Value = 1});

        Assert.Throws<IndexOutOfRangeException>(() => _ = repository.Get(-1));
        Assert.Throws<IndexOutOfRangeException>(() => _ = repository.Get(10));
    }
}
