namespace Gubbins.Unmanaged;

/// <summary>
/// Provides a cached disposal delegate for unmanaged value types that also implement <see cref="IDisposable"/>.
/// </summary>
/// <typeparam name="T">The unmanaged target type.</typeparam>
internal static class Disposer<T> where T : unmanaged
{
    /// <summary>
    /// Represents a disposal function for <typeparamref name="T"/>.
    /// </summary>
    /// <param name="target">The target value to dispose.</param>
    internal delegate void DisposeFunction(ref T target);

    /// <summary>
    /// Gets the cached disposal delegate for <typeparamref name="T"/>, or a no-op when not disposable.
    /// </summary>
    internal static readonly DisposeFunction Dispose;

    /// <summary>
    /// Gets a value indicating whether <typeparamref name="T"/> supports disposal through <see cref="Dispose"/>.
    /// </summary>
    internal static readonly bool IsDisposable;

    /// <summary>
    /// Initializes cached disposal metadata and delegate bindings for <typeparamref name="T"/>.
    /// </summary>
    static Disposer()
    {
        if (!typeof(T).IsAssignableFrom(typeof(IDisposable)))
        {
            IsDisposable = false;
            Dispose      = DoNothing;
            return;
        }

        var method = typeof(T).GetMethod(nameof(IDisposable.Dispose), Type.EmptyTypes);
        if (method is null)
        {
            IsDisposable = false;
            Dispose      = DoNothing;
            return;
        }

        var delegation = method.CreateDelegate(typeof(DisposeFunction)) as DisposeFunction;
        if (delegation is null)
        {
            IsDisposable = false;
            Dispose      = DoNothing;
            return;
        }

        Dispose      = delegation;
        IsDisposable = true;
    }

    /// <summary>
    /// No-op disposal fallback used when <typeparamref name="T"/> does not expose a compatible dispose method.
    /// </summary>
    /// <param name="target">The target value.</param>
    private static void DoNothing(ref T target) { }
}