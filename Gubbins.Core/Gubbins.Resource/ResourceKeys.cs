using Gubbins.Resource;

namespace Gubbins.Resource;

public readonly struct Path(string path) : IResourceKey, IEquatable<Path>
{
    public bool Equals(IResourceKey? other) => other is Path otherPath && Equals(otherPath);

    public bool Equals(Path other) => path == other.ToString();

    public override bool Equals(object? obj) => obj is Path other && Equals(other);

    public override int GetHashCode() => path.GetHashCode();

    public override string ToString() => path;
}

public readonly struct Variant(params IResourceKey[] entries) : IResourceKey, IEquatable<Variant>
{
    private readonly HashSet<IResourceKey> m_Entries = [..entries];

    public bool Equals(IResourceKey other)
    {
        if (m_Entries.Contains(other))
            return true;
        if (other is Variant otherVariant)
            return Equals(otherVariant);
        return false;
    }

    public bool Equals(Variant other) => m_Entries.SetEquals(other.m_Entries);

    public override bool Equals(object? obj) => obj is Variant other && Equals(other);

    public override int GetHashCode() => m_Entries.Aggregate(0, static (hash, tag) => hash ^ tag.GetHashCode());

    public override string ToString() => $"({string.Join(", ", m_Entries)})";
}