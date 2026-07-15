#if GUBBINS_ENABLED
using System;
using Godot;

namespace Gubbins.Events;

/// <summary>
/// Provides functionality for managing Godot scenes and nodes, including registering callbacks for scene loading and unloading events.
/// </summary>
internal static class GodotScene
{
    /// <summary>
    /// The main scene tree of the Godot engine, used to manage scenes and nodes.
    /// </summary>
    private static readonly SceneTree s_Scene;

    /// <summary>
    /// The root window of the Godot engine, used to manage the main window and its properties.
    /// </summary>
    private static readonly Window s_Window;

    /// <summary>
    /// The callback to invoke when a new scene is loaded. This is registered by other parts of the code to be notified when a new scene is loaded.
    /// </summary>
    private static Action<Node> s_OnSceneLoaded;

    /// <summary>
    /// The callback to invoke when a scene is unloaded. This is registered by other parts of the code to be notified when a scene is unloaded.
    /// </summary>
    private static Action<Node> s_OnSceneUnloaded;

    static GodotScene()
    {
        s_Scene  = Engine.GetMainLoop() as SceneTree;
        s_Window = s_Scene.Root;
        if (s_Scene == null || s_Window == null)
        {
            return;
        }

        s_Scene.SceneChanged     += OnSceneChanged;
        s_Scene.NodeRemoved      += OnNodeRemoved;
    }

    /// <summary>
    /// Registers a callback to be invoked when a new scene is loaded.
    /// </summary>
    /// <param name="callback">The callback to invoke when a new scene is loaded.</param>
    internal static void RegisterSceneLoaded(Action<Node> callback) => s_OnSceneLoaded += callback;

    /// <summary>
    /// Registers a callback to be invoked when a scene is unloaded.
    /// </summary>
    /// <param name="callback">The callback to invoke when a scene is unloaded.</param>
    internal static void RegisterSceneUnloaded(Action<Node> callback) => s_OnSceneUnloaded += callback;

    /// <summary>
    /// Unregisters a callback from being invoked when a new scene is loaded.
    /// </summary>
    /// <param name="callback">The callback to unregister from being invoked when a new scene is loaded.</param>
    internal static void UnregisterSceneLoaded(Action<Node> callback) => s_OnSceneLoaded -= callback;

    /// <summary>
    /// Unregisters a callback from being invoked when a scene is unloaded.
    /// </summary>
    /// <param name="callback">The callback to unregister from being invoked when a scene is unloaded.</param>
    internal static void UnregisterSceneUnloaded(Action<Node> callback) => s_OnSceneUnloaded -= callback;

    /// <summary>
    /// Callback invoked when a node is removed from the scene tree. If the removed node is the root window,
    /// it unregisters the scene change and node removal callbacks.
    /// If the removed node is a top-level node with a valid scene file path, it invokes the scene unloaded callback.
    /// </summary>
    /// <param name="node">The node that was removed from the scene tree.</param>
    private static void OnNodeRemoved(Node node)
    {
        if (s_OnSceneUnloaded == null) return;
        if (GodotObject.IsInstanceValid(s_Window) && node.GetParent() == node.GetTree().Root && !string.IsNullOrEmpty(node.SceneFilePath))
        {
            s_OnSceneUnloaded.Invoke(node);
        }
    }

    /// <summary>
    /// Callback invoked when the scene changes. It invokes the scene loaded callback with the current scene.
    /// </summary>
    private static void OnSceneChanged() => s_OnSceneLoaded?.Invoke(s_Scene.CurrentScene);
}
#endif
