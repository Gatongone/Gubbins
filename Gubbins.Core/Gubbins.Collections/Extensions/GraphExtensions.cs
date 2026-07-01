using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;
using Gubbins.Spawner;

namespace Gubbins.Collections;

/// <summary>
/// Provides conversion, spanning-forest, and reduction helpers for graph abstractions.
/// </summary>
public static class GraphExtensions
{
    /// <summary>
    /// Adds read-only weighted graph conversion helpers.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight type.</typeparam>
    /// <param name="graph">Source graph.</param>
    extension<TVertex, TWeight>(IReadOnlyWeightedGraph<TVertex, TWeight> graph)
    {
        /// <summary>
        /// Converts a weighted graph to an edge-set array representation.
        /// </summary>
        /// <param name="isDirected">Whether output should be treated as directed.</param>
        /// <returns>Edge-set array snapshot of the graph.</returns>
        public EdgesetArray<TVertex, TWeight> ToEdgesetArray(bool isDirected)
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
    }

    /// <summary>
    /// Adds read-only unweighted graph conversion helpers.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <param name="graph">Source graph.</param>
    extension<TVertex>(IReadOnlyGraph<TVertex> graph)
    {
        /// <summary>
        /// Converts an unweighted graph to an edge-set array representation.
        /// </summary>
        /// <param name="isDirected">Whether output should be treated as directed.</param>
        /// <returns>Edge-set array snapshot of the graph.</returns>
        public EdgesetArray<TVertex> ToEdgesetArray(bool isDirected)
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
    }

    /// <summary>
    /// Adds minimum/maximum spanning-forest algorithms for undirected weighted graphs.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight type.</typeparam>
    /// <param name="graph">Graph to mutate.</param>
    extension<TVertex, TWeight>(IUndirectedWeightedGraph<TVertex, TWeight> graph) where TVertex : notnull
    {
        /// <summary>
        /// Applies Kruskal in-place and keeps only the min/max spanning forest edges.
        /// </summary>
        /// <param name="reversed"><see langword="false"/> for minimum; <see langword="true"/> for maximum.</param>
        public void Kruskal(bool reversed = false)
        {
            var vertexes = graph.ToArray();
            var edges = Pool<List<UndirectedEdge<TVertex, TWeight>>>.Default.Spawn();
            edges.Clear();

            try
            {
                for (var i = 0; i < vertexes.Length; i++)
                {
                    for (var j = i + 1; j < vertexes.Length; j++)
                    {
                        if (graph.TryGetWeight(vertexes[i], vertexes[j], out var weight))
                            edges.Add(new UndirectedEdge<TVertex, TWeight>(vertexes[i], vertexes[j], weight));
                    }
                }

                edges.Sort((x, y) =>
                {
                    var c = Comparer<TWeight>.Default.Compare(x.Weight, y.Weight);
                    return reversed ? -c : c;
                });

                var uf = new SetGroup<TVertex>(vertexes);
                var kept = new HashSet<UndirectedEdge<TVertex, TWeight>>();
                foreach (var e in edges)
                {
                    if (uf.TryMerge(e.Start, e.End))
                        kept.Add(e);
                }

                for (var i = 0; i < vertexes.Length; i++)
                {
                    for (var j = i + 1; j < vertexes.Length; j++)
                    {
                        if (!graph.TryGetWeight(vertexes[i], vertexes[j], out var weight)) continue;
                        var edge = new UndirectedEdge<TVertex, TWeight>(vertexes[i], vertexes[j], weight);
                        if (!kept.Contains(edge))
                            graph.RemoveEdge(vertexes[i], vertexes[j]);
                    }
                }
            }
            finally
            {
                edges.Clear();
                Pool<List<UndirectedEdge<TVertex, TWeight>>>.Default.Recycle(edges);
            }
        }

        /// <summary>
        /// Applies Prim in-place and keeps only the min/max spanning forest edges.
        /// </summary>
        /// <param name="reversed"><see langword="false"/> for minimum; <see langword="true"/> for maximum.</param>
        public void Prim(bool reversed = false)
        {
            var vertexes = graph.ToArray();
            var globalVisited = new HashSet<TVertex>();
            var kept = new HashSet<UndirectedEdge<TVertex, TWeight>>();
            var comparer = Comparer<TWeight>.Default;

            foreach (var start in vertexes)
            {
                if (!globalVisited.Add(start)) continue;

                var componentVisited = Pool<HashSet<TVertex>>.Default.Spawn();
                componentVisited.Clear();
                componentVisited.Add(start);

                try
                {
                    while (true)
                    {
                        var found = false;
                        TVertex bestFrom = default!;
                        TVertex bestTo = default!;
                        TWeight bestWeight = default!;

                        foreach (var from in componentVisited)
                        {
                            foreach (var (to, weight) in graph.GetAdjacencyEdgesOf(from))
                            {
                                if (componentVisited.Contains(to)) continue;

                                if (!found)
                                {
                                    found      = true;
                                    bestFrom   = from;
                                    bestTo     = to;
                                    bestWeight = weight;
                                    continue;
                                }

                                var cmp = comparer.Compare(weight, bestWeight);
                                if ((!reversed && cmp < 0) || (reversed && cmp > 0))
                                {
                                    bestFrom   = from;
                                    bestTo     = to;
                                    bestWeight = weight;
                                }
                            }
                        }

                        if (!found) break;

                        componentVisited.Add(bestTo);
                        globalVisited.Add(bestTo);
                        kept.Add(new UndirectedEdge<TVertex, TWeight>(bestFrom, bestTo, bestWeight));
                    }
                }
                finally
                {
                    componentVisited.Clear();
                    Pool<HashSet<TVertex>>.Default.Recycle(componentVisited);
                }
            }

            for (var i = 0; i < vertexes.Length; i++)
            {
                for (var j = i + 1; j < vertexes.Length; j++)
                {
                    if (!graph.TryGetWeight(vertexes[i], vertexes[j], out var weight)) continue;
                    var edge = new UndirectedEdge<TVertex, TWeight>(vertexes[i], vertexes[j], weight);
                    if (!kept.Contains(edge))
                        graph.RemoveEdge(vertexes[i], vertexes[j]);
                }
            }
        }
    }

    /// <summary>
    /// Adds spanning-forest algorithms for undirected unweighted graphs.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <param name="graph">Graph to mutate.</param>
    extension<TVertex>(IUndirectedGraph<TVertex> graph) where TVertex : notnull
    {
        /// <summary>
        /// Applies Kruskal in-place on an unweighted undirected graph and keeps one spanning forest.
        /// </summary>
        public void Kruskal()
        {
            var vertexes = graph.ToArray();
            var uf = new SetGroup<TVertex>(vertexes);
            var kept = new HashSet<UndirectedEdge<TVertex, Unit>>();

            for (var i = 0; i < vertexes.Length; i++)
            {
                for (var j = i + 1; j < vertexes.Length; j++)
                {
                    if (!graph.ContainsEdge(vertexes[i], vertexes[j])) continue;
                    if (uf.TryMerge(vertexes[i], vertexes[j]))
                        kept.Add(new UndirectedEdge<TVertex, Unit>(vertexes[i], vertexes[j], Unit.Instance));
                }
            }

            for (var i = 0; i < vertexes.Length; i++)
            {
                for (var j = i + 1; j < vertexes.Length; j++)
                {
                    if (!graph.ContainsEdge(vertexes[i], vertexes[j])) continue;
                    var edge = new UndirectedEdge<TVertex, Unit>(vertexes[i], vertexes[j], Unit.Instance);
                    if (!kept.Contains(edge))
                        graph.RemoveEdge(vertexes[i], vertexes[j]);
                }
            }
        }

        /// <summary>
        /// Applies Prim in-place on an unweighted undirected graph and keeps one spanning forest.
        /// </summary>
        public void Prim()
        {
            var vertexes = graph.ToArray();
            var globalVisited = new HashSet<TVertex>();
            var kept = new HashSet<UndirectedEdge<TVertex, Unit>>();

            foreach (var start in vertexes)
            {
                if (!globalVisited.Add(start)) continue;

                var componentVisited = Pool<HashSet<TVertex>>.Default.Spawn();
                componentVisited.Clear();
                componentVisited.Add(start);

                try
                {
                    while (true)
                    {
                        var found = false;
                        TVertex from = default!;
                        TVertex to = default!;

                        foreach (var v in componentVisited)
                        {
                            foreach (var adj in graph.GetAdjacencyVertexesOf(v))
                            {
                                if (componentVisited.Contains(adj)) continue;
                                from  = v;
                                to    = adj;
                                found = true;
                                break;
                            }

                            if (found) break;
                        }

                        if (!found) break;

                        componentVisited.Add(to);
                        globalVisited.Add(to);
                        kept.Add(new UndirectedEdge<TVertex, Unit>(from, to, Unit.Instance));
                    }
                }
                finally
                {
                    componentVisited.Clear();
                    Pool<HashSet<TVertex>>.Default.Recycle(componentVisited);
                }
            }

            for (var i = 0; i < vertexes.Length; i++)
            {
                for (var j = i + 1; j < vertexes.Length; j++)
                {
                    if (!graph.ContainsEdge(vertexes[i], vertexes[j])) continue;
                    var edge = new UndirectedEdge<TVertex, Unit>(vertexes[i], vertexes[j], Unit.Instance);
                    if (!kept.Contains(edge))
                        graph.RemoveEdge(vertexes[i], vertexes[j]);
                }
            }
        }
    }

    /// <param name="graph">The directed graph to reduce.</param>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Edge weight type.</typeparam>
    extension<TVertex, TWeight>(IDirectedWeightedGraph<TVertex, TWeight> graph) where TVertex : notnull
    {
        /// <summary>
        /// Performs transitive reduction in-place by removing redundant directed edges.
        /// </summary>
        public void TransitiveReduction()
        {
            var edgesToRemove = new List<(TVertex, TVertex)>();

            foreach (var start in graph)
            {
                var directDeps = graph.GetAdjacencyOuterVertexesOf(start);
                foreach (var end in directDeps)
                {
                    if (graph.HasAlternatePath(start, end))
                        edgesToRemove.Add((start, end));
                }
            }

            foreach (var (start, end) in edgesToRemove)
                graph.RemoveEdge(start, end);
        }

        /// <summary>
        /// Determines whether a path from <paramref name="start"/> to <paramref name="target"/> exists
        /// after excluding the direct edge from <paramref name="start"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="start">Path origin.</param>
        /// <param name="target">Path destination.</param>
        /// <returns>
        /// <see langword="true"/> if an alternate path exists; otherwise, <see langword="false"/>.
        /// </returns>
        private bool HasAlternatePath(TVertex start, TVertex target)
        {
            var visited = Pool<HashSet<TVertex>>.Default.Spawn();
            var stack = Pool<Stack<TVertex>>.Default.Spawn();
            visited.Clear();
            stack.Clear();

            try
            {
                visited.Add(start);
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    var nexts = graph.GetAdjacencyOuterVertexesOf(current);

                    foreach (var next in nexts)
                    {
                        if (EqualityComparer<TVertex>.Default.Equals(current, start) && next.Equals(target))
                            continue;

                        if (next.Equals(target))
                            return true;

                        if (visited.Add(next))
                            stack.Push(next);
                    }
                }

                return false;
            }
            finally
            {
                visited.Clear();
                stack.Clear();
                Pool<HashSet<TVertex>>.Default.Recycle(visited);
                Pool<Stack<TVertex>>.Default.Recycle(stack);
            }
        }
    }

    /// <param name="graph">The directed graph to reduce.</param>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    extension<TVertex>(IDirectedGraph<TVertex> graph) where TVertex : notnull
    {
        /// <summary>
        /// Performs transitive reduction in-place by removing redundant directed edges.
        /// </summary>
        public void TransitiveReduction()
        {
            var edgesToRemove = new List<(TVertex, TVertex)>();

            foreach (var start in graph)
            {
                var directDeps = graph.GetAdjacencyOuterVertexesOf(start);
                foreach (var end in directDeps)
                {
                    if (graph.HasAlternatePath(start, end))
                        edgesToRemove.Add((start, end));
                }
            }

            foreach (var (start, end) in edgesToRemove)
                graph.RemoveEdge(start, end);
        }

        /// <summary>
        /// Determines whether a path from <paramref name="start"/> to <paramref name="target"/> exists
        /// after excluding the direct edge from <paramref name="start"/> to <paramref name="target"/>.
        /// </summary>
        /// <param name="start">Path origin.</param>
        /// <param name="target">Path destination.</param>
        /// <returns>
        /// <see langword="true"/> if an alternate path exists; otherwise, <see langword="false"/>.
        /// </returns>
        private bool HasAlternatePath(TVertex start, TVertex target)
        {
            var visited = Pool<HashSet<TVertex>>.Default.Spawn();
            var stack = Pool<Stack<TVertex>>.Default.Spawn();
            visited.Clear();
            stack.Clear();

            try
            {
                visited.Add(start);
                stack.Push(start);

                while (stack.Count > 0)
                {
                    var current = stack.Pop();
                    var nexts = graph.GetAdjacencyOuterVertexesOf(current);

                    foreach (var next in nexts)
                    {
                        if (EqualityComparer<TVertex>.Default.Equals(current, start) && next.Equals(target))
                            continue;

                        if (next.Equals(target))
                            return true;

                        if (visited.Add(next))
                            stack.Push(next);
                    }
                }

                return false;
            }
            finally
            {
                visited.Clear();
                stack.Clear();
                Pool<HashSet<TVertex>>.Default.Recycle(visited);
                Pool<Stack<TVertex>>.Default.Recycle(stack);
            }
        }
    }

    /// <summary>
    /// Represents a directed edge identity plus its weight for local algorithms.
    /// Equality is based on endpoints only.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Weight type.</typeparam>
    private readonly struct DirectedEdge<TVertex, TWeight> : IEquatable<DirectedEdge<TVertex, TWeight>>, IEqualityComparer<DirectedEdge<TVertex, TWeight>>, IComparable<DirectedEdge<TVertex, TWeight>>
    {
        /// <summary>Edge start vertex.</summary>
        public readonly TVertex Start;

        /// <summary>Edge end vertex.</summary>
        public readonly TVertex End;

        /// <summary>Edge weight.</summary>
        public readonly TWeight Weight;

        /// <summary>Default edge value.</summary>
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        public static readonly DirectedEdge<TVertex, TWeight>? Default = new DirectedEdge<TVertex, TWeight>(default!, default!, default!);

        /// <summary>Ascending comparer by weight.</summary>
        public static readonly IComparer<DirectedEdge<TVertex, TWeight>> Less = new Comparer(false);

        /// <summary>Descending comparer by weight.</summary>
        public static readonly IComparer<DirectedEdge<TVertex, TWeight>> Greater = new Comparer(true);

        internal DirectedEdge(TVertex start, TVertex end, TWeight weight)
        {
            Start  = start;
            End    = end;
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

        /// <summary>
        /// Compares directed edges by weight.
        /// </summary>
        private class Comparer : IComparer<DirectedEdge<TVertex, TWeight>>
        {
            private static readonly Comparer<TWeight> s_DefaultComparer = Comparer<TWeight>.Default;

            private readonly bool m_Reversed;

            public Comparer(bool reversed) => m_Reversed = reversed;

            public int Compare(DirectedEdge<TVertex, TWeight> x, DirectedEdge<TVertex, TWeight> y) => m_Reversed ? s_DefaultComparer.Compare(y.Weight, x.Weight) : s_DefaultComparer.Compare(x.Weight, y.Weight);
        }
    }

    /// <summary>
    /// Represents an undirected edge identity plus its weight for local algorithms.
    /// Equality is based on endpoints regardless of order.
    /// </summary>
    /// <typeparam name="TVertex">Vertex type.</typeparam>
    /// <typeparam name="TWeight">Weight type.</typeparam>
    private readonly struct UndirectedEdge<TVertex, TWeight> : IEquatable<UndirectedEdge<TVertex, TWeight>>, IEqualityComparer<UndirectedEdge<TVertex, TWeight>>, IComparable<UndirectedEdge<TVertex, TWeight>>
    {
        /// <summary>One endpoint of the edge.</summary>
        public readonly TVertex Start;

        /// <summary>The other endpoint of the edge.</summary>
        public readonly TVertex End;

        /// <summary>Edge weight.</summary>
        public readonly TWeight Weight;

        /// <summary>Default edge value.</summary>
        public static readonly UndirectedEdge<TVertex, TWeight>? Default = new UndirectedEdge<TVertex, TWeight>(default!, default!, default!);

        /// <summary>Ascending comparer by weight.</summary>
        public static readonly IComparer<UndirectedEdge<TVertex, TWeight>> Less = new Comparer(false);

        /// <summary>Descending comparer by weight.</summary>
        public static readonly IComparer<UndirectedEdge<TVertex, TWeight>> Greater = new Comparer(true);

        internal UndirectedEdge(TVertex start, TVertex end, TWeight weight)
        {
            Start  = start;
            End    = end;
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

        /// <summary>
        /// Compares undirected edges by weight.
        /// </summary>
        private class Comparer : IComparer<UndirectedEdge<TVertex, TWeight>>
        {
            private static readonly Comparer<TWeight> s_DefaultComparer = Comparer<TWeight>.Default;

            private readonly bool m_Reversed;

            public Comparer(bool reversed) => m_Reversed = reversed;

            public int Compare(UndirectedEdge<TVertex, TWeight> x, UndirectedEdge<TVertex, TWeight> y) => m_Reversed ? s_DefaultComparer.Compare(y.Weight, x.Weight) : s_DefaultComparer.Compare(x.Weight, y.Weight);
        }
    }
}