namespace Gubbins.Collections;

public interface IEdge<out TNode>
{
    TNode StartNode { get; }
    TNode EndNode { get; }
}

public interface IWeightedEdge<out TNode> : IEdge<TNode>
{
    int Weight { get; }
}
