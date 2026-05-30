using System;
using UnityEngine;

namespace Gubbins.Enhance
{
    /// <summary>
    /// Serializable wrapper for <see cref="System.Type"/> that supports Unity serialization and type equality.
    /// </summary>
    [Serializable]
    public struct SerializedType : IEquatable<SerializedType>, IEquatable<Type>
    {
        /// <summary>
        /// The serialized string representation of the type.
        /// </summary>
        [SerializeField] private string m_TypeString;

        /// <summary>
        /// The cached <see cref="System.Type"/> instance.
        /// </summary>
        private Type m_Type;

        /// <summary>
        /// Gets the <see cref="System.Type"/> represented by this struct.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedType"/> struct from a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">The type to serialize.</param>
        public SerializedType(Type type) => (m_Type, m_TypeString) = (type, type.ToString());

        /// <summary>
        /// Implicitly converts a <see cref="System.Type"/> to a <see cref="SerializedType"/>.
        /// </summary>
        /// <param name="type">The type to convert.</param>
        public static implicit operator SerializedType(Type type) => new(type);

        /// <summary>
        /// Implicitly converts a <see cref="SerializedType"/> to a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">The serialized type to convert.</param>
        public static implicit operator Type(SerializedType type) => type.Type;

        /// <summary>
        /// Determines whether this instance and another <see cref="SerializedType"/> are equal.
        /// </summary>
        /// <param name="other">The other serialized type to compare.</param>
        /// <returns>True if the types are equal; otherwise, false.</returns>
        public bool Equals(SerializedType other) => Type == other.Type;

        /// <summary>
        /// Determines whether this instance and a <see cref="System.Type"/> are equal.
        /// </summary>
        /// <param name="other">The type to compare.</param>
        /// <returns>True if the types are equal; otherwise, false.</returns>
        public bool Equals(Type other) => Type == other;

        /// <summary>
        /// Returns the string representation of the type.
        /// </summary>
        /// <returns>The type name as a string, or null if not set.</returns>
        public override string ToString() => Type?.ToString();

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code of the type, or 0 if not set.</returns>
        public override int GetHashCode() => Type != null ? Type.GetHashCode() : 0;
    }

    /// <summary>
    /// Attribute for specifying a type constraint on a property, with filtering options.
    /// </summary>
    public class TypeFromAttribute : PropertyAttribute
    {
        /// <summary>
        /// The base type constraint.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// The kind of type to filter (interface, class, etc).
        /// </summary>
        public readonly TypeKind Kind;

        /// <summary>
        /// Types to exclude from the filter.
        /// </summary>
        public readonly Type[] Exclude;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeFromAttribute"/> class.
        /// </summary>
        /// <param name="type">The base type constraint.</param>
        /// <param name="kind">The kind of type to filter (default: All).</param>
        /// <param name="exclude">Types to exclude from the filter.</param>
        public TypeFromAttribute(Type type, TypeKind kind = TypeKind.All, params Type[] exclude)
        {
            Type    = type;
            Kind    = kind;
            Exclude = exclude;
        }
    }

    /// <summary>
    /// Flags for filtering types in the <see cref="TypeFromAttribute"/>.
    /// </summary>
    [Flags]
    public enum TypeKind
    {
        /// <summary>Abstract types.</summary>
        Abstract = 0x001001,

        /// <summary>Interface types.</summary>
        Interface = 0x000010,

        /// <summary>Concrete implementation types.</summary>
        Implementation = 0x000100,

        /// <summary>Class types.</summary>
        Class = 0x001000,

        /// <summary>Struct types.</summary>
        Struct = 0x010000,

        /// <summary>Types with a public parameterless constructor.</summary>
        Newable = 0x110000,

        /// <summary>All types.</summary>
        All = 0x111111
    }
}