using System;
using UnityEngine;

namespace Gubbins.Enhance
{
    /// <summary>
    /// Hide label for serialized field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class HideLabelAttribute : PropertyAttribute { }
}