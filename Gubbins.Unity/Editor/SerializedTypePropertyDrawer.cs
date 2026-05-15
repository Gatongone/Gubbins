// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/05/22-22:58:10
// Github: https://github.com/Gatongone

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Gubbins.Enhance;
using Gubbins.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// Property drawer for <see cref="SerializedType"/> that could be with <see cref="TypeFromAttribute"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(TypeFromAttribute))]
    internal sealed class SerializedTypePropertyDrawer : PropertyDrawer
    {
        /// <summary>
        /// All types in current domain.
        /// </summary>
        private static readonly Type[] s_AllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).ToArray();

        /// <summary>
        /// UIElement version.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (!TryGetTypes(out var types)) return new VisualElement();

            var container = new VisualElement();
            var typeString = property.FindPropertyRelative("m_TypeString");
            var index = typeString.stringValue == null ? 0 : Array.IndexOf(types, typeString.stringValue);
            if (index < 0) index = 0;
            var dropdown = new DropdownField(FormatNaming(property.name), types.ToList(), index);

            dropdown.RegisterValueChangedCallback(evt =>
            {
                typeString.stringValue = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            container.Insert(0, dropdown);

            return container;
        }

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!TryGetTypes(out var types)) return;

            var typeString = property.FindPropertyRelative("m_TypeString");
            var index = typeString.stringValue == null ? 0 : Array.IndexOf(types, typeString.stringValue);
            if (index < 0) index = 0;
            var newIndex = EditorGUI.Popup(position, FormatNaming(label.text), index, types);
            if (newIndex == index) return;
            typeString.stringValue = types[newIndex];
            property.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Try get target types from <see cref="SerializedType"/>.
        /// </summary>
        /// <param name="types">Types matched <see cref="TypeFromAttribute"/>.</param>
        /// <returns>True when the type field type is <see cref="SerializedType"/>.</returns>
        private bool TryGetTypes(out string[] types)
        {
            var typeFrom = attribute as TypeFromAttribute;
            types = null;

            if (fieldInfo.FieldType != typeof(SerializedType)) return false;

            var typeKind = typeFrom?.Kind ?? TypeKind.All;
            var targetTypes = typeFrom is {Type: not null} ? GetAllSubTypes(typeFrom.Type) : s_AllTypes;

            types = targetTypes.Where(type => VerifyTypeKind(typeKind, type) && !typeFrom.Exclude.Contains(type))
                               .Select(type => type.ToString())
                               .ToArray();
            return true;
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
        private static string FormatNaming(string name)
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
        /// Get all types which could assignable to <c>type</c>.
        /// </summary>
        /// <param name="type">Selected target type.</param>
        private static Type[] GetAllSubTypes(Type type) => s_AllTypes.Where(type.IsAssignableFrom).ToArray();

        /// <summary>
        /// Verify the type is matched to the type kind.
        /// </summary>
        private static bool VerifyTypeKind(TypeKind typeKind, Type type)
        {
            // Seems we don't need to do reflection all the time.
            if (typeKind == TypeKind.All) return true;
            if (typeKind.HasFlag(TypeKind.Implementation) && type.IsAbstract) return false;
            if (typeKind.HasFlag(TypeKind.Interface) && !type.IsInterface) return false;
            if (typeKind.HasFlag(TypeKind.Abstract) && !type.IsAbstract) return false;
            if (typeKind.HasFlag(TypeKind.Newable) && !type.IsNewable(out _)) return false;
            if (typeKind.HasFlag(TypeKind.Struct) && !typeKind.HasFlag(TypeKind.Newable) && !type.IsValueType) return false;
            if (typeKind.HasFlag(TypeKind.Class) && type.IsValueType) return false;
            return true;
        }
    }
}