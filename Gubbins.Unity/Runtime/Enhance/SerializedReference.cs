using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gubbins.Enhance
{
    [Serializable]
    public class SerializedReference<T> where T : class
    {
        [SerializeReference] private T pureReference;

        [SerializeField] private Object unityReference;

        [SerializeField] private string expectedTypeName;

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
}