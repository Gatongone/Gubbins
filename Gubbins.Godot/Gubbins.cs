#if TOOLS
using Godot;

namespace Gubbins.Plugins;

/// <summary>
/// Gubbins plugin registry.
/// </summary>
[Tool]
public partial class Gubbins : EditorPlugin
{
    public override void _EnterTree() { }

    public override void _ExitTree() { }
}
#endif