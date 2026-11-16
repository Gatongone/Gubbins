using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gubbins.Enhance;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
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
        private static readonly Dictionary<Type, bool>                       s_IsNewableCache   = new();
        private static readonly Dictionary<string, Type>                     s_WrapperTypeCache = new();

        #region Helpers

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
            if ((typeKind & TypeKind.Unmanaged) != 0 && !UnsafeUtility.IsUnmanaged(type))
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
        private static Type[] LoadAllTypes() => AssemblyCache.AllTypes
                                                             .Where(IsLoadableTypes)
                                                             .Distinct()
                                                             .ToArray();

        /// <summary>
        /// Filter out compiler-generated and non-instantiable types to avoid cluttering the dropdown with unusable options.
        /// </summary>
        private static bool IsLoadableTypes(Type type) => type is
        {
            IsNestedPrivate    : false,
            IsNestedFamily     : false,
            IsNestedFamANDAssem: false,
            IsNestedFamORAssem : false
        } and not {IsAbstract: true, IsSealed: true};

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
            var candidateTypes = s_AllTypes;

            var builtOptions = candidateTypes.Where(type => type != null && IsSelectableTypeCandidate(type) && !excludedTypeSet.Contains(type) && VerifyTypeKind(typeKind, type))
                                             .Union(includeTypes)
                                             .GroupBy(type => type.ToString())
                                             .Select(group => group.First())
                                             .ToArray();

            var duplicateNameKeys = builtOptions.GroupBy(type => $"{TypeName.GetNamespaceGroup(type)}|{TypeName.GetFriendlyTypeName(type)}")
                                                .Where(group => group.Count() > 1)
                                                .Select(group => group.Key)
                                                .ToHashSet();

            var typedOptions = builtOptions.Select(type =>
                                           {
                                               var key = $"{TypeName.GetNamespaceGroup(type)}|{TypeName.GetFriendlyTypeName(type)}";
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
    }
}