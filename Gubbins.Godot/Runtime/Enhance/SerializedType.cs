using System;
using Godot;
using Gubbins.Unsafe;

namespace Gubbins.Enhance;

/// <summary>
/// Serializable wrapper for <see cref="System.Type"/> that supports Godot serialization and type equality.
/// </summary>
[Serializable]
public struct SerializedType : IEquatable<SerializedType>, IEquatable<Type>
{
    /// <summary>
    /// The serialized string representation of the type.
    /// </summary>
    [Export] private string m_TypeString;

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
            if (m_Type != null && m_TypeString == m_Type.AssemblyQualifiedName)
                return m_Type;
            if (string.IsNullOrEmpty(m_TypeString))
                return null;
            m_Type = Type.GetType(m_TypeString, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
            return m_Type;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializedType"/> struct from a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="type">The type to serialize.</param>
    public SerializedType(Type type)
    {
        m_Type       = type;
        m_TypeString = type?.AssemblyQualifiedName;
    }

    /// <summary>
    /// Implicitly converts a <see cref="System.Type"/> to a <see cref="SerializedType"/>.
    /// </summary>
    /// <param name="type">The type to convert.</param>
    public static implicit operator SerializedType(Type type) => new SerializedType(type);

    /// <summary>
    /// Implicitly converts a <see cref="SerializedType"/> to a <see cref="System.Type"/>.
    /// </summary>
    /// <param name="type">The serialized type to convert.</param>
    public static implicit operator Type(SerializedType type) => type.Type;

    /// <summary>
    /// Determines whether this instance and another <see cref="SerializedType"/> are equal.
    /// </summary>
    public bool Equals(SerializedType other) => Type == other.Type;

    /// <summary>
    /// Determines whether this instance and a <see cref="System.Type"/> are equal.
    /// </summary>
    public bool Equals(Type other) => Type == other;

    /// <summary>
    /// Returns the string representation of the type.
    /// </summary>
    public override string ToString() => Type?.ToString();

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => Type != null ? Type.GetHashCode() : 0;
}

/// <summary>
/// Serializable wrapper for <see cref="System.Type"/> that supports Godot serialization and type equality,
/// with a generic constraint for filtering in the editor.
/// </summary>
/// <typeparam name="T">Base type for filtering types in the editor.</typeparam>
[Serializable]
public struct SerializedType<T> : IEquatable<SerializedType>, IEquatable<Type>, IEquatable<SerializedType<T>>
{
    [Export] private string m_TypeString;
    private          Type   m_Type;

    public Type Type
    {
        get
        {
            if (m_Type != null && m_TypeString == m_Type.AssemblyQualifiedName)
                return m_Type;
            if (string.IsNullOrEmpty(m_TypeString))
                return null;
            m_Type = Type.GetType(m_TypeString, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
            return m_Type;
        }
    }

    public SerializedType(Type type)
    {
        m_Type       = type;
        m_TypeString = type?.AssemblyQualifiedName;
    }

    public static implicit operator SerializedType<T>(Type type) => new SerializedType<T>(type);
    public static implicit operator Type(SerializedType<T> type) => type.Type;
    public static implicit operator SerializedType(SerializedType<T> type) => type.Type;

    public bool Equals(SerializedType other) => Type == other.Type;
    public bool Equals(Type other) => Type == other;
    public bool Equals(SerializedType<T> other) => m_TypeString == other.m_TypeString || m_Type == other.m_Type;

    public override string ToString() => Type?.ToString();
    public override int GetHashCode() => HashCode.Combine(m_TypeString, m_Type);
    public override bool Equals(object obj) => obj is SerializedType<T> other && Equals(other);
}

/// <summary>
/// Attribute for specifying a type constraint on a property, with filtering options.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class TypeFromAttribute : Attribute
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
/// Adapted for Godot: replaces Unity-specific flags (Component, ScriptableObject, UnityObject)
/// with Godot equivalents (GodotObject, Node, Resource, RefCounted, etc.).
/// </summary>
[Flags]
public enum TypeKind
{
    /// <summary>Abstract types.</summary>
    Abstract = 1 << 0,

    /// <summary>Interface types.</summary>
    Interface = 1 << 1,

    /// <summary>Non‑abstract types.</summary>
    NotAbstract = 1 << 2,

    /// <summary>Non‑interface types</summary>
    NotInterface = 1 << 3,

    /// <summary>Concrete implementation types (non‑abstract, non‑interface).</summary>
    Implementation = 1 << 4,

    /// <summary>Class types (reference types).</summary>
    Class = 1 << 5,

    /// <summary>Struct types (value types).</summary>
    Struct = 1 << 6,

    /// <summary>Types with a public parameterless constructor.</summary>
    Newable = 1 << 7,

    /// <summary>Unmanaged types (only structs with no managed references).</summary>
    Unmanaged = 1 << 8,

    /// <summary>Type contains generic parameters.</summary>
    Generic = 1 << 9,

    /// <summary>Type does not contain generic parameters.</summary>
    NotGeneric = 1 << 10,

    // --- Godot‑specific flags ---

    /// <summary>Types deriving from <see cref="GodotObject"/> (the root of all Godot objects).</summary>
    GodotObject = 1 << 11,

    /// <summary>Types deriving from <see cref="Node"/>.</summary>
    Node = 1 << 12,

    /// <summary>Types deriving from <see cref="Resource"/>.</summary>
    Resource = 1 << 13,

    /// <summary>All types.</summary>
    All = 1 << 15
}