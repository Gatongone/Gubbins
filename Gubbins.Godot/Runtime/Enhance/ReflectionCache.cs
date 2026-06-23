namespace Gubbins.Enhance;

public static class ReflectionCache
{
    public static readonly Type[] AllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();
}