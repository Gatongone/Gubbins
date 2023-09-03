namespace Gubbins.Structure;

internal class InternalSingleton
{
    private static class TypeStore<TTypeValue> where TTypeValue :  new()
    {
        internal static readonly TTypeValue Value = new();
    }

    internal static TTypeValue InstanceOf<TTypeValue>() where TTypeValue : new() 
        => TypeStore<TTypeValue>.Value;
}