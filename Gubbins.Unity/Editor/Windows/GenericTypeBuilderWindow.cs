using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gubbins.Unsafe;
using UnityEditor;
using UnityEngine;

namespace Gubbins.Editor
{
    internal sealed class GenericTypeBuilderWindow : EditorWindow
    {
        private Type         m_OpenGenericType;
        private Type[]       m_ParamTypes;
        private Type[][]     m_Candidates;
        private Action<Type> m_OnComplete;
        private bool         m_Resolved;

        /// <summary>
        /// Open a window to fill in all generic parameters for <paramref name="openGenericType"/>.
        /// The window is asynchronous: <paramref name="onComplete"/> is invoked with the closed generic type
        /// when the user confirms, or with null if the user cancels or closes the window.
        /// </summary>
        /// <remarks>
        /// This cannot return the type synchronously: it is opened from inside an active GUI event (a property
        /// drawer's OnGUI or a UIToolkit value-change callback), where Unity refuses to start a nested modal loop,
        /// so a blocking ShowModal() would return immediately. The callback defers the result to a later frame.
        /// </remarks>
        public static void Show(Type openGenericType, Action<Type> onComplete)
        {
            var window = CreateInstance<GenericTypeBuilderWindow>();
            window.Initialize(openGenericType, onComplete);
            window.titleContent = new GUIContent($"Build {TypeName.GetFriendlyTypeName(openGenericType)}");
            var paramCount = openGenericType.GetGenericArguments().Length;
            var height = Mathf.Clamp(80 + paramCount * 170, 280, 620);
            window.minSize = new Vector2(440, height);
            window.maxSize = new Vector2(440, height);
            window.ShowUtility();
        }

        private void Initialize(Type openGenericType, Action<Type> onComplete)
        {
            m_OpenGenericType = openGenericType;
            m_OnComplete      = onComplete;
            var args = openGenericType.GetGenericArguments();
            m_ParamTypes = new Type[args.Length];
            m_Candidates = new Type[args.Length][];
            for (var i = 0; i < args.Length; i++)
                m_Candidates[i] = BuildCandidates(args[i]);
        }

        /// <summary>
        /// Invoke the completion callback exactly once with the given result.
        /// </summary>
        private void Complete(Type result)
        {
            if (m_Resolved) return;
            m_Resolved = true;
            var callback = m_OnComplete;
            m_OnComplete = null;
            callback?.Invoke(result);
        }

        /// <summary>
        /// If the window is closed via its chrome (not Apply/Cancel), treat it as a cancel.
        /// </summary>
        private void OnDestroy() => Complete(null);

        private static Type[] BuildCandidates(Type genericParam)
        {
            var constraints = genericParam.GetGenericParameterConstraints();
            var attrs = genericParam.GenericParameterAttributes;
            return AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(asm => asm.GetTypes())
                            .Where(t => t is
                            {
                                IsNestedPrivate    : false,
                                IsNestedFamily     : false,
                                IsNestedFamANDAssem: false,
                                IsNestedFamORAssem : false
                            } and not {IsAbstract: true, IsSealed: true} && SatisfiesConstraints(t, constraints, attrs))
                            .OrderBy(t => t.ToString(), StringComparer.Ordinal)
                            .ToArray();
        }

        private static bool SatisfiesConstraints(Type t, Type[] constraints, GenericParameterAttributes attrs)
        {
            if ((attrs & GenericParameterAttributes.ReferenceTypeConstraint) != 0 && !t.IsClass)
                return false;
            if ((attrs & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0 && !t.IsValueType)
                return false;
            if ((attrs & GenericParameterAttributes.DefaultConstructorConstraint) != 0 &&
                !t.IsValueType && t.GetConstructor(Type.EmptyTypes) == null)
                return false;
            return constraints.All(c => c.IsAssignableFrom(t));
        }

        private Type BuildClosedType()
        {
            try
            {
                return m_OpenGenericType.MakeGenericType(m_ParamTypes);
            }
            catch
            {
                return null;
            }
        }

        private void OnGUI()
        {
            if (m_OpenGenericType == null)
            {
                Close();
                return;
            }

            var args = m_OpenGenericType.GetGenericArguments();

            var headerStyle = new GUIStyle(EditorStyles.boldLabel) {fontSize = 12};
            EditorGUILayout.LabelField(TypeName.GetFriendlyTypeName(m_OpenGenericType), headerStyle);
            EditorGUILayout.Space(6);

            for (var i = 0; i < args.Length; i++)
            {
                DrawParameter(i, args[i]);
                if (i < args.Length - 1)
                    EditorGUILayout.Space(4);
            }

            GUILayout.FlexibleSpace();
            DrawButtons();
        }

        private void DrawParameter(int index, Type genericParam)
        {
            var constraints = genericParam.GetGenericParameterConstraints();
            var constraintText = constraints.Length > 0
                ? $" where {genericParam.Name} : {string.Join(", ", constraints.Select(TypeName.GetFriendlyTypeName))}"
                : string.Empty;

            EditorGUILayout.LabelField($"{genericParam.Name}{constraintText}", EditorStyles.miniBoldLabel);

            var selected = m_ParamTypes[index];
            var selectedLabel = selected != null ? TypeName.GetFriendlyTypeFullName(selected) : "Null";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected", GUILayout.Width(EditorGUIUtility.labelWidth));
            var candidates = m_Candidates[index];
            var options = candidates.Select(t => new TypeOption(t, false)).ToArray();
            if (EditorGUILayout.DropdownButton(new GUIContent(selectedLabel), FocusType.Keyboard))
            {
                var screenRect = EditorGUILayout.GetControlRect();
                var dropdown = new TypeAdvancedDropdown(options, i =>
                {
                    var selectedTypeName = i <= 0 ? null : options[i - 1].SerializedName;
                    var picked = string.IsNullOrEmpty(selectedTypeName)
                        ? null
                        : Type.GetType(selectedTypeName, Reflection.LoadAssemblyResolver, Reflection.DomainTypeResolver);
                    if (picked is {ContainsGenericParameters: true})
                    {
                        // Nested open generic: recurse asynchronously and assign once the user finishes.
                        Show(picked, closed =>
                        {
                            m_ParamTypes[index] = closed;
                            Repaint();
                        });
                        return;
                    }

                    m_ParamTypes[index] = picked;
                    Repaint();
                });
                dropdown.Show(screenRect);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawButtons()
        {
            EditorGUILayout.Space(4);
            var allFilled = m_ParamTypes != null && m_ParamTypes.All(p => p != null);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledGroupScope(!allFilled))
            {
                if (GUILayout.Button("Apply", GUILayout.Height(26)))
                {
                    Complete(BuildClosedType());
                    Close();
                }
            }

            if (GUILayout.Button("Cancel", GUILayout.Height(26)))
            {
                Complete(null);
                Close();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }
    }
}