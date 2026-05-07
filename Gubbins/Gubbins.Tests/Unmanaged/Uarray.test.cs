using System.Collections;

namespace Gubbins.Unmanaged.Tests;

[TestFixture]
public sealed class UarrayTests
{
    [Test]
    public void ConstructorWithLengthTest()
    {
        const int length = 5;
        var array = new Uarray<int>(length);

        Assert.Multiple(() =>
        {
            Assert.That(array, Has.Count.EqualTo(length));
            Assert.That(array.IsValid, Is.True);
            Assert.That(array.IsEmpty, Is.False);
        });

        array.Dispose();
    }

    [Test]
    public void DefaultConstructorTest()
    {
        var array = new Uarray<int>();

        Assert.Multiple(() =>
        {
            Assert.That(array, Is.Empty);
            Assert.That(array.IsEmpty, Is.True);
            Assert.That(array.IsValid, Is.False);
        });
        array.Dispose();
    }

    [Test]
    public void IndexerShouldThrowIndexOutOfRangeExceptionForNegativeIndex()
    {
        var array = new Uarray<int>(5);
        Assert.Throws<IndexOutOfRangeException>(() => { _ = array[-1]; });
        array.Dispose();
    }

    [Test]
    public void IndexOutOfRangeExceptionTest()
    {
        var array = new Uarray<int>(3);
        array[0] = 1;
        array[1] = 2;
        array[2] = 3;

        Assert.Multiple(() =>
        {
            Assert.That(() => _         = array[-1], Throws.TypeOf<IndexOutOfRangeException>());
            Assert.That(() => _         = array[3], Throws.TypeOf<IndexOutOfRangeException>());
            Assert.That(() => array[-1] = 5, Throws.TypeOf<IndexOutOfRangeException>());
            Assert.That(() => array[3]  = 5, Throws.TypeOf<IndexOutOfRangeException>());
        });

        array.Dispose();
    }

    [Test]
    public void SpanConstructorTest()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var span = new ReadOnlySpan<int>(sourceData);

        var uarray = new Uarray<int>(span);

        Assert.Multiple(() =>
        {
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray.IsEmpty, Is.False);
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[1], Is.EqualTo(2));
            Assert.That(uarray[2], Is.EqualTo(3));
            Assert.That(uarray[3], Is.EqualTo(4));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void IsValidReturnsFalseWhenArrayIsDisposed()
    {
        var array = new Uarray<int>(5);
        Assert.That(array.IsValid, Is.True);

        array.Dispose();

        Assert.That(array.IsValid, Is.False);
    }

    [Test]
    public void SliceThrowsObjectDisposedExceptionWhenDisposed()
    {
        var array = new Uarray<int>(5);
        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Slice(0, 2));
    }

    [Test]
    public void DisposeTest()
    {
        var array = new Uarray<int>(5);

        Assert.Multiple(() =>
        {
            Assert.That(array.IsValid, Is.True);
            Assert.That(array, Has.Count.EqualTo(5));
        });

        array.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void GetHashCodeTest()
    {
        var array = new Uarray<int>(5);
        var hashCode = array.GetHashCode();

        Assert.That(hashCode, Is.Not.EqualTo(0));

        array.Dispose();
    }

    [Test]
    public void AsSpanMethodsReturnCorrectSpanWithSpecifiedParameters()
    {
        var array = new Uarray<int>(10);
        for (int i = 0; i < 10; i++)
        {
            array[i] = i + 1;
        }

        var spanWithStartAndLength = array.AsSpan(2, 5);
        var spanWithStart = array.AsSpan(3);
        var spanFull = array.AsSpan();


        Assert.That(spanWithStartAndLength.Length, Is.EqualTo(5));
        Assert.That(spanWithStartAndLength[0], Is.EqualTo(3));
        Assert.That(spanWithStartAndLength[4], Is.EqualTo(7));

        Assert.That(spanWithStart.Length, Is.EqualTo(7));
        Assert.That(spanWithStart[0], Is.EqualTo(4));
        Assert.That(spanWithStart[6], Is.EqualTo(10));

        Assert.That(spanFull.Length, Is.EqualTo(10));
        Assert.That(spanFull[0], Is.EqualTo(1));
        Assert.That(spanFull[9], Is.EqualTo(10));

        array.Dispose();
    }

    [Test]
    public void SequenceEqualReturnsFalseWhenArraysHaveDifferentSequences()
    {
        var array1 = new Uarray<int>(new ReadOnlySpan<int>(new[] {1, 2, 3, 4, 5}));
        var array2 = new ReadOnlySpan<int>(new[] {1, 2, 3, 4, 6});

        var result = array1.SequenceEqual(array2);

        Assert.That(result, Is.False);

        array1.Dispose();
    }

    [Test]
    public void IListMethodsShouldThrowNotSupportedException()
    {
        var array = new Uarray<int>(3);
        IList ilist = array;
        Assert.That(() => ilist.Add(5), Throws.TypeOf<NotSupportedException>().With.Message.EqualTo("Array is fixed size."));
        Assert.That(() => ilist.Insert(0, 5), Throws.TypeOf<NotSupportedException>().With.Message.EqualTo("Array is fixed size."));
        Assert.That(() => ilist.Remove(5), Throws.TypeOf<NotSupportedException>().With.Message.EqualTo("Array is fixed size."));
        Assert.That(() => ilist.RemoveAt(0), Throws.TypeOf<NotSupportedException>().With.Message.EqualTo("Array is fixed size."));

        array.Dispose();
    }

    [Test]
    public void ToArray_WithDisposedOriginFalse_PreservesOriginalArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var managedArray = uarray.ToArray(disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(managedArray, Is.EqualTo(sourceData));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToArray_WithDefaultDisposedOriginParameterShouldDisposeOriginalArrayAndReturnCorrectManagedArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var array = new Uarray<int>(sourceData);

        var result = array.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(5));
            Assert.That(result[0], Is.EqualTo(1));
            Assert.That(result[1], Is.EqualTo(2));
            Assert.That(result[2], Is.EqualTo(3));
            Assert.That(result[3], Is.EqualTo(4));
            Assert.That(result[4], Is.EqualTo(5));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToArray_WithStartIndexAndDisposedOriginFalsePreservesOriginalArrayAndCopiesFromSpecifiedPosition()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var managedArray = uarray.ToArray(2, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(managedArray.Length, Is.EqualTo(3));
            Assert.That(managedArray[0], Is.EqualTo(3));
            Assert.That(managedArray[1], Is.EqualTo(4));
            Assert.That(managedArray[2], Is.EqualTo(5));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToArray_WithStartIndexShouldCopyElementsFromSpecifiedPositionAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50};
        var array = new Uarray<int>(sourceData);

        var result = array.ToArray(2, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(30));
            Assert.That(result[1], Is.EqualTo(40));
            Assert.That(result[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToArray_WithStartAndLengthParametersPreservesOriginalArrayWhenDisposedOriginIsFalse()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var result = array.ToArray(2, 3, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(30));
            Assert.That(result[1], Is.EqualTo(40));
            Assert.That(result[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.True);
            Assert.That(array, Has.Count.EqualTo(7));
            Assert.That(array[0], Is.EqualTo(10));
            Assert.That(array[6], Is.EqualTo(70));
        });

        array.Dispose();
    }

    [Test]
    public void ToArray_WithStartAndLengthShouldCreateCorrectArrayAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var result = array.ToArray(2, 3, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(30));
            Assert.That(result[1], Is.EqualTo(40));
            Assert.That(result[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToList_WithDisposedOriginFalse_PreservesOriginalArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var managedList = uarray.ToList(disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Is.EqualTo(sourceData));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToList_WithDefaultDisposedOriginParameterShouldDisposeOriginalArrayAndReturnCorrectManagedArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var array = new Uarray<int>(sourceData);

        var managedList = array.ToList();

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Is.Not.Null);
            Assert.That(managedList, Has.Count.EqualTo(5));
            Assert.That(managedList[0], Is.EqualTo(1));
            Assert.That(managedList[1], Is.EqualTo(2));
            Assert.That(managedList[2], Is.EqualTo(3));
            Assert.That(managedList[3], Is.EqualTo(4));
            Assert.That(managedList[4], Is.EqualTo(5));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToList_WithStartIndexAndDisposedOriginFalsePreservesOriginalArrayAndCopiesFromSpecifiedPosition()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var managedList = uarray.ToList(2, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Has.Count.EqualTo(3));
            Assert.That(managedList[0], Is.EqualTo(3));
            Assert.That(managedList[1], Is.EqualTo(4));
            Assert.That(managedList[2], Is.EqualTo(5));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToList_WithStartIndexShouldCopyElementsFromSpecifiedPositionAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50};
        var array = new Uarray<int>(sourceData);

        var managedList = array.ToList(2, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Is.Not.Null);
            Assert.That(managedList, Has.Count.EqualTo(3));
            Assert.That(managedList[0], Is.EqualTo(30));
            Assert.That(managedList[1], Is.EqualTo(40));
            Assert.That(managedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToList_WithStartAndLengthParametersPreservesOriginalArrayWhenDisposedOriginIsFalse()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var managedList = array.ToList(2, 3, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Is.Not.Null);
            Assert.That(managedList, Has.Count.EqualTo(3));
            Assert.That(managedList[0], Is.EqualTo(30));
            Assert.That(managedList[1], Is.EqualTo(40));
            Assert.That(managedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.True);
            Assert.That(array, Has.Count.EqualTo(7));
            Assert.That(array[0], Is.EqualTo(10));
            Assert.That(array[6], Is.EqualTo(70));
        });

        array.Dispose();
    }

    [Test]
    public void ToList_WithStartAndLengthShouldCreateCorrectArrayAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var managedList = array.ToList(2, 3, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(managedList, Is.Not.Null);
            Assert.That(managedList, Has.Count.EqualTo(3));
            Assert.That(managedList[0], Is.EqualTo(30));
            Assert.That(managedList[1], Is.EqualTo(40));
            Assert.That(managedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToUlist_WithDisposedOriginFalse_PreservesOriginalArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var unmanagedList = uarray.ToUlist(disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList, Is.EqualTo(sourceData));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToUlist_WithDefaultDisposedOriginParameterShouldDisposeOriginalArrayAndReturnCorrectManagedArray()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var array = new Uarray<int>(sourceData);

        var unmanagedList = array.ToUlist();

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList.IsValid, Is.True);
            Assert.That(unmanagedList, Has.Count.EqualTo(5));
            Assert.That(unmanagedList[0], Is.EqualTo(1));
            Assert.That(unmanagedList[1], Is.EqualTo(2));
            Assert.That(unmanagedList[2], Is.EqualTo(3));
            Assert.That(unmanagedList[3], Is.EqualTo(4));
            Assert.That(unmanagedList[4], Is.EqualTo(5));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToUlist_WithStartIndexAndDisposedOriginFalsePreservesOriginalArrayAndCopiesFromSpecifiedPosition()
    {
        var sourceData = new[] {1, 2, 3, 4, 5};
        var uarray = new Uarray<int>(sourceData);

        var unmanagedList = uarray.ToUlist(2, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList, Has.Count.EqualTo(3));
            Assert.That(unmanagedList[0], Is.EqualTo(3));
            Assert.That(unmanagedList[1], Is.EqualTo(4));
            Assert.That(unmanagedList[2], Is.EqualTo(5));
            Assert.That(uarray.IsValid, Is.True);
            Assert.That(uarray, Has.Count.EqualTo(5));
            Assert.That(uarray[0], Is.EqualTo(1));
            Assert.That(uarray[4], Is.EqualTo(5));
        });

        uarray.Dispose();
    }

    [Test]
    public void ToUlist_WithStartIndexShouldCopyElementsFromSpecifiedPositionAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50};
        var array = new Uarray<int>(sourceData);

        var unmanagedList = array.ToUlist(2, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList.IsValid, Is.True);
            Assert.That(unmanagedList, Has.Count.EqualTo(3));
            Assert.That(unmanagedList[0], Is.EqualTo(30));
            Assert.That(unmanagedList[1], Is.EqualTo(40));
            Assert.That(unmanagedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }

    [Test]
    public void ToUlist_WithStartAndLengthParametersPreservesOriginalArrayWhenDisposedOriginIsFalse()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var unmanagedList = array.ToUlist(2, 3, disposedOrigin: false);

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList.IsValid, Is.True);
            Assert.That(unmanagedList, Has.Count.EqualTo(3));
            Assert.That(unmanagedList[0], Is.EqualTo(30));
            Assert.That(unmanagedList[1], Is.EqualTo(40));
            Assert.That(unmanagedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.True);
            Assert.That(array, Has.Count.EqualTo(7));
            Assert.That(array[0], Is.EqualTo(10));
            Assert.That(array[6], Is.EqualTo(70));
        });

        array.Dispose();
    }

    [Test]
    public void ToUlist_WithStartAndLengthShouldCreateCorrectArrayAndDisposeOriginalWhenDisposedOriginIsTrue()
    {
        var sourceData = new[] {10, 20, 30, 40, 50, 60, 70};
        var array = new Uarray<int>(sourceData);

        var unmanagedList = array.ToUlist(2, 3, disposedOrigin: true);

        Assert.Multiple(() =>
        {
            Assert.That(unmanagedList.IsValid, Is.True);
            Assert.That(unmanagedList, Has.Count.EqualTo(3));
            Assert.That(unmanagedList[0], Is.EqualTo(30));
            Assert.That(unmanagedList[1], Is.EqualTo(40));
            Assert.That(unmanagedList[2], Is.EqualTo(50));
            Assert.That(array.IsValid, Is.False);
            Assert.That(array, Is.Empty);
        });
    }
}