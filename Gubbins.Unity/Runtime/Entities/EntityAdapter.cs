using System;
using System.Buffers;
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
        /// Global list of all entity transforms managed by adapters.
        /// </summary>
        internal static NativeList<TransformAccess> Transforms;

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Transforms = new NativeList<TransformAccess>(Allocator.Persistent);
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
            Transforms.Dispose();
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

            var types = ArrayPool<Type>.Shared.Rent(m_Components.ComponentCount);
            for (var i = 0; i < m_Components.ComponentCount; i++)
            {
                types[i] = m_Components[i].GetType();
            }
            var entity = BuildEntity(cmd, m_Components.Payload, types.AsSpan(0, m_Components.ComponentCount));
            m_Index   = entity.Index;
            m_Version = entity.Version;
            ArrayPool<Type>.Shared.Return(types);
        }

        /// <summary>
        /// Builds the entity and synchronizes transform/component data with the ECS world.
        /// </summary>
        /// <param name="cmd">The entity command interface for insertion.</param>
        /// <param name="payload">The payload buffer to write component data to.</param>
        /// <param name="types">The array of component types to be added to the entity.</param>
        private Entity BuildEntity(IEntityCommand cmd, Span<byte> payload, Span<Type> types)
        {
            var trans = new TransformAccess
            {
                position   = transform.position,
                rotation   = transform.rotation,
                localScale = transform.localScale
            };
            Transforms.Add(trans);
             return cmd.Insert(payload, types);
        }
        //
        // /// <summary>
        // /// Tries to initialize transform-related component data for the entity payload.
        // /// </summary>
        // /// <param name="payload">The payload buffer to write to.</param>
        // /// <param name="trans">The transform access struct.</param>
        // /// <param name="type">The component type.</param>
        // /// <param name="size">The size of the component struct.</param>
        // /// <returns>True if the type was handled and written; otherwise, false.</returns>
        // private static unsafe bool TryInitPosition(byte[] payload, ref TransformAccess trans, Type type, int size)
        // {
        //     if (type == typeof(PositionX))
        //     {
        //         var posX = new PositionX {Value = trans.position.x};
        //         payload.AddRange(Native.GetAddress(ref posX), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(PositionY))
        //     {
        //         var posY = new PositionY {Value = trans.position.y};
        //         payload.AddRange(Native.GetAddress(ref posY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(PositionZ))
        //     {
        //         var posZ = new PositionZ {Value = trans.position.z};
        //         payload.AddRange(Native.GetAddress(ref posZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(PositionXY))
        //     {
        //         var posXY = new PositionXY {X = trans.position.x, Y = trans.position.y};
        //         payload.AddRange(Native.GetAddress(ref posXY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(PositionXZ))
        //     {
        //         var posXZ = new PositionXZ {X = trans.position.x, Z = trans.position.z};
        //         payload.AddRange(Native.GetAddress(ref posXZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(PositionYZ))
        //     {
        //         var posYZ = new PositionYZ {Y = trans.position.y, Z = trans.position.z};
        //         payload.AddRange(Native.GetAddress(ref posYZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(Position))
        //     {
        //         var pos = new Position {X = trans.position.x, Y = trans.position.y, Z = trans.position.z};
        //         payload.AddRange(Native.GetAddress(ref pos), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationX))
        //     {
        //         var rotX = new RotationX {Value = trans.rotation.eulerAngles.x};
        //         payload.AddRange(Native.GetAddress(ref rotX), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationY))
        //     {
        //         var rotY = new RotationY {Value = trans.rotation.eulerAngles.y};
        //         payload.AddRange(Native.GetAddress(ref rotY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationZ))
        //     {
        //         var rotZ = new RotationZ {Value = trans.rotation.eulerAngles.z};
        //         payload.AddRange(Native.GetAddress(ref rotZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationXY))
        //     {
        //         var rotXY = new RotationXY {X = trans.rotation.eulerAngles.x};
        //         payload.AddRange(Native.GetAddress(ref rotXY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationXZ))
        //     {
        //         var rotXZ = new RotationXZ {X = trans.rotation.eulerAngles.x, Z = trans.rotation.eulerAngles.z};
        //         payload.AddRange(Native.GetAddress(ref rotXZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(RotationYZ))
        //     {
        //         var rotYZ = new RotationYZ {Y = trans.rotation.eulerAngles.y, Z = trans.rotation.eulerAngles.z};
        //         payload.AddRange(Native.GetAddress(ref rotYZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(Rotation))
        //     {
        //         var rot = new Rotation {X = trans.rotation.eulerAngles.x, Y = trans.rotation.eulerAngles.y, Z = trans.rotation.eulerAngles.z};
        //         payload.AddRange(Native.GetAddress(ref rot), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleX))
        //     {
        //         var scaleX = new ScaleX {Value = trans.localScale.x};
        //         payload.AddRange(Native.GetAddress(ref scaleX), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleY))
        //     {
        //         var scaleY = new ScaleY {Value = trans.localScale.y};
        //         payload.AddRange(Native.GetAddress(ref scaleY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleZ))
        //     {
        //         var scaleZ = new ScaleZ {Value = trans.localScale.z};
        //         payload.AddRange(Native.GetAddress(ref scaleZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleXY))
        //     {
        //         var scaleXY = new ScaleXY {X = trans.localScale.x, Y = trans.localScale.y};
        //         payload.AddRange(Native.GetAddress(ref scaleXY), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleXZ))
        //     {
        //         var scaleXZ = new ScaleXZ {X = trans.localScale.x, Z = trans.localScale.z};
        //         payload.AddRange(Native.GetAddress(ref scaleXZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(ScaleYZ))
        //     {
        //         var scaleYZ = new ScaleYZ {Y = trans.localScale.y, Z = trans.localScale.z};
        //         payload.AddRange(Native.GetAddress(ref scaleYZ), size);
        //         return true;
        //     }
        //
        //     if (type == typeof(Scale))
        //     {
        //         var scale = new Scale {X = trans.localScale.x, Y = trans.localScale.y, Z = trans.localScale.z};
        //         payload.AddRange(Native.GetAddress(ref scale), size);
        //         return true;
        //     }
        //
        //     return false;
        // }
        //
    }
}