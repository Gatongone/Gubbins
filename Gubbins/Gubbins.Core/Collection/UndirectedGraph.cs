using System.Collections;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Collection;

/// <summary>
/// Sparse undirected weighted graph which implement by adjacency multi-list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
/// <typeparam name="TWeight">Graph edge weight.</typeparam>
public class UndirectedGraph<TVertex, TWeight> : IWeightedGraph<TVertex, TWeight>, IUndirectedWeightedGraph<TVertex, TWeight>
{
    private readonly Dictionary<TVertex, VertexInfo> m_VertexMap = new();

    public int VertexCount => m_VertexMap.Count;
    public int EdgeCount { get; private set; }

    public UndirectedGraph() { }

    public UndirectedGraph(EdgesetArray<TVertex, TWeight> graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (graph.IsDirected) throw new ArgumentException("Graph must be undirected.");

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex)
    {
        if (m_VertexMap.ContainsKey(vertex))
            throw new ArgumentException("Vertex already exists.");
        var vertexInfo = new VertexInfo(vertex);
        m_VertexMap.Add(vertex, vertexInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end, TWeight weight)
    {
        // Get start vertex.
        if (!m_VertexMap.TryGetValue(start, out var startInfo))
        {
            startInfo = new VertexInfo(start);
            m_VertexMap.Add(start, startInfo);
        }

        // Get end vertex.
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex)
    {
        if (m_VertexMap.ContainsKey(vertex))
            return false;
        var vertexInfo = new VertexInfo(vertex);
        m_VertexMap.Add(vertex, vertexInfo);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end, TWeight weight)
    {
        // Vertex doesn't exist
        if (!m_VertexMap.TryGetValue(start, out var startInfo) || !m_VertexMap.TryGetValue(end, out var endInfo))
            return false;

        AddEdge(startInfo, endInfo, weight);
        return true;
    }

    public bool RemoveVertex(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo))
            return false;

        // Removes all edges adjacency with target vertex.
        foreach (var adjacentVertex in m_VertexMap.Values)
        {
            RemoveEdge(vertexInfo, adjacentVertex);
        }

        // Removes vertex from cache.
        return m_VertexMap.Remove(vertex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) =>
        m_VertexMap.TryGetValue(start, out var startInfo) &&
        m_VertexMap.TryGetValue(end, out var endInfo) &&
        RemoveEdge(startInfo, endInfo);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveEdge(VertexInfo startInfo, VertexInfo endInfo)
    {
        // Remove edge from start link and end link.
        if (!RemoveEdgeFromLink(startInfo, endInfo) || !RemoveEdgeFromLink(endInfo, startInfo)) return false;
        EdgeCount--;
        return true;
    }

    private bool RemoveEdgeFromLink(VertexInfo startInfo, VertexInfo endInfo)
    {
        var preEdge = default(EdgeInfo);
        var curEdge = startInfo.FirstEdge;

        // Find the target edge from start vertex.
        while (curEdge != null &&
               curEdge.StartVertex != endInfo &&
               curEdge.EndVertex != endInfo)
        {
            preEdge = curEdge;
            curEdge = GetNextEdge(curEdge, startInfo);
        }

        // Edge doesn't exist.
        if (curEdge == null)
            return false;

        var isCurStartEdge = curEdge.StartVertex == startInfo;

        // If the edge is the first edge of start vertex.
        if (curEdge == startInfo.FirstEdge)
        {
            startInfo.FirstEdge = isCurStartEdge ? curEdge.StartEdge : curEdge.EndEdge;
        }
        // If the edge is not the first edge of start vertex.
        else if (preEdge != null)
        {
            // Check which edge should be removed.
            var isPreStartEdge = preEdge.StartVertex == startInfo;
            switch (isPreStartEdge)
            {
                case true when isCurStartEdge:
                    preEdge.StartEdge = curEdge.StartEdge;
                    break;
                case true when !isCurStartEdge:
                    preEdge.StartEdge = curEdge.EndEdge;
                    break;
                case false when isCurStartEdge:
                    preEdge.EndEdge = curEdge.StartEdge;
                    break;
                default:
                    preEdge.EndEdge = curEdge.EndEdge;
                    break;
            }
        }
        // You must be kidding me.
        else return false;

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_VertexMap.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex)
    {
        if (!m_VertexMap.TryGetValue(oldVertex, out var vertexInfo))
            throw new ArgumentException("Vertex does not exist.");
        // Reset data.
        vertexInfo.Data = newVertex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_VertexMap.ContainsKey(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => GetEdge(start, end) != null;

    public int GetDegreeOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return 0;

        var count = 0;
        var edge = vertexInfo.FirstEdge;

        // Foreach all adjacency edges.
        while (edge != null)
        {
            count++;
            edge = GetNextEdge(edge, vertexInfo);
        }

        return count;
    }

    public TVertex[] GetAdjacencyVertexesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return Array.Empty<TVertex>();

        var edge = vertexInfo.FirstEdge;
        var list = new List<TVertex>();

        // Foreach all adjacency edges.
        while (edge != null)
        {
            list.Add(edge.StartVertex == vertexInfo ? edge.EndVertex.Data : edge.StartVertex.Data);
            edge = GetNextEdge(edge, vertexInfo);
        }

        return list.ToArray();
    }

    public (TVertex AdjacencyVertex, TWeight Weight)[] GetAdjacencyEdgesOf(TVertex vertex)
    {
        if (!m_VertexMap.TryGetValue(vertex, out var vertexInfo)) return Array.Empty<(TVertex, TWeight)>();

        var edge = vertexInfo.FirstEdge;
        var list = new List<(TVertex, TWeight)>();

        // Foreach all adjacency edges.
        while (edge != null)
        {
            list.Add((edge.StartVertex == vertexInfo ? edge.EndVertex.Data : edge.StartVertex.Data, edge.Weight));
            edge = GetNextEdge(edge, vertexInfo);
        }

        return list.ToArray();
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TWeight GetWeight(TVertex start, TVertex end)
    {
        var edge = GetEdge(start, end);
        if (edge == null)
            throw new ArgumentException("Edge does not exist.");
        return edge.Weight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWeight(TVertex start, TVertex end, TWeight weight)
    {
        var edge = GetEdge(start, end);
        if (edge == null)
            throw new ArgumentException("Edge does not exist.");
        edge.Weight = weight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsEdge(VertexInfo startInfo, VertexInfo endInfo) => GetEdge(startInfo, endInfo) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EdgeInfo? GetEdge(TVertex start, TVertex end)
    {
        if (!m_VertexMap.TryGetValue(start, out var startInfo) || !m_VertexMap.TryGetValue(end, out var endInfo))
            return null;
        return GetEdge(startInfo, endInfo);
    }

    private EdgeInfo? GetEdge(VertexInfo startInfo, VertexInfo endInfo)
    {
        var edge = startInfo.FirstEdge;
        // Find the target edge from start vertex.
        while (edge != null &&
               edge.StartVertex != endInfo &&
               edge.EndVertex != endInfo)
        {
            edge = GetNextEdge(edge, startInfo);
        }

        return edge;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEdge(VertexInfo startInfo, VertexInfo endInfo, TWeight weight)
    {
        // Create new edge.
        var edge = new EdgeInfo(startInfo, endInfo, weight)
        {
            StartEdge = startInfo.FirstEdge,
            EndEdge = endInfo.FirstEdge
        };

        // Head insert edge.
        startInfo.FirstEdge = edge;
        endInfo.FirstEdge = edge;
        EdgeCount++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EdgeInfo? GetNextEdge(EdgeInfo edge, VertexInfo vertexInfo) => edge.StartVertex == vertexInfo ? edge.StartEdge : edge.EndEdge;

    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => m_VertexMap.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => m_VertexMap.Keys.GetEnumerator();

    private class VertexInfo(TVertex data)
    {
        /// <summary>
        /// Vertex data.
        /// </summary>
        public TVertex Data = data;

        /// <summary>
        /// The first edge which start from current vertex.
        /// </summary>
        public EdgeInfo? FirstEdge;
    }

    private class EdgeInfo
    {
        /// <summary>
        /// The vertex of current edge's beginning.
        /// </summary>
        public readonly VertexInfo StartVertex;

        /// <summary>
        /// The vertex of current edge's ending.
        /// </summary>
        public readonly VertexInfo EndVertex;

        /// <summary>
        /// The cur edge which start from start vertex.
        /// </summary>
        public EdgeInfo? StartEdge;

        /// <summary>
        /// The cur edge which end from end vertex.
        /// </summary>
        public EdgeInfo? EndEdge;

        /// <summary>
        /// Edge weight.
        /// </summary>
        public TWeight Weight;

        public EdgeInfo(VertexInfo startVertex, VertexInfo endVertex, TWeight weight)
        {
            StartVertex = startVertex;
            EndVertex = endVertex;
            Weight = weight;
        }
    }
}

/// <summary>
/// Sparse undirected graph which implement by adjacency multi-list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
public class UndirectedGraph<TVertex> : IGraph<TVertex>, IUndirectedGraph<TVertex> where TVertex : notnull
{
    // Proxy for weighted graph with None weighted.
    private readonly UndirectedGraph<TVertex, Unit> m_Graph = new();

    public int VertexCount => m_Graph.VertexCount;

    public int EdgeCount => m_Graph.EdgeCount;

    public UndirectedGraph() { }

    public UndirectedGraph(EdgesetArray<TVertex> graph)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));
        if (graph.IsDirected) throw new ArgumentException("Graph must be undirected.");

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex) => m_Graph.AddVertex(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end) => m_Graph.AddEdge(start, end, Unit.Instance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex) => m_Graph.TryAddVertex(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end) => m_Graph.TryAddEdge(start, end, Unit.Instance);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveVertex(TVertex vertex) => m_Graph.RemoveVertex(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) => m_Graph.RemoveEdge(start, end);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_Graph.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex) => m_Graph.ReplaceVertex(oldVertex, newVertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_Graph.ContainsVertex(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => m_Graph.ContainsEdge(start, end);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVertex[] GetAdjacencyVertexesOf(TVertex vertex) => m_Graph.GetAdjacencyVertexesOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetDegreeOf(TVertex vertex) => m_Graph.GetDegreeOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => ((IEnumerable<TVertex>) m_Graph).GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) m_Graph).GetEnumerator();
}