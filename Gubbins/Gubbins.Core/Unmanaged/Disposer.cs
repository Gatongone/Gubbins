namespace Gubbins.Unmanaged;

public static class Disposer<T> where T : unmanaged
{
    public delegate void DisposeFunction(ref T target);

    public static readonly DisposeFunction Dispose;
    public static readonly bool            IsDisposable;

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

    private static void DoNothing(ref T target) { }
}