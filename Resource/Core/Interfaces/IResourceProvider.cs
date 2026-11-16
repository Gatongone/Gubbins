namespace Gubbins.Resource;

/// <summary>
/// Provides synchronous and asynchronous resource loading and unloading operations for a resource key type.
/// </summary>
/// <typeparam name="TKey">The strongly typed key that identifies a resource.</typeparam>
/// <typeparam name="TResource">The resource instance type produced by the provider.</typeparam>
public interface IResourceProvider<in TKey, TResource> where TKey : struct, IResourceKey
{
    /// <summary>
    /// Unloads a previously loaded resource instance.
    /// </summary>
    /// <param name="key">The key associated with the resource.</param>
    /// <param name="resource">The resource instance to unload.</param>
    /// <param name="progress">Reports unload progress from 0 to 1.</param>
    void Unload(TKey key, TResource resource, IProgress<float> progress);

    /// <summary>
    /// Asynchronously unloads a previously loaded resource instance.
    /// </summary>
    /// <param name="key">The key associated with the resource.</param>
    /// <param name="resource">The resource instance to unload.</param>
    /// <param name="progress">Reports unload progress from 0 to 1.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that completes when unload finishes.</returns>
    Task UnloadAsync(TKey key, TResource resource, IProgress<float> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Loads a resource for the given key.
    /// </summary>
    /// <param name="key">The key that identifies the resource to load.</param>
    /// <param name="progress">Reports load progress from 0 to 1.</param>
    /// <returns>The loaded resource instance.</returns>
    TResource Load(TKey key, IProgress<float> progress);

    /// <summary>
    /// Asynchronously loads a resource for the given key.
    /// </summary>
    /// <param name="key">The key that identifies the resource to load.</param>
    /// <param name="progress">Reports load progress from 0 to 1.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task that resolves to the loaded resource instance.</returns>
    Task<TResource> LoadAsync(TKey key, IProgress<float> progress, CancellationToken cancellationToken);
}

/// <summary>
/// Resolves resource providers for specific key and resource type combinations.
/// </summary>
public interface IResourceProviderFactory
{
    /// <summary>
    /// Gets a provider capable of handling the specified key and resource type.
    /// </summary>
    /// <typeparam name="TKey">The strongly typed resource key.</typeparam>
    /// <typeparam name="TResource">The resource instance type.</typeparam>
    /// <param name="key">A key value used to select the provider implementation.</param>
    /// <returns>A matching resource provider.</returns>
    IResourceProvider<TKey, TResource> GetProvider<TKey, TResource>(TKey key) where TKey : struct, IResourceKey;
}