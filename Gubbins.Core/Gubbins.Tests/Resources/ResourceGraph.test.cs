using Gubbins.Resource;

namespace Gubbins.Resources.Tests;

[TestFixture]
public class ResourceGraphTests
{
    [Test]
    public void GetGroup_WithUnknownKey_ThrowsKeyNotFoundException()
    {
        var graph = new ResourceGraph();
        var a = new TestResourceKey("A");
        var unknown = new TestResourceKey("Unknown");

        graph.AddResource(a);
        var groups = graph.Optimize();

        Assert.Throws<KeyNotFoundException>(() => _ = groups[unknown]);
    }

    [Test]
    public void Optimize_SingleDependencyChain_CollapsesIntoSingleGroup()
    {
        var graph = new ResourceGraph();
        var a = new TestResourceKey("A");
        var b = new TestResourceKey("B");

        graph.AddDependency(a, b);
        var groups = graph.Optimize();

        Assert.Multiple(() =>
        {
            Assert.That(groups[a], Does.Contain(a));
            Assert.That(groups[b], Does.Contain(b));
            var allMembers = graph.Graph.SelectMany(GetGroupMembers);
            Assert.That(allMembers.All(member => member is TestResourceKey), Is.True);
        });
    }

    [Test]
    public void Optimize_SameDependencySet_ProducesQueryableGroups()
    {
        var graph = new ResourceGraph();
        var a = new TestResourceKey("A");
        var b = new TestResourceKey("B");
        var c = new TestResourceKey("C");
        var d = new TestResourceKey("D");

        graph.AddDependency(a, c);
        graph.AddDependency(a, d);
        graph.AddDependency(b, c);
        graph.AddDependency(b, d);
        var groups = graph.Optimize();

        Assert.Multiple(() =>
        {
            Assert.That(groups[a], Does.Contain(a));
            Assert.That(groups[b], Does.Contain(b));
            Assert.That(groups[c], Does.Contain(c));
            Assert.That(groups[d], Does.Contain(d));
        });
    }

    [Test]
    public void Optimize_CalledTwice_KeepsGroupsFlat()
    {
        var graph = new ResourceGraph();
        var a = new TestResourceKey("A");
        var b = new TestResourceKey("B");
        var c = new TestResourceKey("C");
        var d = new TestResourceKey("D");
        var e = new TestResourceKey("E");

        graph.AddDependency(a, c);
        graph.AddDependency(a, d);
        graph.AddDependency(b, c);
        graph.AddDependency(b, d);
        graph.AddResource(e);

        graph.Optimize();
        var groups2 = graph.Optimize();

        Assert.Multiple(() =>
        {
            Assert.That(groups2[a], Does.Contain(a));
            Assert.That(groups2[b], Does.Contain(b));
            Assert.That(groups2[c], Does.Contain(c));
            Assert.That(groups2[d], Does.Contain(d));
            Assert.That(groups2[e], Does.Contain(e));
            var allMembers = graph.Graph.SelectMany(GetGroupMembers);
            Assert.That(allMembers.All(member => member is TestResourceKey), Is.True);
        });
    }

    [Test]
    public void Optimize_WithParallelEnabled_ProducesSameResultAsSequential()
    {
        var sequential = CreateLargeGraph();
        var parallel = CreateLargeGraph();

        sequential.Optimize();
        parallel.Optimize(parallel: true);

        var sequentialGroups = SnapshotGroups(sequential);
        var parallelGroups = SnapshotGroups(parallel);
        var sequentialEdges = SnapshotEdges(sequential);
        var parallelEdges = SnapshotEdges(parallel);

        Assert.Multiple(() =>
        {
            CollectionAssert.AreEquivalent(sequentialGroups, parallelGroups);
            CollectionAssert.AreEquivalent(sequentialEdges, parallelEdges);
        });
    }

    private static IEnumerable<IResourceKey> GetGroupMembers(IResourceKey key)
    {
        return key is ResourceGroupKey groupKey ? groupKey.Members : new[] {key};
    }

    private static ResourceGraph CreateLargeGraph()
    {
        var graph = new ResourceGraph();
        for (var i = 0; i < 80; i++)
        {
            var root = new TestResourceKey($"R{i}");
            var depA = new TestResourceKey($"A{i}");
            var depB = new TestResourceKey($"B{i}");

            graph.AddDependency(root, depA);
            graph.AddDependency(root, depB);
        }

        return graph;
    }

    private static string[] SnapshotGroups(ResourceGraph graph)
    {
        return graph.Graph
            .Select(key =>
            {
                if (key is ResourceGroupKey groupKey)
                {
                    return groupKey.Members;
                }

                return new List<IResourceKey> {key};
            })
            .Select(group => string.Join("|", group.Select(member => member.ToString()).OrderBy(value => value)))
            .OrderBy(value => value)
            .ToArray();
    }

    private static string[] SnapshotEdges(ResourceGraph graph)
    {
        return graph.Graph
            .SelectMany(start => graph.Graph.GetAdjacencyOuterVertexesOf(start),
                (start, end) =>
                {
                    var startGroup = start is ResourceGroupKey startKey ? startKey.Members : new List<IResourceKey> {start};
                    var endGroup = end is ResourceGroupKey endKey ? endKey.Members : new List<IResourceKey> {end};
                    return $"{string.Join("|", startGroup.Select(member => member.ToString()).OrderBy(value => value))}" +
                           $"->{string.Join("|", endGroup.Select(member => member.ToString()).OrderBy(value => value))}";
                })
            .OrderBy(value => value)
            .ToArray();
    }

    private sealed class TestResourceKey(string id) : IResourceKey
    {
        private string Id { get; } = id;

        public bool Equals(IResourceKey? other) => other is TestResourceKey key && Id == key.Id;

        public override bool Equals(object? obj) => obj is IResourceKey other && Equals(other);

        public override int GetHashCode() => Id.GetHashCode();

        public override string ToString() => Id;
    }
}

