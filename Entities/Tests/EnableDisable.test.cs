using System.Runtime.InteropServices;

namespace Gubbins.Entities.Tests;

[TestFixture]
public class EnableDisableTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    [StructLayout(LayoutKind.Sequential, Size = 4096)]
    private struct HugeComp
    {
        public byte Marker;
    }

    [Test]
    public void Disable_ShouldHideTheEntityFromQueries()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        var other = repository.Insert(new CompA(2));

        Assert.That(repository.Disable(target), Is.True);

        var chunks = repository.Query(new ComponentFilter().Include<CompA>());
        using var entities = chunks.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(repository.IsEnabled(target), Is.False);
            Assert.That(repository.Contains(target), Is.True, "A disabled entity is still a live handle.");
            Assert.That(repository.Count, Is.EqualTo(2), "Disabling does not remove the entity.");
            Assert.That(entities.Elements.Count, Is.EqualTo(1));
            Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {other.Index}));
        });
    }

    [Test]
    public void Enable_ShouldRestoreTheEntityWithItsComponentValues()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(7), new CompB(8));

        Assert.That(repository.Disable(target), Is.True);
        Assert.That(repository.Enable(target), Is.True);

        var chunks = repository.Query(new ComponentFilter().Include<CompA>().Include<CompB>());
        using var entities = chunks.GetLayout<Entity>();
        using var valuesA = chunks.GetLayout<CompA>();
        using var valuesB = chunks.GetLayout<CompB>();

        Assert.Multiple(() =>
        {
            Assert.That(repository.IsEnabled(target), Is.True);
            Assert.That(entities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(valuesA.Batch.GetElement(0).Value, Is.EqualTo(7));
            Assert.That(valuesB.Batch.GetElement(0).Value, Is.EqualTo(8));
        });
    }

    [Test]
    public void SetEnabled_ShouldBeIdempotent()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));

        Assert.Multiple(() =>
        {
            Assert.That(repository.Disable(target), Is.True);
            Assert.That(repository.Disable(target), Is.False);
            Assert.That(repository.Enable(target), Is.True);
            Assert.That(repository.Enable(target), Is.False);
        });
    }

    [Test]
    public void Disable_ShouldKeepTheHandleUsable()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));

        repository.Disable(target);

        // A disabled entity still accepts component writes through its handle.
        repository.Update(target, new CompA(42));
        repository.Enable(target);

        var chunks = repository.Query(new ComponentFilter().Include<CompA>());
        using var values = chunks.GetLayout<CompA>();

        Assert.That(values.Batch.GetElement(0).Value, Is.EqualTo(42));
    }

    [Test]
    public void Disable_ShouldCompactTheSourceChunkWithoutLosingEntities()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var entities = new List<Entity>();
        for (var i = 0; i < 6; i++)
        {
            entities.Add(repository.Insert(new CompA(i)));
        }

        repository.Disable(entities[1]);
        repository.Disable(entities[4]);

        var filter = new ComponentFilter().Include<CompA>();
        Assert.Multiple(() =>
        {
            Assert.That(ReadValues(repository, filter), Is.EquivalentTo(new[] {0, 2, 3, 5}));
            Assert.That(AllEnabled(repository, entities), Is.EqualTo(new[] {true, false, true, true, false, true}));
        });

        // The survivors keep working after the source chunk compacted.
        repository.Update(entities[5], new CompA(55));
        Assert.That(ReadValues(repository, filter), Is.EquivalentTo(new[] {0, 2, 3, 55}));

        repository.Enable(entities[1]);
        repository.Enable(entities[4]);
        Assert.Multiple(() =>
        {
            Assert.That(ReadValues(repository, filter), Is.EquivalentTo(new[] {0, 1, 2, 3, 4, 55}));
            Assert.That(repository.IsEnabled(entities[1]), Is.True);
            Assert.That(repository.IsEnabled(entities[4]), Is.True);
        });
    }

    [Test]
    public void Query_ShouldReturnDisabledEntitiesOnlyWhenAskedFor()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        var other = repository.Insert(new CompA(2));
        repository.Disable(target);

        var enabled = repository.Query(new ComponentFilter().Include<CompA>());
        var disabled = repository.Query(new ComponentFilter().Include<CompA>().Include<Disabled>());
        var everything = repository.Query(new ComponentFilter().Include<CompA>()).IncludeDisabled();
        using var enabledEntities = enabled.GetLayout<Entity>();
        using var disabledEntities = disabled.GetLayout<Entity>();
        using var allEntities = everything.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(enabledEntities.Elements.Count, Is.EqualTo(1));
            Assert.That(disabledEntities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(allEntities.Elements.Count, Is.EqualTo(2));
            Assert.That(other.Index, Is.Not.EqualTo(target.Index));
        });
    }

    [Test]
    public void Insert_WithDisabledTag_ShouldStartDisabled()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1), default(Disabled));
        var filter = new ComponentFilter().Include<CompA>();

        var hidden = repository.Query(filter);
        using (var hiddenEntities = hidden.GetLayout<Entity>())
        {
            Assert.Multiple(() =>
            {
                Assert.That(repository.IsEnabled(target), Is.False);
                Assert.That(hiddenEntities.Elements.Count, Is.EqualTo(0));
            });
        }

        Assert.That(repository.Enable(target), Is.True);

        var visible = repository.Query(filter);
        using var visibleEntities = visible.GetLayout<Entity>();
        Assert.That(visibleEntities.Elements.Count, Is.EqualTo(1));
    }

    [Test]
    public void Disable_ThenDelete_ShouldReleaseTheEntity()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        repository.Disable(target);

        Assert.That(repository.Delete(target), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(repository.Contains(target), Is.False);
            Assert.That(repository.Count, Is.EqualTo(0));
        });

        // The freed index is reused by the next insert, at a newer version.
        var reused = repository.Insert(new CompA(2));
        Assert.Multiple(() =>
        {
            Assert.That(reused.Index, Is.EqualTo(target.Index));
            Assert.That(reused.Version, Is.GreaterThan(target.Version));
        });
    }

    [Test]
    public void SetEnabled_StaleHandle_ShouldThrow()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));
        repository.Delete(target);

        Assert.Multiple(() =>
        {
            Assert.Throws<InvalidOperationException>(() => repository.Disable(target));
            Assert.Throws<InvalidOperationException>(() => repository.Enable(target));
            Assert.Throws<InvalidOperationException>(() => repository.IsEnabled(target));
            Assert.Throws<InvalidOperationException>(() => repository.SetEnabled(5, false));
        });
    }

    [Test]
    public void AddComponent_ShouldMoveTheEntityIntoTheWiderArchetype()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));

        Assert.That(repository.AddComponent(target, new CompB(2)), Is.True);

        var filter = new ComponentFilter().Include<CompA>().Include<CompB>();
        var chunks = repository.Query(filter);
        using var valuesA = chunks.GetLayout<CompA>();
        using var valuesB = chunks.GetLayout<CompB>();

        Assert.Multiple(() =>
        {
            Assert.That(chunks.GetLayout<Entity>().Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(valuesA.Batch.GetElement(0).Value, Is.EqualTo(1));
            Assert.That(valuesB.Batch.GetElement(0).Value, Is.EqualTo(2));
            Assert.That(repository.AddComponent(target, new CompB(3)), Is.False, "The entity already holds CompB.");
        });
    }

    [Test]
    public void RemoveComponent_ShouldMoveTheEntityIntoTheNarrowerArchetype()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1), new CompB(2));

        Assert.That(repository.RemoveComponent<CompB>(target), Is.True);

        var onlyA = repository.Query(new ComponentFilter().Include<CompA>().Exclude<CompB>());
        using var valuesA = onlyA.GetLayout<CompA>();

        Assert.Multiple(() =>
        {
            Assert.That(onlyA.GetLayout<Entity>().Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
            Assert.That(valuesA.Batch.GetElement(0).Value, Is.EqualTo(1));
            Assert.That(repository.RemoveComponent<CompB>(target), Is.False, "The entity does not hold CompB anymore.");
        });
    }

    [Test]
    public void AddComponent_WithAComponentLargerThanTheInlineBuffer_ShouldCarryTheValueOver()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));

        Assert.That(repository.AddComponent(target, new HugeComp {Marker = 7}), Is.True);

        var chunks = repository.Query(new ComponentFilter().Include<CompA>().Include<HugeComp>());
        using var valuesA = chunks.GetLayout<CompA>();
        using var valuesHuge = chunks.GetLayout<HugeComp>();

        Assert.Multiple(() =>
        {
            Assert.That(valuesA.Batch.GetElement(0).Value, Is.EqualTo(1));
            Assert.That(valuesHuge.Batch.GetElement(0).Marker, Is.EqualTo(7));
        });

        Assert.That(repository.RemoveComponent<HugeComp>(target), Is.True);

        var onlyA = repository.Query(new ComponentFilter().Include<CompA>().Exclude<HugeComp>());
        using var remaining = onlyA.GetLayout<CompA>();
        Assert.That(remaining.Batch.GetElement(0).Value, Is.EqualTo(1));
    }

    [Test]
    public void Disable_ShouldStampBothChunksForChangeFiltering()
    {
        using var repository = new EntityRepository(TestAllocator.Shared);
        var target = repository.Insert(new CompA(1));

        var before = repository.Version;
        repository.Disable(target);

        var filter = new ComponentFilter().Include<CompA>();
        var enabledChunks = repository.Query(filter, before);
        var disabledChunks = repository.Query(filter, before).IncludeDisabled();
        using var enabledEntities = enabledChunks.GetLayout<Entity>();
        using var disabledEntities = disabledChunks.GetLayout<Entity>();

        Assert.Multiple(() =>
        {
            Assert.That(enabledEntities.Elements.Count, Is.EqualTo(0));
            Assert.That(disabledEntities.Batch.GetIndexes(), Is.EquivalentTo(new[] {target.Index}));
        });
    }

    /// <summary>
    /// Reads the <see cref="CompA"/> values of the entities the enabled filter returns.
    /// </summary>
    private static List<int> ReadValues(EntityRepository repository, ComponentFilter filter)
    {
        var chunks = repository.Query(filter);
        using var values = chunks.GetLayout<CompA>();
        var result = new List<int>(values.Elements.Count);
        for (var i = 0; i < values.Elements.Count; i++)
        {
            result.Add(values.Elements[i].Value);
        }

        return result;
    }

    /// <summary>
    /// Reads the enabled state of every entity in insertion order.
    /// </summary>
    private static bool[] AllEnabled(EntityRepository repository, List<Entity> entities)
    {
        var result = new bool[entities.Count];
        for (var i = 0; i < entities.Count; i++)
        {
            result[i] = repository.IsEnabled(entities[i]);
        }

        return result;
    }
}
