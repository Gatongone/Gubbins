using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gubbins.Enhance
{
    /// <summary>
    /// A serializable reference to an object of type T, which can be either a pure C# object or a UnityEngine.Object
    /// which's class allows for easy serialization and deserialization of references to objects in Unity, while also providing support for polymorphic types and generic constraints.
    /// The field can be appended with the <see cref="GenericArgumentFromAttribute"/> to constrain the selectable implementations to a closed generic type, where the type argument is taken from a sibling field at draw time.
    /// One can also use the <see cref="AllowDefaultConstructorMissingAttribute"/> to indicate that the field is allowed to be null or have a type that does not have a default constructor. In this case, the field will be
    /// only serialized type information, and the value will be null. This is useful for cases where the field may be assigned a value at runtime or through other means, and the default constructor is not required.
    /// </summary>
    /// <typeparam name="T">The type of the object that this serialized reference points to. Must be a class.</typeparam>
    [Serializable]
    public class SerializedReference<T> where T : class
    {
        /// <summary>
        /// Runtime-only reference. For generic types Unity cannot serialize via SerializeReference,
        /// this is reconstructed from <see cref="pureReferenceData"/> on load. For non-generic types
        /// with zero fields it is likewise reconstructed; the inspector draws fields manually from
        /// the reconstructed object rather than through a SerializedProperty.
        /// </summary>
        [NonSerialized] internal T pureReference;

        [SerializeField, HideInInspector] internal string pureReferenceData;

        [SerializeField, HideInInspector] internal Object unityReference;

        [SerializeField, HideInInspector] internal string expectedTypeName;

        public Type ExpectedType => Type.GetType(expectedTypeName);

        public T Value
        {
            get
            {
                // Migrate legacy "{}" data (empty JSON for fieldless types) to null.
                if (pureReferenceData == "{}")
                    pureReferenceData = null;

                if (pureReference != null)
                    return pureReference;

                if (unityReference != null)
                {
                    if (unityReference is T unityObj)
                        return unityObj;

                    if (unityReference is GameObject go)
                        return go.GetComponent<T>();
                }

                if (!string.IsNullOrEmpty(expectedTypeName))
                {
                    if (!string.IsNullOrEmpty(pureReferenceData))
                    {
                        var type = Type.GetType(expectedTypeName);
                        if (type != null)
                            pureReference = JsonUtility.FromJson(pureReferenceData, type) as T;
                        if (pureReference != null)
                            return pureReference;
                    }

                    var expectedType = Type.GetType(expectedTypeName);
                    return pureReference = expectedType != null && typeof(T).IsAssignableFrom(expectedType)
                        ? (T) Activator.CreateInstance(expectedType)!
                        : null;
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    pureReference     = null;
                    pureReferenceData = null;
                    unityReference    = null;
                    expectedTypeName  = null;
                }
                else if (value is Object uObj)
                {
                    pureReference     = null;
                    pureReferenceData = null;
                    unityReference    = uObj;
                    expectedTypeName  = uObj.GetType().AssemblyQualifiedName;
                }
                else
                {
                    pureReference     = value;
                    unityReference    = null;
                    expectedTypeName  = value.GetType().AssemblyQualifiedName;
                    var json = JsonUtility.ToJson(value);
                    pureReferenceData = json == "{}" ? null : json;
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

    /// <summary>
    /// Indicates that a <see cref="SerializedReference{T}"/> field is allowed to be null or have a type that does not have a default constructor.
    /// This is useful for cases where the field may be assigned a value at runtime or through other means, and the default constructor is not required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AllowDefaultConstructorMissingAttribute : Attribute
    {
        public AllowDefaultConstructorMissingAttribute() { }
    }
}