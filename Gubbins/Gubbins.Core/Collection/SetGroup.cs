namespace Gubbins.Collection;

/// <summary>
/// Union-Find set.
/// </summary>
public class SetGroup : ISetGroup
{
    // The set(tree)'s root index.
    // m_RootIndexes[i] represents the parent node pointed to by the i-th element
    private readonly int[] m_RootIndexes;

    // Used to record the layer of set.
    // m_LayerRank represents the number of layers of the tree represented by the set with the i-th element as the root
    private readonly int[] m_LayerRank;

    public SetGroup(int size)
    {
        m_RootIndexes = new int[size];
        m_LayerRank = new int[size];

        for (var i = 0; i < size; i++)
        {
            // Init every set's root index to itself.
            m_RootIndexes[i] = i;
            // The initialization layer is 1.
            m_LayerRank[i] = 1;
        }
    }

    /// <summary>
    /// Get the root element index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private int FindRoot(int index)
    {
        if (index < 0 || index >= m_RootIndexes.Length)
            throw new ArgumentOutOfRangeException($"Index is out of bound. Index:{index}");

        // When the index is not equals to the element,
        // means that the element is not the root.
        while (index != m_RootIndexes[index])
        {
            // Path compression.
            m_RootIndexes[index] = m_RootIndexes[m_RootIndexes[index]];

            // Move to next element.
            index = m_RootIndexes[index];
        }

        return index;
    }

    /// <summary>
    /// Are the sets in same group.
    /// </summary>
    /// <param name="left">Left set index.</param>
    /// <param name="right">Right set index.</param>
    /// <returns>Are the sets in same group.</returns>
    public bool InSameGroup(int left, int right) => FindRoot(left) == FindRoot(right);

    /// <summary>
    /// Merge sets to a group.
    /// </summary>
    /// <param name="left">Left set index.</param>
    /// <param name="right">Right set index.</param>
    /// <returns>Are the sets not in the same group</returns>
    public bool TryMerge(int left, int right)
    {
        var leftRoot = FindRoot(left);
        var rightRoot = FindRoot(right);

        // In the same set.
        if (leftRoot == rightRoot) return false;

        // Merge sets. In order to reduce the layer,
        // the set with larger layer should be merged to the set with a smaller layer.
        if (m_LayerRank[leftRoot] < m_LayerRank[rightRoot])
        {
            m_RootIndexes[leftRoot] = rightRoot;
        }
        else if (m_LayerRank[rightRoot] < m_LayerRank[leftRoot])
        {
            m_RootIndexes[rightRoot] = leftRoot;
        }
        else
        {
            m_RootIndexes[leftRoot] = rightRoot;
            m_LayerRank[rightRoot]++;
        }

        return true;
    }
}

/// <summary>
/// Union-Find set for arbitrary element keys.
/// </summary>
/// <typeparam name="T">Element type.</typeparam>
public class SetGroup<T> : ISetGroup<T>
{
    private readonly Dictionary<T, T> m_Parent = new();
    private readonly Dictionary<T, int> m_Rank = new();

    /// <summary>
    /// Initializes the disjoint-set with each element in its own group.
    /// </summary>
    /// <param name="elements">Initial element set.</param>
    public SetGroup(IEnumerable<T> elements)
    {
        foreach (var element in elements)
        {
            m_Parent.Add(element, element);
            m_Rank.Add(element, 0);
        }
    }

    /// <summary>
    /// Finds and returns the root representative of the specified element.
    /// </summary>
    /// <param name="element">Element to query.</param>
    /// <returns>The representative element of the set.</returns>
    public T FindRoot(T element)
    {
        var root = element;
        while (!EqualityComparer<T>.Default.Equals(m_Parent[root], root))
        {
            var last = root;
            root = m_Parent[root];
            m_Parent[last] = root;
        }

        return root;
    }

    /// <summary>
    /// Determines whether two elements are in the same group.
    /// </summary>
    public bool InSameGroup(T x, T y) => EqualityComparer<T>.Default.Equals(FindRoot(x), FindRoot(y));

    /// <summary>
    /// Merges two groups if they are different.
    /// </summary>
    /// <returns><see langword="true"/> if merged; otherwise <see langword="false"/>.</returns>
    public bool TryMerge(T left, T right)
    {
        var leftRoot = FindRoot(left);
        var rightRoot = FindRoot(right);

        // In the same set.
        if (EqualityComparer<T>.Default.Equals(leftRoot, rightRoot)) return false;

        // Merge sets. In order to reduce the layer,
        // the set with larger layer should be merged to the set with a smaller layer.
        if (m_Rank[leftRoot] < m_Rank[rightRoot])
        {
            m_Parent[leftRoot] = rightRoot;
        }
        else if (m_Rank[rightRoot] < m_Rank[leftRoot])
        {
            m_Parent[rightRoot] = leftRoot;
        }
        else
        {
            m_Parent[rightRoot] = leftRoot;
            m_Rank[leftRoot]++;
        }

        return true;
    }
}