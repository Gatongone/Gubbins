namespace Gubbins.Unmanaged.Tests;

[TestFixture]
public sealed class UlistTests
{
    [Test]
    public void Insert_IndexGreaterThanCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        // Count is now 2

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(3, 42));
    }

    [Test]
    public void Insert_ShouldSuccessfullyInsertElementAtValidIndexAndShiftExistingElements()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(4);
        list.Add(5);

        // Act
        list.Insert(2, 3);

        // Assert
        Assert.That(list, Has.Count.EqualTo(5));
        Assert.Multiple(() =>
        {
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
            Assert.That(list[4], Is.EqualTo(5));
        });
        list.Dispose();
    }

    [Test]
    public void IndexOf_NonExistingElement_ReturnsMinusOne()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        // Act
        var result = list.IndexOf(99);

        // Assert
        Assert.That(result, Is.EqualTo(-1));

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void IndexerGet_IndexGreGaterThanOrEqualToCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        // Act & Assert
        Assert.DoesNotThrow(() => { _                     = list[0]; });
        Assert.DoesNotThrow(() => { _                     = list[1]; });
        Assert.DoesNotThrow(() => { _                     = list[2]; });
        Assert.Throws<IndexOutOfRangeException>(() => { _ = list[3]; });

        list.Dispose();
    }

    [Test]
    public void Indexer_ShouldThrowArgumentOutOfRangeException_WhenAccessingNegativeIndex()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        // Act & Assert
        Assert.Throws<IndexOutOfRangeException>(() => { _ = list[-1]; });
    }

    [Test]
    public void Should_Initialize_Ulist_From_ReadOnlySpan_With_Correct_Count_And_Elements()
    {
        // Arrange
        var sourceArray = new[] {1, 2, 3, 4, 5};
        var span = new ReadOnlySpan<int>(sourceArray);

        // Act
        var list = new Ulist<int>(span);

        // Assert
        Assert.That(list, Has.Count.EqualTo(5));

        for (var i = 0; i < sourceArray.Length; i++)
        {
            Assert.That(((IList<int>) list)[i], Is.EqualTo(sourceArray[i]));
        }

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void Constructor_WithSpecifiedCapacity_ShouldInitializeWithCapacityAndZeroCount()
    {
        // Arrange
        const int expectedCapacity = 10;

        // Act
        var list = new Ulist<int>(expectedCapacity);

        // Assert
        Assert.That(list.Count, Is.EqualTo(0));

        // Verify capacity by adding elements up to the expected capacity without triggering expansion
        for (int i = 0; i < expectedCapacity; i++)
        {
            list.Add(i);
        }

        Assert.That(list, Has.Count.EqualTo(expectedCapacity));

        // Clean up
        list.Dispose();
    }

    [Test]
    public void Ulist_DefaultConstructor_ShouldInitializeEmptyListWithZeroCountAndCapacity()
    {
        // Arrange & Act
        var list = new Ulist<int>();

        // Assert
        Assert.That(list.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveAt_ShouldWorkCorrectlyOnListWithMaximumCapacityElements()
    {
        // Arrange
        const int capacity = 5;
        var list = new Ulist<int>(capacity);

        // Fill list to maximum capacity
        for (int i = 0; i < capacity; i++)
        {
            list.Add(i + 1); // Adding values 1, 2, 3, 4, 5
        }

        // Act - Remove element at index 2 (value 3)
        list.RemoveAt(2);

        // Assert
        Assert.That(list, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(4)); // Element shifted left
            Assert.That(list[3], Is.EqualTo(5)); // Element shifted left
        });

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void RemoveAt_ShouldThrowIndexOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);
        list.Add(40);

        // Act
        list.RemoveAt(1); // Remove element at index 1 (value 20)

        // Assert
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(list[0], Is.EqualTo(10));
            Assert.That(list[1], Is.EqualTo(30)); // Elements shifted left
            Assert.That(list[2], Is.EqualTo(40));
            Assert.Throws<IndexOutOfRangeException>(() => { _ = list[3]; });
        });
        list.Dispose();
    }

    [Test]
    public void RemoveAt_ShouldDecreaseCountByOne_AfterSuccessfulRemoval()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        var initialCount = list.Count;

        // Act
        list.RemoveAt(1);

        // Assert
        Assert.That(list, Has.Count.EqualTo(initialCount - 1));

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void RemoveAt_ShouldSuccessfullyRemoveMiddleElementAndShiftRemainingElementsLeft()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);

        // Act
        list.RemoveAt(2); // Remove element at index 2 (value 3)

        // Assert
        Assert.That(list, Has.Count.EqualTo(4));
        Assert.Multiple(() =>
        {
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(4)); // Element 4 shifted left
            Assert.That(list[3], Is.EqualTo(5)); // Element 5 shifted left
        });

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void RemoveAt_ShouldSuccessfullyRemoveElementAtIndexZeroFromSingleElementList()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(42);

        // Act
        list.RemoveAt(0);

        // Assert
        Assert.That(list.Count, Is.EqualTo(0));
        Assert.That(list.IsEmpty, Is.True);

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void RemoveAt_IndexEqualsCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        // Count is now 3

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));

        list.Dispose();
    }

    [Test]
    public void RemoveAt_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void RemoveAt_IndexGreaterThanCount_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        // Count is now 2

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));

        list.Dispose();
    }

    [Test]
    public void RemoveAt_ShouldSuccessfullyRemoveLastElementWithoutShiftingOtherElements()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);

        // Act
        list.RemoveAt(3); // Remove last element (4)

        // Assert
        Assert.That(list, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
        });

        // Cleanup
        list.Dispose();
    }

    [Test]
    public void AsUarray_ShouldReturnViewWithCorrectCountWhenListHasElements()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        // Act
        var result = list.AsUarray();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo(10));
            Assert.That(result[1], Is.EqualTo(20));
            Assert.That(result[2], Is.EqualTo(30));
        });

        // Cleanup
        list.Dispose();
        result.Dispose();
    }

    [Test]
    public void AsUarray_ShouldReturnEmptyViewWhenListIsEmpty()
    {
        // Arrange
        var list = new Ulist<int>(5);

        // Act
        var result = list.AsUarray();

        // Assert
        Assert.That(result.Count, Is.EqualTo(0));

        // Cleanup
        list.Dispose();
        result.Dispose();
    }

    [Test]
    public void ToArray_WithDisposeOriginFalse_ShouldCreateManagedArrayAndPreserveOriginalList()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);

        // Act
        var result = list.ToArray(disposeOrigin: false);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(4));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(result[1], Is.EqualTo(2));
            Assert.That(result[2], Is.EqualTo(3));
            Assert.That(result[3], Is.EqualTo(4));
            Assert.That(list, Has.Count.EqualTo(4));
            Assert.That(list.IsValid, Is.True);
            Assert.That(list[0], Is.EqualTo(1));
            Assert.That(list[1], Is.EqualTo(2));
            Assert.That(list[2], Is.EqualTo(3));
            Assert.That(list[3], Is.EqualTo(4));
        });
        // Cleanup
        list.Dispose();
    }

    [Test]
    public void ToUarray_WithStartAndLength_ShouldCreateCorrectSubsetAndDisposeOriginWhenDisposeOriginIsTrue()
    {
        // Arrange
        var list = new Ulist<int>(10);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);
        list.Add(6);

        // Act
        var result = list.ToUarray(2, 3, disposeOrigin: true);

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.Multiple(() =>
        {
            Assert.That(result[0], Is.EqualTo(3));
            Assert.That(result[1], Is.EqualTo(4));
            Assert.That(result[2], Is.EqualTo(5));
            Assert.That(list.IsValid, Is.False);
        });

        // Cleanup
        result.Dispose();
    }

    [Test]
    public void ToArray_WithDisposeOriginTrue_ShouldCreateManagedArrayAndDisposeOriginalList()
    {
        // Arrange
        var list = new Ulist<int>(5);
        list.Add(10);
        list.Add(20);
        list.Add(30);

        // Act
        var result = list.ToArray();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(10));
            Assert.That(result[1], Is.EqualTo(20));
            Assert.That(result[2], Is.EqualTo(30));
            Assert.That(list.IsValid, Is.False);
        });
    }

    [Test]
    public void ToUarray_WithStartOnly_ShouldCreateSubsetFromStartToEndAndDisposeOriginWhenDisposeOriginIsTrue()
    {
        // Arrange
        var list = new Ulist<int>(10);
        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);
        list.Add(6);

        // Act
        var result = list.ToUarray(2, disposeOrigin: true);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(4));
            Assert.That(result[0], Is.EqualTo(3));
            Assert.That(result[1], Is.EqualTo(4));
            Assert.That(result[2], Is.EqualTo(5));
            Assert.That(result[3], Is.EqualTo(6));
            Assert.That(list.IsValid, Is.False);
        });

        // Cleanup
        result.Dispose();
    }
}