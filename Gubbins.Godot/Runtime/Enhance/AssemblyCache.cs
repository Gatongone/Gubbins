namespace Gubbins.Enhance;

/// <summary>
/// A static class that caches all public, non-abstract types from the current AppDomain's assemblies.
/// </summary>
internal static class AssemblyCache
{
    /// <summary>
    /// A static readonly array that contains all public, non-abstract types from the current AppDomain's assemblies.
    /// </summary>
    public static readonly Type[] AllTypes = AppDomain.CurrentDomain
                                                      .GetAssemblies()
                                                      .SelectMany(static assembly => assembly.GetTypes())
                                                      .Where(static t => t.IsPublic && !(t.IsSealed && t.IsAbstract))
                                                      .ToArray();
}