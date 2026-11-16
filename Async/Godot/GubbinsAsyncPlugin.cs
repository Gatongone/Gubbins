#if TOOLS
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Godot;

namespace Gubbins.Godot.Async;

/// <summary>
/// Injects the Gubbins Async assembly references into the host project.
/// </summary>
[Tool]
public partial class GubbinsAsyncPlugin : EditorPlugin
{
    private const string Label = "Gubbins.Async";

    /// <inheritdoc/>
    public override void _EnterTree() => ModifyCsproj(inject: true);

    /// <inheritdoc/>
    public override void _ExitTree() => ModifyCsproj(inject: false);

    private void ModifyCsproj(bool inject)
    {
        var root = ProjectSettings.GlobalizePath("res://");
        var path = Directory.GetFiles(root, "*.csproj").FirstOrDefault(f => !f.Replace('\\', '/').Contains("/addons/"));
        if (path is null) return;

        try
        {
            var doc = XDocument.Load(path);
            var ns = doc.Root!.Name.Namespace;
            doc.Descendants().Where(e => (string) e.Attribute("Label") == Label).ToList().ForEach(e => e.Remove());

            if (inject)
            {
                var addonDir = ProjectSettings.GlobalizePath(((Script) GetScript()).ResourcePath).GetBaseDir();
                var pluginsDir = Path.Combine(addonDir, "Plugins");
                var baseDir = Path.GetDirectoryName(path)!;
                var itemGroup = new XElement(ns + "ItemGroup", new XAttribute("Label", Label));

                foreach (var dll in Directory.GetFiles(pluginsDir, "*.dll")
                                             .Where(d => !d.EndsWith(".Generator.dll", StringComparison.OrdinalIgnoreCase)))
                {
                    itemGroup.Add(new XElement(ns + "Reference", new XAttribute("Include", Relative(baseDir, dll))));
                }

                foreach (var generator in Directory.GetFiles(pluginsDir, "*.Generator.dll"))
                {
                    itemGroup.Add(new XElement(ns + "Analyzer", new XAttribute("Include", Relative(baseDir, generator))));
                }

                var propGroup = new XElement(
                    ns + "PropertyGroup",
                    new XAttribute("Label", Label),
                    new XElement(ns + "DefineConstants", "$(DefineConstants);GUBBINS_ENABLED"),
                    new XElement(ns + "AllowUnsafeBlocks", "true"));

                doc.Root.Add(propGroup, itemGroup);
            }

            doc.Save(path);
        }
        catch (Exception ex)
        {
            GD.PushError($"[Gubbins] Failed to modify .csproj: {ex.Message}");
        }
    }

    private static string Relative(string baseDir, string target)
    {
        var baseUri = new Uri(baseDir.TrimEnd('/', '\\') + "/");
        var targetUri = new Uri(target);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(targetUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
    }
}
#endif