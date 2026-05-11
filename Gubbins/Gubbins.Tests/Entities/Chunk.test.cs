using System.Runtime.InteropServices;

namespace Gubbins.Entities.Tests;

[TestFixture]
public class ChunkBehaviorTests
{
    private record struct CompA(int Value);

    private record struct CompB(int Value);

    private record struct CompC(int Value);

    [StructLayout(LayoutKind.Sequential, Size = 4096)]
    private struct HugeComp;

    [Test]
    public void Ctor_NullTypes_ShouldThrowNullReferenceException()
    {
        Assert.Throws<NullReferenceException>(() => _ = new Chunk(null!));
    }

    [Test]
    public void Ctor_EmptyTypes_ShouldThrowIndexOutOfRangeException()
    {
        Assert.Throws<IndexOutOfRangeException>(() => _ = new Chunk([]));
    }

    [Test]
    public void Ctor_WithoutEntity_ShouldAutomaticallyProvideEntityColumn()
    {
        using var chunk = new Chunk([typeof(CompA)]);
        var types = new[] {typeof(CompA)};

        var index = chunk.Add(CreateSingleData(7), types);
        chunk.Set(index, new Entity {Index = 77, Valid = true});

        Assert.That(chunk.Get<Entity>(index).Index, Is.EqualTo(77));
    }

    [Test]
    public void Ctor_OnlyEntityType_ShouldAllowAddWithNoComponentPayload()
    {
        using var chunk = new Chunk([typeof(Entity)]);

        var index = chunk.Add(Array.Empty<byte>(), Array.Empty<Type>());
        chunk.Set(index, new Entity {Index = 9, Valid = true});

        Assert.Multiple(() =>
        {
            Assert.That(chunk.Count, Is.EqualTo(1));
            Assert.That(chunk.Get<Entity>(index).Index, Is.EqualTo(9));
            Assert.That(chunk.Get<Entity>(index).Valid, Is.True);
        });
    }

    [Test]
    public void Add_LengthMismatch_ShouldThrowArgumentOutOfRangeException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var invalidData = new byte[sizeof(int)];
        var types = new[] {typeof(CompA), typeof(CompB)};

        Assert.Throws<ArgumentOutOfRangeException>(() => chunk.Add(invalidData, types));
    }

    [Test]
    public void Add_UnknownType_ShouldThrowInvalidOperationException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var data = CreateData(11, 22).ToArray();
        var invalidTypes = new[] {typeof(CompA), typeof(CompC)};

        Assert.Throws<InvalidOperationException>(() => chunk.Add(data, invalidTypes));
    }

    [Test]
    public void Add_MissingComponentType_ShouldThrowInvalidOperationException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var data = CreateData(11, 22).ToArray();
        var missingTypeSet = new[] {typeof(CompA)};

        Assert.Throws<InvalidOperationException>(() => chunk.Add(data, missingTypeSet));
    }

    [Test]
    public void Add_DuplicateTypes_ShouldThrowInvalidOperationException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var data = CreateData(11, 22).ToArray();
        var duplicateTypes = new[] {typeof(CompA), typeof(CompA)};

        Assert.Throws<InvalidOperationException>(() => chunk.Add(data, duplicateTypes));
    }

    [Test]
    public void Add_WithEntityTypeInInputTypes_ShouldThrowInvalidOperationException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var data = CreateData(11, 22).ToArray();
        var invalidTypes = new[] {typeof(Entity), typeof(CompA)};

        Assert.Throws<InvalidOperationException>(() => chunk.Add(data, invalidTypes));
    }

    [Test]
    public void Add_WithReorderedTypes_ShouldMapToCorrectComponents()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var types = new[] {typeof(CompB), typeof(CompA)};
        var data = CreateData(700, 900);

        var index = chunk.Add(data, types);

        Assert.Multiple(() =>
        {
            Assert.That(chunk.Get<CompA>(index).Value, Is.EqualTo(900));
            Assert.That(chunk.Get<CompB>(index).Value, Is.EqualTo(700));
        });
    }

    [Test]
    public void Add_CanonicalTypes_WithSwappedPayloadSegments_ShouldFollowPayloadLayout()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var intendedA = 70;
        var intendedB = 90;
        var types = new[] {typeof(CompA), typeof(CompB)};
        var misaligned = CreateDataSwappedSegments(intendedA, intendedB);

        var index = chunk.Add(misaligned, types);

        Assert.Multiple(() =>
        {
            // This locks in the current contract: Add trusts caller-provided byte segment order.
            Assert.That(chunk.Get<CompA>(index).Value, Is.EqualTo(intendedB));
            Assert.That(chunk.Get<CompB>(index).Value, Is.EqualTo(intendedA));
        });
    }

    [Test]
    public void Add_ToCapacity_ShouldSetIsFull_AndNextAddShouldThrow()
    {
        using var chunk = new Chunk([typeof(HugeComp)]);
        var types = new[] {typeof(HugeComp)};

        for (var i = 0; i < chunk.Capacity; i++)
        {
            chunk.Add(new byte[Marshal.SizeOf<HugeComp>()], types);
        }

        Assert.Multiple(() =>
        {
            Assert.That(chunk.IsFull, Is.True);
            Assert.That(chunk.Count, Is.EqualTo(chunk.Capacity));
            Assert.Throws<ArgumentOutOfRangeException>(() => chunk.Add(new byte[Marshal.SizeOf<HugeComp>()], types));
        });
    }

    [Test]
    public void Remove_FromFullChunk_ShouldClearIsFull()
    {
        using var chunk = new Chunk([typeof(HugeComp)]);
        var types = new[] {typeof(HugeComp)};

        for (var i = 0; i < chunk.Capacity; i++)
        {
            chunk.Add(new byte[Marshal.SizeOf<HugeComp>()], types);
        }

        var removed = chunk.Remove(0);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(chunk.IsFull, Is.False);
            Assert.That(chunk.Count, Is.EqualTo(chunk.Capacity - 1));
        });
    }

    [Test]
    public void Remove_InvalidIndex_ShouldReturnFalse()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};

        var index = chunk.Add(CreateData(1, 2), types);
        chunk.Set(index, new Entity {Index = 10, Valid = true});

        var removed = chunk.Remove(-1, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(chunk.Count, Is.EqualTo(1));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Index, Is.EqualTo(0));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_EmptyChunk_ShouldReturnFalse()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);

        var removed = chunk.Remove(0, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(chunk.Count, Is.EqualTo(0));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_IndexEqualCount_ShouldReturnFalse()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        var removed = chunk.Remove(chunk.Count, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(chunk.Count, Is.EqualTo(1));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_IndexGreaterThanCount_ShouldReturnFalse()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        var removed = chunk.Remove(chunk.Count + 1, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.False);
            Assert.That(chunk.Count, Is.EqualTo(1));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_LastEntity_ShouldNotReturnMovedEntity()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};

        var index = chunk.Add(CreateData(1, 2), types);
        chunk.Set(index, new Entity {Index = 20, Valid = true});

        var removed = chunk.Remove(index, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(chunk.Count, Is.EqualTo(0));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Index, Is.EqualTo(0));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_SingleEntityAtZero_ShouldNotReturnMovedEntity()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};

        chunk.Add(CreateData(10, 20), types);

        var removed = chunk.Remove(0, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(chunk.Count, Is.EqualTo(0));
            Assert.That(movedFromIndex, Is.EqualTo(-1));
            Assert.That(movedEntity.Valid, Is.False);
        });
    }

    [Test]
    public void Remove_MiddleEntity_ShouldMoveLastEntityIntoRemovedSlot()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};

        var first = chunk.Add(CreateData(1, 11), types);
        var second = chunk.Add(CreateData(2, 22), types);
        var third = chunk.Add(CreateData(3, 33), types);

        chunk.Set(first, new Entity {Index  = 100, Valid = true});
        chunk.Set(second, new Entity {Index = 200, Valid = true});
        chunk.Set(third, new Entity {Index  = 300, Valid = true});

        var removed = chunk.Remove(second, out var movedEntity, out var movedFromIndex);

        Assert.Multiple(() =>
        {
            Assert.That(removed, Is.True);
            Assert.That(chunk.Count, Is.EqualTo(2));
            Assert.That(movedFromIndex, Is.EqualTo(third));
            Assert.That(movedEntity.Index, Is.EqualTo(300));
            Assert.That(chunk.Get<Entity>(second).Index, Is.EqualTo(300));
            Assert.That(chunk.Get<CompA>(second).Value, Is.EqualTo(3));
            Assert.That(chunk.Get<CompB>(second).Value, Is.EqualTo(33));
        });
    }

    [Test]
    public void Get_UnmatchedType_ShouldThrowArgumentException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        Assert.Throws<ArgumentException>(() => { _ = chunk.Get<CompC>(0); });
    }

    [Test]
    public void Get_NegativeIndex_ShouldThrowArgumentOutOfRangeException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = chunk.Get<CompA>(-1); });
    }

    [Test]
    public void Get_IndexEqualCount_ShouldThrowArgumentOutOfRangeException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        Assert.Throws<ArgumentOutOfRangeException>(() => { _ = chunk.Get<CompA>(chunk.Count); });
    }

    [Test]
    public void GetAll_UnmatchedType_ShouldThrowArgumentException()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        chunk.Add(CreateData(1, 2), types);

        Assert.Throws<ArgumentException>(() => { _ = chunk.GetAll<CompC>(); });
    }

    [Test]
    public void GetAll_ShouldReturnLiveViewWithCountLength()
    {
        using var chunk = new Chunk([typeof(CompA), typeof(CompB)]);
        var types = new[] {typeof(CompA), typeof(CompB)};
        var index = chunk.Add(CreateData(7, 8), types);

        var snippet = chunk.GetAll<CompA>();

        Assert.That(snippet.Length, Is.EqualTo(chunk.Count));

        snippet.Span[0] = new CompA(1234);

        Assert.That(chunk.Get<CompA>(index).Value, Is.EqualTo(1234));
    }

    [Test]
    public void GetAll_OnEmptyChunk_ShouldReturnEmptySnippet()
    {
        using var chunk = new Chunk([typeof(CompA)]);

        var snippet = chunk.GetAll<CompA>();

        Assert.That(snippet.Length, Is.EqualTo(0));
    }

    [Test]
    public void GetAll_Entity_OnEmptyChunk_ShouldReturnEmptySnippet()
    {
        using var chunk = new Chunk([typeof(CompA)]);

        var snippet = chunk.GetAll<Entity>();

        Assert.That(snippet.Length, Is.EqualTo(0));
    }

    private static Span<byte> CreateData(int first, int second)
    {
        var bytes = new byte[sizeof(int) * 2];
        var firstComp = new CompA(first);
        var secondComp = new CompB(second);
        MemoryMarshal.Write(bytes.AsSpan(0, sizeof(int)), ref firstComp);
        MemoryMarshal.Write(bytes.AsSpan(sizeof(int), sizeof(int)), ref secondComp);
        return bytes;
    }

    private static Span<byte> CreateSingleData(int first)
    {
        var bytes = new byte[sizeof(int)];
        var firstComp = new CompA(first);
        MemoryMarshal.Write(bytes.AsSpan(), ref firstComp);
        return bytes;
    }

    private static Span<byte> CreateDataSwappedSegments(int first, int second)
    {
        var bytes = new byte[sizeof(int) * 2];
        var firstComp = new CompA(first);
        var secondComp = new CompB(second);

        // Write B then A to simulate a caller-provided payload/layout mismatch.
        MemoryMarshal.Write(bytes.AsSpan(0, sizeof(int)), ref secondComp);
        MemoryMarshal.Write(bytes.AsSpan(sizeof(int), sizeof(int)), ref firstComp);
        return bytes;
    }
}