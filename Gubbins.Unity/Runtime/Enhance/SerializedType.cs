using System;
using UnityEngine;

namespace Gubbins.Enhance
{
    [Serializable]
    public struct SerializedType : IEquatable<SerializedType>, IEquatable<Type>
    {
        [SerializeField]
        private string m_TypeString;
        private Type m_Type;

        public Type Type
        {
            get
            {
                if (m_Type != null && m_TypeString == m_Type.ToString()) return m_Type;
                if (string.IsNullOrEmpty(m_TypeString)) return null;
                m_Type = Type.GetType(m_TypeString);

                return m_Type;
            }
        }

        public SerializedType(Type type) => (m_Type, m_TypeString) = (type, type.ToString());
        public static implicit operator SerializedType(Type type) => new(type);
        public static implicit operator Type(SerializedType type) => type.Type;
        public bool Equals(SerializedType other) => Type == other.Type;
        public bool Equals(Type other) => Type == other;
        public override string ToString() => Type?.ToString();
        public override int GetHashCode() => Type != null ? Type.GetHashCode() : 0;
    }

    public class TypeFromAttribute : PropertyAttribute
    {
        public readonly Type Type;
        public readonly TypeKind Kind;
        public readonly Type[] Exclude;

        public TypeFromAttribute(Type type, TypeKind kind = TypeKind.All, params Type[] exclude)
        {
            Type = type;
            Kind = kind;
            Exclude = exclude;
        }
    }

    [Flags]
    public enum TypeKind
    {
        Abstract = 0x001001,
        Interface = 0x000010,
        Implementation = 0x000100,
        Class = 0x001000,
        Struct = 0x010000,
        Newable = 0x110000,
        All = 0x111111
    }
}