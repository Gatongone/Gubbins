using System;
using Gubbins.Events;
using Gubbins.Game;
using Gubbins.Pipeline;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

namespace Gubbins.Entities
{
    /// <summary>
    /// A per-slot snapshot of an entity's transform components, gathered on the main thread and
    /// consumed by <see cref="TransformSyncJob"/>. <see cref="Flags"/> records which individual axes
    /// are present — including axis-subset variants like <see cref="PositionZ"/> — so absent axes
    /// leave the corresponding component of the bound transform untouched.
    /// </summary>
    internal struct TransformSnapshot
    {
        internal const ushort POS_X = 1 << 0;
        internal const ushort POS_Y = 1 << 1;
        internal const ushort POS_Z = 1 << 2;
        internal const ushort ROT_X = 1 << 3;
        internal const ushort ROT_Y = 1 << 4;
        internal const ushort ROT_Z = 1 << 5;
        internal const ushort SCL_X = 1 << 6;
        internal const ushort SCL_Y = 1 << 7;
        internal const ushort SCL_Z = 1 << 8;

        // Whole-quaternion orientation; mutually exclusive with the Euler rotation axes above.
        internal const ushort HAS_ORIENTATION = 1 << 9;

        // Channel masks: set when any axis of the channel is present.
        internal const ushort HAS_POSITION = POS_X | POS_Y | POS_Z;
        internal const ushort HAS_ROTATION = ROT_X | ROT_Y | ROT_Z;
        internal const ushort HAS_SCALE    = SCL_X | SCL_Y | SCL_Z;

        public Vector3    Position;
        public Vector3    Euler;
        public Quaternion Orientation;
        public Vector3    Scale;
        public ushort     Flags;
    }

    /// <summary>
    /// Writes the gathered <see cref="TransformSnapshot"/> values back onto the registered transforms
    /// in parallel. Scheduled over <see cref="EntityAdapter.Transforms"/>, so snapshot index matches
    /// transform slot.
    /// </summary>
    [BurstCompile]
    internal struct TransformSyncJob : IJobParallelForTransform
    {
        [ReadOnly] public NativeArray<TransformSnapshot> Snapshots;

        public void Execute(int index, TransformAccess transform)
        {
            var snapshot = Snapshots[index];
            var flags    = snapshot.Flags;

            // Each channel merges only the axes it owns onto the live transform, so an entity that
            // carries just PositionZ moves along Z while its X/Y stay whatever the transform held.
            if ((flags & TransformSnapshot.HAS_POSITION) != 0)
            {
                var position = transform.localPosition;
                if ((flags & TransformSnapshot.POS_X) != 0) position.x = snapshot.Position.x;
                if ((flags & TransformSnapshot.POS_Y) != 0) position.y = snapshot.Position.y;
                if ((flags & TransformSnapshot.POS_Z) != 0) position.z = snapshot.Position.z;
                transform.localPosition = position;
            }

            // A quaternion orientation is written verbatim and takes precedence; otherwise the Euler
            // axes are merged onto the transform's current rotation.
            if ((flags & TransformSnapshot.HAS_ORIENTATION) != 0)
            {
                transform.localRotation = snapshot.Orientation;
            }
            else if ((flags & TransformSnapshot.HAS_ROTATION) != 0)
            {
                var euler = transform.localRotation.eulerAngles;
                if ((flags & TransformSnapshot.ROT_X) != 0) euler.x = snapshot.Euler.x;
                if ((flags & TransformSnapshot.ROT_Y) != 0) euler.y = snapshot.Euler.y;
                if ((flags & TransformSnapshot.ROT_Z) != 0) euler.z = snapshot.Euler.z;
                transform.localRotation = Quaternion.Euler(euler);
            }

            if ((flags & TransformSnapshot.HAS_SCALE) != 0)
            {
                var scale = transform.localScale;
                if ((flags & TransformSnapshot.SCL_X) != 0) scale.x = snapshot.Scale.x;
                if ((flags & TransformSnapshot.SCL_Y) != 0) scale.y = snapshot.Scale.y;
                if ((flags & TransformSnapshot.SCL_Z) != 0) scale.z = snapshot.Scale.z;
                transform.localScale = scale;
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
        [Event(typeof(LoopEvents.Lately))]
        private void LateUpdate(float _) => EntityAdapter.SyncTransforms();
    }
}
