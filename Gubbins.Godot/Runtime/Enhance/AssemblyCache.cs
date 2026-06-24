namespace Gubbins.Enhance;

internal static class AssemblyCache
{
    public static readonly Type[] AllTypes = AppDomain.CurrentDomain
                                                      .GetAssemblies()
                                                      .SelectMany(static assembly => assembly.GetTypes())
                                                      .Where(static t => t.IsPublic && !(t.IsSealed && t.IsAbstract))
                                                      .ToArray();
}