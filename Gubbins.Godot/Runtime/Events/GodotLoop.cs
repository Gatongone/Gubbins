#if GUBBINS_ENABLED
using System;
using Godot;
using Gubbins.Game;

namespace Gubbins.Events;

/// <summary>
/// Provides internal management of Godot WindowLoop phases, allowing registration and unregistration of custom update delegates for each phase.
/// </summary>
internal static class GodotLoop
{
    /// <summary>
    /// Indicates whether the GodotLoop has been initialized. This flag is used to ensure that the loop is only initialized once.
    /// </summary>
    private static bool s_Initialized;

    /// <summary>
    /// The main loop of the Godot engine, cast to a SceneTree for access to its events.
    /// </summary>
    private static SceneTree s_Looper;

    /// <summary>
    /// The main window of the Godot engine, used to retrieve delta time for frame updates.
    /// </summary>
    private static Window s_Window;

    /// <summary>
    /// Event that is invoked during the ProcessFrame phase of the Godot main loop, passing the delta time since the last frame as a parameter.
    /// </summary>
    private static event Action<float> s_ProcessFrame;

    /// <summary>
    /// Event that is invoked during the PhysicsFrame phase of the Godot main loop, passing the delta time since the last physics frame as a parameter.
    /// </summary>
    private static event Action<float> s_PhysicsFrame;

    /// <summary>
    /// Event that is invoked during the Preprocess phase of the Godot main loop, passing the delta time since the last frame as a parameter.
    /// </summary>
    private static event Action<float> s_PreFrame;

    /// <summary>
    /// Event that is invoked during the Postprocess phase of the Godot main loop, passing the delta time since the last frame as a parameter.
    /// </summary>
    private static event Action<float> s_PostFrame;

    static GodotLoop() => Init();

    internal static void Init()
    {
        if (s_Initialized)
        {
            return;
        }

        LoopEvent.Registrar = new GodotLoopPhaseRegistrar();
        s_Looper = Engine.GetMainLoop() as SceneTree;
        s_Window = s_Looper.Root;
        if (s_Looper == null || s_Window == null)
        {
            return;
        }

        if (!s_Looper.IsConnected(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame)))
            s_Looper.ProcessFrame += OnProcessFrame;
        if (!s_Looper.IsConnected(SceneTree.SignalName.PhysicsFrame, Callable.From(OnPhysicsFrame)))
            s_Looper.PhysicsFrame += OnPhysicsFrame;

        s_Initialized = true;
    }

    /// <summary>
    /// Registers a custom update delegate to the specified WindowLoop phase.
    /// </summary>
    /// <param name="kind">The WindowLoop phase.</param>
    /// <param name="onUpdate">The delegate to invoke during the phase.</param>
    internal static void RegisterUpdate(Kind kind, Action<float> onUpdate)
    {
        if (s_Looper == null || s_Window == null)
        {
            return;
        }

        switch (kind)
        {
            case Kind.Physics:
                s_ProcessFrame += onUpdate;
                break;
            case Kind.Process:
                s_PhysicsFrame += onUpdate;
                break;
            case Kind.Preprocess:
                s_PreFrame += onUpdate;
                break;
            case Kind.Postprocess:
                s_PostFrame += onUpdate;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    /// <summary>
    /// Unregisters a custom update delegate from the specified WindowLoop phase.
    /// </summary>
    /// <param name="kind">The WindowLoop phase.</param>
    /// <param name="onUpdate">The delegate to remove from the phase.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified kind is not a valid WindowLoop phase.</exception>
    internal static void UnregisterUpdate(Kind kind, Action<float> onUpdate)
    {
        if (s_Looper == null || s_Window == null)
        {
            return;
        }

        switch (kind)
        {
            case Kind.Physics:
                s_ProcessFrame -= onUpdate;
                break;
            case Kind.Process:
                s_PhysicsFrame -= onUpdate;
                break;
            case Kind.Preprocess:
                s_PreFrame -= onUpdate;
                break;
            case Kind.Postprocess:
                s_PostFrame -= onUpdate;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }

    /// <summary>
    /// Invokes the registered ProcessFrame delegates with the delta time since the last frame.
    /// </summary>
    private static void OnProcessFrame()
    {
        var deltaTime = s_Window.GetProcessDeltaTime();
        s_PreFrame?.Invoke((float) deltaTime);
        s_ProcessFrame?.Invoke((float) deltaTime);
        s_PostFrame?.Invoke((float) deltaTime);
    }

    /// <summary>
    /// Invokes the registered PhysicsFrame delegates with the delta time since the last physics frame.
    /// </summary>
    private static void OnPhysicsFrame() => s_PhysicsFrame?.Invoke((float) s_Window.GetPhysicsProcessDeltaTime());

    /// <summary>
    /// Enumerates supported WindowLoop phases.
    /// </summary>
    internal enum Kind
    {
        /// <summary>
        /// The Physics phase, which occurs during the physics processing of the frame. This phase is typically used for physics-related calculations and updates, such as collision detection and response.
        /// </summary>
        Physics,

        /// <summary>
        /// The Preprocess phase, which occurs before the ProcessFrame phase. This phase is typically used for initialization or setup tasks that need to occur before the main processing of the frame begins.
        /// </summary>
        Preprocess,

        /// <summary>
        /// The Process phase, which occurs during the main processing of the frame. This phase is typically used for the core logic of the application, such as updating game objects, handling input, and performing calculations.
        /// </summary>
        Process,

        /// <summary>
        /// The Postprocess phase, which occurs after the ProcessFrame phase. This phase is typically used for cleanup or finalization tasks that need to occur after all other processing has completed.
        /// </summary>
        Postprocess
    }
}
#endif