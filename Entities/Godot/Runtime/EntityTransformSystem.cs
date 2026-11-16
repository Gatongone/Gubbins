#if GUBBINS_ENABLED
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// Something an <see cref="EntityTransformSystem"/> slot writes back to: a bound scene node (2D or 3D)
/// plus the registry bookkeeping the swap-back removal needs. Implemented by <see cref="EntityAdapter3D"/>
/// and <see cref="EntityAdapter2D"/> so the dimension-agnostic system can drive either without knowing
/// whether it holds a <see cref="Node2D"/> or a <see cref="Node3D"/>.
/// </summary>
internal interface ITransformBinding
{
    /// <summary>This binding's slot in the registry, or -1 when unregistered. Set by the system.</summary>
    int TransformSlot { get; set; }

    /// <summary>The entity's index, used to repoint the query slot map on removal.</summary>
    int EntityIndex { get; }

    /// <summary>The query whose scatter group this binding belongs to, or null.</summary>
    IEntityQuery Query { get; }

    /// <summary>Writes the gathered snapshot back onto the bound node's transform.</summary>
    void ApplyTransform(in TransformSnapshot snapshot);
}

/// <summary>
/// Dimension-agnostic core of the entity transform sync: the global slot registry, the per-frame scatter
/// of transform-component columns into per-slot snapshots, and the writeback dispatch. Both
/// <see cref="EntityAdapter3D"/> (3D) and <see cref="EntityAdapter2D"/> (2D) register here and receive their
/// writeback through <see cref="ITransformBinding"/>, so all the shared ECS machinery lives in one place
/// while each node type only supplies how it seeds and applies its own transform.
/// </summary>
/// <remarks>
/// Godot has no Burst/Jobs, so the scatter + writeback run single-threaded on the main thread (the Unity
/// "main-thread" scatter path); the registry uses managed lists rather than <c>TransformAccessArray</c>/
/// <c>NativeList</c>.
/// </remarks>
internal static class EntityTransformSystem
{
    /// <summary>
    /// The live bindings, one per slot. <c>s_Slots[i]</c> is both the node written to and the owner
    /// repointed after a swap-back removal.
    /// </summary>
    private static readonly List<ITransformBinding> s_Slots = new();

    /// <summary>
    /// Persistent scratch buffer reused each frame to gather component snapshots for the writeback. Grown
    /// on demand; index matches the slot.
    /// </summary>
    private static TransformSnapshot[] s_Snapshots;

    /// <summary>
    /// Scatter groups keyed by the <see cref="IEntityQuery"/> each binding resolves (one per repository).
    /// Each group keeps an entity-index → slot map so the per-frame scatter can write component columns
    /// straight into <see cref="s_Snapshots"/> without a per-entity record lookup. Entity indices collide
    /// across repositories, so the maps stay partitioned by query rather than shared globally.
    /// </summary>
    private static readonly Dictionary<IEntityQuery, QueryGroup> s_Groups = new();

    /// <summary>
    /// Registers a binding into a fresh slot and joins its repository's scatter group. Sets
    /// <see cref="ITransformBinding.TransformSlot"/> on the binding. Bindings without a query supply no
    /// data, so their slot simply stays untouched by the writeback.
    /// </summary>
    internal static void Register(ITransformBinding binding, Entity entity, IEntityQuery query, TransformVariants variants)
    {
        binding.TransformSlot = s_Slots.Count;
        s_Slots.Add(binding);

        if (query == null)
        {
            return;
        }

        if (!s_Groups.TryGetValue(query, out var group))
        {
            group = new QueryGroup(query);
            s_Groups.Add(query, group);
        }

        group.ActiveVariants |= variants;
        group.MemberCount++;
        group.Map(entity.Index, binding.TransformSlot);
    }

    /// <summary>
    /// Removes a binding, compacting the slot list via swap-back and repointing the moved binding's slot
    /// (both in the registry and in its query's slot map).
    /// </summary>
    internal static void Unregister(ITransformBinding binding)
    {
        var slot = binding.TransformSlot;
        if (slot < 0)
        {
            return;
        }

        var last = s_Slots.Count - 1;

        // Leave this binding's scatter group before the swap-back repoints slots below.
        if (binding.Query != null && s_Groups.TryGetValue(binding.Query, out var myGroup))
        {
            if (myGroup.SlotMap != null && binding.EntityIndex < myGroup.SlotMap.Length)
            {
                myGroup.SlotMap[binding.EntityIndex] = -1;
            }

            if (--myGroup.MemberCount == 0)
            {
                s_Groups.Remove(binding.Query);
            }
        }

        // The former last element now occupies 'slot'; repoint its owner so the cached slot stays valid.
        var moved = s_Slots[last];
        s_Slots[slot] = moved;
        s_Slots.RemoveAt(last);
        moved.TransformSlot = slot;

        // Repoint the moved binding's slot in its own group map. Skipped when this was the last slot (then
        // 'moved' is this binding, whose entry was just cleared above).
        if (slot != last && moved.Query != null && s_Groups.TryGetValue(moved.Query, out var movedGroup) && movedGroup.SlotMap != null)
        {
            movedGroup.SlotMap[moved.EntityIndex] = slot;
        }

        binding.TransformSlot = -1;
    }

    /// <summary>
    /// Pushes each repository's transform-component columns into the per-slot snapshots, then writes them
    /// back onto the bound nodes. Driven once per frame by <see cref="EntityTransformSync"/>.
    /// </summary>
    /// <remarks>
    /// Rather than reading each entity's record one at a time, this iterates the SoA component snippets of
    /// one query per active variant and scatters them into <see cref="s_Snapshots"/> by slot. Both the
    /// canonical <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/> components, their
    /// axis-subset variants and the quaternion <see cref="Orientation"/> are synced back; absent axes leave
    /// the transform untouched.
    /// </remarks>
    internal static void SyncTransforms()
    {
        var count = s_Slots.Count;
        if (count == 0)
        {
            return;
        }

        EnsureSnapshotCapacity(count);

        // Clear the active slots so nodes whose entity supplies no data this frame are left untouched by
        // the writeback (Flags == 0). The scatter re-populates the live slots.
        Array.Clear(s_Snapshots, 0, count);

        foreach (var group in s_Groups.Values)
        {
            ScatterGroupMain(group);
        }

        for (var slot = 0; slot < count; slot++)
        {
            if (s_Snapshots[slot].Flags == 0)
            {
                continue;
            }

            s_Slots[slot].ApplyTransform(s_Snapshots[slot]);
        }
    }

    /// <summary>Ensures <see cref="s_Snapshots"/> can hold at least <paramref name="count"/> entries.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void EnsureSnapshotCapacity(int count)
    {
        if (s_Snapshots != null && s_Snapshots.Length >= count)
        {
            return;
        }

        var size = System.Math.Max(count, (s_Snapshots?.Length ?? 0) * 2);
        s_Snapshots = new TransformSnapshot[size];
    }

    /// <summary>
    /// Overwrites the payload slices of any built-in transform components (Position/Rotation/Scale and
    /// their axis subsets) with values derived from a node's local transform, expressed as the four source
    /// vectors. The walk mirrors the layout produced by <see cref="ComponentSet.BuildPayload"/>, so offsets
    /// stay aligned. Callers supply the vectors from their own node type: a <see cref="Node3D"/> passes its
    /// 3D transform verbatim, a <see cref="Node2D"/> lifts its 2D transform into the XY plane.
    /// </summary>
    internal static void Seed(Span<byte> payload, ReadOnlySpan<Type> types, Vector3 position, Vector3 euler, Quaternion rotation, Vector3 scale)
    {
        var offset = 0;
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            var size = (int) Native.GetStackSize(type);
            WriteTransformComponent(type, payload.Slice(offset, size), position, euler, rotation, scale);
            offset += size;
        }
    }

    /// <summary>
    /// Writes the transform-derived value for a single built-in component type into its payload slice.
    /// Unrecognized (user-defined) component types are left untouched.
    /// </summary>
    private static void WriteTransformComponent(Type type, Span<byte> destination, Vector3 position, Vector3 euler, Quaternion rotation, Vector3 scale)
    {
        if (type == typeof(Position))
        {
            var v = new Position {X = position.X, Y = position.Y, Z = position.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionX))
        {
            var v = new PositionX {Value = position.X};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionY))
        {
            var v = new PositionY {Value = position.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionZ))
        {
            var v = new PositionZ {Value = position.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionXY))
        {
            var v = new PositionXY {X = position.X, Y = position.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionXZ))
        {
            var v = new PositionXZ {X = position.X, Z = position.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(PositionYZ))
        {
            var v = new PositionYZ {Y = position.Y, Z = position.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(Rotation))
        {
            var v = new Rotation {X = euler.X, Y = euler.Y, Z = euler.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationX))
        {
            var v = new RotationX {Value = euler.X};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationY))
        {
            var v = new RotationY {Value = euler.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationZ))
        {
            var v = new RotationZ {Value = euler.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationXY))
        {
            var v = new RotationXY {X = euler.X, Y = euler.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationXZ))
        {
            var v = new RotationXZ {X = euler.X, Z = euler.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(RotationYZ))
        {
            var v = new RotationYZ {Y = euler.Y, Z = euler.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(Orientation))
        {
            var v = new Orientation {X = rotation.X, Y = rotation.Y, Z = rotation.Z, W = rotation.W};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(Scale))
        {
            var v = new Scale {X = scale.X, Y = scale.Y, Z = scale.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleX))
        {
            var v = new ScaleX {Value = scale.X};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleY))
        {
            var v = new ScaleY {Value = scale.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleZ))
        {
            var v = new ScaleZ {Value = scale.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleXY))
        {
            var v = new ScaleXY {X = scale.X, Y = scale.Y};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleXZ))
        {
            var v = new ScaleXZ {X = scale.X, Z = scale.Z};
            MemoryMarshal.Write(destination, v);
        }
        else if (type == typeof(ScaleYZ))
        {
            var v = new ScaleYZ {Y = scale.Y, Z = scale.Z};
            MemoryMarshal.Write(destination, v);
        }
    }

    /// <summary>
    /// Runs one query per active variant and writes each column into <see cref="s_Snapshots"/> inline. A
    /// snapshot may be touched by several passes (e.g. an entity carrying both Position and Rotation),
    /// which is fine since the writes are sequential here.
    /// </summary>
    private static void ScatterGroupMain(QueryGroup group)
    {
        var a = group.ActiveVariants;

        if ((a & TransformVariants.Position) != 0)    ScatterComponentMain<Position>(group, s_CtxPosition);
        if ((a & TransformVariants.PositionX) != 0)   ScatterComponentMain<PositionX>(group, s_CtxPositionX);
        if ((a & TransformVariants.PositionY) != 0)   ScatterComponentMain<PositionY>(group, s_CtxPositionY);
        if ((a & TransformVariants.PositionZ) != 0)   ScatterComponentMain<PositionZ>(group, s_CtxPositionZ);
        if ((a & TransformVariants.PositionXY) != 0)  ScatterComponentMain<PositionXY>(group, s_CtxPositionXY);
        if ((a & TransformVariants.PositionXZ) != 0)  ScatterComponentMain<PositionXZ>(group, s_CtxPositionXZ);
        if ((a & TransformVariants.PositionYZ) != 0)  ScatterComponentMain<PositionYZ>(group, s_CtxPositionYZ);
        if ((a & TransformVariants.Rotation) != 0)    ScatterComponentMain<Rotation>(group, s_CtxRotation);
        if ((a & TransformVariants.RotationX) != 0)   ScatterComponentMain<RotationX>(group, s_CtxRotationX);
        if ((a & TransformVariants.RotationY) != 0)   ScatterComponentMain<RotationY>(group, s_CtxRotationY);
        if ((a & TransformVariants.RotationZ) != 0)   ScatterComponentMain<RotationZ>(group, s_CtxRotationZ);
        if ((a & TransformVariants.RotationXY) != 0)  ScatterComponentMain<RotationXY>(group, s_CtxRotationXY);
        if ((a & TransformVariants.RotationXZ) != 0)  ScatterComponentMain<RotationXZ>(group, s_CtxRotationXZ);
        if ((a & TransformVariants.RotationYZ) != 0)  ScatterComponentMain<RotationYZ>(group, s_CtxRotationYZ);
        if ((a & TransformVariants.Orientation) != 0) ScatterComponentMain<Orientation>(group, s_CtxOrientation);
        if ((a & TransformVariants.Scale) != 0)       ScatterComponentMain<Scale>(group, s_CtxScale);
        if ((a & TransformVariants.ScaleX) != 0)      ScatterComponentMain<ScaleX>(group, s_CtxScaleX);
        if ((a & TransformVariants.ScaleY) != 0)      ScatterComponentMain<ScaleY>(group, s_CtxScaleY);
        if ((a & TransformVariants.ScaleZ) != 0)      ScatterComponentMain<ScaleZ>(group, s_CtxScaleZ);
        if ((a & TransformVariants.ScaleXY) != 0)     ScatterComponentMain<ScaleXY>(group, s_CtxScaleXY);
        if ((a & TransformVariants.ScaleXZ) != 0)     ScatterComponentMain<ScaleXZ>(group, s_CtxScaleXZ);
        if ((a & TransformVariants.ScaleYZ) != 0)     ScatterComponentMain<ScaleYZ>(group, s_CtxScaleYZ);
    }

    /// <summary>
    /// Queries one component type and writes each value into the owning entity's slot in contiguous SoA
    /// order. Entities with no registered binding (slot &lt; 0) or outside the map are skipped.
    /// </summary>
    private static void ScatterComponentMain<T>(QueryGroup group, ComponentFilter context) where T : unmanaged, ITransformComponent
    {
        if (group.SlotMap == null)
        {
            return;
        }

        using var chunks     = group.Query.Search(context);
        using var entities   = chunks.GetComponents<Entity>();
        using var components = chunks.GetComponents<T>();

        var map     = group.SlotMap;
        var idSegs  = entities.Segments;
        var valSegs = components.Segments;

        for (var segment = 0; segment < chunks.Count; segment++)
        {
            var ids   = idSegs[segment];
            var comps = valSegs[segment];

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

    // Cached single-component query filters, one per transform variant. Reused every frame so the scatter
    // never allocates a filter.
    private static readonly ComponentFilter s_CtxPosition    = new ComponentFilter().Include<Position>();
    private static readonly ComponentFilter s_CtxPositionX   = new ComponentFilter().Include<PositionX>();
    private static readonly ComponentFilter s_CtxPositionY   = new ComponentFilter().Include<PositionY>();
    private static readonly ComponentFilter s_CtxPositionZ   = new ComponentFilter().Include<PositionZ>();
    private static readonly ComponentFilter s_CtxPositionXY  = new ComponentFilter().Include<PositionXY>();
    private static readonly ComponentFilter s_CtxPositionXZ  = new ComponentFilter().Include<PositionXZ>();
    private static readonly ComponentFilter s_CtxPositionYZ  = new ComponentFilter().Include<PositionYZ>();
    private static readonly ComponentFilter s_CtxRotation    = new ComponentFilter().Include<Rotation>();
    private static readonly ComponentFilter s_CtxRotationX   = new ComponentFilter().Include<RotationX>();
    private static readonly ComponentFilter s_CtxRotationY   = new ComponentFilter().Include<RotationY>();
    private static readonly ComponentFilter s_CtxRotationZ   = new ComponentFilter().Include<RotationZ>();
    private static readonly ComponentFilter s_CtxRotationXY  = new ComponentFilter().Include<RotationXY>();
    private static readonly ComponentFilter s_CtxRotationXZ  = new ComponentFilter().Include<RotationXZ>();
    private static readonly ComponentFilter s_CtxRotationYZ  = new ComponentFilter().Include<RotationYZ>();
    private static readonly ComponentFilter s_CtxOrientation = new ComponentFilter().Include<Orientation>();
    private static readonly ComponentFilter s_CtxScale       = new ComponentFilter().Include<Scale>();
    private static readonly ComponentFilter s_CtxScaleX      = new ComponentFilter().Include<ScaleX>();
    private static readonly ComponentFilter s_CtxScaleY      = new ComponentFilter().Include<ScaleY>();
    private static readonly ComponentFilter s_CtxScaleZ      = new ComponentFilter().Include<ScaleZ>();
    private static readonly ComponentFilter s_CtxScaleXY     = new ComponentFilter().Include<ScaleXY>();
    private static readonly ComponentFilter s_CtxScaleXZ     = new ComponentFilter().Include<ScaleXZ>();
    private static readonly ComponentFilter s_CtxScaleYZ     = new ComponentFilter().Include<ScaleYZ>();

    /// <summary>
    /// Per-repository scatter state: the resolved query, an entity-index → slot map, the union of variants
    /// its members carry, and a member count so the group is dropped when empty.
    /// </summary>
    private sealed class QueryGroup
    {
        public readonly IEntityQuery      Query;
        public          int[]             SlotMap;
        public          TransformVariants ActiveVariants;
        public          int               MemberCount;

        public QueryGroup(IEntityQuery query) => Query = query;

        /// <summary>Maps an entity index to its slot, growing the map as needed.</summary>
        public void Map(int entityIndex, int slot)
        {
            if (SlotMap == null || entityIndex >= SlotMap.Length)
            {
                var old   = SlotMap?.Length ?? 0;
                var size  = System.Math.Max(entityIndex + 1, old == 0 ? 16 : old * 2);
                var grown = new int[size];

                for (var i = 0; i < size; i++)
                {
                    grown[i] = i < old ? SlotMap[i] : -1;
                }

                SlotMap = grown;
            }

            SlotMap[entityIndex] = slot;
        }
    }

    /// <summary>
    /// Maps a component type to its <see cref="TransformVariants"/> flag, or
    /// <see cref="TransformVariants.None"/> for user-defined components.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TransformVariants Classify(Type type)
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
}

/// <summary>
/// Bitset of the transform components an entity can carry — the canonical
/// <see cref="Position"/>/<see cref="Rotation"/>/<see cref="Scale"/> triples plus their axis-subset
/// variants — used to drive the per-frame writeback scatter.
/// </summary>
[Flags]
internal enum TransformVariants : uint
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
#endif
