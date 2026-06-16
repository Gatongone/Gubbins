using Gubbins.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// Burst compiles a separate concrete method for each closed generic job, so every component type that
// can be scattered must be registered here. Missing registrations fall back to the (slower) managed
// path rather than failing, but registering keeps the scatter fully Burst-accelerated.
[assembly: RegisterGenericJobType(typeof(ScatterJob<Position>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionX>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionXY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionXZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<PositionYZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<Rotation>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationX>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationXY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationXZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<RotationYZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<Orientation>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<Scale>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleX>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleXY>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleXZ>))]
[assembly: RegisterGenericJobType(typeof(ScatterJob<ScaleYZ>))]

namespace Gubbins.Entities
{
    /// <summary>
    /// Implemented by transform components so the scatter (main-thread and Burst) can write their
    /// values and axis flags into a <see cref="TransformSnapshot"/> through a constrained generic call,
    /// avoiding both per-type dispatch and boxing.
    /// </summary>
    internal interface ITransformComponent
    {
        /// <summary>Merges this component's value(s) and axis flags into <paramref name="snapshot"/>.</summary>
        void Write(ref TransformSnapshot snapshot);
    }

    /// <summary>
    /// Burst job that scatters one component column into the per-slot snapshots, scheduled once per
    /// query segment (chunk). Snapshot writes target arbitrary slots resolved from the entity index, so
    /// the parallel-for range restriction is disabled. Distinct entities map to distinct slots, so there
    /// is no intra-job aliasing; cross-variant ordering (an entity carrying e.g. both Position and
    /// Rotation) is enforced by the scheduler chaining variants in <c>EntityAdapter</c>.
    /// </summary>
    /// <typeparam name="T">The transform component type being scattered.</typeparam>
    [BurstCompile]
    internal struct ScatterJob<T> : IJobParallelFor where T : unmanaged, ITransformComponent
    {
        // Entities/Components alias pinned chunk memory (Allocator.None views). The job safety system
        // rejects pointer-constructed containers in a parallel job, so opt them out of the restriction;
        // they are read-only and each entity maps to a distinct slot, so there is no aliasing hazard.
        [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<Entity> Entities;
        [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<T>      Components;
        [ReadOnly] public NativeArray<int> SlotByEntityIndex;

        [NativeDisableParallelForRestriction]
        public NativeArray<TransformSnapshot> Snapshots;

        public void Execute(int i)
        {
            var index = Entities[i].Index;
            if ((uint) index >= (uint) SlotByEntityIndex.Length)
            {
                return;
            }

            var slot = SlotByEntityIndex[index];
            if (slot < 0)
            {
                return;
            }

            var snapshot = Snapshots[slot];
            Components[i].Write(ref snapshot);
            Snapshots[slot] = snapshot;
        }
    }
}
