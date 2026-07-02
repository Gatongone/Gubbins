using Godot;
using Godot.Collections;
using Gubbins.Enhance;
using Gubbins.Spawner;
using Gubbins.Unsafe;

namespace Gubbins.Context;

/// <summary>
/// SerializedInstallInfo is a Godot Resource that encapsulates the installation information for a specific type within a given scope.
/// </summary>
[GlobalClass, Tool]
public partial class SerializedInstallInfo : global::Godot.Resource
{
    private Scope         m_Scope;
    private string        m_Key;
    private string        m_Type;
    private uint          m_Prewarm;
    private Array<string> m_Binding;
    private string        m_SpawnerType;
    private string        m_ControllerType;
    private GodotObject   m_Spawner;
    private GodotObject   m_Controller;

    /// <summary>
    /// Gets the Type of the spawner based on the stored spawner type string. If the string is null or empty, it returns null. Otherwise, it attempts to resolve the type using reflection.
    /// </summary>
    private Type SpawnerType => string.IsNullOrEmpty(m_SpawnerType) ? null : Type.GetType(m_SpawnerType, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);

    /// <summary>
    /// Gets the Type of the controller based on the stored controller type string. If the string is null or empty, it returns null. Otherwise, it attempts to resolve the type using reflection.
    /// </summary>
    private Type ControllerType => string.IsNullOrEmpty(m_ControllerType) ? null : Type.GetType(m_ControllerType, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);

    /// <summary>
    /// Gets the Type of the main type based on the stored type string. If the string is null or empty, it returns null. Otherwise, it attempts to resolve the type using reflection.
    /// </summary>
    public Type Type => string.IsNullOrEmpty(m_Type) ? null : Type.GetType(m_Type, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);

    /// <summary>
    /// Gets the scope of the installation information. This indicates how instances of the type should be managed (e.g., singleton, multiton, Custom.).
    /// </summary>
    public Scope Scope => m_Scope;

    /// <summary>
    /// Gets the key associated with the installation information. This key is used to identify the specific instance or configuration of the type within the given scope.
    /// </summary>
    public string Key => m_Key;

    /// <summary>
    /// Gets the number of instances to prewarm for the type. Prewarming is the process of creating instances ahead of time to improve performance when they are needed.
    /// </summary>
    public uint Prewarm => m_Prewarm;

    /// <summary>
    /// Gets the spawner instance. If the spawner is not already instantiated, it attempts to create an instance of the spawner type using reflection. If the spawner type is a GodotObject, it returns null.
    /// </summary>
    public ISpawner Spawner => m_Spawner as ISpawner ?? (typeof(GodotObject).IsAssignableFrom(SpawnerType) ? null : Activator.CreateInstance(SpawnerType) as ISpawner);

    /// <summary>
    /// Gets the controller instance. If the controller is not already instantiated, it attempts to create an instance of the controller type using reflection. If the controller type is a GodotObject, it returns null.
    /// </summary>
    public IScopeController Controller => m_Controller as IScopeController ?? (typeof(GodotObject).IsAssignableFrom(ControllerType) ? null : Activator.CreateInstance(ControllerType) as IScopeController);

    /// <summary>
    /// Gets the set of types that the main type binds to. This is determined by resolving each type string in the binding array using reflection and filtering out any null results.
    /// </summary>
    public HashSet<Type> Bindings => [..m_Binding.Select(static item => Type.GetType(item, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver)).Where(static item => item != null)];

    /// <summary>
    /// Converts the <see cref="SerializedInstallInfo"/> to an <see cref="InstallInfo"/> instance.
    /// </summary>
    /// <returns>An <see cref="InstallInfo"/> instance representing the serialized install info.</returns>
    internal InstallInfo ToInstallInfo()
    {
        var spawner = Spawner;
        var result = new InstallInfo
        {
            Scope      = Scope,
            Key        = Key,
            Type       = Type,
            Bindings   = Bindings,
            Spawner    = spawner,
            Controller = Controller,
            BakeCount  = Prewarm
        };
        if (Prewarm > 0 && spawner != null)
        {
            if (Scope != Scope.Multiton)
            {
                result.Instance = spawner.Spawn();
            }
            else
            {
                var array = new object[Prewarm];
                for (var i = 0; i < Prewarm; i++)
                {
                    array[i] = spawner.Spawn();
                }

                result.Instances = array;
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public override Array<Dictionary> _GetPropertyList()
    {
        var properties = new Array<Dictionary>();
        BuildScope(properties);
        BuildKey(properties);
        BuildType(properties);
        BuildPrewarm(properties);
        BuildController(properties);
        BuildSpawner(properties);
        BuildBinding(properties);
        return properties;
    }

    /// <inheritdoc/>
    public override Variant _Get(StringName property)
    {
        if (property == "Key")
        {
            return m_Key;
        }

        if (property == "Scope")
        {
            return (int) m_Scope;
        }

        if (property == "Type")
        {
            return m_Type;
        }

        if (property == "Binding")
        {
            return m_Binding;
        }

        if (property == "Prewarm")
        {
            return m_Prewarm;
        }

        if (property == "Spawner" || property == "Spawner_Type")
        {
            return m_SpawnerType;
        }

        if (property == "Spawner_Instance")
        {
            return m_Spawner;
        }

        if (property == "Controller")
        {
            return m_ControllerType;
        }

        return default;
    }

    /// <inheritdoc/>
    public override bool _Set(StringName property, Variant value)
    {
        if (property == "Key")
        {
            m_Key = value.AsString();
            return true;
        }

        if (property == "Scope")
        {
            m_Scope = (Scope) value.AsInt32();
            if (m_Scope is not Scope.Multiton)
            {
                if (m_Prewarm > 1)
                {
                    m_Prewarm = 1;
                }
            }

            NotifyPropertyListChanged();
            return true;
        }

        if (property == "Type")
        {
            m_Type = value.AsString();
            if (!string.IsNullOrEmpty(m_Type))
            {
                m_Key = m_Type;
            }

            m_ControllerType = null;
            m_Controller     = null;
            m_SpawnerType    = null;
            m_Spawner        = null;

            NotifyPropertyListChanged();
            return true;
        }

        if (property == "Binding")
        {
            m_Binding = value.AsGodotArray<string>();
            return true;
        }

        if (property == "Prewarm")
        {
            if (value.Obj is bool)
            {
                m_Prewarm = value.AsBool() ? 1u : 0u;
            }
            else
            {
                m_Prewarm = value.AsUInt32();
            }

            return true;
        }

        if (property == "Spawner" || property == "Spawner_Type")
        {
            m_SpawnerType = value.AsString();
            NotifyPropertyListChanged();
            return true;
        }

        if (property == "Spawner_Instance")
        {
            m_Spawner = value.AsGodotObject();
            return true;
        }

        if (property == "Controller" || property == "Controller_Type")
        {
            m_ControllerType = value.AsString();
            NotifyPropertyListChanged();
            return true;
        }

        if (property == "Controller_Instance")
        {
            m_Controller = value.AsGodotObject();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Builds the scope property for the SerializedInstallInfo. It adds an integer property named "Scope" to the property list,
    /// with an enum hint that lists all possible values of the Scope enum.
    /// </summary>
    private void BuildScope(Array<Dictionary> properties)
    {
        properties.Add(new Dictionary
        {
            {"name", "Scope"},
            {"type", (int) Variant.Type.Int},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", string.Join(",", Enum.GetNames(typeof(Scope)))},
            {"usage", (int) PropertyUsageFlags.Default}
        });
    }

    /// <summary>
    /// Builds the key property for the SerializedInstallInfo. It adds a string property named "Key" to the property list.
    /// </summary>
    private void BuildKey(Array<Dictionary> properties)
    {
        properties.Add(new Dictionary
        {
            {"name", "Key"},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.None},
            {"usage", (int) PropertyUsageFlags.Default}
        });
    }

    /// <summary>
    /// Builds the type property for the SerializedInstallInfo. It retrieves all public, non-abstract, non-interface types from the
    /// assembly cache and adds them to the property list as a comma-separated string for the hint_string.
    /// </summary>
    private void BuildType(Array<Dictionary> properties)
    {
        var implementingTypes = AssemblyCache.AllTypes
                                             .Where(t => !t.IsInterface && !t.IsAbstract && t.IsPublic && !t.ContainsGenericParameters)
                                             .ToList();
        var hintString = string.Join(",", implementingTypes.Select(t => t.ToString()));
        properties.Add(new Dictionary
        {
            {"name", "Type"},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", hintString},
            {"usage", (int) PropertyUsageFlags.Default}
        });
    }

    /// <summary>
    /// Builds the binding property for the SerializedInstallInfo. If the type is null, it returns early. Otherwise, it retrieves
    /// all base types and interfaces of the specified type and adds them to the property list as a comma-separated string.
    /// </summary>
    private void BuildBinding(Array<Dictionary> properties)
    {
        if (m_Type == null)
        {
            return;
        }

        var type = Type;
        var bindings = new List<Type>();
        GetAllBaseTypes(type, bindings);
        GetAllInterfaces(type, bindings);
        var hintString = string.Join(",", bindings.Select(t => t.ToString()));
        properties.Add(new Dictionary
        {
            {"name", "Binding"},
            {"type", (int) Variant.Type.Array},
            {"hint", (int) PropertyHint.TypeString},
            {"hint_string", $"2/2:{hintString}"},
            {"usage", (int) PropertyUsageFlags.Default}
        });

        void GetAllBaseTypes(Type t, List<Type> baseTypes)
        {
            var baseType = t.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                baseTypes.Add(baseType);
                GetAllBaseTypes(baseType, baseTypes);
            }
        }

        void GetAllInterfaces(Type t, List<Type> interfaces)
        {
            if (t == null)
            {
                return;
            }

            var typeInterfaces = t.GetInterfaces();
            interfaces.AddRange(typeInterfaces);
            foreach (var typeInterface in typeInterfaces)
            {
                GetAllInterfaces(typeInterface, interfaces);
            }
        }
    }

    /// <summary>
    /// Builds the prewarm property for the SerializedInstallInfo. If the scope is Multiton,
    /// it adds an integer property for prewarm count. Otherwise, it adds a boolean property for prewarm.
    /// </summary>
    private void BuildPrewarm(Array<Dictionary> properties)
    {
        if (m_Scope == Scope.Multiton)
        {
            properties.Add(new Dictionary
            {
                {"name", "Prewarm"},
                {"type", (int) Variant.Type.Int},
                {"hint", (int) PropertyHint.None},
                {"hint_string", (int) PropertyHint.None},
                {"usage", (int) PropertyUsageFlags.Default}
            });
        }
        else
        {
            properties.Add(new Dictionary
            {
                {"name", "Prewarm"},
                {"type", (int) Variant.Type.Bool},
                {"hint", (int) PropertyHint.None},
                {"usage", (int) PropertyUsageFlags.Default}
            });
        }
    }

    /// <summary>
    /// Builds the spawner properties for the SerializedInstallInfo. If the type is null, it returns early. Otherwise, it retrieves all types
    /// that implement ISpawner<T> and adds them to the property list. If a spawner type is specified, it adds additional properties for the spawner type and instance.
    /// </summary>
    private void BuildSpawner(Array<Dictionary> properties)
    {
        if (m_Type == null)
        {
            return;
        }

        var spawnerTypes = GenericTypeResolver.GetConstrainedImplementations(typeof(ISpawner<>).MakeGenericType(Type), Type);
        var hintString = string.Join(",", spawnerTypes.Select(t => t.ToString()));
        var spawnerType = SpawnerType;
        if (string.IsNullOrEmpty(m_SpawnerType) || spawnerType == null || !typeof(GodotObject).IsAssignableFrom(spawnerType))
        {
            properties.Add(new Dictionary
            {
                {"name", "Spawner"},
                {"type", (int) Variant.Type.String},
                {"hint", (int) PropertyHint.Enum},
                {"hint_string", hintString},
                {"usage", (int) PropertyUsageFlags.Default}
            });
            return;
        }

        properties.Add(new Dictionary
        {
            {"name", "Spawner"},
            {"type", (int) Variant.Type.Nil},
            {"hint", (int) PropertyHint.None},
            {"hint_string", "Spawner_"},
            {"usage", (int) PropertyUsageFlags.Group}
        });

        properties.Add(new Dictionary
        {
            {"name", "Spawner_Type"},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", hintString},
            {"usage", (int) PropertyUsageFlags.Default}
        });

        properties.Add(new Dictionary
        {
            {"name", "Spawner_Instance"},
            {"type", (int) Variant.Type.Object},
            {"hint", (int) PropertyHint.ResourceType},
            {"hint_string", spawnerType.Name},
            {"usage", (int) PropertyUsageFlags.Default}
        });
    }

    /// <summary>
    /// Builds the controller properties for the SerializedInstallInfo. If the type is null or the scope is not Custom, it returns early.
    /// Otherwise, it retrieves all types that implement IScopeController and adds them to the property list. If a controller type is specified,
    /// it adds additional properties for the controller type and instance.
    /// </summary>
    private void BuildController(Array<Dictionary> properties)
    {
        if (m_Type == null || m_Scope != Scope.Custom)
        {
            return;
        }

        var controllerTypes = AssemblyCache.AllTypes.Where(t => typeof(IScopeController).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToList();
        var hintString = string.Join(",", controllerTypes.Select(t => t.ToString()));
        var controllerType = ControllerType;
        if (string.IsNullOrEmpty(m_ControllerType) || controllerType == null || !typeof(GodotObject).IsAssignableFrom(controllerType))
        {
            properties.Add(new Dictionary
            {
                {"name", "Controller"},
                {"type", (int) Variant.Type.String},
                {"hint", (int) PropertyHint.Enum},
                {"hint_string", hintString},
                {"usage", (int) PropertyUsageFlags.Default}
            });
            return;
        }

        properties.Add(new Dictionary
        {
            {"name", "Controller"},
            {"type", (int) Variant.Type.Nil},
            {"hint", (int) PropertyHint.None},
            {"hint_string", "Spawner_"},
            {"usage", (int) PropertyUsageFlags.Group}
        });

        properties.Add(new Dictionary
        {
            {"name", "Controller_Type"},
            {"type", (int) Variant.Type.String},
            {"hint", (int) PropertyHint.Enum},
            {"hint_string", hintString},
            {"usage", (int) PropertyUsageFlags.Default}
        });

        properties.Add(new Dictionary
        {
            {"name", "Controller_Instance"},
            {"type", (int) Variant.Type.Object},
            {"hint", (int) PropertyHint.ResourceType},
            {"hint_string", controllerType.Name},
            {"usage", (int) PropertyUsageFlags.Default}
        });
    }
}