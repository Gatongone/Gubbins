using Gubbins.Enhance;

namespace Gubbins.Events
{
    public class UnityEventHandler : IEventHandler
    {
        private readonly UnityEngine.Events.UnityEvent m_UnityEvent;

        public UnityEventHandler(UnityEngine.Events.UnityEvent unityEvent) => m_UnityEvent = unityEvent;

        public void Handle(Unit notification) => m_UnityEvent.Invoke();
    }
}