using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Gubbins.Generator;

/// <summary>
/// Generates the <c>Gubbins.Entities</c> surface for a component marked with <c>[SpanAs]</c> or <c>[SpanTo]</c>:
/// extension methods on <c>Components&lt;TComponent&gt;</c> that apply an operation in place across every chunk of a
/// query, with the chunk-level serial/parallel dispatch that <c>ComponentsParallelExtensions</c> provides.
/// </summary>
/// <remarks>
/// <para>
/// This is a separate generator on purpose: it emits a separate source file and only runs when the compilation
/// actually references <c>Gubbins.Entities</c>, so a project that only takes <c>Gubbins.Span</c> never sees ECS types in
/// its generated output. Keeping it in its own file also means it can move to its own analyzer assembly without
/// touching <c>SpanOperationsGenerator</c>.
/// </para>
/// <para>
/// The discovery code below is intentionally self-contained rather than shared with <c>SpanOperationsGenerator</c>:
/// analyzer assemblies cannot reference each other after a split, so a shared helper would have to be duplicated at
/// that point anyway. The rules that must stay in sync are the attribute shape, the <c>SpanAs</c> single-field
/// requirement and the <c>SpanTo</c> layout/lane rules.
/// </para>
/// <para>
/// This generator reports no diagnostics. <c>SpanOperationsGenerator</c> validates the same components and owns the
/// diagnostics; reporting here as well would duplicate every error.
/// </para>
/// </remarks>
[Generator]
public sealed class ComponentsOperationsGenerator : ISourceGenerator
{
    private const string COMPONENTS_FILE_SUFFIX = "ComponentsOperations.g.cs";
    private const string COMPONENT_ATTRIBUTE_NAMESPACE = "Gubbins.Span";
    private const string SPAN_AS_ATTRIBUTE_NAME = "SpanAsAttribute";
    private const string SPAN_TO_ATTRIBUTE_NAME = "SpanToAttribute";
    private const string SPAN_OPERATION_INTERFACE_NAMESPACE = "Gubbins.Span";
    private const string SPAN_OPERATION_INTERFACE_NAME = "ISpanOperation";
    private const string COMPONENTS_TYPE_METADATA_NAME = "Gubbins.Entities.ChunkLayout`1";

    /// <summary>
    /// <c>System.Runtime.InteropServices.LayoutKind.Explicit</c> (Auto = 0, Sequential = 1, Explicit = 2).
    /// </summary>
    private const int EXPLICIT_LAYOUT_KIND = 2;

    /// <summary>
    /// How a component's storage relates to the operation's value type.
    /// </summary>
    private enum Bridge
    {
        /// <summary>The component holds a member of the value type; only that member is operated on.</summary>
        As,

        /// <summary>The component's whole layout equals one value; every field is operated on.</summary>
        To
    }

    /// <summary>
    /// Which operand shape an emitted overload takes.
    /// </summary>
    private enum OperandKind
    {
        /// <summary>The operation takes no operand.</summary>
        None,

        /// <summary>The operand is the component itself, bridged to the value type.</summary>
        Component,

        /// <summary>The operand is the value type directly.</summary>
        Value
    }

    public void Initialize(GeneratorInitializationContext context)
        => context.RegisterForSyntaxNotifications(() => new ComponentsStructReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not ComponentsStructReceiver receiver) return;
        if (context.Compilation.GetTypeByMetadataName(COMPONENTS_TYPE_METADATA_NAME) == null) return;

        var processed = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var component in receiver.Structs)
        {
            if (!processed.Add(component)) continue;

            var targets = BuildTargets(component);
            if (targets.Count == 0) continue;

            var hintName = $"{GetHintSafeName(component)}.{COMPONENTS_FILE_SUFFIX}";
            context.AddSource(hintName, GenerateSource(component, targets));
        }
    }

    // ------------------------------------------------------------------
    // Target discovery
    // ------------------------------------------------------------------

    private static List<Target> BuildTargets(INamedTypeSymbol component)
    {
        var targets = new List<Target>();

        foreach (var attribute in GetComponentAttributes(component))
        {
            var bridge = GetBridge(attribute);
            var renames = GetRenames(attribute);
            var memberOverride = GetNamedString(attribute, "Member");
            var lanes = bridge == Bridge.To ? GetStringArray(attribute, "Lane") : Array.Empty<string>();

            foreach (var requestedInterface in ParseRequestedInterfaces(attribute))
            {
                if (requestedInterface.TypeKind != TypeKind.Interface) continue;
                if (!IsSpanOperationInterface(requestedInterface)) continue;
                if (!requestedInterface.IsGenericType || requestedInterface.TypeArguments.Length == 0) continue;

                var valueType = requestedInterface.TypeArguments[0];

                Member member;
                if (bridge == Bridge.To)
                {
                    if (!TryCreateLayoutMember(component, valueType, lanes, out member)) continue;
                }
                else
                {
                    var members = GetBindableMembers(component, valueType, memberOverride).ToArray();
                    if (members.Length != 1) continue;

                    member = members[0];
                    if (!IsBridgableLayout(component, member)) continue;
                }

                targets.Add(new Target(requestedInterface, member, bridge, renames));
            }
        }

        return targets;
    }

    // ------------------------------------------------------------------
    // Emission
    // ------------------------------------------------------------------

    private static string GenerateSource(INamedTypeSymbol component, IReadOnlyList<Target> targets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Runtime.InteropServices;");
        sb.AppendLine("using Gubbins.Span;");
        sb.AppendLine();

        var hasNamespace = !component.ContainingNamespace.IsGlobalNamespace;
        if (hasNamespace)
        {
            sb.AppendLine($"namespace {component.ContainingNamespace.ToDisplayString()}");
            sb.AppendLine("{");
        }

        GenerateClass(sb, hasNamespace ? "    " : string.Empty, component, targets);

        if (hasNamespace)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Emits the extension class. Only "single-column in place" methods are emitted: a void method whose span
    /// parameters are all <c>Span&lt;V&gt;</c> of the bridged value type, at most two of them, with the second one being
    /// the final parameter. Methods that need a second column (for example <c>Max(left, right, result)</c>) or a
    /// reduction (<c>GetMax</c>) cannot be expressed on a single component column and are skipped.
    /// </summary>
    private static void GenerateClass(StringBuilder sb, string outerIndent, INamedTypeSymbol component, IReadOnlyList<Target> targets)
    {
        var componentTypeName = ToQualifiedName(component);
        var className = $"{GetHintSafeName(component)}ComponentsOperations";
        var methodIndent = outerIndent + "    ";
        var bodyIndent = methodIndent + "    ";

        var emittedSignatures = new HashSet<string>(StringComparer.Ordinal);
        var stateTypes = new Dictionary<string, (string KernelType, string ValueType)>(StringComparer.Ordinal);

        sb.AppendLine($"{outerIndent}[global::System.Runtime.CompilerServices.CompilerGenerated]");
        sb.AppendLine($"{outerIndent}public static class {className}");
        sb.AppendLine($"{outerIndent}{{");
        sb.AppendLine($"{methodIndent}private static global::Gubbins.Span.IterationMode ResolveKernelMode(global::Gubbins.Span.IterationMode mask)");
        sb.AppendLine($"{methodIndent}    => (mask & global::Gubbins.Span.IterationMode.Simd) != 0");
        sb.AppendLine($"{methodIndent}        ? global::Gubbins.Span.IterationMode.Simd");
        sb.AppendLine($"{methodIndent}        : global::Gubbins.Span.IterationMode.Serial;");
        sb.AppendLine();

        foreach (var target in targets)
        {
            var valueTypeName = ToQualifiedName(target.Member.Type);
            var kernelTypeName = ToQualifiedName(target.RequestedInterface);

            foreach (var method in GetOperationMethods(target.RequestedInterface))
            {
                if (!TryDescribeSurface(method, target.Member.Type, out var layout)) continue;

                var methodName = ResolveMethodName(target, method);
                var stateName = $"{target.RequestedInterface.Name}{method.Name}State";

                // The parallel path always allocates the state, so register it regardless of the operand shape.
                stateTypes[stateName] = (kernelTypeName, valueTypeName);

                if (layout.ScalarIndexes.Length > 0)
                {
                    Emit(OperandKind.Component);
                    if (layout.ScalarIndexes.Length == 1) Emit(OperandKind.Value);
                }
                else
                {
                    Emit(OperandKind.None);
                }

                void Emit(OperandKind operandKind)
                {
                    var operandType = operandKind switch
                    {
                        OperandKind.Component => componentTypeName,
                        OperandKind.Value => valueTypeName,
                        _ => null
                    };

                    if (!emittedSignatures.Add($"{methodName}({operandType ?? "none"})")) return;

                    var operandDeclaration = operandType == null ? string.Empty : $"{operandType} operand, ";
                    var operandValue = operandKind switch
                    {
                        OperandKind.Value => "operand",
                        OperandKind.Component => target.Bridge == Bridge.To
                            ? $"global::Gubbins.Span.SpanBridge.AsValue<{componentTypeName}, {valueTypeName}>(ref operand)"
                            : $"operand.{target.Member.Name}",
                        _ => null
                    };

                    var directArguments = BuildArguments(method, layout, "view", operandValue);
                    var stateArguments = BuildArguments(method, layout, "view", "state.Operand");
                    var stateValue = operandValue ?? $"default({valueTypeName})";

                    sb.AppendLine($"{methodIndent}/// <summary>");
                    sb.AppendLine($"{methodIndent}/// Applies <c>{method.Name}</c> in place to every element of the component column.");
                    sb.AppendLine($"{methodIndent}/// </summary>");
                    sb.AppendLine($"{methodIndent}public static void {methodName}(");
                    sb.AppendLine($"{methodIndent}    this global::Gubbins.Entities.ChunkLayout<{componentTypeName}> components,");
                    sb.AppendLine($"{methodIndent}    global::Gubbins.Span.IterationMode mask,");
                    sb.AppendLine($"{methodIndent}    {operandDeclaration}int workerCount = 0)");
                    sb.AppendLine($"{methodIndent}{{");
                    sb.AppendLine($"{bodyIndent}var kernel = global::Gubbins.Span.SpanOperations.GetOperation<{valueTypeName}, {kernelTypeName}>(ResolveKernelMode(mask));");
                    sb.AppendLine($"{bodyIndent}if (kernel is null) return;");
                    sb.AppendLine();
                    sb.AppendLine($"{bodyIndent}if ((mask & global::Gubbins.Span.IterationMode.Parallel) == 0)");
                    sb.AppendLine($"{bodyIndent}{{");
                    sb.AppendLine($"{bodyIndent}    var segments = components.Segments;");
                    sb.AppendLine($"{bodyIndent}    for (var i = 0; i < segments.Count; i++)");
                    sb.AppendLine($"{bodyIndent}    {{");
                    sb.AppendLine($"{bodyIndent}        var view = MemoryMarshal.Cast<{componentTypeName}, {valueTypeName}>(segments[i]);");
                    sb.AppendLine($"{bodyIndent}        kernel.{method.Name}({directArguments});");
                    sb.AppendLine($"{bodyIndent}    }}");
                    sb.AppendLine();
                    sb.AppendLine($"{bodyIndent}    return;");
                    sb.AppendLine($"{bodyIndent}}}");
                    sb.AppendLine();
                    sb.AppendLine($"{bodyIndent}global::Gubbins.Entities.ComponentsParallelExtensions.ApplyParallel(");
                    sb.AppendLine($"{bodyIndent}    components,");
                    sb.AppendLine($"{bodyIndent}    new {stateName}(kernel, {stateValue}),");
                    sb.AppendLine($"{bodyIndent}    static (span, state) =>");
                    sb.AppendLine($"{bodyIndent}    {{");
                    sb.AppendLine($"{bodyIndent}        var view = MemoryMarshal.Cast<{componentTypeName}, {valueTypeName}>(span);");
                    sb.AppendLine($"{bodyIndent}        state.Kernel.{method.Name}({stateArguments});");
                    sb.AppendLine($"{bodyIndent}    }},");
                    sb.AppendLine($"{bodyIndent}    workerCount);");
                    sb.AppendLine($"{methodIndent}}}");
                    sb.AppendLine();
                }
            }
        }

        foreach (var entry in stateTypes)
        {
            sb.AppendLine($"{methodIndent}private readonly struct {entry.Key}");
            sb.AppendLine($"{methodIndent}{{");
            sb.AppendLine($"{methodIndent}    public readonly {entry.Value.KernelType} Kernel;");
            sb.AppendLine($"{methodIndent}    public readonly {entry.Value.ValueType} Operand;");
            sb.AppendLine();
            sb.AppendLine($"{methodIndent}    public {entry.Key}({entry.Value.KernelType} kernel, {entry.Value.ValueType} operand)");
            sb.AppendLine($"{methodIndent}    {{");
            sb.AppendLine($"{methodIndent}        Kernel  = kernel;");
            sb.AppendLine($"{methodIndent}        Operand = operand;");
            sb.AppendLine($"{methodIndent}    }}");
            sb.AppendLine($"{methodIndent}}}");
            sb.AppendLine();
        }

        sb.AppendLine($"{outerIndent}}}");
        sb.AppendLine();
    }

    /// <summary>
    /// Describes the parameter layout of a method that can be applied in place to a single component column.
    /// </summary>
    private readonly struct ColumnLayout(int[] columnIndexes, int[] scalarIndexes)
    {
        public int[] ColumnIndexes { get; } = columnIndexes;
        public int[] ScalarIndexes { get; } = scalarIndexes;
    }

    private static bool TryDescribeSurface(IMethodSymbol method, ITypeSymbol valueType, out ColumnLayout layout)
    {
        layout = default;

        if (!method.ReturnsVoid) return false;

        var columns = new List<int>();
        var scalars = new List<int>();

        for (var index = 0; index < method.Parameters.Length; index++)
        {
            var parameter = method.Parameters[index];
            if (parameter.RefKind != RefKind.None) return false;

            if (TryGetSpanElementType(parameter.Type, out var elementType))
            {
                if (!SymbolEqualityComparer.Default.Equals(elementType, valueType)) return false;
                if ((parameter.Type as INamedTypeSymbol)?.Name != "Span") return false;
                columns.Add(index);
                continue;
            }

            if (SymbolEqualityComparer.Default.Equals(parameter.Type, valueType))
            {
                scalars.Add(index);
                continue;
            }

            return false;
        }

        if (columns.Count is 0 or > 2) return false;
        if (columns.Count == 2 && columns[1] != method.Parameters.Length - 1) return false;

        layout = new ColumnLayout(columns.ToArray(), scalars.ToArray());
        return true;
    }

    private static string BuildArguments(IMethodSymbol method, ColumnLayout layout, string viewExpression, string? scalarExpression)
    {
        var arguments = new string[method.Parameters.Length];
        foreach (var index in layout.ColumnIndexes) arguments[index] = viewExpression;
        foreach (var index in layout.ScalarIndexes) arguments[index] = scalarExpression ?? viewExpression;
        return string.Join(", ", arguments);
    }

    private static string ResolveMethodName(Target target, IMethodSymbol method)
        => target.Renames.TryGetValue(method.Name, out var renamed) && !string.IsNullOrWhiteSpace(renamed)
            ? renamed
            : method.Name;

    // ------------------------------------------------------------------
    // Discovery (kept in sync with SpanOperationsGenerator by contract)
    // ------------------------------------------------------------------

    private static IEnumerable<AttributeData> GetComponentAttributes(INamedTypeSymbol component)
    {
        foreach (var attribute in component.GetAttributes())
        {
            if (attribute.AttributeClass?.ContainingNamespace.ToDisplayString() != COMPONENT_ATTRIBUTE_NAMESPACE) continue;
            if (IsComponentAttribute(attribute.AttributeClass)) yield return attribute;
        }
    }

    private static bool IsComponentAttribute(INamedTypeSymbol? attributeClass)
    {
        if (attributeClass == null) return false;

        var name = attributeClass.Name;
        return name == SPAN_AS_ATTRIBUTE_NAME
               || name == SPAN_TO_ATTRIBUTE_NAME
               || name == SPAN_AS_ATTRIBUTE_NAME.Replace("Attribute", string.Empty)
               || name == SPAN_TO_ATTRIBUTE_NAME.Replace("Attribute", string.Empty);
    }

    private static Bridge GetBridge(AttributeData attribute)
        => attribute.AttributeClass?.Name is var name
           && (name == SPAN_TO_ATTRIBUTE_NAME || name == SPAN_TO_ATTRIBUTE_NAME.Replace("Attribute", string.Empty))
            ? Bridge.To
            : Bridge.As;

    private static IEnumerable<INamedTypeSymbol> ParseRequestedInterfaces(AttributeData attribute)
    {
        var result = new List<INamedTypeSymbol>();

        foreach (var argument in attribute.ConstructorArguments)
        {
            CollectTypeSymbols(argument, result);
        }

        return result;
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

    private static Dictionary<string, string> GetRenames(AttributeData attribute)
    {
        var renames = new Dictionary<string, string>(StringComparer.Ordinal);
        var method = GetNamedString(attribute, "Method");
        var overwrite = GetNamedString(attribute, "Overwrite");
        if (!string.IsNullOrWhiteSpace(method) && !string.IsNullOrWhiteSpace(overwrite))
        {
            renames[method!] = overwrite!;
        }

        return renames;
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

    private static IEnumerable<Member> GetBindableMembers(INamedTypeSymbol component, ITypeSymbol valueType, string? memberOverride)
    {
        foreach (var member in component.GetMembers())
        {
            if (member is IFieldSymbol field)
            {
                if (field.IsStatic || field.IsConst) continue;
                if (!string.IsNullOrWhiteSpace(memberOverride) && !field.Name.Equals(memberOverride, StringComparison.Ordinal)) continue;
                if (!SymbolEqualityComparer.Default.Equals(field.Type, valueType)) continue;
                if (!IsReadable(field.DeclaredAccessibility)) continue;

                yield return new Member(field.Name, field.Type, isField: true);
            }

            if (member is IPropertySymbol property)
            {
                if (property.IsStatic) continue;
                if (!string.IsNullOrWhiteSpace(memberOverride) && !property.Name.Equals(memberOverride, StringComparison.Ordinal)) continue;
                if (!SymbolEqualityComparer.Default.Equals(property.Type, valueType)) continue;
                if (property.GetMethod == null || !IsReadable(property.GetMethod.DeclaredAccessibility)) continue;

                yield return new Member(property.Name, property.Type, isField: false);
            }
        }
    }

    private static bool IsReadable(Accessibility accessibility)
        => accessibility == Accessibility.Public
           || accessibility == Accessibility.Internal
           || accessibility == Accessibility.ProtectedOrInternal;

    private static bool IsSpanOperationInterface(INamedTypeSymbol symbol)
        => IsSpanOperation(symbol) || symbol.AllInterfaces.Any(IsSpanOperation);

    private static bool IsSpanOperation(INamedTypeSymbol symbol)
        => symbol.Name == SPAN_OPERATION_INTERFACE_NAME
           && symbol.ContainingNamespace?.ToDisplayString() == SPAN_OPERATION_INTERFACE_NAMESPACE;

    /// <summary>
    /// <c>SpanAs</c>: the component is a single-field wrapper whose only instance field is the bridged member, which is
    /// the only shape whose size and layout the cast can prove.
    /// </summary>
    private static bool IsBridgableLayout(INamedTypeSymbol component, Member member)
    {
        IFieldSymbol? onlyField = null;
        var instanceFields = 0;

        foreach (var symbol in component.GetMembers())
        {
            if (symbol is not IFieldSymbol field) continue;
            if (field.IsStatic || field.IsConst) continue;

            instanceFields++;
            onlyField = field;
            if (field.IsFixedSizeBuffer) return false;
        }

        if (instanceFields != 1 || onlyField == null) return false;

        var bridgedBySameField = member.IsField
            ? onlyField.Name.Equals(member.Name, StringComparison.Ordinal)
            : SymbolEqualityComparer.Default.Equals(onlyField.Type, member.Type);

        return bridgedBySameField && !HasExplicitLayout(component);
    }

    /// <summary>
    /// <c>SpanTo</c>: the component's sequential layout must be exactly one value, with no padding, and the optional
    /// <c>Lane</c> entries must be positionally consistent.
    /// </summary>
    private static bool TryCreateLayoutMember(INamedTypeSymbol component, ITypeSymbol valueType, string[] lanes, out Member member)
    {
        member = default;

        if (!TryGetLayoutSize(component, out var componentSize) || !TryGetLayoutSize(valueType, out var valueSize)) return false;
        if (componentSize != valueSize) return false;

        var fields = GetInstanceFields(component);
        var laneNames = GetLaneNames(valueType);

        if (lanes.Length == 0)
        {
            if (laneNames != null && laneNames.Count != fields.Count) return false;
            member = new Member(string.Empty, valueType, isField: false);
            return true;
        }

        if (laneNames == null) return false;

        var mapped = new HashSet<int>();
        foreach (var entry in lanes)
        {
            var parts = entry.Split(':', '=');
            if (parts.Length != 2) return false;

            var fieldName = parts[0].Trim();
            var laneName = parts[1].Trim();

            var fieldIndex = fields.FindIndex(field => GetFieldName(field).Equals(fieldName, StringComparison.Ordinal));
            if (fieldIndex < 0 || !mapped.Add(fieldIndex)) return false;

            var laneIndex = int.TryParse(laneName, out var parsed) ? parsed : laneNames.IndexOf(laneName);
            if (laneIndex < 0 || laneIndex >= laneNames.Count || laneIndex != fieldIndex) return false;
        }

        member = new Member(string.Empty, valueType, isField: false);
        return true;
    }

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
    /// Display name of an instance field: the property name for a positional/auto-property backing field, otherwise the
    /// field name.
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

    private static List<string>? GetLaneNames(ITypeSymbol valueType)
    {
        if (!TryGetFieldSize(valueType, out _, out _)) return null;

        var fields = GetInstanceFields((INamedTypeSymbol)valueType);
        return fields.Count == 0 ? null : fields.Select(GetFieldName).ToList();
    }

    private static bool TryGetLayoutSize(ITypeSymbol type, out int size)
    {
        size = 0;
        if (type is not INamedTypeSymbol named) return false;
        if (HasExplicitLayout(named)) return false;

        var fields = GetInstanceFields(named);
        if (fields.Count == 0) return false;

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

    private static string ToQualifiedName(ITypeSymbol symbol)
        => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string GetHintSafeName(INamedTypeSymbol symbol)
    {
        var value = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var chars = value.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        return new string(chars).Trim('_');
    }

    private readonly struct Member(string name, ITypeSymbol type, bool isField)
    {
        public string Name { get; } = name;
        public ITypeSymbol Type { get; } = type;
        public bool IsField { get; } = isField;
    }

    private readonly struct Target(
        INamedTypeSymbol requestedInterface,
        Member member,
        Bridge bridge,
        IReadOnlyDictionary<string, string> renames)
    {
        public INamedTypeSymbol RequestedInterface { get; } = requestedInterface;
        public Member Member { get; } = member;
        public Bridge Bridge { get; } = bridge;
        public IReadOnlyDictionary<string, string> Renames { get; } = renames;
    }

    private sealed class ComponentsStructReceiver : ISyntaxContextReceiver
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

            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ContainingNamespace.ToDisplayString() != COMPONENT_ATTRIBUTE_NAMESPACE) continue;
                if (!IsComponentAttribute(attribute.AttributeClass)) continue;

                Structs.Add(symbol);
                return;
            }
        }
    }
}
