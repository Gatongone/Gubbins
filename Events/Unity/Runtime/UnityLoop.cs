using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Gubbins.Game;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Gubbins.Events
{
    /// <summary>
    /// Provides internal management of Unity PlayerLoop phases, allowing registration and unregistration
    /// of custom update delegates for each phase. Used by Gubbins.Events to implement strongly-typed loop events.
    /// </summary>
    internal static class UnityLoop
    {
        /// <summary>PlayerLoop phase name for TimeUpdate.</summary>
        private const string SYS_NAME_TIME_UPDATE = "TimeUpdate";

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
        private static PlayerLoopSystemWrapper s_TimeUpdate;

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
        private static PlayerLoopSystem s_RootLooper;

        /// <summary>Default PlayerLoop system (Unity's default loop).</summary>
        private static PlayerLoopSystem s_DefaultLooper;

        private static bool s_Initialized;

        /// <summary>
        /// Static constructor. Initializes PlayerLoopSystemWrappers for each phase and injects custom update slots.
        /// Registers editor play mode state change handler to restore default loop on exit.
        /// </summary>
        static UnityLoop()
        {
            if (!s_Initialized)
            {
                Init();
            }
        }

        internal static void Init()
        {
            LoopEvent.Registrar = new UnityLoopPhaseRegistrar();
            var loop = PlayerLoop.GetDefaultPlayerLoop();
            s_DefaultLooper = loop;
            for (var index = 0; index < loop.subSystemList.Length; index++)
            {
                ref var system = ref loop.subSystemList[index];
                var updateKind = GetUpdateKind(system);
                var newSysCol = new PlayerLoopSystem[system.subSystemList.Length + 1];
                for (var i = 0; i < system.subSystemList.Length; i++)
                {
                    newSysCol[i] = system.subSystemList[i];
                }

                newSysCol[system.subSystemList.Length] = new PlayerLoopSystem();
                SetLoopSystem(updateKind, new PlayerLoopSystemWrapper(newSysCol, system.subSystemList.Length, updateKind));
                system.subSystemList = newSysCol;
            }

            PlayerLoop.SetPlayerLoop(loop);
            s_RootLooper  = loop;
            s_Initialized = true;
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
                s_Initialized = false;
            }
        }
#endif

        /// <summary>
        /// Registers a custom update delegate to the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onUpdate">The delegate to invoke during the phase.</param>
        internal static void RegisterUpdate(Kind kind, Action<float> onUpdate)
        {
            GetLoopSystem(kind).AddListener(onUpdate);
        }

        /// <summary>
        /// Registers a custom job update delegate to the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onJobUpdate">The delegate to invoke during the phase, which takes deltaTime and a JobHandle for dependencies, and returns a new JobHandle.</param>
        internal static void RegisterUpdate(Kind kind, Func<float, JobHandle, JobHandle> onJobUpdate)
        {
            GetLoopSystem(kind).AddListener(onJobUpdate);
        }

        /// <summary>
        /// Unregisters a custom job update delegate from the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onJobUpdate">The delegate to remove.</param>
        internal static bool UnregisterUpdate(Kind kind, Func<float, JobHandle, JobHandle> onJobUpdate)
        {
            return GetLoopSystem(kind).RemoveListener(onJobUpdate);
        }

        /// <summary>
        /// Unregisters a custom update delegate from the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="onUpdate">The delegate to remove.</param>
        internal static bool UnregisterUpdate(Kind kind, Action<float> onUpdate)
        {
            return GetLoopSystem(kind).RemoveListener(onUpdate);
        }

        /// <summary>
        /// Registers a custom job update delegate to the specified PlayerLoop phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <returns>The delegate to invoke during the phase, which takes deltaTime and a JobHandle for dependencies, and returns a new JobHandle.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PlayerLoopSystemWrapper GetLoopSystem(Kind kind) => kind switch
        {
            Kind.TimeUpdate     => s_TimeUpdate,
            Kind.Initialization => s_InitializationSys,
            Kind.EarlyUpdate    => s_EarlyUpdateSys,
            Kind.FixedUpdate    => s_FixedUpdateSys,
            Kind.PreUpdate      => s_PreUpdateSys,
            Kind.Update         => s_UpdateSys,
            Kind.PreLateUpdate  => s_PreLateUpdateSys,
            Kind.PostLateUpdate => s_PostLateUpdateSys,
            _                   => throw new ArgumentOutOfRangeException()
        };

        /// <summary>
        /// Sets the PlayerLoopSystemWrapper for a given phase.
        /// </summary>
        /// <param name="kind">The PlayerLoop phase.</param>
        /// <param name="playerLoopSystemWrapper">The wrapper to set.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetLoopSystem(Kind kind, PlayerLoopSystemWrapper playerLoopSystemWrapper)
        {
            switch (kind)
            {
                case Kind.TimeUpdate:
                    s_TimeUpdate = playerLoopSystemWrapper;
                    break;
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
            SYS_NAME_TIME_UPDATE      => Kind.TimeUpdate,
            SYS_NAME_INITIALIZATION   => Kind.Initialization,
            SYS_NAME_EARLY_UPDATE     => Kind.EarlyUpdate,
            SYS_NAME_FIXED_UPDATE     => Kind.FixedUpdate,
            SYS_NAME_PRE_UPDATE       => Kind.PreUpdate,
            SYS_NAME_UPDATE           => Kind.Update,
            SYS_NAME_PRE_LATE_UPDATE  => Kind.PreLateUpdate,
            SYS_NAME_POST_LATE_UPDATE => Kind.PostLateUpdate,
            _                         => Kind.Unknown
        };

        /// <summary>
        /// Enumerates supported PlayerLoop phases.
        /// </summary>
        public enum Kind
        {
            /// <summary>Unknown phase.</summary>
            Unknown = 0,

            /// <summary>TimeUpdate phase</summary>
            TimeUpdate,

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
            /// <summary>
            /// List of delegates for the job update phase. Each delegate takes deltaTime and a JobHandle for dependencies, and returns a new JobHandle.
            /// </summary>
            private readonly List<Func<float, JobHandle, JobHandle>> m_JobActions = new();

            /// <summary>
            /// List of delegates for the normal update phase. Each delegate is a simple Action with no parameters.
            /// </summary>
            private readonly List<Action<float>> m_Actions = new List<Action<float>>();

            /// <summary>
            /// List of delegates for the normal update phase. Each delegate is a simple UpdateFunction with no parameters.
            /// </summary>
            private readonly Kind m_Kind;

            /// <summary>
            /// Constructs a wrapper for a PlayerLoopSystem array and index.
            /// </summary>
            /// <param name="sysCol">The PlayerLoopSystem array.</param>
            /// <param name="index">The index of the custom slot.</param>
            public PlayerLoopSystemWrapper(PlayerLoopSystem[] sysCol, int index, Kind kind)
            {
                var loppers = sysCol;
                var index1 = index;
                m_Kind = kind;
                ref var lopper = ref loppers[index1];
                lopper.updateDelegate = UpdateFunction;
            }

            /// <summary>
            /// Adds a delegate to the job update phase.
            /// </summary>
            /// <param name="onJobUpdate">The delegate to add.</param>
            public void AddListener(Func<float, JobHandle, JobHandle> onJobUpdate) => m_JobActions.Add(onJobUpdate);

            /// <summary>
            /// Adds a delegate to the update phase.
            /// </summary>
            /// <param name="onUpdate">The delegate to add.</param>
            public void AddListener(Action<float> onUpdate) => m_Actions.Add(onUpdate);

            /// <summary>
            /// Removes a delegate from the job update phase.
            /// </summary>
            /// <param name="onJobUpdate">The delegate to remove.</param>
            public bool RemoveListener(Func<float, JobHandle, JobHandle> onJobUpdate) => m_JobActions.Remove(onJobUpdate);

            /// <summary>
            /// Removes a delegate from the update phase.
            /// </summary>
            /// <param name="onUpdate">The delegate to remove.</param>
            public bool RemoveListener(Action<float> onUpdate) => m_Actions.Remove(onUpdate);

            private void UpdateFunction()
            {
                JobHandle currentHandle = default;
                var deltaTime = GetDeltaTimeForKind(m_Kind);
                // First execute normal update delegates,
                foreach (var action in m_Actions) action(deltaTime);
                // Then execute job update delegates with proper deltaTime and JobHandle chaining.
                for (var index = 0; index < m_JobActions.Count; index++)
                {
                    var jobFunc = m_JobActions[index];
                    currentHandle = jobFunc(deltaTime, currentHandle);
                }
            }

            /// <summary>
            /// Gets the appropriate deltaTime for the given PlayerLoop phase kind.
            /// </summary>
            /// <param name="kind">The PlayerLoop phase kind.</param>
            /// <returns>The deltaTime to use for job updates in that phase.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float GetDeltaTimeForKind(Kind kind) => kind switch
            {
                Kind.FixedUpdate    => Time.fixedDeltaTime,
                Kind.Initialization => 0f,
                _                   => Time.deltaTime
            };
        }
    }
}