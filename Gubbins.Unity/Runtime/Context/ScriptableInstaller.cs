using System.Linq;
using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    /// <summary>
    /// A <see cref="ScriptableObject"/> installer that registers dependencies from a serialized
    /// list of <see cref="SerializedInstallInfo"/> entries. 
    /// </summary>
    [CreateAssetMenu(fileName = "ScriptableInstaller", menuName = "Context/ScriptableInstaller")]
    public class ScriptableInstaller : ScriptableObject, IDependenciesInstaller
    {
        /// <summary>
        /// Serialized install info array.
        /// </summary>
        [HideLabel] public SerializedInstallInfo[] InstallInfos;

        /// <inheritdoc/>
        public void Install(IDependenciesRegistry registry) => registry.RegisterAll(InstallInfos.Select(static item => item.ToInstallInfo()));
    }
}