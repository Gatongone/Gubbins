using Gubbins.Events;

namespace Gubbins.Game;

public class SceneEvent : IEventSubscriable<Scene>
{
    public static ISceneEventRegistrar Registrar { get; set; } = NullRegistrar.Instance;

    private readonly SceneEventKind m_Kind;
    private readonly Dictionary<IEventHandler<Scene>, Action<Scene>> m_HandlerMap = new();

    internal SceneEvent(SceneEventKind kind) => m_Kind = kind;

    public void Subscribe(IEventHandler<Scene> handler)
    {
        Action<Scene> action;
        if (handler is ActionEventHandler<Scene> actionHandler)
            action = actionHandler.Invocation;
        else
            action = handler.Handle;

        m_HandlerMap.Add(handler, action);
        Registrar.Register(m_Kind, action);
    }

    public bool Unsubscribe(IEventHandler<Scene> handler)
    {
        if (!m_HandlerMap.Remove(handler, out var action)) return false;
        Registrar.Unregister(m_Kind, action);
        return true;
    }

    public void Clear()
    {
        foreach (var action in m_HandlerMap.Values)
            Registrar.Unregister(m_Kind, action);
        m_HandlerMap.Clear();
    }

    private sealed class NullRegistrar : ISceneEventRegistrar
    {
        public static readonly NullRegistrar Instance = new();
        public void Register(SceneEventKind kind, Action<Scene> handler) { }
        public void Unregister(SceneEventKind kind, Action<Scene> handler) { }
    }
}
