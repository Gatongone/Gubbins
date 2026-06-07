using UnityEngine;

namespace Gubbins.Context
{
    [CreateAssetMenu(fileName = "ScriptableInstaller", menuName = "Context/ScriptableInstaller")]
    public class ScriptableInstaller : ScriptableObject, IDependenciesInstaller
    {
        public SerializedInstallInfo[] InstallInfo;
        public void Install(IDependenciesRegistry registry)
        {
        }
    }
}