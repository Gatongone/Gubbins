using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gubbins.Enhance
{
    [Serializable]
    public class SerializedReference<T> where T : class
    {
        [SerializeReference] internal T pureReference;

        [SerializeField] internal Object unityReference;

        [SerializeField] internal string expectedTypeName;

        public Type ExpectedType => Type.GetType(expectedTypeName);

        public T Value
        {
            get
            {
                if (pureReference != null)
                {
                    return pureReference;
                }

                if (unityReference != null)
                {
                    if (unityReference is T unityObj)
                    {
                        return unityObj;
                    }

                    if (unityReference is GameObject go)
                    {
                        return go.GetComponent<T>();
                    }
                }

                if (!string.IsNullOrEmpty(expectedTypeName))
                {
                    var expectedType = Type.GetType(expectedTypeName);
                    return pureReference = expectedType != null && typeof(T).IsAssignableFrom(expectedType) ? (T) Activator.CreateInstance(expectedType)! : null;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    pureReference    = null;
                    unityReference   = null;
                    expectedTypeName = null;
                }
                else if (value is Object uObj)
                {
                    pureReference    = null;
                    unityReference   = uObj;
                    expectedTypeName = uObj.GetType().AssemblyQualifiedName;
                }
                else
                {
                    pureReference    = value;
                    unityReference   = null;
                    expectedTypeName = value.GetType().AssemblyQualifiedName;
                }
            }
        }

        public static implicit operator T(SerializedReference<T> reference) => reference.Value;
    }

    /// <summary>
    /// Constrains a <see cref="SerializedReference{T}"/> field's selectable implementations to a closed
    /// generic type, where the type argument is taken from a sibling field at draw time. Generic
    /// implementation definitions are auto-closed with that same type argument.
    /// </summary>
    /// <example>
    /// <c>[GenericArgumentFrom(typeof(ISpawner&lt;&gt;), nameof(Type))]</c> on a
    /// <c>SerializedReference&lt;ISpawner&gt;</c> field restricts the dropdown to <c>ISpawner&lt;Type&gt;</c>.
    /// </example>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GenericArgumentFromAttribute : Attribute
    {
        /// <summary>The open generic definition to close, e.g. <c>typeof(ISpawner&lt;&gt;)</c>.</summary>
        public Type OpenGeneric { get; }

        /// <summary>The name of the sibling field whose value supplies the generic argument.</summary>
        public string TypeMember { get; }

        public GenericArgumentFromAttribute(Type openGeneric, string typeMember) => (OpenGeneric, TypeMember) = (openGeneric, typeMember);
    }
}