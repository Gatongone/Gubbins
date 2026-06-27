using System;
using System.Collections.Generic;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides strongly-typed actionpers for Unity PlayerLoop phase events.
    /// </summary>
    public static class LoopEvents
    {
        /// <summary>
        /// Event for the Initialization phase of the PlayerLoop.
        /// </summary>
        public class Initialization : LoopEvent
        {
            public Initialization() : base(UnityLoop.Kind.Initialization) { }
        }

        /// <summary>
        /// Event for the EarlyUpdate phase of the PlayerLoop.
        /// </summary>
        public class EarlyUpdate : LoopEvent
        {
            public EarlyUpdate() : base(UnityLoop.Kind.EarlyUpdate) { }
        }

        /// <summary>
        /// Event for the Update phase of the PlayerLoop.
        /// </summary>
        public class Update : LoopEvent
        {
            public Update() : base(UnityLoop.Kind.Update) { }
        }

        /// <summary>
        /// Event for the FixedUpdate phase of the PlayerLoop.
        /// </summary>
        public class FixedUpdate : LoopEvent
        {
            public FixedUpdate() : base(UnityLoop.Kind.FixedUpdate) { }
        }

        /// <summary>
        /// Event for the PreUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreUpdate : LoopEvent
        {
            public PreUpdate() : base(UnityLoop.Kind.PreUpdate) { }
        }

        /// <summary>
        /// Event for the PreLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreLateUpdate : LoopEvent
        {
            public PreLateUpdate() : base(UnityLoop.Kind.PreLateUpdate) { }
        }

        /// <summary>
        /// Event for the PostLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PostLateUpdate : LoopEvent
        {
            public PostLateUpdate() : base(UnityLoop.Kind.PostLateUpdate) { }
        }
    }

    /// <summary>
    /// Base class for PlayerLoop event actionpers.
    /// </summary>
    public class LoopEvent : IEventSubscriable<float>
    {
        private readonly UnityLoop.Kind                                  m_Kind;
        private readonly Dictionary<IEventHandler<float>, Action<float>> m_HandlerMap = new();

        internal LoopEvent(UnityLoop.Kind kind)
        {
            m_Kind = kind;
        }

        /// <inheritdoc/>
        public void Subscribe(IEventHandler<float> handler)
        {
            Action<float> action;
            if (handler is ActionEventHandler<float> actionHandler)
            {
                action = actionHandler.Invocation;
            }
            else
            {
                action = handler.Handle;
            }

            m_HandlerMap.Add(handler, action);
            UnityLoop.RegisterUpdate(m_Kind, action);
        }

        /// <inheritdoc/>
        public bool Unsubscribe(IEventHandler<float> handler)
        {
            if (!m_HandlerMap.Remove(handler, out var action)) return false;
            UnityLoop.UnregisterUpdate(m_Kind, action);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var action in m_HandlerMap.Values)
            {
                UnityLoop.UnregisterUpdate(m_Kind, action);
            }

            m_HandlerMap.Clear();
        }
    }
}