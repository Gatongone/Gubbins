using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Spawner;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

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
            var prototype = CreatePrototypeField(properties.Prototype, properties.Type);
            var currentScope = (Scope) properties.Scope.enumValueIndex;
            var serializedType = (SerializedType) properties.Type.GetValue();

            // Build root.
            root.Add(key);
            root.Add(scope);
            root.Add(type);
            if (serializedType.Type != null && typeof(Object).IsAssignableFrom(serializedType.Type))
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
            scope.RegisterValueChangeCallback(OnScopeChanged);
            type.RegisterValueChangeCallback(OnTypeChanged);
            prototype.RegisterValueChangedCallback(OnPrototypeChanged);
            return root;

            void OnScopeChanged(SerializedPropertyChangeEvent evt)
            {
                var newScope = (Scope) evt.changedProperty.enumValueIndex;
                if (currentScope == newScope)
                {
                    return;
                }

                currentScope = newScope;
                if (newScope is Scope.Singleton or Scope.Multiton)
                {
                    properties.Controller.Clear();
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
            }

            void OnTypeChanged(SerializedPropertyChangeEvent evt)
            {
                var changedType = (SerializedType) evt.changedProperty.GetValue();
                properties.Prototype.Clear();
                properties.Bindings.Clear();
#if UNITY_2023_2_OR_NEWER
                AssignSpawner(properties.Spawner, SelectSpawnerType(changedType.Type));
#endif
                properties.Key.stringValue = changedType.Type != null ? changedType.Type.ToString() : string.Empty;
                var canAssignObject = changedType.Type != null && typeof(Object).IsAssignableFrom(changedType.Type);

                if (root.Contains(prototype) && !canAssignObject)
                {
                    root.Remove(prototype);
                }
                else if (!root.Contains(prototype) && canAssignObject)
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

                // Rebuild the spawner field so its dropdown reflects the new product-type constraint.
                var spawnerIndex = root.IndexOf(spawner);
                if (spawnerIndex >= 0)
                {
                    root.Remove(spawner);
                    spawner = CreatePropertyField(properties.Spawner);
                    root.Insert(spawnerIndex, spawner);
                }
            }

            void OnPrototypeChanged(ChangeEvent<Object> evt)
            {
                var newPrototype = evt.newValue;
                if (newPrototype == null)
                {
                    return;
                }

                var targetType = properties.Type.GetValue() is SerializedType st ? st.Type : null;
                if (targetType != null && !IsValidPrototype(newPrototype, targetType))
                {
                    return;
                }

                WirePrototypeToSpawner(properties.Spawner, newPrototype);
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

        private ObjectField CreatePrototypeField(SerializedProperty prototypeProperty, SerializedProperty typeProperty)
        {
            if (prototypeProperty == null)
            {
                throw new ArgumentNullException(nameof(prototypeProperty));
            }

            var field = new ObjectField(prototypeProperty.displayName) {objectType = typeof(UnityEngine.Object)};
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            field.BindProperty(prototypeProperty);

            field.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                var targetType = typeProperty?.GetValue() is SerializedType st ? st.Type : null;
                if (targetType == null) return;

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (IsValidPrototype(obj, targetType)) return;
                }

                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);

            field.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue == null) return;
                var targetType = typeProperty?.GetValue() is SerializedType st ? st.Type : null;
                if (targetType == null || IsValidPrototype(evt.newValue, targetType)) return;

                Debug.LogWarning($"[InstallInfo] Prototype must be a {targetType.Name} or a GameObject with a {targetType.Name} component.");
                field.SetValueWithoutNotify(evt.previousValue);
                prototypeProperty.objectReferenceValue = evt.previousValue;
                prototypeProperty.serializedObject.ApplyModifiedProperties();
            });

            return field;
        }

        private static bool IsValidPrototype(UnityEngine.Object obj, Type type)
        {
            if (obj == null || type == null) return true;
            if (type.IsAssignableFrom(obj.GetType())) return true;
            if (obj is GameObject go && typeof(Component).IsAssignableFrom(type))
                return go.GetComponent(type) != null;
            return false;
        }

        /// <summary>
        /// Choose the spawner best suited to constructing <paramref name="product"/>, closed with that product type:
        /// ScriptableSpawner for ScriptableObjects, ComponentSpawner for Components, NewableSpawner when a public
        /// parameterless constructor exists, otherwise UninitializedSpawner.
        /// </summary>
        private static Type SelectSpawnerType(Type product)
        {
            if (product == null)
            {
                return null;
            }

            Type open;
            if (typeof(ScriptableObject).IsAssignableFrom(product))
            {
                open = typeof(ScriptableSpawner<>);
            }
            else if (typeof(Component).IsAssignableFrom(product))
            {
                open = typeof(ComponentSpawner<>);
            }
            else if (product.GetConstructor(Type.EmptyTypes) != null)
            {
                open = typeof(NewableSpawner<>);
            }
            else
            {
                open = typeof(UninitializedSpawner<>);
            }

            try
            {
                return open.MakeGenericType(product);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Assign a freshly constructed spawner of <paramref name="spawnerType"/> to the
        /// <see cref="SerializedReference{T}"/> backing the Spawner field, or clear it when null.
        /// </summary>
        private static void AssignSpawner(SerializedProperty spawnerProperty, Type spawnerType)
        {
            var pureRef = spawnerProperty.FindPropertyRelative("pureReference");
            var unityRef = spawnerProperty.FindPropertyRelative("unityReference");
            var typeName = spawnerProperty.FindPropertyRelative("expectedTypeName");

            if (spawnerType == null)
            {
                if (pureRef != null) pureRef.managedReferenceValue = null;
                if (unityRef != null) unityRef.objectReferenceValue = null;
                if (typeName != null) typeName.stringValue = string.Empty;
                return;
            }

            object instance = null;
            try
            {
                instance = Activator.CreateInstance(spawnerType);
            }
            catch (Exception e)
            {
                Debug.LogError($"[InstallInfo] Failed to create spawner {spawnerType}: {e.Message}");
            }

            if (pureRef != null) pureRef.managedReferenceValue = instance;
            if (unityRef != null) unityRef.objectReferenceValue = null;
            if (typeName != null) typeName.stringValue = spawnerType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Copy a prototype into the spawner when their kinds match: a GameObject into ComponentSpawner.Prefab,
        /// or a ScriptableObject into ScriptableSpawner.Instance. No-op otherwise.
        /// </summary>
        private static void WirePrototypeToSpawner(SerializedProperty spawnerProperty, Object prototype)
        {
            if (spawnerProperty == null || prototype == null)
            {
                return;
            }

            var pureRef = spawnerProperty.FindPropertyRelative("pureReference");
            var instance = pureRef?.managedReferenceValue;
            if (instance == null || !instance.GetType().IsGenericType)
            {
                return;
            }

            var definition = instance.GetType().GetGenericTypeDefinition();
            string fieldName;
            if (prototype is GameObject && definition == typeof(ComponentSpawner<>))
            {
                fieldName = nameof(ComponentSpawner<Component>.Prefab);
            }
            else if (prototype is ScriptableObject && definition == typeof(ScriptableSpawner<>))
            {
                fieldName = nameof(ScriptableSpawner<ScriptableObject>.Instance);
            }
            else
            {
                return;
            }

            var field = instance.GetType().GetField(fieldName);
            if (field == null || !field.FieldType.IsInstanceOfType(prototype))
            {
                return;
            }

            field.SetValue(instance, prototype);
            pureRef.managedReferenceValue = instance;
            spawnerProperty.serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws the binding types of the <see cref="type"/> that must could be assignable from.
        /// </summary>
        private VisualElement CreateBindingProperty(SerializedProperty property, SerializedProperty type)
        {
            Type actualType = null;
            if (type?.GetValue() is SerializedType serializedType)
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
#if !UNITY_2023_2_OR_NEWER
            listView.itemsAdded   += OnAdd;
            listView.itemsRemoved += OnRemove;
#endif
            listView.itemIndexChanged += OnRecord;
            foldout.text              =  property.displayName;
            foldout.Add(listView);
            return foldout;

            void OnRecord(int start, int end)
            {
                var newOrder = (IList<Type>) listView.itemsSource;
                property.ClearArray();
                property.arraySize = newOrder.Count;
                for (var i = 0; i < newOrder.Count; i++)
                {
                    var elem = property.GetArrayElementAtIndex(i);
                    elem.SetValue(new SerializedType(newOrder[i]));
                }

                property.serializedObject.ApplyModifiedProperties();
            }

#if UNITY_2023_2_OR_NEWER
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

            void RefreshList(BaseListView list)
            {
                list.itemsSource = ExtractArrayProperties(property);
                list.Rebuild();
            }
#else
            void OnAdd(IEnumerable<int> items)
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
                            element.SetValue(new SerializedType(bindingType));
                            property.serializedObject.ApplyModifiedProperties();
                            RefreshList(listView);
                        });
                    }

                    menu.ShowAsContext();
                }
            }

            void OnRemove(IEnumerable<int> items)
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
            }

            void RefreshList(BaseListView list)
            {
                list.itemsSource = ExtractArrayProperties(property);
                list.Rebuild();
            }
#endif
            void BindItem(VisualElement element, int index)
            {
                var label = (Label) element;
                var types = ExtractArrayProperties(property);
                label.text = index < types.Count ? TypeName.GetFriendlyTypeFullName(types[index]) : "null";
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
                    var elementType = (SerializedType) element.GetValue();
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