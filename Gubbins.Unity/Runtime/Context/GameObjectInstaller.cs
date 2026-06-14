using System.Linq;
using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    /// <summary>
    /// A <see cref="MonoBehaviour"/> installer that registers dependencies from a serialized list
    /// of <see cref="SerializedInstallInfo"/> entries. Attach to any GameObject in the scene to
    /// contribute bindings to the context at install time.
    /// </summary>
    public class GameObjectInstaller : MonoBehaviour, IDependenciesInstaller
    {
        /// <summary>
        /// Serialized install info array.
        /// </summary>
        [HideLabel] public SerializedInstallInfo[] InstallInfos;

        /// <inheritdoc/>
        public void Install(IDependenciesRegistry registry) => registry.RegisterAll(InstallInfos.Select(static item => item.ToInstallInfo()));

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/Context/GameObjectInstaller", priority = 11)]
        private static GameObject CreatePrefab()
        {
            var go = new GameObject("GameObjectInstaller");
            go.AddComponent<GameObjectInstaller>();
            return go;
        }
#endif
    }
}