using System;
using System.Buffers;
using System.Collections.Generic;
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
        /// Whether this entity carries the canonical <see cref="Position"/>/<see cref="Rotation"/>/
        /// <see cref="Scale"/> components, cached so the writeback gather avoids per-frame type checks.
        /// </summary>
        private bool m_HasPosition;
        private bool m_HasRotation;
        private bool m_HasScale;

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

                    // Cache which canonical transform components this entity owns for the writeback sync.
                    if (type == typeof(Position)) m_HasPosition = true;
                    else if (type == typeof(Rotation)) m_HasRotation = true;
                    else if (type == typeof(Scale)) m_HasScale = true;
                }

                // Rebuild the payload from the live components so we never depend on a stale
                // serialized buffer; its byte layout mirrors the type order above.
                var payload = m_Components.BuildPayload();
                var entity  = BuildEntity(cmd, payload, types.AsSpan(0, count));
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
            var self  = transform;
            var pos   = self.localPosition;
            var euler = self.localEulerAngles;
            var scale = self.localScale;

            var offset = 0;
            for (var i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var size = (int) Native.GetStackSize(type);
                WriteTransformComponent(type, payload.Slice(offset, size), pos, euler, scale);
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
        /// <param name="scale">The transform's local scale.</param>
        private static void WriteTransformComponent(Type type, Span<byte> destination, Vector3 position, Vector3 euler, Vector3 scale)
        {
            if (type == typeof(Position))        { var v = new Position   {X = position.x, Y = position.y, Z = position.z}; MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionX))  { var v = new PositionX  {Value = position.x};                            MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionY))  { var v = new PositionY  {Value = position.y};                            MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionZ))  { var v = new PositionZ  {Value = position.z};                            MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionXY)) { var v = new PositionXY {X = position.x, Y = position.y};                MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionXZ)) { var v = new PositionXZ {X = position.x, Z = position.z};                MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(PositionYZ)) { var v = new PositionYZ {Y = position.y, Z = position.z};                MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(Rotation))   { var v = new Rotation   {X = euler.x, Y = euler.y, Z = euler.z};         MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationX))  { var v = new RotationX  {Value = euler.x};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationY))  { var v = new RotationY  {Value = euler.y};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationZ))  { var v = new RotationZ  {Value = euler.z};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationXY)) { var v = new RotationXY {X = euler.x, Y = euler.y};                      MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationXZ)) { var v = new RotationXZ {X = euler.x, Z = euler.z};                      MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(RotationYZ)) { var v = new RotationYZ {Y = euler.y, Z = euler.z};                      MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(Scale))      { var v = new Scale      {X = scale.x, Y = scale.y, Z = scale.z};         MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleX))     { var v = new ScaleX     {Value = scale.x};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleY))     { var v = new ScaleY     {Value = scale.y};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleZ))     { var v = new ScaleZ     {Value = scale.z};                               MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleXY))    { var v = new ScaleXY    {X = scale.x, Y = scale.y};                      MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleXZ))    { var v = new ScaleXZ    {X = scale.x, Z = scale.z};                      MemoryMarshal.Write(destination, ref v); }
            else if (type == typeof(ScaleYZ))    { var v = new ScaleYZ    {Y = scale.y, Z = scale.z};                      MemoryMarshal.Write(destination, ref v); }
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
        /// Only the canonical <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/>
        /// components are synced back; axis-subset variants are seeded at creation but not tracked here.
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
        /// Reads this entity's canonical transform components from its repository into a snapshot.
        /// Returns an empty snapshot (no flags) when the handle is stale or no query is available, so
        /// the writeback job leaves the transform untouched.
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

            if (m_HasPosition)
            {
                ref var position = ref chunk.Get<Position>(index);
                snapshot.Position =  new Vector3(position.X, position.Y, position.Z);
                snapshot.Flags    |= TransformSnapshot.HasPosition;
            }

            if (m_HasRotation)
            {
                ref var rotation = ref chunk.Get<Rotation>(index);
                snapshot.Euler =  new Vector3(rotation.X, rotation.Y, rotation.Z);
                snapshot.Flags |= TransformSnapshot.HasRotation;
            }

            if (m_HasScale)
            {
                ref var scale = ref chunk.Get<Scale>(index);
                snapshot.Scale =  new Vector3(scale.X, scale.Y, scale.Z);
                snapshot.Flags |= TransformSnapshot.HasScale;
            }

            return snapshot;
        }
    }
}