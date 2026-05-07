// Copyright ©2024 Gatongone
// Author: Gatongone
// Email: gatongone@gmail.com
// Created On: 2024/03/18-01:11:19
// Github: https://github.com/Gatongone

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

[Generator]
public class ModuleRegisterGenerator : ISourceGenerator
{
    private const string DOMAIN_NAMESPACE = "Gubbins.Entities";
    private const string DEFAULT_DOMAIN   = $"{DOMAIN_NAMESPACE}.Domain";
    private const string DOMAIN_INTERFACE = $"{DOMAIN_NAMESPACE}.IDomain";
    private const string FILE_NAME        = "ModuleRegister.g.cs";

    private const string CODE_BODY =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using Gubbins.Domain;

        namespace Gubbins.Generated
        {
            internal static partial class ModuleRegistry
            {
                 /// <summary>
                 /// Auto generated.
                 /// </summary>
                 [global::System.Runtime.CompilerServices.CompilerGenerated]
                 [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
                 private static void {MethodName}()
                 {
                     var category = Category.GetCategory<{CategoryType}>();
                     IModule Module = new {ModuleType}();
                     category.Modules.AddModule(Module);
                 }
            }
        }
        """;

    private const string CODE_BODY_PARTIAL =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using Gubbins.Domain;

        {TypeStart}
            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}private void Awake()
            {Indentation}{
            {Indentation}   Category.GetCategory<{CategoryType}>().Modules.AddModule(this);
            {Indentation}   {Init}
            {Indentation}}

            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}private void OnDestroy()
            {Indentation}{
            {Indentation}   Category.GetCategory<{CategoryType}>().Modules.RemoveModule<{ModuleType}>();
            {Indentation}   {Destroy}
            {Indentation}}
        {TypeEnd}
        """;

    private const string CODE_BODY_PARTIAL_NAMESPACE =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        using Gubbins.Entities;

        namespace {Namespace}
        {
        {TypeStart}
            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}private void Awake()
            {Indentation}{
            {Indentation}   Category.GetCategory<{CategoryType}>().Modules.AddModule(this);
            {Indentation}   {Init}
            {Indentation}}

            {Indentation}/// <summary>
            {Indentation}/// Auto generated.
            {Indentation}/// </summary>
            {Indentation}[global::System.Runtime.CompilerServices.CompilerGenerated]
            {Indentation}private void OnDestroy()
            {Indentation}{
            {Indentation}   Category.GetCategory<{CategoryType}>().Modules.RemoveModule<{ModuleType}>();
            {Indentation}   {Destroy}
            {Indentation}}
        {TypeEnd}
        }
        """;

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver());

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxReceiver) return;
        foreach (var info in syntaxReceiver.Infos)
        {
            if (!ValidateModuleInfo(context, info)) continue;

            var categoryType = info.Category?.ToDisplayString() ?? DEFAULT_DOMAIN;

            string body;
            if (info.ClassType is ClassType.ScriptableObject or ClassType.Component)
            {
                var containsNamespace = !string.IsNullOrEmpty(info.Namespace) && !info.Namespace.Equals("<global namespace>");
                body = CreatePartialBody(categoryType, info, containsNamespace);
            }
            else
            {
                body = CreateRegistryBody(categoryType, info);
            }

            context.AddSource($"{info.FullName}.{FILE_NAME}", body);
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static string CreatePartialBody(string categoryType, ModuleInfo info, bool containsNamespace) => (containsNamespace ? CODE_BODY_PARTIAL_NAMESPACE : CODE_BODY_PARTIAL).Format
    (
        Generator => nameof(ModuleRegisterGenerator),
        CreateTime => DateTime.Now.ToString(CultureInfo.InvariantCulture),
        Namespace => info.Namespace,
        ModuleType => info.FullName,
        CategoryType => categoryType,
        TypeStart => info.NestedTypes.GetTypeStringBegin(containsNamespace ? 0 : -1),
        TypeEnd => info.NestedTypes.GetTypeStringEnd(containsNamespace ? 0 : -1),
        Indentation => new string('\t', containsNamespace ? info.NestedTypes.Length : info.NestedTypes.Length - 1),
        Init => info.ContainsInit
            ? info.IsCoroutineInit ? "StartCoroutine(this.Init());" : "this.Init();"
            : "// You can use the 'Init()' method instead of the 'Awake()' method.",
        Destroy => info.ContainsDispose
            ? info.IsCoroutineInit ? "StartCoroutine(this.Dispose());" : "this.Dispose();"
            : "// You can use the 'Dispose()' method instead of the 'OnDestroy()' method."
    );

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private static string CreateRegistryBody(string categoryType, ModuleInfo info) => CODE_BODY.Format
    (
        Generator => nameof(ModuleRegisterGenerator),
        CreateTime => DateTime.Now.ToString(CultureInfo.InvariantCulture),
        ModuleType => info.FullName,
        CategoryType => categoryType,
        MethodName => $"Register_{info.FullName.Replace('.', '_')}"
    );

    private static bool ValidateModuleInfo(GeneratorExecutionContext context, ModuleInfo info)
    {
        var succeed = true;
        if (!info.IsClass)
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.ModuleIsValueType, info.Location, info.FullName);
            succeed = false;
        }

        if (info.ClassType == ClassType.UnityObject)
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.ModuleBaseTypeInvalid, info.Location);
            succeed = false;
        }

        if (info.Category != null && !info.Category.ContainsInterface(DOMAIN_INTERFACE))
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.CategoryTypeError, info.Category.Locations.First(), info.FullName);
            succeed = false;
        }

        if (info.Category != null && !info.Category.IsNewable())
        {
            context.ReportDiagnostic(ErrorDiagnostic.Descriptor.TypeDoesNotExistPublicEmptyCtor, info.Category.Locations.First(), info.Category.ToDisplayString());
            succeed = false;
        }

        return succeed;
    }

    private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public readonly System.Collections.Generic.List<ModuleInfo> Infos = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (symbol is not ITypeSymbol
            {
                IsAbstract: false,
                IsStatic  : false
            } typeSymbol || !typeSymbol.ContainsInterface($"{DOMAIN_NAMESPACE}.IModule"))
            {
                return;
            }

            var baseType = ClassType.Object;
            if (typeSymbol.ContainsBaseType("UnityEngine.Component"))
            {
                baseType = ClassType.Component;
            }
            else if (typeSymbol.ContainsBaseType("UnityEngine.ScriptableObject"))
            {
                baseType = ClassType.ScriptableObject;
            }
            else if (typeSymbol.ContainsBaseType("UnityEngine.ScriptableObject"))
            {
                baseType = ClassType.UnityObject;
            }

            var containsInit = typeSymbol.TryGetMethod("Init", out var init);
            var isCoroutineInit = init != null && init.ReturnType.ToDisplayString().Equals("Module.Collections.IEnumerator");
            var containsDispose = typeSymbol.TryGetMethod("Dispose", out var dispose);

            var info = new ModuleInfo
            {
                ClassType       = baseType,
                IsClass         = typeSymbol.IsReferenceType,
                FullName        = typeSymbol.ToDisplayString(),
                Location        = typeSymbol.Locations.ItemRef(0),
                Namespace       = typeSymbol.ContainingNamespace.ToDisplayString(),
                NestedTypes     = typeSymbol.GetNestedTypeName(),
                ContainsInit    = containsInit,
                IsCoroutineInit = isCoroutineInit,
                ContainsDispose = containsDispose,
                Category        = GetCategoryInfo(typeSymbol)
            };
            Infos.Add(info);
        }

        private static ITypeSymbol? GetCategoryInfo(ISymbol typeSymbol)
        {
            var attribute = typeSymbol.GetAttributes().FirstOrDefault(MatchCategoryAttribute);

            if (attribute == null) return null;
            return attribute.ConstructorArguments.Length switch
            {
                0 => attribute.AttributeClass!.TypeArguments.FirstOrDefault(),
                1 => attribute.ConstructorArguments[0].Value as ITypeSymbol,
                _ => null
            };

            bool MatchCategoryAttribute(AttributeData att)
            {
                if (att.AttributeClass == null) return false;
                var attributeNamespace = att.AttributeClass.ContainingNamespace.ToDisplayString();
                var attributeName = att.AttributeClass.Name;
                return attributeNamespace.Equals(DOMAIN_NAMESPACE) && (attributeName.Equals("CategoryAttribute") || attributeName.Equals("Category"));
            }
        }
    }

    private struct ModuleInfo
    {
        public (string TypeName, string TypeKind)[] NestedTypes;
        public ClassType                            ClassType;
        public bool                                 IsClass;
        public bool                                 ContainsInit;
        public bool                                 IsCoroutineInit;
        public bool                                 ContainsDispose;
        public string                               Namespace;
        public string                               FullName;
        public ITypeSymbol?                         Category;
        public Location                             Location;
    }

    private enum ClassType
    {
        Object,
        UnityObject,
        Component,
        ScriptableObject
    }
}