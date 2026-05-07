namespace Gubbins.Resource;

public abstract class CounterResource<TResource> : IResource, IReleasable
{
    private readonly TResource m_Resource;
    private int m_Count;
    public ResourceState State { get; private set; }

    protected CounterResource(TResource resource)
    {
        State = ResourceState.Loaded;
        m_Resource = resource;
    }

    public TResource Increment()
    {
        Interlocked.Increment(ref m_Count);
        State = ResourceState.Loaded;
        return m_Resource;
    }

    public void Decrement()
    {
        Interlocked.Decrement(ref m_Count);
        if (m_Count != 0 || m_Resource is not IRecyclable recycler) return;
        State = ResourceState.Recycling;
        recycler.Recycle();
        State = ResourceState.Recycled;
    }

    protected abstract void Release();

    void IReleasable.Release()
    {
        State = ResourceState.Releasing;
        Release();
        State = ResourceState.Released;
    }
}