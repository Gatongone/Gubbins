using Godot;

namespace Gubbins.Events;

/// <summary>
/// Provides a static class for managing Godot window events and registering callbacks for various window-related actions.
/// </summary>
internal static class GodotWindow
{
    /// <summary>
    /// The main scene of the Godot application. This is used to register callbacks for various window events.
    /// </summary>
    private static readonly Window s_Window;

    /// <summary>
    /// The callback to invoke when the window size changes. This is registered by other parts of the code to be notified when the window size changes.
    /// </summary>
    private static Action<Vector2I> s_OnWindowSizeChanged;

    /// <summary>
    /// The callback to invoke when the window DPI changes. This is registered by other parts of the code to be notified when the window DPI changes.
    /// </summary>
    private static Action<int, float> s_OnWindowDpiChanged;

    /// <summary>
    /// The callback to invoke when the mouse enters the window. This is registered by other parts of the code to be notified when the mouse enters the window.
    /// </summary>
    private static Action s_OnMouseEntered;

    /// <summary>
    /// The callback to invoke when the mouse exits the window. This is registered by other parts of the code to be notified when the mouse exits the window.
    /// </summary>
    private static Action s_OnMouseExited;

    /// <summary>
    /// The callback to invoke when the window gains focus. This is registered by other parts of the code to be notified when the window gains focus.
    /// </summary>
    private static Action s_OnFocusEntered;

    /// <summary>
    /// The callback to invoke when the window loses focus. This is registered by other parts of the code to be notified when the window loses focus.
    /// </summary>
    private static Action s_OnFocusExited;

    /// <summary>
    /// The callback to invoke when the window is ready. This is registered by other parts of the code to be notified when the window is ready.
    /// </summary>
    private static Action s_OnWindowReady;

    /// <summary>
    /// The callback to invoke when a "go back" action is requested. This is registered by other parts of the code to be notified when a "go back" action is requested.
    /// </summary>
    private static Action s_OnGobackRequested;

    /// <summary>
    /// The callback to invoke when a close action is requested. This is registered by other parts of the code to be notified when a close action is requested.
    /// </summary>
    private static Action s_OnCloseRequested;

    static GodotWindow()
    {
        var scene = Engine.GetMainLoop() as SceneTree;
        s_Window = scene.Root;
        if (s_Window == null)
        {
            return;
        }

        s_Window.SizeChanged     += OnSizeChanged;
        s_Window.DpiChanged      += OnDpiChanged;
        s_Window.MouseEntered    += OnMouseEntered;
        s_Window.MouseExited     += OnMouseExited;
        s_Window.FocusEntered    += OnFocusEntered;
        s_Window.FocusExited     += OnFocusExited;
        s_Window.Ready           += OnWindowReady;
        s_Window.GoBackRequested += OnGobackRequested;
        s_Window.CloseRequested  += OnCloseRequested;
    }

    /// <summary>
    /// Registers a callback to be invoked when the window size changes. The callback will receive the new window size as a Vector2I parameter.
    /// </summary>
    internal static void RegisterWindowSizeChangedCallback(Action<Vector2I> callback) => s_OnWindowSizeChanged += callback;

    /// <summary>
    /// Registers a callback to be invoked when the window DPI changes. The callback will receive the new DPI and scale as parameters.
    /// </summary>
    internal static void RegisterWindowDpiChangedCallback(Action<int, float> callback) => s_OnWindowDpiChanged += callback;

    /// <summary>
    /// Registers a callback to be invoked when the mouse enters the window. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterMouseEnteredCallback(Action callback) => s_OnMouseEntered += callback;

    /// <summary>
    /// Registers a callback to be invoked when the mouse exits the window. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterMouseExitedCallback(Action callback) => s_OnMouseExited += callback;

    /// <summary>
    /// Registers a callback to be invoked when the window gains focus. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterFocusEnteredCallback(Action callback) => s_OnFocusEntered += callback;

    /// <summary>
    /// Registers a callback to be invoked when the window loses focus. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterFocusExitedCallback(Action callback) => s_OnFocusExited += callback;

    /// <summary>
    /// Registers a callback to be invoked when the window is ready. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterWindowReadyCallback(Action callback) => s_OnWindowReady += callback;

    /// <summary>
    /// Registers a callback to be invoked when a "go back" action is requested. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterGobackRequestedCallback(Action callback) => s_OnGobackRequested += callback;

    /// <summary>
    /// / Registers a callback to be invoked when a close action is requested. The callback will be invoked without any parameters.
    /// </summary>
    internal static void RegisterCloseRequestedCallback(Action callback) => s_OnCloseRequested += callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the window size changes. The callback will no longer be invoked when the window size changes.
    /// </summary>
    internal static void UnregisterWindowSizeChangedCallback(Action<Vector2I> callback) => s_OnWindowSizeChanged -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the window DPI changes. The callback will no longer be invoked when the window DPI changes.
    /// </summary>
    internal static void UnregisterWindowDpiChangedCallback(Action<int, float> callback) => s_OnWindowDpiChanged -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the mouse enters the window. The callback will no longer be invoked when the mouse enters the window.
    /// </summary>
    internal static void UnregisterMouseEnteredCallback(Action callback) => s_OnMouseEntered -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the mouse exits the window. The callback will no longer be invoked when the mouse exits the window.
    /// </summary>
    internal static void UnregisterMouseExitedCallback(Action callback) => s_OnMouseExited -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the window gains focus. The callback will no longer be invoked when the window gains focus.
    /// </summary>
    internal static void UnregisterFocusEnteredCallback(Action callback) => s_OnFocusEntered -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the window loses focus. The callback will no longer be invoked when the window loses focus.
    /// </summary>
    internal static void UnregisterFocusExitedCallback(Action callback) => s_OnFocusExited -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when the window is ready. The callback will no longer be invoked when the window is ready.
    /// </summary>
    internal static void UnregisterWindowReadyCallback(Action callback) => s_OnWindowReady -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when a "go back" action is requested. The callback will no longer be invoked when a "go back" action is requested.
    /// </summary>
    internal static void UnregisterGobackRequestedCallback(Action callback) => s_OnGobackRequested -= callback;

    /// <summary>
    /// Unregisters a callback that was previously registered to be invoked when a close action is requested. The callback will no longer be invoked when a close action is requested.
    /// </summary>
    /// <param name="callback"></param>
    internal static void UnregisterCloseRequestedCallback(Action callback) => s_OnCloseRequested -= callback;

    /// <summary>
    /// Callback invoked when the window size changes. It invokes the window size changed callback with the root window.
    /// </summary>
    private static void OnSizeChanged() => s_OnWindowSizeChanged?.Invoke(s_Window.Size);

    /// <summary>
    /// Callback invoked when the window DPI changes. It invokes the window DPI changed callback with the current DPI and scale.
    /// </summary>
    private static void OnDpiChanged() => s_OnWindowDpiChanged?.Invoke(DisplayServer.ScreenGetDpi(), DisplayServer.ScreenGetScale());

    /// <summary>
    /// Callback invoked when the mouse enters the window. It invokes the mouse entered callback.
    /// </summary>
    private static void OnMouseEntered() => s_OnMouseEntered?.Invoke();

    /// <summary>
    /// Callback invoked when the mouse exits the window. It invokes the mouse exited callback.
    /// </summary>
    private static void OnMouseExited() => s_OnMouseExited?.Invoke();

    /// <summary>
    /// Callback invoked when the window gains focus. It invokes the focus entered callback.
    /// </summary>
    private static void OnFocusEntered() => s_OnFocusEntered?.Invoke();

    /// <summary>
    /// Callback invoked when the window loses focus. It invokes the focus exited callback.
    /// </summary>
    private static void OnFocusExited() => s_OnFocusExited?.Invoke();

    /// <summary>
    /// Callback invoked when the window is ready. It invokes the window ready callback.
    /// </summary>
    private static void OnWindowReady() => s_OnWindowReady?.Invoke();

    /// <summary>
    /// Callback invoked when a "go back" action is requested. It invokes the go back requested callback.
    /// </summary>
    private static void OnGobackRequested() => s_OnGobackRequested?.Invoke();

    /// <summary>
    /// Callback invoked when a close action is requested. It invokes the close requested callback.
    /// </summary>
    private static void OnCloseRequested() => s_OnCloseRequested?.Invoke();
}