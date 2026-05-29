// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/17-22:17:00
// Github: https://github.com/Gatongone

using System;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

namespace Gubbins.Events
{
    internal static class UnityLoop
    {
        private const string SYS_NAME_INITIALIZATION   = "Initialization";
        private const string SYS_NAME_EARLY_UPDATE     = "EarlyUpdate";
        private const string SYS_NAME_FIXED_UPDATE     = "FixedUpdate";
        private const string SYS_NAME_PRE_UPDATE       = "PreUpdate";
        private const string SYS_NAME_UPDATE           = "Update";
        private const string SYS_NAME_PRE_LATE_UPDATE  = "PreLateUpdate";
        private const string SYS_NAME_POST_LATE_UPDATE = "PostLateUpdate";
        private static PlayerLoopSystemWrapper m_InitializationSys;
        private static PlayerLoopSystemWrapper m_EarlyUpdateSys;
        private static PlayerLoopSystemWrapper m_FixedUpdateSys;
        private static PlayerLoopSystemWrapper m_PreUpdateSys;
        private static PlayerLoopSystemWrapper m_UpdateSys;
        private static PlayerLoopSystemWrapper m_PreLateUpdateSys;
        private static PlayerLoopSystemWrapper m_PostLateUpdateSys;
        private static readonly PlayerLoopSystem m_RootLooper;
        private static readonly PlayerLoopSystem m_DefaultLooper;

        static UnityLoop()
        {
            m_DefaultLooper = PlayerLoop.GetDefaultPlayerLoop();
            var loop = PlayerLoop.GetDefaultPlayerLoop();
            for (var index = 0; index < loop.subSystemList.Length; index++)
            {
                ref var system = ref loop.subSystemList[index];
                var updateKind = GetUpdateKind(system);
                if (updateKind == UpdateKind.None) continue;
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
            m_RootLooper = loop;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;
#endif
        }
#if UNITY_EDITOR
        private static void OnStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
            {
                PlayerLoop.SetPlayerLoop(m_DefaultLooper);
            }
        }
#endif

        internal static void RegisterUpdate(UpdateKind kind, PlayerLoopSystem.UpdateFunction onUpdate)
        {
            Assert.AreNotEqual(kind, UpdateKind.None, $"Not supported update kind: {kind}");
            switch (kind)
            {
                case UpdateKind.Initialization:
                    m_InitializationSys.AddListener(onUpdate);
                    break;
                case UpdateKind.EarlyUpdate:
                    m_EarlyUpdateSys.AddListener(onUpdate);
                    break;
                case UpdateKind.FixedUpdate:
                    m_FixedUpdateSys.AddListener(onUpdate);
                    break;
                case UpdateKind.PreUpdate:
                    m_PreUpdateSys.AddListener(onUpdate);
                    break;
                case UpdateKind.Update:
                    m_UpdateSys.AddListener(onUpdate);
                    break;
                case UpdateKind.PreLateUpdate:
                    m_PreLateUpdateSys.AddListener(onUpdate);
                    break;
                case UpdateKind.PostLateUpdate:
                    m_PostLateUpdateSys.AddListener(onUpdate);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
            PlayerLoop.SetPlayerLoop(m_RootLooper);
        }

        public static void UnregisterUpdate(UpdateKind kind, PlayerLoopSystem.UpdateFunction onUpdate)
        {
            Assert.AreNotEqual(kind, UpdateKind.None, $"Not supported update kind: {kind}");
            switch (kind)
            {
                case UpdateKind.Initialization:
                    m_InitializationSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.EarlyUpdate:
                    m_EarlyUpdateSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.FixedUpdate:
                    m_FixedUpdateSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.PreUpdate:
                    m_PreUpdateSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.Update:
                    m_UpdateSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.PreLateUpdate:
                    m_PreLateUpdateSys.RemoveListener(onUpdate);
                    break;
                case UpdateKind.PostLateUpdate:
                    m_PostLateUpdateSys.RemoveListener(onUpdate);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static void SetLoopSystem(UpdateKind kind, PlayerLoopSystemWrapper playerLoopSystemWrapper)
        {
            switch (kind)
            {
                case UpdateKind.Initialization:
                    m_InitializationSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.EarlyUpdate:
                    m_EarlyUpdateSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.FixedUpdate:
                    m_FixedUpdateSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.PreUpdate:
                    m_PreUpdateSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.Update:
                    m_UpdateSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.PreLateUpdate:
                    m_PreLateUpdateSys = playerLoopSystemWrapper;
                    break;
                case UpdateKind.PostLateUpdate:
                    m_PostLateUpdateSys = playerLoopSystemWrapper;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        private static UpdateKind GetUpdateKind(PlayerLoopSystem system) => system.type.Name switch
        {
            SYS_NAME_INITIALIZATION   => UpdateKind.Initialization,
            SYS_NAME_EARLY_UPDATE     => UpdateKind.EarlyUpdate,
            SYS_NAME_FIXED_UPDATE     => UpdateKind.FixedUpdate,
            SYS_NAME_PRE_UPDATE       => UpdateKind.PreUpdate,
            SYS_NAME_UPDATE           => UpdateKind.Update,
            SYS_NAME_PRE_LATE_UPDATE  => UpdateKind.PreLateUpdate,
            SYS_NAME_POST_LATE_UPDATE => UpdateKind.PostLateUpdate,
            _                         => UpdateKind.None
        };

        public enum UpdateKind
        {
            None,
            Initialization,
            EarlyUpdate,
            FixedUpdate,
            PreUpdate,
            Update,
            PreLateUpdate,
            PostLateUpdate
        }


        private class PlayerLoopSystemWrapper
        {
            private readonly PlayerLoopSystem[] m_Loppers;
            private readonly int m_Index;
            public PlayerLoopSystemWrapper(PlayerLoopSystem[] sysCol, int index) => (m_Loppers, m_Index) = (sysCol, index);

            public void AddListener(PlayerLoopSystem.UpdateFunction onUpdate)
            {
                ref var lopper = ref m_Loppers[m_Index];
                lopper.updateDelegate += onUpdate;
            }

            public void RemoveListener(PlayerLoopSystem.UpdateFunction onUpdate)
            {
                ref var lopper = ref m_Loppers[m_Index];
                lopper.updateDelegate -= onUpdate;
            }
        }
    }
}