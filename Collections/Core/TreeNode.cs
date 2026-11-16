using System.Runtime.CompilerServices;

namespace Gubbins.Collections;

public class TreeNode<T> : ITreeNode<T>
{
    private readonly List<TreeNode<T>> m_Children;

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Children.Count;
    }

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
    }

    ITreeNode<T> ITreeNode<T>.this[int childIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[childIndex];
        set => this[childIndex] = (TreeNode<T>) value;
    }

    public TreeNode<T> this[int childIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Children[childIndex];
        set => m_Children[childIndex] = value;
    }

    public TreeNode(T value, int init = 2) => (Value, m_Children) = (value, new List<TreeNode<T>>(init));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddChild(T value) => m_Children.Add(new TreeNode<T>(value));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddChild(TreeNode<T> treeNode) => m_Children.Add(treeNode);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveChild(TreeNode<T> treeNode) => m_Children.Remove(treeNode);

    public bool RemoveChild(T value)
    {
        var comparer = Comparer<T>.Default;
        for (var index = m_Children.Count - 1; index >= 0; index--)
        {
            if (comparer.Compare(m_Children[index].Value, value) != 0) continue;
            m_Children.RemoveAt(index);
            return true;
        }

        return false;
    }
}

public class BinaryTreeNode<T>(T value) : ITreeNode<T>
{
    public BinaryTreeNode<T>? Left;
    public BinaryTreeNode<T>? Right;

    public T Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set;
    } = value;

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Left == null) return Right == null ? 0 : 1;
            if (Right == null) return Left == null ? 0 : 1;
            return 2;
        }
    }

    public BinaryTreeNode<T> this[int childIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => childIndex switch
        {
            0 => Left!,
            1 => Right!,
            _ => throw new ArgumentOutOfRangeException(nameof(childIndex))
        };
        set
        {
            switch (childIndex)
            {
                case 0:
                    Left = value;
                    return;
                case 1:
                    Right = value;
                    return;
                default: throw new ArgumentOutOfRangeException(nameof(childIndex));
            }
        }
    }

    ITreeNode<T> ITreeNode<T>.this[int childIndex]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[childIndex];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this[childIndex] = (BinaryTreeNode<T>) value;
    }
}