using System;
using System.Collections.Generic;
using System.Linq;
using Gubbins.Entities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gubbins.Editor
{
    /// <summary>
    /// Custom property drawer for the ComponentSet class, providing both IMGUI and UI Toolkit implementations.
    /// </summary>
    [CustomPropertyDrawer(typeof(ComponentSet))]
    internal class ComponentSetDrawer : PropertyDrawer
    {
        private const int BORDER_RADIUS = 4;

        /// <summary>
        /// All Component types.
        /// </summary>
        private static readonly Type[] s_ComponentTypes = GetComponentTypes();

        /// <summary>
        /// Each SerializedProperty corresponds to a ReorderableList, cached with the path as the key.
        /// </summary>
        private readonly Dictionary<string, ReorderableList> m_ListCache = new();

        /// <summary>
        /// IMGUI foldout header style.
        /// </summary>
        private static GUIStyle s_FoldoutHeaderStyle => new(EditorStyles.foldoutHeader)
        {
            margin    = new RectOffset(0, 0, 0, 0),
            padding   = new RectOffset(14, 0, 0, 0),
            fontStyle = FontStyle.Bold
        };

        /// <summary>
        /// Retrieves all valid component types that can be added to the ComponentSet. A valid component type is defined as:
        /// - Implements the IComponent interface.
        /// - Is not abstract.
        /// - Does not contain generic parameters.
        /// - Is an unmanaged type (value type without reference type fields).
        /// </summary>
        private static Type[] GetComponentTypes() => AppDomain.CurrentDomain.GetAssemblies()
                                                              .SelectMany(asm => asm.GetTypes())
                                                              .Where(t => typeof(IComponent).IsAssignableFrom(t) &&
                                                                  !t.IsAbstract &&
                                                                  !t.ContainsGenericParameters &&
                                                                  CheckIsTypeUnmanaged(t))
                                                              .ToArray();

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

            // // A type is considered unmanaged if it is a value type and all of its fields are either of
            // the same type (to allow for recursive structs) or are unmanaged types.
            return !type.GetFields().Any(f => f.FieldType != type && !CheckIsTypeUnmanaged(f.FieldType));
        }

        #region UIToolkit

        /// <summary>
        /// UI Toolkit version.
        /// </summary>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (s_ComponentTypes.Length == 0)
                return new Label("No valid component types found.");

            var componentsProp = property.FindPropertyRelative("Components");
            if (componentsProp == null)
                return new Label("Components property not found.");

            var root = new VisualElement();
            var foldout = CreateComponentFoldout(property, componentsProp);
            var listView = CreateComponentListView(property, componentsProp);

            var refreshUI = new Action(() =>
            {
                UpdateFoldoutTitle(foldout, property, componentsProp);
                RefreshListView(listView, componentsProp);
            });

            RegisterReorderCallback(listView, property, componentsProp, refreshUI);

            foldout.Add(listView);
            foldout.Add(CreateListFooter(property, componentsProp, foldout, listView, refreshUI));
            root.Add(foldout);

            return root;
        }

        /// <summary>
        /// Creates the foldout header that displays the component count and tracks expanded state.
        /// </summary>
        private static Foldout CreateComponentFoldout(SerializedProperty property, SerializedProperty componentsProp)
        {
            var borderCol = new Color(0.1f, 0.1f, 0.1f);
            var foldout = new Foldout
            {
                value = property.isExpanded,
                style =
                {
                    paddingLeft             = 15,
                    marginLeft              = 0,
                    marginRight             = 0,
                    marginTop               = 2,
                    marginBottom            = 2,
                    borderTopWidth          = 1,
                    borderRightWidth        = 1,
                    borderBottomWidth       = 1,
                    borderLeftWidth         = 1,
                    borderTopColor          = borderCol,
                    borderRightColor        = borderCol,
                    borderBottomColor       = borderCol,
                    borderLeftColor         = borderCol,
                    borderBottomLeftRadius  = BORDER_RADIUS,
                    borderBottomRightRadius = BORDER_RADIUS,
                    borderTopLeftRadius     = BORDER_RADIUS,
                    borderTopRightRadius    = BORDER_RADIUS,
                    backgroundColor         = EditorColors.Content,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            foldout.contentContainer.style.paddingLeft  = 0;
            foldout.contentContainer.style.marginLeft   = 0;
            foldout.contentContainer.style.paddingRight = 5;
            foldout.contentContainer.style.marginRight  = 5;
            UpdateFoldoutTitle(foldout, property, componentsProp);
            foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);
            return foldout;
        }

        /// <summary>
        /// Creates and configures the UI Toolkit ListView used to render component entries.
        /// </summary>
        private static ListView CreateComponentListView(SerializedProperty property, SerializedProperty componentsProp) => new()
        {
            reorderable                   = true,
            reorderMode                   = ListViewReorderMode.Animated,
            showBorder                    = true,
            selectionType                 = SelectionType.Single,
            showAlternatingRowBackgrounds = AlternatingRowBackground.None,
            virtualizationMethod          = CollectionVirtualizationMethod.DynamicHeight,
            itemsSource                   = GetComponentItems(componentsProp),
            makeItem                      = MakeComponentListItem,
            bindItem                      = (item, index) => BindComponentListItem(item, index, componentsProp, property.serializedObject),
            style =
            {
                marginLeft  = 2,
                marginRight = 2,
                height      = StyleKeyword.Auto,
                flexGrow    = 0
            }
        };

        /// <summary>
        /// Builds the visual tree for one list item.
        /// </summary>
        private static VisualElement MakeComponentListItem()
        {
            var itemRoot = new VisualElement();

            var typeLabel = new Label
            {
                name = "type-label",
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            itemRoot.Add(typeLabel);

            var propertiesContainer = new VisualElement {name = "properties-container"};
            itemRoot.Add(propertiesContainer);
            return itemRoot;
        }

        /// <summary>
        /// Binds one list item to the corresponding managed-reference component data.
        /// </summary>
        private static void BindComponentListItem(VisualElement item, int index, SerializedProperty componentsProp, SerializedObject serializedObject)
        {
            if (index < 0 || index >= componentsProp.arraySize)
                return;

            var element = componentsProp.GetArrayElementAtIndex(index);
            var typeLabel = item.Q<Label>("type-label");
            var propertiesContainer = item.Q<VisualElement>("properties-container");

            typeLabel.text = element.managedReferenceValue?.GetType().Name ?? "Null";
            propertiesContainer.Clear();

            var prop = element.Copy();
            var endProp = prop.GetEndProperty();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(prop, endProp))
            {
                var field = new PropertyField(prop.Copy());
                field.Bind(serializedObject);
                propertiesContainer.Add(field);
                enterChildren = false;
            }

            var style = item.style;
            var borderCol = EditorColors.Text;
            borderCol = new Color(borderCol.r, borderCol.g, borderCol.b, 0.15f);

            style.paddingTop              = 4;
            style.paddingBottom           = 4;
            style.paddingLeft             = 4;
            style.paddingRight            = 4;
            style.borderBottomWidth       = 1;
            style.borderTopWidth          = 1;
            style.borderLeftWidth         = 1;
            style.borderRightWidth        = 1;
            style.borderBottomColor       = borderCol;
            style.borderTopColor          = borderCol;
            style.borderLeftColor         = borderCol;
            style.borderRightColor        = borderCol;
            style.borderBottomLeftRadius  = BORDER_RADIUS;
            style.borderBottomRightRadius = BORDER_RADIUS;
            style.borderTopLeftRadius     = BORDER_RADIUS;
            style.borderTopRightRadius    = BORDER_RADIUS;
            style.backgroundColor         = index % 2 == 0 ? EditorColors.Background2 : EditorColors.Background;
        }

        /// <summary>
        /// Registers reorder handling so drag operations are written back to the serialized array.
        /// </summary>
        private static void RegisterReorderCallback(ListView listView, SerializedProperty property, SerializedProperty componentsProp, Action refreshUI)
        {
            listView.itemIndexChanged += (oldIndex, newIndex) =>
            {
                componentsProp.MoveArrayElement(oldIndex, newIndex);
                property.serializedObject.ApplyModifiedProperties();
                refreshUI();
                listView.selectedIndex = newIndex;
            };
        }

        /// <summary>
        /// Creates the footer with add/remove buttons for component list operations.
        /// </summary>
        private static VisualElement CreateListFooter(
            SerializedProperty property,
            SerializedProperty componentsProp,
            Foldout foldout,
            ListView listView,
            Action refreshUI)
        {
            var footer = new VisualElement();
            footer.AddToClassList("unity-base-vertical-collection-view__footer");
            footer.style.width          = Length.Percent(100);
            footer.style.flexDirection  = FlexDirection.Row;
            footer.style.justifyContent = Justify.FlexEnd;
            footer.style.marginTop      = 3;
            footer.style.paddingBottom  = 2;

            var footerButtons = new VisualElement
            {
                style =
                {
                    flexDirection  = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd
                }
            };

            var addButton = new Button
            {
#if UNITY_2023_1_OR_NEWER
                iconImage = Background.FromTexture2D(EditorGUIUtility.IconContent("Toolbar Plus").image as Texture2D),
#else
                text = "+",
#endif
                tooltip = "Add Component",
                style =
                {
                    backgroundColor = Color.clear
                }
            };
            addButton.clicked += () => ShowAddComponentMenuUIElements(addButton, componentsProp, property, foldout, listView);

            var removeButton = new Button
            {
#if UNITY_2023_1_OR_NEWER
                iconImage = Background.FromTexture2D(EditorGUIUtility.IconContent("Toolbar Minus").image as Texture2D),
#else
                text = "-",
#endif
                tooltip = "Remove Selected Component",
                style =
                {
                    backgroundColor = Color.clear
                }
            };
            removeButton.clicked += () => RemoveSelectedComponent(listView, componentsProp, property, refreshUI);

            footerButtons.Add(addButton);
            footerButtons.Add(removeButton);
            footer.Add(footerButtons);

            return footer;
        }

        /// <summary>
        /// Removes the currently selected component item and refreshes the list UI.
        /// </summary>
        private static void RemoveSelectedComponent(ListView listView, SerializedProperty componentsProp, SerializedProperty property, Action refreshUI)
        {
            var selectedIndex = listView.selectedIndex;
            if (selectedIndex < 0 || selectedIndex >= componentsProp.arraySize)
                return;

            componentsProp.DeleteArrayElementAtIndex(selectedIndex);
            property.serializedObject.ApplyModifiedProperties();

            refreshUI();
            listView.selectedIndex = Mathf.Clamp(selectedIndex - 1, 0, componentsProp.arraySize - 1);
        }

        /// <summary>
        /// Updates the foldout title text to include the current component count.
        /// </summary>
        private static void UpdateFoldoutTitle(Foldout foldout, SerializedProperty property, SerializedProperty componentsProp)
        {
            foldout.text = $"{property.displayName} ({componentsProp.arraySize})";
        }

        #endregion

        #region IMGUI

        /// <summary>
        /// IMGUI version.
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (s_ComponentTypes.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No valid component types found.");
                return;
            }

            var componentsProp = property.FindPropertyRelative("Components");
            var isExpanded = property.isExpanded;

            // Draw foldout header with count label.
            var foldoutRect = new Rect(position.x + 15, position.y, position.width - 20, EditorGUIUtility.singleLineHeight);
            var countLabel = $"({componentsProp.arraySize})";
            var fullLabel = new GUIContent($"{label.text} {countLabel}");
            var headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + 2);
            Handles.DrawSolidRectangleWithOutline(headerRect, Color.clear, Color.black);
            var newExpanded = EditorGUI.BeginFoldoutHeaderGroup(foldoutRect, isExpanded, fullLabel, s_FoldoutHeaderStyle);
            EditorGUI.EndFoldoutHeaderGroup();
            property.isExpanded = newExpanded;

            // Draw box around the list when expanded.
            if (newExpanded)
            {
                var list = GetOrCreateList(property);
                var listRect = new Rect(position.x, position.y + headerRect.height, position.width, position.height - headerRect.height);
                list.DoList(listRect);
            }
        }

        /// <inheritdoc/>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight + 4; // Property itself height.
            if (property.isExpanded)
            {
                height += GetOrCreateList(property).GetHeight();
            }

            return height;
        }

        /// <summary>
        /// Refreshes the ListView to reflect changes in the underlying array.
        /// </summary>
        private static void RefreshListView(ListView listView, SerializedProperty arrayProperty)
        {
            listView.itemsSource = GetComponentItems(arrayProperty);
            listView.Rebuild();
        }

        /// <summary>
        /// Gets a list of component items for the ListView.
        /// </summary>
        private static List<int> GetComponentItems(SerializedProperty arrayProperty)
        {
            var items = new List<int>();
            for (var i = 0; i < arrayProperty.arraySize; i++)
            {
                items.Add(i);
            }

            return items;
        }

        /// <summary>
        /// Shows a dropdown menu to add a new component to the list using UIElements.
        /// </summary>
        private static void ShowAddComponentMenuUIElements(
            VisualElement triggerElement,
            SerializedProperty componentsProp,
            SerializedProperty property,
            Foldout foldout,
            ListView listView)
        {
            // Get existing component types in the list to filter out from the menu
            var existingTypes = new HashSet<Type>();
            for (var i = 0; i < componentsProp.arraySize; i++)
            {
                var element = componentsProp.GetArrayElementAtIndex(i);
                var obj = element.managedReferenceValue;
                if (obj != null)
                    existingTypes.Add(obj.GetType());
            }

            // Filter available component types to only those not already in the list
            var availableTypes = s_ComponentTypes.Where(t => !existingTypes.Contains(t)).ToArray();

            if (availableTypes.Length == 0)
                return;

            var menu = new GenericMenu();
            foreach (var type in availableTypes)
            {
                var capturedType = type;
                menu.AddItem(new GUIContent(capturedType.FullName), false, () =>
                {
                    componentsProp.arraySize++;
                    var newElement = componentsProp.GetArrayElementAtIndex(componentsProp.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(capturedType);
                    property.serializedObject.ApplyModifiedProperties();

                    // Update UI
                    foldout.text = $"{property.displayName} ({componentsProp.arraySize})";
                    RefreshListView(listView, componentsProp);
                });
            }

            menu.DropDown(triggerElement.worldBound);
        }

        /// <summary>
        /// Get or create a ReorderableList for the given SerializedProperty.
        /// The list is cached based on the property's path to ensure that each property gets its own list instance.
        /// </summary>
        private ReorderableList GetOrCreateList(SerializedProperty property)
        {
            var componentsProp = property.FindPropertyRelative("Components");
            var key = property.propertyPath;

            if (!m_ListCache.TryGetValue(key, out var list))
            {
                list = new ReorderableList(property.serializedObject, componentsProp, true, false, true, true);

                list.elementHeightCallback = index => GetElementHeight(list.serializedProperty, index);
                list.drawElementCallback   = (rect, index, _, _) => DrawElement(rect, index, list.serializedProperty);
                list.onAddDropdownCallback = (buttonRect, _) => ShowAddComponentMenu(buttonRect, property);
                list.footerHeight          = EditorGUIUtility.singleLineHeight;

                m_ListCache[key] = list;
            }
            else
            {
                // Make sure to update the serialized property reference in case the same drawer instance is reused for a different property.
                list.serializedProperty = componentsProp;
            }

            return list;
        }

        /// <summary>
        /// Calculates the height of a single element in the list by summing the heights of all its visible properties,
        /// including the type name label at the top. This ensures that each element is tall enough
        /// to display all its content without clipping, regardless of how many properties it has or their individual heights.
        /// </summary>
        private static float GetElementHeight(SerializedProperty arrayProperty, int index)
        {
            var element = arrayProperty.GetArrayElementAtIndex(index);
            var height = EditorGUIUtility.singleLineHeight; // Type name label height

            var prop = element.Copy();
            var endProp = prop.GetEndProperty();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(prop, endProp))
            {
                height        += EditorGUI.GetPropertyHeight(prop, true);
                enterChildren =  false;
            }

            return height;
        }

        /// <summary>
        /// Draws a single element in the list, which includes a type name label at the top and all
        /// visible properties of the component below it.
        /// </summary>
        private static void DrawElement(Rect rect, int index, SerializedProperty arrayProperty)
        {
            var element = arrayProperty.GetArrayElementAtIndex(index);
            var typeName = element.managedReferenceValue.GetType().Name;

            // Type name label at the top of the element
            var labelRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, typeName, EditorStyles.boldLabel);

            // Draw all visible properties of the component below the type name
            var y = rect.y + EditorGUIUtility.singleLineHeight;
            var prop = element.Copy();
            var endProp = prop.GetEndProperty();
            var enterChildren = true;
            while (prop.NextVisible(enterChildren) &&
                !SerializedProperty.EqualContents(prop, endProp))
            {
                var propHeight = EditorGUI.GetPropertyHeight(prop, true);
                var propRect = new Rect(rect.x, y, rect.width, propHeight);
                EditorGUI.PropertyField(propRect, prop, true);
                y             += propHeight;
                enterChildren =  false;
            }
        }

        /// <summary>
        /// Shows a dropdown menu to add a new component to the list.
        /// The menu is populated with all valid component types that are not already in the list.
        /// </summary>
        private static void ShowAddComponentMenu(Rect buttonRect, SerializedProperty property)
        {
            var componentsProp = property.FindPropertyRelative("Components");

            // Get existing component types in the list to filter out from the menu
            var existingTypes = new HashSet<Type>();
            for (var i = 0; i < componentsProp.arraySize; i++)
            {
                var element = componentsProp.GetArrayElementAtIndex(i);
                var obj = element.managedReferenceValue;
                if (obj != null)
                    existingTypes.Add(obj.GetType());
            }

            // Filter available component types to only those not already in the list
            var availableTypes = s_ComponentTypes.Where(t => !existingTypes.Contains(t)).ToArray();

            if (availableTypes.Length == 0)
                return;

            var menu = new GenericMenu();
            foreach (var type in availableTypes)
            {
                var capturedType = type;
                var capturedPropertyPath = property.propertyPath;
                var targetObject = property.serializedObject;
                var so = targetObject;
                var prop = so.FindProperty(capturedPropertyPath).FindPropertyRelative("Components");

                menu.AddItem(new GUIContent(capturedType.FullName), false, () =>
                {
                    prop.arraySize++;
                    var newElement = prop.GetArrayElementAtIndex(prop.arraySize - 1);
                    newElement.managedReferenceValue = Activator.CreateInstance(capturedType);
                    so.ApplyModifiedProperties();
                });
            }

            menu.DropDown(buttonRect);
        }

        #endregion
    }
}