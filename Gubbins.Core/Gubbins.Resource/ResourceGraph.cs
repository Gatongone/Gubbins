using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Gubbins.Collections;
using Gubbins.Spawner;

namespace Gubbins.Resource;

/// <summary>
/// Maintains a directed resource dependency graph and can collapse equivalent resources into grouped vertices.
/// </summary>
public class ResourceGraph : IEnumerable<IResourceKey>
{
    /// <summary>
    /// Minimum number of groups required before edge collection considers parallel execution.
    /// </summary>
    private const int PARALLEL_EDGE_COLLECTION_GROUP_THRESHOLD = 64;

    /// <summary>
    /// Backing dependency graph. After optimization, vertices are <see cref="ResourceGroupKey"/> instances.
    /// </summary>
    private readonly DirectedGraph<IResourceKey> m_Graph = new();

    /// <summary>
    /// Adds a resource to the graph.
    /// </summary>
    /// <param name="key">Resource key to add as a vertex.</param>
    public void AddResource(IResourceKey key) => m_Graph.AddVertex(key);

    /// <summary>
    /// Adds a dependency edge from resource to dependency.
    /// </summary>
    /// <param name="resource">Dependent resource.</param>
    /// <param name="dependency">Resource that <paramref name="resource"/> depends on.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="resource"/> equals <paramref name="dependency"/>.</exception>
    public void AddDependency(IResourceKey resource, IResourceKey dependency)
    {
        if (resource.Equals(dependency))
            throw new ArgumentException("Resource cannot depend on itself.", nameof(dependency));

        m_Graph.AddEdge(resource, dependency);
    }

    /// <summary>
    /// Removes a dependency edge.
    /// </summary>
    /// <param name="resource">Dependent resource.</param>
    /// <param name="dependency">Dependency to remove.</param>
    /// <returns><see langword="true"/> when the edge existed and was removed; otherwise <see langword="false"/>.</returns>
    public bool RemoveDependency(IResourceKey resource, IResourceKey dependency)
    {
        return m_Graph.RemoveEdge(resource, dependency);
    }

    /// <summary>
    /// Optimizes the graph, optionally parallelizing read-only phases for large graphs.
    /// The optimization process includes:
    /// <list type="number">
    /// <li>Pruning transitive edges to simplify the graph structure.</li>
    /// <li>Merging nodes that have exactly one dependency into their dependency, effectively collapsing linear chains of dependencies.</li>
    /// <li>Merging nodes that share identical sets of dependencies into single groups, reducing redundancy.</li>
    /// <li>Collapsing the graph in-place by replacing vertices with group keys that own their member resources and updating edges to reflect the new groupings.</li>
    /// </list>
    /// </summary>
    /// <param name="parallel">When <see langword="true"/>, enables parallel edge collection for sufficiently large graphs.</param>
    /// <returns>A <see cref="ResourceGroups"/> instance containing the optimized grouping information.</returns>
    public ResourceGroups Optimize(bool parallel = false)
    {
        // Prune transitive edges
        m_Graph.TransitiveReduction();

        // Compute groups
        var unionFind = new SetGroup<IResourceKey>(m_Graph);
        MergeSingleDependencyGroups(m_Graph, unionFind);
        MergeSameDependencySetGroups(m_Graph, unionFind);

        // Collapse m_Graph in-place. Each remaining vertex is a group key that stores its own members.
        var groupLookup = CollapseGroups(m_Graph, unionFind, parallel);
        return new ResourceGroups(groupLookup);
    }

    /// <summary>
    /// Exposes the graph directly. After <see cref="Optimize()"/>, each vertex is a group key.
    /// </summary>
    public DirectedGraph<IResourceKey> Graph => m_Graph;

    /// <summary>
    /// Collapses groups in-place by replacing graph vertices with group keys that own their members.
    /// </summary>
    /// <param name="graph">Graph to rewrite.</param>
    /// <param name="uf">Union-find structure describing merged groups.</param>
    /// <param name="enableParallel">Enables parallel edge collection for large group counts.</param>
    /// <returns>A lookup from original resource keys to their collapsed group key.</returns>
    private static Dictionary<IResourceKey, ResourceGroupKey> CollapseGroups(
        DirectedGraph<IResourceKey> graph,
        SetGroup<IResourceKey> uf,
        bool enableParallel)
    {
        var groupMap = BuildGroupMap(graph, uf, out var rootLookup);
        var vertexMap = BuildGroupVertexMap(groupMap);
        var edges = CollectGroupEdges(graph, rootLookup, groupMap, vertexMap, enableParallel);
        ReplaceGraphContents(graph, vertexMap.Values, edges);
        return BuildGroupLookup(vertexMap.Values);
    }

    /// <summary>
    /// Builds per-root grouping information and a vertex-to-root lookup table.
    /// </summary>
    private static Dictionary<IResourceKey, GroupInfo> BuildGroupMap(
        DirectedGraph<IResourceKey> graph,
        SetGroup<IResourceKey> uf,
        out Dictionary<IResourceKey, IResourceKey> rootLookup)
    {
        var groupMap = new Dictionary<IResourceKey, GroupInfo>();
        rootLookup = new Dictionary<IResourceKey, IResourceKey>();
        foreach (var vertex in graph)
        {
            var root = uf.FindRoot(vertex);
            rootLookup.Add(vertex, root);
            if (!groupMap.TryGetValue(root, out var info))
            {
                info = new GroupInfo();
                groupMap.Add(root, info);
            }

            info.Vertices.Add(vertex);
            info.AddMembers(vertex);
        }

        return groupMap;
    }

    /// <summary>
    /// Creates concrete graph vertices for each union-find root.
    /// </summary>
    private static Dictionary<IResourceKey, ResourceGroupKey> BuildGroupVertexMap(
        IReadOnlyDictionary<IResourceKey, GroupInfo> groupMap)
    {
        var vertexMap = new Dictionary<IResourceKey, ResourceGroupKey>(groupMap.Count);
        foreach (var (root, info) in groupMap)
        {
            vertexMap.Add(root, new ResourceGroupKey(info.Members));
        }

        return vertexMap;
    }

    /// <summary>
    /// Collects edges between collapsed groups, optionally using a parallel path for large inputs.
    /// </summary>
    private static List<(ResourceGroupKey Start, ResourceGroupKey End)> CollectGroupEdges(
        DirectedGraph<IResourceKey> graph,
        IReadOnlyDictionary<IResourceKey, IResourceKey> rootLookup,
        IReadOnlyDictionary<IResourceKey, GroupInfo> groupMap,
        IReadOnlyDictionary<IResourceKey, ResourceGroupKey> vertexMap,
        bool enableParallel)
    {
        var useParallel = enableParallel &&
            groupMap.Count >= PARALLEL_EDGE_COLLECTION_GROUP_THRESHOLD &&
            Environment.ProcessorCount > 1;

        if (useParallel)
            return CollectGroupEdgesParallel(graph, rootLookup, groupMap, vertexMap);

        var edges = new List<(ResourceGroupKey Start, ResourceGroupKey End)>();
        foreach (var (root, info) in groupMap)
        {
            CollectEdgesForGroup(graph, rootLookup, vertexMap, edge => edges.Add(edge), root, info);
        }

        return edges;
    }

    /// <summary>
    /// Parallel edge collection implementation used when optimization heuristics allow it.
    /// </summary>
    private static List<(ResourceGroupKey Start, ResourceGroupKey End)> CollectGroupEdgesParallel(
        DirectedGraph<IResourceKey> graph,
        IReadOnlyDictionary<IResourceKey, IResourceKey> rootLookup,
        IReadOnlyDictionary<IResourceKey, GroupInfo> groupMap,
        IReadOnlyDictionary<IResourceKey, ResourceGroupKey> vertexMap)
    {
        var edges = new ConcurrentBag<(ResourceGroupKey Start, ResourceGroupKey End)>();

        Parallel.ForEach(groupMap, pair =>
        {
            var root = pair.Key;
            var info = pair.Value;
            CollectEdgesForGroup(graph, rootLookup, vertexMap, edge => edges.Add(edge), root, info);
        });

        return edges.ToList();
    }

    /// <summary>
    /// Adds all intergroup edges that originate from vertices contained in a specific group.
    /// </summary>
    private static void CollectEdgesForGroup(
        DirectedGraph<IResourceKey> graph,
        IReadOnlyDictionary<IResourceKey, IResourceKey> rootLookup,
        IReadOnlyDictionary<IResourceKey, ResourceGroupKey> vertexMap,
        Action<(ResourceGroupKey Start, ResourceGroupKey End)> addEdge,
        IResourceKey root,
        GroupInfo info)
    {
        var start = vertexMap[root];
        foreach (var vertex in info.Vertices)
        {
            foreach (var dep in graph.GetAdjacencyOuterVertexesOf(vertex))
            {
                var depRoot = rootLookup[dep];
                if (EqualityComparer<IResourceKey>.Default.Equals(root, depRoot))
                    continue;

                addEdge((start, vertexMap[depRoot]));
            }
        }
    }

    /// <summary>
    /// Replaces graph contents with provided collapsed vertices and edges.
    /// </summary>
    private static void ReplaceGraphContents(
        DirectedGraph<IResourceKey> graph,
        IEnumerable<ResourceGroupKey> vertices,
        IEnumerable<(ResourceGroupKey Start, ResourceGroupKey End)> edges)
    {
        graph.Clear();

        foreach (var vertex in vertices)
        {
            graph.AddVertex(vertex);
        }

        foreach (var (start, end) in edges)
        {
            graph.TryAddEdge(start, end);
        }
    }

    /// <summary>
    /// Builds a lookup table from original resources to their owning collapsed group.
    /// </summary>
    private static Dictionary<IResourceKey, ResourceGroupKey> BuildGroupLookup(IEnumerable<ResourceGroupKey> groups)
    {
        var lookup = new Dictionary<IResourceKey, ResourceGroupKey>();
        foreach (var group in groups)
        {
            foreach (var member in group.Members)
            {
                lookup[member] = group;
            }
        }

        return lookup;
    }

    /// <summary>
    /// Repeatedly merges a node/group into its only dependency when exactly one dependency exists.
    /// </summary>
    private static void MergeSingleDependencyGroups(DirectedGraph<IResourceKey> graph, SetGroup<IResourceKey> uf)
    {
        bool changed;
        do
        {
            changed = false;
            foreach (var vertex in graph) // avoid ToArray allocation
            {
                var deps = graph.GetAdjacencyOuterVertexesOf(vertex);
                if (deps.Length != 1) continue;

                var onlyDep = deps[0];
                changed |= uf.TryMerge(vertex, onlyDep);
            }
        } while (changed);
    }

    /// <summary>
    /// Repeatedly merges groups that have identical dependency representative sets.
    /// </summary>
    private static void MergeSameDependencySetGroups(DirectedGraph<IResourceKey> graph, SetGroup<IResourceKey> uf)
    {
        bool changed;
        var pool = Pool<HashSet<IResourceKey>>.Default;
        do
        {
            changed = false;

            // representative -> merged dependency-set (all mapped to representative)
            var repDeps = new Dictionary<IResourceKey, HashSet<IResourceKey>>();

            foreach (var vertex in graph)
            {
                var rep = uf.FindRoot(vertex);
                if (!repDeps.TryGetValue(rep, out var depsOfRep))
                {
                    depsOfRep = pool.Spawn();
                    depsOfRep.Clear();
                    repDeps.Add(rep, depsOfRep);
                }

                var deps = graph.GetAdjacencyOuterVertexesOf(vertex);
                foreach (var dep in deps)
                {
                    var depRep = uf.FindRoot(dep);
                    if (!EqualityComparer<IResourceKey>.Default.Equals(rep, depRep))
                        depsOfRep.Add(depRep);
                }
            }

            var reps = repDeps.Keys.ToArray();
            for (var i = 0; i < reps.Length; i++)
            {
                var left = reps[i];
                var leftDeps = repDeps[left];
                if (leftDeps.Count == 0) continue;

                for (var j = i + 1; j < reps.Length; j++)
                {
                    var right = reps[j];
                    var rightDeps = repDeps[right];
                    if (rightDeps.Count == 0) continue;

                    if (leftDeps.SetEquals(rightDeps))
                        changed |= uf.TryMerge(left, right);
                }
            }

            foreach (var set in repDeps.Values)
            {
                set.Clear();
                pool.Recycle(set);
            }
        } while (changed);
    }

    /// <summary>
    /// Temporary grouping state built during collapse.
    /// </summary>
    private sealed class GroupInfo
    {
        /// <summary>
        /// Original graph vertices that belong to the represented union-find root.
        /// </summary>
        public List<IResourceKey> Vertices { get; } = [];

        /// <summary>
        /// Flattened list of original resource keys represented by this group.
        /// </summary>
        public List<IResourceKey> Members { get; } = [];

        /// <summary>
        /// Adds original members from a vertex, flattening nested grouped vertices when encountered.
        /// </summary>
        public void AddMembers(IResourceKey vertex)
        {
            if (vertex is ResourceGroupKey group)
            {
                Members.AddRange(group.Members);
                return;
            }

            Members.Add(vertex);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the vertices in the graph. After optimization, these will be group keys.
    /// </summary>
    public DirectedGraph<IResourceKey>.Enumerator GetEnumerator() => m_Graph.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator<IResourceKey> IEnumerable<IResourceKey>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Graph vertex key representing a collapsed group of original resources.
/// </summary>
/// <remarks>
/// Equality is intentionally reference-based so each instance represents a unique graph vertex identity.
/// </remarks>
public sealed class ResourceGroupKey : IResourceKey
{
    /// <summary>
    /// Initializes a group key from member resources while preserving insertion order and removing duplicates.
    /// </summary>
    public ResourceGroupKey(IEnumerable<IResourceKey> members)
    {
        var memberSet = new HashSet<IResourceKey>();
        var orderedMembers = new List<IResourceKey>();

        foreach (var member in members)
        {
            if (!memberSet.Add(member))
                continue;

            orderedMembers.Add(member);
        }

        Members = orderedMembers;
    }

    /// <summary>
    /// Original resource keys represented by this collapsed vertex.
    /// </summary>
    public IReadOnlyList<IResourceKey> Members { get; }

    /// <summary>
    /// Uses reference identity to compare group keys.
    /// </summary>
    public bool Equals(IResourceKey? other) => ReferenceEquals(this, other);

    /// <summary>
    /// Uses reference identity to compare group keys.
    /// </summary>
    public override bool Equals(object? obj) => obj is IResourceKey other && Equals(other);

    /// <summary>
    /// Returns a hash code derived from object identity.
    /// </summary>
    public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
}

/// <summary>
/// Provides access to the results of graph optimization, allowing lookup of group members by either original resource keys or group vertices.
/// </summary>
/// <param name="groupLookup">Lookup from original resource keys to their owning group vertex.</param>
public class ResourceGroups(Dictionary<IResourceKey, ResourceGroupKey> groupLookup) : IEnumerable<IReadOnlyList<IResourceKey>>
{
    /// <summary>
    /// Returns the members of the group represented by <paramref name="representative"/>.
    /// </summary>
    /// <param name="representative">Group vertex or original resource key.</param>
    /// <returns>All original members that belong to the resolved group.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when <paramref name="representative"/> cannot be resolved.</exception>
    public IReadOnlyList<IResourceKey> this[IResourceKey representative]
        => TryGetGroup(representative, out var members)
            ? members
            : throw new KeyNotFoundException($"Resource group for '{representative}' was not found.");

    /// <summary>
    /// Attempts to resolve the group members for a given representative key, which can be either an original resource or a group vertex.
    /// </summary>
    /// <param name="representative">Group vertex or original resource key.</param>
    /// <param name="members">When this method returns, contains the members of the resolved group if found; otherwise, null.</param>
    /// <returns><see langword="true"/> if the group was successfully resolved; otherwise, <see langword="false"/>.</returns>
    public bool TryGetGroup(IResourceKey representative, out IReadOnlyList<IResourceKey> members)
    {
        if (representative is ResourceGroupKey groupKey)
        {
            members = groupKey.Members;
            return true;
        }

        if (groupLookup.TryGetValue(representative, out var group))
        {
            members = group.Members;
            return true;
        }

        members = null!;
        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through the groups in this collection, exposing each group as a list of its original resource members.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(groupLookup.Values.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator<IReadOnlyList<IResourceKey>> IEnumerable<IReadOnlyList<IResourceKey>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerator that iterates through the groups in this collection,
    /// exposing each group as a list of its original resource members.
    /// </summary>
    public struct Enumerator(Dictionary<IResourceKey, ResourceGroupKey>.ValueCollection.Enumerator groupLookupValues) : IEnumerator<IReadOnlyList<IResourceKey>>
    {
        /// <summary>
        /// Enumerator over the group lookup values, which are the group vertices containing their member lists.
        /// This is used to yield the member lists directly during enumeration.
        /// </summary>
        private Dictionary<IResourceKey, ResourceGroupKey>.ValueCollection.Enumerator m_GroupLookupValues = groupLookupValues;

        /// <inheritdoc/>
        public IReadOnlyList<IResourceKey> Current => m_GroupLookupValues.Current.Members;

        /// <inheritdoc/>
        object IEnumerator.Current => Current;

        /// <inheritdoc/>
        public void Dispose() => m_GroupLookupValues.Dispose();

        /// <inheritdoc/>
        public bool MoveNext() => m_GroupLookupValues.MoveNext();

        /// <inheritdoc/>
        public void Reset() => ((IEnumerator) m_GroupLookupValues).Reset();
    }
}