using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Gubbins.Collections;

/// <summary>
/// Weighted graph with vertexes and edges array.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
/// <typeparam name="TWeight">Graph edge weight.</typeparam>
[Serializable]
public class EdgesetArray<TVertex, TWeight> : IWeightedGraph<TVertex, TWeight>, ISerializable
{
    private readonly List<TVertex> m_Vertexes;
    private readonly List<Edge> m_Edges;
    public readonly bool IsDirected;
    public IReadOnlyList<TVertex> Vertexes => m_Vertexes;
    public IReadOnlyList<Edge> Edges => m_Edges;
    public int VertexCount => m_Vertexes.Count;
    public int EdgeCount => m_Edges.Count;

    public EdgesetArray(IEnumerable<TVertex> vertexes, bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (vertexes.ToList(), new(), isDirected);

    public EdgesetArray(bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (new(), new(), isDirected);

    internal EdgesetArray(IEnumerable<TVertex> vertexes, IEnumerable<Edge> edges, bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (vertexes.ToList(), edges.ToList(), isDirected);

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Directed", IsDirected);
        info.AddValue(nameof(Vertexes), m_Vertexes.ToArray());
        info.AddValue(nameof(Edges), Edges.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex) => m_Vertexes.Add(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEdge(Edge edge)
    {
        if (ContainsEdge(edge))
            throw new ArgumentException("Edge already exists.");
        if (!m_Vertexes.Contains(edge.Start))
            m_Vertexes.Add(edge.Start);
        if (!m_Vertexes.Contains(edge.End))
            m_Vertexes.Add(edge.End);
        m_Edges.Add(edge);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end, TWeight weight) => AddEdge(new Edge(start, end, weight, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex)
    {
        if (ContainsVertex(vertex))
            return false;
        m_Vertexes.Add(vertex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end, TWeight weight)
    {
        if (!m_Vertexes.Contains(start) || !m_Vertexes.Contains(end) || ContainsEdge(start, end))
            return false;

        m_Edges.Add(new Edge(start, end, weight, IsDirected));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveVertex(TVertex vertex)
    {
        if (vertex == null)
            return false;
        m_Edges.RemoveAll(edge => (edge.Start != null && edge.Start.Equals(vertex)) || (edge.End != null && edge.End.Equals(vertex)));
        return m_Vertexes.Remove(vertex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveEdge(Edge edge) => m_Edges.Remove(edge);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) => RemoveEdge(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAllEdges() => m_Edges.Clear();

    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex)
    {
        var index = m_Vertexes.IndexOf(oldVertex);
        if (index < 0)
            throw new ArgumentException("Vertex does not exist.");

        // Reset vertex in vertex list.
        m_Vertexes[index] = newVertex;

        // Reset vertex in edge list.
        for (var i = 0; i < m_Edges.Count; i++)
        {
            var edge = m_Edges[i];
            if (edge.Start != null && edge.Start.Equals(oldVertex))
                m_Edges[i] = new Edge(newVertex, edge.End, edge.Weight, IsDirected);
            if (edge.End != null && edge.End.Equals(oldVertex))
                m_Edges[i] = new Edge(edge.Start, newVertex, edge.Weight, IsDirected);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_Vertexes.Clear();
        m_Edges.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_Vertexes.Contains(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsEdge(Edge edge) => m_Edges.Contains(edge);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => ContainsEdge(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TWeight GetWeight(Edge edge)
    {
        var index = m_Edges.IndexOf(edge);
        if (index < 0)
            throw new ArgumentException("Edge does not exist.");
        return m_Edges[index].Weight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetWeight(TVertex start, TVertex end, out TWeight weight)
    {
        var edge = new Edge(start, end, IsDirected);
        var index = m_Edges.IndexOf(edge);
        if (index < 0)
        {
            weight = default!;
            return false;
        }

        weight = m_Edges[index].Weight;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TWeight GetWeight(TVertex start, TVertex end) => GetWeight(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWeight(Edge edge)
    {
        var index = m_Edges.IndexOf(edge);
        if (index < 0)
            throw new ArgumentException("Edge does not exist.");
        m_Edges[index] = edge;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetWeight(TVertex start, TVertex end, TWeight weight) => SetWeight(new Edge(start, end, weight, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => m_Vertexes.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => m_Vertexes.GetEnumerator();

    public readonly struct Edge : IEquatable<Edge>, IEqualityComparer<Edge>
    {
        private readonly bool m_IsDirected;
        public readonly TVertex Start;
        public readonly TVertex End;
        public readonly TWeight Weight;

        internal Edge(TVertex start, TVertex end, bool isDirected)
        {
            Start = start;
            End = end;
            m_IsDirected = isDirected;
            Weight = default!;
        }

        internal Edge(TVertex start, TVertex end, TWeight weight, bool isDirected)
        {
            Start = start;
            End = end;
            Weight = weight;
            m_IsDirected = isDirected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Edge other) =>
            EqualityComparer<TVertex>.Default.Equals(Start, other.Start) && EqualityComparer<TVertex>.Default.Equals(End, other.End)
            || m_IsDirected
            && EqualityComparer<TVertex>.Default.Equals(Start, other.End) && EqualityComparer<TVertex>.Default.Equals(End, other.Start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Edge x, Edge y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Edge other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Edge left, Edge right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Edge left, Edge right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Edge obj) => obj.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int) 2166136261;
                hash = (hash * 16777619) ^ Start?.GetHashCode() ?? 0;
                hash = (hash * 16777619) ^ End?.GetHashCode() ?? 0;
                return hash;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Start), Start);
            info.AddValue(nameof(End), End);
            info.AddValue(nameof(Weight), Weight);
        }
    }
}

/// <summary>
/// Graph with vertexes and edges array.
/// </summary>
/// <typeparam name="TVertex">Graph node.</typeparam>
public class EdgesetArray<TVertex> : IGraph<TVertex>, ISerializable
{
    private readonly List<TVertex> m_Vertexes;
    private readonly List<Edge> m_Edges;
    public readonly bool IsDirected;
    public IReadOnlyList<TVertex> Vertexes => m_Vertexes.ToArray();
    public IReadOnlyList<Edge> Edges => m_Edges.ToArray();
    public int VertexCount => m_Vertexes.Count;
    public int EdgeCount => m_Edges.Count;

    public EdgesetArray(IEnumerable<TVertex> vertexes, bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (vertexes.ToList(), new(), isDirected);

    public EdgesetArray(bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (new(), new(), isDirected);

    internal EdgesetArray(IEnumerable<TVertex> vertexes, IEnumerable<Edge> edges, bool isDirected) => (m_Vertexes, m_Edges, IsDirected) = (vertexes.ToList(), edges.ToList(), isDirected);

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Vertexes), m_Vertexes.ToArray());
        info.AddValue(nameof(Edges), Edges.ToArray());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddVertex(TVertex vertex) => m_Vertexes.Add(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEdge(Edge edge)
    {
        if (ContainsEdge(edge))
            throw new ArgumentException("Edge already exists.");
        if (!m_Vertexes.Contains(edge.Start))
            m_Vertexes.Add(edge.Start);
        if (!m_Vertexes.Contains(edge.End))
            m_Vertexes.Add(edge.End);
        m_Edges.Add(edge);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddEdge(TVertex start, TVertex end) => AddEdge(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddVertex(TVertex vertex)
    {
        if (ContainsVertex(vertex))
            return false;
        m_Vertexes.Add(vertex);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddEdge(TVertex start, TVertex end)
    {
        if (!m_Vertexes.Contains(start) || !m_Vertexes.Contains(end) || ContainsEdge(start, end))
            return false;

        m_Edges.Add(new Edge(start, end, IsDirected));
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveVertex(TVertex vertex)
    {
        if (vertex == null)
            return false;
        m_Edges.RemoveAll(edge => (edge.Start != null && edge.Start.Equals(vertex)) || (edge.End != null && edge.End.Equals(vertex)));
        return m_Vertexes.Remove(vertex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool RemoveEdge(Edge edge) => m_Edges.Remove(edge);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool RemoveEdge(TVertex start, TVertex end) => RemoveEdge(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RemoveAllEdges() => m_Edges.Clear();

    public void ReplaceVertex(TVertex oldVertex, TVertex newVertex)
    {
        var index = m_Vertexes.IndexOf(oldVertex);
        if (index < 0)
            throw new ArgumentException("Vertex does not exist.");

        // Reset vertex in vertex list.
        m_Vertexes[index] = newVertex;

        // Reset vertex in edge list.
        for (var i = 0; i < m_Edges.Count; i++)
        {
            var edge = m_Edges[i];
            if (edge.Start != null && edge.Start.Equals(oldVertex))
                m_Edges[i] = new Edge(newVertex, edge.End, IsDirected);
            if (edge.End != null && edge.End.Equals(oldVertex))
                m_Edges[i] = new Edge(edge.Start, newVertex, IsDirected);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        m_Vertexes.Clear();
        m_Edges.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsVertex(TVertex vertex) => m_Vertexes.Contains(vertex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ContainsEdge(Edge edge) => m_Edges.Contains(edge);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsEdge(TVertex start, TVertex end) => ContainsEdge(new Edge(start, end, IsDirected));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator<TVertex> IEnumerable<TVertex>.GetEnumerator() => m_Vertexes.GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IEnumerator IEnumerable.GetEnumerator() => m_Vertexes.GetEnumerator();

    [Serializable]
    public readonly struct Edge : IEquatable<Edge>, IEqualityComparer<Edge>, ISerializable
    {
        private readonly bool m_IsDirected;
        public readonly TVertex Start;
        public readonly TVertex End;

        internal Edge(TVertex start, TVertex end, bool isDirected)
        {
            Start = start;
            End = end;
            m_IsDirected = isDirected;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Edge other) =>
            EqualityComparer<TVertex>.Default.Equals(Start, other.Start) && EqualityComparer<TVertex>.Default.Equals(End, other.End)
            || m_IsDirected && EqualityComparer<TVertex>.Default.Equals(Start, other.End) && EqualityComparer<TVertex>.Default.Equals(End, other.Start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Edge x, Edge y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is Edge other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Edge left, Edge right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Edge left, Edge right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Edge obj) => obj.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int) 2166136261;
                hash = (hash * 16777619) ^ Start?.GetHashCode() ?? 0;
                hash = (hash * 16777619) ^ End?.GetHashCode() ?? 0;
                return hash;
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Start), Start);
            info.AddValue(nameof(End), End);
            info.AddValue("Directed", m_IsDirected);
        }
    }
}