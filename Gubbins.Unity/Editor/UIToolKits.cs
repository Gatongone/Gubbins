using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace Gubbins.Editor
{
    internal static class UIToolKits
    {
#if UNITY_2022_2_OR_NEWER
        internal static object GetValue(this SerializedProperty property)
        {
            return property.boxedValue;
        }

        internal static void SetValue(this SerializedProperty property, object value)
        {
           return property.boxedValue = value;
        }
#else
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