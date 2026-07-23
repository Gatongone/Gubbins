using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Gubbins.Events
{
    /// <summary>
    /// Static class containing events for the different phases of the Unity PlayerLoop with JobSystem.
    /// </summary>
    public static class JobEvents
    {
        /// <summary>
        /// Event for the Initialization phase of the PlayerLoop.
        /// </summary>
        public class TimeUpdate : JobLoopEvent
        {
            public TimeUpdate() : base(UnityLoop.Kind.TimeUpdate) { }
        }

        /// <summary>
        /// Event for the Initialization phase of the PlayerLoop.
        /// </summary>
        public class Initialization : JobLoopEvent
        {
            public Initialization() : base(UnityLoop.Kind.Initialization) { }
        }

        /// <summary>
        /// Event for the EarlyUpdate phase of the PlayerLoop.
        /// </summary>
        public class EarlyUpdate : JobLoopEvent
        {
            public EarlyUpdate() : base(UnityLoop.Kind.EarlyUpdate) { }
        }

        /// <summary>
        /// Event for the Update phase of the PlayerLoop.
        /// </summary>
        public class Update : JobLoopEvent
        {
            public Update() : base(UnityLoop.Kind.Update) { }
        }

        /// <summary>
        /// Event for the FixedUpdate phase of the PlayerLoop.
        /// </summary>
        public class FixedUpdate : JobLoopEvent
        {
            public FixedUpdate() : base(UnityLoop.Kind.FixedUpdate) { }
        }

        /// <summary>
        /// Event for the PreUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreUpdate : JobLoopEvent
        {
            public PreUpdate() : base(UnityLoop.Kind.PreUpdate) { }
        }

        /// <summary>
        /// Event for the PreLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreLateUpdate : JobLoopEvent
        {
            public PreLateUpdate() : base(UnityLoop.Kind.PreLateUpdate) { }
        }

        /// <summary>
        /// Event for the PostLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PostLateUpdate : JobLoopEvent
        {
            public PostLateUpdate() : base(UnityLoop.Kind.PostLateUpdate) { }
        }
    }

    /// <summary>
    /// Base class for PlayerLoop event wrapper.
    /// </summary>
    public class JobLoopEvent : ILinkableEventSubscriable<float, JobHandle>
    {
        private readonly UnityLoop.Kind                                                                         m_Kind;
        private readonly Dictionary<ILinkableEventHandler<float, JobHandle>, Func<float, JobHandle, JobHandle>> m_Handlers = new();

        internal JobLoopEvent(UnityLoop.Kind kind) => m_Kind = kind;

        /// <inheritdoc/>
        public void Subscribe(ILinkableEventHandler<float, JobHandle> handler)
        {
            if (m_Handlers.ContainsKey(handler))
                return;
            Func<float, JobHandle, JobHandle> action;
            if (handler is LinkableEventHandler<float, JobHandle> linkableHandler)
            {
                action = linkableHandler.Invocation;
            }
            else
            {
                action = handler.Handle;
            }

            m_Handlers.Add(handler, action);
            UnityLoop.RegisterUpdate(m_Kind, action);
        }

        /// <inheritdoc/>
        public bool Unsubscribe(ILinkableEventHandler<float, JobHandle> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var action))
                return false;

            UnityLoop.UnregisterUpdate(m_Kind, action);
            return m_Handlers.Remove(handler);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var action in m_Handlers)
                UnityLoop.UnregisterUpdate(m_Kind, action.Value);
            m_Handlers.Clear();
        }
    }
}