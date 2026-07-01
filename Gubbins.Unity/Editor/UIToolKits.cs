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
            var end      = iterator.GetEndProperty();
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
                    var end  = iter.GetEndProperty();
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

#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// Get the value of <paramref name="property"/> as an object.
        /// </summary>
        internal static object GetValue(this SerializedProperty property)
        {
            return property.boxedValue;
        }

        /// <summary>
        /// Set the value of <paramref name="property"/> to <paramref name="value"/>.
        /// </summary>
        internal static void SetValue(this SerializedProperty property, object value)
        {
            property.boxedValue = value;
        }
#else
        /// <summary>
        /// Get the value of <paramref name="property"/> as an object.
        /// </summary>
        internal static object GetValue(this SerializedProperty property)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
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
        /// Set the value of <paramref name="property"/> to <paramref name="value"/>.
        /// </summary>
        internal static void SetValue(this SerializedProperty property, object value)
        {
            var path = property.propertyPath.Replace(".Array.data[", "[");
            object obj = property.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements.Take(elements.Length - 1))
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
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
                var elementName = lastElement.Substring(0, lastElement.IndexOf("["));
                var index = Convert.ToInt32(lastElement.Substring(lastElement.IndexOf("[")).Replace("[", "").Replace("]", ""));
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

        private static object GetFieldOrPropertyValue(object source, string name, int index)
        {
            if (GetFieldOrPropertyValue(source, name) is not System.Collections.IEnumerable enumerable) return null;
            var enumerator = enumerable.GetEnumerator();
            for (var i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }

            var result = enumerator.Current;
            ((IDisposable) enumerator).Dispose();
            return result;
        }

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
#endif
    }
}