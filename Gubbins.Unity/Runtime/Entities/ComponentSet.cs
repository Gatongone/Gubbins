using System;
using Gubbins.Unsafe;
using UnityEngine;

namespace Gubbins.Entities
{
    [Serializable]
    internal struct ComponentSet : ISerializationCallbackReceiver
    {
        [SerializeReference] public IComponent[] Components;

        [SerializeField, HideInInspector] private byte[] m_Payload;

        /// <summary>
        /// The raw component data laid out as the concatenation of each component's unmanaged
        /// struct bytes, in declaration order. This matches the buffer layout expected by
        /// <see cref="IEntityCommand.Insert"/>; its length equals the sum of the component sizes.
        /// </summary>
        public byte[] Payload => m_Payload;

        public int ComponentCount => Components?.Length ?? 0;
        public IComponent this[int index] => Components[index];

        public void Clear()
        {
            Components = null;
            m_Payload  = null;
        }

        /// <summary>
        /// Rebuilds <see cref="m_Payload"/> from the current components by copying each boxed
        /// component's unmanaged bytes into a single contiguous buffer, in declaration order.
        /// </summary>
        /// <returns>The rebuilt payload buffer.</returns>
        public unsafe byte[] BuildPayload()
        {
            var count = ComponentCount;
            if (count == 0)
            {
                return m_Payload = Array.Empty<byte>();
            }

            var totalSize = 0;
            for (var i = 0; i < count; i++)
            {
                var component = Components[i];
                if (component == null)
                    continue;

                totalSize += (int) Native.GetStackSize(component.GetType());
            }

            var payload = new byte[totalSize];
            fixed (byte* destination = payload)
            {
                var offset = 0;
                for (var i = 0; i < count; i++)
                {
                    var component = Components[i];
                    if (component == null)
                        continue;

                    var size = (int) Native.GetStackSize(component.GetType());
                    Native.CopyMemory(Native.Unbox(component), destination + offset, (uint) size);
                    offset += size;
                }
            }

            return m_Payload = payload;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => BuildPayload();

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }
}