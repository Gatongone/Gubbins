namespace Gubbins.Collections;

public interface ITree<out TNode>
{
    int Count { get; }
    TreeTraversalType DefaultTraversalType { get; }
    IEnumerable<TNode> GetTraverser(TreeTraversalType traversalType);
}

public interface ITreeNode<TNode>
{
    int Count { get; }
    TNode Value { get; }
    ITreeNode<TNode> this[int childIndex] { get; set; }
}