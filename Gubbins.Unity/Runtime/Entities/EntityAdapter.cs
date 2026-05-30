using System;
using System.Buffers;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;

namespace Gubbins.Entities
{
    public class EntityAdapter : MonoBehaviour
    {
        internal static readonly NativeList<TransformAccess> Transforms = new(Allocator.Persistent);

        [SerializeField, ReadOnly]
        private int m_Index;

        [SerializeField, ReadOnly]
        private uint m_Version;

        [SerializeField, TypeFrom(typeof(IContext), TypeKind.Implementation)]
        private SerializedType m_Context;

        [SerializeField, TypeFrom(typeof(IComponent), TypeKind.Implementation)]
        private SerializedType[] m_Components;

        public IContext Context { get; private set; }

        private void Awake()
        {
            if (m_Context.Type == null)
            {
                throw new ArgumentException("Context is not assigned.");
            }

            Context = ApplicationContext.Global.Resolve(m_Context.Type) as IContext;

            var cmd = Context!.Resolve<IEntityCommand>();
            if (cmd == null)
            {
                throw new ArgumentException("No IEntityCommand registered in the context.");
            }
            BuildEntity(cmd);
        }

        private void BuildEntity(IEntityCommand cmd)
        {
            var trans = new TransformAccess
            {
                position   = transform.position,
                rotation   = transform.rotation,
                localScale = transform.localScale
            };

            Transforms.Add(trans);

            var types = ArrayPool<Type>.Shared.Rent(m_Components.Length);
            using var payload = new NativeList<byte>(m_Components.Length, Allocator.Temp);
            var payloadSize = 0;
            for (var i = 0; i < m_Components.Length; i++)
            {
                var type = m_Components[i].Type;
                var size = UnsafeUtility.SizeOf(type);
                payloadSize += size;
                if (!TryInitPosition(payload, ref trans, type, UnsafeUtility.SizeOf(type)))
                {
                    payload.AddReplicate(0, size);
                }
                types[i] = type;
            }
            var entity = cmd.Insert(payload.AsArray().AsSpan(), types.AsSpan());
            payload.Resize(payloadSize, NativeArrayOptions.UninitializedMemory);
            m_Index = entity.Index;
            m_Version = entity.Version;
        }

        private static unsafe bool TryInitPosition(NativeList<byte> payload, ref TransformAccess trans, Type type, int size)
        {
            if (type == typeof(PositionX))
            {
                var posX = new PositionX {Value = trans.position.x};
                payload.AddRange(Native.GetAddress(ref posX), size);
                return true;
            }

            if (type == typeof(PositionY))
            {
                var posY = new PositionY {Value = trans.position.y};
                payload.AddRange(Native.GetAddress(ref posY), size);
                return true;
            }

            if (type == typeof(PositionZ))
            {
                var posZ = new PositionZ {Value = trans.position.z};
                payload.AddRange(Native.GetAddress(ref posZ), size);
                return true;
            }

            if (type == typeof(PositionXY))
            {
                var posXY = new PositionXY {X = trans.position.x, Y = trans.position.y};
                payload.AddRange(Native.GetAddress(ref posXY), size);
                return true;
            }

            if (type == typeof(PositionXZ))
            {
                var posXZ = new PositionXZ {X = trans.position.x, Z = trans.position.z};
                payload.AddRange(Native.GetAddress(ref posXZ), size);
                return true;
            }

            if (type == typeof(PositionYZ))
            {
                var posYZ = new PositionYZ {Y = trans.position.y, Z = trans.position.z};
                payload.AddRange(Native.GetAddress(ref posYZ), size);
                return true;
            }

            if (type == typeof(Position))
            {
                var pos = new Position {X = trans.position.x, Y = trans.position.y, Z = trans.position.z};
                payload.AddRange(Native.GetAddress(ref pos), size);
                return true;
            }

            if (type == typeof(RotationX))
            {
                var rotX = new RotationX {Value = trans.rotation.eulerAngles.x};
                payload.AddRange(Native.GetAddress(ref rotX), size);
                return true;
            }

            if (type == typeof(RotationY))
            {
                var rotY = new RotationY {Value = trans.rotation.eulerAngles.y};
                payload.AddRange(Native.GetAddress(ref rotY), size);
                return true;
            }

            if (type == typeof(RotationZ))
            {
                var rotZ = new RotationZ {Value = trans.rotation.eulerAngles.z};
                payload.AddRange(Native.GetAddress(ref rotZ), size);
                return true;
            }

            if (type == typeof(RotationXY))
            {
                var rotXY = new RotationXY {X = trans.rotation.eulerAngles.x};
                payload.AddRange(Native.GetAddress(ref rotXY), size);
                return true;
            }

            if (type == typeof(RotationXZ))
            {
                var rotXZ = new RotationXZ {X = trans.rotation.eulerAngles.x, Z = trans.rotation.eulerAngles.z};
                payload.AddRange(Native.GetAddress(ref rotXZ), size);
                return true;
            }

            if (type == typeof(RotationYZ))
            {
                var rotYZ = new RotationYZ {Y = trans.rotation.eulerAngles.y, Z = trans.rotation.eulerAngles.z};
                payload.AddRange(Native.GetAddress(ref rotYZ), size);
                return true;
            }

            if (type == typeof(Rotation))
            {
                var rot = new Rotation {X = trans.rotation.eulerAngles.x, Y = trans.rotation.eulerAngles.y, Z = trans.rotation.eulerAngles.z};
                payload.AddRange(Native.GetAddress(ref rot), size);
                return true;
            }

            if (type == typeof(ScaleX))
            {
                var scaleX = new ScaleX {Value = trans.localScale.x};
                payload.AddRange(Native.GetAddress(ref scaleX), size);
                return true;
            }

            if (type == typeof(ScaleY))
            {
                var scaleY = new ScaleY {Value = trans.localScale.y};
                payload.AddRange(Native.GetAddress(ref scaleY), size);
                return true;
            }

            if (type == typeof(ScaleZ))
            {
                var scaleZ = new ScaleZ {Value = trans.localScale.z};
                payload.AddRange(Native.GetAddress(ref scaleZ), size);
                return true;
            }

            if (type == typeof(ScaleXY))
            {
                var scaleXY = new ScaleXY {X = trans.localScale.x, Y = trans.localScale.y};
                payload.AddRange(Native.GetAddress(ref scaleXY), size);
                return true;
            }

            if (type == typeof(ScaleXZ))
            {
                var scaleXZ = new ScaleXZ {X = trans.localScale.x, Z = trans.localScale.z};
                payload.AddRange(Native.GetAddress(ref scaleXZ), size);
                return true;
            }

            if (type == typeof(ScaleYZ))
            {
                var scaleYZ = new ScaleYZ {Y = trans.localScale.y, Z = trans.localScale.z};
                payload.AddRange(Native.GetAddress(ref scaleYZ), size);
                return true;
            }

            if (type == typeof(Scale))
            {
                var scale = new Scale {X = trans.localScale.x, Y = trans.localScale.y, Z = trans.localScale.z};
                payload.AddRange(Native.GetAddress(ref scale), size);
                return true;
            }

            return false;
        }
    }
}