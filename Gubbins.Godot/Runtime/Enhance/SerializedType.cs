using Godot;
using Godot.Collections;
using Gubbins.Unsafe;

namespace Gubbins.Enhance;

[GlobalClass, Tool]
public partial class SerializedType(Type assignableType = null, TypeKind kind = TypeKind.All) : global::Godot.Resource
{
    private string m_TypeString = "";

    public Type Type => string.IsNullOrEmpty(m_TypeString) ? null : Type.GetType(m_TypeString, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
    private TypeKind m_TypeKindFlags = kind;

    public SerializedType() : this(null) { }

    public override Array<Dictionary> _GetPropertyList()
    {
        var properties = new Array<Dictionary>();
        var implementingTypes = assignableType != null
            ? AssemblyCache.AllTypes
                             .Where(t => assignableType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && VerifyTypeKind(m_TypeKindFlags, t))
                             .ToList()
            : AssemblyCache.AllTypes
                             .Where(t => VerifyTypeKind(m_TypeKindFlags, t))
                             .ToList();

        var hintString = string.Join(",", implementingTypes.Select(t => t.ToString()));

        // Add the "SelectedTypeName" property to the list
        properties.Add(new Dictionary
        {
            {"name", nameof(Type)},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", hintString},
            {"usage", (int) PropertyUsageFlags.Default}
        });
        return properties;
    }

    /// <summary>
    /// Verify the type matches the requested <see cref="TypeKind"/> flags.
    /// </summary>
    private static bool VerifyTypeKind(TypeKind typeKind, Type type)
    {
        if (typeKind == TypeKind.All)
            return true;
        if ((typeKind & TypeKind.Interface) != 0 && !type.IsInterface)
            return false;
        if ((typeKind & TypeKind.Abstract) != 0 && !type.IsAbstract)
            return false;
        if ((typeKind & TypeKind.NotAbstract) != 0 && type.IsAbstract)
            return false;
        if ((typeKind & TypeKind.NotInterface) != 0 && type.IsInterface)
            return false;
        if ((typeKind & TypeKind.Implementation) != 0 && (type.IsAbstract || type.IsInterface))
            return false;
        if ((typeKind & TypeKind.Newable) != 0 && type.GetConstructor(Type.EmptyTypes) != null)
            return false;
        if ((typeKind & TypeKind.Struct) != 0 && !type.IsValueType)
            return false;
        if ((typeKind & TypeKind.Class) != 0 && !type.IsClass)
            return false;
        if ((typeKind & TypeKind.Unmanaged) != 0 && !IsUnmanaged(type))
            return false;
        if ((typeKind & TypeKind.Generic) != 0 && !type.ContainsGenericParameters)
            return false;
        if ((typeKind & TypeKind.NotGeneric) != 0 && type.ContainsGenericParameters)
            return false;
        if ((typeKind & TypeKind.Node) != 0 && !typeof(Node).IsAssignableFrom(type))
            return false;
        if ((typeKind & TypeKind.Resource) != 0 && !typeof(global::Godot.Resource).IsAssignableFrom(type))
            return false;
        if ((typeKind & TypeKind.RefCounted) != 0 && !typeof(RefCounted).IsAssignableFrom(type))
            return false;
        if ((typeKind & TypeKind.GodotObject) != 0 && !typeof(RefCounted).IsAssignableFrom(type))
            return false;
        return true;

        static bool IsUnmanaged(Type type)
        {
            if (type.IsPrimitive || type.IsPointer || type.IsEnum)
            {
                return true;
            }

            if (!type.IsValueType)
            {
                return false;
            }

            return !type.GetFields().Any(f => f.FieldType != type && !IsUnmanaged(f.FieldType));
        }
    }

    public override bool _Set(StringName property, Variant value)
    {
        if (property == nameof(Type))
        {
            m_TypeString = value.As<string>();
            return true;
        }

        return false;
    }

    public override Variant _Get(StringName property)
    {
        if (property == nameof(Type)) return m_TypeString;
        return default;
    }
}