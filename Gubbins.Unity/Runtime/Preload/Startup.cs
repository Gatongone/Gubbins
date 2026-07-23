using Gubbins.Events;
using Gubbins.Unsafe;
using UnityEngine;

namespace Gubbins.Preload
{
    /// <summary>
    /// Static class responsible for preloading and patching necessary components before the main context initialization.
    /// </summary>
    public static class Startup
    {
        /// <summary>
        /// Indicates whether the preload process has been initialized.
        /// </summary>
        public static bool HasInitialized { get; private set; }

        /// <summary>
        /// Initializes the preload process by replacing Unity's memory management with a custom implementation.
        /// </summary>
        /// <remarks>
        /// You should call this when preload phase starts if your project trying to use preloaded assets in PlayerSettings.
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Init()
        {
            if (!HasInitialized)
            {
                UnityMemory.ReplaceInstance();
                UnityLoop.Init();
                UnityScene.Init();
                HasInitialized = true;
            }
        }
    }
}