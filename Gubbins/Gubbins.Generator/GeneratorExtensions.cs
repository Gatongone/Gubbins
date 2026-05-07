using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

internal static class GeneratorExtensions
{
    internal static string GetConstraintsString(this IMethodSymbol method)
    {
        var constraints = new List<string>();
        foreach (var p in method.TypeParameters)
        {
            var curConstraints = new List<string>();
            if (p.HasNotNullConstraint)
                curConstraints.Add("notnull");
            else if (p.HasUnmanagedTypeConstraint)
                curConstraints.Add("unmanaged");
            else if (p.HasValueTypeConstraint)
                curConstraints.Add("struct");
            else if (p.HasReferenceTypeConstraint)
                curConstraints.Add("class");

            foreach (var constraint in p.ConstraintTypes)
            {
                curConstraints.Add(constraint.ToDisplayString());
            }

            if (p is {HasConstructorConstraint: true, HasUnmanagedTypeConstraint: false, HasValueTypeConstraint: false})
                curConstraints.Add("new()");

            if (curConstraints.Any())
                constraints.Add($"where {p.Name} : {string.Join(", ", curConstraints)}");
        }

        return string.Join(" ", constraints);
    }

    internal static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol type) => type.GetMembers().Where(m => m is IMethodSymbol).Cast<IMethodSymbol>();

    /// <summary>
    /// Get type declaration string.
    /// </summary>
    /// <example>
    /// <code>
    /// partial class MyClass
    /// {
    /// </code>
    /// or
    /// <code>
    /// partial class NestedClass
    /// {
    ///     partial class MyClass
    ///     {
    /// </code>
    /// </example>
    /// <param name="nestedTypes">Partial nested types collections.</param>
    /// <param name="fixedLength">Fixed indentation length.</param>
    /// <returns>type declaration string.</returns>
    internal static string GetTypeStringBegin(this (string TypeName, string TypeKind)[] nestedTypes, int fixedLength)
    {
        var classTypeName = string.Empty;
        for (var index = 0; index < nestedTypes.Length; index++)
        {
            var type = nestedTypes[index];
            var indentation = new string('\t', nestedTypes.Length - index + fixedLength);
            if (index == 0)
            {
                classTypeName = $"{indentation}partial class {type.TypeName}"
                    + "\n"
                    + new string('\t', nestedTypes.Length - index + fixedLength) + "{"
                    + classTypeName;
            }
            else
            {
                classTypeName = $"{indentation}partial {type.TypeKind} {type.TypeName}"
                    + "\n"
                    + new string('\t', nestedTypes.Length - index + fixedLength) + "{" + '\n'
                    + classTypeName;
            }
        }

        return classTypeName;
    }

    internal static string GetTypeStringEnd(this (string TypeName, string TypeKind)[] nestedTypes, int fixedLength)
    {
        var brackets = string.Empty;
        for (var index = nestedTypes.Length - 1; index >= 0; index--)
        {
            brackets = new string('\t', nestedTypes.Length - index + fixedLength) + "}" + '\n' + brackets;
        }

        return brackets;
    }

    /// <summary>
    /// Recursive acquisition of all nested type of the current type (including itself).
    /// </summary>
    /// <param name="type">Target type symbol.</param>
    /// <returns>Nested type of the current type (including itself).</returns>
    internal static (string TypeName, string TypeKind)[] GetNestedTypeName(this ITypeSymbol type)
    {
        var result = new List<(string typeName, string typeKind)> {(type.Name, type.TypeKind.ToString().ToLower())};
        while (type.ContainingType != null)
        {
            type = type.ContainingType;
            result.Add((type.Name, type.TypeKind.ToString().ToLower()));
        }

        return result.ToArray();
    }

    internal static void ReportDiagnostic(this GeneratorExecutionContext context, DiagnosticDescriptor descriptor, Location location, params object[] args)
    {
        var diagnostic = Diagnostic.Create(descriptor, location, args);
        context.ReportDiagnostic(diagnostic);
    }

    internal static bool IsNewable(this ITypeSymbol typeSymbol)
    {
        if (typeSymbol.IsAbstract || typeSymbol.IsStatic) return false;
        if (typeSymbol.IsValueType) return true;

        var ctorCount = 0;
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol {MethodKind: MethodKind.Constructor} method) continue;
            if (method is
            {
                Parameters.Length    : 0,
                DeclaredAccessibility: Accessibility.Public
            }) return true;
            ctorCount++;
        }

        return ctorCount == 0;
    }

    internal static bool ContainsInterface(this ITypeSymbol typeSymbol, string interfaceFullName)
    {
        var curType = typeSymbol as INamedTypeSymbol;
        while (curType != null)
        {
            if (curType.Interfaces.Any(ins => ins.ToDisplayString().Equals(interfaceFullName)))
                return true;
            curType = curType is not ITypeSymbol baseTypeSymbol ? null : baseTypeSymbol.BaseType;
        }

        return false;
    }

    internal static bool ContainsAttribute(this ITypeSymbol typeSymbol, string attributeNamespace, string attributeName) => TryGetAttribute(typeSymbol, attributeNamespace, attributeName, out _);

    internal static bool TryGetAttribute(this ITypeSymbol typeSymbol, string attributeNamespace, string attributeName, out AttributeData attribute)
    {
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ContainingNamespace.ToDisplayString() != attributeNamespace) continue;
            if (attr.AttributeClass!.Name == attributeName || attr.AttributeClass!.Name == attributeName.Replace("Attribute", string.Empty))
            {
                attribute = attr;
                return true;
            }
        }

        attribute = null!;
        return false;
    }

    internal static bool ContainsMethod(this ITypeSymbol typeSymbol, string methodName, params string[] paramTypes) => TryGetMethod(typeSymbol, methodName, out _, paramTypes);

    internal static bool TryGetMethod(this ITypeSymbol typeSymbol, string methodName, out IMethodSymbol? symbol, params string[] paramTypes)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method ||
                method.Name != methodName ||
                method.Parameters.Length != paramTypes.Length ||
                !method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray().SequenceEqual(paramTypes)
            ) continue;
            symbol = method;
            return true;
        }

        symbol = null;
        return false;
    }

    internal static bool ContainsBaseType(this ITypeSymbol typeSymbol, string baseTypeFullName)
    {
        var curType = typeSymbol.BaseType;
        while (curType != null)
        {
            if (curType.ToDisplayString().Equals(baseTypeFullName))
                return true;
            curType = curType is not ITypeSymbol baseTypeSymbol ? null : baseTypeSymbol.BaseType;
        }

        return false;
    }

    internal static bool TryGetInterface(this ITypeSymbol typeSymbol, Predicate<INamedTypeSymbol> filter, out INamedTypeSymbol? interfaceSymbol)
    {
        var curType = typeSymbol.BaseType;
        while (curType != null)
        {
            foreach (var @interface in curType.Interfaces)
            {
                if (filter(@interface))
                {
                    interfaceSymbol = @interface;
                    return true;
                }
            }

            curType = curType is not ITypeSymbol baseTypeSymbol ? null : baseTypeSymbol.BaseType;
        }

        interfaceSymbol = null;
        return false;
    }
}