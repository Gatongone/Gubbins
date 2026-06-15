using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gubbins.Editor
{
    internal static class AssemblyCache
    {
        internal static readonly Type[] AllTypes = GetAllTypes();

        private static Type[] GetAllTypes()
        {
            var assemblies = GetAllAssemblies();
            var types = new List<Type>();
            foreach (var assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types.AddRange(ex.Types);
                }
            }
            return types.ToArray();
        }
        private static IReadOnlyList<Assembly> GetAllAssemblies()
        {
#if UNITY_6000_5_OR_NEWER
            return UnityEngine.Assemblies.CurrentAssemblies.GetLoadedAssemblies();
#else
            return AppDomain.CurrentDomain.GetAssemblies();
#endif
        }
    }
}