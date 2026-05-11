namespace Gubbins.Entities.Tests;

[TestFixture]
public class EntityCommandTests
{
    private record struct CompA(int Value);

    [Test]
    public void Insert_ShouldSetEntityValidAndIncreaseCount()
    {
        var repository = new EntityRepository();

        var entity = repository.Insert(new CompA {Value = 7});
        var record = repository.Get(entity.Index);
        var value = record.Chunk.Get<CompA>(record.IndexInChunk).Value;

        Assert.Multiple(() =>
        {
            Assert.That(entity.Valid, Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
            Assert.That(repository.Contains(entity.Index), Is.True);
            Assert.That(value, Is.EqualTo(7));
        });
    }

    [Test]
    public void Delete_ShouldUpdateMovedEntityRecordIndexInChunk()
    {
        var repository = new EntityRepository();

        var first = repository.Insert(new CompA {Value  = 10});
        var second = repository.Insert(new CompA {Value = 11});

        var deleted = repository.Delete(first.Index);
        var secondRecord = repository.Get(second.Index);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
            Assert.That(repository.Contains(second.Index), Is.True);
            Assert.That(secondRecord.IndexInChunk, Is.EqualTo(0));
        });
    }

    [Test]
    public void Delete_InvalidIndex_ShouldReturnFalseAndKeepCount()
    {
        var repository = new EntityRepository();
        repository.Insert(new CompA {Value = 1});

        var before = repository.Count;
        var deletedNegative = repository.Delete(-1);
        var deletedOutOfRange = repository.Delete(99);

        Assert.Multiple(() =>
        {
            Assert.That(deletedNegative, Is.False);
            Assert.That(deletedOutOfRange, Is.False);
            Assert.That(repository.Count, Is.EqualTo(before));
        });
    }

    [Test]
    public void Delete_SameEntityTwice_ShouldOnlyDeleteOnce()
    {
        var repository = new EntityRepository();
        var entity = repository.Insert(new CompA {Value = 1});

        var firstDelete = repository.Delete(entity.Index);
        var secondDelete = repository.Delete(entity.Index);

        Assert.Multiple(() =>
        {
            Assert.That(firstDelete, Is.True);
            Assert.That(secondDelete, Is.False);
            Assert.That(repository.Count, Is.EqualTo(0));
            Assert.That(repository.Contains(entity.Index), Is.False);
        });
    }

    [Test]
    public void Insert_AfterDelete_ShouldReuseRemovedIndex()
    {
        var repository = new EntityRepository();
        var first = repository.Insert(new CompA {Value  = 1});
        var second = repository.Insert(new CompA {Value = 2});

        repository.Delete(first.Index);
        var reused = repository.Insert(new CompA {Value = 3});

        Assert.Multiple(() =>
        {
            Assert.That(reused.Index, Is.EqualTo(first.Index));
            Assert.That(reused.Valid, Is.True);
            Assert.That(repository.Count, Is.EqualTo(2));
            Assert.That(repository.Contains(reused.Index), Is.True);
            Assert.That(repository.Contains(second.Index), Is.True);
        });
    }

    [Test]
    public void Insert_AfterMultipleDeletes_ShouldReuseIndexesInFifoOrder()
    {
        var repository = new EntityRepository();
        var e0 = repository.Insert(new CompA {Value = 0});
        var e1 = repository.Insert(new CompA {Value = 1});
        var e2 = repository.Insert(new CompA {Value = 2});

        repository.Delete(e1.Index);
        repository.Delete(e0.Index);

        var reused1 = repository.Insert(new CompA {Value = 10});
        var reused2 = repository.Insert(new CompA {Value = 11});

        Assert.Multiple(() =>
        {
            Assert.That(reused1.Index, Is.EqualTo(e1.Index));
            Assert.That(reused2.Index, Is.EqualTo(e0.Index));
            Assert.That(repository.Count, Is.EqualTo(3));
            Assert.That(repository.Contains(e2.Index), Is.True);
        });
    }

    [Test]
    public void DeleteAll_ShouldDecreaseCountOncePerEntity()
    {
        var repository = new EntityRepository();

        var e1 = repository.Insert(new CompA {Value = 1});
        var e2 = repository.Insert(new CompA {Value = 2});
        var e3 = repository.Insert(new CompA {Value = 3});

        Span<int> indexes = [e1.Index, e2.Index];
        var removed = repository.DeleteAll(indexes);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(repository.Count, Is.EqualTo(1));
            Assert.That(repository.Contains(e3.Index), Is.True);
        });
    }

    [Test]
    public void Update_DeletedEntity_ShouldThrowInvalidOperationException()
    {
        var repository = new EntityRepository();
        var entity = repository.Insert(new CompA {Value = 1});
        repository.Delete(entity.Index);

        Assert.Throws<InvalidOperationException>(() => repository.Update(entity.Index, new CompA {Value = 2}));
    }

    [Test]
    public void DeleteAll_WithMixedIndexes_ShouldOnlyDeleteExistingValidEntities()
    {
        var repository = new EntityRepository();
        var e1 = repository.Insert(new CompA {Value = 1});
        var e2 = repository.Insert(new CompA {Value = 2});
        var e3 = repository.Insert(new CompA {Value = 3});

        repository.Delete(e2.Index);

        Span<int> indexes = [e1.Index, e2.Index, 99, e3.Index, e1.Index];
        var removed = repository.DeleteAll(indexes);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(2));
            Assert.That(repository.Count, Is.EqualTo(0));
            Assert.That(repository.Contains(e1.Index), Is.False);
            Assert.That(repository.Contains(e2.Index), Is.False);
            Assert.That(repository.Contains(e3.Index), Is.False);
        });
    }

}