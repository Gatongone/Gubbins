// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/05/20-06:05:41
// Github: https://github.com/Gatongone

using System;
using System.Collections.Generic;
using Gubbins.Engine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gubbins.Unity
{
    internal sealed class SceneFeature : IFeature
    {
        private static readonly List<WeakReference<ISceneManager>> s_SceneManagers = new();
        public int Priority => int.MinValue + 200;
        public bool Enable { get; set; } = true;

        public void Evaluate(ICategory category, ISystem system)
        {
            if (system is ISceneManager sceneManager)
            {
                s_SceneManagers.Add(new WeakReference<ISceneManager>(sceneManager));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterCallback()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            for (var index = s_SceneManagers.Count - 1; index >= 0; index--)
            {
                var sceneManager = s_SceneManagers[index];

                if (!sceneManager.TryGetTarget(out var instance) ||
                    instance is UnityEngine.Object unityObj && unityObj == null)
                {
                    s_SceneManagers.RemoveAt(index);
                    continue;
                }

                if (instance.SceneName == null || instance.SceneName == scene.name)
                {
                    instance.OnSceneEnter();
                }
            }
        }

        private static void OnSceneUnloaded(Scene scene)
        {
            for (var index = s_SceneManagers.Count - 1; index >= 0; index--)
            {
                var sceneManager = s_SceneManagers[index];

                if (!sceneManager.TryGetTarget(out var instance) ||
                    instance is UnityEngine.Object unityObj && unityObj == null)
                {
                    s_SceneManagers.RemoveAt(index);
                    continue;
                }

                if (instance.SceneName == null || instance.SceneName == scene.name)
                {
                    instance.OnSceneExit();
                }
            }
        }
    }
}