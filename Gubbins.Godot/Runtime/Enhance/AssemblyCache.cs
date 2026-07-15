using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;

namespace Gubbins.Enhance;

internal static class AssemblyCache
{
    static AssemblyCache()
    {
        LoadPlugins();

        AllTypes = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(static a => a.GetTypes())
                            .Where(static t => t.IsPublic && !(t.IsSealed && t.IsAbstract))
                            .ToArray();
    }

    public static readonly Type[] AllTypes;

    private static void LoadPlugins()
    {
#if TOOLS
        var dir = AppDomain.CurrentDomain.BaseDirectory;

        while (dir is not null)
        {
            var addonsDir = Path.Combine(dir, "addons");
            if (Directory.Exists(addonsDir))
            {
                foreach (var addonDir in Directory.GetDirectories(addonsDir))
                {
                    var pluginsDir = Path.Combine(addonDir, "Plugins");
                    if (!Directory.Exists(pluginsDir) ||
                        Directory.GetFiles(pluginsDir, "Gubbins.*.dll").Length <= 0)
                        continue;

                    foreach (var file in Directory.GetFiles(pluginsDir, "*.dll"))
                    {
                        if (file.EndsWith("Gubbins.Generator.dll", StringComparison.OrdinalIgnoreCase))
                            continue;
                        AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                    }

                    return;
                }
            }

            dir = Path.GetDirectoryName(dir);
#endif
        }
    }
}