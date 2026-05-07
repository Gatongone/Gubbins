namespace Gubbins.Resource;

public abstract class ReferenceResource<TResource> : IResource, IReleasable
{
    public ResourceState State { get;private set; }
    
    /// <summary>
    /// The resources instance, Never set the value but the ReferenceResources instance as a member field
    /// </summary>
    public TResource Value { get; }

    protected ReferenceResource(TResource resource)
    {
        Value = resource;
    }

    ~ReferenceResource()
    {
        if (Value is not IRecyclable recycler) return;
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