using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Entities;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gubbins.Editor
{
    [CustomPropertyDrawer(typeof(ComponentSet))]
    internal class ComponentSetDrawer : PropertyDrawer
    {
        /// <summary>
        /// All Component types.
        /// </summary>
        private static readonly Type[] s_ComponentTypes = new ComponentSetDrawer().GetComponentTypes();

        /// <summary>
        /// Each SerializedProperty corresponds to a ReorderableList, cached with the path as the key.
        /// </summary>
        private readonly Dictionary<string, ReorderableList> m_ListCache = new();

        private static GUIStyle s_FoldoutHeaderStyle => new(EditorStyles.foldoutHeader)
        {
            margin  = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(14, 0, 0, 0),
            fontStyle = FontStyle.Bold
        };

        /// <summary>
        /// Retrieves all valid component types that can be added to the ComponentSet. A valid component type is defined as:
        /// - Implements the IComponent interface.
        /// - Is not abstract.
        /// - Does not contain generic parameters.
        /// - Is an unmanaged type (value type without reference type fields).
        /// </summary>
        public Type[] GetComponentTypes() => AppDomain.CurrentDomain.GetAssemblies()
                                                      .SelectMany(asm => asm.GetTypes())
                                                      .Where(t => typeof(IComponent).IsAssignableFrom(t) &&
                                                          !t.IsAbstract &&
                                                          !t.ContainsGenericParameters &&
                                                          CheckIsTypeUnmanaged(t))
                                                      .ToArray();
        /// <inheritdoc/>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (s_ComponentTypes.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No valid component types found.");
                return;
            }

            var componentsProp = property.FindPropertyRelative("Components");
            var isExpanded = property.isExpanded;

            // Draw foldout header with count label.
            var foldoutRect = new Rect(position.x + 15, position.y, position.width - 20, EditorGUIUtility.singleLineHeight);
            var countLabel = $"({componentsProp.arraySize})";
            var fullLabel = new GUIContent($"{label.text} {countLabel}");
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + 2);
            Handles.DrawSolidRectangleWithOutline(headerRect, Color.clear, Color.black);
            var newExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, isExpanded, fullLabel, s_FoldoutHeaderStyle);
            EditorGUI.EndFoldoutHeaderGroup();
            property.isExpanded = newExpanded;

            // Draw box around the list when expanded.
            if (newExpanded)
            {
                var list = GetOrCreateList(property);
                var listRect = new Rect(position.x, position.y + headerRect.height, position.width, position.height - headerRect.height);
                list.DoList(listRect);
            }
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + 4; // Property itself height.
            if (property.isExpanded)
            {
                height += GetOrCreateList(property).GetHeight();
            }
            return height;
        }

        /// <summary>
        /// Get or create a ReorderableList for the given SerializedProperty.
        /// The list is cached based on the property's path to ensure that each property gets its own list instance.
        /// </summary>
        private ReorderableList GetOrCreateList(SerializedProperty property)
        {
            var componentsProp = property.FindPropertyRelative("Components");
            var key = property.propertyPath;

            if (!m_ListCache.TryGetValue(key, out var list))
            {
                list = new ReorderableList(property.serializedObject, componentsProp, true, false, true, true);

                list.elementHeightCallback = index => GetElementHeight(list.serializedProperty, index);
                list.drawElementCallback   = (rect, index, isActive, isFocused) => DrawElement(rect, index, list.serializedProperty);
                list.onAddDropdownCallback = (buttonRect, rlist) => ShowAddComponentMenu(buttonRect, property);
                list.footerHeight          = EditorGUIUtility.singleLineHeight;

                m_ListCache[key] = list;
            }
            else
            {
                // Make sure to update the serialized property reference in case the same drawer instance is reused for a different property.
                list.serializedProperty = componentsProp;
            }

            return list;
        }

        /// <summary>
        /// Calculates the height of a single element in the list by summing the heights of all its visible properties,
        /// including the type name label at the top. This ensures that each element is tall enough
        /// to display all its content without clipping, regardless of how many properties it has or their individual heights.
        /// </summary>
        private static float GetElementHeight(SerializedProperty arrayProperty, int index)
        {
            var element = arrayProperty.GetArrayElementAtIndex(index);
            var height = EditorGUIUtility.singleLineHeight; // Type name label height

            var prop = element.Copy();
            var endProp = prop.GetEndProperty();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(prop, endProp))
            {
                height        += EditorGUI.GetPropertyHeight(prop, true);
                enterChildren =  false;
            }

            return height;
        }

        /// <summary>
        /// Draws a single element in the list, which includes a type name label at the top and all
        /// visible properties of the component below it.
        /// </summary>
        private static void DrawElement(Rect rect, int index, SerializedProperty arrayProperty)
        {
            var element = arrayProperty.GetArrayElementAtIndex(index);
            var typeName = element.managedReferenceValue.GetType().Name;

            // Type name label at the top of the element
            var labelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, typeName, EditorStyles.boldLabel);

            // Draw all visible properties of the component below the type name
            var y = rect.y + EditorGUIUtility.singleLineHeight;
            var prop = element.Copy();
            var endProp = prop.GetEndProperty();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(prop, endProp))
            {
                var propHeight = EditorGUI.GetPropertyHeight(prop, true);
                var propRect = new Rect(rect.x, y, rect.width, propHeight);
                EditorGUI.PropertyField(propRect, prop, true);
                y             += propHeight;
                enterChildren =  false;
            }
        }

        /// <summary>
        /// Shows a dropdown menu to add a new component to the list.
        /// The menu is populated with all valid component types that are not already in the list.
        /// </summary>
        private static void ShowAddComponentMenu(Rect buttonRect, SerializedProperty property)
        {
            var componentsProp = property.FindPropertyRelative("Components");

            // Get existing component types in the list to filter out from the menu
            var existingTypes = new HashSet<Type>();
            for (var i = 0; i < componentsProp.arraySize; i++)
            {
                var element = componentsProp.GetArrayElementAtIndex(i);
                var obj = element.managedReferenceValue;
                if (obj != null)
                    existingTypes.Add(obj.GetType());
            }

            // Filter available component types to only those not already in the list
            var availableTypes = s_ComponentTypes.Where(t => !existingTypes.Contains(t)).ToArray();

            if (availableTypes.Length == 0)
                return;

            var menu = new GenericMenu();
            foreach (var type in availableTypes)
            {
                var capturedType = type;
                var capturedPropertyPath = property.propertyPath;
                var targetObject = property.serializedObject;
                var so = targetObject;
                var prop = so.FindProperty(capturedPropertyPath).FindPropertyRelative("Components");

                menu.AddItem(new GUIContent(capturedType.FullName), false, () =>
                {
                    prop.arraySize++;
                    var newElement = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(capturedType);
                    so.ApplyModifiedProperties();
                });
            }

            menu.DropDown(buttonRect);
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
    }
}