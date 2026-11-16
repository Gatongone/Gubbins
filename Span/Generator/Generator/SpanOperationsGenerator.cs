using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Generator;

[Generator]
public sealed class SpanOperationsGenerator : ISourceGenerator
{
    private const string FILE_SUFFIX = "SpanOperations.g.cs";
    private const string EXTENSIONS_FILE_SUFFIX = "SpanExtensions.g.cs";
    private const string COMPONENT_ATTRIBUTE_NAMESPACE = "Gubbins.Span";
    private const string SPAN_OPERATION_INTERFACE_NAMESPACE = "Gubbins.Span";
    private const string SPAN_OPERATION_INTERFACE_NAME = "ISpanOperation";
    private const string SPAN_AS_ATTRIBUTE_NAME = "SpanAsAttribute";
    private const string SPAN_TO_ATTRIBUTE_NAME = "SpanToAttribute";

    /// <summary>
    /// How a component's storage relates to the operation's value type.
    /// </summary>
    private enum Bridge
    {
        /// <summary>The component holds a member of the value type; only that member is operated on (1:1).</summary>
        As,

        /// <summary>The component's whole layout equals one value; every field is operated on (1:1).</summary>
        To
    }

    /// <summary>
    /// <c>System.Runtime.InteropServices.LayoutKind.Explicit</c> (Auto = 0, Sequential = 1, Explicit = 2).
    /// </summary>
    private const int EXPLICIT_LAYOUT_KIND = 2;

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
                var (bridge, lanes) = ResolveDeclaration(componentAttributes, requestedInterface);

                ComponentMember member;
                if (bridge == Bridge.To)
                {
                    if (!TryCreateLayoutMember(context, location, component, valueType, lanes, out member)) continue;
                }
                else
                {
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

                    member = members[0];
                    if (!ValidateComponentLayout(context, location, component, member)) continue;
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

                if (!ValidateMethods(context, location, requestedInterface, proxiedInterface, component, member))
                {
                    continue;
                }

                targets.Add(new InterfaceTarget(
                    requestedInterface,
                    proxiedInterface,
                    member,
                    config.GetMethodRenames(requestedInterface),
                    bridge));
            }

            targets = FilterConflictingTargets(context, location, component, targets);
            if (targets.Count == 0) continue;

            // The adapter is nested inside the component and registered from its static constructor, so the generator
            // has to reopen the declaration (and every enclosing type).
            if (!IsPartial(component))
            {
                context.ReportDiagnostic(
                    ErrorDiagnostic.Descriptor.SpanOperationComponentNotPartial,
                    location,
                    component.ToDisplayString());
                continue;
            }

            var hintSafeName = GetHintSafeName(component);

            // Gubbins.Span surface: always generated, depends on nothing but Gubbins.Span.
            context.AddSource($"{hintSafeName}.{FILE_SUFFIX}", GenerateAdapter(component, targets));
            context.AddSource($"{hintSafeName}.{EXTENSIONS_FILE_SUFFIX}", GenerateExtensionsSource(component, targets));

        }
    }

    /// <summary>
    /// Whether every declaration of the type - and of each enclosing type, which the generated part has to reopen
    /// too - is <c>partial</c>.
    /// </summary>
    private static bool IsPartial(INamedTypeSymbol component)
    {
        for (INamedTypeSymbol? current = component; current != null; current = current.ContainingType)
        {
            foreach (var reference in current.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is not TypeDeclarationSyntax declaration) return false;
                if (!declaration.Modifiers.Any(SyntaxKind.PartialKeyword)) return false;
            }
        }

        return true;
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
            if (IsComponentAttribute(attr.AttributeClass))
            {
                yield return attr;
            }
        }
    }

    /// <summary>
    /// Whether the attribute is one of the three declaration attributes (<c>SpanAs</c>, <c>SpanTo</c> or the legacy
    /// <c>SpanOperation</c>).
    /// </summary>
    private static bool IsComponentAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        var name = attributeClass.Name;
        return name == SPAN_AS_ATTRIBUTE_NAME
               || name == SPAN_TO_ATTRIBUTE_NAME
               || name == SPAN_AS_ATTRIBUTE_NAME.Replace("Attribute", string.Empty)
               || name == SPAN_TO_ATTRIBUTE_NAME.Replace("Attribute", string.Empty);
    }

    /// <summary>
    /// Reads the bridge a single attribute declares. The legacy <c>SpanOperation</c> attribute maps to
    /// <see cref="Bridge.As"/>.
    /// </summary>
    private static Bridge GetBridge(AttributeData attribute)
        => attribute.AttributeClass?.Name is var name
           && (name == SPAN_TO_ATTRIBUTE_NAME || name == SPAN_TO_ATTRIBUTE_NAME.Replace("Attribute", string.Empty))
            ? Bridge.To
            : Bridge.As;

    /// <summary>
    /// Finds which declaration requested an interface and returns its bridge plus its lane mapping.
    /// </summary>
    private static (Bridge Bridge, string[] Lanes) ResolveDeclaration(
        IReadOnlyList<AttributeData> attributes,
        INamedTypeSymbol requestedInterface)
    {
        foreach (var attribute in attributes)
        {
            if (!ParseRequestedInterfaces(attribute).Any(i => SymbolEqualityComparer.Default.Equals(i, requestedInterface))) continue;

            var bridge = GetBridge(attribute);
            return bridge == Bridge.To
                ? (bridge, GetStringArray(attribute, "Lane"))
                : (bridge, Array.Empty<string>());
        }

        return (Bridge.As, Array.Empty<string>());
    }

    private static string[] GetStringArray(AttributeData attribute, string name)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (!argument.Key.Equals(name, StringComparison.Ordinal)) continue;
            if (argument.Value.Kind != TypedConstantKind.Array) return Array.Empty<string>();

            return argument.Value.Values
                .Select(value => value.Value as string)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray()!;
        }

        return Array.Empty<string>();
    }

    /// <summary>
    /// Validates a <see cref="Bridge.To"/> declaration and produces the synthetic target member: layout equivalence
    /// plus (optional) lane mapping. Reports GUB3009/GUB3010 and returns <see langword="false"/> on failure.
    /// </summary>
    private static bool TryCreateLayoutMember(
        GeneratorExecutionContext context,
        Location location,
        INamedTypeSymbol component,
        ITypeSymbol valueType,
        string[] lanes,
        out ComponentMember member)
    {
        member = default;

        if (!TryGetLayoutSize(component, out var componentSize) || !TryGetLayoutSize(valueType, out var valueSize))
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLayoutMismatch, location, component.ToDisplayString(), valueType.ToDisplayString());
            return false;
        }

        if (componentSize != valueSize)
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLayoutMismatch, location, component.ToDisplayString(), valueType.ToDisplayString());
            return false;
        }

        var fields = GetInstanceFields(component);
        var laneNames = GetLaneNames(valueType);

        // No mapping declared: the value's lanes must line up with the fields as declared.
        if (lanes.Length == 0)
        {
            if (laneNames != null && laneNames.Count != fields.Count)
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"the value has {laneNames.Count} lanes but the component has {fields.Count} fields; declare Lane to map them");
                return false;
            }

            member = new ComponentMember(string.Empty, valueType, canWriteInInitializer: true, isField: false);
            return true;
        }

        if (laneNames == null)
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                "the value type exposes no lanes to map onto");
            return false;
        }

        var mapped = new HashSet<int>();
        foreach (var entry in lanes)
        {
            var parts = entry.Split(':', '=');
            if (parts.Length != 2)
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"entry '{entry}' is not 'Field:Lane'");
                return false;
            }

            var fieldName = parts[0].Trim();
            var laneName = parts[1].Trim();

            var fieldIndex = fields.FindIndex(field => GetFieldName(field).Equals(fieldName, StringComparison.Ordinal));
            if (fieldIndex < 0)
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"'{fieldName}' is not an instance field of the component (fields: {string.Join(", ", fields.Select(GetFieldName))})");
                return false;
            }

            if (!mapped.Add(fieldIndex))
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"'{fieldName}' is mapped more than once");
                return false;
            }

            var laneIndex = int.TryParse(laneName, out var parsed)
                ? parsed
                : laneNames.IndexOf(laneName);

            if (laneIndex < 0 || laneIndex >= laneNames.Count)
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"'{laneName}' is not a lane of the value (lanes: {string.Join(", ", laneNames)})");
                return false;
            }

            if (laneIndex != fieldIndex)
            {
                context.ReportDiagnostic(ErrorDiagnostic.Descriptor.SpanOperationLanesInvalid, location, component.ToDisplayString(), valueType.ToDisplayString(),
                    $"'{fieldName}' is physical field {fieldIndex} but is mapped to lane '{laneName}' ({laneIndex}); reordering is not supported because the bridge is zero-copy");
                return false;
            }
        }

        member = new ComponentMember(string.Empty, valueType, canWriteInInitializer: true, isField: false);
        return true;
    }

    /// <summary>
    /// Display name of an instance field: the property name for a positional/auto-property backing field
    /// (<c>&lt;Y&gt;k__BackingField</c> becomes <c>Y</c>), otherwise the field name.
    /// </summary>
    private static string GetFieldName(IFieldSymbol field)
    {
        const string suffix = ">k__BackingField";
        var name = field.Name;
        return name.Length > suffix.Length + 1
               && name[0] == '<'
               && name.EndsWith(suffix, StringComparison.Ordinal)
            ? name.Substring(1, name.Length - suffix.Length - 1)
            : name;
    }

    private static List<IFieldSymbol> GetInstanceFields(INamedTypeSymbol type)
    {
        var fields = new List<IFieldSymbol>();
        foreach (var member in type.GetMembers())
        {
            if (member is not IFieldSymbol field) continue;
            if (field.IsStatic || field.IsConst) continue;
            fields.Add(field);
        }

        return fields;
    }

    /// <summary>
    /// Lane names of a value type: its instance fields in declaration order (<c>X, Y, Z, W</c> for a vector), or
    /// <see langword="null"/> for primitive values, which have no lanes.
    /// </summary>
    private static List<string>? GetLaneNames(ITypeSymbol valueType)
    {
        if (!TryGetFieldSize(valueType, out _, out _)) return null;

        var fields = GetInstanceFields((INamedTypeSymbol)valueType);
        return fields.Count == 0 ? null : fields.Select(field => field.Name).ToList();
    }

    /// <summary>
    /// Size of a component or value type under the sequential layout rules, rejecting explicit layouts, fixed buffers
    /// and any field whose size is not known.
    /// </summary>
    private static bool TryGetLayoutSize(ITypeSymbol type, out int size)
    {
        size = 0;
        if (type is not INamedTypeSymbol named) return false;
        if (HasExplicitLayout(named)) return false;

        var fields = GetInstanceFields(named);
        if (fields.Count == 0)
        {
            // Fieldless types - primitives above all - still have a known size; scalars are the common SpanTo target
            // for single-value components.
            return TryGetFieldSize(named, out size, out _);
        }

        var offset = 0;
        var sum = 0;
        var maximumAlignment = 1;

        foreach (var field in fields)
        {
            if (field.IsFixedSizeBuffer) return false;
            if (!TryGetFieldSize(field.Type, out var fieldSize, out var alignment)) return false;

            offset = RoundUp(offset, alignment);
            if (offset != sum) return false;

            offset += fieldSize;
            sum += fieldSize;
            maximumAlignment = Math.Max(maximumAlignment, alignment);
        }

        if (offset % maximumAlignment != 0) return false;

        size = offset;
        return true;
    }

    /// <summary>
    /// Size and natural alignment of a known value type. Only primitives and the System.Numerics vectors are known, so
    /// anything else makes the layout unverifiable and therefore unsupported.
    /// </summary>
    private static bool TryGetFieldSize(ITypeSymbol type, out int size, out int alignment)
    {
        size = 0;
        alignment = 1;

        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
                size = 1;
                return true;
            case SpecialType.System_Char:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
                size = 2;
                alignment = 2;
                return true;
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Single:
                size = 4;
                alignment = 4;
                return true;
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Double:
                size = 8;
                alignment = 8;
                return true;
        }

        var metadataName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
        switch (metadataName)
        {
            case "System.Numerics.Vector2":
                size = 8;
                alignment = 4;
                return true;
            case "System.Numerics.Vector3":
                size = 12;
                alignment = 4;
                return true;
            case "System.Numerics.Vector4":
                size = 16;
                alignment = 4;
                return true;
            case "System.IntPtr":
            case "System.UIntPtr":
                size = 8;
                alignment = 8;
                return true;
        }

        return false;
    }

    private static int RoundUp(int value, int alignment) => (value + alignment - 1) / alignment * alignment;
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

    /// <summary>
    /// The generated bridge reinterprets <c>Span&lt;Component&gt;</c> as the member's span with
    /// <c>MemoryMarshal.Cast</c>, which is only sound when the component's size equals the member's size. That holds
    /// for a single-field wrapper whose only instance field is the bound member, and silently stops holding for
    /// anything with extra state, explicit padding or a property that does not alias that field.
    /// </summary>
    private static bool ValidateComponentLayout(
        GeneratorExecutionContext context,
        Location location,
        INamedTypeSymbol component,
        ComponentMember member)
    {
        IFieldSymbol? onlyField = null;
        var instanceFields = 0;
        var hasFixedBuffer = false;

        foreach (var symbol in component.GetMembers())
        {
            if (symbol is not IFieldSymbol field) continue;
            if (field.IsStatic || field.IsConst) continue;

            instanceFields++;
            onlyField = field;
            hasFixedBuffer |= field.IsFixedSizeBuffer;
        }

        var bridgedBySameField = onlyField != null
                                 && (member.IsField
                                     ? onlyField.Name.Equals(member.Name, StringComparison.Ordinal)
                                     : SymbolEqualityComparer.Default.Equals(onlyField.Type, member.Type));

        var sound = !hasFixedBuffer
                    && instanceFields == 1
                    && bridgedBySameField
                    && !HasExplicitLayout(component);

        if (sound)
        {
            return true;
        }

        context.ReportDiagnostic(
            ErrorDiagnostic.Descriptor.SpanOperationComponentLayout,
            location,
            component.ToDisplayString(),
            member.Name);
        return false;
    }

    /// <summary>
    /// Rejects explicit layouts, whose padding or layout kind the cast would run past.
    /// </summary>
    private static bool HasExplicitLayout(INamedTypeSymbol component)
    {
        foreach (var attribute in component.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() != "System.Runtime.InteropServices.StructLayoutAttribute") continue;

            if (attribute.ConstructorArguments.Length > 0
                && attribute.ConstructorArguments[0].Value is int kind
                && kind == EXPLICIT_LAYOUT_KIND)
            {
                return true;
            }

            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == "Size" || argument.Key == "Pack") return true;
            }
        }

        return false;
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
        => IsSpanOperation(symbol) || symbol.AllInterfaces.Any(IsSpanOperation);

    /// <summary>
    /// Matches <c>Gubbins.Span.ISpanOperation&lt;T&gt;</c> by namespace and name so the check does not depend on the
    /// generic arity or on how the symbol happens to be displayed.
    /// </summary>
    private static bool IsSpanOperation(INamedTypeSymbol symbol)
        => symbol.Name == SPAN_OPERATION_INTERFACE_NAME
           && symbol.ContainingNamespace?.ToDisplayString() == SPAN_OPERATION_INTERFACE_NAMESPACE;

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

        return
        [
            .. targets
                .Where(t => !conflictingSurfaces.Contains(ToQualifiedName(t.ProxiedInterface)))
        ];
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
                    canWriteInInitializer: !field.IsReadOnly,
                    isField: true);
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
                    canWriteInInitializer: canSet,
                    isField: false);
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

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Gubbins.Enhance;");
        sb.AppendLine("using Gubbins.Span;");
        sb.AppendLine();

        var hasNamespace = !component.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {component.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
        }

        // Reopen the component, innermost level last, so the adapter lives inside it rather than beside it.
        var chain = GetTypeChain(component);
        var indent = hasNamespace ? "    " : string.Empty;
        foreach (var type in chain)
        {
            sb.AppendLine($"{indent}{GetTypeDeclaration(type)}");
            sb.AppendLine($"{indent}{{");
            indent += "    ";
        }

        sb.AppendLine($"{indent}/// <summary>Generated span operation adapter for <see cref=\"{componentTypeName}\"/>.</summary>");
        sb.AppendLine($"{indent}/// <remarks>");
        sb.AppendLine($"{indent}/// Registered by the component's static constructor, so <see cref=\"SpanOperations\"/> resolves it as soon as");
        sb.AppendLine($"{indent}/// the component type is used. The generated kernels are scalar and field-wise; asking for a SIMD or");
        sb.AppendLine($"{indent}/// parallel mask selects this same implementation.");
        sb.AppendLine($"{indent}/// </remarks>");
        sb.AppendLine($"{indent}[global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"{indent}internal sealed class GeneratedSpanOperations :");
        for (var index = 0; index < targets.Count; index++)
        {
            var suffix = index == targets.Count - 1 ? string.Empty : ",";
            sb.AppendLine($"{indent}    {ToQualifiedName(targets[index].ProxiedInterface)}{suffix}");
        }

        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    public bool Supported => true;");
        sb.AppendLine();

        foreach (var target in targets)
        {
            foreach (var method in GetOperationMethods(target.ProxiedInterface))
            {
                GenerateMethod(sb, indent, component, componentTypeName, target, method);
                sb.AppendLine();
            }
        }

        sb.AppendLine($"{indent}}}");
        sb.AppendLine();

        // Self-registration: a static constructor runs the first time the component type is touched, which is always
        // before a query can hand out spans of it.
        sb.AppendLine($"{indent}static {component.Name}()");
        sb.AppendLine($"{indent}{{");
        sb.AppendLine($"{indent}    var operation = new GeneratedSpanOperations();");
        sb.AppendLine($"{indent}    global::Gubbins.Span.SpanOperations.RegisterSpanOperation<{componentTypeName}>(operation, global::Gubbins.Span.IterationMode.Serial);");
        sb.AppendLine($"{indent}    global::Gubbins.Span.SpanOperations.RegisterSpanOperation<{componentTypeName}>(operation, global::Gubbins.Span.IterationMode.Simd | global::Gubbins.Span.IterationMode.Parallel);");
        sb.AppendLine($"{indent}    global::Gubbins.Span.SpanOperations.RegisterSpanOperation<{componentTypeName}>(operation, global::Gubbins.Span.IterationMode.Simd);");
        sb.AppendLine($"{indent}    global::Gubbins.Span.SpanOperations.RegisterSpanOperation<{componentTypeName}>(operation, global::Gubbins.Span.IterationMode.Parallel);");
        sb.AppendLine($"{indent}}}");

        foreach (var _ in chain)
        {
            indent = indent.Substring(4);
            sb.AppendLine($"{indent}}}");
        }

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>Lists the containing types of <paramref name="type"/>, outermost first, the type itself last.</summary>
    private static List<INamedTypeSymbol> GetTypeChain(INamedTypeSymbol type)
    {
        var chain = new List<INamedTypeSymbol>();
        for (INamedTypeSymbol? current = type; current != null; current = current.ContainingType)
        {
            chain.Insert(0, current);
        }

        return chain;
    }

    /// <summary>
    /// Rebuilds a declaration header that can sit next to the original: partial declaration modifiers have to agree
    /// with the user's declaration, or the compiler rejects the second part.
    /// </summary>
    private static string GetTypeDeclaration(INamedTypeSymbol type)
    {
        var sb = new StringBuilder();

        sb.Append(type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public ",
            Accessibility.Internal => "internal ",
            Accessibility.Private => "private ",
            Accessibility.Protected => "protected ",
            Accessibility.ProtectedOrInternal => "protected internal ",
            Accessibility.ProtectedAndInternal => "private protected ",
            _ => string.Empty
        });

        if (type.IsStatic) sb.Append("static ");
        if (type.TypeKind == TypeKind.Struct && type.IsReadOnly) sb.Append("readonly ");
        if (type.IsRefLikeType) sb.Append("ref ");

        sb.Append("partial ");
        sb.Append(type.TypeKind switch
        {
            TypeKind.Struct => type.IsRecord ? "record struct " : "struct ",
            TypeKind.Class => type.IsRecord ? "record " : "class ",
            TypeKind.Interface => "interface ",
            _ => "class "
        });
        sb.Append(type.Name);

        if (type.TypeParameters.Length > 0)
        {
            sb.Append('<');
            sb.Append(string.Join(", ", type.TypeParameters.Select(parameter => parameter.Name)));
            sb.Append('>');

            foreach (var parameter in type.TypeParameters)
            {
                var constraints = new List<string>();
                if (parameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("unmanaged");
                }
                else if (parameter.HasValueTypeConstraint)
                {
                    constraints.Add("struct");
                }
                else if (parameter.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }

                constraints.AddRange(parameter.ConstraintTypes.Select(ToQualifiedName));

                if (parameter.HasConstructorConstraint && !parameter.HasValueTypeConstraint && !parameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Count > 0)
                {
                    sb.Append($" where {parameter.Name} : {string.Join(", ", constraints)}");
                }
            }
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
            var mappedName = MapParameter(parameter, componentType, componentTypeName, target.Member, target.Bridge, bodyIndent, sb, index);
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
            sb.AppendLine(target.Bridge == Bridge.To
                ? $"{bodyIndent}return global::Gubbins.Span.SpanBridge.FromValue<{componentTypeName}, {ToQualifiedName(target.Member.Type)}>(value);"
                : $"{bodyIndent}return new {componentTypeName} {{ {target.Member.Name} = value }};");
        }
        else
        {
            sb.AppendLine($"{bodyIndent}return {invocation};");
        }

        sb.AppendLine($"{methodIndent}}}");
    }

    /// <summary>
    /// Builds the public extension surface as its own file: the adapter is an implementation detail that nothing
    /// registers by default, while these extensions are the API callers actually use.
    /// </summary>
    private static string GenerateExtensionsSource(INamedTypeSymbol component, IReadOnlyList<InterfaceTarget> targets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Gubbins.Enhance;");
        sb.AppendLine("using Gubbins.Span;");
        sb.AppendLine();

        var hasNamespace = !component.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {component.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
        }

        GenerateExtensions(sb, hasNamespace ? "    " : string.Empty, component, ToQualifiedName(component), targets);

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
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
                .Select((parameter, index) => MapExtensionParameter(parameter, index, spec.OperationMethod, spec.Target.Member.Type, spec.Target.Member.Name, spec.Target.Bridge, componentQualifiedName, spec.UseComponentScalarParameters))
                .ToArray();

            var parameterDeclarations = string.Join(", ", mappedParameters.Select(p => p.Declaration));
            var returnType = spec.AsComponentReturn ? componentQualifiedName : ToQualifiedName(spec.OperationMethod.ReturnType);

            var signatureKey = $"{spec.MethodName}({parameterDeclarations})";
            if (!emittedSet.Add(signatureKey)) return;

            builder.AppendLine($"{methodIndentation}public static {returnType} {spec.MethodName}({parameterDeclarations}, IterationMode mask = IterationMode.Auto)");
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
                builder.AppendLine(spec.Target.Bridge == Bridge.To
                    ? $"{bodyIndentation}return global::Gubbins.Span.SpanBridge.FromValue<{componentQualifiedName}, {ToQualifiedName(spec.Target.Member.Type)}>(value);"
                    : $"{bodyIndentation}return new {componentQualifiedName} {{ {spec.Target.Member.Name} = value }};");
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
                    list = [];
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
                spec.Target.Bridge,
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
        Bridge bridge,
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
                callArgument: bridge == Bridge.To
                    ? $"global::Gubbins.Span.SpanBridge.AsValue<{componentTypeName}, {ToQualifiedName(valueType)}>(ref {parameter.Name})"
                    : $"{parameter.Name}.{componentMemberName}",
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
        Bridge bridge,
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
            // Layout bridging has no member to read: reinterpret the operand, which is exactly one value.
            return bridge == Bridge.To
                ? $"global::Gubbins.Span.SpanBridge.AsValue<{componentTypeName}, {ToQualifiedName(member.Type)}>(ref {parameter.Name})"
                : $"{parameter.Name}.{member.Name}";
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
        List<IMethodSymbol> methods = [.. interfaceType.GetMembers().OfType<IMethodSymbol>()];

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
        public ComponentMember(string name, ITypeSymbol type, bool canWriteInInitializer, bool isField)
        {
            Name = name;
            Type = type;
            CanWriteInInitializer = canWriteInInitializer;
            IsField = isField;
        }

        public string Name { get; }
        public ITypeSymbol Type { get; }
        public bool CanWriteInInitializer { get; }

        /// <summary>
        /// Whether the member is a field rather than a property. The generated bridge can only be proven sound for a
        /// field, because that is the storage the <c>MemoryMarshal.Cast</c> reinterprets.
        /// </summary>
        public bool IsField { get; }
    }

    private readonly struct InterfaceTarget(INamedTypeSymbol requestedInterface, INamedTypeSymbol proxiedInterface, ComponentMember member, IReadOnlyDictionary<string, string> methodRenames, Bridge bridge)
    {
        public INamedTypeSymbol RequestedInterface { get; } = requestedInterface;
        public INamedTypeSymbol ProxiedInterface { get; } = proxiedInterface;
        public ComponentMember Member { get; } = member;
        public IReadOnlyDictionary<string, string> MethodRenames { get; } = methodRenames;
        public Bridge Bridge { get; } = bridge;
    }

    private readonly struct ExtensionParameterMap(string declaration, string callArgument, string prepareLine)
    {
        public string Declaration { get; } = declaration;
        public string CallArgument { get; } = callArgument;
        public string PrepareLine { get; } = prepareLine;
    }

    private sealed class ComponentConfig
    {
        private static readonly IReadOnlyDictionary<string, string> emptyRenames = new Dictionary<string, string>(StringComparer.Ordinal);

        public ComponentConfig(
            List<INamedTypeSymbol> interfaces,
            Dictionary<string, string> memberByOperation,
            Dictionary<string, Dictionary<string, string>> renameByOperation)
        {
            Interfaces = interfaces;
            m_MemberByOperation = memberByOperation;
            m_RenameByOperation = renameByOperation;
        }

        private readonly Dictionary<string, string> m_MemberByOperation;
        private readonly Dictionary<string, Dictionary<string, string>> m_RenameByOperation;

        public List<INamedTypeSymbol> Interfaces { get; }

        public bool TryGetMemberName(INamedTypeSymbol operation, out string memberName)
            => m_MemberByOperation.TryGetValue(ToQualifiedName(operation), out memberName!);

        public IReadOnlyDictionary<string, string> GetMethodRenames(INamedTypeSymbol operation)
            => m_RenameByOperation.TryGetValue(ToQualifiedName(operation), out var map) ? map : emptyRenames;
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

            var hasComponent = symbol.GetAttributes().Any(a => a.AttributeClass?.ContainingNamespace.ToDisplayString() == COMPONENT_ATTRIBUTE_NAMESPACE
                                                               && IsComponentAttribute(a.AttributeClass));
            if (hasComponent)
            {
                Structs.Add(symbol);
            }
        }
    }
}
