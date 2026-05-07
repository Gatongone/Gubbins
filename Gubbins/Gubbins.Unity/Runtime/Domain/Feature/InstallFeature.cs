// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/24-01:53:11
// Github: https://github.com/Gatongone

using Gubbins.Context;
using UnityEngine;

namespace Gubbins.Unity
{
    internal class InstallFeature : IFeature
    {
        public int Priority => int.MinValue;
        public bool Enable { get; set; } = true;

        public void Evaluate(ICategory category, ISystem system)
        {
            if (system is not IDependenciesInstaller installer) return;

            if(category.Context is IDependenciesRegistry registry)
                installer.Install(registry);
            else
                Debug.LogWarning($"Category '{category.GetType()}' doesn't supported {nameof(InstallFeature)}. Because there is no '{typeof(IDependenciesRegistry)}' can be resolve in the category context.");
        }
    }
}