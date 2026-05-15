using System;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

[Generator]
public class FeatureRegisterGenerator : ISourceGenerator
{
    private const string FILE_NAME = "FeatureRegister.g.cs";

    private const string CODE_BODY =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        namespace Gubbins.Generated
        {
            internal static partial class FeatureRegistry
            {
                 /// <summary>
                 /// Auto generated.
                 /// </summary>
                 [global::System.Runtime.CompilerServices.CompilerGenerated]
                 [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
                 private static void {MethodName}()
                 {
                    Gubbins.Entities.FeatureManager.AddFeature(new {FeatureType}());
                 }
            }
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
            if (!info.IsNewable)
            {
                var diagnostic = Diagnostic.Create(
                    ErrorDiagnostic.Descriptor.TypeDoesNotExistPublicEmptyCtor,
                    info.Location,
                    info.FullName);
                context.ReportDiagnostic(diagnostic);
                continue;
            }

            // ReSharper disable InconsistentNaming
            var body = CODE_BODY.Format
            (
                Generator   => nameof(FeatureRegisterGenerator),
                CreateTime  => DateTime.Now.ToString(CultureInfo.InvariantCulture),
                FeatureType => info.FullName,
                MethodName  => $"Register_{info.FullName.Replace('.', '_')}"
            );
            context.AddSource($"{info.FullName}.{FILE_NAME}", body);
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        public readonly System.Collections.Generic.List<FeatureInfo> Infos = [];

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);
            if (symbol is not ITypeSymbol
            {
                IsAbstract: false,
                IsStatic  : false
            } typeSymbol || !typeSymbol.ContainsInterface($"{nameof(Gubbins)}.Entities.IFeature"))
            {
                return;
            }

            var info = new FeatureInfo
            {
                IsNewable = typeSymbol.IsNewable(),
                FullName  = typeSymbol.ToDisplayString(),
                Location  = typeSymbol.Locations.ItemRef(0)
            };
            Infos.Add(info);
        }
    }

    private struct FeatureInfo
    {
        public bool IsNewable;
        public string FullName;
        public Location Location;
    }
}