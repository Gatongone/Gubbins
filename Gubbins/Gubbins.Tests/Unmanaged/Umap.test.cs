using System.Collections;

namespace Gubbins.Unmanaged.Tests;

[TestFixture]
public class UmapTests
{
    [Test]
    public void DefaultConstructor_InitializesCorrectly()
    {
        // Arrange & Act
        using var map = new Umap<int, Ustring>();

        // Assert
        Assert.That(map, Is.Empty);
        Assert.That(map.ContainsKey(42), Is.False);
    }

    [Test]
    public void ConstructorWithCapacity_InitializesCorrectly()
    {
        // Arrange & Act
        using var map = new Umap<int, Ustring>(16);

        // Assert
        Assert.That(map, Is.Empty);
        Assert.That(map.ContainsKey(42), Is.False);
    }

    [Test]
    public void ConstructorWithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Umap<int, Ustring>(-1));
    }

    [Test]
    public void Add_KeyValuePair_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        map.Add(new KeyValuePair<int, Ustring>(1, "one"));

        // Assert
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.IsTrue(map.ContainsKey(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void Add_Tuple_KeyValue_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        map.Add((1, "one"));

        // Assert
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.IsTrue(map.ContainsKey(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void Add_DuplicateKey_ThrowsArgumentException()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => map.Add(1, "two"));
    }

    [Test]
    public void TryAdd_ExistingKey_ReturnsFalse()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        var result = map.TryAdd(1, "two");

        // Assert
        Assert.That(result, Is.False);
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void TryAdd_NewKey_ReturnsTrue()
    {
        // Arrange
         using var map = new Umap<int, Ustring>();

        // Act
        var result = map.TryAdd(1, "one");

        // Assert
        Assert.IsTrue(result);
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void Remove_ExistingKey_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        var result = map.Remove(1);

        // Assert
        Assert.IsTrue(result);
        Assert.That(map, Is.Empty);
        Assert.That(map.ContainsKey(1), Is.False);
    }

    [Test]
    public void Remove_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        var result = map.Remove(1);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(map, Is.Empty);
    }

    [Test]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.IsTrue(map.ContainsKey(1));
    }

    [Test]
    public void ContainsKey_NonExistingKey_ReturnsFalse()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act & Assert
        Assert.That(map.ContainsKey(1), Is.False);
    }

    [Test]
    public void TryGetValue_ExistingKey_ReturnsTrueAndValue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        var result = map.TryGetValue(1, out var value);

        // Assert
        Assert.IsTrue(result);
        Assert.That(value, Is.EqualTo("one"));
    }

    [Test]
    public void TryGetValue_NonExistingKey_ReturnsFalseAndDefault()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        var result = map.TryGetValue(1, out var value);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(value.IsInvalidOrEmpty, Is.True);
    }

    [Test]
    public void IndexerGet_ExistingKey_ReturnsValue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void IndexerSet_ExistingKey_UpdatesValue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        map[1] = "uno";

        // Assert
        Assert.That(map[1], Is.EqualTo("uno"));
    }

    [Test]
    public void IndexerSet_NonExistingKey_AddsNewEntry()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        map[1] = "one";

        // Assert
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void RefIndexerGet_ExistingKey_ReturnsRefToValue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        ref var valueRef = ref map[1];
        valueRef = "uno";

        // Assert
        Assert.That(map[1], Is.EqualTo("uno"));
    }

    [Test]
    public void TryGetValueRefOrAddDefault_ExistingKey_ReturnsRefAndTrue()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        ref var valueRef = ref map.TryGetValueRefOrAddDefault(1, out var exists);

        // Assert
        Assert.IsTrue(exists);
        Assert.That(valueRef, Is.EqualTo("one"));
        valueRef = "uno";
        Assert.That(map[1], Is.EqualTo("uno"));
    }

    [Test]
    public void TryGetValueRefOrAddDefault_NonExistingKey_ReturnsRefToDefaultAndFalse()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();

        // Act
        ref var valueRef = ref map.TryGetValueRefOrAddDefault(1, out var exists);

        // Assert
        Assert.That(exists, Is.False);
        Assert.That(valueRef.IsInvalidOrEmpty, Is.True);
        valueRef = "one";
        Assert.That(map.Count, Is.EqualTo(1));
        Assert.That(map[1], Is.EqualTo("one"));
    }

    [Test]
    public void Clear_EmptiesMap()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");

        // Act
        map.Clear();

        // Assert
        Assert.That(map, Is.Empty);
        Assert.That(map.ContainsKey(1), Is.False);
        Assert.That(map.ContainsKey(2), Is.False);
    }

    [Test]
    public void CopyTo_Array_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");
        var array = new KeyValuePair<int, Ustring>[2];

        // Act
        map.CopyTo(array, 0);

        // Assert
        Assert.That(array[0].Key, Is.EqualTo(1));
        Assert.That(array[0].Value, Is.EqualTo("one"));
        Assert.That(array[1].Key, Is.EqualTo(2));
        Assert.That(array[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void CopyTo_Span_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");
        var array = new KeyValuePair<int, Ustring>[2];
        var span = array.AsSpan();

        // Act
        map.CopyTo(span, 0);

        // Assert
        Assert.That(array[0].Key, Is.EqualTo(1));
        Assert.That(array[0].Value, Is.EqualTo("one"));
        Assert.That(array[1].Key, Is.EqualTo(2));
        Assert.That(array[1].Value, Is.EqualTo("two"));
    }

    [Test]
    public void CopyTo_InvalidArray_Throws()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => map.CopyTo((KeyValuePair<int, Ustring>[])null, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.CopyTo(new KeyValuePair<int, Ustring>[1], -1));
        Assert.Throws<ArgumentException>(() => map.CopyTo(new KeyValuePair<int, Ustring>[0], 0));
    }

    [Test]
    public void Enumerator_IteratesCorrectly()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");

        // Act
        var keys = new List<int>();
        var values = new List<Ustring>();
        using var enumerator = map.GetEnumerator();
        while (enumerator.MoveNext())
        {
            keys.Add(enumerator.Current.Key);
            values.Add(enumerator.Current.Value);
        }

        // Assert
        Assert.That(keys.Count, Is.EqualTo(2));
        CollectionAssert.Contains(keys, 1);
        CollectionAssert.Contains(keys, 2);
        CollectionAssert.Contains(values, "one");
        CollectionAssert.Contains(values, "two");
    }

    [Test]
    public void Keys_Collection_IteratesCorrectly()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");

        // Act
        var keys = map.Keys.ToArray();

        // Assert
        Assert.That(keys.Count, Is.EqualTo(2));
        CollectionAssert.Contains(keys, 1);
        CollectionAssert.Contains(keys, 2);
    }

    [Test]
    public void Values_Collection_IteratesCorrectly()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");

        // Act
        var values = map.Values.ToArray();

        // Assert
        Assert.That(values.Count, Is.EqualTo(2));
        CollectionAssert.Contains(values, "one");
        CollectionAssert.Contains(values, "two");
    }

    [Test]
    public void KeyCollection_CopyTo_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");
        var array = new int[2];

        // Act
        map.Keys.CopyTo(array, 0);

        // Assert
        Assert.That(array[0], Is.EqualTo(1));
        Assert.That(array[1], Is.EqualTo(2));
    }

    [Test]
    public void ValueCollection_CopyTo_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");
        var array = new Ustring[2];

        // Act
        map.Values.CopyTo(array, 0);

        // Assert
        Assert.That(array[0], Is.EqualTo("one"));
        Assert.That(array[1], Is.EqualTo("two"));
    }

    [Test]
    public void KeyCollection_Contains_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.IsTrue(map.Keys.Contains(1));
        Assert.That(map.Keys.Contains(2), Is.False);
    }

    [Test]
    public void ValueCollection_Contains_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        Assert.IsTrue(map.Values.Contains("one"));
        Assert.That(map.Values.Contains("two"), Is.False);
    }

    [Test]
    public void Resize_GrowsBeyondLoadFactor()
    {
        // Arrange
        using var map = new Umap<int, Ustring>(1); // Small capacity to force resize quickly

        // Act
        for (int i = 0; i < 10; i++)
        {
            map.Add(i, i.ToString());
        }

        // Assert
        Assert.That(map.Count, Is.EqualTo(10));
        for (int i = 0; i < 10; i++)
        {
            Assert.IsTrue(map.ContainsKey(i));
            Assert.That(map[i], Is.EqualTo(i.ToString()));
        }
    }

    [Test]
    public void Dispose_PreventsOperations()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => map.Add(2, "two"));
        Assert.Throws<ObjectDisposedException>(() => _ = map[1]);
        Assert.Throws<ObjectDisposedException>(() => map.ContainsKey(1));
        Assert.Throws<ObjectDisposedException>(() => map.Remove(1));
        Assert.Throws<ObjectDisposedException>(() => map.TryGetValue(1, out _));
        Assert.Throws<ObjectDisposedException>(() => _ = map[1]);
        Assert.Throws<ObjectDisposedException>(() => map.TryGetValueRefOrAddDefault(2, out _));
        Assert.Throws<ObjectDisposedException>(() => map.Clear());
        Assert.Throws<ObjectDisposedException>(() => map.CopyTo(new KeyValuePair<int, Ustring>[1], 0));
    }

    [Test]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Dispose();

        // Act & Assert
        Assert.DoesNotThrow(() => map.Dispose());
    }

    [Test]
    public void ICollectionContains_KeyValuePair_Succeeds()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act & Assert
        var collection = (ICollection<KeyValuePair<int, Ustring>>)map;
        Assert.IsTrue(collection.Contains(new KeyValuePair<int, Ustring>(1, "one")));
        Assert.That(collection.Contains(new KeyValuePair<int, Ustring>(1, "two")), Is.False);
    }

    [Test]
    public void ICollectionRemove_KeyValuePair_CallsKeyRemove()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");

        // Act
        var collection = (ICollection<KeyValuePair<int, Ustring>>) map;
        var result = collection.Remove(new KeyValuePair<int, Ustring>(1, "one"));

        // Assert
        Assert.IsTrue(result);
        Assert.That(collection, Is.Empty);
        Assert.That(map, Is.Not.Empty);
    }

    [Test]
    public void IEnumerableGetEnumerator_IteratesCorrectly()
    {
        // Arrange
        using var map = new Umap<int, Ustring>();
        map.Add(1, "one");
        map.Add(2, "two");

        // Act
        var enumerable = (IEnumerable)map;
        var enumerator = enumerable.GetEnumerator();
        var list = new List<KeyValuePair<int, Ustring>>();
        while (enumerator.MoveNext())
        {
            list.Add((KeyValuePair<int, Ustring>)enumerator.Current);
        }

        // Assert
        Assert.That(list.Count, Is.EqualTo(2));
        CollectionAssert.Contains(list, new KeyValuePair<int, Ustring>(1, "one"));
        CollectionAssert.Contains(list, new KeyValuePair<int, Ustring>(2, "two"));
    }

    [Test]
    public void ContainsKey_NullKey_DoesNotThrowButReturnsFalse()
    {
        // Arrange
        using var map = new Umap<Ustring, int>();

        // Act & Assert
        Assert.That(map.ContainsKey(null!), Is.False); // Should not throw, as per implementation
    }

    [Test]
    public void TryGetValue_NullKey_DoesNotThrowReturnsFalse()
    {
        // Arrange
        using var map = new Umap<Ustring, int>();

        // Act & Assert
        Assert.That(map.TryGetValue(null!, out _), Is.False);
    }

}