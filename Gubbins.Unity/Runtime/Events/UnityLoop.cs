using System;
using UnityEngine.Assertions;
using UnityEngine.LowLevel;

namespace Gubbins.Events
{
    internal static class UnityLoop
    {
        private const           string                  SYS_NAME_INITIALIZATION   = "Initialization";
        private const           string                  SYS_NAME_EARLY_UPDATE     = "EarlyUpdate";
        private const           string                  SYS_NAME_FIXED_UPDATE     = "FixedUpdate";
        private const           string                  SYS_NAME_PRE_UPDATE       = "PreUpdate";
        private const           string                  SYS_NAME_UPDATE           = "Update";
        private const           string                  SYS_NAME_PRE_LATE_UPDATE  = "PreLateUpdate";
        private const           string                  SYS_NAME_POST_LATE_UPDATE = "PostLateUpdate";
        private static          PlayerLoopSystemWrapper s_InitializationSys;
        private static          PlayerLoopSystemWrapper s_EarlyUpdateSys;
        private static          PlayerLoopSystemWrapper s_FixedUpdateSys;
        private static          PlayerLoopSystemWrapper s_PreUpdateSys;
        private static          PlayerLoopSystemWrapper s_UpdateSys;
        private static          PlayerLoopSystemWrapper s_PreLateUpdateSys;
        private static          PlayerLoopSystemWrapper s_PostLateUpdateSys;
        private static readonly PlayerLoopSystem        s_RootLooper;
        private static readonly PlayerLoopSystem        s_DefaultLooper;

        static UnityLoop()
        {
            s_DefaultLooper = PlayerLoop.GetDefaultPlayerLoop();
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
        private static void OnStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
            {
                PlayerLoop.SetPlayerLoop(s_DefaultLooper);
            }
        }
#endif

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

        public enum Kind
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
            private readonly int                m_Index;
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