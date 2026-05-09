namespace Gubbins.Collection;

/// <summary>
/// Read-only graph abstraction.
/// </summary>
/// <typeparam name="TVertex">Vertex type.</typeparam>
public interface IReadOnlyGraph<TVertex> : IEnumerable<TVertex>
{
    /// <summary>Gets total vertex count.</summary>
    int VertexCount { get; }

    /// <summary>Gets total edge count.</summary>
    int EdgeCount { get; }

    /// <summary>Checks whether a vertex exists.</summary>
    bool ContainsVertex(TVertex vertex);

    /// <summary>Checks whether an edge exists.</summary>
    bool ContainsEdge(TVertex start, TVertex end);
}

/// <summary>
/// Mutable graph abstraction.
/// </summary>
public interface IGraph<TVertex> : IReadOnlyGraph<TVertex>, IClearable
{
    /// <summary>Adds a vertex to the graph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    void AddVertex(TVertex vertex);

    /// <summary>Adds an edge to the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    void AddEdge(TVertex start, TVertex end);

    /// <summary>Attempts to add a vertex to the graph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added, false if it already exists.</returns>
    bool TryAddVertex(TVertex vertex);

    /// <summary>Attempts to add an edge to the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <returns>True if the edge was added, false if it already exists.</returns>
    bool TryAddEdge(TVertex start, TVertex end);

    /// <summary>Removes a vertex from the graph.</summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed, false if it does not exist.</returns>
    bool RemoveVertex(TVertex vertex);

    /// <summary>Removes an edge from the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <returns>True if the edge was removed, false if it does not exist.</returns>
    bool RemoveEdge(TVertex start, TVertex end);

    /// <summary>Replaces an old vertex with a new vertex.</summary>
    /// <param name="oldVertex">The vertex to be replaced.</param>
    /// <param name="newVertex">The new vertex.</param>
    void ReplaceVertex(TVertex oldVertex, TVertex newVertex);
}

/// <summary>
/// Read-only weighted graph abstraction.
/// </summary>
public interface IReadOnlyWeightedGraph<TVertex, TWeight> : IReadOnlyGraph<TVertex>
{
    /// <summary>Attempts to get edge weight.</summary>
    bool TryGetWeight(TVertex start, TVertex end, out TWeight weight);

    /// <summary>Gets edge weight.</summary>
    TWeight GetWeight(TVertex start, TVertex end);
}

/// <summary>
/// Mutable weighted graph abstraction.
/// </summary>
public interface IWeightedGraph<TVertex, TWeight> : IReadOnlyWeightedGraph<TVertex, TWeight>, IClearable
{
    /// <summary>Adds a vertex to the graph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    void AddVertex(TVertex vertex);

    /// <summary>Adds an edge to the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <param name="weight">The weight of the edge.</param>
    void AddEdge(TVertex start, TVertex end, TWeight weight);

    /// <summary>Attempts to add a vertex to the graph.</summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added, false if it already exists.</returns>
    bool TryAddVertex(TVertex vertex);

    /// <summary>Attempts to add an edge to the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <param name="weight">The weight of the edge.</param>
    /// <returns>True if the edge was added, false if it already exists.</returns>
    bool TryAddEdge(TVertex start, TVertex end, TWeight weight);

    /// <summary>Removes a vertex from the graph.</summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed, false if it does not exist.</returns>
    bool RemoveVertex(TVertex vertex);

    /// <summary>Removes an edge from the graph.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <returns>True if the edge was removed, false if it does not exist.</returns>
    bool RemoveEdge(TVertex start, TVertex end);

    /// <summary>Sets the weight of an edge.</summary>
    /// <param name="start">The starting vertex of the edge.</param>
    /// <param name="end">The ending vertex of the edge.</param>
    /// <param name="weight">The new weight of the edge.</param>
    void SetWeight(TVertex start, TVertex end, TWeight weight);

    /// <summary>Replaces an old vertex with a new vertex.</summary>
    /// <param name="oldVertex">The vertex to be replaced.</param>
    /// <param name="newVertex">The new vertex.</param>
    void ReplaceVertex(TVertex oldVertex, TVertex newVertex);
}

/// <summary>Mutable directed graph abstraction.</summary>
public interface IDirectedGraph<TVertex> : IReadOnlyDirectedGraph<TVertex>, IGraph<TVertex>;

/// <summary>Mutable undirected graph abstraction.</summary>
public interface IUndirectedGraph<TVertex> : IReadOnlyUndirectedGraph<TVertex>, IGraph<TVertex>;

/// <summary>Read-only directed graph abstraction.</summary>
public interface IReadOnlyDirectedGraph<TVertex> : IReadOnlyGraph<TVertex>
{
    /// <summary>Gets inner vertexes adjacent to the specified vertex.</summary>
    TVertex[] GetAdjacencyInnerVertexesOf(TVertex vertex);

    /// <summary>Gets outer vertexes adjacent to the specified vertex.</summary>
    TVertex[] GetAdjacencyOuterVertexesOf(TVertex vertex);

    /// <summary>Gets the in-degree of the specified vertex.</summary>
    int GetInDegreeOf(TVertex vertex);

    /// <summary>Gets the out-degree of the specified vertex.</summary>
    int GetOutDegreeOf(TVertex vertex);
}

/// <summary>Read-only undirected graph abstraction.</summary>
public interface IReadOnlyUndirectedGraph<TVertex> : IReadOnlyGraph<TVertex>
{
    /// <summary>Gets vertexes adjacent to the specified vertex.</summary>
    TVertex[] GetAdjacencyVertexesOf(TVertex vertex);

    /// <summary>Gets the degree of the specified vertex.</summary>
    int GetDegreeOf(TVertex vertex);
}

/// <summary>Mutable directed weighted graph abstraction.</summary>
public interface IDirectedWeightedGraph<TVertex, TWeight> : IReadOnlyDirectedWeightedGraph<TVertex, TWeight>, IWeightedGraph<TVertex, TWeight>;

/// <summary>Mutable undirected weighted graph abstraction.</summary>
public interface IUndirectedWeightedGraph<TVertex, TWeight> : IReadOnlyUndirectedWeightedGraph<TVertex, TWeight>, IWeightedGraph<TVertex, TWeight>;

/// <summary>Read-only directed weighted graph abstraction.</summary>
public interface IReadOnlyDirectedWeightedGraph<TVertex, TWeight> : IReadOnlyDirectedGraph<TVertex>, IReadOnlyWeightedGraph<TVertex, TWeight>
{
    /// <summary>Gets inner edges (vertex-weight pairs) adjacent to the specified vertex.</summary>
    (TVertex Vertex, TWeight Weight)[] GetAdjacencyInnerEdgesOf(TVertex vertex);

    /// <summary>Gets outer edges (vertex-weight pairs) adjacent to the specified vertex.</summary>
    (TVertex Vertex, TWeight Weight)[] GetAdjacencyOuterEdgesOf(TVertex vertex);
}

/// <summary>Read-only undirected weighted graph abstraction.</summary>
public interface IReadOnlyUndirectedWeightedGraph<TVertex, TWeight> : IReadOnlyUndirectedGraph<TVertex>, IReadOnlyWeightedGraph<TVertex, TWeight>
{
    /// <summary>Gets edges (adjacency vertex-weight pairs) adjacent to the specified vertex.</summary>
    (TVertex AdjacencyVertex, TWeight Weight)[] GetAdjacencyEdgesOf(TVertex vertex);
}