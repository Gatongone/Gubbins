namespace Gubbins.Resource;

/// <summary>
/// Interface for loading and unloading resources with a specified key.
/// </summary>
/// <typeparam name="TKey">The type of the resource key</typeparam>
public interface IResourceLoader<in TKey> where TKey : struct, IResourceKey
{
    /// <summary>
    /// Load the resource with the specified key, it will automatically load the resource and add it to the resource manager,
    /// </summary>
    /// <param name="key">The key of the resource to load, it must be unique and can be used to identify the resource.</param>
    /// <param name="progress">progress reporter that can be used to report the progress of the loading operation, it will report a value between 0 and 1.</param>
    /// <typeparam name="TResource">The type of the resource, it can be any type that represents a resource.</typeparam>
    /// <returns>The loaded resource, it will be automatically released when the resource manager is disposed.</returns>
    TResource Load<TResource>(TKey key, IProgress<float> progress);

    /// <summary>
    /// Asynchronously load the resource with the specified key, it will automatically load the resource and add it to the resource manager,
    /// </summary>
    /// <param name="key">The key of the resource to load, it must be unique and can be used to identify the resource.</param>
    /// <param name="progress">A progress reporter that can be used to report the progress of the loading operation, it will report a value between 0 and 1.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the loading operation.</param>
    /// <typeparam name="TResource">The type of the resource, it can be any type that represents a resource.</typeparam>
    /// <returns></returns>
    Task<TResource> LoadAsync<TResource>(TKey key, IProgress<float> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Unload the resource with the specified key, it will automatically release the resource and remove it from the resource manager,
    /// </summary>
    /// <param name="key">The key of the resource to unload, it must be the same as the key used to load the resource.</param>
    /// <param name="resource"> The resource to unload, it must be the same as the resource returned by the load operation, it will be automatically released when the unloading operation is completed.</param>
    /// <param name="progress">Progress reporter that can be used to report the progress of the unloading operation, it will report a value between 0 and 1.</param>
    /// <typeparam name="TResource">The type of the resource, it can be any type that represents a resource.</typeparam>
    void Unload<TResource>(TKey key, TResource resource, IProgress<float> progress);

    /// <summary>
    /// Asynchronously unload the resource with the specified key, it will automatically release the resource and remove it from the resource manager,
    /// </summary>
    /// <param name="key">The key of the resource to unload, it must be the same as the key used to load the resource.</param>
    /// <param name="resource">The resource to unload, it must be the same as the resource returned by the load operation, it will be automatically released when the unloading operation is completed.</param>
    /// <param name="progress">Progress reporter that can be used to report the progress of the unloading operation, it will report a value between 0 and 1.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the unloading operation.</param>
    /// <typeparam name="TResource">The type of the resource, it can be any type that represents a resource.</typeparam>
    /// <returns>A task that represents the asynchronous unload operation.</returns>
    Task UnloadAsync<TResource>(TKey key, TResource resource, IProgress<float> progress, CancellationToken cancellationToken);
}