using System;
using System.Collections.Generic;
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
        /// Indicates whether the GamePipeline should automatically start when the application starts.
        /// If set to true, the Start method will be called during the OnEnable phase, allowing the pipeline to begin execution immediately. If set to false,
        /// the Start method must be called manually to initiate the pipeline.
        /// </summary>
        [Tooltip("If true, the GamePipeline will automatically start when the pipeline was loaded. If false, you must call the \"Start()\" method manually to initiate the pipeline."), SerializeField]
        private bool m_AutoStart;

        /// <summary>
        /// The context reference for the GamePipeline. This context will be used to register the event listeners defined in the pipeline.
        /// </summary>
        [Tooltip("The context reference for the GamePipeline. This context will be used to register the event listeners defined in the pipeline. If not set, it will default to the global application context."), SerializeField]
        private SerializedReference<IContext> m_Context;

        /// <summary>
        /// The list of event listeners to register with the context. These listeners will be executed in the order they are defined in the array.
        /// When the listener type contains a constructor with parameters, the context will attempt to resolve the dependencies and inject them into the constructor.
        /// Else, it will be instantiated using the default constructor an inject by fields/properties or method.
        /// </summary>
        [Tooltip("The list of event listeners to register with the context. These listeners will be executed in the order they are defined in the array. When the listener type contains a constructor with parameters, the context will attempt to resolve the dependencies and inject them into the constructor. Else, it will be instantiated using the default constructor an inject by fields/properties or method."), SerializeField, AllowDefaultConstructorMissing]
        private SerializedReference<IEventListener>[] m_Listeners;

        /// <inheritdoc/>
        public PipeLineState State { get; private set; }

        /// <summary>
        /// A list of instantiated event listeners that have been registered with the context.
        /// </summary>
        private readonly List<IEventListener> m_ListenerInstances = new();

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
            if (State == PipeLineState.Running)
            {
                throw new InvalidOperationException("The GamePipeline has already been started. Please ensure that the Start method is called only once during the application lifecycle.");
            }

            if (m_Listeners.Length == 0)
            {
                State = PipeLineState.Completed;
                return;
            }

            var context = m_Context.Value;
            if (context == null)
            {
                context = m_Context.Value = ApplicationContext.Global;
                Debug.LogWarning("The context reference for the GamePipeline is not set. Defaulting to the global application context. Please assign a valid context reference to ensure proper functionality.");
            }

            State = PipeLineState.Running;

            try
            {
                if (State == PipeLineState.NotStarted)
                {
                    State = PipeLineState.Running;
                    RegisterListeners(context);
                }
                else
                {
                    foreach (var listener in m_ListenerInstances)
                    {
                        listener.Listen(context, context);
                    }
                }
            }
            catch
            {
                State = PipeLineState.Failed;
                throw;
            }
        }

        /// <summary>
        /// Registers the event listeners defined in the GamePipeline with the provided context.
        /// </summary>
        private void RegisterListeners(IContext context)
        {
            foreach (var listener in m_Listeners)
            {
                var targetType = listener.ExpectedType;
                // Try inject by ctor first.
                if (targetType != null && InjectCache.GetInjectConstructor(targetType) != null)
                {
                    var item = context.InjectByCtor(targetType) as IEventListener;
                    listener.Value = item;
                    if (item != null)
                    {
                        item.Listen(context, context);
                        m_ListenerInstances.Add(item);
                    }
                }
                else
                {
                    var item = listener.Value;
                    if (item != null)
                    {
                        context.Inject(item);
                        item.Listen(context, context);
                        m_ListenerInstances.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Stops the GamePipeline by clearing the registered event listeners from the context and transitioning the pipeline state to Completed.
        /// </summary>
        public void Stop()
        {
            if (State != PipeLineState.Running)
            {
                return;
            }

            var context = m_Context.Value;

            if (context != null)
            {
                foreach (var listener in m_ListenerInstances)
                {
                    listener?.Clear(context);
                }
            }

            State = PipeLineState.Completed;
        }

        /// <inheritdoc/>
        Unit IPhase<Unit, Unit>.Start(Unit input)
        {
            Start();
            return input;
        }
    }
}