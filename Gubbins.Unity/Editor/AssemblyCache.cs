using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gubbins.Editor
{
    /// <summary>
    /// Caches all types from all loaded assemblies to avoid repeated reflection calls.
    /// </summary>
    internal static class AssemblyCache
    {
        /// <summary>
        /// Gets all types from all loaded assemblies.
        /// </summary>
        internal static readonly Type[] AllTypes = GetAllTypes();

        /// <summary>
        /// Gets all types from all loaded assemblies, handling any exceptions that may occur during type loading.
        /// </summary>
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

        /// <summary>
        /// Gets all loaded assemblies in the current application domain, with special handling for Unity.
        /// </summary>
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