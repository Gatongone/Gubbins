using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Gubbins.Enhance;

/// <summary>
/// An implementation for Service Locator Pattern.
/// </summary>
/// <typeparam name="TTheme">The type of the theme.</typeparam>
public sealed class Locator<TTheme> : ILocator<TTheme>, IEnumerable<object>, IDisposable
{
    /// <summary>
    /// Located instance cache.
    /// </summary>
    private readonly Dictionary<TTheme, object> m_Cache = new();

    /// <summary>
    /// Whether the <see cref="Dispose()"/> invoked
    /// </summary>
    private bool m_HasDisposed;

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~Locator()
    {
        if (!m_HasDisposed) Dispose();
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TTheme theme, object instance) => m_Cache[theme] = instance ?? throw new ArgumentException(nameof(instance));

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(TTheme theme) => m_Cache.Remove(theme);

    /// <inheritdoc/>
    public bool Remove(object instance)
    {
        var key = default(TTheme);
        foreach (var kv in m_Cache)
        {
            if (kv.Value != instance) continue;
            key = kv.Key;
            break;
        }

        return key != null && m_Cache.Remove(key);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(TTheme theme) => m_Cache.ContainsKey(theme);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(object instance) => m_Cache.ContainsValue(instance);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetInstance(TTheme theme, out object? instance) => m_Cache.TryGetValue(theme, out instance);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetTheme(object instance, out TTheme? theme)
    {
        foreach (var kv in m_Cache)
        {
            if (kv.Value != instance) continue;
            theme = kv.Key;
            return true;
        }

        theme = default;
        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => m_Cache.Clear();

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T? Locate<T>(TTheme theme) where T : class
    {
        if (m_Cache.TryGetValue(theme, out var instance)) return instance as T;
        if (typeof(TTheme).IsNewable(out var newer))
        {
            instance = newer.New();
            m_Cache.Add(theme, instance);
        }
        else if (typeof(T).IsAbstract || typeof(T).IsSealed)
        {
            return null;
        }

        instance = FormatterServices.GetUninitializedObject(typeof(T));
        m_Cache.Add(theme, instance);
        return instance as T;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var item in m_Cache.Values)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        m_HasDisposed = true;
    }

    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(m_Cache.Values.GetEnumerator());

    /// <inheritdoc/>
    IEnumerator<object> IEnumerable<object>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enumerator for <see cref="Locator{TTheme}"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<object>
    {
        /// <summary>
        /// The enumerator of the located instance collection.
        /// </summary>
        private Dictionary<TTheme, object>.ValueCollection.Enumerator m_Enumerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumerator"/> struct.
        /// </summary>
        /// <param name="enumerator">The enumerator of the located instance collection.</param>
        internal Enumerator(Dictionary<TTheme, object>.ValueCollection.Enumerator enumerator) => m_Enumerator = enumerator;

        /// <inheritdoc/>
        public void Dispose() => m_Enumerator.Dispose();

        /// <inheritdoc/>
        public bool MoveNext() => m_Enumerator.MoveNext();

        /// <inheritdoc/>
        public void Reset() { }

        /// <inheritdoc/>
        public object Current => m_Enumerator.Current!;

        object IEnumerator.Current => Current;
    }
}