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
    /// Base class for PlayerLoop job event wrapper.
    /// </summary>
    public class JobLoopEvent
    {
        private readonly UnityLoop.Kind m_Kind;
        private readonly HashSet<Func<float, JobHandle, JobHandle>> m_Handlers = new();

        internal JobLoopEvent(UnityLoop.Kind kind) => m_Kind = kind;

        public void Subscribe(Func<float, JobHandle, JobHandle> handler)
        {
            if (!m_Handlers.Add(handler))
                return;
            UnityLoop.RegisterUpdate(m_Kind, handler);
        }

        public bool Unsubscribe(Func<float, JobHandle, JobHandle> handler)
        {
            if (!m_Handlers.Remove(handler))
                return false;
            UnityLoop.UnregisterUpdate(m_Kind, handler);
            return true;
        }

        public void Clear()
        {
            foreach (var handler in m_Handlers)
                UnityLoop.UnregisterUpdate(m_Kind, handler);
            m_Handlers.Clear();
        }
    }
}

