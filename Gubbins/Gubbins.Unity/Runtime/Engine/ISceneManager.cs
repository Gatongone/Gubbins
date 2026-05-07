// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/05/20-06:07:32
// Github: https://github.com/Gatongone

using JetBrains.Annotations;

namespace Gubbins.Unity
{
    public interface ISceneManager
    {
        /// <summary>
        /// Scene name, if null, it would apply to every scene.
        /// </summary>
        [CanBeNull] string SceneName { get; }
        void OnSceneEnter();
        void OnSceneExit();
    }
}