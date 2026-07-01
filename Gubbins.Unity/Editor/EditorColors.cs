using System;
using UnityEditor;
using UnityEngine;

namespace Gubbins.Editor
{
    /// <summary>
    /// Provides a set of colors that adapt to the Unity Editor's theme (dark or light).
    /// </summary>
    public class EditorColors
    {
        /// <summary>
        /// Defines the possible themes for the Unity Editor: Dark and Light.
        /// </summary>
        private enum Theme
        {
            /// <summary>
            /// Represents the dark theme of the Unity Editor.
            /// </summary>
            Dark,

            /// <summary>
            /// Represents the light theme of the Unity Editor.
            /// </summary>
            Light
        }

        /// <summary>
        /// Gets the current theme of the Unity Editor (dark or light) based on the EditorGUIUtility.isProSkin property.
        /// </summary>
        private static Theme theme => EditorGUIUtility.isProSkin ? Theme.Dark : Theme.Light;

        /// <summary>
        /// Gets the text color for the Unity Editor, which changes based on the current theme (dark or light).
        /// </summary>
        internal static Color Text => theme switch
        {
            Theme.Dark  => Color.white,
            Theme.Light => Color.black,
            _           => throw new ArgumentOutOfRangeException()
        };

        /// <summary>
        /// Gets the content color for the Unity Editor, which changes based on the current theme (dark or light).
        /// </summary>
        internal static Color Content => theme switch
        {
            Theme.Dark  => new Color(42 / 255f, 42 / 255f, 42 / 255f, 1),
            Theme.Light => new Color(240 / 255f, 240 / 255f, 240 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };

        /// <summary>
        /// Gets the main background color for the Unity Editor, which changes based on the current theme (dark or light).
        /// </summary>
        internal static Color Background => theme switch
        {
            Theme.Dark  => new Color(20 / 255f, 20 / 255f, 20 / 255f, 1),
            Theme.Light => new Color(220 / 255f, 220 / 255f, 220 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };

        /// <summary>
        /// Gets a secondary background color that is slightly different from the main background color, useful for creating visual separation in the UI.
        /// </summary>
        internal static Color Background2 => theme switch
        {
            Theme.Dark  => new Color(32 / 255f, 32 / 255f, 32 / 255f, 1),
            Theme.Light => new Color(230 / 255f, 230 / 255f, 230 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };
    }
}