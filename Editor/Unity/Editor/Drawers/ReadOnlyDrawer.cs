using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// A custom property drawer for the ReadOnlyAttribute, which makes fields in the Unity Inspector read-only.
    /// </summary>
    [CustomPropertyDrawer(typeof(Enhance.ReadOnlyAttribute))]
    internal class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Maintains the proper spacing for the field in the inspector layout
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Disables the GUI interaction specifically for this field
            GUI.enabled = false;

            // Draws the property in its native style, but un-editable
            EditorGUI.PropertyField(position, property, label, true);

            // Re-enables GUI interaction so subsequent fields remain interactive
            GUI.enabled = true;
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Creates the default property field
            var field = new PropertyField(property);

            // Disables the field to make it read-only
            field.SetEnabled(false);

            return field;
        }
    }
}