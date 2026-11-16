using System;
using System.Collections.Generic;
using Gubbins.Game;
using UnityEngine.SceneManagement;
using Scene = Gubbins.Game.Scene;

namespace Gubbins.Events
{
    public static class UnityScene
    {
        public static void Init()
        {
            SceneEvent.Registrar = new Registrar();
        }

        private sealed class Registrar : ISceneEventRegistrar
        {
            private readonly List<Action<Scene>> m_LoadedActions   = new();
            private readonly List<Action<Scene>> m_UnloadedActions = new();

            public Registrar()
            {
                SceneManager.sceneLoaded += (scene, _) =>
                {
                    var s = new Scene(scene.name);
                    foreach (var action in m_LoadedActions) action(s);
                };

                SceneManager.sceneUnloaded += scene =>
                {
                    var s = new Scene(scene.name);
                    foreach (var action in m_UnloadedActions) action(s);
                };
            }

            public void Register(SceneEventKind kind, Action<Scene> handler)
            {
                if (kind == SceneEventKind.Load)
                    m_LoadedActions.Add(handler);
                else
                    m_UnloadedActions.Add(handler);
            }

            public void Unregister(SceneEventKind kind, Action<Scene> handler)
            {
                if (kind == SceneEventKind.Load)
                    m_LoadedActions.Remove(handler);
                else
                    m_UnloadedActions.Remove(handler);
            }
        }
    }
}
