using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Gubbins.Collection;

public static class GraphExtensions
{
    public static EdgesetArray<TVertex, TWeight> ToEdgesetArray<TVertex, TWeight>(DirectedGraph<TVertex, TWeight> graph) => ToEdgesetArray(graph, true);

    public static EdgesetArray<TVertex, TWeight> ToEdgesetArray<TVertex, TWeight>(UndirectedGraph<TVertex, TWeight> graph) => ToEdgesetArray(graph, false);

    public static EdgesetArray<TVertex, TWeight> ToEdgesetArray<TVertex, TWeight>(IReadOnlyWeightedGraph<TVertex, TWeight> graph, bool isDirected)
    {
        var vertexes = new List<TVertex>();
        var adjEdges = new List<EdgesetArray<TVertex, TWeight>.Edge>();
        switch (graph)
        {
            case IDirectedWeightedGraph<TVertex, TWeight> directedGraph when isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(directedGraph.GetAdjacencyOuterEdgesOf(vertex)
                                                   .Select(adjInfo => new EdgesetArray<TVertex, TWeight>.Edge(vertex, adjInfo.Vertex, adjInfo.Weight, true)));
                }

                return new EdgesetArray<TVertex, TWeight>(vertexes, adjEdges, true);
            }
            case IDirectedGraph<TVertex> directedGraph when isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(directedGraph.GetAdjacencyOuterVertexesOf(vertex)
                                                   .Select(adjVertex => new EdgesetArray<TVertex, TWeight>.Edge(vertex, adjVertex, graph.GetWeight(vertex, adjVertex), true)));
                }

                return new EdgesetArray<TVertex, TWeight>(vertexes, adjEdges, true);
            }
            case IUndirectedWeightedGraph<TVertex, TWeight> undirectedGraph when !isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(undirectedGraph.GetAdjacencyEdgesOf(vertex)
                                                     .Select(adjInfo => new EdgesetArray<TVertex, TWeight>.Edge(vertex, adjInfo.AdjacencyVertex, adjInfo.Weight, false)));
                }

                return new EdgesetArray<TVertex, TWeight>(vertexes, adjEdges, false);
            }
            case IUndirectedGraph<TVertex> undirectedGraph when !isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(undirectedGraph.GetAdjacencyVertexesOf(vertex)
                                                     .Select(adjVertex => new EdgesetArray<TVertex, TWeight>.Edge(vertex, adjVertex, graph.GetWeight(vertex, adjVertex), false)));
                }

                return new EdgesetArray<TVertex, TWeight>(vertexes, adjEdges, false);
            }
            default:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(graph.Where(endVertex => graph.ContainsEdge(vertex, endVertex))
                                           .Select(adjVertex => new EdgesetArray<TVertex, TWeight>.Edge(vertex, adjVertex, graph.GetWeight(vertex, adjVertex), isDirected)));
                }

                return new EdgesetArray<TVertex, TWeight>(vertexes, adjEdges, isDirected);
            }
        }
    }

    public static EdgesetArray<TVertex> ToEdgesetArray<TVertex>(IReadOnlyGraph<TVertex> graph, bool isDirected)
    {
        var vertexes = new List<TVertex>();
        var adjEdges = new List<EdgesetArray<TVertex>.Edge>();
        switch (graph)
        {
            case IDirectedGraph<TVertex> directedGraph when isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(directedGraph.GetAdjacencyOuterVertexesOf(vertex)
                                                   .Select(adjVertex => new EdgesetArray<TVertex>.Edge(vertex, adjVertex, true)));
                }

                return new EdgesetArray<TVertex>(vertexes, adjEdges, true);
            }
            case IUndirectedGraph<TVertex> undirectedGraph when !isDirected:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(undirectedGraph.GetAdjacencyVertexesOf(vertex)
                                                     .Select(adjVertex => new EdgesetArray<TVertex>.Edge(vertex, adjVertex, false)));
                }

                return new EdgesetArray<TVertex>(vertexes, adjEdges, false);
            }
            default:
            {
                foreach (var vertex in graph)
                {
                    vertexes.Add(vertex);
                    adjEdges.AddRange(graph.Where(endVertex => graph.ContainsEdge(vertex, endVertex))
                                           .Select(adjVertex => new EdgesetArray<TVertex>.Edge(vertex, adjVertex, isDirected)));
                }

                return new EdgesetArray<TVertex>(vertexes, adjEdges, isDirected);
            }
        }
    }

    /// <summary>
    /// Get minimum/maximum spanning tree/forest in raw graph with Kruskal's algorithm.
    /// It will return a graph with all minimally/extremely connected subgraph in raw graph when the raw graph is not connected.
    /// It will return a minimum spanning tree when the raw graph is connected.
    /// </summary>
    /// <param name="graph">Weighted graph.</param>
    /// <param name="reversed">If false, it would return minimum spanning tree/forest. If true, it would return maximum spanning tree/forest</param>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight.</typeparam>
    /// <returns>minimum/maximum spanning tree/forest.</returns>
    public static IReadOnlyWeightedGraph<TVertex, TWeight> Kruskal<TVertex, TWeight>(this IUndirectedWeightedGraph<TVertex, TWeight> graph, bool reversed) where TVertex : notnull
    {
        var mst = new DirectedGraph<TVertex, TWeight>();
        var edges = new SortedSet<UndirectedEdge<TVertex, TWeight>>(reversed ? UndirectedEdge<TVertex, TWeight>.Greater : UndirectedEdge<TVertex, TWeight>.Less);

        var vertexes = graph.ToArray();
        for (var i = 0; i < vertexes.Length; i++)
        {
            for (var j = i + 1; j < vertexes.Length; j++)
            {
                if (!graph.TryGetWeight(vertexes[i], vertexes[j], out var weight)) continue;
                var edge = new UndirectedEdge<TVertex, TWeight>(vertexes[i], vertexes[j], weight);
                if (edges.Contains(edge)) continue;
                edges.Add(edge);
            }
        }

        var unionFind = new SetGroup<TVertex>(vertexes);
        foreach (var edge in edges)
        {
            if (!unionFind.TryMerge(edge.Start, edge.End))
            {
                mst.AddEdgeWithoutExistentCheck(edge.Start, edge.End, edge.Weight);
            }
        }

        return mst;
    }

    /// <summary>
    /// Get minimum/maximum spanning tree/forest in raw graph with Kruskal's algorithm.
    /// It will return a graph with all minimally/extremely strongly connected subgraph in raw graph when the raw graph is not connected.
    /// It will return a minimum spanning tree when the raw graph is connected.
    /// </summary>
    /// <param name="graph">Weighted graph.</param>
    /// <param name="reversed">If false, it would return minimum spanning tree/forest. If true, it would return maximum spanning tree/forest</param>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight.</typeparam>
    /// <returns>minimum/maximum spanning tree/forest.</returns>
    public static IReadOnlyWeightedGraph<TVertex, TWeight> Kruskal<TVertex, TWeight>(this IDirectedWeightedGraph<TVertex, TWeight> graph, bool reversed = false) where TVertex : notnull
    {
        var mst = new DirectedGraph<TVertex, TWeight>();
        var edges = new SortedSet<DirectedEdge<TVertex, TWeight>>(reversed ? DirectedEdge<TVertex, TWeight>.Greater : DirectedEdge<TVertex, TWeight>.Less);
        var vertexes = graph.ToArray();
        for (var i = 0; i < vertexes.Length; i++)
        {
            for (var j = i + 1; j < vertexes.Length; j++)
            {
                if (!graph.TryGetWeight(vertexes[i], vertexes[j], out var weight)) continue;
                var edge = new DirectedEdge<TVertex, TWeight>(vertexes[i], vertexes[j], weight);
                if (!edges.Add(edge)) continue;
            }
        }

        var unionFind = new SetGroup<TVertex>(vertexes);
        foreach (var edge in edges)
        {
            if (!unionFind.TryMerge(edge.Start, edge.End))
            {
                mst.AddEdgeWithoutExistentCheck(edge.Start, edge.End, edge.Weight);
            }
        }

        return mst;
    }

    /// <summary>
    /// Get a minimally/extremely connected subgraph in raw graph with Prim's algorithm.
    /// It may return a minimally/extremely connected subgraph in raw graph when the graph is not connected.
    /// It will return a minimum spanning tree when the raw graph is connected.
    /// </summary>
    /// <param name="graph">Connected graph </param>
    /// <param name="extremeWeight">Minimum/Maximum weight.</param>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight.</typeparam>
    /// <returns>minimally/extremely connected subgraph in raw graph.</returns>
    public static IReadOnlyWeightedGraph<TVertex, TWeight> Prim<TVertex, TWeight>(this IUndirectedWeightedGraph<TVertex, TWeight> graph, TWeight extremeWeight) where TVertex : notnull
    {
        var mst = new UndirectedGraph<TVertex, TWeight>();

        var visited = new HashSet<TVertex>();

        foreach (var vertex in graph)
        {
            if (visited.Count == 0)
            {
                visited.Add(vertex);
                continue;
            }

            var edge = UndirectedEdge<TVertex, TWeight>.Default;

            var adjEdges = graph.GetAdjacencyEdgesOf(vertex);

            // The graph is not a  connected graph. And it's a free vertex.
            if (adjEdges.Length == 0)
                continue;

            // Find minimum edge.
            foreach (var adjEdge in adjEdges)
            {
                var weight = adjEdge.Weight;
                if (!visited.Contains(adjEdge.AdjacencyVertex) && Comparer<TWeight>.Default.Compare(weight, extremeWeight) < 0)
                {
                    edge = new UndirectedEdge<TVertex, TWeight>(vertex, adjEdge.AdjacencyVertex, weight);
                    extremeWeight = weight;
                }
            }

            // Sanity
            Debug.Assert(edge != null);

            // Add to mst.
            var value = edge.Value;
            mst.AddEdgeWithoutExistentCheck(value.Start, value.End, value.Weight);

            // Set visited.
            visited.Add(value.End);
        }

        return mst;
    }

    private readonly struct DirectedEdge<TVertex, TWeight> : IEquatable<DirectedEdge<TVertex, TWeight>>, IEqualityComparer<DirectedEdge<TVertex, TWeight>>, IComparable<DirectedEdge<TVertex, TWeight>>
    {
        public readonly TVertex Start;
        public readonly TVertex End;
        public readonly TWeight Weight;

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static readonly DirectedEdge<TVertex, TWeight>? Default = new DirectedEdge<TVertex, TWeight>(default!, default!, default!);

        public static readonly IComparer<DirectedEdge<TVertex, TWeight>> Less = new Comparer(false);
        public static readonly IComparer<DirectedEdge<TVertex, TWeight>> Greater = new Comparer(true);

        internal DirectedEdge(TVertex start, TVertex end, TWeight weight)
        {
            Start = start;
            End = end;
            Weight = weight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(DirectedEdge<TVertex, TWeight> other) => EqualityComparer<TVertex>.Default.Equals(Start, other.Start) && EqualityComparer<TVertex>.Default.Equals(End, other.End);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(DirectedEdge<TVertex, TWeight> x, DirectedEdge<TVertex, TWeight> y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(DirectedEdge<TVertex, TWeight> other) => Less.Compare(this, other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is DirectedEdge<TVertex, TWeight> other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DirectedEdge<TVertex, TWeight> left, DirectedEdge<TVertex, TWeight> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DirectedEdge<TVertex, TWeight> left, DirectedEdge<TVertex, TWeight> right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(DirectedEdge<TVertex, TWeight> obj) => obj.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
#if NET7_0_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            return HashCode.Combine(Start, End);
#else
            unchecked
            {
                var hash = (int) 2166136261;
                hash = (hash * 16777619) ^ Start?.GetHashCode() ?? 0;
                hash = (hash * 16777619) ^ End?.GetHashCode() ?? 0;
                return hash;
            }
#endif
        }

        private class Comparer : IComparer<DirectedEdge<TVertex, TWeight>>
        {
            private static readonly Comparer<TWeight> s_DefaultComparer = Comparer<TWeight>.Default;

            private readonly bool m_Reversed;

            public Comparer(bool reversed) => m_Reversed = reversed;

            public int Compare(DirectedEdge<TVertex, TWeight> x, DirectedEdge<TVertex, TWeight> y) => m_Reversed ? s_DefaultComparer.Compare(y.Weight, x.Weight) : s_DefaultComparer.Compare(x.Weight, y.Weight);
        }
    }

    private readonly struct UndirectedEdge<TVertex, TWeight> : IEquatable<UndirectedEdge<TVertex, TWeight>>, IEqualityComparer<UndirectedEdge<TVertex, TWeight>>, IComparable<UndirectedEdge<TVertex, TWeight>>
    {
        public readonly TVertex Start;
        public readonly TVertex End;
        public readonly TWeight Weight;
        public static readonly UndirectedEdge<TVertex, TWeight>? Default = new UndirectedEdge<TVertex, TWeight>(default!, default!, default!);
        public static readonly IComparer<UndirectedEdge<TVertex, TWeight>> Less = new Comparer(false);
        public static readonly IComparer<UndirectedEdge<TVertex, TWeight>> Greater = new Comparer(true);

        internal UndirectedEdge(TVertex start, TVertex end, TWeight weight)
        {
            Start = start;
            End = end;
            Weight = weight;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UndirectedEdge<TVertex, TWeight> other)
            => EqualityComparer<TVertex>.Default.Equals(Start, other.Start) && EqualityComparer<TVertex>.Default.Equals(End, other.End) ||
                EqualityComparer<TVertex>.Default.Equals(Start, other.End) && EqualityComparer<TVertex>.Default.Equals(End, other.Start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(UndirectedEdge<TVertex, TWeight> x, UndirectedEdge<TVertex, TWeight> y) => x.Equals(y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CompareTo(UndirectedEdge<TVertex, TWeight> other) => Less.Compare(this, other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is UndirectedEdge<TVertex, TWeight> other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UndirectedEdge<TVertex, TWeight> left, UndirectedEdge<TVertex, TWeight> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UndirectedEdge<TVertex, TWeight> left, UndirectedEdge<TVertex, TWeight> right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(UndirectedEdge<TVertex, TWeight> obj) => obj.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
#if NETCOREAPP2_1_OR_GREATER
            return HashCode.Combine(Start, End);
#else
            unchecked
            {
                var hash = (int) 2166136261;
                hash = (hash * 16777619) ^ Start?.GetHashCode() ?? 0;
                hash = (hash * 16777619) ^ End?.GetHashCode() ?? 0;
                return hash;
            }
#endif
        }

        private class Comparer : IComparer<UndirectedEdge<TVertex, TWeight>>
        {
            private static readonly Comparer<TWeight> s_DefaultComparer = Comparer<TWeight>.Default;

            private readonly bool m_Reversed;

            public Comparer(bool reversed) => m_Reversed = reversed;

            public int Compare(UndirectedEdge<TVertex, TWeight> x, UndirectedEdge<TVertex, TWeight> y) => m_Reversed ? s_DefaultComparer.Compare(y.Weight, x.Weight) : s_DefaultComparer.Compare(x.Weight, y.Weight);
        }
    }
}