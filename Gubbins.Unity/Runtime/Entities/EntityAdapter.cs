using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Gubbins.Entities
{
    /// <summary>
    /// A MonoBehaviour that serves as an adapter to create and manage an entity
    /// based on the GameObject's transform and specified components.
    /// </summary>
    public class EntityAdapter : MonoBehaviour
    {
        /// <summary>
        /// Global array of the live transforms backing every entity managed by adapters.
        /// Entries are bound to actual <see cref="Transform"/> handles so an
        /// <see cref="IJobParallelForTransform"/> can read and write them in parallel.
        /// </summary>
        internal static TransformAccessArray Transforms;

        /// <summary>
        /// Entity-per-slot mapping kept parallel to <see cref="Transforms"/>: <c>Entities[i]</c> is the
        /// entity whose transform is <c>Transforms[i]</c>. Job-accessible so a parallel-for-transform
        /// job can recover the entity for the transform it is processing.
        /// </summary>
        internal static NativeList<Entity> Entities;

        /// <summary>
        /// Main-thread bookkeeping kept parallel to <see cref="Transforms"/>: the adapter owning each
        /// slot. Used to repoint a moved adapter's cached slot after a swap-back removal.
        /// </summary>
        private static readonly List<EntityAdapter> s_Owners = new();

        /// <summary>
        /// Persistent scratch buffer reused each frame to gather component snapshots for the writeback
        /// job. Grown on demand; index matches the transform slot.
        /// </summary>
        private static NativeArray<TransformSnapshot> s_Snapshots;

        /// <summary>
        /// Scatter groups keyed by the <see cref="IEntityQuery"/> each adapter resolves (one per
        /// repository). Each group keeps an entity-index → transform-slot map so the per-frame
        /// query-push can write component columns straight into <see cref="s_Snapshots"/> without a
        /// per-entity record lookup. Entity indices collide across repositories, so the maps must stay
        /// partitioned by query rather than shared globally.
        /// </summary>
        private static readonly Dictionary<IEntityQuery, QueryGroup> s_Groups = new();

        /// <summary>
        /// When true, the per-frame scatter runs as chained Burst jobs (<see cref="ScatterJob{T}"/>);
        /// when false it runs inline on the main thread. Exposed so the two strategies can be compared
        /// at runtime. Toggling takes effect on the next <see cref="SyncTransforms"/>.
        /// </summary>
        public static bool BurstScatter = true;

        /// <summary>
        /// The entity index.
        /// </summary>
        [SerializeField, Enhance.ReadOnly] private int m_Index;

        /// <summary>
        /// The entity version.
        /// </summary>
        [SerializeField, Enhance.ReadOnly] private uint m_Version;

        [SerializeField] public SerializedReference<IContext> m_Context;

        /// <summary>
        /// Serialized component types for entity construction.
        /// </summary>
        [SerializeField] private ComponentSet m_Components;

        /// <summary>
        /// The resolved context instance for this entity.
        /// </summary>
        public IContext Context => m_SerializedContext ?? m_Context.Value;

        private IContext m_SerializedContext;

        /// <summary>
        /// The command used to create the entity, cached so it can be deleted on destroy.
        /// </summary>
        private IEntityCommand m_Command;

        /// <summary>
        /// The query used to read this entity's components back during the per-frame transform sync.
        /// </summary>
        private IEntityQuery m_Query;

        /// <summary>
        /// The canonical transform components and axis-subset variants this entity carries, cached so
        /// the writeback gather can dispatch reads without per-frame type checks.
        /// </summary>
        private TransformVariants m_Variants;

        /// <summary>
        /// This adapter's slot in <see cref="Transforms"/>/<see cref="Entities"/>, or -1 when unregistered.
        /// </summary>
        private int m_TransformSlot = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            // Reset defensively so a session started with domain reload disabled does not inherit
            // containers/owners from the previous run.
            DisposeRegistry();

            Transforms = new TransformAccessArray(0);
            Entities   = new NativeList<Entity>(Allocator.Persistent);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += OnStateChanged;
#endif
        }

        /// <summary>
        /// Disposes the global registry containers and clears owner bookkeeping.
        /// </summary>
        private static void DisposeRegistry()
        {
            if (Transforms.isCreated)
            {
                Transforms.Dispose();
            }

            if (Entities.IsCreated)
            {
                Entities.Dispose();
            }

            if (s_Snapshots.IsCreated)
            {
                s_Snapshots.Dispose();
            }

            s_Owners.Clear();

            foreach (var group in s_Groups.Values)
            {
                group.Dispose();
            }

            s_Groups.Clear();
        }
#if UNITY_EDITOR
        /// <summary>
        /// Disposes the global registry when exiting play mode in the editor.
        /// </summary>
        /// <param name="obj">The play mode state change event.</param>
        private static void OnStateChanged(UnityEditor.PlayModeStateChange obj)
        {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
            {
                DisposeRegistry();
            }
        }
#endif

        /// <summary>
        /// Configures this adapter from code before activation, for spawning entities at runtime
        /// (tests/benchmarks) instead of via the inspector. Call on an inactive GameObject so the values
        /// are in place when <see cref="Awake"/> runs.
        /// </summary>
        /// <param name="context">The context resolving <see cref="IEntityCommand"/>/<see cref="IEntityQuery"/>.</param>
        /// <param name="components">The component instances for the entity (transform components are reseeded from the GameObject).</param>
        public void Configure(IContext context, params IComponent[] components)
        {
            m_Context ??= new SerializedReference<IContext>();
            m_Context.Value        = context;
            m_Components.Components = components;
        }

        /// <summary>
        /// Unity Awake callback. Initializes the entity and registers it in the ECS world.
        /// </summary>
        private void Awake()
        {
            var context = m_Context.Value;

            m_SerializedContext = context ?? throw new ArgumentException("Context is not assigned.");

            var cmd = m_SerializedContext!.Resolve<IEntityCommand>();
            if (cmd == null)
            {
                throw new ArgumentException("No IEntityCommand registered in the context.");
            }

            m_Command = cmd;
            m_Query   = m_SerializedContext.Resolve<IEntityQuery>();

            var count = m_Components.ComponentCount;
            var types = ArrayPool<Type>.Shared.Rent(count);
            try
            {
                for (var i = 0; i < count; i++)
                {
                    var type = m_Components[i].GetType();
                    types[i] = type;

                    // Cache which transform components (canonical and axis-subset) this entity owns so
                    // the writeback gather can read them back without per-frame type checks.
                    m_Variants |= Classify(type);
                }

                // Rebuild the payload from the live components so we never depend on a stale
                // serialized buffer; its byte layout mirrors the type order above.
                var payload = m_Components.BuildPayload();
                var entity = BuildEntity(cmd, payload, types.AsSpan(0, count));
                m_Index   = entity.Index;
                m_Version = entity.Version;
            }
            finally
            {
                ArrayPool<Type>.Shared.Return(types);
            }
        }

        /// <summary>
        /// Builds the entity and registers its transform with the global parallel arrays.
        /// </summary>
        /// <param name="cmd">The entity command interface for insertion.</param>
        /// <param name="payload">The contiguous component data buffer to insert.</param>
        /// <param name="types">The array of component types to be added to the entity.</param>
        private Entity BuildEntity(IEntityCommand cmd, Span<byte> payload, Span<Type> types)
        {
            // Seed the built-in transform components from this GameObject's transform so the entity
            // starts in sync with the scene; other components keep their serialized values.
            BindTransform(payload, types);

            var entity = cmd.Insert(payload, types);

            // TransformAccess only carries data while bound to a real Transform inside a
            // TransformAccessArray, so register the live transform rather than a value snapshot.
            // Keep Entities/s_Owners aligned with the same slot for later lookup and removal.
            m_TransformSlot = Transforms.length;
            Transforms.Add(transform);
            Entities.Add(entity);
            s_Owners.Add(this);

            // Join the scatter group for this repository so the per-frame push can resolve this
            // entity's transform slot by entity index. Adapters without a query supply no data, so the
            // slot simply stays untouched by the writeback.
            if (m_Query != null)
            {
                if (!s_Groups.TryGetValue(m_Query, out var group))
                {
                    group = new QueryGroup(m_Query);
                    s_Groups.Add(m_Query, group);
                }

                group.ActiveVariants |= m_Variants;
                group.MemberCount++;
                group.Map(entity.Index, m_TransformSlot);
            }

            return entity;
        }

        /// <summary>
        /// Overwrites the payload slices of any built-in transform components (Position/Rotation/Scale
        /// and their axis subsets) with values derived from this GameObject's local transform. The walk
        /// mirrors the layout produced by <see cref="ComponentSet.BuildPayload"/>, so offsets stay aligned.
        /// </summary>
        /// <param name="payload">The contiguous component data buffer to patch in place.</param>
        /// <param name="types">The component types, in payload order.</param>
        private void BindTransform(Span<byte> payload, ReadOnlySpan<Type> types)
        {
            var self = transform;
            var pos = self.localPosition;
            var euler = self.localEulerAngles;
            var quat = self.localRotation;
            var scale = self.localScale;

            var offset = 0;
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var size = (int) Native.GetStackSize(type);
                WriteTransformComponent(type, payload.Slice(offset, size), pos, euler, quat, scale);
                offset += size;
            }
        }

        /// <summary>
        /// Writes the transform-derived value for a single built-in component type into its payload slice.
        /// Unrecognized (user-defined) component types are left untouched.
        /// </summary>
        /// <param name="type">The component type occupying <paramref name="destination"/>.</param>
        /// <param name="destination">The payload slice for this component.</param>
        /// <param name="position">The transform's local position.</param>
        /// <param name="euler">The transform's local Euler angles, in degrees.</param>
        /// <param name="rotation">The transform's local rotation as a quaternion.</param>
        /// <param name="scale">The transform's local scale.</param>
        private static void WriteTransformComponent(Type type, Span<byte> destination, Vector3 position, Vector3 euler, Quaternion rotation, Vector3 scale)
        {
            if (type == typeof(Position))
            {
                var v = new Position {X = position.x, Y = position.y, Z = position.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionX))
            {
                var v = new PositionX {Value = position.x};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionY))
            {
                var v = new PositionY {Value = position.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionZ))
            {
                var v = new PositionZ {Value = position.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionXY))
            {
                var v = new PositionXY {X = position.x, Y = position.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionXZ))
            {
                var v = new PositionXZ {X = position.x, Z = position.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(PositionYZ))
            {
                var v = new PositionYZ {Y = position.y, Z = position.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(Rotation))
            {
                var v = new Rotation {X = euler.x, Y = euler.y, Z = euler.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationX))
            {
                var v = new RotationX {Value = euler.x};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationY))
            {
                var v = new RotationY {Value = euler.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationZ))
            {
                var v = new RotationZ {Value = euler.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationXY))
            {
                var v = new RotationXY {X = euler.x, Y = euler.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationXZ))
            {
                var v = new RotationXZ {X = euler.x, Z = euler.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(RotationYZ))
            {
                var v = new RotationYZ {Y = euler.y, Z = euler.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(Orientation))
            {
                var v = new Orientation {X = rotation.x, Y = rotation.y, Z = rotation.z, W = rotation.w};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(Scale))
            {
                var v = new Scale {X = scale.x, Y = scale.y, Z = scale.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleX))
            {
                var v = new ScaleX {Value = scale.x};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleY))
            {
                var v = new ScaleY {Value = scale.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleZ))
            {
                var v = new ScaleZ {Value = scale.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleXY))
            {
                var v = new ScaleXY {X = scale.x, Y = scale.y};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleXZ))
            {
                var v = new ScaleXZ {X = scale.x, Z = scale.z};
                MemoryMarshal.Write(destination, ref v);
            }
            else if (type == typeof(ScaleYZ))
            {
                var v = new ScaleYZ {Y = scale.y, Z = scale.z};
                MemoryMarshal.Write(destination, ref v);
            }
        }

        /// <summary>
        /// Unity OnDestroy callback. Deletes the entity and removes its transform registration,
        /// compacting the global parallel arrays via swap-back.
        /// </summary>
        private void OnDestroy()
        {
            // Nothing was registered, or the global registry was already torn down on play-mode exit.
            if (m_TransformSlot < 0 || !Transforms.isCreated)
            {
                return;
            }

            m_Command?.Delete(new Entity {Index = m_Index, Version = m_Version});

            var slot = m_TransformSlot;
            var last = Transforms.length - 1;

            // Leave this entity's scatter group before the swap-back repoints slots below.
            if (m_Query != null && s_Groups.TryGetValue(m_Query, out var myGroup))
            {
                if (myGroup.SlotMap.IsCreated && m_Index < myGroup.SlotMap.Length)
                {
                    myGroup.SlotMap[m_Index] = -1;
                }

                if (--myGroup.MemberCount == 0)
                {
                    myGroup.Dispose();
                    s_Groups.Remove(m_Query);
                }
            }

            Transforms.RemoveAtSwapBack(slot);
            Entities.RemoveAtSwapBack(slot);

            // The former last element now occupies 'slot'; repoint its owner so the cached slot stays valid.
            var moved = s_Owners[last];
            s_Owners[slot] = moved;
            s_Owners.RemoveAt(last);
            moved.m_TransformSlot = slot;

            // Repoint the moved adapter's slot in its own group map. Skipped when this was the last slot
            // (then 'moved' is this adapter, whose entry was just cleared above).
            if (slot != last && moved.m_Query != null && s_Groups.TryGetValue(moved.m_Query, out var movedGroup) && movedGroup.SlotMap.IsCreated)
            {
                movedGroup.SlotMap[moved.m_Index] = slot;
            }

            m_TransformSlot = -1;
        }

        /// <summary>
        /// Pushes each repository's transform-component columns into the per-slot snapshots, then writes
        /// them back onto the bound transforms in parallel via <see cref="TransformSyncJob"/>. Driven
        /// once per frame by <see cref="EntityTransformSyncRunner"/>.
        /// </summary>
        /// <remarks>
        /// Rather than reading each entity's record one at a time, this iterates the SoA component
        /// snippets of one query per active variant and scatters them into <see cref="s_Snapshots"/> by
        /// transform slot. Both the canonical <see cref="Position"/>/<see cref="Rotation"/>/
        /// <see cref="Scale"/> components, their axis-subset variants and the quaternion
        /// <see cref="Orientation"/> are synced back; absent axes leave the transform untouched.
        /// </remarks>
        internal static void SyncTransforms()
        {
            if (!Transforms.isCreated)
            {
                return;
            }

            var count = Transforms.length;
            if (count == 0)
            {
                return;
            }

            EnsureSnapshotCapacity(count);

            // Zero the active slots in one sequential memset so transforms whose entity supplies no data
            // this frame are left untouched by the writeback (Flags == 0). The scatter re-populates the
            // live slots; this is much cheaper than a strided per-slot read-modify-write.
            ClearSnapshots(count);

            if (BurstScatter)
            {
                // Chain a scatter job per active variant: variants serialize (an entity may carry both
                // Position and Rotation → same slot), but each variant's segments run in parallel.
                var dependency = default(JobHandle);
                foreach (var group in s_Groups.Values)
                {
                    dependency = ScatterGroupBurst(group, dependency);
                }

                new TransformSyncJob {Snapshots = s_Snapshots}.Schedule(Transforms, dependency).Complete();
            }
            else
            {
                foreach (var group in s_Groups.Values)
                {
                    ScatterGroupMain(group);
                }

                new TransformSyncJob {Snapshots = s_Snapshots}.Schedule(Transforms).Complete();
            }
        }

        /// <summary>
        /// Ensures <see cref="s_Snapshots"/> can hold at least <paramref name="count"/> entries.
        /// </summary>
        /// <param name="count">The required capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EnsureSnapshotCapacity(int count)
        {
            if (s_Snapshots.IsCreated && s_Snapshots.Length >= count)
            {
                return;
            }

            if (s_Snapshots.IsCreated)
            {
                s_Snapshots.Dispose();
            }

            s_Snapshots = new NativeArray<TransformSnapshot>(count, Allocator.Persistent);
        }

        /// <summary>
        /// Zeroes the first <paramref name="count"/> snapshots with a single sequential memset, clearing
        /// every channel and its flags so unwritten slots are skipped by the writeback.
        /// </summary>
        private static unsafe void ClearSnapshots(int count)
        {
            UnsafeUtility.MemClear(
                NativeArrayUnsafeUtility.GetUnsafePtr(s_Snapshots),
                (long) count * UnsafeUtility.SizeOf<TransformSnapshot>());
        }

        /// <summary>
        /// Main-thread scatter: runs one query per active variant and writes each column into
        /// <see cref="s_Snapshots"/> inline. A snapshot may be touched by several passes (e.g. an entity
        /// carrying both Position and Rotation), which is fine since the writes are sequential here.
        /// </summary>
        private static void ScatterGroupMain(QueryGroup group)
        {
            var a = group.ActiveVariants;

            if ((a & TransformVariants.Position)    != 0) ScatterComponentMain(group, s_CtxPosition);
            if ((a & TransformVariants.PositionX)   != 0) ScatterComponentMain(group, s_CtxPositionX);
            if ((a & TransformVariants.PositionY)   != 0) ScatterComponentMain(group, s_CtxPositionY);
            if ((a & TransformVariants.PositionZ)   != 0) ScatterComponentMain(group, s_CtxPositionZ);
            if ((a & TransformVariants.PositionXY)  != 0) ScatterComponentMain(group, s_CtxPositionXY);
            if ((a & TransformVariants.PositionXZ)  != 0) ScatterComponentMain(group, s_CtxPositionXZ);
            if ((a & TransformVariants.PositionYZ)  != 0) ScatterComponentMain(group, s_CtxPositionYZ);
            if ((a & TransformVariants.Rotation)    != 0) ScatterComponentMain(group, s_CtxRotation);
            if ((a & TransformVariants.RotationX)   != 0) ScatterComponentMain(group, s_CtxRotationX);
            if ((a & TransformVariants.RotationY)   != 0) ScatterComponentMain(group, s_CtxRotationY);
            if ((a & TransformVariants.RotationZ)   != 0) ScatterComponentMain(group, s_CtxRotationZ);
            if ((a & TransformVariants.RotationXY)  != 0) ScatterComponentMain(group, s_CtxRotationXY);
            if ((a & TransformVariants.RotationXZ)  != 0) ScatterComponentMain(group, s_CtxRotationXZ);
            if ((a & TransformVariants.RotationYZ)  != 0) ScatterComponentMain(group, s_CtxRotationYZ);
            if ((a & TransformVariants.Orientation) != 0) ScatterComponentMain(group, s_CtxOrientation);
            if ((a & TransformVariants.Scale)       != 0) ScatterComponentMain(group, s_CtxScale);
            if ((a & TransformVariants.ScaleX)      != 0) ScatterComponentMain(group, s_CtxScaleX);
            if ((a & TransformVariants.ScaleY)      != 0) ScatterComponentMain(group, s_CtxScaleY);
            if ((a & TransformVariants.ScaleZ)      != 0) ScatterComponentMain(group, s_CtxScaleZ);
            if ((a & TransformVariants.ScaleXY)     != 0) ScatterComponentMain(group, s_CtxScaleXY);
            if ((a & TransformVariants.ScaleXZ)     != 0) ScatterComponentMain(group, s_CtxScaleXZ);
            if ((a & TransformVariants.ScaleYZ)     != 0) ScatterComponentMain(group, s_CtxScaleYZ);
        }

        /// <summary>
        /// Burst scatter: schedules one <see cref="ScatterJob{T}"/> per active variant, chained after
        /// <paramref name="dependency"/> so variants serialize while each variant's segments run in
        /// parallel. Returns the combined handle for the next group / the writeback.
        /// </summary>
        private static JobHandle ScatterGroupBurst(QueryGroup group, JobHandle dependency)
        {
            var a = group.ActiveVariants;

            if ((a & TransformVariants.Position)    != 0) dependency = ScatterComponentBurst(group, s_CtxPosition,    dependency);
            if ((a & TransformVariants.PositionX)   != 0) dependency = ScatterComponentBurst(group, s_CtxPositionX,   dependency);
            if ((a & TransformVariants.PositionY)   != 0) dependency = ScatterComponentBurst(group, s_CtxPositionY,   dependency);
            if ((a & TransformVariants.PositionZ)   != 0) dependency = ScatterComponentBurst(group, s_CtxPositionZ,   dependency);
            if ((a & TransformVariants.PositionXY)  != 0) dependency = ScatterComponentBurst(group, s_CtxPositionXY,  dependency);
            if ((a & TransformVariants.PositionXZ)  != 0) dependency = ScatterComponentBurst(group, s_CtxPositionXZ,  dependency);
            if ((a & TransformVariants.PositionYZ)  != 0) dependency = ScatterComponentBurst(group, s_CtxPositionYZ,  dependency);
            if ((a & TransformVariants.Rotation)    != 0) dependency = ScatterComponentBurst(group, s_CtxRotation,    dependency);
            if ((a & TransformVariants.RotationX)   != 0) dependency = ScatterComponentBurst(group, s_CtxRotationX,   dependency);
            if ((a & TransformVariants.RotationY)   != 0) dependency = ScatterComponentBurst(group, s_CtxRotationY,   dependency);
            if ((a & TransformVariants.RotationZ)   != 0) dependency = ScatterComponentBurst(group, s_CtxRotationZ,   dependency);
            if ((a & TransformVariants.RotationXY)  != 0) dependency = ScatterComponentBurst(group, s_CtxRotationXY,  dependency);
            if ((a & TransformVariants.RotationXZ)  != 0) dependency = ScatterComponentBurst(group, s_CtxRotationXZ,  dependency);
            if ((a & TransformVariants.RotationYZ)  != 0) dependency = ScatterComponentBurst(group, s_CtxRotationYZ,  dependency);
            if ((a & TransformVariants.Orientation) != 0) dependency = ScatterComponentBurst(group, s_CtxOrientation, dependency);
            if ((a & TransformVariants.Scale)       != 0) dependency = ScatterComponentBurst(group, s_CtxScale,       dependency);
            if ((a & TransformVariants.ScaleX)      != 0) dependency = ScatterComponentBurst(group, s_CtxScaleX,      dependency);
            if ((a & TransformVariants.ScaleY)      != 0) dependency = ScatterComponentBurst(group, s_CtxScaleY,      dependency);
            if ((a & TransformVariants.ScaleZ)      != 0) dependency = ScatterComponentBurst(group, s_CtxScaleZ,      dependency);
            if ((a & TransformVariants.ScaleXY)     != 0) dependency = ScatterComponentBurst(group, s_CtxScaleXY,     dependency);
            if ((a & TransformVariants.ScaleXZ)     != 0) dependency = ScatterComponentBurst(group, s_CtxScaleXZ,     dependency);
            if ((a & TransformVariants.ScaleYZ)     != 0) dependency = ScatterComponentBurst(group, s_CtxScaleYZ,     dependency);

            return dependency;
        }

        /// <summary>
        /// Main-thread variant: queries one component type and writes each value into the owning
        /// entity's transform slot in contiguous SoA order. Entities with no registered adapter
        /// (slot &lt; 0) or outside the map are skipped.
        /// </summary>
        private static void ScatterComponentMain<T>(QueryGroup group, EntityQueryContext<T> context)
            where T : unmanaged, ITransformComponent
        {
            if (!group.SlotMap.IsCreated)
            {
                return;
            }

            using var result = group.Query.GetQueryHandle(context).Query();

            var (entities, components) = result.Batches;
            var map = group.SlotMap;

            for (var segment = 0; segment < entities.SegmentCount; segment++)
            {
                var ids   = entities[segment];
                var comps = components[segment];

                for (var j = 0; j < ids.Length; j++)
                {
                    var index = ids[j].Index;
                    if ((uint) index >= (uint) map.Length)
                    {
                        continue;
                    }

                    var slot = map[index];
                    if (slot < 0)
                    {
                        continue;
                    }

                    var snapshot = s_Snapshots[slot];
                    comps[j].Write(ref snapshot);
                    s_Snapshots[slot] = snapshot;
                }
            }
        }

        /// <summary>
        /// Burst variant: schedules a <see cref="ScatterJob{T}"/> per query segment, chained after
        /// <paramref name="dependency"/> and after each other — all jobs write the shared snapshot array,
        /// so they must be ordered (each is still internally parallel over its chunk). The snippet spans
        /// are aliased as <see cref="NativeArray{T}"/> views over the pinned chunk memory with no copy;
        /// the query result can be disposed immediately because the chunk — not the result — owns the
        /// pin, and all jobs complete within the frame.
        /// </summary>
        private static JobHandle ScatterComponentBurst<T>(QueryGroup group, EntityQueryContext<T> context, JobHandle dependency)
            where T : unmanaged, ITransformComponent
        {
            if (!group.SlotMap.IsCreated)
            {
                return dependency;
            }

            using var result = group.Query.GetQueryHandle(context).Query();

            var (entities, components) = result.Batches;

            for (var segment = 0; segment < entities.SegmentCount; segment++)
            {
                var ids   = entities[segment];
                var count = ids.Length;
                if (count == 0)
                {
                    continue;
                }

                var job = new ScatterJob<T>
                {
                    Entities          = ids.ToNativeArray(),
                    Components        = components[segment].ToNativeArray(),
                    SlotByEntityIndex = group.SlotMap,
                    Snapshots         = s_Snapshots,
                };

                // Every job writes s_Snapshots, and distinct variants may target the same slot, so all
                // jobs must run in sequence. Each is still internally parallel over its chunk's elements.
                dependency = job.Schedule(count, 128, dependency);
            }

            return dependency;
        }

        // Cached single-component query contexts, one per transform variant. Reused every frame so the
        // scatter never allocates a context.
        private static readonly EntityQueryContext<Position>    s_CtxPosition    = new();
        private static readonly EntityQueryContext<PositionX>   s_CtxPositionX   = new();
        private static readonly EntityQueryContext<PositionY>   s_CtxPositionY   = new();
        private static readonly EntityQueryContext<PositionZ>   s_CtxPositionZ   = new();
        private static readonly EntityQueryContext<PositionXY>  s_CtxPositionXY  = new();
        private static readonly EntityQueryContext<PositionXZ>  s_CtxPositionXZ  = new();
        private static readonly EntityQueryContext<PositionYZ>  s_CtxPositionYZ  = new();
        private static readonly EntityQueryContext<Rotation>    s_CtxRotation    = new();
        private static readonly EntityQueryContext<RotationX>   s_CtxRotationX   = new();
        private static readonly EntityQueryContext<RotationY>   s_CtxRotationY   = new();
        private static readonly EntityQueryContext<RotationZ>   s_CtxRotationZ   = new();
        private static readonly EntityQueryContext<RotationXY>  s_CtxRotationXY  = new();
        private static readonly EntityQueryContext<RotationXZ>  s_CtxRotationXZ  = new();
        private static readonly EntityQueryContext<RotationYZ>  s_CtxRotationYZ  = new();
        private static readonly EntityQueryContext<Orientation> s_CtxOrientation = new();
        private static readonly EntityQueryContext<Scale>       s_CtxScale       = new();
        private static readonly EntityQueryContext<ScaleX>      s_CtxScaleX      = new();
        private static readonly EntityQueryContext<ScaleY>      s_CtxScaleY      = new();
        private static readonly EntityQueryContext<ScaleZ>      s_CtxScaleZ      = new();
        private static readonly EntityQueryContext<ScaleXY>     s_CtxScaleXY     = new();
        private static readonly EntityQueryContext<ScaleXZ>     s_CtxScaleXZ     = new();
        private static readonly EntityQueryContext<ScaleYZ>     s_CtxScaleYZ     = new();

        /// <summary>
        /// Per-repository scatter state: the resolved query, an entity-index → transform-slot map (a
        /// native array so the Burst job can read it directly), the union of variants its members carry,
        /// and a member count so the group is dropped — and its map disposed — when empty.
        /// </summary>
        private sealed class QueryGroup
        {
            public readonly IEntityQuery      Query;
            public          NativeArray<int>  SlotMap;
            public          TransformVariants ActiveVariants;
            public          int               MemberCount;

            public QueryGroup(IEntityQuery query) => Query = query;

            /// <summary>Maps an entity index to its transform slot, growing the native map as needed.</summary>
            public void Map(int entityIndex, int slot)
            {
                if (!SlotMap.IsCreated || entityIndex >= SlotMap.Length)
                {
                    var old   = SlotMap.IsCreated ? SlotMap.Length : 0;
                    var size  = Mathf.Max(entityIndex + 1, old == 0 ? 16 : old * 2);
                    var grown = new NativeArray<int>(size, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

                    for (var i = 0; i < size; i++)
                    {
                        grown[i] = i < old ? SlotMap[i] : -1;
                    }

                    if (SlotMap.IsCreated)
                    {
                        SlotMap.Dispose();
                    }

                    SlotMap = grown;
                }

                SlotMap[entityIndex] = slot;
            }

            /// <summary>Releases the native slot map.</summary>
            public void Dispose()
            {
                if (SlotMap.IsCreated)
                {
                    SlotMap.Dispose();
                }
            }
        }

        /// <summary>
        /// Maps a component type to its <see cref="TransformVariants"/> flag, or
        /// <see cref="TransformVariants.None"/> for user-defined components.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TransformVariants Classify(Type type)
        {
            if (type == typeof(Position)) return TransformVariants.Position;
            if (type == typeof(PositionX)) return TransformVariants.PositionX;
            if (type == typeof(PositionY)) return TransformVariants.PositionY;
            if (type == typeof(PositionZ)) return TransformVariants.PositionZ;
            if (type == typeof(PositionXY)) return TransformVariants.PositionXY;
            if (type == typeof(PositionXZ)) return TransformVariants.PositionXZ;
            if (type == typeof(PositionYZ)) return TransformVariants.PositionYZ;
            if (type == typeof(Rotation)) return TransformVariants.Rotation;
            if (type == typeof(RotationX)) return TransformVariants.RotationX;
            if (type == typeof(RotationY)) return TransformVariants.RotationY;
            if (type == typeof(RotationZ)) return TransformVariants.RotationZ;
            if (type == typeof(RotationXY)) return TransformVariants.RotationXY;
            if (type == typeof(RotationXZ)) return TransformVariants.RotationXZ;
            if (type == typeof(RotationYZ)) return TransformVariants.RotationYZ;
            if (type == typeof(Orientation)) return TransformVariants.Orientation;
            if (type == typeof(Scale)) return TransformVariants.Scale;
            if (type == typeof(ScaleX)) return TransformVariants.ScaleX;
            if (type == typeof(ScaleY)) return TransformVariants.ScaleY;
            if (type == typeof(ScaleZ)) return TransformVariants.ScaleZ;
            if (type == typeof(ScaleXY)) return TransformVariants.ScaleXY;
            if (type == typeof(ScaleXZ)) return TransformVariants.ScaleXZ;
            if (type == typeof(ScaleYZ)) return TransformVariants.ScaleYZ;
            return TransformVariants.None;
        }

        /// <summary>
        /// Bitset of the transform components an entity can carry — the canonical
        /// <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/> triples plus their
        /// axis-subset variants — used to drive the per-frame writeback gather.
        /// </summary>
        [Flags]
        private enum TransformVariants : uint
        {
            None        = 0,
            Position    = 1u << 0,
            PositionX   = 1u << 1,
            PositionY   = 1u << 2,
            PositionZ   = 1u << 3,
            PositionXY  = 1u << 4,
            PositionXZ  = 1u << 5,
            PositionYZ  = 1u << 6,
            Rotation    = 1u << 7,
            RotationX   = 1u << 8,
            RotationY   = 1u << 9,
            RotationZ   = 1u << 10,
            RotationXY  = 1u << 11,
            RotationXZ  = 1u << 12,
            RotationYZ  = 1u << 13,
            Scale       = 1u << 14,
            ScaleX      = 1u << 15,
            ScaleY      = 1u << 16,
            ScaleZ      = 1u << 17,
            ScaleXY     = 1u << 18,
            ScaleXZ     = 1u << 19,
            ScaleYZ     = 1u << 20,
            Orientation = 1u << 21,

            AnyPosition = Position | PositionX | PositionY | PositionZ | PositionXY | PositionXZ | PositionYZ,
            AnyRotation = Rotation | RotationX | RotationY | RotationZ | RotationXY | RotationXZ | RotationYZ,
            AnyScale    = Scale | ScaleX | ScaleY | ScaleZ | ScaleXY | ScaleXZ | ScaleYZ
        }
    }
}