namespace Gubbins.Structure;

public static class InternalMap
{
    private static class TypeStore<TTypeKey, TTypeValue> where TTypeValue : new()
    {
        internal static TTypeValue Value = new();
    }

    public static TTypeValue Get<TTypeKey, TTypeValue>() where TTypeValue : new()
        => TypeStore<TTypeKey, TTypeValue>.Value;

    public static void Set<TTypeKey, TTypeValue>(TTypeValue value) where TTypeValue : new()
        => TypeStore<TTypeKey, TTypeValue>.Value = value;
}