using Gubbins.Unity;
using Gubbins.Domain;

namespace Gubbins.Editor
{
    internal class CategoryRecycler
    {
        [UnityEditor.InitializeOnLoadMethod]
        private static void OnEnter()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;
        }

        private static void OnStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
            {
                Domain.Standard.Reset();
            }
        }
    }
}