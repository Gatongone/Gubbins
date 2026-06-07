using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Enhance;
using Gubbins.Unsafe;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// Custom property drawer for <see cref="SerializedReference{T}"/> that supports both pure C# references and UnityEngine.Object references.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializedReference<>))]
    internal class SerializedReferencePropertyDrawer : PropertyDrawer
    {
        private const float VERTICAL_SPACING = 2;

        /// <summary>
        /// Extract the expected type T from <see cref="SerializedReference{T}"/> using reflection.
        /// </summary>
        private Type GetExpectedType(SerializedProperty property)
        {
            // Fast path: Unity usually provides fieldInfo for direct fields and collection elements.
            var expectedType = TryGetExpectedTypeFromContainerType(fieldInfo?.FieldType);
            if (expectedType != null)
                return expectedType;

            // Fallback: resolve nested property paths (including Array.data[x]) via reflection.
            var targetType = property.serializedObject.targetObject.GetType();
            var resolvedType = ResolvePropertyPathType(targetType, property.propertyPath);
            return TryGetExpectedTypeFromContainerType(resolvedType);
        }

        /// <summary>
        /// Try to extract the expected type T from a given container type, which could be either the <seealso cref="SerializedReference{T}"/> itself or a collection containing it (e.g. <c>SerializedReference{T}[]</c>).
        /// </summary>
        private Type TryGetExpectedTypeFromContainerType(Type containerType)
        {
            if (containerType == null)
                return null;

            if (containerType.IsGenericType && containerType.GetGenericTypeDefinition() == typeof(SerializedReference<>))
                return containerType.GetGenericArguments()[0];

            var elementType = GetElementType(containerType);
            if (elementType == null)
                return null;

            return elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(SerializedReference<>)
                ? elementType.GetGenericArguments()[0]
                : null;
        }

        /// <summary>
        /// Resolve the type of a property given its root object type and the property's path, which may include nested fields and array elements (e.g. "myList.Array.data[0].myField").
        /// </summary>
        private Type ResolvePropertyPathType(Type rootType, string propertyPath)
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
        /// Search for a field with the given name in the specified type and its base types, returning the field's type if found, or null if not found.
        /// </summary>
        private Type GetFieldTypeInHierarchy(Type currentType, string fieldName)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic;

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
        /// If the given type is a collection (array or generic collection), return the element type; otherwise, return null.
        /// </summary>
        private Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
                return null;

            if (collectionType.IsArray)
                return collectionType.GetElementType();

            if (collectionType.IsGenericType)
            {
                var genericDef = collectionType.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) || genericDef == typeof(IList<>) || genericDef == typeof(IReadOnlyList<>))
                    return collectionType.GetGenericArguments()[0];
            }

            var enumerableInterface = collectionType.GetInterfaces()
                                                    .FirstOrDefault(i => i.IsGenericType &&
                                                        i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableInterface?.GetGenericArguments()[0];
        }

        /// <summary>
        /// Get all implementations of the given interface type across all loaded assemblies.
        /// </summary>
        private List<Type> GetAllImplementations(Type expectedType)
        {
            if (expectedType == null) return new List<Type>();
            return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(asm => asm.GetTypes())
                            .Where(t => expectedType.IsAssignableFrom(t) &&
                                !t.IsAbstract &&
                                !t.ContainsGenericParameters &&
                                (ContainsDefaultConstructor(t) || typeof(UnityEngine.Object).IsAssignableFrom(t)))
                            .ToList();
        }

        private bool ContainsDefaultConstructor(Type type) => type.GetConstructor(Type.EmptyTypes) != null;

        /// <summary>
        /// UIElement version.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var pureProp = property.FindPropertyRelative("pureReference");
            var unityProp = property.FindPropertyRelative("unityReference");
            var typeNameProp = property.FindPropertyRelative("expectedTypeName");
            var expectedType = GetExpectedType(property);

            // If we can't determine the expected type, show an error message in the inspector.
            if (expectedType == null)
            {
                root.Add(new Label("Error: Cannot determine interface type") {style = {color = Color.red}});
                return root;
            }

            // Get all implementations of the expected type for the dropdown, and prepare the list of type names with a "Null" option at the beginning.
            var allImplementations = GetAllImplementations(expectedType);
            var typeNames = allImplementations.Select(t => t.Name).Prepend("Null").ToList();


            // Type selector dropdown
            EditorGUIUtility.labelWidth = 0;
            var typeDropdown = new DropdownField(property.displayName, typeNames, GetCurrentIndex());
            typeDropdown.AddToClassList(ObjectField.alignedFieldUssClassName);
            root.Add(typeDropdown);

            // Container for the reference field, which will be dynamically updated based on the selected type.
            var contentContainer = new VisualElement();
            root.Add(contentContainer);
            typeDropdown.RegisterValueChangedCallback(evt =>
            {
                var newIndex = typeNames.IndexOf(evt.newValue);
                var oldIndex = GetCurrentIndex();
                if (newIndex == oldIndex) return;

                if (newIndex == 0)
                {
                    ClearReference(pureProp, unityProp, typeNameProp);
                }
                else
                {
                    var newType = allImplementations[newIndex - 1];
                    if (typeof(UnityEngine.Object).IsAssignableFrom(newType))
                    {
                        pureProp.managedReferenceValue = null;
                        unityProp.objectReferenceValue = null;
                        typeNameProp.stringValue       = newType.AssemblyQualifiedName;
                    }
                    else
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(newType);
                            pureProp.managedReferenceValue = instance;
                            unityProp.objectReferenceValue = null;
                            typeNameProp.stringValue       = newType.AssemblyQualifiedName;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to create instance of {newType.Name}: {e.Message}");
                        }
                    }
                }

                property.serializedObject.ApplyModifiedProperties();
                RefreshContent();
            });

            // Tracking serialized object to update the dropdown and content when changes are made to the property from other places (e.g. undo/redo, or changes to the reference fields).
            root.TrackSerializedObjectValue(property.serializedObject, _ =>
            {
                typeDropdown.SetValueWithoutNotify(typeNames[GetCurrentIndex()]);
                RefreshContent();
            });

            RefreshContent();
            return root;

            Type GetCurrentType()
            {
                var currentValue = GetCurrentValue(pureProp, unityProp);
                return currentValue != null ? currentValue.GetType() : GetTypeFromName(typeNameProp.stringValue);
            }

            int GetCurrentIndex()
            {
                var currentType = GetCurrentType();
                if (currentType == null) return 0;
                var idx = allImplementations.IndexOf(currentType);
                return idx >= 0 ? idx + 1 : 0;
            }

            void RefreshContent()
            {
                contentContainer.Clear();

                var effectiveType = GetCurrentOrExpectedType(pureProp, unityProp, typeNameProp);
                if (effectiveType == null) return;

                // If it's not a Unity Object, draw the managed reference property field.
                if (!typeof(UnityEngine.Object).IsAssignableFrom(effectiveType))
                {
                    var propertyField = new PropertyField(pureProp, " ");
                    propertyField.SetEnabled(pureProp.managedReferenceValue != null);
                    propertyField.RegisterValueChangeCallback(_ => { property.serializedObject.ApplyModifiedProperties(); });
                    contentContainer.Add(propertyField);
                    return;
                }

                // Unity Object.
                var objectType = typeof(ScriptableObject).IsAssignableFrom(effectiveType) ? effectiveType : typeof(GameObject);
                var objectField = new ObjectField(" ")
                {
                    objectType        = objectType,
                    value             = unityProp.objectReferenceValue,
                    allowSceneObjects = true
                };
                objectField.AddToClassList(ObjectField.alignedFieldUssClassName);
                objectField.RegisterValueChangedCallback(evt =>
                {
                    var newObj = evt.newValue;
                    if (newObj == null)
                    {
                        unityProp.objectReferenceValue = null;
                        typeNameProp.stringValue       = null;
                    }
                    else
                    {
                        UnityEngine.Object finalObj = null;
                        var isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(effectiveType);
                        if (isScriptableObject)
                        {
                            if (effectiveType.IsAssignableFrom(newObj.GetType()))
                                finalObj = newObj;
                            else
                                Debug.LogWarning($"Object type {newObj.GetType().Name} is not assignable to {effectiveType.Name}");
                        }
                        else
                        {
                            if (newObj is GameObject go)
                            {
                                var component = go.GetComponent(effectiveType);
                                if (component != null)
                                    finalObj = component;
                                else
                                    Debug.LogWarning($"GameObject '{go.name}' does not have component of type {effectiveType.Name}");
                            }
                            else if (effectiveType.IsAssignableFrom(newObj.GetType()))
                            {
                                finalObj = newObj;
                            }
                            else
                            {
                                Debug.LogWarning($"Invalid object: Expected {effectiveType.Name} component or GameObject with that component.");
                            }
                        }

                        if (finalObj != null)
                        {
                            unityProp.objectReferenceValue = finalObj;
                            typeNameProp.stringValue       = finalObj.GetType().AssemblyQualifiedName;
                        }
                    }

                    unityProp.serializedObject.ApplyModifiedProperties();
                    typeDropdown.SetValueWithoutNotify(typeNames[GetCurrentIndex()]);
                    RefreshContent();
                });
                contentContainer.Add(objectField);
            }
        }

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var pureProp = property.FindPropertyRelative("pureReference");
            var unityProp = property.FindPropertyRelative("unityReference");
            var typeNameProp = property.FindPropertyRelative("expectedTypeName");

            var type = GetExpectedType(property);

            // If we can't determine the expected type, show an error message in the inspector and exit.
            if (type == null)
            {
                EditorGUI.LabelField(position, label.text, "Error: Cannot determine interface type", EditorStyles.boldLabel);
                EditorGUI.EndProperty();
                return;
            }

            // Get all implementations of the expected type for the dropdown, and prepare the list of type names with a "Null" option at the beginning.
            var currentValue = GetCurrentValue(pureProp, unityProp);
            var currentType = currentValue?.GetType() ?? GetTypeFromName(typeNameProp.stringValue);
            var allTypes = GetAllImplementations(type);
            var typeNames = allTypes.Select(t => t.Name).Prepend("Null").ToArray();

            // Draw the type selector popup
            var typeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var currentIndex = currentType == null ? 0 : allTypes.IndexOf(currentType) + 1;
            var newIndex = EditorGUI.Popup(typeRect, label.text + " Type", currentIndex, typeNames);
            if (newIndex != currentIndex)
            {
                if (newIndex == 0)
                {
                    // User selected "Null", clear both references and the expected type name.
                    ClearReference(pureProp, unityProp, typeNameProp);
                }
                else
                {
                    var newType = allTypes[newIndex - 1];
                    // Just set reference for UnityEngine.Object types
                    if (typeof(UnityEngine.Object).IsAssignableFrom(newType))
                    {
                        pureProp.managedReferenceValue = null;
                        unityProp.objectReferenceValue = null;
                        typeNameProp.stringValue       = newType.AssemblyQualifiedName;
                    }
                    // Create instance for pure C# class
                    else
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(newType);
                            pureProp.managedReferenceValue = instance;
                            unityProp.objectReferenceValue = null;
                            typeNameProp.stringValue       = newType.AssemblyQualifiedName;
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Failed to create instance of {newType.Name}: {e.Message}");
                        }
                    }
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            // If there is a valid-desired type, draw the specific editing area.
            var effectiveType = GetCurrentOrExpectedType(pureProp, unityProp, typeNameProp);
            if (effectiveType != null)
            {
                EditorGUI.indentLevel++;
                var fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + VERTICAL_SPACING,
                    position.width, GetPropertyHeight(property, label) - EditorGUIUtility.singleLineHeight - VERTICAL_SPACING);
                if (typeof(UnityEngine.Object).IsAssignableFrom(effectiveType))
                {
                    DrawUnityObjectField(fieldRect, unityProp, effectiveType, typeNameProp);
                }
                else
                {
                    // Expands all serializable fields for the pure C# object.
                    EditorGUI.PropertyField(fieldRect, pureProp, new GUIContent(" "), true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
            return;

            // Draw an ObjectField for the Unity reference, ensuring that the assigned object is of the expected type (or has a component of that type if it's a GameObject).
            void DrawUnityObjectField(Rect rect, SerializedProperty unityProp, Type expectedType, SerializedProperty typeNameProp)
            {
                var isScriptableObject = typeof(ScriptableObject).IsAssignableFrom(expectedType);
                var objectFieldType = isScriptableObject ? expectedType : typeof(GameObject);
                var currentObj = unityProp.objectReferenceValue;

                EditorGUI.BeginChangeCheck();
                var newObj = EditorGUI.ObjectField(rect, string.Empty, currentObj, objectFieldType, true);
                if (!EditorGUI.EndChangeCheck()) return;
                if (newObj == null)
                {
                    unityProp.objectReferenceValue = null;
                    typeNameProp.stringValue       = null;
                    return;
                }

                UnityEngine.Object finalObj = null;
                if (isScriptableObject)
                {
                    if (expectedType.IsAssignableFrom(newObj.GetType()))
                        finalObj = newObj;
                    else
                        Debug.LogWarning($"Object type {newObj.GetType().Name} is not assignable to {expectedType.Name}");
                }
                else
                {
                    if (newObj is GameObject go)
                    {
                        var component = go.GetComponent(expectedType);
                        if (component != null)
                            finalObj = component;
                        else
                            Debug.LogWarning($"GameObject '{go.name}' does not have component of type {expectedType.Name}");
                    }
                    else if (expectedType.IsAssignableFrom(newObj.GetType()))
                    {
                        finalObj = newObj;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid object: Expected {expectedType.Name} component or GameObject with that component.");
                    }
                }

                if (finalObj != null)
                {
                    unityProp.objectReferenceValue = finalObj;
                    typeNameProp.stringValue       = finalObj.GetType().AssemblyQualifiedName;
                }

                unityProp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Get the current value of the reference, prioritizing the pure C# reference if it exists, otherwise falling back to the UnityEngine.Object reference.
        /// </summary>
        private object GetCurrentValue(SerializedProperty pureProp, SerializedProperty unityProp)
        {
            if (pureProp.managedReferenceValue != null)
                return pureProp.managedReferenceValue;
            return unityProp.objectReferenceValue;
        }

        /// <summary>
        /// Get a Type object from its assembly qualified name, returning null if the name is null or empty.
        /// </summary>
        private Type GetTypeFromName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;
            return Type.GetType(typeName, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
        }

        /// <summary>
        /// Determine the effective type to use for drawing the property, prioritizing the actual type of the current
        /// reference value (either pure or Unity) and falling back to the expected type name if no reference is currently set.
        /// </summary>
        private Type GetCurrentOrExpectedType(SerializedProperty pureProp, SerializedProperty unityProp, SerializedProperty typeNameProp)
        {
            if (pureProp.managedReferenceValue != null)
                return pureProp.managedReferenceValue.GetType();
            if (unityProp.objectReferenceValue != null)
                return unityProp.objectReferenceValue.GetType();
            return GetTypeFromName(typeNameProp.stringValue);
        }

        /// <summary>
        /// Clear both the pure C# reference and the UnityEngine.Object reference, as well as the expected type name,
        /// effectively resetting the property to a null state.
        /// </summary>
        private void ClearReference(SerializedProperty pureProp, SerializedProperty unityProp, SerializedProperty typeNameProp)
        {
            pureProp.managedReferenceValue = null;
            unityProp.objectReferenceValue = null;
            typeNameProp.stringValue       = null;
        }

        /// <summary>
        /// Calculate the total height needed to draw the property, which includes the height of the type selector and the height of the reference field
        /// (either Unity ObjectField or pure C# property fields) based on the currently selected type.
        /// </summary>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + VERTICAL_SPACING;
            var pureProp = property.FindPropertyRelative("pureReference");
            var unityProp = property.FindPropertyRelative("unityReference");
            var typeNameProp = property.FindPropertyRelative("expectedTypeName");
            var effectiveType = GetCurrentOrExpectedType(pureProp, unityProp, typeNameProp);
            if (effectiveType != null)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(effectiveType))
                {
                    height += EditorGUIUtility.singleLineHeight;
                }
                else
                {
                    height += EditorGUI.GetPropertyHeight(pureProp, true);
                }
            }

            return height;
        }
    }
}