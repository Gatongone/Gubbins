using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Gubbins.Editor
{
    /// <summary>
    /// Custom dropdown implementation to display type options in a hierarchical menu with search support.
    /// </summary>
    /// <remarks>
    /// The search functionality will cause lag, maybe we should use UIToolkit for better performance if this becomes a problem.
    /// </remarks>
    public sealed class TypeAdvancedDropdown : AdvancedDropdown
    {
        private const    string                    NULL_OPTION_LABEL = "Null";
        private readonly IReadOnlyList<TypeOption> m_Options;
        private readonly Action<int>               m_OnSelected;

        public TypeAdvancedDropdown(IReadOnlyList<TypeOption> options, Action<int> onSelected)
            : base(new AdvancedDropdownState())
        {
            m_Options    = options;
            m_OnSelected = onSelected;
            minimumSize  = new Vector2(420f, 460f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Select Type");
            root.AddChild(new TypeAdvancedDropdownItem(NULL_OPTION_LABEL, 0));

            var groups = new Dictionary<string, AdvancedDropdownItem>();
            for (var index = 0; index < m_Options.Count; index++)
            {
                var option = m_Options[index];
                AddPath(root, groups, option.MenuPath, index + 1);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeAdvancedDropdownItem typedItem)
                m_OnSelected?.Invoke(typedItem.Index);
        }

        private static void AddPath(AdvancedDropdownItem root, IDictionary<string, AdvancedDropdownItem> groups, string path, int index)
        {
            var segments = path.Split('/');
            var parent = root;
            var key = string.Empty;

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];
                if (string.IsNullOrEmpty(segment))
                    continue;

                key = string.IsNullOrEmpty(key) ? segment : $"{key}/{segment}";
                var isLeaf = i == segments.Length - 1;

                if (isLeaf)
                {
                    parent.AddChild(new TypeAdvancedDropdownItem(segment, index));
                    continue;
                }

                if (!groups.TryGetValue(key, out var groupItem))
                {
                    groupItem   = new AdvancedDropdownItem(segment);
                    groups[key] = groupItem;
                    parent.AddChild(groupItem);
                }

                parent = groupItem;
            }
        }
    }

    /// <summary>
    /// Dropdown item that holds the index of the corresponding type option for selection callbacks.
    /// </summary>
    public sealed class TypeAdvancedDropdownItem : AdvancedDropdownItem
    {
        public readonly int Index;

        public TypeAdvancedDropdownItem(string name, int index) : base(name)
        {
            Index = index;
        }
    }

    /// <summary>
    /// Structured representation of a type option for display and selection.
    /// </summary>
    public readonly struct TypeOption
    {
        public readonly string NamespaceGroup;
        public readonly string TypeName;
        public readonly string AssemblyName;
        public readonly string SerializedName;
        public readonly string DisplayName;
        public readonly string MenuPath;

        public TypeOption(Type type, bool appendAssembly)
        {
            NamespaceGroup = Editor.TypeName.GetNamespaceGroup(type);
            TypeName       = Editor.TypeName.GetFriendlyTypeName(type);
            AssemblyName   = type.Assembly.GetName().Name;
            SerializedName = type.AssemblyQualifiedName;
            var name = appendAssembly && !string.IsNullOrEmpty(AssemblyName)
                ? $"{TypeName} [{AssemblyName}]"
                : TypeName;
            DisplayName = $"{name} [{NamespaceGroup}]";
            MenuPath    = BuildMenuPath(NamespaceGroup, name);
        }

        public TypeOption(string serializedName)
        {
            NamespaceGroup = "Missing";
            TypeName       = serializedName;
            AssemblyName   = string.Empty;
            SerializedName = serializedName;
            DisplayName    = $"Missing [{serializedName}]";
            MenuPath       = BuildMenuPath("Missing", serializedName);
        }

        /// <summary>
        /// Build a menu path with namespace segments split to submenus.
        /// </summary>
        private static string BuildMenuPath(string namespaceGroup, string typeName)
        {
            var namespacePath = namespaceGroup.Replace('.', '/');
            return string.IsNullOrEmpty(namespacePath) ? typeName : $"{namespacePath}/{typeName} [{namespaceGroup}]";
        }
    }
}