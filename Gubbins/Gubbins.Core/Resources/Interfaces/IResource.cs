namespace Gubbins.Resource;

[Flags]
public enum ResourceState
{
    NotLoaded    = 0,
    Loading      = 3,
    Loaded       = 4,
    Recycling    = 12,
    Recycled     = 20,
    Releasing    = 36,
    Released     = 64,
    NonReference = 84
}

public interface IResource
{
    ResourceState State { get; }
}