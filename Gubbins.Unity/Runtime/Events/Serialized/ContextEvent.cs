using System;
using Gubbins.Context;
using Gubbins.Enhance;
using UnityEngine;
using UnityEngine.Events;

namespace Gubbins.Events
{
    /// <summary>
    /// A MonoBehaviour that subscribes to an event bus in a context and invokes UnityEvents when the event is triggered.
    /// </summary>
    [Serializable]
    public class ContextEvent : MonoBehaviour, IScopeController
    {
        private const TypeKind DEFAULT_TYPE_KIND = TypeKind.Implementation | TypeKind.Newable | TypeKind.Class;

        /// <summary>
        /// Event type for subscribe to event bus. The event bus will be resolved from context by this type.
        /// The event bus will be created and registered to context if not exist.
        /// </summary>
        /// <remarks>
        /// If the event hasn't been registered to context, It will be registered as Custom scope,
        /// which means the scope will be controlled by this ContextEvent.
        /// The scope will be finished when the GameObject of this ContextEvent is destroyed.
        /// So the event bus will be removed from context when the scope is finished.
        /// </remarks>
        [SerializeField, TypeFrom(kind: DEFAULT_TYPE_KIND, exclude: new[] {typeof(SerializedEvent), typeof(Event)})]
        private SerializedType<IEvent> m_Event;

        /// <summary>
        /// UnityEvent for subscribe to event bus.
        /// </summary>
        [SerializeField] private UnityEvent m_Handlers;

        /// <summary>
        /// Context reference for resolve event bus. The event bus will be created and registered to context if not exist.
        /// </summary>
        [SerializeField] private SerializedReference<IContext> m_Context;

        /// <summary>
        /// Handlers for wrap <see cref="m_Handlers"/>.
        /// </summary>
        private UnityEventHandler m_UnityEventHandlers;

        /// <summary>
        /// Event invoked when the scope of this ContextEvent is finished, which is when the GameObject is destroyed.
        /// This can be used to perform cleanup or other actions when the ContextEvent is no longer needed.
        /// </summary>
        private Action m_OnScopeFinish;

        /// <inheritdoc cref="IScopeController.OnScopeFinish"/>.
        event Action IScopeController.OnScopeFinish
        {
            add => m_OnScopeFinish += value;
            remove => m_OnScopeFinish -= value;
        }

        /// <summary>
        /// Subscribes all UnityEvent handlers to the event bus when the GameObject is initialized.
        /// </summary>
        private void Awake()
        {
            if (m_Context.Value == null || m_Event.Type == null) return;
            var context = m_Context.Value;
            var eventType = m_Event.Type;
            var eventBus = context.Resolve(m_Event.Type) as IEvent;
            if (eventBus == null && eventType.IsNewable(out var newer))
            {
                eventBus = (IEvent) newer.New();
                if (context is not IDependenciesRegistry registry)
                {
                    throw new ArgumentException("Context must implement IDependenciesRegistry to register event bus.");
                }

                registry.Register(eventBus)
                        .BindTo(m_Event)
                        .BindTo<IEvent, IEventSubscriable, IEventBroadcastable, IWeakEventSubscriable>()
                        .BindTo<IEvent<Unit>, IEventBroadcastable<Unit>, IEventSubscriable<Unit>>()
                        .WithKey(m_Event.Type.Name)
                        .AsCustom(this);
            }
            else
            {
                throw new ArgumentException("Event required null value.");
            }

            m_UnityEventHandlers = new UnityEventHandler(m_Handlers);
            eventBus.Subscribe(m_UnityEventHandlers);
        }

        /// <summary>
        /// Unsubscribes all UnityEvent handlers from the event bus and invokes the scope finish event when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            if (m_Context.Value == null || m_Event.Type == null) return;
            var context = m_Context.Value;
            var eventType = m_Event.Type;
            if (context.Resolve(eventType) is IEvent eventBus)
            {
                eventBus.Unsubscribe(m_UnityEventHandlers);
            }

            m_OnScopeFinish?.Invoke();
        }

        /// <summary>
        /// Unsubscribe all unity events.
        /// </summary>
        public void RemoveAllHandlers() => m_Handlers.RemoveAllListeners();
    }
}