using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
                TypeName       = GetFriendlyTypeName(type);
                AssemblyName   = type.Assembly.GetName().Name;
                SerializedName = type.ToString();
                var name = appendAssembly && !string.IsNullOrEmpty(AssemblyName)
                    ? $"{TypeName} [{AssemblyName}]"
                    : TypeName;
                DisplayName = $"{NamespaceGroup} ▸ {name}";
                MenuPath    = BuildMenuPath(NamespaceGroup, name);
            }

            public TypeOption(string serializedName)
            {
                NamespaceGroup = "Missing";
                TypeName       = serializedName;
                AssemblyName   = string.Empty;
                SerializedName = serializedName;
                DisplayName    = $"Missing ▸ {serializedName}";
                MenuPath       = BuildMenuPath("Missing", serializedName);
            }
        }

        // /// <summary>
        // /// UI Toolkit version.
        // /// </summary>
        // public override VisualElement CreatePropertyGUI(SerializedProperty property)
        // {
        //     // Use IMGUIContainer so UI Toolkit inspector gets the same searchable dropdown behavior.
        //     return new IMGUIContainer(() =>
        //     {
        //         var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        //         DrawTypeSelector(rect, property, new GUIContent(property.displayName));
        //     });
        // }

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            => DrawTypeSelector(position, property, label);

        /// <summary>
        /// Shared IMGUI drawing path used by both IMGUI and UI Toolkit inspectors.
        /// </summary>
        private void DrawTypeSelector(Rect position, SerializedProperty property, GUIContent label)
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
            var buttonRect = EditorGUI.PrefixLabel(position, new GUIContent(FormatNaming(label.text)));
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
            var includeTypes = GetIncludeTypes(wrapperType, typeFrom).ToArray();
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

            var duplicateNameKeys = builtOptions.GroupBy(type => $"{GetNamespaceGroup(type)}|{GetFriendlyTypeName(type)}")
                                                .Where(group => group.Count() > 1)
                                                .Select(group => group.Key)
                                                .ToHashSet();

            var typedOptions = builtOptions.Select(type =>
                                           {
                                               var key = $"{GetNamespaceGroup(type)}|{GetFriendlyTypeName(type)}";
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
        /// Build the include roots from the generic wrapper type and the TypeFrom attribute.
        /// </summary>
        private static IEnumerable<Type> GetIncludeTypes(Type wrapperType, TypeFromAttribute typeFrom)
        {
            if (wrapperType.IsGenericType && wrapperType.GetGenericTypeDefinition() == typeof(SerializedType<>))
                yield return wrapperType.GetGenericArguments()[0];

            if (typeFrom?.Include == null)
                yield break;

            foreach (var type in typeFrom.Include)
            {
                if (type != null)
                    yield return type;
            }
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
                return assembly.GetTypes();
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
            return string.IsNullOrEmpty(namespacePath) ? typeName : $"{namespacePath}/{typeName}";
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
                foreach (var option in m_Options.Select((value, index) => (value, index)))
                {
                    AddPath(root, groups, option.value.MenuPath, option.index + 1);
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

        private sealed class TypeAdvancedDropdownItem : AdvancedDropdownItem
        {
            public readonly int Index;

            public TypeAdvancedDropdownItem(string name, int index)
                : base(name)
            {
                Index = index;
            }
        }

        /// <summary>
        /// Get the namespace grouping label for a type.
        /// </summary>
        private static string GetNamespaceGroup(Type type) => string.IsNullOrEmpty(type.Namespace) ? "(Global)" : type.Namespace;

        /// <summary>
        /// Build a friendly type name suitable for editor popups.
        /// </summary>
        private static string GetFriendlyTypeName(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.IsGenericParameter)
                return type.Name;

            if (type.IsArray)
                return $"{GetFriendlyTypeName(type.GetElementType())}[{new string(',', type.GetArrayRank() - 1)}]";

            if (type.IsByRef)
                return $"{GetFriendlyTypeName(type.GetElementType())}&";

            if (type.IsPointer)
                return $"{GetFriendlyTypeName(type.GetElementType())}*";

            var typeName = GetFriendlyNestedTypeName(type);
            var genericArguments = GetOwnGenericArguments(type);
            if (genericArguments.Length == 0)
                return typeName;

            return $"{typeName}<{string.Join(", ", genericArguments.Select(GetFriendlyTypeName))}>";
        }

        /// <summary>
        /// Build a nested type name without namespace and generic arity suffixes.
        /// </summary>
        private static string GetFriendlyNestedTypeName(Type type)
        {
            var name = StripGenericArity(type.Name);
            if (type.DeclaringType == null)
                return name;

            return $"{GetFriendlyNestedTypeName(type.DeclaringType)}.{name}";
        }

        /// <summary>
        /// Get only the generic arguments introduced by the current type, excluding declaring-type arguments.
        /// </summary>
        private static Type[] GetOwnGenericArguments(Type type)
        {
            if (!type.IsGenericType)
                return Array.Empty<Type>();

            var allArguments = type.GetGenericArguments();
            var declaringArgumentCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
            var ownArgumentCount = Math.Max(0, allArguments.Length - declaringArgumentCount);
            if (ownArgumentCount == 0)
                return Array.Empty<Type>();

            return allArguments.Skip(declaringArgumentCount).ToArray();
        }

        /// <summary>
        /// Strip the generic arity suffix from a CLR type name.
        /// </summary>
        private static string StripGenericArity(string typeName)
        {
            var genericIndex = typeName.IndexOf('`');
            return genericIndex < 0 ? typeName : typeName.Substring(0, genericIndex);
        }

        /// <summary>
        /// Upper camel case strategy. The following naming will output "SuperMan":
        /// <list type="bullet">
        ///     <item>_superMan</item>
        ///     <item>_SuperMan</item>
        ///     <item>s_SuperMan</item>
        ///     <item>m_SuperMan</item>
        ///     <item>superMan</item>
        ///     <item>Super_Man</item>
        ///     <item>SUPER_MAN</item>
        ///     <item>super_man</item>
        /// </list>
        /// </summary>
        internal static string FormatNaming(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            name = StandardizeHungarianNotation(name);

            var chars = name.ToCharArray();
            var sb = new StringBuilder();

            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '_') continue;

                var c = chars[i];
                if (i == 0)
                {
                    if (char.IsLower(c)) c = char.ToUpper(c);
                }
                else
                {
                    if (char.IsUpper(chars[i - 1]) && char.IsUpper(chars[i]))
                        c = char.ToLower(c);
                    if (chars[i - 1] == '_')
                        c = char.ToUpper(c);
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Clear Hungarian style.
        /// </summary>
        /// <param name="name">Origin input string.</param>
        /// <returns>Standardized name.</returns>
        private static string StandardizeHungarianNotation(string name)
        {
            if (Regex.IsMatch(name, @"^[ms]_\w.+"))
                name = name.Substring(2, name.Length - 2);
            if (Regex.IsMatch(name, @"^_\w.+") || Regex.IsMatch(name, "^[ms][A-Z].*"))
                name = name.Substring(1, name.Length - 1);
            return name;
        }

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
    }
}