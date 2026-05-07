using System.Collections.Concurrent;
using System.Reflection;

namespace Gubbins.Enhance;

public interface IResetable
{
    void Reset();
}

internal static class ResetableExtensions
{
    private static readonly ConcurrentQueue<MonoResetable>         s_Cache       = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> s_MethodCache = new();

    public static bool CanBeResetable(this object obj, out IResetable resetable)
    {
        if (!s_Cache.TryDequeue(out var cache))
        {
            cache = new MonoResetable();
        }

        if (obj is IResetable validInstance)
        {
            resetable = validInstance;
            return true;
        }

        var type = obj.GetType();
        if (!s_MethodCache.TryGetValue(type, out var method))
        {
            method ??= type.GetMethod("Reset", []);
            if (method == null || method.IsStatic)
            {
                resetable = null!;
                return false;
            }
        }

        s_Cache.Enqueue(cache);
        s_MethodCache[obj.GetType()] = method;
        cache.Provider               = obj;
        cache.Method                 = method;
        resetable                    = cache;
        return true;
    }

    private sealed class MonoResetable : IResetable
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal object     Provider;
        internal MethodInfo Method;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public void Reset()
        {
            Method.Invoke(Provider, []);
            Method   = null!;
            Provider = null!;
        }
    }
}