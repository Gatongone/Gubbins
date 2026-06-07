using System;
using Gubbins.Unsafe;
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
                m_Type = Type.GetType(m_TypeString, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
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
    /// Serializable wrapper for <see cref="System.Type"/> that supports Unity serialization and type equality.
    /// </summary>
    /// <typeparam name="T">Base type for filtering types in the editor.</typeparam>
    [Serializable]
    public struct SerializedType<T> : IEquatable<SerializedType>, IEquatable<Type>, IEquatable<SerializedType<T>>
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
                m_Type = Type.GetType(m_TypeString, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
                return m_Type;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializedType{T}"/> struct from a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">The type to serialize.</param>
        public SerializedType(Type type) => (m_Type, m_TypeString) = (type, type.ToString());

        /// <summary>
        /// Implicitly converts a <see cref="System.Type"/> to a <see cref="SerializedType{T}"/>.
        /// </summary>
        /// <param name="type">The type to convert.</param>
        public static implicit operator SerializedType<T>(Type type) => new(type);

        /// <summary>
        /// Implicitly converts a <see cref="SerializedType{T}"/> to a <see cref="System.Type"/>.
        /// </summary>
        /// <param name="type">The serialized type to convert.</param>
        public static implicit operator Type(SerializedType<T> type) => type.Type;

        /// <summary>
        /// Implicitly converts a <see cref="SerializedType{T}"/> to a <see cref="SerializedType"/>.
        /// </summary>
        /// <param name="type">The serialized type to convert.</param>
        public static implicit operator SerializedType(SerializedType<T> type) => type.Type;

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
        public override int GetHashCode() => HashCode.Combine(m_TypeString, m_Type);

        public bool Equals(SerializedType<T> other) => m_TypeString == other.m_TypeString || m_Type == other.m_Type;

        public override bool Equals(object obj) => obj is SerializedType<T> other && Equals(other);
    }

    /// <summary>
    /// Attribute for specifying a type constraint on a property, with filtering options.
    /// </summary>
    public class TypeFromAttribute : PropertyAttribute
    {
        /// <summary>
        /// The kind of type to filter (interface, class, etc).
        /// </summary>
        public readonly TypeKind Kind;

        /// <summary>
        /// Types to exclude from the filter.
        /// </summary>
        public readonly Type[] Exclude;

        /// <summary>
        /// Types to include in the filter.
        /// </summary>
        public readonly Type[] Include;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeFromAttribute"/> class.
        /// </summary>
        /// <param name="kind">The kind of type to filter (default: All).</param>
        /// <param name="include">Additional root types to include in the filter.</param>
        /// <param name="exclude">Types to exclude from the filter.</param>
        public TypeFromAttribute(TypeKind kind = TypeKind.All, Type[] include = null, Type[] exclude = null)
        {
            Kind    = kind;
            Exclude = exclude ?? Array.Empty<Type>();
            Include = include ?? Array.Empty<Type>();
        }
    }

    /// <summary>
    /// Flags for filtering types in the <see cref="TypeFromAttribute"/>.
    /// </summary>
    [Flags]
    public enum TypeKind
    {
        /// <summary>Abstract types.</summary>
        Abstract = 1 << 0,

        /// <summary>Interface types.</summary>
        Interface = 1 << 1,

        /// <summary>Non-abstract types.</summary>
        NotAbstract = 1 << 2,

        /// <summary>Non-interface types</summary>
        NotInterface = 1 << 3,

        /// <summary>Concrete implementation types.</summary>
        Implementation = 1 << 4,

        /// <summary>Class types.</summary>
        Class = 1 << 5,

        /// <summary>Struct types.</summary>
        Struct = 1 << 6,

        /// <summary>Types with a public parameterless constructor.</summary>
        Newable = 1 << 7,

        /// <summary>Unmanaged types.</summary>
        Unmanaged = 1 << 8,

        /// <summary>Type contains generic parameters.</summary>
        Generic = 1 << 9,

        /// <summary>Type does not contain generic parameters.</summary>
        NotGeneric = 1 << 10,

        /// <summary>Unity components (classes deriving from <see cref="UnityEngine.Component"/>).</summary>
        Component = 1 << 11,

        /// <summary>Unity scriptable objects (classes deriving from <see cref="UnityEngine.ScriptableObject"/>).</summary>
        ScriptableObject = 1 << 12,

        /// <summary>Unity objects including components, scriptable objects, and other Unity types.</summary>
        UnityObject = Component | ScriptableObject,

        /// <summary>All types.</summary>
        All = 1 << 13
    }
}