using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Context;
using Gubbins.Enhance;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    [CustomPropertyDrawer(typeof(SerializedInstallInfo))]
    public class InstallInfoDrawer : PropertyDrawer
    {
        private struct Properties
        {
            public readonly SerializedProperty Scope;
            public readonly SerializedProperty Key;
            public readonly SerializedProperty Type;
            public readonly SerializedProperty Bindings;
            public readonly SerializedProperty Controller;
            public readonly SerializedProperty Spawner;
            public readonly SerializedProperty Prewarm;
            public readonly SerializedProperty Prototype;

            public Properties(SerializedProperty property)
            {
                Scope      = property.FindPropertyRelative(nameof(SerializedInstallInfo.Scope));
                Key        = property.FindPropertyRelative(nameof(SerializedInstallInfo.Key));
                Type       = property.FindPropertyRelative(nameof(SerializedInstallInfo.Type));
                Bindings   = property.FindPropertyRelative(nameof(SerializedInstallInfo.Bindings));
                Controller = property.FindPropertyRelative(nameof(SerializedInstallInfo.Controller));
                Spawner    = property.FindPropertyRelative(nameof(SerializedInstallInfo.Spawner));
                Prewarm    = property.FindPropertyRelative(nameof(SerializedInstallInfo.Prewarm));
                Prototype  = property.FindPropertyRelative(nameof(SerializedInstallInfo.Prototype));
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Create a container for the property fields
            var root = new VisualElement();
            var properties = new Properties(property);
            var scope = CreatePropertyField(properties.Scope);
            var key = CreatePropertyField(properties.Key);
            var type = CreatePropertyField(properties.Type);
            var bindings = CreateBindingProperty(properties.Bindings, properties.Type);
            var controller = CreatePropertyField(properties.Controller);
            var spawner = CreatePropertyField(properties.Spawner);
            var prewarm = CreatePropertyField(properties.Prewarm);
            var prototype = CreatePropertyField(properties.Prototype);
            var currentScope = (Scope) properties.Scope.enumValueIndex;
            var serializedType = (SerializedType) properties.Type.boxedValue;

            // Build root.
            root.Add(key);
            root.Add(scope);
            root.Add(type);
            if (currentScope is not Scope.Multiton && serializedType.Type != null && !serializedType.Type.IsAssignableFrom(typeof(UnityEngine.Object)))
            {
                root.Add(prototype);
            }

            root.Add(spawner);
            if (currentScope is not (Scope.Singleton or Scope.Multiton))
            {
                root.Add(controller);
            }

            root.Add(prewarm);
            if (bindings != null)
            {
                root.Add(bindings);
            }

            // Register callbacks.
            scope.RegisterValueChangeCallback(evt =>
            {
                var newScope = (Scope) evt.changedProperty.enumValueIndex;
                if (currentScope == newScope)
                {
                    return;
                }

                currentScope = newScope;
                if (newScope is Scope.Singleton or Scope.Multiton)
                {
                    ClearProperty(properties.Controller);
                    if (root.Contains(controller))
                    {
                        root.Remove(controller);
                    }

                    controller.MarkDirtyRepaint();
                }
                else
                {
                    if (!root.Contains(controller))
                    {
                        root.Insert(root.IndexOf(spawner), controller);
                    }
                }

                if (newScope is not Scope.Multiton)
                {
                    properties.Prewarm.intValue = Math.Clamp(properties.Prewarm.intValue, 0, 1);
                }

                property.serializedObject.ApplyModifiedProperties();
            });
            type.RegisterValueChangeCallback(evt =>
            {
                var changedType = (SerializedType) evt.changedProperty.boxedValue;
                var curScope = (Scope) properties.Scope.enumValueIndex;
                ClearProperty(properties.Prototype);
                ClearProperty(properties.Bindings);
                properties.Key.stringValue = changedType.Type != null ? changedType.Type.ToString() : string.Empty;

                if (root.Contains(prototype) && (curScope is Scope.Multiton || changedType.Type == null || !changedType.Type.IsAssignableFrom(typeof(UnityEngine.Object))))
                {
                    root.Remove(prototype);
                }
                else if (!root.Contains(prototype))
                {
                    root.Insert(root.IndexOf(type) + 1, prototype);
                }

                if (root.Contains(bindings))
                {
                    root.Remove(bindings);
                }

                if (changedType.Type != null)
                {
                    bindings = CreateBindingProperty(properties.Bindings, properties.Type);
                    if (bindings != null)
                    {
                        root.Add(bindings);
                    }
                }

                property.serializedObject.ApplyModifiedProperties();
            });
            return root;
        }

        private static void ClearProperty(SerializedProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.isArray)
            {
                property.ClearArray();
            }
            else if (property.boxedValue != null)
            {
                property.boxedValue = null;
            }
            else if (property.objectReferenceValue != null)
            {
                property.objectReferenceValue = null;
            }
        }

        private PropertyField CreatePropertyField(SerializedProperty property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var field = new PropertyField(property, property.displayName);
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            return field;
        }

        /// <summary>
        /// Draws the binding types of the <see cref="type"/> that must could be assignable from.
        /// </summary>
        private VisualElement CreateBindingProperty(SerializedProperty property, SerializedProperty type)
        {
            Type actualType = null;
            if (type?.boxedValue is SerializedType serializedType)
            {
                actualType = serializedType.Type;
            }

            if (actualType == null)
            {
                return null;
            }

            var bindingTypes = new List<Type>();
            GetAllBaseTypes(actualType, bindingTypes);
            GetAllInterfaces(actualType, bindingTypes);

            var borderCol = new Color(0.1f, 0.1f, 0.1f);
            const int borderRadius = 4;
            var foldout = new Foldout
            {
                value = property.isExpanded,
                style =
                {
                    borderTopWidth          = 1,
                    borderRightWidth        = 1,
                    borderBottomWidth       = 1,
                    borderLeftWidth         = 1,
                    paddingLeft             = 15,
                    marginLeft              = 0,
                    marginRight             = 0,
                    marginTop               = 2,
                    marginBottom            = 2,
                    borderTopColor          = borderCol,
                    borderRightColor        = borderCol,
                    borderBottomColor       = borderCol,
                    borderLeftColor         = borderCol,
                    borderBottomLeftRadius  = borderRadius,
                    borderBottomRightRadius = borderRadius,
                    borderTopLeftRadius     = borderRadius,
                    borderTopRightRadius    = borderRadius
                }
            };
            foldout.contentContainer.style.paddingLeft  = 0;
            foldout.contentContainer.style.marginLeft   = 0;
            foldout.contentContainer.style.paddingRight = 10;
            foldout.contentContainer.style.marginRight  = 10;
            foldout.style.unityFontStyleAndWeight       = FontStyle.Bold;
            foldout.contentContainer.style.paddingLeft  = 0;
            foldout.contentContainer.style.marginLeft   = 0;
            foldout.contentContainer.style.paddingRight = 5;
            foldout.contentContainer.style.marginRight  = 5;

            var listView = new ListView
            {
                reorderable = true,

                showAddRemoveFooter     = true,
                showBoundCollectionSize = false,
                itemsSource             = ExtractArrayProperties(property),
                reorderMode             = ListViewReorderMode.Animated,
                selectionType           = SelectionType.Single,
#if UNITY_2023_2_OR_NEWER
                onAdd = OnAdd,
                onRemove = OnRemove,
                allowAdd = true,
                allowRemove = true,
#endif
                showBorder = true,
                bindItem   = BindItem,
                makeItem   = static () => new Label(),
                style =
                {
                    paddingBottom = 10
                }
            };
            listView.itemIndexChanged += OnRecord;
            foldout.text              =  property.displayName;
            foldout.Add(listView);
#if !UNITY_2023_2_OR_NEWER
            listView.itemsAdded += items =>
            {
                foreach (var item in items)
                {
                    var menu = new GenericMenu();
                    var boundedTypes = ExtractArrayProperties(property);
                    foreach (var bindingType in bindingTypes.Except(boundedTypes).OrderBy(t => t.ToString()))
                    {
                        menu.AddItem(new GUIContent(TypeName.GetFriendlyTypeFullName(bindingType)), false, () =>
                        {
                            property.arraySize++;
                            var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                            element.boxedValue = new SerializedType(bindingType);
                            property.serializedObject.ApplyModifiedProperties();
                            RefreshList(listView);
                        });
                    }

                    menu.ShowAsContext();
                }
            };
            listView.itemsRemoved += items =>
            {
                foreach (var item in items)
                {
                    if (item < 0 || item >= property.arraySize)
                    {
                        continue;
                    }

                    property.DeleteArrayElementAtIndex(item);
                    property.serializedObject.ApplyModifiedProperties();
                }

                RefreshList(listView);
            };
#endif
            return foldout;

            void OnRecord(int start, int end)
            {
                var newOrder = (IList<Type>) listView.itemsSource;
                property.ClearArray();
                property.arraySize = newOrder.Count;
                for (var i = 0; i < newOrder.Count; i++)
                {
                    var elem = property.GetArrayElementAtIndex(i);
                    elem.boxedValue = new SerializedType(newOrder[i]);
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            void RefreshList(BaseListView list)
            {
                list.itemsSource = ExtractArrayProperties(property);
                list.Rebuild();
            }

            void BindItem(VisualElement element, int index)
            {
                var label = (Label) element;
                var types = ExtractArrayProperties(property);
                label.text = index < types.Count ? TypeName.GetFriendlyTypeFullName(types[index]) : "null";
            }

            void OnAdd(BaseListView list)
            {
                var menu = new GenericMenu();
                var boundedTypes = ExtractArrayProperties(property);
                foreach (var bindingType in bindingTypes.Except(boundedTypes).OrderBy(t => t.ToString()))
                {
                    menu.AddItem(new GUIContent(TypeName.GetFriendlyTypeFullName(bindingType)), false, () =>
                    {
                        property.arraySize++;
                        var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                        element.boxedValue = new SerializedType(bindingType);
                        property.serializedObject.ApplyModifiedProperties();
                        RefreshList(list);
                    });
                }

                menu.ShowAsContext();
            }

            void OnRemove(BaseListView list)
            {
                if (list.selectedIndex < 0 || list.selectedIndex >= property.arraySize)
                {
                    return;
                }

                property.DeleteArrayElementAtIndex(list.selectedIndex);
                property.serializedObject.ApplyModifiedProperties();
                list.RemoveAt(list.selectedIndex);
                RefreshList(list);
            }

            List<Type> ExtractArrayProperties(SerializedProperty arrayProperty)
            {
                // Sanity check.
                if (!arrayProperty.isArray)
                {
                    throw new ArgumentException("Expected an array property.", nameof(arrayProperty));
                }

                var types = new List<Type>();
                for (var i = 0; i < arrayProperty.arraySize; i++)
                {
                    var element = arrayProperty.GetArrayElementAtIndex(i);
                    var elementType = (SerializedType) element.boxedValue;
                    if (elementType.Type != null)
                    {
                        types.Add(elementType.Type);
                    }
                }

                return types;
            }

            void GetAllBaseTypes(Type t, List<Type> baseTypes)
            {
                var baseType = t.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    baseTypes.Add(baseType);
                    GetAllBaseTypes(baseType, baseTypes);
                }
            }

            void GetAllInterfaces(Type t, List<Type> interfaces)
            {
                if (t == null)
                {
                    return;
                }

                var typeInterfaces = t.GetInterfaces();
                interfaces.AddRange(typeInterfaces);
                foreach (var typeInterface in typeInterfaces)
                {
                    GetAllInterfaces(typeInterface, interfaces);
                }
            }
        }
    }
}