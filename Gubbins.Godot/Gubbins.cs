#if TOOLS
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Godot;

namespace Gubbins.Godot;

[Tool]
public partial class Gubbins : EditorPlugin
{
    private const string XML_LABEL      = "Gubbins";
    private const string ENABLED_DEFINE = "GUBBINS_ENABLED";

    public override void _EnterTree() => ModifyCsproj(inject: true);

    public override void _ExitTree() => ModifyCsproj(inject: false);

    private void ModifyCsproj(bool inject)
    {
        var path = FindMainCsproj();
        if (path is null)
        {
            GD.PushError("[Gubbins] Cannot locate the project .csproj file.");
            return;
        }

        try
        {
            var doc = XDocument.Load(path);
            var ns = doc.Root!.Name.Namespace;

            // Remove any previously-injected elements first (idempotent)
            doc.Descendants()
               .Where(e => (string) e.Attribute("Label") == XML_LABEL)
               .ToList()
               .ForEach(e => e.Remove());

            if (inject)
            {
                var pluginsDir = ProjectSettings.GlobalizePath("res://addons/gubbins/Plugins");
                var baseDir = Path.GetDirectoryName(path)!;

                var dlls = Directory.GetFiles(pluginsDir, "*.dll")
                                    .Where(d => !Path.GetFileName(d).Equals(
                                        "Gubbins.Generator.dll", StringComparison.OrdinalIgnoreCase))
                                    .ToList();

                var generator = Path.Combine(pluginsDir, "Gubbins.Generator.dll");

                var itemGroup = new XElement(ns + "ItemGroup", new XAttribute("Label", XML_LABEL));
                foreach (var dll in dlls)
                {
                    itemGroup.Add(new XElement(ns + "Reference", new XAttribute("Include", Relative(baseDir, dll))));
                }

                if (File.Exists(generator))
                {
                    itemGroup.Add(new XElement(ns + "Analyzer", new XAttribute("Include", Relative(baseDir, generator))));
                }

                var propGroup = new XElement(ns + "PropertyGroup", new XAttribute("Label", XML_LABEL),
                    new XElement(ns + "DefineConstants", $"$(DefineConstants);{ENABLED_DEFINE}"),
                    new XElement(ns + "AllowUnsafeBlocks", "true"));

                doc.Root.Add(propGroup, itemGroup);
            }
            else
            {
                GD.Print("[Gubbins] References removed.");
            }

            doc.Save(path);
        }
        catch (Exception ex)
        {
            GD.PushError($"[Gubbins] Failed to modify .csproj: {ex.Message}");
        }
    }

    private static string FindMainCsproj()
    {
        var root = ProjectSettings.GlobalizePath("res://");

        // ProjectSettings dotnet/project/project_directory
        if (ProjectSettings.HasSetting("dotnet/project/project_directory"))
        {
            var rel = (string) ProjectSettings.GetSetting("dotnet/project/project_directory");
            if (!string.IsNullOrEmpty(rel))
            {
                var dir = Path.GetFullPath(Path.Combine(root, rel));
                if (Directory.Exists(dir))
                {
                    var hit = Directory.GetFiles(dir, "*.csproj").FirstOrDefault();
                    if (hit is not null) return hit;
                }
            }
        }

        // <AssemblyName>.csproj at project root
        if (ProjectSettings.HasSetting("dotnet/project/assembly_name"))
        {
            var name = (string) ProjectSettings.GetSetting("dotnet/project/assembly_name");
            var candidate = Path.Combine(root, $"{name}.csproj");
            if (File.Exists(candidate)) return candidate;
        }

        // Any .csproj at root that isn't inside addons/
        return Directory.GetFiles(root, "*.csproj").FirstOrDefault(f => !f.Replace('\\', '/').Contains("/addons/"));
    }

    private static string Relative(string baseDir, string target)
    {
        var baseUri = new Uri(baseDir.TrimEnd('/', '\\') + "/");
        var targetUri = new Uri(target);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }
}
#endif