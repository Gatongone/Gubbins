#if TOOLS
using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using Gubbins.Enhance;

namespace Gubbins.Editor;

/// <summary>
/// Editor inspector plugin for <see cref="SerializedType"/> and <see cref="SerializedType{T}"/>,
/// optionally filtered by <see cref="TypeFromAttribute"/>.
/// </summary>
[Tool]
public partial class SerializedTypeEditorPlugin : EditorInspectorPlugin
{
    private static readonly Type[]                                       s_AllTypes         = LoadAllTypes();
    private static readonly Dictionary<FilterCacheKey, List<TypeOption>> s_OptionCache      = new();
    private static readonly Dictionary<Type, Type[]>                     s_SubTypeCache     = new();
    private static readonly Dictionary<Type, bool>                       s_IsNewableCache   = new();

    internal const string NULL_OPTION_LABEL = "Null";

    #region Common

    private static Type GetSerializedTypeWrapper(FieldInfo fieldInfo)
    {
        if (fieldInfo == null) return null;
        var type = fieldInfo.FieldType;
        while (type != null)
        {
            if (IsSerializedTypeWrapper(type)) return type;
            type = GetElementType(type);
        }

        return null;
    }

    private static Type GetElementType(Type type)
    {
        if (type == null) return null;
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IList<>) || def == typeof(IReadOnlyList<>))
                return type.GetGenericArguments()[0];
        }

        var enumerable = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        return enumerable?.GetGenericArguments()[0];
    }

    private static IEnumerable<Type> GetAllSubTypesCached(Type type)
    {
        if (type == null) return [];
        if (!s_SubTypeCache.TryGetValue(type, out var subTypes))
        {
            subTypes             = s_AllTypes.Where(type.IsAssignableFrom).ToArray();
            s_SubTypeCache[type] = subTypes;
        }

        return subTypes;
    }

    private static bool VerifyTypeKind(TypeKind kind, Type type)
    {
        if (kind == TypeKind.All) return true;
        if ((kind & TypeKind.Interface) != 0 && !type.IsInterface) return false;
        if ((kind & TypeKind.Abstract) != 0 && !type.IsAbstract) return false;
        if ((kind & TypeKind.NotAbstract) != 0 && type.IsAbstract) return false;
        if ((kind & TypeKind.NotInterface) != 0 && type.IsInterface) return false;
        if ((kind & TypeKind.Implementation) != 0 && (type.IsAbstract || type.IsInterface)) return false;
        if ((kind & TypeKind.Newable) != 0 && !IsTypeNewableCached(type)) return false;
        if ((kind & TypeKind.Struct) != 0 && !type.IsValueType) return false;
        if ((kind & TypeKind.Class) != 0 && !type.IsClass) return false;
        if ((kind & TypeKind.Unmanaged) != 0 && !UnsafeUtility.IsUnmanaged(type)) return false;
        if ((kind & TypeKind.Generic) != 0 && !type.ContainsGenericParameters) return false;
        if ((kind & TypeKind.NotGeneric) != 0 && type.ContainsGenericParameters) return false;
        if ((kind & TypeKind.GodotObject) != 0 && !typeof(GodotObject).IsAssignableFrom(type)) return false;
        if ((kind & TypeKind.Node) != 0 && !typeof(Node).IsAssignableFrom(type)) return false;
        if ((kind & TypeKind.Resource) != 0 && !typeof(Godot.Resource).IsAssignableFrom(type)) return false;
        return true;
    }

    private static Type[] LoadAllTypes()
    {
        var all = new List<Type>();
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                all.AddRange(asm.GetTypes());
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (ex.Types != null) all.AddRange(ex.Types.Where(t => t != null));
            }
        }

        return all.Where(IsLoadableTypes).Distinct().ToArray();
    }

    private static bool IsLoadableTypes(Type type) =>
        type is {IsNestedPrivate: false, IsNestedFamily: false, IsNestedFamANDAssem: false, IsNestedFamORAssem: false}
        && !(type.IsAbstract && type.IsSealed);

    private static bool IsSerializedTypeWrapper(Type type) =>
        type == typeof(SerializedType) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SerializedType<>));

    private static bool IsSelectableTypeCandidate(Type type)
    {
        if (type == null) return false;
        if (type.IsDefined(typeof(CompilerGeneratedAttribute), false)) return false;
        var fullName = type.FullName;
        if (string.IsNullOrEmpty(fullName)) return false;
        if (fullName.Contains("<>") || fullName.Contains("AnonymousType")) return false;
        if (type.Name.StartsWith("<", StringComparison.Ordinal)) return false;
        return true;
    }

    private static bool IsTypeNewableCached(Type type)
    {
        if (s_IsNewableCache.TryGetValue(type, out var result)) return result;
        result                 = type.IsNewable(out _);
        s_IsNewableCache[type] = result;
        return result;
    }

    private static string BuildTypeKey(IEnumerable<Type> types)
    {
        if (types == null) return string.Empty;
        return string.Join("|", types.Where(t => t != null)
                                     .Select(t => t.AssemblyQualifiedName)
                                     .OrderBy(n => n, StringComparer.Ordinal));
    }

    #endregion

    #region Inspector Plugin Overrides

    public override bool _CanHandle(GodotObject @object) => true;

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name,
        PropertyHint hintType, string hintString,
        PropertyUsageFlags usageFlags, bool wide)
    {
        var targetType = @object.GetType();
        var fieldInfo = targetType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fieldInfo == null) return false;

        var wrapperType = GetSerializedTypeWrapper(fieldInfo);
        if (wrapperType == null) return false;

        var typeFrom = fieldInfo.GetCustomAttribute<TypeFromAttribute>();
        if (!TryBuildOptions(fieldInfo, typeFrom, out var options)) return false;

        // Use EditorUndoRedoManager (Godot 4)
        var undoRedo = EditorInterface.Singleton.GetEditorUndoRedo();
        var editor = new SerializedTypeEditorProperty(name, fieldInfo, @object, options, undoRedo);
        AddPropertyEditor(name, editor);
        return true;
    }

    private bool TryBuildOptions(FieldInfo fieldInfo, TypeFromAttribute typeFrom, out List<TypeOption> options)
    {
        options = null;
        var wrapperType = GetSerializedTypeWrapper(fieldInfo);
        if (wrapperType == null) return false;

        var includeTypes = typeFrom?.Include ?? [];
        if (includeTypes.Length == 0 && wrapperType.IsGenericType && wrapperType.GetGenericTypeDefinition() == typeof(SerializedType<>))
        {
            includeTypes = [wrapperType.GetGenericArguments()[0]];
        }

        var typeKind = typeFrom?.Kind ?? TypeKind.All;
        var cacheKey = new FilterCacheKey(wrapperType, typeKind, BuildTypeKey(includeTypes), BuildTypeKey(typeFrom?.Exclude ?? []));

        if (s_OptionCache.TryGetValue(cacheKey, out var cached))
        {
            options = cached;
            return true;
        }

        var excludedSet = new HashSet<Type>(typeFrom?.Exclude ?? []);
        var candidates = includeTypes.Length == 0
            ? s_AllTypes
            : includeTypes.SelectMany(GetAllSubTypesCached).Distinct().ToArray();

        var built = candidates.Where(t => t != null)
                              .Where(IsSelectableTypeCandidate)
                              .Where(t => !excludedSet.Contains(t))
                              .Where(t => VerifyTypeKind(typeKind, t))
                              .GroupBy(t => t.ToString())
                              .Select(g => g.First())
                              .ToArray();

        var duplicateKeys = built.GroupBy(t => $"{TypeName.GetNamespaceGroup(t)}|{TypeName.GetFriendlyTypeName(t)}")
                                 .Where(g => g.Count() > 1)
                                 .Select(g => g.Key)
                                 .ToHashSet();

        var typedOptions = built.Select(t =>
                                {
                                    var key = $"{TypeName.GetNamespaceGroup(t)}|{TypeName.GetFriendlyTypeName(t)}";
                                    return new TypeOption(t, duplicateKeys.Contains(key));
                                })
                                .OrderBy(o => o.NamespaceGroup, StringComparer.Ordinal)
                                .ThenBy(o => o.TypeName, StringComparer.Ordinal)
                                .ThenBy(o => o.AssemblyName, StringComparer.Ordinal)
                                .ThenBy(o => o.SerializedName, StringComparer.Ordinal)
                                .ToList();

        s_OptionCache[cacheKey] = typedOptions;
        options                 = typedOptions;
        return true;
    }

    #endregion

    private readonly struct FilterCacheKey : IEquatable<FilterCacheKey>
    {
        public readonly Type     WrapperType;
        public readonly TypeKind Kind;
        public readonly string   IncludeKey;
        public readonly string   ExcludeKey;

        public FilterCacheKey(Type wrapperType, TypeKind kind, string includeKey, string excludeKey)
        {
            WrapperType = wrapperType;
            Kind        = kind;
            IncludeKey  = includeKey;
            ExcludeKey  = excludeKey;
        }

        public bool Equals(FilterCacheKey other) =>
            WrapperType == other.WrapperType && Kind == other.Kind &&
            IncludeKey == other.IncludeKey && ExcludeKey == other.ExcludeKey;

        public override bool Equals(object obj) => obj is FilterCacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = WrapperType?.GetHashCode() ?? 0;
                hash = (hash * 397) ^ (int) Kind;
                hash = (hash * 397) ^ (IncludeKey?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (ExcludeKey?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}

/// <summary>
/// Custom EditorProperty for displaying a dropdown to select a type.
/// </summary>
internal partial class SerializedTypeEditorProperty : EditorProperty
{
    private readonly string                m_PropertyName;
    private readonly FieldInfo             m_FieldInfo;
    private readonly GodotObject           m_Target;
    private readonly List<TypeOption>      m_Options;
    private readonly EditorUndoRedoManager m_UndoRedo;
    private readonly Button                m_Button;
    private          string                m_CurrentValue;

    public SerializedTypeEditorProperty(string propertyName, FieldInfo fieldInfo, GodotObject target,
        List<TypeOption> options, EditorUndoRedoManager undoRedo)
    {
        m_PropertyName = propertyName;
        m_FieldInfo    = fieldInfo;
        m_Target       = target;
        m_Options      = options;
        m_UndoRedo     = undoRedo;

        m_CurrentValue = (string) m_FieldInfo.GetValue(m_Target);

        m_Button                     =  new Button();
        m_Button.Text                =  GetDisplayText();
        m_Button.SizeFlagsHorizontal =  SizeFlags.ExpandFill;
        m_Button.Pressed             += OnButtonPressed;

        var hBox = new HBoxContainer();
        var label = new Label {Text = propertyName};
        label.SizeFlagsHorizontal = SizeFlags.Expand;
        hBox.AddChild(label);
        hBox.AddChild(m_Button);
        AddChild(hBox);
    }

    private void OnButtonPressed()
    {
        var popup = new PopupMenu();
        popup.AddItem(SerializedTypeEditorPlugin.NULL_OPTION_LABEL);
        foreach (var opt in m_Options)
            popup.AddItem(opt.DisplayName);

        var rect = m_Button.GetRect();
        var pos = m_Button.GetGlobalPosition();
        popup.Position = new Vector2I((int) pos.X, (int) (pos.Y + rect.Size.Y));
        popup.Size     = new Vector2I((int) rect.Size.X, popup.Size.Y);

        popup.IndexPressed += idx =>
        {
            if (idx < 0) return;
            var optionIndex = idx - 1; // index 0 is "Null"
            ApplySelection((int) optionIndex);
            popup.QueueFree();
        };

        AddChild(popup);
        popup.Popup();
    }

    private void ApplySelection(int optionIndex)
    {
        var newValue = optionIndex < 0 ? null : m_Options[optionIndex].SerializedName;
        var oldValue = m_CurrentValue;
        if (newValue == oldValue) return;

        // Undo/Redo using EditorUndoRedoManager
        m_UndoRedo.CreateAction("Change SerializedType");
        m_UndoRedo.AddDoMethod(this, nameof(SetValue), newValue!);
        m_UndoRedo.AddUndoMethod(this, nameof(SetValue), oldValue);
        m_UndoRedo.CommitAction();

        SetValue(newValue);
    }

    /// <summary>
    /// Method used by undo/redo to change the value.
    /// </summary>
    public void SetValue(string? value)
    {
        m_CurrentValue = value;
        m_FieldInfo.SetValue(m_Target, value);
        m_Target.NotifyPropertyListChanged();
        UpdateButtonText();
        EmitChanged(m_PropertyName, Variant.From(value));
    }

    private void UpdateButtonText() => m_Button.Text = GetDisplayText();

    private string GetDisplayText()
    {
        if (string.IsNullOrEmpty(m_CurrentValue))
            return SerializedTypeEditorPlugin.NULL_OPTION_LABEL;

        foreach (var opt in m_Options)
            if (opt.SerializedName == m_CurrentValue)
                return opt.DisplayName;
        return m_CurrentValue;
    }

    private void OnUndoRedo()
    {
        m_CurrentValue = (string) m_FieldInfo.GetValue(m_Target);
        UpdateButtonText();
    }
}

/// <summary>
/// Editor plugin that registers the custom inspector plugin.
/// </summary>
[Tool]
public partial class SerializedTypePlugin : EditorPlugin
{
    private SerializedTypeEditorPlugin m_InspectorPlugin;

    public override void _EnterTree()
    {
        m_InspectorPlugin = new SerializedTypeEditorPlugin();
        AddInspectorPlugin(m_InspectorPlugin);
    }

    public override void _ExitTree()
    {
        if (m_InspectorPlugin != null)
        {
            RemoveInspectorPlugin(m_InspectorPlugin);
            m_InspectorPlugin = null;
        }
    }
}

/// <summary>
/// Helper struct for display options.
/// </summary>
internal readonly struct TypeOption
{
    public readonly string SerializedName;
    public readonly string DisplayName;
    public readonly string TypeName;
    public readonly string NamespaceGroup;
    public readonly string AssemblyName;

    public TypeOption(Type type, bool includeAssembly = false)
    {
        SerializedName = type.ToString();
        TypeName       = Editor.TypeName.GetFriendlyTypeName(type);
        NamespaceGroup = Editor.TypeName.GetNamespaceGroup(type);
        AssemblyName   = type.Assembly.GetName().Name;
        DisplayName    = includeAssembly ? $"{TypeName} ({AssemblyName})" : TypeName;
    }

    public TypeOption(string unknownSerializedName)
    {
        SerializedName = unknownSerializedName;
        TypeName       = unknownSerializedName;
        NamespaceGroup = "Unknown";
        AssemblyName   = string.Empty;
        DisplayName    = unknownSerializedName + " (missing)";
    }
}

/// <summary>
/// Static utilities for type name formatting.
/// </summary>
internal static class TypeName
{
    public static string GetFriendlyTypeName(Type type)
    {
        if (type == null) return "null";
        if (!type.IsGenericType) return type.Name;

        var sb = new System.Text.StringBuilder();
        var name = type.Name;
        var backtick = name.IndexOf('`');
        sb.Append(backtick > 0 ? name.Substring(0, backtick) : name);
        sb.Append('<');
        var args = type.GetGenericArguments();
        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(GetFriendlyTypeName(args[i]));
        }

        sb.Append('>');
        return sb.ToString();
    }

    public static string GetNamespaceGroup(Type type)
    {
        var ns = type.Namespace;
        if (string.IsNullOrEmpty(ns)) return "Global";
        var parts = ns.Split('.');
        return parts.Length > 0 ? parts[0] : ns;
    }
}

/// <summary>
/// Simple placeholder for UnsafeUtility (since Godot doesn't have it, we emulate with a runtime check).
/// </summary>
internal static class UnsafeUtility
{
    public static bool IsUnmanaged(Type type)
    {
        if (type == null) return false;
        if (type.IsPrimitive || type.IsPointer) return true;
        if (type.IsValueType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (!IsUnmanaged(field.FieldType)) return false;
            }
            return true;
        }
        return false;
    }
}
#endif