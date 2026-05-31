using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Entities;
using UnityEditor;
using UnityEngine;

namespace Gubbins.Editor
{
    [CustomPropertyDrawer(typeof(ComponentSet))]
    internal class ComponentSetDrawer : UnityEditor.PropertyDrawer
    {
        private static readonly Type[] s_ComponentTypes = new ComponentSetDrawer().GetComponentTypes();

        public Type[] GetComponentTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(asm => asm.GetTypes())
                            .Where(t => typeof(IComponent).IsAssignableFrom(t) &&
                                !t.IsAbstract &&
                                !t.ContainsGenericParameters &&
                                CheckIsTypeUnmanaged(t))
                            .ToArray();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (s_ComponentTypes.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No valid component types found.");
                return;
            }
            const int accentLength = 4;
            const int deleteButtonWidth = 20;
            var curComponents = property.FindPropertyRelative("Components");
            var curComponentsTypeNames = GetElementTypeNames(curComponents);
            var accentColor = new Color(127 / 255f, 214 / 255f, 168 / 252f, 1f);
            var deleteButtonColor = new Color(1f, 0.2f, 0.3f, 1f);
            var AddButtonColor = new Color(70 / 255f, 165 / 255f, 255 / 255f, 1f);
            var prevColor = GUI.backgroundColor;

            var y = position.y;
            if (property.isExpanded)
            {
                EditorGUI.DrawRect(new Rect(position.x, y, position.width - deleteButtonWidth, EditorGUIUtility.singleLineHeight), EditorColors.Content);
                property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, position.width - deleteButtonWidth, EditorGUIUtility.singleLineHeight), property.isExpanded, "Components", true, EditorStyles.foldout);
            }
            else
            {
                property.isExpanded = EditorGUI.Foldout(new Rect(position.x, y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight), property.isExpanded, "Components", true);
            }

            y += EditorGUIUtility.singleLineHeight;

            if (property.isExpanded)
            {
                for (var i = 0; i < curComponents.arraySize; i++)
                {
                    var originY = y;
                    var color = i % 2 == 0 ? EditorColors.Background : EditorColors.Background2;
                    var element = curComponents.GetArrayElementAtIndex(i);
                    EditorGUI.DrawRect(new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight), color);
                    var typeName = element.managedReferenceValue.GetType().Name;
                    GUILayout.FlexibleSpace();
                    EditorGUI.DrawRect(new Rect(position.x - accentLength, y, accentLength, EditorGUIUtility.singleLineHeight), accentColor);
                    EditorGUI.LabelField(new Rect(position.x, y, position.width - deleteButtonWidth, EditorGUIUtility.singleLineHeight), $"{typeName}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    y += EditorGUIUtility.singleLineHeight;

                    var prop = element.Copy();
                    var endProp = prop.GetEndProperty();
                    var enterChildren = true;
                    var rectHeight = EditorGUIUtility.singleLineHeight;
                    while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProp))
                    {
                        var propHeight = EditorGUI.GetPropertyHeight(prop, true);
                        EditorGUI.DrawRect(new Rect(position.x - accentLength, y, accentLength, propHeight), accentColor);
                        EditorGUI.DrawRect(new Rect(position.x, y, position.width - deleteButtonWidth, propHeight), color);
                        EditorGUI.PropertyField(new Rect(position.x, y, position.width - deleteButtonWidth, propHeight), prop, true);

                        rectHeight += propHeight;
                        y += propHeight;

                        enterChildren = false;
                    }
                    prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = deleteButtonColor;
                    if (GUI.Button(new Rect(position.x + position.width - deleteButtonWidth, originY, deleteButtonWidth, rectHeight), "×"))
                    {
                        curComponents.DeleteArrayElementAtIndex(i);
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    GUI.backgroundColor = prevColor;
                }

                prevColor = GUI.backgroundColor;
                GUI.backgroundColor = AddButtonColor;
                if (GUI.Button(new Rect(position.x + position.width - deleteButtonWidth, position.y, deleteButtonWidth, EditorGUIUtility.singleLineHeight), "+"))
                {
                    var menu = new GenericMenu();
                    foreach (var type in s_ComponentTypes)
                    {
                        if (curComponentsTypeNames.Contains(type.FullName))
                            continue;
                        menu.AddItem(new GUIContent(type.FullName), false, () =>
                        {
                            curComponents.arraySize++;
                            var newElement = curComponents.GetArrayElementAtIndex(curComponents.arraySize - 1);
                            newElement.managedReferenceValue = Activator.CreateInstance(type);
                            property.serializedObject.ApplyModifiedProperties();
                        });
                    }

                    menu.ShowAsContext();
                }
                GUI.backgroundColor = prevColor;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight; // Foldout
            var curComponents = property.FindPropertyRelative("Components");
            if (property.isExpanded)
            {
                for (var i = 0; i < curComponents.arraySize; i++)
                {
                    var element = curComponents.GetArrayElementAtIndex(i);
                    height += EditorGUIUtility.singleLineHeight; // Label
                    var prop = element.Copy();
                    var endProp = prop.GetEndProperty();
                    var enterChildren = true;
                    while (prop.NextVisible(enterChildren) && !SerializedProperty.EqualContents(prop, endProp))
                    {
                        height += EditorGUI.GetPropertyHeight(prop, true); // Field
                        enterChildren = false;
                    }
                }
            }

            return height;
        }

        /// <summary>
        /// Gets the full type names of the elements in the provided array property,
        /// which is expected to be an array of managed references.
        /// This is used to determine which component types are currently present
        /// in the ComponentSet for the purpose of filtering out those types from the add menu.
        /// </summary>
        /// <param name="arrayProperty"></param>
        /// <returns></returns>
        private static string[] GetElementTypeNames(SerializedProperty arrayProperty)
        {
            var elementTypes = new List<string>();
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                var element = arrayProperty.GetArrayElementAtIndex(i);
                var type = element.managedReferenceFullTypename;
                if (type != null)
                {
                    elementTypes.Add(type);
                }
            }

            return elementTypes.ToArray();
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

            // A type is considered unmanaged if it is a value type and all of its fields are either of the same type (to allow for recursive structs) or are unmanaged types.
            var isUnmanaged = !type.GetFields().Any(f => f.FieldType != type && !CheckIsTypeUnmanaged(f.FieldType));
            return isUnmanaged;
        }
    }
}