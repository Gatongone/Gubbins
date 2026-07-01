using System;
using System.Collections.Generic;
using System.Reflection;
using Gubbins.Enhance;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// Hides the label of a scalar/struct field marked with <see cref="HideLabelAttribute"/>.
    /// Arrays/lists are handled by <see cref="HideLabelEditor"/> instead: Unity does not apply
    /// attribute-based property drawers to collection fields (by design), so a PropertyDrawer can
    /// never reach an array's foldout header.
    /// </summary>
    [CustomPropertyDrawer(typeof(HideLabelAttribute))]
    internal class HideLabelDrawer : PropertyDrawer
    {
        /// <summary>
        /// UI Toolkit version.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property) =>
            new PropertyField(property, string.Empty);

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, GUIContent.none, property);
            EditorGUI.PropertyField(position, property, GUIContent.none, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, GUIContent.none, true);
    }

    /// <summary>
    /// UI Toolkit does not apply attribute-based property drawers to array/list fields, so
    /// <see cref="HideLabelDrawer"/> can never reach an array's foldout header (expand arrow +
    /// field name + element-count). This editor fills the default inspector and then turns that header
    /// off for every collection field marked with <see cref="HideLabelAttribute"/>, leaving the elements
    /// laid out directly. Scalar/struct fields are still handled by the property drawer.
    ///
    /// It is registered for all MonoBehaviours and ScriptableObjects; types that declare their own
    /// <c>[CustomEditor]</c> keep that editor (and simply won't get this collection-header handling).
    /// </summary>
    internal abstract class HideLabelEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            // Field name == top-level bindingPath; only collections need header stripping.
            var hideFields = CollectHideLabelCollectionFields(target.GetType());
            if (hideFields.Count == 0)
            {
                return root;
            }

            // The ListViews don't exist until the inspector has been laid out and bound, so defer the
            // header removal to the first geometry pass and keep reapplying it (a ListView rebuild would
            // otherwise restore its header).
            var addHooked = new HashSet<ListView>();
            root.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                foreach (var field in root.Query<PropertyField>().ToList())
                {
                    if (!hideFields.Contains(field.bindingPath))
                    {
                        continue;
                    }

                    if (field.Q<ListView>() is { } listView)
                    {
                        listView.showFoldoutHeader       = false;
                        listView.showBoundCollectionSize = false;

                        // Unity's default "+" duplicates the last serialized element; reset freshly added
                        // entries to their default value instead. Wire once per ListView.
                        if (addHooked.Add(listView))
                        {
                            var path = field.bindingPath;
                            listView.itemsAdded += indices => ResetAddedElements(path, indices, listView);
                        }
                    }
                }
            });

            return root;
        }

        /// <summary>
        /// Reset the array elements at <paramref name="indices"/> (just appended by the default add button,
        /// and therefore copies of the previous element) back to their default value.
        /// </summary>
        private void ResetAddedElements(string arrayPath, IEnumerable<int> indices, ListView listView)
        {
            if (serializedObject.FindProperty(arrayPath) is not {isArray: true} arrayProperty)
            {
                return;
            }

            foreach (var index in indices)
            {
                if (index >= 0 && index < arrayProperty.arraySize)
                {
                    ResetToDefault(arrayProperty.GetArrayElementAtIndex(index));
                }
            }

            serializedObject.ApplyModifiedProperties();
            // Apply happens during the add operation; refresh the rows on the next tick so they show defaults.
            listView.schedule.Execute(listView.RefreshItems);
        }

        /// <summary>
        /// Recursively reset a <see cref="SerializedProperty"/> to its default value. Handles managed
        /// references (<c>[SerializeReference]</c>) and arrays, which <c>boxedValue</c> cannot.
        /// </summary>
        private static void ResetToDefault(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.ManagedReference)
            {
                property.managedReferenceValue = null;
                return;
            }

            if (property.isArray && property.propertyType != SerializedPropertyType.String)
            {
                property.ClearArray();
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:          property.longValue             = 0; break;
                case SerializedPropertyType.Boolean:          property.boolValue             = false; break;
                case SerializedPropertyType.Float:            property.doubleValue           = 0; break;
                case SerializedPropertyType.String:           property.stringValue           = string.Empty; break;
                case SerializedPropertyType.Color:            property.colorValue            = default; break;
                case SerializedPropertyType.ObjectReference:  property.objectReferenceValue  = null; break;
                case SerializedPropertyType.LayerMask:        property.intValue              = 0; break;
                case SerializedPropertyType.Enum:             property.enumValueFlag         = 0; break;
                case SerializedPropertyType.Vector2:          property.vector2Value          = default; break;
                case SerializedPropertyType.Vector3:          property.vector3Value          = default; break;
                case SerializedPropertyType.Vector4:          property.vector4Value          = default; break;
                case SerializedPropertyType.Rect:             property.rectValue             = default; break;
                case SerializedPropertyType.ArraySize:        property.intValue              = 0; break;
                case SerializedPropertyType.Character:        property.intValue              = 0; break;
                case SerializedPropertyType.AnimationCurve:   property.animationCurveValue   = null; break;
                case SerializedPropertyType.Bounds:           property.boundsValue           = default; break;
                case SerializedPropertyType.Quaternion:       property.quaternionValue       = Quaternion.identity; break;
                case SerializedPropertyType.ExposedReference: property.exposedReferenceValue = null; break;
                case SerializedPropertyType.Vector2Int:       property.vector2IntValue       = default; break;
                case SerializedPropertyType.Vector3Int:       property.vector3IntValue       = default; break;
                case SerializedPropertyType.RectInt:          property.rectIntValue          = default; break;
                case SerializedPropertyType.BoundsInt:        property.boundsIntValue        = default; break;
                default:
                    // Generic struct/class: reset each direct child. Recursion on a copy lets the outer
                    // iterator skip the subtree via NextVisible(false).
                    if (property.propertyType == SerializedPropertyType.Generic && property.hasChildren)
                    {
                        var child = property.Copy();
                        var end = property.GetEndProperty();
                        if (child.NextVisible(true))
                        {
                            do
                            {
                                ResetToDefault(child);
                            } while (child.NextVisible(false) &&
                                !SerializedProperty.EqualContents(child, end));
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Collect the names of all collection fields carrying <see cref="HideLabelAttribute"/>, walking
        /// the type hierarchy so inherited fields are covered too.
        /// </summary>
        private static HashSet<string> CollectHideLabelCollectionFields(Type type)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var result = new HashSet<string>();

            for (var current = type;
                current != null && current != typeof(MonoBehaviour) && current != typeof(ScriptableObject);
                current = current.BaseType)
            {
                foreach (var field in current.GetFields(flags))
                {
                    if (field.GetCustomAttribute<HideLabelAttribute>() != null && IsCollection(field.FieldType))
                    {
                        result.Add(field.Name);
                    }
                }
            }

            return result;
        }

        private static bool IsCollection(Type type) =>
            type != typeof(string) &&
            (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)));
    }

    /// <summary>
    /// Registered for all MonoBehaviours to strip collection headers of fields marked
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MonoBehaviour), editorForChildClasses: true)]
    internal sealed class HideLabelMonoBehaviourEditor : HideLabelEditor { }

    /// <summary>
    /// Registered for all ScriptableObjects to strip collection headers of fields marked
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ScriptableObject), editorForChildClasses: true)]
    internal sealed class HideLabelScriptableObjectEditor : HideLabelEditor { }
}