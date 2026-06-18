using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Gubbins.Events
{
    public static class JobLoopEvents
    {
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

    public class JobLoopEvent : ILinkableEventSubscriable<float, JobHandle>
    {
        private readonly UnityLoop.Kind                                                                         m_Kind;
        private readonly Dictionary<ILinkableEventHandler<float, JobHandle>, Func<float, JobHandle, JobHandle>> m_Handlers = new();

        internal JobLoopEvent(UnityLoop.Kind kind) => m_Kind = kind;

        public void Subscribe(ILinkableEventHandler<float, JobHandle> handler)
        {
            if (m_Handlers.ContainsKey(handler))
                return;

            Func<float, JobHandle, JobHandle> wrap = (delta, jobHandle) =>
            {
                handler.Handle(delta, jobHandle);
                return jobHandle;
            };

            m_Handlers.Add(handler, wrap);
            UnityLoop.RegisterUpdate(m_Kind, wrap);
        }

        public void Unsubscribe(ILinkableEventHandler<float, JobHandle> handler)
        {
            if (!m_Handlers.TryGetValue(handler, out var wrap))
                return;

            UnityLoop.UnregisterUpdate(m_Kind, wrap);
            m_Handlers.Remove(handler);
        }

        public void Clear()
        {
            foreach (var wrap in m_Handlers)
                UnityLoop.UnregisterUpdate(m_Kind, wrap.Value);
            m_Handlers.Clear();
        }
    }
}