#if TOOLS
using Godot;

namespace Gubbins.Plugins;

[Tool]
public partial class Gubbins : EditorPlugin
{
    public override void _EnterTree() { }

    public override void _ExitTree() { }
}
#endif