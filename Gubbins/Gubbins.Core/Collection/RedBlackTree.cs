using System.Runtime.CompilerServices;

namespace Gubbins.Collection;

/// <summary>
/// Sorted set with a kind of self-balancing binary lookup tree.
/// For a detailed description of the algorithm, take a look at "Algorithms" by Robert Sedgewick.
/// </summary>
/// <typeparam name="TNode">Tree node type.</typeparam>
public class RedBlackTree<TNode> : Tree<TNode>, ICollection<TNode>
{
    private int   m_Count;
    private Node? m_Root;
    internal Node? InternalRoot => m_Root;
    protected override ITreeNode<TNode> Root => m_Root!;
    public override int Count => m_Count;
    public override TreeTraversalType DefaultTraversalType => TreeTraversalType.Inorder;
    public bool IsReadOnly => false;
    public readonly  bool             AllowDuplicate;
    private readonly IComparer<TNode> m_Comparer;

    /// <param name="allowDuplicate">If not allow duplicate, it would throw ArgumentException when item is duplicate.</param>
    public RedBlackTree(bool allowDuplicate = true) : this(Comparer<TNode>.Default, allowDuplicate) { }

    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    /// <param name="allowDuplicate">If not allow duplicate, it would throw ArgumentException when item is duplicate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the comparer is null.</exception>
    public RedBlackTree(IComparer<TNode> comparer, bool allowDuplicate = true) => (m_Comparer, AllowDuplicate) = (comparer ?? throw new ArgumentNullException(nameof(comparer)), allowDuplicate);

    /// <param name="collection">
    /// The collection whose elements should be added to the end of the RedBlackTree&lt;T&gt;.
    /// The collection cannot contain elements that are null, if type T is a reference type.
    /// </param>
    /// <param name="comparer">The default comparer to use for comparing objects.</param>
    /// <param name="allowDuplicate">If not allow duplicate, it would throw ArgumentException when item is duplicate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the collection contains null elements.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the comparer is null.</exception>
    public RedBlackTree(IEnumerable<TNode> collection, IComparer<TNode> comparer, bool allowDuplicate = true) : this(comparer, allowDuplicate) => AddRange(collection);

    /// <param name="collection">
    /// The collection whose elements should be added to the end of the RedBlackTree&lt;T&gt;.
    /// The collection cannot contain elements that are null, if type T is a reference type.
    /// </param>
    /// <param name="allowDuplicate">If not allow duplicate, it would throw ArgumentException when item is duplicate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the collection contains null elements.</exception>
    public RedBlackTree(IEnumerable<TNode> collection, bool allowDuplicate = true) : this(Comparer<TNode>.Default, allowDuplicate) => AddRange(collection);

    /// <summary>
    /// Adds the elements of the given collection to this tree.
    /// </summary>
    /// <param name="collection">
    /// The collection whose elements should be added to the end of the RedBlackTree&lt;T&gt;.
    /// The collection cannot contain elements that are null, if type T is a reference type.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when the collection contains null elements.</exception>
    public void AddRange(IEnumerable<TNode> collection)
    {
        foreach (var item in collection)
        {
            Add(item);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TNode item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        var newNode = new Node(item);

        AddBSTNode(newNode);
        FixAddingViolations(newNode);
        UpdateVersion();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_Root = null;
        UpdateVersion();
        m_Count = 0;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TNode item) => TryGetNode(item, out _);

    /// <inheritdoc/>
    public void CopyTo(TNode[] array, int arrayIndex)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        if (m_Count > array.Length - arrayIndex)
            throw new ArgumentException("Destination array was not long enough. Check the destination index, length, and the array's lower bounds.");
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TNode item)
    {
        if (!TryGetNode(item, out var node))
            return false;
        RemoveBSTNode(node!);
        UpdateVersion();
        return true;
    }

    private void AddBSTNode(Node newNode)
    {
        // If is root.
        if (m_Root == null)
        {
            m_Root = newNode;
            m_Count++;
            return;
        }

        var curNode = m_Root;
        var value = newNode.Value;
        while (curNode != null)
        {
            var cmp = m_Comparer.Compare(value, curNode.Value);

            if (cmp == 0 && !AllowDuplicate)
                throw new ArgumentException("Duplicate node.");

            if (cmp < 0)
            {
                if (curNode.Left == null)
                {
                    curNode.Left   = newNode;
                    newNode.Parent = curNode;
                    break;
                }

                curNode = curNode.Left;
            }
            else
            {
                if (curNode.Right == null)
                {
                    curNode.Right  = newNode;
                    newNode.Parent = curNode;
                    break;
                }

                curNode = curNode.Right;
            }
        }

        m_Count++;
    }

    private void RemoveBSTNode(Node node)
    {
        var parent = node.Parent;
        var left = node.Left;
        var right = node.Right;

        if (left != null && right != null)
        {
            var predecessor = GetPredecessor(node);
            node.Value = predecessor.Value;
            RemoveBSTNode(predecessor);
            return;
        }

        var child = left ?? right;

        if (parent == null)
        {
            m_Root = child;
        }
        else
        {
            if (parent.Left == node)
                parent.Left = child;
            else
                parent.Right = child;
        }

        if (child != null)
            child.Parent = parent;

        if (!node.IsRed)
            FixRemovingViolations(child, parent);

        m_Count--;
    }

    private static Node GetPredecessor(Node node)
    {
        if (node.Left != null)
        {
            var predecessor = node.Left;
            while (predecessor.Right != null)
                predecessor = predecessor.Right;
            return predecessor;
        }

        var current = node;
        var parent = node.Parent;
        while (parent != null &&
            current == parent.Left)
        {
            current = parent;
            parent  = parent.Parent;
        }

        return parent;
    }

    private void FixRemovingViolations(Node? node, Node? parent)
    {
        while (node != m_Root && IsBlack(node))
        {
            var isLeft = parent.Left == node;
            var brother = isLeft ? parent.Right : parent.Left;

            if (IsRed(brother))
            {
                brother.IsRed = false;
                parent.IsRed  = true;
                if (isLeft)
                    LeftRotation(parent);
                else
                    RightRotation(parent);
                brother = isLeft ? parent.Right : parent.Left;
            }

            if (IsBlack(brother.Left) && IsBlack(brother.Right))
            {
                brother.IsRed = true;
                node          = parent;
                parent        = node.Parent;
            }
            else
            {
                if (isLeft && IsBlack(brother.Right))
                {
                    brother.Left.IsRed = false;
                    brother.IsRed      = true;
                    RightRotation(brother);
                    brother = parent.Right;
                }
                else if (!isLeft && IsBlack(brother.Left))
                {
                    brother.Right.IsRed = false;
                    brother.IsRed       = true;
                    LeftRotation(brother);
                    brother = parent.Left;
                }

                brother.IsRed = parent.IsRed;
                parent.IsRed  = false;
                if (isLeft)
                {
                    brother.Right.IsRed = false;
                    LeftRotation(parent);
                }
                else
                {
                    brother.Left.IsRed = false;
                    RightRotation(parent);
                }

                node = m_Root;
                break;
            }
        }

        if (node != null)
            node.IsRed = false;
    }

    private void FixAddingViolations(Node node)
    {
        while (node != m_Root && node.Parent.IsRed)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;
            var uncle = grandparent.Left == parent ? grandparent.Right : grandparent.Left;

            if (IsRed(uncle))
            {
                parent.IsRed      = false;
                uncle.IsRed       = false;
                grandparent.IsRed = true;
                node              = grandparent;
            }
            else
            {
                if (parent == grandparent.Left)
                {
                    if (node == parent.Right)
                    {
                        node = parent;
                        LeftRotation(node);
                        parent = node.Parent;
                    }

                    parent.IsRed      = false;
                    grandparent.IsRed = true;
                    RightRotation(grandparent);
                }
                else
                {
                    if (node == parent.Left)
                    {
                        node = parent;
                        RightRotation(node);
                        parent = node.Parent;
                    }

                    parent.IsRed      = false;
                    grandparent.IsRed = true;
                    LeftRotation(grandparent);
                }
            }
        }

        m_Root.IsRed = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LeftRotation(Node node)
    {
        var right = node.Right;
        node.Right = right.Left;
        if (right.Left != null)
            right.Left.Parent = node;
        right.Parent = node.Parent;
        if (node.Parent == null)
            m_Root = right;
        else if (node == node.Parent.Left)
            node.Parent.Left = right;
        else
            node.Parent.Right = right;
        right.Left  = node;
        node.Parent = right;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RightRotation(Node node)
    {
        var left = node.Left;
        node.Left = left.Right;
        if (left.Right != null)
            left.Right.Parent = node;
        left.Parent = node.Parent;
        if (node.Parent == null)
            m_Root = left;
        else if (node == node.Parent.Right)
            node.Parent.Right = left;
        else
            node.Parent.Left = left;
        left.Right  = node;
        node.Parent = left;
    }

    private bool TryGetNode(TNode value, out Node? node)
    {
        node = m_Root;
        while (node != null)
        {
            var cmp = m_Comparer.Compare(value, node.Value);
            if (cmp < 0)
                node = node.Left;
            else if (cmp > 0)
                node = node.Right;
            else
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsBlack(Node? node) => node is null or {IsRed: false};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsRed(Node? node) => node is {IsRed: true};

    internal class Node(TNode value) : BinaryTreeNode<TNode>(value)
    {
        public Node? Parent;

        public new Node? Left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Node?) base.Left;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => base.Left = value;
        }

        public new Node? Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (Node?) base.Right;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => base.Right = value;
        }

        internal bool IsRed = true;
    }
}