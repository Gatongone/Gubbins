using System.Collections.Generic;
using Gubbins.Enhance;
using UnityEngine.LowLevel;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides strongly-typed wrappers for Unity PlayerLoop phase events.
    /// </summary>
    public static class LoopEvents
    {
        /// <summary>
        /// Event for the Initialization phase of the PlayerLoop.
        /// </summary>
        public class Initialization : LoopEvent
        {
            internal Initialization() : base(UnityLoop.Kind.Initialization) { }
        }

        /// <summary>
        /// Event for the EarlyUpdate phase of the PlayerLoop.
        /// </summary>
        public class EarlyUpdate : LoopEvent
        {
            internal EarlyUpdate() : base(UnityLoop.Kind.EarlyUpdate) { }
        }

        /// <summary>
        /// Event for the Update phase of the PlayerLoop.
        /// </summary>
        public class Update : LoopEvent
        {
            internal Update() : base(UnityLoop.Kind.Update) { }
        }

        /// <summary>
        /// Event for the FixedUpdate phase of the PlayerLoop.
        /// </summary>
        public class FixedUpdate : LoopEvent
        {
            internal FixedUpdate() : base(UnityLoop.Kind.FixedUpdate) { }
        }

        /// <summary>
        /// Event for the PreUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreUpdate : LoopEvent
        {
            internal PreUpdate() : base(UnityLoop.Kind.PreUpdate) { }
        }

        /// <summary>
        /// Event for the PreLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PreLateUpdate : LoopEvent
        {
            internal PreLateUpdate() : base(UnityLoop.Kind.PreLateUpdate) { }
        }

        /// <summary>
        /// Event for the PostLateUpdate phase of the PlayerLoop.
        /// </summary>
        public class PostLateUpdate : LoopEvent
        {
            internal PostLateUpdate() : base(UnityLoop.Kind.PostLateUpdate) { }
        }
    }

    /// <summary>
    /// Base class for PlayerLoop event wrappers.
    /// </summary>
    public class LoopEvent : IEventSubscriable<Unit>
    {
        private readonly UnityLoop.Kind                                                   m_Kind;
        private readonly Dictionary<IEventHandler<Unit>, PlayerLoopSystem.UpdateFunction> m_HandlerMap = new();

        internal LoopEvent(UnityLoop.Kind kind)
        {
            m_Kind = kind;
        }

        /// <inheritdoc/>
        public void Subscribe(IEventHandler<Unit> handler)
        {
            PlayerLoopSystem.UpdateFunction wrap = () => handler.Handle(Unit.Instance);
            m_HandlerMap.Add(handler, wrap);
            UnityLoop.RegisterUpdate(m_Kind, wrap);
        }

        /// <inheritdoc/>
        public bool Unsubscribe(IEventHandler<Unit> handler)
        {
            if (!m_HandlerMap.Remove(handler, out var wrap)) return false;
            UnityLoop.UnregisterUpdate(m_Kind, wrap);
            return true;
        }

        /// <inheritdoc/>
        public void Clear()
        {
            foreach (var wrap in m_HandlerMap.Values)
            {
                UnityLoop.UnregisterUpdate(m_Kind, wrap);
            }

            m_HandlerMap.Clear();
        }
    }
}