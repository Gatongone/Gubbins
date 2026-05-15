using System.Collections.Concurrent;

namespace Gubbins.Enhance;

/// <summary>
/// Defines a contract for creating new instances of objects.
/// </summary>
public interface INewable
{
    /// <summary>
    /// Creates a new instance of an object.
    /// </summary>
    /// <returns>A new instance of the object.</returns>
    object New();
}

/// <summary>
/// Provides functionality for creating instances of types and managing factory registrations.
/// </summary>
public static class Newable
{
    private static readonly ConcurrentDictionary<Type?, INewable> s_InstanceCache = new();

    /// <summary>
    /// Registers a factory for creating instances of the specified type.
    /// </summary>
    /// <typeparam name="T">The type for which to register the factory.</typeparam>
    /// <param name="factory">The factory instance that will be used to create instances of type T.</param>
    public static void RegisterFactory<T>(INewable factory) => s_InstanceCache[typeof(T)] = factory;

    /// <summary>
    /// Determines whether the specified type can be instantiated and provides an appropriate factory.
    /// </summary>
    /// <param name="type">The type to check for instantiation capability.</param>
    /// <param name="newer">When this method returns, contains the factory that can create instances of the type, or null if the type cannot be instantiated.</param>
    /// <returns>true if the type can be instantiated; otherwise, false.</returns>
    public static bool IsNewable(this Type type, out INewable? newer)
    {
        if (s_InstanceCache.TryGetValue(type, out newer)) return newer == null!;

        if (!type.IsValueType && type.GetConstructor([]) is null)
        {
            newer = null!;
            s_InstanceCache.TryAdd(type, newer);
            return false;
        }

        newer = new Newer(type);
        return s_InstanceCache.TryAdd(type, newer);
    }

    /// <summary>
    /// A factory implementation that creates instances using Activator.CreateInstance.
    /// </summary>
    /// <param name="type">The type to create instances of.</param>
    private class Newer(Type type) : INewable
    {
        /// <summary>
        /// Creates a new instance of the specified type using Activator.CreateInstance.
        /// </summary>
        /// <returns>A new instance of the type.</returns>
        public object New() => Activator.CreateInstance(type);
    }

    /// <summary>
    /// A factory implementation that creates instances using a delegate function.
    /// </summary>
    /// <typeparam name="T">The type of object created by the delegate.</typeparam>
    /// <param name="function">The function delegate used to create instances.</param>
    private class DelegateNewer<T>(Func<T> function) : INewable
    {
#pragma warning disable CS8603 // Possible null reference return.
        /// <summary>
        /// Creates a new instance by invoking the delegate function.
        /// </summary>
        /// <returns>A new instance created by the delegate function.</returns>
        public object New() => function();
#pragma warning restore CS8603 // Possible null reference return.
    }
}