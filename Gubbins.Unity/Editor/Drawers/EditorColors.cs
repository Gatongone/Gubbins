using System;
using UnityEditor;
using UnityEngine;

namespace Gubbins.Editor
{
    public class EditorColors
    {
        private enum Theme
        {
            Dark,
            Light,
        }

        private static Theme theme => EditorGUIUtility.isProSkin ? Theme.Dark : Theme.Light;

        internal static Color Text => theme switch
        {
            Theme.Dark  => Color.white,
            Theme.Light => Color.black,
            _           => throw new ArgumentOutOfRangeException()
        };

        internal static Color Content => theme switch
        {
            Theme.Dark  => new Color(42 / 255f, 42 / 255f, 42 / 255f, 1),
            Theme.Light => new Color(240 / 255f, 240 / 255f, 240 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };

        internal static Color Background => theme switch
        {
            Theme.Dark  => new Color(20 / 255f, 20 / 255f, 20 / 255f, 1),
            Theme.Light => new Color(220 / 255f, 220 / 255f, 220 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };

        internal static Color Background2 => theme switch
        {
            Theme.Dark  => new Color(32 / 255f, 32 / 255f, 32 / 255f, 1),
            Theme.Light => new Color(230 / 255f, 230 / 255f, 230 / 255f, 1),
            _           => throw new ArgumentOutOfRangeException()
        };
    }
}