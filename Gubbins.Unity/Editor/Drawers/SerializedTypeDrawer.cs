using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// Property drawer for <see cref="SerializedType"/> and <see cref="SerializedType{T}"/>, optionally filtered by <see cref="TypeFromAttribute"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializedType))]
    [CustomPropertyDrawer(typeof(SerializedType<>))]
    [CustomPropertyDrawer(typeof(TypeFromAttribute))]
    internal sealed class SerializedTypePropertyDrawer : PropertyDrawer
    {
        private const string TYPE_STRING_PROPERTY_NAME = "m_TypeString";
        private const string NULL_OPTION_LABEL         = "Null";

        private static readonly Type[]                                       s_AllTypes         = LoadAllTypes();
        private static readonly Dictionary<FilterCacheKey, List<TypeOption>> s_OptionCache      = new();
        private static readonly Dictionary<Type, Type[]>                     s_SubTypeCache     = new();
        private static readonly Dictionary<Type, bool>                       s_IsUnmanagedCache = new();
        private static readonly Dictionary<Type, bool>                       s_IsNewableCache   = new();
        private static readonly Dictionary<string, Type>                     s_WrapperTypeCache = new();

        #region Common

        /// <summary>
        /// Resolve the actual serialized wrapper type for direct fields and collection elements.
        /// </summary>
        private Type GetSerializedTypeWrapper(SerializedProperty property)
        {
            var wrapperType = TryGetSerializedTypeWrapper(fieldInfo?.FieldType);
            if (wrapperType != null)
                return wrapperType;

            var targetObject = property.serializedObject.targetObject;
            if (targetObject == null)
                return null;

            var cacheKey = $"{targetObject.GetType().AssemblyQualifiedName}|{property.propertyPath}";
            if (!s_WrapperTypeCache.TryGetValue(cacheKey, out wrapperType))
            {
                var resolvedType = ResolvePropertyPathType(targetObject.GetType(), property.propertyPath);
                wrapperType                  = TryGetSerializedTypeWrapper(resolvedType);
                s_WrapperTypeCache[cacheKey] = wrapperType;
            }

            return wrapperType;
        }

        /// <summary>
        /// Try to extract a SerializedType wrapper from a type or a collection element type.
        /// </summary>
        private static Type TryGetSerializedTypeWrapper(Type type)
        {
            while (type != null)
            {
                if (IsSerializedTypeWrapper(type))
                    return type;

                type = GetElementType(type);
            }

            return null;
        }

        /// <summary>
        /// Resolve the type of a nested serialized property path, including array/list elements.
        /// </summary>
        private static Type ResolvePropertyPathType(Type rootType, string propertyPath)
        {
            if (rootType == null || string.IsNullOrEmpty(propertyPath))
                return null;

            var currentType = rootType;
            var path = propertyPath.Replace(".Array.data[", "[");
            var pathElements = path.Split('.');

            foreach (var element in pathElements)
            {
                var bracketIndex = element.IndexOf('[');
                if (bracketIndex >= 0)
                {
                    var fieldName = element.Substring(0, bracketIndex);
                    currentType = GetFieldTypeInHierarchy(currentType, fieldName);
                    currentType = GetElementType(currentType);
                }
                else
                {
                    currentType = GetFieldTypeInHierarchy(currentType, element);
                }

                if (currentType == null)
                    return null;
            }

            return currentType;
        }

        /// <summary>
        /// Search for a field in the current type and its base types.
        /// </summary>
        private static Type GetFieldTypeInHierarchy(Type currentType, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (currentType != null)
            {
                var field = currentType.GetField(fieldName, flags);
                if (field != null)
                    return field.FieldType;

                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Get element type if the type is array or collection, otherwise return null.
        /// </summary>
        private static Type GetElementType(Type type)
        {
            if (type == null)
                return null;

            if (type.IsArray)
                return type.GetElementType();

            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();
                if (genericDefinition == typeof(List<>) || genericDefinition == typeof(IList<>) || genericDefinition == typeof(IReadOnlyList<>))
                    return type.GetGenericArguments()[0];
            }

            var enumerableInterface = type.GetInterfaces()
                                          .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0];
        }

        /// <summary>
        /// Get all types that can be assigned to <paramref name="type"/>.
        /// </summary>
        private static IEnumerable<Type> GetAllSubTypesCached(Type type)
        {
            if (type == null)
                return Array.Empty<Type>();

            if (!s_SubTypeCache.TryGetValue(type, out var subTypes))
            {
                subTypes             = s_AllTypes.Where(type.IsAssignableFrom).ToArray();
                s_SubTypeCache[type] = subTypes;
            }

            return subTypes;
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
            if ((typeKind & TypeKind.Newable) != 0 && !IsTypeNewableCached(type))
                return false;
            if ((typeKind & TypeKind.Struct) != 0 && !type.IsValueType)
                return false;
            if ((typeKind & TypeKind.Class) != 0 && !type.IsClass)
                return false;
            if ((typeKind & TypeKind.Unmanaged) != 0 && !IsTypeUnmanagedCached(type))
                return false;
            if ((typeKind & TypeKind.Generic) != 0 && !type.ContainsGenericParameters)
                return false;
            if ((typeKind & TypeKind.NotGeneric) != 0 && type.ContainsGenericParameters)
                return false;
            if ((typeKind & TypeKind.UnityObject) == TypeKind.UnityObject) return typeof(UnityEngine.Object).IsAssignableFrom(type);

            if ((typeKind & TypeKind.Component) != 0 && !typeof(Component).IsAssignableFrom(type))
                return false;
            if ((typeKind & TypeKind.ScriptableObject) != 0 && !typeof(ScriptableObject).IsAssignableFrom(type))
                return false;

            return true;
        }

        /// <summary>
        /// Load all non-null types from all loaded assemblies.
        /// </summary>
        private static Type[] LoadAllTypes() => AppDomain.CurrentDomain.GetAssemblies()
                                                         .SelectMany(GetLoadableTypes)
                                                         .Where(type => type != null)
                                                         .Distinct()
                                                         .ToArray();

        /// <summary>
        /// Get all types from an assembly without failing on partial load errors.
        /// </summary>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes().Where(static type => type is
                {
                    IsNestedPrivate    : false,
                    IsNestedFamily     : false,
                    IsNestedFamANDAssem: false,
                    IsNestedFamORAssem : false
                } and not {IsAbstract: true, IsSealed: true});
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type != null);
            }
        }

        /// <summary>
        /// Determine whether a type is SerializedType or SerializedType{T}.
        /// </summary>
        private static bool IsSerializedTypeWrapper(Type type) => type == typeof(SerializedType) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SerializedType<>));

        /// <summary>
        /// Get the currently selected popup index.
        /// </summary>
        private static int GetCurrentIndex(IReadOnlyList<TypeOption> options, string currentSerializedName)
        {
            if (string.IsNullOrEmpty(currentSerializedName))
                return 0;

            for (var i = 0; i < options.Count; i++)
            {
                if (options[i].SerializedName == currentSerializedName)
                    return i + 1;
            }

            return 0;
        }

        /// <summary>
        /// Apply the popup selection back to the serialized string field.
        /// </summary>
        private static void ApplySelection(SerializedProperty typeString, IReadOnlyList<TypeOption> options, int index)
        {
            typeString.stringValue = index <= 0 ? null : options[index - 1].SerializedName;
        }

        /// <summary>
        /// Build a menu path with namespace segments split to submenus.
        /// </summary>
        private static string BuildMenuPath(string namespaceGroup, string typeName)
        {
            var namespacePath = namespaceGroup.Replace('.', '/');
            return string.IsNullOrEmpty(namespacePath) ? typeName : $"{namespacePath}/{typeName} [{namespaceGroup}]";
        }

        /// <summary>
        /// Returns true for user-facing types and false for compiler-generated internals.
        /// </summary>
        private static bool IsSelectableTypeCandidate(Type type)
        {
            if (type == null)
                return false;

            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return false;

            var fullName = type.FullName;
            if (string.IsNullOrEmpty(fullName))
                return false;

            if (fullName.Contains("<>") || fullName.Contains("AnonymousType"))
                return false;

            if (type.Name.StartsWith("<", StringComparison.Ordinal))
                return false;

            return true;
        }

        /// <summary>
        /// Get the namespace grouping label for a type.
        /// </summary>
        private static string GetNamespaceGroup(Type type) => string.IsNullOrEmpty(type.Namespace) ? "(Global)" : type.Namespace;

        /// <summary>
        /// Checks if the provided type is unmanaged, which means it is a value type that does not contain any reference type fields.
        /// </summary>
        /// <param name="type">The type to check for being unmanaged. </param>
        /// <returns>True if the type is unmanaged; otherwise, false.</returns>
        private static bool CheckIsTypeUnmanaged(Type type)
        {
            if (type.IsPrimitive || type.IsPointer || type.IsEnum)
                return true;
            if (!type.IsValueType)
                return false;

            // // A type is considered unmanaged if it is a value type and all of its fields are either of
            // the same type (to allow for recursive structs) or are unmanaged types.
            return !type.GetFields().Any(f => f.FieldType != type && !CheckIsTypeUnmanaged(f.FieldType));
        }

        /// <summary>
        /// Cached wrapper for IsNewable to avoid repeated reflection checks across repaints.
        /// </summary>
        private static bool IsTypeNewableCached(Type type)
        {
            if (s_IsNewableCache.TryGetValue(type, out var result))
                return result;

            result                 = type.IsNewable(out _);
            s_IsNewableCache[type] = result;
            return result;
        }

        /// <summary>
        /// Cached wrapper for unmanaged checks.
        /// </summary>
        private static bool IsTypeUnmanagedCached(Type type)
        {
            if (s_IsUnmanagedCache.TryGetValue(type, out var result))
                return result;

            result                   = CheckIsTypeUnmanaged(type);
            s_IsUnmanagedCache[type] = result;
            return result;
        }

        /// <summary>
        /// Build a stable cache key from type arrays.
        /// </summary>
        private static string BuildTypeKey(IEnumerable<Type> types)
        {
            if (types == null)
                return string.Empty;

            return string.Join("|", types.Where(type => type != null)
                                         .Select(type => type.AssemblyQualifiedName)
                                         .OrderBy(name => name, StringComparer.Ordinal));
        }

        #endregion

        #region UIToolKit

        /// <summary>
        /// UIToolkit version.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var typeString = property.FindPropertyRelative(TYPE_STRING_PROPERTY_NAME);
            if (typeString == null)
            {
                return new Label("SerializedType requires an m_TypeString backing field.");
            }

            if (!TryBuildOptions(property, typeString.stringValue, out var options))
            {
                return new Label("TypeFrom only supports SerializedType and SerializedType<T>.");
            }

            var root = new VisualElement();
            var button = new ButtonPopupField(property.displayName);
            button.AddToClassList(ButtonPopupField.alignedFieldUssClassName);
            root.Add(button);
            UpdateButtonText();

            button.clicked += () =>
            {
                // Get the button's screen rectangle for the dropdown
                var buttonRect = button.worldBound;
                var screenRect = GUIUtility.GUIToScreenRect(buttonRect);
                var dropdown = new TypeAdvancedDropdown(options, newIndex =>
                {
                    ApplySelection(typeString, options, newIndex);
                    UpdateButtonText();
                    property.serializedObject.ApplyModifiedProperties();
                });
                dropdown.Show(screenRect);
            };

            Undo.undoRedoPerformed += OnUndoRedo;

            // Clean up event subscription when the element is removed
            root.RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);

            return root;

            // Refresh button text from the current serialized value
            void UpdateButtonText()
            {
                var currentString = typeString.stringValue;
                var idx = GetCurrentIndex(options, currentString);
                button.text = idx <= 0 ? NULL_OPTION_LABEL : options[idx - 1].DisplayName;
            }

            // Handle external changes (undo/redo, reset, etc.)
            void OnUndoRedo()
            {
                property.serializedObject.Update();
                UpdateButtonText();
            }
        }

        #endregion

        #region IMGUI

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var typeString = property.FindPropertyRelative(TYPE_STRING_PROPERTY_NAME);
            if (typeString == null)
            {
                EditorGUI.LabelField(position, label.text, "SerializedType requires an m_TypeString backing field.");
                EditorGUI.EndProperty();
                return;
            }

            if (!TryBuildOptions(property, typeString.stringValue, out var options))
            {
                EditorGUI.LabelField(position, label.text, "TypeFrom only supports SerializedType and SerializedType<T>.");
                EditorGUI.EndProperty();
                return;
            }

            var currentIndex = GetCurrentIndex(options, typeString.stringValue);
            var currentDisplay = currentIndex <= 0 ? NULL_OPTION_LABEL : options[currentIndex - 1].DisplayName;
            var buttonRect = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName));
            if (EditorGUI.DropdownButton(buttonRect, new GUIContent(currentDisplay), FocusType.Keyboard))
            {
                var screenRect = GUIUtility.GUIToScreenRect(buttonRect);
                var dropdown = new TypeAdvancedDropdown(options, newIndex =>
                {
                    ApplySelection(typeString, options, newIndex);
                    property.serializedObject.ApplyModifiedProperties();
                });
                dropdown.Show(screenRect);
            }

            EditorGUI.EndProperty();
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        /// <summary>
        /// Build the selectable type list for the current property.
        /// </summary>
        private bool TryBuildOptions(SerializedProperty property, string currentSerializedName, out List<TypeOption> options)
        {
            options = null;
            var wrapperType = GetSerializedTypeWrapper(property);
            if (wrapperType == null)
                return false;

            var typeFrom = attribute as TypeFromAttribute;
            options = GetOrBuildBaseOptions(wrapperType, typeFrom);

            if (!string.IsNullOrEmpty(currentSerializedName) && options.All(option => option.SerializedName != currentSerializedName))
                options = options.Concat(new[] {new TypeOption(currentSerializedName)}).ToList();

            return true;
        }

        /// <summary>
        /// Resolve or build cached options for a specific wrapper and filter configuration.
        /// </summary>
        private static List<TypeOption> GetOrBuildBaseOptions(Type wrapperType, TypeFromAttribute typeFrom)
        {
            var includeTypes = typeFrom?.Include ?? Array.Empty<Type>();
            var excludedTypes = typeFrom?.Exclude ?? Array.Empty<Type>();
            var typeKind = typeFrom?.Kind ?? TypeKind.All;
            var cacheKey = new FilterCacheKey(wrapperType, typeKind, BuildTypeKey(includeTypes), BuildTypeKey(excludedTypes));

            if (s_OptionCache.TryGetValue(cacheKey, out var cachedOptions))
                return cachedOptions;

            var excludedTypeSet = new HashSet<Type>(excludedTypes);
            var candidateTypes = includeTypes.Length == 0
                ? s_AllTypes
                : includeTypes.SelectMany(GetAllSubTypesCached).Distinct().ToArray();

            var builtOptions = candidateTypes.Where(type => type != null)
                                             .Where(IsSelectableTypeCandidate)
                                             .Where(type => !excludedTypeSet.Contains(type))
                                             .Where(type => VerifyTypeKind(typeKind, type))
                                             .GroupBy(type => type.ToString())
                                             .Select(group => group.First())
                                             .ToArray();

            var duplicateNameKeys = builtOptions.GroupBy(type => $"{GetNamespaceGroup(type)}|{TypeName.GetFriendlyTypeName(type)}")
                                                .Where(group => group.Count() > 1)
                                                .Select(group => group.Key)
                                                .ToHashSet();

            var typedOptions = builtOptions.Select(type =>
                                           {
                                               var key = $"{GetNamespaceGroup(type)}|{TypeName.GetFriendlyTypeName(type)}";
                                               return new TypeOption(type, duplicateNameKeys.Contains(key));
                                           })
                                           .OrderBy(option => option.NamespaceGroup, StringComparer.Ordinal)
                                           .ThenBy(option => option.TypeName, StringComparer.Ordinal)
                                           .ThenBy(option => option.AssemblyName, StringComparer.Ordinal)
                                           .ThenBy(option => option.SerializedName, StringComparer.Ordinal)
                                           .ToList();

            s_OptionCache[cacheKey] = typedOptions;
            return typedOptions;
        }

        #endregion

        /// <summary>
        /// Struct used as a cache key for filtered type options.
        /// </summary>
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

            public bool Equals(FilterCacheKey other) => WrapperType == other.WrapperType &&
                Kind == other.Kind &&
                IncludeKey == other.IncludeKey &&
                ExcludeKey == other.ExcludeKey;

            public override bool Equals(object obj) => obj is FilterCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = WrapperType != null ? WrapperType.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (int) Kind;
                    hashCode = (hashCode * 397) ^ (IncludeKey != null ? IncludeKey.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (ExcludeKey != null ? ExcludeKey.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// Structured representation of a type option for display and selection.
        /// </summary>
        private readonly struct TypeOption
        {
            public readonly string NamespaceGroup;
            public readonly string TypeName;
            public readonly string AssemblyName;
            public readonly string SerializedName;
            public readonly string DisplayName;
            public readonly string MenuPath;

            public TypeOption(Type type, bool appendAssembly)
            {
                NamespaceGroup = GetNamespaceGroup(type);
                TypeName       = Editor.TypeName.GetFriendlyTypeName(type);
                AssemblyName   = type.Assembly.GetName().Name;
                SerializedName = type.ToString();
                var name = appendAssembly && !string.IsNullOrEmpty(AssemblyName)
                    ? $"{TypeName} [{AssemblyName}]"
                    : TypeName;
                DisplayName = $"{name} [{NamespaceGroup}]";
                MenuPath    = BuildMenuPath(NamespaceGroup, name);
            }

            public TypeOption(string serializedName)
            {
                NamespaceGroup = "Missing";
                TypeName       = serializedName;
                AssemblyName   = string.Empty;
                SerializedName = serializedName;
                DisplayName    = $"Missing [{serializedName}]";
                MenuPath       = BuildMenuPath("Missing", serializedName);
            }
        }

        /// <summary>
        /// Custom dropdown implementation to display type options in a hierarchical menu with search support.
        /// </summary>
        /// <remarks>
        /// The search functionality will cause lag, maybe we should use UIToolkit for better performance if this becomes a problem.
        /// </remarks>
        private sealed class TypeAdvancedDropdown : AdvancedDropdown
        {
            private readonly IReadOnlyList<TypeOption> m_Options;
            private readonly Action<int>               m_OnSelected;

            public TypeAdvancedDropdown(IReadOnlyList<TypeOption> options, Action<int> onSelected)
                : base(new AdvancedDropdownState())
            {
                m_Options    = options;
                m_OnSelected = onSelected;
                minimumSize  = new Vector2(420f, 460f);
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                var root = new AdvancedDropdownItem("Select Type");
                root.AddChild(new TypeAdvancedDropdownItem(NULL_OPTION_LABEL, 0));

                var groups = new Dictionary<string, AdvancedDropdownItem>();
                for (var index = 0; index < m_Options.Count; index++)
                {
                    var option = m_Options[index];
                    AddPath(root, groups, option.MenuPath, index + 1);
                }

                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                if (item is TypeAdvancedDropdownItem typedItem)
                    m_OnSelected?.Invoke(typedItem.Index);
            }

            private static void AddPath(AdvancedDropdownItem root, IDictionary<string, AdvancedDropdownItem> groups, string path, int index)
            {
                var segments = path.Split('/');
                var parent = root;
                var key = string.Empty;

                for (var i = 0; i < segments.Length; i++)
                {
                    var segment = segments[i];
                    if (string.IsNullOrEmpty(segment))
                        continue;

                    key = string.IsNullOrEmpty(key) ? segment : $"{key}/{segment}";
                    var isLeaf = i == segments.Length - 1;

                    if (isLeaf)
                    {
                        parent.AddChild(new TypeAdvancedDropdownItem(segment, index));
                        continue;
                    }

                    if (!groups.TryGetValue(key, out var groupItem))
                    {
                        groupItem   = new AdvancedDropdownItem(segment);
                        groups[key] = groupItem;
                        parent.AddChild(groupItem);
                    }

                    parent = groupItem;
                }
            }
        }

        /// <summary>
        /// Dropdown item that holds the index of the corresponding type option for selection callbacks.
        /// </summary>
        private sealed class TypeAdvancedDropdownItem : AdvancedDropdownItem
        {
            public readonly int Index;

            public TypeAdvancedDropdownItem(string name, int index) : base(name)
            {
                Index = index;
            }
        }
    }
}