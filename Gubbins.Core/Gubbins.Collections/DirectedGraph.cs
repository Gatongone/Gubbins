using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Collections;

/// <summary>
/// Sparse directed weighted graph which implement by orthogonal list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
/// <typeparam name="TWeight">Graph edge weight.</typeparam>
public class DirectedGraph<TVertex, TWeight> : IDirectedWeightedGraph<TVertex, TWeight>
{
    /// <summary>
    /// Vertex map which maps vertex data to vertex info. The vertex info contains the actual vertex data and the first edge of inner and outer link list.
    /// </summary>
    private readonly Dictionary<TVertex, VertexInfo> m_VertexMap = new();

    /// <inheritdoc/>
    public int VertexCount => m_VertexMap.Count;

    /// <inheritdoc/>
    public int EdgeCount { get; private set; }

    public DirectedGraph() { }

    /// <summary>
    /// Initializes a new instance of the DirectedGraph class based on the provided EdgesetArray.
    /// </summary>
    /// <param name="graph">EdgesetArray containing the graph data to initialize from. The graph must be directed.</param>
    /// <exception cref="ArgumentNullException">Thrown when the provided graph is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the provided graph is not directed.</exception>
    public DirectedGraph(EdgesetArray<TVertex, TWeight> graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (!graph.IsDirected) throw new ArgumentException("Graph must be directed.");

        // Add edges.
        foreach (var edge in graph.Edges)
        {
            AddEdge(edge.Start, edge.End, edge.Weight);
        }

        // Add free vertexes.
        foreach (var vertex in graph.Vertexes)
        {
            if (!m_VertexMap.ContainsKey(vertex))
                m_VertexMap.Add(vertex, new VertexInfo(vertex));
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex)
    {
        if (m_VertexMap.ContainsKey(vertex))
            throw new ArgumentException("Vertex already exists.");
        var vertexInfo = new VertexInfo(vertex);
        m_VertexMap.Add(vertex, vertexInfo);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end, TWeight weight)
    {
        // Get outer vertex.
        if (!m_VertexMap.TryGetValue(start, out var startInfo))
        {
            startInfo = new VertexInfo(start);
            m_VertexMap.Add(start, startInfo);
        }

        // Get inner vertex.
        if (!m_VertexMap.TryGetValue(end, out var endInfo))
        {
            endInfo = new VertexInfo(end);
            m_VertexMap.Add(end, endInfo);
        }

        // Assert is the edge already exist.
        if (ContainsEdge(startInfo, endInfo))
            throw new ArgumentException("Edge already exists.");

        AddEdge(startInfo, endInfo, weight);
    }

    /// <summary>
    /// Adds an edge to the graph without checking whether the edge already exists. This method is intended for internal use when the caller can guarantee that the edge does not exist, such as when building the graph from a known data source. Using this method with existing edges will result in undefined behavior.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddEdgeWithoutExistentCheck(TVertex start, TVertex end, TWeight weight)
    {
        // Get outer vertex.
        if (!m_VertexMap.TryGetValue(start, out var startInfo))
        {
            startInfo = new VertexInfo(start);
            m_VertexMap.Add(start, startInfo);
        }

        // Get inner vertex.
        if (!m_VertexMap.TryGetValue(end, out var endInfo))
        {
            endInfo = new VertexInfo(end);
            m_VertexMap.Add(end, endInfo);
        }

        AddEdge(startInfo, endInfo, weight);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex)
    {
        if (m_VertexMap.ContainsKey(vertex))
            return false;
        var vertexInfo = new VertexInfo(vertex);
        m_VertexMap.Add(vertex, vertexInfo);
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end, TWeight weight)
    {
        // Vertex doesn't exist
        if (!m_VertexMap.TryGetValue(start, out var startInfo) || !m_VertexMap.TryGetValue(end, out var endInfo))
            return false;

        AddEdge(startInfo, endInfo, weight);
        return true;
    }

    /// <inheritdoc/>
    public bool RemoveVertex(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo))
            return false;

        // Removes all edges ending with target vertex.
        foreach (var startInfo in m_VertexMap.Values)
        {
            RemoveEdge(startInfo, vertexInfo, false);
        }

        // Recount edge of target vertex's outer edges.
        var outerCount = 0;
        var outerEdge = vertexInfo.FirstOut;
        while (outerEdge != null)
        {
            outerEdge = outerEdge.OuterEdge;
            outerCount++;
        }

        EdgeCount -= outerCount;

        // Removes vertex from cache.
        return m_VertexMap.Remove(vertex);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) =>
        m_VertexMap.TryGetValue(start, out var startInfo) &&
        m_VertexMap.TryGetValue(end, out var endInfo) &&
        RemoveEdge(startInfo, endInfo, true);

    /// <summary>
    /// Removes an edge from the graph. If <paramref name="removeFromInnerLink"/> is true,
    /// the edge will be removed from both inner and outer link lists, which is necessary when removing an edge normally.
    /// If <paramref name="removeFromInnerLink"/> is false, the edge will only be removed from the outer link list,
    /// which is used when removing all edges ending with a vertex to avoid redundant traversal of inner link list.
    /// </summary>
    private bool RemoveEdge(VertexInfo startInfo, VertexInfo endInfo, bool removeFromInnerLink)
    {
        var curInnerEdge = endInfo.FirstIn;
        var curOuterEdge = startInfo.FirstOut;
        var preInnerEdge = curInnerEdge;
        var preOuterEdge = curOuterEdge;

        // Both are free vertex.
        if (curInnerEdge == null || curOuterEdge == null)
            return false;

        //Finds edge from outer link list.
        while (curOuterEdge.InnerVertex != endInfo &&
            curOuterEdge.OuterEdge != null)
        {
            preOuterEdge = curOuterEdge;
            curOuterEdge = curOuterEdge.OuterEdge;
        }

        // Edge doesn't exists.
        if (curOuterEdge.InnerVertex != endInfo || preOuterEdge == null)
            return false;

        // Removes outer edge.
        if (curOuterEdge == startInfo.FirstOut)
            startInfo.FirstOut = curOuterEdge.OuterEdge;
        else
            preOuterEdge.OuterEdge = curOuterEdge.OuterEdge;

        // Only removes edge from inner link.
        if (!removeFromInnerLink)
        {
            EdgeCount--;
            return true;
        }

        //Finds edge from inner link list.
        while (curInnerEdge.OuterVertex != startInfo &&
            curInnerEdge.InnerEdge != null)
        {
            preInnerEdge = curInnerEdge;
            curInnerEdge = curInnerEdge.InnerEdge;
        }

        // Sanity.
        Debug.Assert(curInnerEdge.OuterVertex == startInfo && preInnerEdge != null);

        // Removes inner edge.
        if (curInnerEdge == startInfo.FirstOut)
            startInfo.FirstIn = curInnerEdge.InnerEdge;
        else
            preInnerEdge.InnerEdge = curInnerEdge.InnerEdge;

        EdgeCount--;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_VertexMap.Clear();
        EdgeCount = 0;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex)
    {
        if (!m_VertexMap.TryGetValue(oldVertex, out var vertexInfo))
            throw new ArgumentException("Vertex does not exist.");
        // Reset data.
        vertexInfo.Data = newVertex;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_VertexMap.ContainsKey(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => GetEdge(start, end) != null;

    /// <inheritdoc/>
    public TVertex[] GetAdjacencyInnerVertexesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return [];

        var list = new List<TVertex>();
        var edge = vertexInfo.FirstIn;

        // Foreach all inner edges.
        while (edge != null)
        {
            list.Add(edge.OuterVertex.Data);
            edge = edge.InnerEdge;
        }

        return list.ToArray();
    }

    /// <inheritdoc/>
    public TVertex[] GetAdjacencyOuterVertexesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return [];

        var list = new List<TVertex>();
        var edge = vertexInfo.FirstOut;

        // Foreach all outer edges.
        while (edge != null)
        {
            list.Add(edge.InnerVertex.Data);
            edge = edge.OuterEdge;
        }

        return list.ToArray();
    }

    /// <inheritdoc/>
    public (TVertex Vertex, TWeight Weight)[] GetAdjacencyInnerEdgesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return [];

        var list = new List<(TVertex, TWeight)>();
        var edge = vertexInfo.FirstIn;

        // Foreach all inner edges.
        while (edge != null)
        {
            list.Add((edge.OuterVertex.Data, edge.Weight));
            edge = edge.InnerEdge;
        }

        return list.ToArray();
    }

    /// <inheritdoc/>
    public (TVertex Vertex, TWeight Weight)[] GetAdjacencyOuterEdgesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return [];

        var list = new List<(TVertex, TWeight)>();
        var edge = vertexInfo.FirstOut;

        // Foreach all outer edges.
        while (edge != null)
        {
            list.Add((edge.InnerVertex.Data, edge.Weight));
            edge = edge.OuterEdge;
        }

        return list.ToArray();
    }

    /// <inheritdoc/>
    public int GetInDegreeOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return 0;

        var count = 0;
        var edge = vertexInfo.FirstIn;

        // Foreach all inner edges.
        while (edge != null)
        {
            count++;
            edge = edge.InnerEdge;
        }

        return count;
    }

    /// <inheritdoc/>
    public int GetOutDegreeOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return 0;

        var count = 0;
        var edge = vertexInfo.FirstOut;

        // Foreach all outer edges.
        while (edge != null)
        {
            count++;
            edge = edge.OuterEdge;
        }

        return count;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWeight(TVertex start, TVertex end, TWeight weight)
    {
        var edge = GetEdge(start, end);
        if (edge == null)
            throw new ArgumentException("Edge does not exist.");
        edge.Weight = weight;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetWeight(TVertex start, TVertex end, out TWeight weight)
    {
        var edge = GetEdge(start, end);

        if (edge == null)
        {
            weight = default!;
            return false;
        }

        weight = edge.Weight;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TWeight GetWeight(TVertex start, TVertex end)
    {
        var edge = GetEdge(start, end);
        if (edge == null)
            throw new ArgumentException("Edge does not exist.");
        return edge.Weight;
    }

    /// <summary>
    /// Checks whether the edge from start vertex to end vertex exists.
    /// This method is intended for internal use when the caller can guarantee that the vertexes exist,
    /// such as when building the graph from a known data source. Using this method with non-existent vertexes will result in undefined behavior.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsEdge(VertexInfo startInfo, VertexInfo endInfo) => GetEdge(startInfo, endInfo) != null;

    /// <summary>
    /// Gets edge info from start vertex to end vertex. Returns null if the edge does not exist.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EdgeInfo? GetEdge(TVertex start, TVertex end)
    {
        // Check vertexes existent.
        if (!m_VertexMap.TryGetValue(start, out var startInfo) || !m_VertexMap.TryGetValue(end, out var endInfo))
            return null;
        return GetEdge(startInfo, endInfo);
    }

    /// <summary>
    /// Gets edge info from start vertex to end vertex. Returns null if the edge does not exist.
    /// </summary>
    private EdgeInfo? GetEdge(VertexInfo startInfo, VertexInfo endInfo)
    {
        // Find edge.
        var edge = startInfo.FirstOut;
        while (edge != null &&
            edge.InnerVertex != endInfo)
        {
            edge = edge.OuterEdge;
        }

        return edge;
    }

    /// <summary>
    /// Adds an edge from start vertex to end vertex with given weight.
    /// This method is intended for internal use when the caller can guarantee that the edge does not exist,
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEdge(VertexInfo startInfo, VertexInfo endInfo, TWeight weight)
    {
        // Create new edge.
        var edge = new EdgeInfo(startInfo, endInfo, weight)
        {
            OuterEdge = startInfo.FirstOut,
            InnerEdge = endInfo.FirstIn
        };

        startInfo.FirstOut = edge;
        endInfo.FirstIn    = edge;
        EdgeCount++;
    }

    /// <summary>
    /// Gets enumerator of vertexes in the graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(m_VertexMap.Keys.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Vertex info which contains vertex data and the first edge of inner and outer link list.
    /// The inner link list is a linked list of edges that end with the vertex,
    /// while the outer link list is a linked list of edges that start with the vertex.
    /// Each edge in the inner link list has a pointer to the corresponding edge in the outer link list,
    /// and vice versa, which allows for efficient traversal and modification of the graph structure.
    /// </summary>
    internal class VertexInfo
    {
        /// <summary>
        /// Vertex data. This is the actual vertex value that the graph represents. It is of type TVertex,
        /// which is a generic type parameter that allows the graph to store any type of vertex data.
        /// The vertex data is used for various graph operations, such as adding edges, checking for vertex existence, and retrieving adjacent vertices.
        /// </summary>
        public TVertex Data;

        /// <summary>
        /// The first edge of the outer link list, which is a linked list of edges that start with this vertex.
        /// </summary>
        public EdgeInfo? FirstOut;

        /// <summary>
        /// The first edge of the inner link list, which is a linked list of edges that end with this vertex.
        /// </summary>
        public EdgeInfo? FirstIn;

        public VertexInfo(TVertex data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Edge info which contains edge weight and pointers to the corresponding edges in the inner and outer link lists of the start and end vertexes.
    /// </summary>
    internal class EdgeInfo
    {
        /// <summary>
        /// The vertex at the end of the edge, which is the target vertex of the edge.
        /// </summary>
        public readonly VertexInfo InnerVertex;

        /// <summary>
        /// The vertex at the start of the edge, which is the source vertex of the edge.
        /// </summary>
        public readonly VertexInfo OuterVertex;

        /// <summary>
        /// The first edge in the outer link list of the source vertex that starts with the same source vertex.
        /// </summary>
        public EdgeInfo? OuterEdge;

        /// <summary>
        /// The first edge in the inner link list of the target vertex that ends with the same target vertex.
        /// </summary>
        public EdgeInfo? InnerEdge;

        /// <summary>
        /// The weight of the edge, which is of type TWeight.
        /// </summary>
        public TWeight Weight;

        public EdgeInfo(VertexInfo innerVertex, VertexInfo outerVertex, TWeight weight)
        {
            InnerVertex = innerVertex;
            OuterVertex = outerVertex;
            Weight      = weight;
        }
    }

    /// <summary>
    /// Enumerator of vertexes in the graph. It is a wrapper around the enumerator of the key collection of the vertex map,
    /// </summary>
    public struct Enumerator : IEnumerator<TVertex>
    {
        internal Enumerator(Dictionary<TVertex, VertexInfo>.KeyCollection.Enumerator enumerator) => m_Enumerator = enumerator;

        private Dictionary<TVertex, VertexInfo>.KeyCollection.Enumerator m_Enumerator;

        public TVertex Current => m_Enumerator.Current;

        object IEnumerator.Current => Current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => m_Enumerator.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => ((IEnumerator) m_Enumerator).Reset();

        public void Dispose() => m_Enumerator.Dispose();
    }
}

/// <summary>
/// Sparse directed graph which implement by orthogonal list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
public class DirectedGraph<TVertex> : IDirectedGraph<TVertex> where TVertex : notnull
{
    /// <summary>
    /// Proxy for weighted graph with None weighted. 
    /// </summary>
    private readonly DirectedGraph<TVertex, Unit> m_Graph = new();

    /// <inheritdoc/>
    public int VertexCount => m_Graph.VertexCount;

    /// <inheritdoc/>
    public int EdgeCount => m_Graph.EdgeCount;

    public DirectedGraph() { }

    public DirectedGraph(EdgesetArray<TVertex> graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (!graph.IsDirected) throw new ArgumentException("Graph must be directed.");

        // Add edges.
        foreach (var edge in graph.Edges)
        {
            AddEdge(edge.Start, edge.End);
        }

        // Add free vertexes.
        foreach (var vertex in graph.Vertexes)
        {
            if (!ContainsVertex(vertex))
                AddVertex(vertex);
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex) => m_Graph.AddVertex(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end) => m_Graph.AddEdge(start, end, Unit.Instance);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex) => m_Graph.TryAddVertex(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end) => m_Graph.TryAddEdge(start, end, Unit.Instance);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveVertex(TVertex vertex) => m_Graph.RemoveVertex(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) => m_Graph.RemoveEdge(start, end);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_Graph.Clear();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex) => m_Graph.ReplaceVertex(oldVertex, newVertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_Graph.ContainsVertex(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => m_Graph.ContainsEdge(start, end);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVertex[] GetAdjacencyInnerVertexesOf(TVertex vertex) => m_Graph.GetAdjacencyInnerVertexesOf(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVertex[] GetAdjacencyOuterVertexesOf(TVertex vertex) => m_Graph.GetAdjacencyOuterVertexesOf(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInDegreeOf(TVertex vertex) => m_Graph.GetInDegreeOf(vertex);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOutDegreeOf(TVertex vertex) => m_Graph.GetOutDegreeOf(vertex);

    /// <summary>
    /// Gets enumerator of vertexes in the graph.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(m_Graph.GetEnumerator());

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<TVertex>
    {
        private DirectedGraph<TVertex, Unit>.Enumerator m_Enumerator;

        internal Enumerator(DirectedGraph<TVertex, Unit>.Enumerator enumerator) => m_Enumerator = enumerator;

        public TVertex Current => m_Enumerator.Current;

        object IEnumerator.Current => Current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => m_Enumerator.MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => ((IEnumerator) m_Enumerator).Reset();

        public void Dispose() => m_Enumerator.Dispose();
    }
}