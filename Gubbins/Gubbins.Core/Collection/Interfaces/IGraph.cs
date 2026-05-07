namespace Gubbins.Collection;

public interface IReadOnlyGraph<TVertex> : IEnumerable<TVertex>
{
    int VertexCount { get; }
    int EdgeCount { get; }
    bool ContainsVertex(TVertex vertex);
    bool ContainsEdge(TVertex start, TVertex end);
}

public interface IGraph<TVertex> : IReadOnlyGraph<TVertex>
{
    void AddVertex(TVertex vertex);
    void AddEdge(TVertex start, TVertex end);
    bool TryAddVertex(TVertex vertex);
    bool TryAddEdge(TVertex start, TVertex end);
    bool RemoveVertex(TVertex vertex);
    bool RemoveEdge(TVertex start, TVertex end);
    void ReplaceVertex(TVertex oldVertex, TVertex newVertex);
    void Clear();
}

public interface IReadOnlyWeightedGraph<TVertex, TWeight> : IReadOnlyGraph<TVertex>
{
    bool TryGetWeight(TVertex start, TVertex end, out TWeight weight);
    TWeight GetWeight(TVertex start, TVertex end);
}

public interface IWeightedGraph<TVertex, TWeight> : IReadOnlyWeightedGraph<TVertex, TWeight>
{
    void AddVertex(TVertex vertex);
    void AddEdge(TVertex start, TVertex end, TWeight weight);
    bool TryAddVertex(TVertex vertex);
    bool TryAddEdge(TVertex start, TVertex end, TWeight weight);
    bool RemoveVertex(TVertex vertex);
    bool RemoveEdge(TVertex start, TVertex end);
    void SetWeight(TVertex start, TVertex end, TWeight weight);
    void ReplaceVertex(TVertex oldVertex, TVertex newVertex);
    void Clear();
}

public interface IDirectedGraph<TVertex> : IReadOnlyGraph<TVertex>
{
    TVertex[] GetAdjacencyInnerVertexesOf(TVertex vertex);
    TVertex[] GetAdjacencyOuterVertexesOf(TVertex vertex);
    int GetInDegreeOf(TVertex vertex);
    int GetOutDegreeOf(TVertex vertex);
}

public interface IUndirectedGraph<TVertex> : IReadOnlyGraph<TVertex>
{
    TVertex[] GetAdjacencyVertexesOf(TVertex vertex);
    int GetDegreeOf(TVertex vertex);
}

public interface IDirectedWeightedGraph<TVertex, TWeight> : IDirectedGraph<TVertex>, IReadOnlyWeightedGraph<TVertex, TWeight>
{
    (TVertex Vertex, TWeight Weight)[] GetAdjacencyInnerEdgesOf(TVertex vertex);
    (TVertex Vertex, TWeight Weight)[] GetAdjacencyOuterEdgesOf(TVertex vertex);
}

public interface IUndirectedWeightedGraph<TVertex, TWeight> : IUndirectedGraph<TVertex>, IReadOnlyWeightedGraph<TVertex, TWeight>
{
    (TVertex AdjacencyVertex, TWeight Weight)[] GetAdjacencyEdgesOf(TVertex vertex);
}