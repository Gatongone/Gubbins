#if TOOLS
using Godot;
using Gubbins.Editor;

[Tool]
public partial class GubbinsGodot : EditorPlugin
{
    private SerializedTypeEditorPlugin m_SerializedTypeEditorPlugin;

    public override void _EnterTree()
    {
        m_SerializedTypeEditorPlugin = new SerializedTypeEditorPlugin();
        AddInspectorPlugin(m_SerializedTypeEditorPlugin);
    }

    public override void _ExitTree()
    {
        RemoveInspectorPlugin(m_SerializedTypeEditorPlugin);
    }
}
#endif