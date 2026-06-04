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

        public byte[] Payload => m_Payload;

        public int ComponentCount => Components?.Length ?? 0;
        public IComponent this[int index] => Components[index];

        public void Clear()
        {
            Components = null;
            m_Payload  = null;
        }

        /// <summary>
        /// Rebuilds derived arrays so they stay aligned with Components.
        /// </summary>
        private void SyncDerivedData()
        {
            var count = ComponentCount;
            if (count == 0)
            {
                m_Payload = Array.Empty<byte>();
                return;
            }

            m_Payload = new byte[count];

            for (var i = 0; i < count; i++)
            {
                var component = Components[i];
                if (component == null)
                    continue;

                var type = component.GetType();
                m_Payload[i] = (byte) Native.GetStackSize(type);
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() => SyncDerivedData();

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }
}