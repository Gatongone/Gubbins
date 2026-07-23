namespace Gubbins.Game;

public class SceneEvents
{
    public class Load() : SceneEvent(SceneEventKind.Load);

    public class Unload() : SceneEvent(SceneEventKind.Unload);
}