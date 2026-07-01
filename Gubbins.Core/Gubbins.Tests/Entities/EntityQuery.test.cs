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
        var filter = new ComponentFilter().Include<CompA>();
        using var chunks = repository.Search(filter);
        using var entities = chunks.GetComponents<Entity>();
        using var values = chunks.GetComponents<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(entities.Elements.Count, Is.EqualTo(3));
            Assert.That(values.Elements.Count, Is.EqualTo(3));
            CollectionAssert.AreEquivalent(new[] {onlyA1.Index, onlyA2.Index, withAB.Index}, entities.Batch.GetIndexes());
        });
    }

    [Test]
    public void Query_WithIncludeExclude_ShouldFilterExpectedArchetype()
    {
        var repository = new EntityRepository();

        var target = repository.Insert(new CompA {Value = 1}, new CompB {Value = 10});
        repository.Insert(new CompA {Value = 2}, new CompB {Value = 20}, new CompC {Value = 200});
        repository.Insert(new CompA {Value = 3});

        var filter = new ComponentFilter().Include<CompA>().Include<CompB>().Exclude<CompC>();
        using var chunks = repository.Search(filter);
        using var entities = chunks.GetComponents<Entity>();
        using var valuesA = chunks.GetComponents<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(entities.Elements.Count, Is.EqualTo(1));
            Assert.That(valuesA.Elements.Count, Is.EqualTo(1));
            Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(valuesA.Batch.GetElement(0).Value, Is.EqualTo(1));
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

        var filter = new ComponentFilter().Include<HugeComp>();
        using var chunks = repository.Search(filter);
        var entities = chunks.GetComponents<Entity>();
        var values = chunks.GetComponents<HugeComp>();

        Assert.Multiple(() =>
        {
            Assert.That(entities.Elements.Count, Is.EqualTo(10));
            Assert.That(values.Elements.Count, Is.EqualTo(10));
            Assert.That(entities.Segments.Count, Is.GreaterThan(1));
            CollectionAssert.AreEquivalent(insertedIndexes, entities.Batch.GetIndexes());
        });
    }

    [Test]
    public void Get_InvalidIndex_ShouldThrow()
    {
        var repository = new EntityRepository();
        repository.Insert(new CompA {Value = 1});

        Assert.Throws<IndexOutOfRangeException>(() => _ = repository.GetRecord(-1));
        Assert.Throws<IndexOutOfRangeException>(() => _ = repository.GetRecord(10));
    }
}
