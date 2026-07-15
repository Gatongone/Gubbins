#if GUBBINS_ENABLED
using System;
using System.Linq;
using Godot;
using Godot.Collections;
using Gubbins.Unsafe;

namespace Gubbins.Enhance;

/// <summary>
/// A serialized reference to a GodotObject of type <see cref="T"/>, allowing for easy serialization and deserialization of references in Godot.
/// </summary>
/// <typeparam name="T">The type of the GodotObject that this serialized reference points to. Must be a class.</typeparam>
public partial class SerializedReference<T> : global::Godot.Resource where T : class
{
    /// <summary>
    /// Gets or sets the value of the serialized reference. If the reference is null, this will return null. If the reference type is not assignable to <see cref="T"/>,
    /// this will return null. If the reference type is null or empty, this will return a new instance of <see cref="T"/>.
    /// </summary>
    public T Value
    {
        get
        {
            var type = ExpectedType;
            if (m_ReferenceObject != null && type != null && typeof(T).IsAssignableFrom(type))
            {
                if (m_ReferenceObject is global::Godot.Resource r && !string.IsNullOrEmpty(r.ResourcePath))
                {
                    var cached = ResourceLoader.Load(r.ResourcePath);
                    return cached as T ?? m_ReferenceObject as T;
                }

                return m_ReferenceObject as T;
            }

            if (type == null)
            {
                return null;
            }
            var containEmptyCtor = type.GetConstructor(Type.EmptyTypes) != null;
            return containEmptyCtor ? Activator.CreateInstance(type) as T : null;
        }
        set
        {
            if (value == null)
            {
                m_ReferenceType   = string.Empty;
                m_ReferenceObject = null;
            }
            else
            {
                m_ReferenceType   = value?.GetType().ToString();
                m_ReferenceObject = value as GodotObject;
            }
        }
    }

    /// <summary>
    /// The string representation of the type that this serialized reference points to. This is used for serialization and deserialization of the reference.
    /// </summary>
    private string m_ReferenceType = string.Empty;

    /// <summary>
    /// The GodotObject that this serialized reference points to. This is used for serialization and deserialization of the reference.
    /// </summary>
    private GodotObject m_ReferenceObject;

    /// <summary>
    /// Gets the Type represented by this serialized reference. If the reference type string is null or empty, this will return null.
    /// </summary>
    public Type ExpectedType => string.IsNullOrEmpty(m_ReferenceType) ? null : Type.GetType(m_ReferenceType, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);

    /// <inheritdoc/>
    public override Array<Dictionary> _GetPropertyList()
    {
        var properties = new Array<Dictionary>();
        var referenceTypes = AssemblyCache.AllTypes.Where(t =>
            typeof(T).IsAssignableFrom(t) &&
            !t.IsInterface &&
            !t.IsAbstract).ToList();
        var hintString = string.Join(",", referenceTypes.Select(t => t.ToString()));
        var type = ExpectedType;
        if (string.IsNullOrEmpty(m_ReferenceType) || type == null || !typeof(GodotObject).IsAssignableFrom(type))
        {
            properties.Add(new Dictionary
            {
                {"name", "Reference"},
                {"type", (int) Variant.Type.String},
                {"hint", (int) PropertyHint.Enum},
                {"hint_string", hintString},
                {"usage", (int) PropertyUsageFlags.Default}
            });
            return properties;
        }

        properties.Add(new Dictionary
        {
            {"name", "Reference"},
            {"type", (int) Variant.Type.Nil},
            {"hint", (int) PropertyHint.None},
            {"hint_string", "Reference_"},
            {"usage", (int) PropertyUsageFlags.Group}
        });

        properties.Add(new Dictionary
        {
            {"name", "Reference_Type"},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", hintString},
            {"usage", (int) PropertyUsageFlags.Default}
        });

        properties.Add(new Dictionary
        {
            {"name", "Reference_Instance"},
            {"type", (int) Variant.Type.Object},
            {"hint", (int) PropertyHint.ResourceType},
            {"hint_string", type.Name},
            {"usage", (int) PropertyUsageFlags.Default}
        });

        return properties;
    }

    /// <inheritdoc/>
    public override Variant _Get(StringName property)
    {
        if (property == "Reference" || property == "Reference_Type")
        {
            return m_ReferenceType;
        }

        if (property == "Reference_Instance")
        {
            return m_ReferenceObject;
        }

        return base._Get(property);
    }

    /// <inheritdoc/>
    public override bool _Set(StringName property, Variant value)
    {
        if (property == "Reference" || property == "Reference_Type")
        {
            m_ReferenceType = value.AsString();
            NotifyPropertyListChanged();
            return true;
        }

        if (property == "Reference_Instance")
        {
            m_ReferenceObject = value.AsGodotObject();
            return true;
        }

        return base._Set(property, value);
    }
}
#endif