using System;
using Godot;

#if GUBBINS_ENABLED
namespace Gubbins.Enhance;

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

    /// <summary>Node types (classes deriving from <see cref="Godot.Node"/>).</summary>
    Node = 1 << 11,

    /// <summary>Resource types (classes deriving from <see cref="Godot.Resource"/>).</summary>
    Resource = 1 << 12,

    /// <summary>Resource types (classes deriving from <see cref="Godot.RefCounted"/>).</summary>
    RefCounted = 1 << 13,

    /// <summary>GodotObject types (classes deriving from <see cref="Godot.GodotObject"/>).</summary>
    GodotObject = 1 << 14,

    /// <summary>All types.</summary>
    All = 999999
}
#endif
