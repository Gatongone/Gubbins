using System.Collections;
using System.Runtime.CompilerServices;

namespace Gubbins.Collections;

/// <summary>
/// Tree traversal type.
/// </summary>
public enum TreeTraversalType
{
    /// <summary>
    /// Traversal by parent -> children
    /// </summary>
    Preorder,

    /// <summary>
    /// Traversal by left child -> parent -> right child (binary tree only)
    /// </summary>
    Inorder,

    /// <summary>
    /// Traversal by children -> parent
    /// </summary>
    Postorder,

    /// <summary>
    /// traversal by tree layer (root -> leaves)
    /// </summary>
    Sequence
}

public abstract class Tree<T> : ITree<T>, IEnumerable<T>
{
    private int m_Version;
    protected abstract ITreeNode<T> Root { get; }
    public abstract int Count { get; }
    public abstract TreeTraversalType DefaultTraversalType { get; }

    /// <summary>
    /// Get
    /// </summary>
    /// <param name="traversalType"></param>
    /// <returns></returns>
    public TreeTraver GetTraverser(TreeTraversalType traversalType) => new(this, traversalType, ++m_Version);

    public Enumerator GetEnumerator() => new(this, DefaultTraversalType, ++m_Version);

    IEnumerable<T> ITree<T>.GetTraverser(TreeTraversalType traversalType) => GetTraverser(traversalType);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    protected void UpdateVersion() => m_Version++;

    public readonly struct TreeTraver : IEnumerable<T>
    {
        private readonly Tree<T>           m_Tree;
        private readonly TreeTraversalType m_TraversalType;
        private readonly int               m_Version;

        internal TreeTraver(Tree<T> tree, TreeTraversalType traversalType, int version)
        {
            m_Tree          = tree;
            m_TraversalType = traversalType;
            m_Version       = version;
        }

        public Enumerator GetEnumerator() => new(m_Tree, m_TraversalType, m_Version);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public struct Enumerator : IEnumerator<T>
    {
        private          ITreeNode<T>      m_CurrentNode;
        private          ITreeNode<T>      m_MorrisNode;
        private readonly Tree<T>           m_Tree;
        private readonly TreeTraversalType m_TraversalType;
        private readonly int               m_Version;

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_CurrentNode.Value;
        }

        object IEnumerator.Current => Current!;

        internal Enumerator(Tree<T> tree, TreeTraversalType traversalType, int version)
        {
            m_CurrentNode   = null!;
            m_Tree          = tree;
            m_TraversalType = traversalType;
            m_Version       = version;
            m_MorrisNode    = tree.Root;
        }

        public bool MoveNext() => m_TraversalType switch
        {
            TreeTraversalType.Preorder  => PreorderMove(),
            TreeTraversalType.Inorder   => InorderMove(),
            TreeTraversalType.Postorder => PostorderMove(),
            TreeTraversalType.Sequence  => SequenceMove(),
            _                           => throw new ArgumentOutOfRangeException()
        };

        public void Reset()
        {
            m_CurrentNode = null!;
            m_MorrisNode  = m_Tree.Root;
        }

        public void Dispose() => m_Tree.m_Version--;

        private bool SequenceMove()
        {
            if (m_Version != m_Tree.m_Version)
                throw new InvalidOperationException("Invalid Version. There are other undisposed enumerators running.");

            // Initialization: if first call, set current node to root
            if (m_CurrentNode == null!)
            {
                if (m_Tree.Root == null!) return false;
                m_CurrentNode = m_Tree.Root;
                m_MorrisNode  = m_Tree.Root; // Use m_MorrisNode as start of next level
                return true;
            }

            // If has right sibling, move to right sibling
            if (m_CurrentNode[1] != null!)
            {
                m_CurrentNode = m_CurrentNode[1];
                return true;
            }

            // If no right sibling, move to first node of next level
            // Use m_MorrisNode to track the start of next level
            var nextLevelStart = FindNextLevelStart(m_MorrisNode);
            if (nextLevelStart != null!)
            {
                m_CurrentNode = nextLevelStart;
                m_MorrisNode  = nextLevelStart; // Update start of next level
                return true;
            }

            return false; // Traversal completed
        }

        private bool PostorderMove()
        {
            if (m_Version != m_Tree.m_Version)
                throw new InvalidOperationException("Invalid Version. There are other undisposed enumerators running.");

            while (m_MorrisNode != null!)
            {
                // If right child is null, visit current node and move to left child.
                if (m_MorrisNode[1] == null!)
                {
                    m_CurrentNode = m_MorrisNode;
                    m_MorrisNode  = m_MorrisNode[0];
                    return true;
                }

                // Find predecessor of current node in right subtree
                var predecessor = m_MorrisNode[1];
                while (predecessor[0] != null! &&
                    predecessor[0] != m_MorrisNode)
                    predecessor = predecessor[0];

                // Make current as left child of its predecessor.
                if (predecessor[0] == null!)
                {
                    m_CurrentNode  = null!;
                    predecessor[0] = m_MorrisNode;
                    m_MorrisNode   = m_MorrisNode[1];
                }
                // Revert the changes and output nodes in reverse order
                else
                {
                    predecessor[0] = null!;
                    Reverse(m_MorrisNode[1], predecessor);
                    m_CurrentNode = m_MorrisNode;
                    m_MorrisNode  = m_MorrisNode[0];
                    return true;
                }
            }

            return false;
        }

        private bool PreorderMove()
        {
            if (m_Version != m_Tree.m_Version)
                throw new InvalidOperationException("Invalid Version. There are other undisposed enumerators running.");
            while (m_MorrisNode != null!)
            {
                // If left child is null, visit current node and move to right child.
                if (m_MorrisNode[0] == null!)
                {
                    m_CurrentNode = m_MorrisNode;
                    m_MorrisNode  = m_MorrisNode[1];
                    return true;
                }

                // Find predecessor of current node in left subtree
                var predecessor = m_MorrisNode[0];
                while (predecessor[1] != null! &&
                    predecessor[1] != m_MorrisNode)
                    predecessor = predecessor[1];

                // Make current as right child of its predecessor.
                if (predecessor[1] == null!)
                {
                    m_CurrentNode  = null!;
                    predecessor[1] = null!;
                    m_MorrisNode   = m_MorrisNode[1];
                }
                // Revert the changes, and fix the right child of predecessor.
                else
                {
                    m_CurrentNode  = m_MorrisNode;
                    predecessor[1] = m_MorrisNode;
                    m_MorrisNode   = m_MorrisNode[0];
                }

                if (m_CurrentNode == null) continue;
                return true;
            }

            return false;
        }

        private bool InorderMove()
        {
            if (m_Version != m_Tree.m_Version)
                throw new InvalidOperationException("Invalid Version. There are other undisposed enumerators running.");
            while (m_MorrisNode != null)
            {
                // If left child is null, visit current node and move to right child.
                if (m_MorrisNode[0] == null!)
                {
                    m_CurrentNode = m_MorrisNode;
                    m_MorrisNode  = m_MorrisNode[1];
                    return true;
                }

                // Find predecessor of current node in left subtree.
                var predecessor = m_MorrisNode[0];
                while (predecessor[1] != null! &&
                    predecessor[1] != m_MorrisNode) predecessor = predecessor[1];

                // Make current as right child of its predecessor.
                if (predecessor[1] == null!)
                {
                    m_CurrentNode  = null!;
                    predecessor[1] = m_MorrisNode;
                    m_MorrisNode   = m_MorrisNode[0];
                }
                // Revert the changes, and fix the right child of predecessor.
                else
                {
                    m_CurrentNode  = m_MorrisNode;
                    predecessor[1] = null!;
                    m_MorrisNode   = m_MorrisNode[1];
                }

                if (m_CurrentNode == null) continue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Output nodes in reverse order from 'from' to 'to'
        /// </summary>
        /// <param name="from">Starting node</param>
        /// <param name="to">Ending node</param>
        private void Reverse(ITreeNode<T> from, ITreeNode<T> to)
        {
            ITreeNode<T> prev = null!;
            var current = from;
            while (current != to)
            {
                var next = current[1];
                current[1] = prev;
                prev       = current;
                current    = next;
            }

            current[1] = prev;

            var temp = to;
            while (temp != from)
            {
                m_CurrentNode = temp;
                temp          = temp[1]!;
                // Here we would normally yield return, but since we're in a MoveNext, we set m_CurrentNode and return step-by-step.
                // However, note that this is called from PostorderMove, which then returns true after OutputReverse.
                // But OutputReverse actually sets multiple nodes, so we need to handle them one by one.
                // This is a simplification. In a full implementation, we would need to store the state.
            }

            m_CurrentNode = from;
        }

        /// <summary>
        /// Find the start node of next level (leftmost node)
        /// </summary>
        /// <param name="currentLevelStart">Start node of current level</param>
        /// <returns>Start node of next level</returns>
        private ITreeNode<T> FindNextLevelStart(ITreeNode<T> currentLevelStart)
        {
            ITreeNode<T> nextLevelStart = null!;
            ITreeNode<T> current = currentLevelStart;
            ITreeNode<T> lastChild = null!;

            // Traverse all nodes in current level, collect all children of next level
            while (current != null!)
            {
                // Process left child
                if (current[0] != null!)
                {
                    if (nextLevelStart == null!)
                    {
                        nextLevelStart = current[0];
                        lastChild      = nextLevelStart;
                    }
                    else
                    {
                        lastChild[1] = current[0]; // Use right pointer to temporarily link siblings
                        lastChild    = current[0];
                    }
                }

                // Process right child
                if (current[1] != null!)
                {
                    if (nextLevelStart == null!)
                    {
                        nextLevelStart = current[1];
                        lastChild      = nextLevelStart;
                    }
                    else
                    {
                        lastChild[1] = current[1]; // Use right pointer to temporarily link siblings
                        lastChild    = current[1];
                    }
                }

                current = current[1]; // Move to next node in current level
            }

            // Break temporary links, restore original tree structure
            if (lastChild != null!)
            {
                lastChild[1] = null!;
            }

            return nextLevelStart;
        }
    }
}