namespace Gubbins.Game;

public interface ISceneEventRegistrar
{
    void Register(SceneEventKind kind, Action<Scene> handler);
    void Unregister(SceneEventKind kind, Action<Scene> handler);
}
public enum SceneEventKind
{
    Load,
    Unload
}