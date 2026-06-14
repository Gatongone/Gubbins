using System;
using Gubbins.Context;
using Gubbins.Events;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using Object = UnityEngine.Object;

namespace Gubbins.Entities
{
    /// <summary>
    /// A per-slot snapshot of an entity's transform components, gathered on the main thread and
    /// consumed by <see cref="TransformSyncJob"/>. <see cref="Flags"/> records which channels are
    /// present so absent ones leave the bound transform untouched.
    /// </summary>
    internal struct TransformSnapshot
    {
        internal const byte HasPosition = 1 << 0;
        internal const byte HasRotation = 1 << 1;
        internal const byte HasScale    = 1 << 2;

        public Vector3 Position;
        public Vector3 Euler;
        public Vector3 Scale;
        public byte    Flags;
    }

    /// <summary>
    /// Writes the gathered <see cref="TransformSnapshot"/> values back onto the registered transforms
    /// in parallel. Scheduled over <see cref="EntityAdapter.Transforms"/>, so snapshot index matches
    /// transform slot.
    /// </summary>
    internal struct TransformSyncJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<TransformSnapshot> Snapshots;

        public void Execute(int index, TransformAccess transform)
        {
            var snapshot = Snapshots[index];
            var flags    = snapshot.Flags;

            if ((flags & TransformSnapshot.HasPosition) != 0)
            {
                transform.localPosition = snapshot.Position;
            }

            if ((flags & TransformSnapshot.HasRotation) != 0)
            {
                transform.localRotation = Quaternion.Euler(snapshot.Euler);
            }

            if ((flags & TransformSnapshot.HasScale) != 0)
            {
                transform.localScale = snapshot.Scale;
            }
        }
    }

    /// <summary>
    /// Hidden runtime driver that pumps <see cref="EntityAdapter.SyncTransforms"/> once per frame in
    /// LateUpdate, after gameplay/ECS systems have mutated component data.
    /// </summary>
    [Serializable]
    internal sealed partial class EntityTransformSyncRunner : IEventListener
    {
        [Event(typeof(LoopEvents.PostLateUpdate))]
        private void LateUpdate() => EntityAdapter.SyncTransforms();
    }
}
