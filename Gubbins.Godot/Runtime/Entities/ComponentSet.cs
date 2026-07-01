using System;
using Godot;
using Gubbins.Enhance;
using Gubbins.Unsafe;

namespace Gubbins.Entities;

/// <summary>
/// A serialized set of component types that an <see cref="EntityAdapter3D"/> builds its entity from. In
/// the inspector this authors <em>which</em> component types the entity carries; the byte payload is
/// sized to the sum of their unmanaged stack sizes and zero-initialised. Transform components are then
/// reseeded from the node's transform in <see cref="EntityAdapter3D"/>, and code-driven spawning can
/// supply fully-valued component instances via <see cref="SetComponents"/>.
/// </summary>
/// <remarks>
/// This is the Godot analogue of the Unity <c>ComponentSet</c>, which stored boxed <c>IComponent</c>
/// instances through <c>[SerializeReference]</c>. Godot cannot serialize arbitrary polymorphic structs,
/// so the editor path persists type names only (see <see cref="_GetPropertyList"/>); the runtime path
/// keeps the boxed instances so their values survive into <see cref="BuildPayload"/>.
/// </remarks>
[GlobalClass, Tool]
public partial class ComponentSet : Godot.Resource
{
    /// <summary>
    /// The component type names selected in the inspector, stored as <see cref="Type.ToString"/> values
    /// and resolved back through <see cref="Reflection"/>. Ignored once <see cref="SetComponents"/> has
    /// supplied concrete instances.
    /// </summary>
    private Godot.Collections.Array<string> m_ComponentTypeNames = [];

    /// <summary>
    /// Concrete component instances set from code (tests/runtime spawning). When present they take
    /// precedence over <see cref="m_ComponentTypeNames"/> and their values are copied into the payload.
    /// </summary>
    private IComponent[] m_Instances;

    /// <summary>The number of components in the set.</summary>
    public int ComponentCount => m_Instances?.Length ?? m_ComponentTypeNames.Count;

    /// <summary>Gets the boxed component at <paramref name="index"/> (a zeroed instance on the editor path).</summary>
    public IComponent this[int index] => m_Instances != null ? m_Instances[index] : (IComponent) Activator.CreateInstance(TypeAt(index));

    /// <summary>
    /// Resolves the component <see cref="Type"/> at <paramref name="index"/>, from the concrete instance
    /// when set, otherwise from the serialized type name.
    /// </summary>
    public Type TypeAt(int index)
    {
        if (m_Instances != null)
        {
            return m_Instances[index].GetType();
        }

        var name = m_ComponentTypeNames[index];
        return string.IsNullOrEmpty(name) ? null : Type.GetType(name, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
    }

    /// <summary>
    /// Configures the set from concrete component instances, used for code-driven entity spawning. The
    /// instances' bytes are copied verbatim into the payload built by <see cref="BuildPayload"/>.
    /// </summary>
    public void SetComponents(params IComponent[] components) => m_Instances = components;

    /// <summary>
    /// Builds the contiguous component payload as the concatenation of each component's unmanaged struct
    /// bytes, in declaration order — the layout expected by <c>IEntityCommand.Insert</c>. Slots for
    /// components without a concrete instance are left zeroed (transform slots are overwritten from the
    /// node transform by <see cref="EntityAdapter3D"/>).
    /// </summary>
    public unsafe byte[] BuildPayload()
    {
        var count = ComponentCount;
        if (count == 0)
        {
            return [];
        }

        var totalSize = 0;
        for (var i = 0; i < count; i++)
        {
            var type = TypeAt(i);
            if (type == null)
            {
                continue;
            }

            totalSize += (int) Native.GetStackSize(type);
        }

        var payload = new byte[totalSize];
        fixed (byte* destination = payload)
        {
            var offset = 0;
            for (var i = 0; i < count; i++)
            {
                var type = TypeAt(i);
                if (type == null)
                {
                    continue;
                }

                var size = (int) Native.GetStackSize(type);

                // Editor path leaves the slice zeroed; the runtime path copies the instance's bytes.
                if (m_Instances != null && m_Instances[i] != null)
                {
                    Native.CopyMemory(Native.Unbox(m_Instances[i]), destination + offset, (uint) size);
                }

                offset += size;
            }
        }

        return payload;
    }

    /// <inheritdoc/>
    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        // Every unmanaged struct component type in the loaded assemblies is offered per array element as
        // an enum dropdown, mirroring the type-picker style of SerializedType._GetPropertyList.
        var componentTypes = AssemblyCache.AllTypes
                                          .Where(static t => typeof(IComponent).IsAssignableFrom(t)
                                                             && t.IsValueType
                                                             && !t.IsAbstract
                                                             && !t.IsGenericType);

        var enumHint = string.Join(",", componentTypes.Select(static t => t.ToString()));

        // Typed-array element hint: "<elementVariantType>/<elementHint>:<elementHintString>".
        var elementHint = $"{(int) Variant.Type.String}/{(int) PropertyHint.Enum}:{enumHint}";

        return
        [
            new Godot.Collections.Dictionary
            {
                {"name", "ComponentTypes"},
                {"type", (int) Variant.Type.Array},
                {"hint", (int) PropertyHint.TypeString},
                {"hint_string", elementHint},
                {"usage", (int) PropertyUsageFlags.Default}
            }
        ];
    }

    /// <inheritdoc/>
    public override Variant _Get(StringName property)
    {
        if (property == "ComponentTypes")
        {
            return m_ComponentTypeNames;
        }

        return base._Get(property);
    }

    /// <inheritdoc/>
    public override bool _Set(StringName property, Variant value)
    {
        if (property == "ComponentTypes")
        {
            m_ComponentTypeNames = value.As<Godot.Collections.Array<string>>() ?? [];
            return true;
        }

        return base._Set(property, value);
    }
}
