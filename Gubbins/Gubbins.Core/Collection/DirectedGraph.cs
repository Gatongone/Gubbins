using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;

namespace Gubbins.Collection;

/// <summary>
/// Sparse directed weighted graph which implement by orthogonal list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
/// <typeparam name="TWeight">Graph edge weight.</typeparam>
public class DirectedGraph<TVertex, TWeight> : IWeightedGraph<TVertex, TWeight>, IDirectedWeightedGraph<TVertex, TWeight>
{
    private readonly Dictionary<TVertex, VertexInfo> m_VertexMap = new();

    public int VertexCount => m_VertexMap.Count;

    public int EdgeCount { get; private set; }

    public DirectedGraph() { }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) =>
        m_VertexMap.TryGetValue(start, out var startInfo) &&
        m_VertexMap.TryGetValue(end, out var endInfo) &&
        RemoveEdge(startInfo, endInfo, true);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWeight(TVertex start, TVertex end, TWeight weight)
    {
        var edge = GetEdge(start, end);
        if (edge == null)
            throw new ArgumentException("Edge does not exist.");
        edge.Weight = weight;
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
    private bool ContainsEdge(VertexInfo startInfo, VertexInfo endInfo) => GetEdge(startInfo, endInfo) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private EdgeInfo? GetEdge(TVertex start, TVertex end)
    {
        // Check vertexes existent.
        if (!m_VertexMap.TryGetValue(start, out var startInfo) || !m_VertexMap.TryGetValue(end, out var endInfo))
            return null;
        return GetEdge(startInfo, endInfo);
    }

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
        endInfo.FirstIn = edge;
        EdgeCount++;
    }

    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => m_VertexMap.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => m_VertexMap.Keys.GetEnumerator();

    private class VertexInfo
    {
        public TVertex Data;
        public EdgeInfo? FirstOut;
        public EdgeInfo? FirstIn;

        public VertexInfo(TVertex data)
        {
            Data = data;
        }
    }

    private class EdgeInfo
    {
        public readonly VertexInfo InnerVertex;
        public readonly VertexInfo OuterVertex;
        public EdgeInfo? OuterEdge;
        public EdgeInfo? InnerEdge;
        public TWeight Weight;

        public EdgeInfo(VertexInfo innerVertex, VertexInfo outerVertex, TWeight weight)
        {
            InnerVertex = innerVertex;
            OuterVertex = outerVertex;
            Weight = weight;
        }
    }
}

/// <summary>
/// Sparse directed graph which implement by orthogonal list.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
public class DirectedGraph<TVertex> : IGraph<TVertex>, IDirectedGraph<TVertex> where TVertex : notnull
{
    // Proxy for weighted graph with None weighted.
    private readonly DirectedGraph<TVertex, Unit> m_Graph = new();

    public int VertexCount => m_Graph.VertexCount;

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
    public TVertex[] GetAdjacencyInnerVertexesOf(TVertex vertex) => m_Graph.GetAdjacencyInnerVertexesOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TVertex[] GetAdjacencyOuterVertexesOf(TVertex vertex) => m_Graph.GetAdjacencyOuterVertexesOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetInDegreeOf(TVertex vertex) => m_Graph.GetInDegreeOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetOutDegreeOf(TVertex vertex) => m_Graph.GetOutDegreeOf(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => ((IEnumerable<TVertex>) m_Graph).GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) m_Graph).GetEnumerator();
}