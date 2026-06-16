using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Unsafe;
using Unity.Collections;
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

            Transforms.RemoveAtSwapBack(slot);
            Entities.RemoveAtSwapBack(slot);

            // The former last element now occupies 'slot'; repoint its owner so the cached slot stays valid.
            var moved = s_Owners[last];
            s_Owners[slot] = moved;
            s_Owners.RemoveAt(last);
            moved.m_TransformSlot = slot;

            m_TransformSlot = -1;
        }

        /// <summary>
        /// Gathers each registered entity's transform components on the main thread, then writes them
        /// back onto the bound transforms in parallel via <see cref="TransformSyncJob"/>. Driven once
        /// per frame by <see cref="EntityTransformSyncRunner"/>.
        /// </summary>
        /// <remarks>
        /// Both the canonical <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/>
        /// components and their axis-subset variants are synced back; absent axes leave the
        /// corresponding transform component untouched.
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

            for (var i = 0; i < count; i++)
            {
                s_Snapshots[i] = s_Owners[i].CaptureSnapshot();
            }

            new TransformSyncJob {Snapshots = s_Snapshots}.Schedule(Transforms).Complete();
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
        /// Reads this entity's transform components from its repository into a snapshot, recording one
        /// flag per axis so the writeback job merges only the axes this entity actually owns. Returns
        /// an empty snapshot (no flags) when the handle is stale or no query is available, so the
        /// writeback job leaves the transform untouched.
        /// </summary>
        /// <returns>The captured transform snapshot.</returns>
        private TransformSnapshot CaptureSnapshot()
        {
            var snapshot = default(TransformSnapshot);

            if (m_Query == null || !m_Query.Contains(m_Index))
            {
                return snapshot;
            }

            var record = m_Query.Get(m_Index);

            // Guard against a stale handle whose index has been reused by a different entity.
            if (record.Entity.Version != m_Version)
            {
                return snapshot;
            }

            var chunk = record.Chunk;
            var index = record.IndexInChunk;

            if ((m_Variants & TransformVariants.AnyPosition) != 0) CapturePosition(chunk, index, ref snapshot);
            if ((m_Variants & TransformVariants.AnyRotation) != 0) CaptureRotation(chunk, index, ref snapshot);
            if ((m_Variants & TransformVariants.Orientation) != 0) CaptureOrientation(chunk, index, ref snapshot);
            if ((m_Variants & TransformVariants.AnyScale) != 0) CaptureScale(chunk, index, ref snapshot);
            return snapshot;
        }

        /// <summary>
        /// Reads whichever position variants this entity owns into the snapshot's position channel,
        /// flagging only the axes each variant supplies.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CapturePosition(Chunk chunk, int index, ref TransformSnapshot snapshot)
        {
            var variants = m_Variants;

            if ((variants & TransformVariants.Position) != 0)
            {
                ref var c = ref chunk.Get<Position>(index);
                snapshot.Position =  new Vector3(c.X, c.Y, c.Z);
                snapshot.Flags    |= TransformSnapshot.HAS_POSITION;
            }

            if ((variants & TransformVariants.PositionX) != 0)
            {
                snapshot.Position.x =  chunk.Get<PositionX>(index).Value;
                snapshot.Flags      |= TransformSnapshot.POS_X;
            }

            if ((variants & TransformVariants.PositionY) != 0)
            {
                snapshot.Position.y =  chunk.Get<PositionY>(index).Value;
                snapshot.Flags      |= TransformSnapshot.POS_Y;
            }

            if ((variants & TransformVariants.PositionZ) != 0)
            {
                snapshot.Position.z =  chunk.Get<PositionZ>(index).Value;
                snapshot.Flags      |= TransformSnapshot.POS_Z;
            }

            if ((variants & TransformVariants.PositionXY) != 0)
            {
                ref var c = ref chunk.Get<PositionXY>(index);
                snapshot.Position.x =  c.X;
                snapshot.Position.y =  c.Y;
                snapshot.Flags      |= TransformSnapshot.POS_X | TransformSnapshot.POS_Y;
            }

            if ((variants & TransformVariants.PositionXZ) != 0)
            {
                ref var c = ref chunk.Get<PositionXZ>(index);
                snapshot.Position.x =  c.X;
                snapshot.Position.z =  c.Z;
                snapshot.Flags      |= TransformSnapshot.POS_X | TransformSnapshot.POS_Z;
            }

            if ((variants & TransformVariants.PositionYZ) != 0)
            {
                ref var c = ref chunk.Get<PositionYZ>(index);
                snapshot.Position.y =  c.Y;
                snapshot.Position.z =  c.Z;
                snapshot.Flags      |= TransformSnapshot.POS_Y | TransformSnapshot.POS_Z;
            }
        }

        /// <summary>
        /// Reads whichever rotation variants this entity owns into the snapshot's Euler channel,
        /// flagging only the axes each variant supplies.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CaptureRotation(Chunk chunk, int index, ref TransformSnapshot snapshot)
        {
            var variants = m_Variants;

            if ((variants & TransformVariants.Rotation) != 0)
            {
                ref var c = ref chunk.Get<Rotation>(index);
                snapshot.Euler =  new Vector3(c.X, c.Y, c.Z);
                snapshot.Flags |= TransformSnapshot.HAS_ROTATION;
            }

            if ((variants & TransformVariants.RotationX) != 0)
            {
                snapshot.Euler.x =  chunk.Get<RotationX>(index).Value;
                snapshot.Flags   |= TransformSnapshot.ROT_X;
            }

            if ((variants & TransformVariants.RotationY) != 0)
            {
                snapshot.Euler.y =  chunk.Get<RotationY>(index).Value;
                snapshot.Flags   |= TransformSnapshot.ROT_Y;
            }

            if ((variants & TransformVariants.RotationZ) != 0)
            {
                snapshot.Euler.z =  chunk.Get<RotationZ>(index).Value;
                snapshot.Flags   |= TransformSnapshot.ROT_Z;
            }

            if ((variants & TransformVariants.RotationXY) != 0)
            {
                ref var c = ref chunk.Get<RotationXY>(index);
                snapshot.Euler.x =  c.X;
                snapshot.Euler.y =  c.Y;
                snapshot.Flags   |= TransformSnapshot.ROT_X | TransformSnapshot.ROT_Y;
            }

            if ((variants & TransformVariants.RotationXZ) != 0)
            {
                ref var c = ref chunk.Get<RotationXZ>(index);
                snapshot.Euler.x =  c.X;
                snapshot.Euler.z =  c.Z;
                snapshot.Flags   |= TransformSnapshot.ROT_X | TransformSnapshot.ROT_Z;
            }

            if ((variants & TransformVariants.RotationYZ) != 0)
            {
                ref var c = ref chunk.Get<RotationYZ>(index);
                snapshot.Euler.y =  c.Y;
                snapshot.Euler.z =  c.Z;
                snapshot.Flags   |= TransformSnapshot.ROT_Y | TransformSnapshot.ROT_Z;
            }
        }

        /// <summary>
        /// Reads the quaternion <see cref="Orientation"/> component into the snapshot. Written to the
        /// transform verbatim, so it overrides any Euler rotation axes during writeback.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CaptureOrientation(Chunk chunk, int index, ref TransformSnapshot snapshot)
        {
            ref var c = ref chunk.Get<Orientation>(index);
            snapshot.Orientation =  new Quaternion(c.X, c.Y, c.Z, c.W);
            snapshot.Flags       |= TransformSnapshot.HAS_ORIENTATION;
        }

        /// <summary>
        /// Reads whichever scale variants this entity owns into the snapshot's scale channel,
        /// flagging only the axes each variant supplies.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CaptureScale(Chunk chunk, int index, ref TransformSnapshot snapshot)
        {
            var variants = m_Variants;

            if ((variants & TransformVariants.Scale) != 0)
            {
                ref var c = ref chunk.Get<Scale>(index);
                snapshot.Scale =  new Vector3(c.X, c.Y, c.Z);
                snapshot.Flags |= TransformSnapshot.HAS_SCALE;
            }

            if ((variants & TransformVariants.ScaleX) != 0)
            {
                snapshot.Scale.x =  chunk.Get<ScaleX>(index).Value;
                snapshot.Flags   |= TransformSnapshot.SCL_X;
            }

            if ((variants & TransformVariants.ScaleY) != 0)
            {
                snapshot.Scale.y =  chunk.Get<ScaleY>(index).Value;
                snapshot.Flags   |= TransformSnapshot.SCL_Y;
            }

            if ((variants & TransformVariants.ScaleZ) != 0)
            {
                snapshot.Scale.z =  chunk.Get<ScaleZ>(index).Value;
                snapshot.Flags   |= TransformSnapshot.SCL_Z;
            }

            if ((variants & TransformVariants.ScaleXY) != 0)
            {
                ref var c = ref chunk.Get<ScaleXY>(index);
                snapshot.Scale.x =  c.X;
                snapshot.Scale.y =  c.Y;
                snapshot.Flags   |= TransformSnapshot.SCL_X | TransformSnapshot.SCL_Y;
            }

            if ((variants & TransformVariants.ScaleXZ) != 0)
            {
                ref var c = ref chunk.Get<ScaleXZ>(index);
                snapshot.Scale.x =  c.X;
                snapshot.Scale.z =  c.Z;
                snapshot.Flags   |= TransformSnapshot.SCL_X | TransformSnapshot.SCL_Z;
            }

            if ((variants & TransformVariants.ScaleYZ) != 0)
            {
                ref var c = ref chunk.Get<ScaleYZ>(index);
                snapshot.Scale.y =  c.Y;
                snapshot.Scale.z =  c.Z;
                snapshot.Flags   |= TransformSnapshot.SCL_Y | TransformSnapshot.SCL_Z;
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