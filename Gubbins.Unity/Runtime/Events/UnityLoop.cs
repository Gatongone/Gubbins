using System;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides internal management of Unity PlayerLoop phases, allowing registration and unregistration
    /// of custom update delegates for each phase. Used by Gubbins.Events to implement strongly-typed loop events.
    /// </summary>
    internal static class UnityLoop
    {
        /// <summary>PlayerLoop phase name for Initialization.</summary>
        private const string SYS_NAME_INITIALIZATION = "Initialization";

        /// <summary>PlayerLoop phase name for EarlyUpdate.</summary>
        private const string SYS_NAME_EARLY_UPDATE = "EarlyUpdate";

        /// <summary>PlayerLoop phase name for FixedUpdate.</summary>
        private const string SYS_NAME_FIXED_UPDATE = "FixedUpdate";

        /// <summary>PlayerLoop phase name for PreUpdate.</summary>
        private const string SYS_NAME_PRE_UPDATE = "PreUpdate";

        /// <summary>PlayerLoop phase name for Update.</summary>
        private const string SYS_NAME_UPDATE = "Update";

        /// <summary>PlayerLoop phase name for PreLateUpdate.</summary>
        private const string SYS_NAME_PRE_LATE_UPDATE = "PreLateUpdate";

        /// <summary>PlayerLoop phase name for PostLateUpdate.</summary>
        private const string SYS_NAME_POST_LATE_UPDATE = "PostLateUpdate";

        /// <summary>Wrapper for Initialization phase.</summary>
        private static PlayerLoopSystemWrapper s_InitializationSys;

        /// <summary>Wrapper for EarlyUpdate phase.</summary>
        private static PlayerLoopSystemWrapper s_EarlyUpdateSys;

        /// <summary>Wrapper for FixedUpdate phase.</summary>
        private static PlayerLoopSystemWrapper s_FixedUpdateSys;

        /// <summary>Wrapper for PreUpdate phase.</summary>
        private static PlayerLoopSystemWrapper s_PreUpdateSys;

        /// <summary>Wrapper for Update phase.</summary>
        private static PlayerLoopSystemWrapper s_UpdateSys;

        /// <summary>Wrapper for PreLateUpdate phase.</summary>
        private static PlayerLoopSystemWrapper s_PreLateUpdateSys;

        /// <summary>Wrapper for PostLateUpdate phase.</summary>
        private static PlayerLoopSystemWrapper s_PostLateUpdateSys;

        /// <summary>Root PlayerLoop system (current custom loop).</summary>
        private static readonly PlayerLoopSystem s_RootLooper;

        /// <summary>Default PlayerLoop system (Unity's default loop).</summary>
        private static readonly PlayerLoopSystem s_DefaultLooper;

        /// <summary>
        /// Static constructor. Initializes PlayerLoopSystemWrappers for each phase and injects custom update slots.
        /// Registers editor play mode state change handler to restore default loop on exit.
        /// </summary>
        static UnityLoop()
        {
            var loop = PlayerLoop.GetDefaultPlayerLoop();
            for (var index = 0; index < loop.subSystemList.Length; index++)
            {
                ref var system = ref loop.subSystemList[index];
                var updateKind = GetUpdateKind(system);
                if (updateKind == Kind.None) continue;
                var newSysCol = new PlayerLoopSystem[system.subSystemList.Length + 1];
                for (var i = 0; i < system.subSystemList.Length; i++)
                {
                    newSysCol[i] = system.subSystemList[i];
                }

                newSysCol[system.subSystemList.Length] = new PlayerLoopSystem();
                SetLoopSystem(updateKind, new PlayerLoopSystemWrapper(newSysCol, system.subSystemList.Length));
                system.subSystemList = newSysCol;
            }

            PlayerLoop.SetPlayerLoop(loop);
            s_RootLooper = loop;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;
#endif
        }
#if UNITY_EDITOR
        /// <summary>
        /// Handles restoring the default PlayerLoop when exiting play mode in the editor.
        /// </summary>
        /// <param name="obj">The play mode state change event.</param>
        private static void OnStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
            {
                PlayerLoop.SetPlayerLoop(s_DefaultLooper);
            }
        }
#endif

        /// <summary>
        /// Registers a custom update delegate to the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onUpdate">The delegate to invoke during the phase.</param>
        internal static void RegisterUpdate(Kind kind, PlayerLoopSystem.UpdateFunction onUpdate)
        {
            Assert.AreNotEqual(kind, Kind.None, $"Not supported update kind: {kind}");
            switch (kind)
            {
                case Kind.Initialization:
                    s_InitializationSys.AddListener(onUpdate);
                    break;
                case Kind.EarlyUpdate:
                    s_EarlyUpdateSys.AddListener(onUpdate);
                    break;
                case Kind.FixedUpdate:
                    s_FixedUpdateSys.AddListener(onUpdate);
                    break;
                case Kind.PreUpdate:
                    s_PreUpdateSys.AddListener(onUpdate);
                    break;
                case Kind.Update:
                    s_UpdateSys.AddListener(onUpdate);
                    break;
                case Kind.PreLateUpdate:
                    s_PreLateUpdateSys.AddListener(onUpdate);
                    break;
                case Kind.PostLateUpdate:
                    s_PostLateUpdateSys.AddListener(onUpdate);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            PlayerLoop.SetPlayerLoop(s_RootLooper);
        }

        /// <summary>
        /// Unregisters a custom update delegate from the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onUpdate">The delegate to remove.</param>
        public static void UnregisterUpdate(Kind kind, PlayerLoopSystem.UpdateFunction onUpdate)
        {
            Assert.AreNotEqual(kind, Kind.None, $"Not supported update kind: {kind}");
            switch (kind)
            {
                case Kind.Initialization:
                    s_InitializationSys.RemoveListener(onUpdate);
                    break;
                case Kind.EarlyUpdate:
                    s_EarlyUpdateSys.RemoveListener(onUpdate);
                    break;
                case Kind.FixedUpdate:
                    s_FixedUpdateSys.RemoveListener(onUpdate);
                    break;
                case Kind.PreUpdate:
                    s_PreUpdateSys.RemoveListener(onUpdate);
                    break;
                case Kind.Update:
                    s_UpdateSys.RemoveListener(onUpdate);
                    break;
                case Kind.PreLateUpdate:
                    s_PreLateUpdateSys.RemoveListener(onUpdate);
                    break;
                case Kind.PostLateUpdate:
                    s_PostLateUpdateSys.RemoveListener(onUpdate);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>
        /// Sets the PlayerLoopSystemWrapper for a given phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="playerLoopSystemWrapper">The wrapper to set.</param>
        private static void SetLoopSystem(Kind kind, PlayerLoopSystemWrapper playerLoopSystemWrapper)
        {
            switch (kind)
            {
                case Kind.Initialization:
                    s_InitializationSys = playerLoopSystemWrapper;
                    break;
                case Kind.EarlyUpdate:
                    s_EarlyUpdateSys = playerLoopSystemWrapper;
                    break;
                case Kind.FixedUpdate:
                    s_FixedUpdateSys = playerLoopSystemWrapper;
                    break;
                case Kind.PreUpdate:
                    s_PreUpdateSys = playerLoopSystemWrapper;
                    break;
                case Kind.Update:
                    s_UpdateSys = playerLoopSystemWrapper;
                    break;
                case Kind.PreLateUpdate:
                    s_PreLateUpdateSys = playerLoopSystemWrapper;
                    break;
                case Kind.PostLateUpdate:
                    s_PostLateUpdateSys = playerLoopSystemWrapper;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>
        /// Maps a PlayerLoopSystem to a Kind enum value based on its type name.
        /// </summary>
        /// <param name="system">The PlayerLoopSystem to check.</param>
        /// <returns>The corresponding Kind value, or Kind.None if not matched.</returns>
        private static Kind GetUpdateKind(PlayerLoopSystem system) => system.type.Name switch
        {
            SYS_NAME_INITIALIZATION   => Kind.Initialization,
            SYS_NAME_EARLY_UPDATE     => Kind.EarlyUpdate,
            SYS_NAME_FIXED_UPDATE     => Kind.FixedUpdate,
            SYS_NAME_PRE_UPDATE       => Kind.PreUpdate,
            SYS_NAME_UPDATE           => Kind.Update,
            SYS_NAME_PRE_LATE_UPDATE  => Kind.PreLateUpdate,
            SYS_NAME_POST_LATE_UPDATE => Kind.PostLateUpdate,
            _                         => Kind.None
        };

        /// <summary>
        /// Enumerates supported PlayerLoop phases.
        /// </summary>
        public enum Kind
        {
            /// <summary>No phase (invalid).</summary>
            None,

            /// <summary>Initialization phase.</summary>
            Initialization,

            /// <summary>EarlyUpdate phase.</summary>
            EarlyUpdate,

            /// <summary>FixedUpdate phase.</summary>
            FixedUpdate,

            /// <summary>PreUpdate phase.</summary>
            PreUpdate,

            /// <summary>Update phase.</summary>
            Update,

            /// <summary>PreLateUpdate phase.</summary>
            PreLateUpdate,

            /// <summary>PostLateUpdate phase.</summary>
            PostLateUpdate
        }

        /// <summary>
        /// Wraps a PlayerLoopSystem array and provides listener management for a specific phase.
        /// </summary>
        private class PlayerLoopSystemWrapper
        {
            /// <summary>Array of PlayerLoopSystem objects for the phase.</summary>
            private readonly PlayerLoopSystem[] m_Loppers;

            /// <summary>Index of the custom slot in the array.</summary>
            private readonly int m_Index;

            /// <summary>
            /// Constructs a wrapper for a PlayerLoopSystem array and index.
            /// </summary>
            /// <param name="sysCol">The PlayerLoopSystem array.</param>
            /// <param name="index">The index of the custom slot.</param>
            public PlayerLoopSystemWrapper(PlayerLoopSystem[] sysCol, int index) => (m_Loppers, m_Index) = (sysCol, index);

            /// <summary>
            /// Adds a delegate to the update phase.
            /// </summary>
            /// <param name="onUpdate">The delegate to add.</param>
            public void AddListener(PlayerLoopSystem.UpdateFunction onUpdate)
            {
                ref var lopper = ref m_Loppers[m_Index];
                lopper.updateDelegate += onUpdate;
            }

            /// <summary>
            /// Removes a delegate from the update phase.
            /// </summary>
            /// <param name="onUpdate">The delegate to remove.</param>
            public void RemoveListener(PlayerLoopSystem.UpdateFunction onUpdate)
            {
                ref var lopper = ref m_Loppers[m_Index];
                lopper.updateDelegate -= onUpdate;
            }
        }
    }
}