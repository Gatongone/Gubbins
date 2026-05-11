using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Generator;

[Generator]
public sealed class SpanOperationsGenerator : ISourceGenerator
{
    private const string FILE_SUFFIX = "SpanOperations.g.cs";
    private const string COMPONENT_ATTRIBUTE_NAMESPACE = "Gubbins.Enhance";
    private const string COMPONENT_ATTRIBUTE_NAME = "SpanOperationAttribute";
    private const string SPAN_OPERATION_INTERFACE_NAME = "Gubbins.Enhance.ISpanOperation";

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new ComponentStructReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ComponentStructReceiver receiver) return;

        var processed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var component in receiver.Structs)
        {
            if (!processed.Add(component)) continue;
            var componentAttributes = GetComponentAttributes(component).ToArray();
            if (componentAttributes.Length == 0) continue;

            var location = componentAttributes[0].ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? component.Locations.FirstOrDefault()
                           ?? Location.None;

            var config = BuildComponentConfig(componentAttributes);
            var requestedInterfaces = config.Interfaces;
            if (requestedInterfaces.Count == 0) continue;

            var targets = new List<InterfaceTarget>();
            foreach (var requestedInterface in requestedInterfaces)
            {
                if (requestedInterface.TypeKind != TypeKind.Interface || !IsSpanOperationInterface(requestedInterface))
                {
                    context.ReportDiagnostic(
                        ErrorDiagnostic.Descriptor.SpanOperationTypeInvalid,
                        location,
                        requestedInterface.ToDisplayString());
                    continue;
                }

                if (!requestedInterface.IsGenericType || requestedInterface.TypeArguments.Length == 0)
                {
                    context.ReportDiagnostic(
                        ErrorDiagnostic.Descriptor.SpanOperationTypeNotGeneric,
                        location,
                        requestedInterface.ToDisplayString());
                    continue;
                }

                var valueType = requestedInterface.TypeArguments[0];
                var memberOverwrite = config.TryGetMemberName(requestedInterface, out var requestedMemberName)
                    ? requestedMemberName
                    : null;

                var members = GetBindableMembers(component, valueType, memberOverwrite).ToArray();
                if (members.Length == 0)
                {
                    context.ReportDiagnostic(
                        ErrorDiagnostic.Descriptor.SpanOperationMemberNotFound,
                        location,
                        component.ToDisplayString(),
                        valueType.ToDisplayString(),
                        requestedInterface.ToDisplayString());
                    continue;
                }

                if (members.Length > 1)
                {
                    context.ReportDiagnostic(
                        ErrorDiagnostic.Descriptor.SpanOperationMemberAmbiguous,
                        location,
                        component.ToDisplayString(),
                        valueType.ToDisplayString(),
                        requestedInterface.ToDisplayString(),
                        string.Join(", ", members.Select(m => m.Name)));
                    continue;
                }

                var proxiedInterface = BuildProxiedInterface(component, requestedInterface);
                if (proxiedInterface == null)
                {
                    context.ReportDiagnostic(
                        ErrorDiagnostic.Descriptor.SpanOperationTypeNotGeneric,
                        location,
                        requestedInterface.ToDisplayString());
                    continue;
                }

                var member = members[0];
                if (!ValidateMethods(context, location, requestedInterface, proxiedInterface, component, member))
                {
                    continue;
                }

                targets.Add(new InterfaceTarget(
                    requestedInterface,
                    proxiedInterface,
                    member,
                    config.GetMethodRenames(requestedInterface)));
            }

            targets = FilterConflictingTargets(context, location, component, targets);
            if (targets.Count == 0) continue;

            var source = GenerateAdapter(component, targets);
            var hintName = $"{GetHintSafeName(component)}.{FILE_SUFFIX}";
            context.AddSource(hintName, source);
        }
    }

    private static List<INamedTypeSymbol> ParseRequestedInterfaces(AttributeData attribute)
    {
        var result = new List<INamedTypeSymbol>();

        foreach (var argument in attribute.ConstructorArguments)
        {
            CollectTypeSymbols(argument, result);
        }

        foreach (var namedArgument in attribute.NamedArguments)
        {
            if (!namedArgument.Key.Equals("Operations", StringComparison.Ordinal)) continue;
            CollectTypeSymbols(namedArgument.Value, result);
        }

        return result;
    }

    private static IEnumerable<AttributeData> GetComponentAttributes(INamedTypeSymbol component)
    {
        foreach (var attr in component.GetAttributes())
        {
            if (attr.AttributeClass?.ContainingNamespace.ToDisplayString() != COMPONENT_ATTRIBUTE_NAMESPACE) continue;
            if (attr.AttributeClass.Name == COMPONENT_ATTRIBUTE_NAME || attr.AttributeClass.Name == COMPONENT_ATTRIBUTE_NAME.Replace("Attribute", string.Empty))
            {
                yield return attr;
            }
        }
    }

    private static ComponentConfig BuildComponentConfig(IReadOnlyList<AttributeData> attributes)
    {
        var interfaces = new List<INamedTypeSymbol>();
        var memberByOperation = new Dictionary<string, string>(StringComparer.Ordinal);
        var renameByOperation = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

        foreach (var attribute in attributes)
        {
            var operations = ParseRequestedInterfaces(attribute);
            interfaces.AddRange(operations);
            if (operations.Count == 0) continue;

            var member = GetNamedString(attribute, "Member");
            if (!string.IsNullOrWhiteSpace(member))
            {
                foreach (var operation in operations)
                {
                    memberByOperation[ToQualifiedName(operation)] = member!;
                }
            }

            var method = GetNamedString(attribute, "Method");
            var name = GetNamedString(attribute, "Overwrite");
            if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(name)) continue;

            foreach (var operation in operations)
            {
                var key = ToQualifiedName(operation);
                if (!renameByOperation.TryGetValue(key, out var map))
                {
                    map = new Dictionary<string, string>(StringComparer.Ordinal);
                    renameByOperation.Add(key, map);
                }

                map[method!] = name!;
            }
        }

        return new ComponentConfig(interfaces, memberByOperation, renameByOperation);
    }

    private static string? GetNamedString(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (!argument.Key.Equals(name, StringComparison.Ordinal)) continue;
            if (argument.Value.Value is string value && !string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }

    private static void CollectTypeSymbols(TypedConstant constant, ICollection<INamedTypeSymbol> result)
    {
        if (constant.Kind == TypedConstantKind.Type && constant.Value is INamedTypeSymbol named)
        {
            result.Add(named);
            return;
        }

        if (constant.Kind != TypedConstantKind.Array) return;
        foreach (var item in constant.Values)
        {
            if (item.Kind == TypedConstantKind.Type && item.Value is INamedTypeSymbol itemType)
            {
                result.Add(itemType);
            }
        }
    }

    private static bool ValidateMethods(
        GeneratorExecutionContext context,
        Location location,
        INamedTypeSymbol requestedInterface,
        INamedTypeSymbol proxiedInterface,
        INamedTypeSymbol component,
        ComponentMember member)
    {
        foreach (var method in GetOperationMethods(proxiedInterface))
        {
            if (HasUnsupportedByRef(method) || HasUnsupportedReturnType(method.ReturnType, component))
            {
                context.ReportDiagnostic(
                    ErrorDiagnostic.Descriptor.SpanOperationMethodNotSupported,
                    location,
                    method.ToDisplayString(),
                    requestedInterface.ToDisplayString());
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(method.ReturnType, component) && !member.CanWriteInInitializer)
            {
                context.ReportDiagnostic(
                    ErrorDiagnostic.Descriptor.SpanOperationComponentReturnNotWritable,
                    location,
                    method.ToDisplayString(),
                    requestedInterface.ToDisplayString(),
                    component.ToDisplayString(),
                    member.Name);
                return false;
            }
        }

        return true;
    }

    private static bool HasUnsupportedByRef(IMethodSymbol method)
        => method.Parameters.Any(p => p.RefKind != RefKind.None);

    private static bool HasUnsupportedReturnType(ITypeSymbol returnType, INamedTypeSymbol component)
    {
        if (returnType.SpecialType == SpecialType.System_Void) return false;
        if (SymbolEqualityComparer.Default.Equals(returnType, component)) return false;

        if (TryGetSpanElementType(returnType, out var elementType))
        {
            return SymbolEqualityComparer.Default.Equals(elementType, component);
        }

        return false;
    }

    private static bool IsSpanOperationInterface(INamedTypeSymbol symbol)
    {
        if (symbol.ToDisplayString() == SPAN_OPERATION_INTERFACE_NAME) return true;
        return symbol.AllInterfaces.Any(i => i.ToDisplayString() == SPAN_OPERATION_INTERFACE_NAME);
    }

    private static INamedTypeSymbol? BuildProxiedInterface(INamedTypeSymbol component, INamedTypeSymbol requestedInterface)
    {
        var interfaceDefinition = requestedInterface.ConstructedFrom;
        if (!interfaceDefinition.IsGenericType || requestedInterface.TypeArguments.Length == 0) return null;

        var args = requestedInterface.TypeArguments.ToArray();
        args[0] = component;
        return interfaceDefinition.Construct(args);
    }

    private static List<InterfaceTarget> FilterConflictingTargets(
        GeneratorExecutionContext context,
        Location location,
        INamedTypeSymbol component,
        List<InterfaceTarget> targets)
    {
        var conflictingSurfaces = new HashSet<string>(StringComparer.Ordinal);

        foreach (var group in targets.GroupBy(t => ToQualifiedName(t.ProxiedInterface), StringComparer.Ordinal))
        {
            var items = group.ToArray();
            if (items.Length < 2) continue;

            conflictingSurfaces.Add(group.Key);
            context.ReportDiagnostic(
                ErrorDiagnostic.Descriptor.SpanOperationConflict,
                location,
                component.ToDisplayString(),
                items[0].RequestedInterface.ToDisplayString(),
                string.Join(", ", items.Skip(1).Select(i => i.RequestedInterface.ToDisplayString())),
                group.Key);
        }

        if (conflictingSurfaces.Count == 0) return targets;

        return targets
            .Where(t => !conflictingSurfaces.Contains(ToQualifiedName(t.ProxiedInterface)))
            .ToList();
    }

    private static IEnumerable<ComponentMember> GetBindableMembers(INamedTypeSymbol component, ITypeSymbol valueType, string? memberOverwrite)
    {
        foreach (var member in component.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                if (field.IsStatic || field.IsConst) continue;
                if (!string.IsNullOrWhiteSpace(memberOverwrite) && !field.Name.Equals(memberOverwrite, StringComparison.Ordinal)) continue;
                if (!SymbolEqualityComparer.Default.Equals(field.Type, valueType)) continue;
                if (!IsReadable(field.DeclaredAccessibility)) continue;

                yield return new ComponentMember(
                    field.Name,
                    field.Type,
                    canWriteInInitializer: !field.IsReadOnly);
            }

            if (member is IPropertySymbol property)
            {
                if (property.IsStatic) continue;
                if (!string.IsNullOrWhiteSpace(memberOverwrite) && !property.Name.Equals(memberOverwrite, StringComparison.Ordinal)) continue;
                if (!SymbolEqualityComparer.Default.Equals(property.Type, valueType)) continue;
                if (property.GetMethod == null || !IsReadable(property.GetMethod.DeclaredAccessibility)) continue;

                var canSet = property.SetMethod != null && IsReadable(property.SetMethod.DeclaredAccessibility);
                yield return new ComponentMember(
                    property.Name,
                    property.Type,
                    canWriteInInitializer: canSet);
            }
        }
    }

    private static bool IsReadable(Accessibility accessibility)
        => accessibility == Accessibility.Public
           || accessibility == Accessibility.Internal
           || accessibility == Accessibility.ProtectedOrInternal;

    private static string GenerateAdapter(INamedTypeSymbol component, IReadOnlyList<InterfaceTarget> targets)
    {
        var componentTypeName = ToQualifiedName(component);
        var className = $"{GetHintSafeName(component)}GeneratedSpanOperations";

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Gubbins.Enhance;");
        sb.AppendLine();

        var hasNamespace = !component.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {component.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
        }

        var outerIndent = hasNamespace ? "    " : string.Empty;
        sb.AppendLine($"{outerIndent}[global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"{outerIndent}internal sealed class {className} :");
        for (var index = 0; index < targets.Count; index++)
        {
            var suffix = index == targets.Count - 1 ? string.Empty : ",";
            sb.AppendLine($"{outerIndent}    {ToQualifiedName(targets[index].ProxiedInterface)}{suffix}");
        }

        sb.AppendLine($"{outerIndent}{{");
        sb.AppendLine($"{outerIndent}    public bool Supported => true;");
        sb.AppendLine();

        foreach (var target in targets)
        {
            foreach (var method in GetOperationMethods(target.ProxiedInterface))
            {
                GenerateMethod(sb, outerIndent, component, componentTypeName, target, method);
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{outerIndent}}}");

        sb.AppendLine();
        GenerateExtensions(sb, outerIndent, component, componentTypeName, targets);

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    private static void GenerateMethod(
        StringBuilder sb,
        string outerIndent,
        INamedTypeSymbol componentType,
        string componentTypeName,
        InterfaceTarget target,
        IMethodSymbol proxiedMethod)
    {
        var methodIndent = outerIndent + "    ";
        var bodyIndent = methodIndent + "    ";

        var returnType = ToQualifiedName(proxiedMethod.ReturnType);
        var methodParameters = string.Join(", ", proxiedMethod.Parameters.Select(FormatParameter));

        var explicitInterface = ToQualifiedName(proxiedMethod.ContainingType);
        sb.AppendLine($"{methodIndent}{returnType} {explicitInterface}.{proxiedMethod.Name}({methodParameters})");
        sb.AppendLine($"{methodIndent}{{");
        sb.AppendLine($"{bodyIndent}var op = SpanOperations.GetOperation<{ToQualifiedName(target.Member.Type)}, {ToQualifiedName(target.RequestedInterface)}>();");

        if (proxiedMethod.ReturnsVoid)
        {
            sb.AppendLine($"{bodyIndent}if (op == null) return;");
        }
        else
        {
            sb.AppendLine($"{bodyIndent}if (op == null) return default;");
        }

        var callArguments = new List<string>(proxiedMethod.Parameters.Length);
        for (var index = 0; index < proxiedMethod.Parameters.Length; index++)
        {
            var parameter = proxiedMethod.Parameters[index];
            var mappedName = MapParameter(parameter, componentType, componentTypeName, target.Member, bodyIndent, sb, index);
            callArguments.Add(mappedName);
        }

        var invocation = $"op.{proxiedMethod.Name}({string.Join(", ", callArguments)})";
        if (proxiedMethod.ReturnsVoid)
        {
            sb.AppendLine($"{bodyIndent}{invocation};");
        }
        else if (SymbolEqualityComparer.Default.Equals(proxiedMethod.ReturnType, target.ProxiedInterface.TypeArguments[0]))
        {
            sb.AppendLine($"{bodyIndent}var value = {invocation};");
            sb.AppendLine($"{bodyIndent}return new {componentTypeName} {{ {target.Member.Name} = value }};");
        }
        else
        {
            sb.AppendLine($"{bodyIndent}return {invocation};");
        }

        sb.AppendLine($"{methodIndent}}}");
    }

    private static void GenerateExtensions(
        StringBuilder sb,
        string outerIndent,
        INamedTypeSymbol componentType,
        string componentTypeName,
        IReadOnlyList<InterfaceTarget> targets)
    {
        var className = $"{GetHintSafeName(componentType)}SpanExtensions";
        var typeIndent = outerIndent;
        var methodIndent = typeIndent + "    ";
        var bodyIndent = methodIndent + "    ";

        sb.AppendLine($"{typeIndent}[global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"{typeIndent}public static class {className}");
        sb.AppendLine($"{typeIndent}{{");

        var emitted = new HashSet<string>(StringComparer.Ordinal);
        var specs = new List<ExtensionMethodSpec>();
        foreach (var target in targets)
        {
            foreach (var method in GetOperationMethods(target.RequestedInterface))
            {
                var extensionKey = $"{ToQualifiedName(target.RequestedInterface)}::{GetMethodSignatureKey(method)}";
                if (!emitted.Add(extensionKey)) continue;
                if (method.Parameters.Length == 0) continue;
                if (!TryGetSpanElementType(method.Parameters[0].Type, out var firstElement)
                    || !SymbolEqualityComparer.Default.Equals(firstElement, target.Member.Type))
                {
                    continue;
                }

                specs.Add(new ExtensionMethodSpec(
                    target,
                    method,
                    ResolveExtensionMethodName(target, method),
                    useComponentScalarParameters: false,
                    asComponentReturn: false));

                if (method.Parameters.Any(p => IsConvertibleScalarParameter(p, target.Member.Type)))
                {
                    specs.Add(new ExtensionMethodSpec(
                        target,
                        method,
                        ResolveExtensionMethodName(target, method),
                        useComponentScalarParameters: true,
                        asComponentReturn: false));
                }

                if (!method.ReturnsVoid && SymbolEqualityComparer.Default.Equals(method.ReturnType, target.Member.Type))
                {
                    var asComponentMethodName = ResolveExtensionMethodName(target, method) + "AsComponent";
                    specs.Add(new ExtensionMethodSpec(
                        target,
                        method,
                        asComponentMethodName,
                        useComponentScalarParameters: false,
                        asComponentReturn: true));

                    if (method.Parameters.Any(p => IsConvertibleScalarParameter(p, target.Member.Type)))
                    {
                        specs.Add(new ExtensionMethodSpec(
                            target,
                            method,
                            asComponentMethodName,
                            useComponentScalarParameters: true,
                            asComponentReturn: true));
                    }
                }
            }
        }

        var resolvedSpecs = ResolveExtensionNameConflicts(specs, componentTypeName);
        var emittedSignatures = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spec in resolvedSpecs)
        {
            EmitExtensionMethod(sb, methodIndent, bodyIndent, spec, componentTypeName, emittedSignatures);
        }

        sb.AppendLine($"{typeIndent}}}");

        void EmitExtensionMethod(
            StringBuilder builder,
            string methodIndentation,
            string bodyIndentation,
            ExtensionMethodSpec spec,
            string componentQualifiedName,
            HashSet<string> emittedSet)
        {
            var mappedParameters = spec.OperationMethod.Parameters
                .Select((parameter, index) => MapExtensionParameter(parameter, index, spec.OperationMethod, spec.Target.Member.Type, spec.Target.Member.Name, componentQualifiedName, spec.UseComponentScalarParameters))
                .ToArray();

            var parameterDeclarations = string.Join(", ", mappedParameters.Select(p => p.Declaration));
            var returnType = spec.AsComponentReturn ? componentQualifiedName : ToQualifiedName(spec.OperationMethod.ReturnType);

            var signatureKey = $"{spec.MethodName}({parameterDeclarations})";
            if (!emittedSet.Add(signatureKey)) return;

            builder.AppendLine($"{methodIndentation}public static {returnType} {spec.MethodName}({parameterDeclarations}, SpanOperationMask mask = SpanOperationMask.Auto)");
            builder.AppendLine($"{methodIndentation}{{");
            builder.AppendLine($"{bodyIndentation}var op = SpanOperations.GetOperation<{ToQualifiedName(spec.Target.Member.Type)}, {ToQualifiedName(spec.Target.RequestedInterface)}>(mask);");

            if (spec.OperationMethod.ReturnsVoid)
            {
                builder.AppendLine($"{bodyIndentation}if (op == null) return;");
            }
            else
            {
                builder.AppendLine($"{bodyIndentation}if (op == null) return default;");
            }

            foreach (var mapped in mappedParameters)
            {
                if (!string.IsNullOrEmpty(mapped.PrepareLine))
                {
                    builder.AppendLine($"{bodyIndentation}{mapped.PrepareLine}");
                }
            }

            var callArguments = string.Join(", ", mappedParameters.Select(p => p.CallArgument));
            var invocation = $"op.{spec.OperationMethod.Name}({callArguments})";
            if (spec.OperationMethod.ReturnsVoid)
            {
                builder.AppendLine($"{bodyIndentation}{invocation};");
            }
            else if (spec.AsComponentReturn)
            {
                builder.AppendLine($"{bodyIndentation}var value = {invocation};");
                builder.AppendLine($"{bodyIndentation}return new {componentQualifiedName} {{ {spec.Target.Member.Name} = value }};");
            }
            else
            {
                builder.AppendLine($"{bodyIndentation}return {invocation};");
            }

            builder.AppendLine($"{methodIndentation}}}");
            builder.AppendLine();
        }
    }

    private static IReadOnlyList<ExtensionMethodSpec> ResolveExtensionNameConflicts(
        IReadOnlyList<ExtensionMethodSpec> specs,
        string componentTypeName)
    {
        var resolved = specs.ToArray();
        var changed = true;
        while (changed)
        {
            changed = false;
            var signatures = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var index = 0; index < resolved.Length; index++)
            {
                var declaration = BuildExtensionDeclarationSignature(resolved[index], componentTypeName);
                var signature = $"{resolved[index].MethodName}({declaration})";
                if (!signatures.TryGetValue(signature, out var list))
                {
                    list = new List<int>();
                    signatures.Add(signature, list);
                }

                list.Add(index);
            }

            foreach (var duplicateGroup in signatures.Values.Where(group => group.Count > 1))
            {
                for (var order = 0; order < duplicateGroup.Count; order++)
                {
                    var targetIndex = duplicateGroup[order];
                    var current = resolved[targetIndex];
                    var suffix = GetHintSafeName(current.Target.RequestedInterface);
                    if (string.IsNullOrEmpty(suffix))
                    {
                        suffix = "Operation";
                    }

                    var nextName = current.MethodName + "_" + suffix;
                    if (order > 0)
                    {
                        nextName += "_" + order;
                    }

                    resolved[targetIndex] = new ExtensionMethodSpec(
                        current.Target,
                        current.OperationMethod,
                        nextName,
                        current.UseComponentScalarParameters,
                        current.AsComponentReturn);
                    changed = true;
                }
            }
        }

        return resolved;
    }

    private static string BuildExtensionDeclarationSignature(ExtensionMethodSpec spec, string componentTypeName)
        => string.Join(", ", spec.OperationMethod.Parameters.Select((parameter, index) =>
            MapExtensionParameter(
                parameter,
                index,
                spec.OperationMethod,
                spec.Target.Member.Type,
                spec.Target.Member.Name,
                componentTypeName,
                spec.UseComponentScalarParameters).Declaration));

    private static string ResolveExtensionMethodName(InterfaceTarget target, IMethodSymbol method)
    {
        if (target.MethodRenames.TryGetValue(method.Name, out var rename) && !string.IsNullOrWhiteSpace(rename))
        {
            return rename;
        }

        return method.Name;
    }

    private static ExtensionParameterMap MapExtensionParameter(
        IParameterSymbol parameter,
        int index,
        IMethodSymbol method,
        ITypeSymbol valueType,
        string componentMemberName,
        string componentTypeName,
        bool useComponentScalarParameters)
    {
        if (TryGetSpanElementType(parameter.Type, out var elementType)
            && SymbolEqualityComparer.Default.Equals(elementType, valueType))
        {
            var isReadOnly = parameter.Type is INamedTypeSymbol namedSpan && namedSpan.Name == "ReadOnlySpan";
            var spanType = isReadOnly ? "global::System.ReadOnlySpan" : "global::System.Span";
            var declarationType = $"{spanType}<{componentTypeName}>";
            var isFirst = index == 0;
            var extensionPrefix = isFirst ? "this " : string.Empty;
            var castName = $"arg{index}";
            var prepareLine = $"var {castName} = MemoryMarshal.Cast<{componentTypeName}, {ToQualifiedName(valueType)}>({parameter.Name});";
            return new ExtensionParameterMap(
                declaration: $"{extensionPrefix}{declarationType} {parameter.Name}",
                callArgument: castName,
                prepareLine: prepareLine);
        }

        if (useComponentScalarParameters
            && index > 0
            && SymbolEqualityComparer.Default.Equals(parameter.Type, valueType))
        {
            return new ExtensionParameterMap(
                declaration: $"{componentTypeName} {parameter.Name}",
                callArgument: $"{parameter.Name}.{componentMemberName}",
                prepareLine: string.Empty);
        }

        var parameterDeclaration = $"{ToQualifiedName(parameter.Type)} {parameter.Name}";
        if (index == 0 && method.Parameters.Length > 0)
        {
            parameterDeclaration = "this " + parameterDeclaration;
        }

        return new ExtensionParameterMap(
            declaration: parameterDeclaration,
            callArgument: parameter.Name,
            prepareLine: string.Empty);
    }

    private static bool IsConvertibleScalarParameter(IParameterSymbol parameter, ITypeSymbol valueType)
        => !TryGetSpanElementType(parameter.Type, out _)
           && SymbolEqualityComparer.Default.Equals(parameter.Type, valueType);

    private static string MapParameter(
        IParameterSymbol parameter,
        INamedTypeSymbol componentType,
        string componentTypeName,
        ComponentMember member,
        string bodyIndent,
        StringBuilder sb,
        int index)
    {
        // Cast Span<Component>/ReadOnlySpan<Component> to the mapped scalar member type.
        if (TryGetSpanElementType(parameter.Type, out var spanType)
            && SymbolEqualityComparer.Default.Equals(spanType, componentType))
        {
            var casted = $"arg{index}";
            sb.AppendLine($"{bodyIndent}var {casted} = MemoryMarshal.Cast<{componentTypeName}, {ToQualifiedName(member.Type)}>({parameter.Name});");
            return casted;
        }

        if (SymbolEqualityComparer.Default.Equals(parameter.Type, componentType))
        {
            return $"{parameter.Name}.{member.Name}";
        }

        return parameter.Name;
    }

    private static bool TryGetSpanElementType(ITypeSymbol symbol, out ITypeSymbol elementType)
    {
        if (symbol is INamedTypeSymbol named
            && named.ContainingNamespace.ToDisplayString() == "System"
            && (named.Name == "Span" || named.Name == "ReadOnlySpan")
            && named.TypeArguments.Length == 1)
        {
            elementType = named.TypeArguments[0];
            return true;
        }

        elementType = null!;
        return false;
    }

    private static IEnumerable<IMethodSymbol> GetOperationMethods(INamedTypeSymbol interfaceType)
    {
        var methods = new List<IMethodSymbol>();

        methods.AddRange(interfaceType.GetMembers().OfType<IMethodSymbol>());
        foreach (var baseInterface in interfaceType.AllInterfaces)
        {
            methods.AddRange(baseInterface.GetMembers().OfType<IMethodSymbol>());
        }

        return methods
            .Where(m => !m.IsStatic && m.MethodKind == MethodKind.Ordinary)
            .GroupBy(GetMethodSignatureKey, StringComparer.Ordinal)
            .Select(group => group.First());
    }

    private static string GetMethodSignatureKey(IMethodSymbol method)
        => method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);

    private static string FormatParameter(IParameterSymbol parameter)
    {
        var refPrefix = parameter.RefKind switch
        {
            RefKind.In => "in ",
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            _ => string.Empty
        };

        return $"{refPrefix}{ToQualifiedName(parameter.Type)} {parameter.Name}";
    }

    private static string ToQualifiedName(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string GetHintSafeName(INamedTypeSymbol symbol)
    {
        var value = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        return new string(chars).Trim('_');
    }

    private readonly struct ComponentMember
    {
        public ComponentMember(string name, ITypeSymbol type, bool canWriteInInitializer)
        {
            Name = name;
            Type = type;
            CanWriteInInitializer = canWriteInInitializer;
        }

        public string Name { get; }
        public ITypeSymbol Type { get; }
        public bool CanWriteInInitializer { get; }
    }

    private readonly struct InterfaceTarget(INamedTypeSymbol requestedInterface, INamedTypeSymbol proxiedInterface, ComponentMember member, IReadOnlyDictionary<string, string> methodRenames)
    {
        public INamedTypeSymbol RequestedInterface { get; } = requestedInterface;
        public INamedTypeSymbol ProxiedInterface { get; } = proxiedInterface;
        public ComponentMember Member { get; } = member;
        public IReadOnlyDictionary<string, string> MethodRenames { get; } = methodRenames;
    }

    private readonly struct ExtensionParameterMap(string declaration, string callArgument, string prepareLine)
    {
        public string Declaration { get; } = declaration;
        public string CallArgument { get; } = callArgument;
        public string PrepareLine { get; } = prepareLine;
    }

    private sealed class ComponentConfig
    {
        private static readonly IReadOnlyDictionary<string, string> EmptyRenames = new Dictionary<string, string>(StringComparer.Ordinal);

        public ComponentConfig(
            List<INamedTypeSymbol> interfaces,
            Dictionary<string, string> memberByOperation,
            Dictionary<string, Dictionary<string, string>> renameByOperation)
        {
            Interfaces = interfaces;
            _memberByOperation = memberByOperation;
            _renameByOperation = renameByOperation;
        }

        private readonly Dictionary<string, string> _memberByOperation;
        private readonly Dictionary<string, Dictionary<string, string>> _renameByOperation;

        public List<INamedTypeSymbol> Interfaces { get; }

        public bool TryGetMemberName(INamedTypeSymbol operation, out string memberName)
            => _memberByOperation.TryGetValue(ToQualifiedName(operation), out memberName!);

        public IReadOnlyDictionary<string, string> GetMethodRenames(INamedTypeSymbol operation)
            => _renameByOperation.TryGetValue(ToQualifiedName(operation), out var map) ? map : EmptyRenames;
    }

    private readonly struct ExtensionMethodSpec
    {
        public ExtensionMethodSpec(
            InterfaceTarget target,
            IMethodSymbol operationMethod,
            string methodName,
            bool useComponentScalarParameters,
            bool asComponentReturn)
        {
            Target = target;
            OperationMethod = operationMethod;
            MethodName = methodName;
            UseComponentScalarParameters = useComponentScalarParameters;
            AsComponentReturn = asComponentReturn;
        }

        public InterfaceTarget Target { get; }
        public IMethodSymbol OperationMethod { get; }
        public string MethodName { get; }
        public bool UseComponentScalarParameters { get; }
        public bool AsComponentReturn { get; }
    }

    private sealed class ComponentStructReceiver : ISyntaxContextReceiver
    {
        public List<INamedTypeSymbol> Structs { get; } = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is not StructDeclarationSyntax && context.Node is not RecordDeclarationSyntax)
            {
                return;
            }

            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol symbol) return;
            if (symbol.TypeKind != TypeKind.Struct) return;

            var hasComponent = symbol.ContainsAttribute(COMPONENT_ATTRIBUTE_NAMESPACE, COMPONENT_ATTRIBUTE_NAME);
            if (hasComponent)
            {
                Structs.Add(symbol);
            }
        }
    }
}

