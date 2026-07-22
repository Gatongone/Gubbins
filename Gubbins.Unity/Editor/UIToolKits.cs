using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Gubbins.Editor
{
    /// <summary>
    /// A collection of utility methods for working with Unity's <see cref="SerializedProperty"/> class.
    /// </summary>
    internal static class UIToolKits
    {
        /// <summary>
        /// Return the immediate child <see cref="SerializedProperty"/> instances of <paramref name="property"/> —
        /// i.e. every serialized field/property of the type it currently represents. This walks the live serialized
        /// data rather than reflecting over a type, so it works for <c>[SerializeReference]</c> managed references
        /// (where <see cref="SerializedProperty.FindPropertyRelative"/> by name is unreliable). Leaf properties and
        /// object references — which have no embedded children — yield an empty array.
        /// </summary>
        internal static SerializedProperty[] GetChildren(this SerializedProperty property)
        {
            var children = new List<SerializedProperty>();
            if (!property.hasChildren || property.propertyType == SerializedPropertyType.ObjectReference)
            {
                return children.ToArray();
            }

            var iterator = property.Copy();
            var end = iterator.GetEndProperty();
            if (iterator.Next(true))
            {
                while (!SerializedProperty.EqualContents(iterator, end))
                {
                    children.Add(iterator.Copy());
                    if (!iterator.Next(false)) break;
                }
            }

            return children.ToArray();
        }

        /// <summary>
        /// Clear the value of <paramref name="property"/> according to its type: arrays are cleared of all elements;
        /// </summary>
        internal static void Clear(this SerializedProperty property)
        {
            if (property.isArray)
            {
                property.ClearArray();
                return;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    return;
                case SerializedPropertyType.ManagedReference:
                    property.managedReferenceValue = null;
                    return;
                case SerializedPropertyType.String:
                    property.stringValue = string.Empty;
                    return;
                case SerializedPropertyType.Generic:
                {
                    // boxedValue cannot handle types that contain [SerializeReference] fields
                    // (e.g. SerializedReference<T>), so recurse into children instead.
                    var iter = property.Copy();
                    var end = iter.GetEndProperty();
                    if (iter.Next(true))
                    {
                        while (!SerializedProperty.EqualContents(iter, end))
                        {
                            iter.Copy().Clear();
                            if (!iter.Next(false)) break;
                        }
                    }

                    return;
                }
            }

            if (property.GetValue() != null)
            {
                property.SetValue(null);
            }
        }

        /// <summary>
        /// Get the value of <paramref name="property"/> as an object, using reflection to traverse the serialized data.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to get the value of.</param>
        /// <returns>The value of the property as an object, or null if the property is not valid.</returns>
        private static object GetValue(this SerializedProperty property)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    var index = Convert.ToInt32(element[element.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "").Replace("]", ""));
                    obj = GetFieldOrPropertyValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldOrPropertyValue(obj, element);
                }
            }

            return obj;
        }

        /// <summary>
        /// Set the value of <paramref name="property"/> to <paramref name="value"/>, using reflection to traverse the serialized data.
        /// </summary>
        /// <param name="property">The <see cref="SerializedProperty"/> to set the value of.</param>
        /// <param name="value">The value to set the property to.</param>
        private static void SetValue(this SerializedProperty property, object value)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element[..element.IndexOf("[", StringComparison.Ordinal)];
                    var index = Convert.ToInt32(element[element.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "").Replace("]", ""));
                    obj = GetFieldOrPropertyValue(obj, elementName, index);
                }
                else
                {
                    obj = GetFieldOrPropertyValue(obj, element);
                }
            }

            if (ReferenceEquals(obj, null)) return;

            var lastElement = elements.Last();

            if (lastElement.Contains("["))
            {
                var tp = obj.GetType();
                var elementName = lastElement[..lastElement.IndexOf("[", StringComparison.Ordinal)];
                var index = Convert.ToInt32(lastElement[lastElement.IndexOf("[", StringComparison.Ordinal)..].Replace("[", "").Replace("]", ""));
                var field = tp.GetField(elementName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                var arr = field.GetValue(obj) as System.Collections.IList;
                arr[index] = value;
            }
            else
            {
                var tp = obj.GetType();
                var field = tp.GetField(lastElement, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(obj, value);
                }
            }
        }

        /// <summary>
        /// Get the value of a field or property from an object, where the field or property is an enumerable and we want the value at a specific index.
        /// </summary>
        /// <param name="source">The object to get the field or property value from.</param>
        /// <param name="name">The name of the field or property to get the value of.</param>
        /// <param name="index">The index of the value to get from the enumerable field or property.</param>
        /// <returns>The value of the field or property at the specified index, or null if the field or property is not found or is not an enumerable.</returns>
        private static object GetFieldOrPropertyValue(object source, string name, int index)
        {
            if (GetFieldOrPropertyValue(source, name) is not System.Collections.IEnumerable enumerable) return null;
            var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }

            var result = enumerator.Current;
            if (enumerator is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Get the value of a field or property from an object, using reflection to find the field or property by name.
        /// </summary>
        /// <param name="obj">The object to get the field or property value from.</param>
        /// <param name="name">The name of the field or property to get the value of.</param>
        /// <returns>The value of the field or property, or null if the field or property is not found.</returns>
        private static object GetFieldOrPropertyValue(object obj, string name)
        {
            if (obj == null) return null;
            var type = obj.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(obj);
            var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (property != null) return property.GetValue(obj, null);

            return null;
        }
    }
}