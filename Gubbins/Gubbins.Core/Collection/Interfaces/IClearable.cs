using System.Collections.Concurrent;
using System.Reflection;

namespace Gubbins.Collection;

public interface IClearable
{
    void Clear();
}

internal static class ClearableExtensions
{
    private static readonly ConcurrentQueue<MonoClearable>         s_Cache       = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> s_MethodCache = new();

    public static bool CanBeClearable(this object obj, out IClearable clearable)
    {
        if (!s_Cache.TryDequeue(out var cache))
        {
            cache = new MonoClearable();
        }

        if (obj is IClearable validInstance)
        {
            clearable = validInstance;
            return true;
        }

        var type = obj.GetType();
        if (!s_MethodCache.TryGetValue(type, out var method))
        {
            method ??= type.GetMethod("Clear", []);
            if (method == null || method.IsStatic)
            {
                clearable = null!;
                return false;
            }
        }

        s_Cache.Enqueue(cache);
        s_MethodCache[obj.GetType()] = method;
        cache.Provider               = obj;
        cache.Method                 = method;
        clearable                    = cache;
        return true;
    }

    private sealed class MonoClearable : IClearable
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        internal object     Provider;
        internal MethodInfo Method;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public void Clear()
        {
            Method.Invoke(Provider, []);
            Method   = null!;
            Provider = null!;
        }
    }
}