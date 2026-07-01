namespace Gubbins.Collections.Tests;

[TestFixture]
public class RedBlackTreeTests
{
    [Test]
    public void Constructor_WithNoParameters_CreatesEmptyTree()
    {
        var tree = new RedBlackTree<int>();
        Assert.That(tree, Is.Empty);
        Assert.That(tree.Any(), Is.False);
    }

    [Test]
    public void Add_SingleItem_AddsToTree()
    {
        var tree = new RedBlackTree<int> {5};
        Assert.That(tree, Has.Count.EqualTo(1));
        Assert.That(tree, Does.Contain(5));
    }

    [Test]
    public void Add_MultipleItems_AddsToTree()
    {
        var tree = new RedBlackTree<int>();
        tree.Add(5);
        tree.Add(3);
        tree.Add(7);

        Assert.Multiple(() =>
        {
            Assert.That(tree, Has.Count.EqualTo(3));
            Assert.That(tree, Does.Contain(5));
            Assert.That(tree, Does.Contain(3));
            Assert.That(tree, Does.Contain(7));
        });
    }

    [Test]
    public void Add_DuplicateItems_WhenAllowed_AddsToTree()
    {
        var tree = new RedBlackTree<int>(allowDuplicate: true)
        {
            5,
            5
        };
        Assert.That(tree, Has.Count.EqualTo(2));
    }

    [Test]
    public void Add_DuplicateItems_WhenNotAllowed_ThrowsException()
    {
        // ReSharper disable once CollectionNeverQueried.Local
        var tree = new RedBlackTree<int>(allowDuplicate: false) {5};
        Assert.Throws<ArgumentException>(() => tree.Add(5));
    }

    [Test]
    public void Remove_ExistingItem_RemovesFromTree()
    {
        var tree = new RedBlackTree<int>();
        tree.Add(5);
        tree.Add(3);
        tree.Add(7);

        var result = tree.Remove(3);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(tree, Has.Count.EqualTo(2));
            Assert.That(tree, Does.Not.Contain(3));
        });
    }

    [Test]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var tree = new RedBlackTree<int>();
        tree.Add(5);
        var result = tree.Remove(10);
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(tree, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var tree = new RedBlackTree<int> {5};
        var result = tree.Contains(5);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var tree = new RedBlackTree<int> {5};
        var result = tree.Contains(10);
        Assert.That(result, Is.False);
    }

    [Test]
    public void Clear_RemovesAllItems()
    {
        var tree = new RedBlackTree<int>
        {
            5,
            3,
            7
        };

        tree.Clear();

        Assert.That(tree, Is.Empty);
        Assert.That(tree, Does.Not.Contain(5));
    }

    [Test]
    public void CopyTo_CopiesItemsToArray()
    {
        var tree = new RedBlackTree<int>
        {
            5,
            3,
            7
        };

        var array = new int[3];
        tree.CopyTo(array, 0);

        CollectionAssert.AreEquivalent(new[] {3, 5, 7}, array);
    }

    [Test]
    public void GetEnumerator_ReturnsItemsInOrder()
    {
        var tree = new RedBlackTree<int>
        {
            5,
            3,
            7,
            1,
            4
        };

        var items = tree.ToList();

        CollectionAssert.AreEqual(new[] {1, 3, 4, 5, 7}, items);
    }

    [Test]
    public void Root_IsAlwaysBlack()
    {
        var tree = new RedBlackTree<int> {5};

        Assert.That(tree.InternalRoot, Is.Not.Null);

        Assert.That(tree.InternalRoot.IsRed, Is.False);

        tree.Add(3);
        Assert.That(tree.InternalRoot.IsRed, Is.False);

        tree.Add(7);
        Assert.That(tree.InternalRoot.IsRed, Is.False);
    }

    [Test]
    public void RedNodes_HaveBlackChildren()
    {
        var tree = new RedBlackTree<int> {10, 5, 15, 3, 7, 12, 17, 1, 4, 6, 8, 11, 13, 16, 18};

        // Verify red nodes have black children
        VerifyRedBlackProperties(tree.InternalRoot);
    }

    [Test]
    public void BlackHeight_IsConsistentAcrossAllPaths()
    {
        var tree = new RedBlackTree<int>();
        var values = new[] {10, 5, 15, 3, 7, 12, 17, 1, 4, 6, 8, 11, 13, 16, 18};

        foreach (var value in values)
        {
            tree.Add(value);
        }

        // Verify black height is consistent
        var blackHeight = -1;
        VerifyBlackHeight(tree.InternalRoot, 0, ref blackHeight);
    }

    [Test]
    public void LargeNumberOfItems_MaintainsRedBlackProperties()
    {
        var tree = new RedBlackTree<int>();
        var random = new Random();
        var values = new HashSet<int>();

        // Add 1000 random values
        for (var i = 0; i < 1000; i++)
        {
            var value = random.Next(0, 10000);
            if (values.Add(value))
            {
                tree.Add(value);
                // 每添加100个元素验证一次属性
                if (values.Count % 100 == 0)
                {
                    Assert.That(tree.InternalRoot, Is.Not.Null);
                    Assert.IsFalse(tree.InternalRoot.IsRed, "Root should always be black");
                    VerifyRedBlackProperties(tree.InternalRoot);
                }
            }
        }

        Assert.That(tree.InternalRoot != null && tree.InternalRoot.IsRed, Is.False, "Root should always be black");
        VerifyRedBlackProperties(tree.InternalRoot);

        var blackHeight = -1;
        VerifyBlackHeight(tree.InternalRoot, 0, ref blackHeight);
    }

    [Test]
    public void Remove_RandomItems_MaintainsRedBlackProperties()
    {
        var tree = new RedBlackTree<int>();
        var values = new List<int> {10, 5, 15, 3, 7, 12, 17, 1, 4, 6, 8, 11, 13, 16, 18};

        foreach (var value in values)
        {
            tree.Add(value);
        }

        Assert.That(tree.InternalRoot, Is.Not.Null);
        Assert.That(tree.InternalRoot.IsRed, Is.False, "Root should always be black");
        VerifyRedBlackProperties(tree.InternalRoot);

        var blackHeight = -1;
        VerifyBlackHeight(tree.InternalRoot, 0, ref blackHeight);

        tree.Remove(5);
        Assert.That(tree.InternalRoot.IsRed, Is.False, "Root should always be black");
        VerifyRedBlackProperties(tree.InternalRoot);

        tree.Remove(12);
        Assert.IsFalse(tree.InternalRoot.IsRed, "Root should always be black");
        VerifyRedBlackProperties(tree.InternalRoot);

        tree.Remove(17);

        Assert.IsFalse(tree.InternalRoot.IsRed, "Root should always be black");
        VerifyRedBlackProperties(tree.InternalRoot);

        blackHeight = -1;
        VerifyBlackHeight(tree.InternalRoot, 0, ref blackHeight);
    }

    [Test]
    public void Clear_TreeNotEmpty_ShouldEmptyTree()
    {
        var tree = new RedBlackTree<int>
        {
            1,
            2
        };
        tree.Clear();
        Assert.That(tree, Is.Empty);
    }

    [Test]
    public void Remove_ExistingItem_ShouldDecreaseCount()
    {
        var tree = new RedBlackTree<int>
        {
            1,
            2
        };
        tree.Remove(1);
        Assert.That(tree, Has.Count.EqualTo(1));
    }

    [Test]
    public void CopyTo_CopyAllItems_ShouldContainAllItems()
    {
        var tree = new RedBlackTree<int>
        {
            1,
            2
        };
        var array = new int[tree.Count];
        tree.CopyTo(array, 0);
        CollectionAssert.AreEqual(new[] {1, 2}, array);
    }

    [Test]
    public void Contains_ExistingItem_ShouldReturnTrue()
    {
        var tree = new RedBlackTree<int> {1};
        Assert.That(tree, Does.Contain(1));
    }

    [Test]
    public void GetEnumerator_TraverseTree_ShouldReturnInOrder()
    {
        var tree = new RedBlackTree<int>
        {
            2,
            1,
            3
        };

        int[] expected = {1, 2, 3};
        var i = 0;

        foreach (var item in tree)
        {
            Assert.That(item, Is.EqualTo(expected[i++]));
        }
    }

    // Helper method to verify red-black properties
    private static void VerifyRedBlackProperties(RedBlackTree<int>.Node? node)
    {
        if (node == null) return;

        if (node.IsRed)
        {
            Assert.That(node.Left?.IsRed ?? false, Is.False, "Red node has red left child");
            Assert.That(node.Right?.IsRed ?? false, Is.False, "Red node has red right child");
        }

        VerifyRedBlackProperties(node.Left!);
        VerifyRedBlackProperties(node.Right!);
    }

    // Helper method to verify black height consistency
    private static void VerifyBlackHeight(RedBlackTree<int>.Node? node, int currentBlackHeight, ref int expectedBlackHeight)
    {
        if (node == null)
        {
            // Reached a leaf (null is considered black)
            if (expectedBlackHeight == -1)
            {
                expectedBlackHeight = currentBlackHeight;
            }
            else
            {
                Assert.That(currentBlackHeight, Is.EqualTo(expectedBlackHeight), "Black height is not consistent across all paths");
            }

            return;
        }

        var nextBlackHeight = currentBlackHeight + (node.IsRed ? 0 : 1);
        VerifyBlackHeight(node.Left, nextBlackHeight, ref expectedBlackHeight);
        VerifyBlackHeight(node.Right, nextBlackHeight, ref expectedBlackHeight);
    }
}