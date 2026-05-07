using System.Collections.Concurrent;

namespace Gubbins.Enhance;

/// <summary>
/// Proxy for duck.
/// </summary>
/// <remarks>
/// This interface should be implemented by source generator.
/// </remarks>
public interface IProxy : IResetable
{
    /// <summary>
    /// Initialize the proxy.
    /// </summary>
    /// <param name="proxy">The object may be the source of duck interface.</param>
    /// <returns>Whether the object can have a duck proxy.</returns>
    bool TryInit(object proxy);
}

/// <summary>
/// Create a duck proxy for the given interface type. So that you can use <see cref="Duck.Like{T}(object, out T)"/> for type checking.
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Delegate)]
public class DuckAttribute : Attribute;

/// <summary>
/// Duck type helper.
/// </summary>
/// <remarks>
/// "Duck type" can refer to duck typing, a concept in computer programming where an object's suitability for a purpose is determined
/// by the presence of certain methods and properties, rather than its specific class or inheritance, as in "If it walks like a duck
/// and quacks like a duck, it's a duck".
/// </remarks>
public static class Duck
{
    private static readonly ConcurrentDictionary<Type, object> s_DuckCache = new();

    /// <summary>
    /// Check whether <c>obj</c> can have a duck proxy for the given interface type that has <see cref="DuckAttribute"/>.
    /// </summary>
    /// <param name="obj">Object to check for duck proxy.</param>
    /// <param name="result">Resulting duck proxy if available.</param>
    /// <typeparam name="T">Interface or delegate type that has <see cref="DuckAttribute"/>.</typeparam>
    /// <returns>Whether the object can have a duck proxy.</returns>
    public static bool Like<T>(object obj, out T result) where T : class => Like(obj, out result, out _);

    /// <summary>
    /// Check whether <c>obj</c> can have a duck proxy for the given interface type that has <see cref="DuckAttribute"/>.
    /// </summary>
    /// <param name="obj">Object to check for duck proxy.</param>
    /// <param name="result">Resulting duck proxy if available.</param>
    /// <param name="handle">Handle for the pooled duck proxy. You can dispose it when you don't need to use the proxy(<c>result</c>), so that reduces redundant managed memory allocation.</param>
    /// <typeparam name="T">Interface or delegate type that has <see cref="DuckAttribute"/>.</typeparam>
    /// <returns>Whether the object can have a duck proxy.</returns>
    public static bool Like<T>(object obj, out T result, out PooledHandle handle) where T : class
    {
        result = null!;
        if (obj == null!)
        {
            handle = default;
            return false;
        }

        // If the object is already a duck, return it directly.
        if (obj is T target)
        {
            handle = default;
            result = target;
            return true;
        }

        if (!s_DuckCache.TryGetValue(typeof(T), out var duck))
        {
            // Get type from generated code.
            var generatedDuckTypeName = $"Gubbins.Generated.Duck_{typeof(T).FullName.Replace(".", "_").Replace(".", "_").Replace("+", "_").Replace("`", "_")}";
            var duckType = Type.GetType(generatedDuckTypeName, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)!;
            if (duckType == null!)
            {
                handle = default;
                return false;
            }

            duck = Activator.CreateInstance(duckType);
            s_DuckCache[typeof(T)] = duck;
        }

        if (duck == null)
        {
            handle = default;
            return false;
        }

        return ((Duck<T>) duck).Like(obj, out result, out handle);
    }
}

/// <summary>
/// Duck interface checker.
/// </summary>
/// <remarks>
/// This type should be implemented by source generator.
/// </remarks>
public abstract class Duck<T>
{
    /// <summary>
    /// Check whether the type can have a duck proxy for the given interface type.
    /// </summary>
    protected ConcurrentDictionary<Type, Dictionary<Type, ISpawner<IProxy>>> MatchedCache = new();

    /// <summary>
    /// Check whether the type can't have a duck proxy for the given interface type.
    /// </summary>
    protected ConcurrentDictionary<Type, HashSet<Type>> UnmatchedCache = new();

    /// <summary>
    /// Check whether <c>obj</c> can have a duck proxy for the given interface type that has <see cref="DuckAttribute"/>.
    /// </summary>
    /// <param name="obj">Object to check for duck proxy.</param>
    /// <param name="result">Resulting duck proxy if available.</param>
    /// <returns>Whether the object can have a duck proxy.</returns>
    public abstract bool Like(object obj, out T result);

    /// <summary>
    /// Check whether <c>obj</c> can have a duck proxy for the given interface type that has <see cref="DuckAttribute"/>.
    /// </summary>
    /// <param name="obj">Object to check for duck proxy.</param>
    /// <param name="result">Resulting duck proxy if available.</param>
    /// <param name="handle">Handle for the pooled duck proxy. You can dispose it when you don't need to use the proxy(<c>result</c>), so that reduces redundant managed memory allocation.</param>
    /// <returns>Whether the object can have a duck proxy.</returns>
    public abstract bool Like(object obj, out T result, out PooledHandle handle);
}