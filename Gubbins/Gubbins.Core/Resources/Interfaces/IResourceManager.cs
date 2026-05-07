namespace Gubbins.Resource;

public interface IResourceManager
{
    TResource Load<TKey, TResource>(TKey key) where TKey : struct, IResourceKey;
    Task<TResource> LoadAsync<TKey, TResource>(TKey key) where TKey : struct, IResourceKey;
}