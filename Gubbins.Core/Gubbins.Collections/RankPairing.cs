using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Gubbins.Collections;

public class RankPairing<T> : IPriorityQueue<T>
{
    private readonly IComparer<T> m_Comparer;
    private readonly LinkedList<Vertex> m_Roots = new();
    private readonly Dictionary<int, Vertex> m_RankBucket = new();
    private int m_Count;

    public RankPairing() => m_Comparer = Comparer<T>.Default;

    public RankPairing(IComparer<T> comparer) => m_Comparer = comparer;

    public RankPairing(IEnumerable<T> elements, IComparer<T> comparer) : this(comparer)
    {
        foreach (var element in elements) Append(element);
    }

    public int Count => m_Count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Peak() => m_Roots.First.Value.Data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(T item)
    {
        m_Count++;
        Append(new Vertex(item));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Append(Vertex vertex)
    {
        if (m_Roots.Count == 0 || CompareData(vertex, m_Roots.First.Value) < 0)
            m_Roots.AddFirst(vertex);
        else
            m_Roots.AddLast(vertex);
    }

    public T Extract()
    {
        var topNode = m_Roots.First;

        if (topNode == null)
            throw new ArgumentException("The heap is empty.");

        var result = topNode.Value.Data;

        // Disassembly half trees in top root.
        for (var vertex = topNode.Value.Left; vertex != null;)
        {
            // Make all right vertx to a new half tree.
            var rightVert = vertex.Right;
            vertex.Parent = vertex.Right = null;
            TryUnionSameRank(m_RankBucket, vertex);
            vertex = rightVert;
        }

        // OK, now we can reset the header.
        topNode = topNode.Next;
        m_Roots.RemoveFirst();

        // Reunion all root with same rank.
        for (var node = topNode; node != null;)
        {
            var nextNode = node.Next;
            var vertex = node.Value;
            m_Roots.Remove(node);
            TryUnionSameRank(m_RankBucket, vertex);
            node = nextNode;
            if (node != m_Roots.First) break;
        }

        // Flush to root link.
        foreach (var kv in m_RankBucket)
        {
            Append(kv.Value);
        }

        // Clear bucket.
        m_RankBucket.Clear();

        m_Count--;
        return result;
    }

    public void Decrease(T target, T newValue)
    {
        //TODO
        Debug.Assert(m_Comparer.Compare(newValue, target) < 0);
    }

    public bool Contains(T item)
    {
        //TODO
        throw new NotImplementedException();
    }

    public void Clear() => m_Roots.Clear();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TryUnionSameRank(Dictionary<int, Vertex> rankCache, Vertex vertex)
    {
        var rank = vertex.Rank;
        if (rankCache.TryGetValue(rank, out var cache))
        {
            var newRoot = Union(vertex, cache);
            Append(newRoot);
            rankCache.Remove(rank);
        }
        else
            rankCache[rank] = vertex;
    }

    private Vertex Union(Vertex leftRoot, Vertex rightRoot)
    {
        var (better, worse) = CompareData(leftRoot, rightRoot) < 0 ? (leftRoot, rightRoot) : (rightRoot, leftRoot);
        worse.Parent = better;
        worse.Right = better.Left;
        better.Left = worse;
        better.Rank = worse.Rank + 1;
        return better;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CompareData(Vertex left, Vertex right) => m_Comparer.Compare(left.Data, right.Data);

    private class Vertex(T data) : IEquatable<Vertex>, IEqualityComparer<Vertex>
    {
        public readonly T       Data = data;
        public          Vertex? Parent;
        public          Vertex? Left;
        public          Vertex? Right;
        public          int     Rank;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vertex other) => EqualityComparer<T>.Equals(this, other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vertex x, Vertex y) => EqualityComparer<T>.Equals(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(Vertex obj) => obj.Data?.GetHashCode() ?? 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => Data?.GetHashCode() ?? 0;
    }
}