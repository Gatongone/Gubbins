using System;
using UnityEngine;

namespace Gubbins.Enhance
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute { }
}