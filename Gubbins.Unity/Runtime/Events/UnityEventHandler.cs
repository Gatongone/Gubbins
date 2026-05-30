using Gubbins.Enhance;

namespace Gubbins.Events
{
    /// <summary>
    /// Handles UnityEvent invocation as an IEventHandler implementation.
    /// </summary>
    public class UnityEventHandler : IEventHandler
    {
        /// <summary>
        /// The UnityEvent instance to be invoked.
        /// </summary>
        private readonly UnityEngine.Events.UnityEvent m_UnityEvent;

        /// <param name="unityEvent">The UnityEvent to wrap and invoke.</param>
        public UnityEventHandler(UnityEngine.Events.UnityEvent unityEvent)
            => m_UnityEvent = unityEvent;

        /// <inheritdoc/>
        public void Handle(Unit notification) => m_UnityEvent.Invoke();
    }
}