using Gubbins.Enhance;
using UnityEngine;

namespace Gubbins.Context
{
    [CreateAssetMenu(fileName = "ScriptableInstaller", menuName = "Context/ScriptableInstaller")]
    public class ScriptableInstaller : ScriptableObject, IDependenciesInstaller
    {
        [HideLabel] public SerializedInstallInfo[] InstallInfos;
        public void Install(IDependenciesRegistry registry) { }
    }
}