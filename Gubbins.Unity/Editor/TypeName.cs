using System;
using System.Linq;

namespace Gubbins.Editor
{
    public static class TypeName
    {
        /// <summary>
        /// Get the namespace grouping label for a type.
        /// </summary>
        public static string GetNamespaceGroup(Type type) => string.IsNullOrEmpty(type.Namespace) ? "(Global)" : type.Namespace;

        /// <summary>
        /// Build a friendly type name including namespace, suitable for editor popups.
        /// </summary>
        public static string GetFriendlyTypeFullName(Type type) => string.IsNullOrEmpty(type.Namespace) ? GetFriendlyTypeName(type) : $"{type.Namespace}.{GetFriendlyTypeName(type)}";

        /// <summary>
        /// Build a friendly type name suitable for editor popups.
        /// </summary>
        public static string GetFriendlyTypeName(Type type)
        {
            if (type == null)
                return string.Empty;

            if (type.IsGenericParameter)
                return type.Name;

            if (type.IsArray)
                return $"{GetFriendlyTypeName(type.GetElementType())}[{new string(',', type.GetArrayRank() - 1)}]";

            if (type.IsByRef)
                return $"{GetFriendlyTypeName(type.GetElementType())}&";

            if (type.IsPointer)
                return $"{GetFriendlyTypeName(type.GetElementType())}*";

            var typeName = GetFriendlyNestedTypeName(type);
            var genericArguments = GetOwnGenericArguments(type);
            if (genericArguments.Length == 0)
                return typeName;

            return $"{typeName}<{string.Join(", ", genericArguments.Select(GetFriendlyTypeName))}>";
        }

        /// <summary>
        /// Build a nested type name without namespace and generic arity suffixes.
        /// </summary>
        private static string GetFriendlyNestedTypeName(Type type)
        {
            var name = StripGenericArity(type.Name);
            if (type.DeclaringType == null)
                return name;

            return $"{GetFriendlyNestedTypeName(type.DeclaringType)}.{name}";
        }

        /// <summary>
        /// Get only the generic arguments introduced by the current type, excluding declaring-type arguments.
        /// </summary>
        private static Type[] GetOwnGenericArguments(Type type)
        {
            if (!type.IsGenericType)
                return Array.Empty<Type>();

            var allArguments = type.GetGenericArguments();
            var declaringArgumentCount = type.DeclaringType?.GetGenericArguments().Length ?? 0;
            var ownArgumentCount = Math.Max(0, allArguments.Length - declaringArgumentCount);
            if (ownArgumentCount == 0)
                return Array.Empty<Type>();

            return allArguments.Skip(declaringArgumentCount).ToArray();
        }

        /// <summary>
        /// Strip the generic arity suffix from a CLR type name.
        /// </summary>
        private static string StripGenericArity(string typeName)
        {
            var genericIndex = typeName.IndexOf('`');
            return genericIndex < 0 ? typeName : typeName.Substring(0, genericIndex);
        }
    }
}