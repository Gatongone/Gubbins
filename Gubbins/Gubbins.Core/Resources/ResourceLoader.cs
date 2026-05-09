using Gubbins.Resource;

namespace Gubbins.Resources;

public class ResourceLoader<TKey>(IResourceProviderFactory providerFactory) : IResourceLoader<TKey> where TKey : struct, IResourceKey
{
    public TResource Load<TResource>(TKey key, IProgress<float> progress)
    {
        var provider = providerFactory.GetProvider<TKey, TResource>(key);
        return provider.Load(key, progress);
    }

    public Task<TResource> LoadAsync<TResource>(TKey key, IProgress<float> progress, CancellationToken cancellationToken)
    {
        var provider = providerFactory.GetProvider<TKey, TResource>(key);
        return provider.LoadAsync(key, progress, cancellationToken);
    }

    public void Unload<TResource>(TKey key, TResource resource, IProgress<float> progress)
    {
        var provider = providerFactory.GetProvider<TKey, TResource>(key);
        provider.Unload(key, resource, progress);
    }

    public Task UnloadAsync<TResource>(TKey key, TResource resource, IProgress<float> progress, CancellationToken cancellationToken)
    {
        var provider = providerFactory.GetProvider<TKey, TResource>(key);
        return provider.UnloadAsync(key, resource, progress, cancellationToken);
    }
}