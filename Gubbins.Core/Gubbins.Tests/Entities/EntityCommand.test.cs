namespace Gubbins.Entities.Tests;

[TestFixture]
public class EntityCommandTests
{
    private record struct CompA(int Value);

    [Test]
    public void Insert_ShouldSetEntityVersionAndIncreaseCount()
    {
        var repository = new EntityRepository();

        var entity = repository.Insert(new CompA {Value = 7});
        var record = repository.GetRecord(entity.Index);
        var value = record.Chunk.Get<CompA>(record.IndexInChunk).Value;

        Assert.Multiple(() =>
        {
            Assert.That(entity.Version, Is.GreaterThan(0u));
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
        var secondRecord = repository.GetRecord(second.Index);

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
            Assert.That(reused.Version, Is.GreaterThan(first.Version));
            Assert.That(repository.Count, Is.EqualTo(2));
            Assert.That(repository.Contains(reused.Index), Is.True);
            Assert.That(repository.Contains(second.Index), Is.True);
        });
    }

    [Test]
    public void Update_WithStaleEntityHandle_ShouldThrowInvalidOperationException()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});
        repository.Delete(stale.Index);
        repository.Insert(new CompA {Value = 2});

        Assert.Throws<InvalidOperationException>(() => repository.Update(stale, new CompA {Value = 3}));
    }

    [Test]
    public void Update_WithCurrentEntityHandle_ShouldUpdateComponent()
    {
        var repository = new EntityRepository();
        var entity = repository.Insert(new CompA {Value = 1});

        repository.Update(entity, new CompA {Value = 9});
        var record = repository.GetRecord(entity.Index);
        var value = record.Chunk.Get<CompA>(record.IndexInChunk).Value;

        Assert.That(value, Is.EqualTo(9));
    }

    [Test]
    public void Contains_WithStaleAndCurrentEntityHandle_ShouldReflectVersion()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});

        repository.Delete(stale.Index);
        var current = repository.Insert(new CompA {Value = 2});

        Assert.Multiple(() =>
        {
            Assert.That(repository.Contains(stale), Is.False);
            Assert.That(repository.Contains(current), Is.True);
            Assert.That(stale.Index, Is.EqualTo(current.Index));
            Assert.That(current.Version, Is.GreaterThan(stale.Version));
        });
    }

    [Test]
    public void Contains_IndexAndEntity_ShouldDifferForStaleHandleAfterReuse()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});

        repository.Delete(stale.Index);
        var current = repository.Insert(new CompA {Value = 2});

        Assert.Multiple(() =>
        {
            Assert.That(repository.Contains(stale.Index), Is.True);
            Assert.That(repository.Contains(stale), Is.False);
            Assert.That(repository.Contains(current), Is.True);
        });
    }

    [Test]
    public void Delete_WithStaleEntityHandle_ShouldReturnFalseAndKeepCount()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});

        repository.Delete(stale.Index);
        var current = repository.Insert(new CompA {Value = 2});
        var countBefore = repository.Count;

        var deleted = repository.Delete(stale);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.False);
            Assert.That(repository.Count, Is.EqualTo(countBefore));
            Assert.That(repository.Contains(current), Is.True);
        });
    }

    [Test]
    public void Delete_WithCurrentEntityHandle_ShouldSucceed()
    {
        var repository = new EntityRepository();
        var entity = repository.Insert(new CompA {Value = 1});

        var deleted = repository.Delete(entity);

        Assert.Multiple(() =>
        {
            Assert.That(deleted, Is.True);
            Assert.That(repository.Contains(entity), Is.False);
            Assert.That(repository.Contains(entity.Index), Is.False);
            Assert.That(repository.Count, Is.EqualTo(0));
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

    [Test]
    public void DeleteAll_WithReusedIndex_ShouldDeleteCurrentGenerationOnlyOnce()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});
        var survivor = repository.Insert(new CompA {Value = 2});

        repository.Delete(stale.Index);
        var current = repository.Insert(new CompA {Value = 3});

        Span<int> indexes = [stale.Index, stale.Index];
        var removed = repository.DeleteAll(indexes);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Index, Is.EqualTo(current.Index));
            Assert.That(current.Version, Is.GreaterThan(stale.Version));
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(repository.Contains(current), Is.False);
            Assert.That(repository.Contains(stale.Index), Is.False);
            Assert.That(repository.Contains(survivor), Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void DeleteAll_AfterReuse_WhenReinsertedAgain_ShouldKeepVersionMonotonic()
    {
        var repository = new EntityRepository();
        var first = repository.Insert(new CompA {Value = 1});

        repository.Delete(first.Index);
        var second = repository.Insert(new CompA {Value = 2});

        Span<int> indexes = [second.Index];
        var removed = repository.DeleteAll(indexes);
        var third = repository.Insert(new CompA {Value = 3});

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.EqualTo(1));
            Assert.That(first.Index, Is.EqualTo(second.Index));
            Assert.That(second.Index, Is.EqualTo(third.Index));
            Assert.That(second.Version, Is.GreaterThan(first.Version));
            Assert.That(third.Version, Is.GreaterThan(second.Version));
            Assert.That(repository.Contains(third), Is.True);
        });
    }

    [Test]
    public void Delete_StaleThenDeleteAll_ByIndex_ShouldOnlyRemoveCurrentGeneration()
    {
        var repository = new EntityRepository();
        var stale = repository.Insert(new CompA {Value = 1});
        var survivor = repository.Insert(new CompA {Value = 2});

        repository.Delete(stale.Index);
        var current = repository.Insert(new CompA {Value = 3});

        var deletedByStaleHandle = repository.Delete(stale);
        Span<int> indexes = [stale.Index];
        var removedByDeleteAll = repository.DeleteAll(indexes);

        Assert.Multiple(() =>
        {
            Assert.That(stale.Index, Is.EqualTo(current.Index));
            Assert.That(current.Version, Is.GreaterThan(stale.Version));
            Assert.That(deletedByStaleHandle, Is.False);
            Assert.That(removedByDeleteAll, Is.EqualTo(1));
            Assert.That(repository.Contains(current), Is.False);
            Assert.That(repository.Contains(survivor), Is.True);
            Assert.That(repository.Count, Is.EqualTo(1));
        });
    }

}