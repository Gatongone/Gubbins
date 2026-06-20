using System;
using UnityEngine;
using Gubbins.Context;
using Gubbins.Enhance;

namespace Gubbins.Pipeline
{
    /// <summary>
    /// Persistent pipeline that registers a list of event listeners with a specified context.
    /// </summary>
    /// <remarks>
    /// All Singleton scope in this context shouldn't have a MonoBehaviour lifecycle,
    /// otherwise they will be destroyed when the scene is unloaded.
    /// If you want to have a MonoBehaviour lifecycle, you can use <see cref="ScenePipeline"/> instead.
    /// </remarks>
    [CreateAssetMenu(fileName = "GamePipeline", menuName = "Pipeline/GamePipeline")]
    public class GamePipeline : ScriptableObject, IPipeline
    {
        /// <summary>
        /// Indicates whether the GamePipeline has started. This property returns true if the Start method has been called, and false otherwise.
        /// </summary>
        private bool m_HasStarted;

        /// <summary>
        /// The parent context for the GamePipeline, which is a special context that is initialized on preload phase.
        /// </summary>
        [Tooltip("The context reference for the GamePipeline. This context will be used to register the event listeners defined in the pipeline. If not set, it will default to the global application context."), SerializeField]
        private SerializedReference<IContext> m_Context;

        /// <summary>
        /// The list of event listeners to register with the context.
        /// These listeners will be executed in the order they are defined in the array.
        /// </summary>
        [Tooltip("The list of event listeners to register with the context. These listeners will be executed in the order they are defined in the array."), SerializeField]
        private SerializedReference<IEventListener>[] m_Listeners;

        /// <summary>
        /// Indicates whether the GamePipeline should automatically start when the application starts.
        /// If set to true, the Start method will be called during the OnEnable phase, allowing the pipeline to begin execution immediately. If set to false,
        /// the Start method must be called manually to initiate the pipeline.
        /// </summary>
        [Tooltip("If true, the GamePipeline will automatically start when the pipeline was loaded. If false, you must call the \"Start()\" method manually to initiate the pipeline."), SerializeField]
        private bool m_AutoStart;

        /// <inheritdoc/>
        public PipeLineState State { get; private set; }

        /// <summary>
        /// Initializes the ScriptableContext instance and sets up the application context with the specified installers and listeners.
        /// </summary>
        private void OnEnable()
        {
#if UNITY_EDITOR
            var isPlaying = Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;
#else
        var isPlaying = Application.isPlaying;
#endif
            // We don't initialize instance on editor mode.
            if (!isPlaying)
            {
                return;
            }

            if (m_Context.Value == null)
            {
                m_Context.Value = ApplicationContext.Global;
                Debug.LogWarning("The context reference for the GamePipeline is not set. Defaulting to the global application context. Please assign a valid context reference to ensure proper functionality.");
            }

            if (m_AutoStart)
            {
                Start();
            }
        }

        /// <summary>
        /// Starts the GamePipeline by registering the event listeners with the context and transitioning the pipeline state to Running.
        /// If the pipeline has already been started, an <see cref="InvalidOperationException"/> is thrown to prevent multiple invocations of the Start method.
        /// If there are no listeners to register, the pipeline state is set to Completed immediately.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the Start method is called more than once during its lifecycle.</exception>
        public void Start()
        {
            if (m_HasStarted)
            {
                throw new InvalidOperationException("The GamePipeline has already been started. Please ensure that the Start method is called only once during the application lifecycle.");
            }

            if (m_Listeners.Length == 0)
            {
                State = PipeLineState.Completed;
                return;
            }

            State = PipeLineState.Running;
            var context = m_Context.Value;

            try
            {
                foreach (var listener in m_Listeners)
                {
                    var target = listener.Value;
                    if (target != null)
                    {
                        context.Inject(target);
                        target.Listen(context, context);
                    }
                    else
                    {
                        var type = listener.ExpectedType;
                        var ctor = type == null ? null : InjectCache.GetInjectConstructor(type);
                        if (ctor == null) continue;
                        target = context.InjectByCtor(listener.ExpectedType) as IEventListener;
                        target?.Listen(context, context);
                    }
                }

                m_HasStarted = true;
            }
            catch
            {
                State = PipeLineState.Failed;
                throw;
            }
        }

        /// <summary>
        /// Stops the GamePipeline by clearing the registered event listeners from the context and transitioning the pipeline state to Completed.
        /// </summary>
        public void Stop()
        {
            if (!m_HasStarted)
            {
                return;
            }

            var context = m_Context.Value;

            foreach (var listener in m_Listeners)
            {
                var target = listener.Value;
                target?.Clear(context);
            }

            State        = PipeLineState.Completed;
            m_HasStarted = false;
        }

        /// <inheritdoc/>
        Unit IPhase<Unit, Unit>.Start(Unit input)
        {
            Start();
            return input;
        }
    }
}