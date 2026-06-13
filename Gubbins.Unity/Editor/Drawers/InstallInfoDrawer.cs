using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Context;
using Gubbins.Enhance;
using Gubbins.Spawner;
using Gubbins.Unsafe;
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

            public Properties(SerializedProperty property)
            {
                Scope      = property.FindPropertyRelative(nameof(SerializedInstallInfo.Scope));
                Key        = property.FindPropertyRelative(nameof(SerializedInstallInfo.Key));
                Type       = property.FindPropertyRelative(nameof(SerializedInstallInfo.Type));
                Bindings   = property.FindPropertyRelative(nameof(SerializedInstallInfo.Bindings));
                Controller = property.FindPropertyRelative(nameof(SerializedInstallInfo.Controller));
                Spawner    = property.FindPropertyRelative(nameof(SerializedInstallInfo.Spawner));
                Prewarm    = property.FindPropertyRelative(nameof(SerializedInstallInfo.Prewarm));
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
            var spawner = CreateSpawnerProperty(properties.Spawner);
            var currentScope = (Scope) properties.Scope.enumValueIndex;
            var prewarm = CreatePrewarmField();

            // Build root. key/scope/type are always present; everything below Type is laid out by
            // RelayoutDynamic based on the current product Type and Scope.
            root.Add(key);
            root.Add(scope);
            root.Add(type);
            RelayoutDynamic();

            // Register callbacks.
            scope.RegisterValueChangeCallback(OnScopeChanged);
            type.RegisterValueChangeCallback(OnTypeChanged);
            return root;

            // Prewarm means different things per scope: for Multiton it's a preallocation count (numeric field);
            // for every other scope it's a binary load strategy — 1 = eager, 0 = lazy.
            VisualElement CreatePrewarmField()
            {
                if (currentScope is Scope.Multiton)
                {
                    return CreatePropertyField(properties.Prewarm, "Prewarm");
                }

                var choices  = new List<string> {"Lazy", "Eager"};
                var dropdown = new DropdownField("Prewarm", choices, Math.Clamp(properties.Prewarm.intValue, 0, 1));
                dropdown.AddToClassList(ObjectField.alignedFieldUssClassName);
                dropdown.RegisterValueChangedCallback(evt =>
                {
                    var index = choices.IndexOf(evt.newValue);
                    if (index < 0) return;
                    properties.Prewarm.intValue = index;
                    properties.Prewarm.serializedObject.ApplyModifiedProperties();
                });
                return dropdown;
            }

            // Detach the Type/Scope-dependent fields, then re-attach them in canonical order. When no product
            // Type is set, everything below Type stays detached (its data is cleared by the callers).
            void RelayoutDynamic()
            {
                if (root.Contains(controller)) root.Remove(controller);
                if (root.Contains(prewarm))    root.Remove(prewarm);
                if (root.Contains(spawner))    root.Remove(spawner);
                if (bindings != null && root.Contains(bindings)) root.Remove(bindings);

                if (properties.Type.GetValue() is not SerializedType currentType || currentType.Type == null)
                {
                    return;
                }

                if (currentScope is not (Scope.Singleton or Scope.Multiton))
                {
                    root.Add(controller);
                }

                root.Add(prewarm);
                root.Add(spawner);
                if (bindings != null)
                {
                    root.Add(bindings);
                }
            }

            void OnScopeChanged(SerializedPropertyChangeEvent evt)
            {
                var newScope = (Scope) evt.changedProperty.enumValueIndex;
                if (currentScope == newScope)
                {
                    return;
                }

                // Switching into or out of Multiton swaps the prewarm control (numeric ↔ binary).
                var multitonChanged = (currentScope is Scope.Multiton) != (newScope is Scope.Multiton);
                currentScope = newScope;

                if (newScope is Scope.Singleton or Scope.Multiton)
                {
                    properties.Controller.Clear();
                }

                if (newScope is not Scope.Multiton)
                {
                    properties.Prewarm.intValue = Math.Clamp(properties.Prewarm.intValue, 0, 1);
                }

                if (multitonChanged)
                {
                    if (root.Contains(prewarm)) root.Remove(prewarm);
                    prewarm = CreatePrewarmField();
                }

                RelayoutDynamic();

                // A freshly created prewarm field needs explicit binding (the root is already bound). Harmless
                // no-op for the binary DropdownField, which syncs Prewarm.intValue manually.
                if (multitonChanged)
                {
                    prewarm.Bind(property.serializedObject);
                }

                property.serializedObject.ApplyModifiedProperties();
            }

            void OnTypeChanged(SerializedPropertyChangeEvent evt)
            {
                var changedType = (SerializedType) evt.changedProperty.GetValue();
                var hasType     = changedType.Type != null;

                properties.Key.stringValue = hasType ? changedType.Type.ToString() : string.Empty;

                // 1) Update the underlying data.
                properties.Bindings.Clear();
#if UNITY_2023_2_OR_NEWER
                AssignSpawner(properties.Spawner, SelectSpawnerType(changedType.Type));
#endif
                if (!hasType)
                {
                    // Clearing the Type tears down everything that depends on it.
                    properties.Controller.Clear();
                    properties.Prewarm.intValue = 0;
                }

                // 2) Commit so the fields recreated below see the up-to-date serialized state — CreateSpawnerProperty's
                //    initial RebuildChildren calls serializedObject.Update(), which would otherwise revert the changes.
                property.serializedObject.ApplyModifiedProperties();

                // 3) Recreate the type-dependent fields against the new data. The spawner must be rebuilt rather than
                //    just re-attached: its child fields and type selector are wired up inside CreateSpawnerProperty,
                //    and a stale element re-attached after the data changed never re-runs RebuildChildren.
                if (bindings != null && root.Contains(bindings)) root.Remove(bindings);
                if (root.Contains(spawner)) root.Remove(spawner);

                bindings = hasType ? CreateBindingProperty(properties.Bindings, properties.Type) : null;
                if (hasType)
                {
                    spawner = CreateSpawnerProperty(properties.Spawner);
                }

                RelayoutDynamic();

                // The root is already bound, so these freshly created fields won't auto-bind — bind them
                // explicitly or their PropertyFields (e.g. the spawner's type selector) render empty.
                if (hasType)
                {
                    spawner.Bind(property.serializedObject);
                    bindings?.Bind(property.serializedObject);
                }
            }
        }

        private PropertyField CreatePropertyField(SerializedProperty property, string name = "")
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var field = new PropertyField(property, string.IsNullOrEmpty(name) ? property.displayName : name);
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            return field;
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
                if (pureRef != null) pureRef.managedReferenceValue  = null;
                if (unityRef != null) unityRef.objectReferenceValue = null;
                if (typeName != null) typeName.stringValue          = string.Empty;
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

            if (pureRef != null) pureRef.managedReferenceValue  = instance;
            if (unityRef != null) unityRef.objectReferenceValue = null;
            if (typeName != null) typeName.stringValue          = spawnerType.AssemblyQualifiedName;
        }

        private VisualElement CreateSpawnerProperty(SerializedProperty property)
        {
            const string pureProperty = nameof(SerializedReference<object>.pureReference);
            const string unityProperty = nameof(SerializedReference<object>.unityReference);
            const string typeName = nameof(SerializedReference<object>.expectedTypeName);
            var pureProp = property.FindPropertyRelative(pureProperty);
            var unityProp = property.FindPropertyRelative(unityProperty);
            var foldout = CreateFoldout(property);
            var propField = CreatePropertyField(property, "Type");
            propField.AddToClassList("no-expand-children");
            // SerializedReferencePropertyDrawer labels its type DropdownField with the field's displayName
            // ("Spawner") and ignores the PropertyField label, so override it to "Type" once that dropdown exists.
            propField.RegisterCallback<GeometryChangedEvent>(RelabelTypeDropdown);
            foldout.Add(propField);
            var childContainer = new VisualElement();
            foldout.Add(childContainer);

            string lastBuiltType = null;
            RebuildChildren();
            propField.RegisterValueChangeCallback(_ => RebuildChildren());

            return foldout;

            void RelabelTypeDropdown(GeometryChangedEvent _)
            {
                if (propField.Q<DropdownField>() is not { } dropdown) return;
                dropdown.label = "Type";
                propField.UnregisterCallback<GeometryChangedEvent>(RelabelTypeDropdown);
            }

            void RebuildChildren()
            {
                property.serializedObject.Update();
                var newType = property.FindPropertyRelative(typeName).stringValue;
                if (newType == lastBuiltType) return;
                childContainer.Clear();
                lastBuiltType = newType;

                // Cleared to "Null": nothing left to draw, keep the emptied container.
                if (string.IsNullOrEmpty(newType)) return;

                var spawnerType = Type.GetType(newType, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
                var spawnerProperty = typeof(Object).IsAssignableFrom(spawnerType) ? unityProp : pureProp;

                if (typeof(Object).IsAssignableFrom(spawnerType))
                {
                    var f = CreatePropertyField(unityProp, "Object");
                    childContainer.Add(f);
                    f.Bind(property.serializedObject);
                }

                foreach (var child in spawnerProperty.GetChildren())
                {
                    var f = CreatePropertyField(child);
                    childContainer.Add(f);
                    f.Bind(property.serializedObject);
                }
            }
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
            var foldout = CreateFoldout(property);
            var listView = new ListView
            {
                reorderable = true,

                showAddRemoveFooter     = true,
                showBoundCollectionSize = false,
                itemsSource             = ExtractArrayProperties(property),
                reorderMode             = ListViewReorderMode.Animated,
                selectionType           = SelectionType.Single,
#if UNITY_2023_2_OR_NEWER
                onAdd       = OnAdd,
                onRemove    = OnRemove,
                allowAdd    = true,
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
            listView.itemsAdded += OnAdd;
            listView.itemsRemoved += OnRemove;
#endif
            listView.itemIndexChanged += OnRecord;
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

        private static Foldout CreateFoldout(SerializedProperty property)
        {
            var borderCol = new Color(0.1f, 0.1f, 0.1f);
            const int borderRadius = 4;
            var foldout = new Foldout
            {
                value = property.isExpanded,
                text  = property.displayName,
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
            return foldout;
        }
    }
}