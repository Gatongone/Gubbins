using System;
using System.Globalization;
using Microsoft.CodeAnalysis;

namespace Gubbins.Generator;

[Generator]
public class DomainRecycleGenerator : ISourceGenerator
{
    private const string FILE_NAME = "CategoryRecycler.g.cs";

    private const string CODE_BODY =
        """
        // Generator: {Generator}
        // Created On: {CreateTime}

        #if UNITY_EDITOR
        using Gubbins.Entities;

        namespace Gubbins.Generated
        {
            internal partial class CategoryRecycler
            {
                /// <summary>
                /// Auto generated.
                /// </summary>
                [UnityEditor.InitializeOnLoadMethod]
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                private static void On{DisplayName}Enter()
                {
                    UnityEditor.EditorApplication.playModeStateChanged += {DisplayName}_OnStateChanged;
                }

                /// <summary>
                /// Auto generated.
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGenerated]
                private static void {DisplayName}_OnStateChanged(UnityEditor.PlayModeStateChange _)
                {
                    if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
                    {
                        Domain.GetDomain<{FullName}>().Reset();
                    }
                }
            }
        }
        #endif
        """;

    public void Initialize(GeneratorInitializationContext context) => context.RegisterForSyntaxNotifications(static () => new SyntaxContextReceiver());

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver syntaxReceiver) return;
        foreach (var info in syntaxReceiver.Infos)
        {
            if (!info.IsNewable)  continue;

            // ReSharper disable InconsistentNaming
            var body = CODE_BODY.Format
            (
                Generator   => nameof(DomainRecycleGenerator),
                CreateTime  => DateTime.Now.ToString(CultureInfo.InvariantCulture),
                FullName    => info.FullName,
                DisplayName => info.DisplayName
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
            } typeSymbol || !typeSymbol.ContainsBaseType($"{nameof(Gubbins)}.Entities.Category"))
            {
                return;
            }

            var info = new FeatureInfo
            {
                IsNewable = typeSymbol.IsNewable(),
                FullName  = typeSymbol.ToDisplayString(),
                DisplayName  = typeSymbol.ToDisplayString().Replace(".", "_")
            };
            Infos.Add(info);
        }
    }

    private struct FeatureInfo
    {
        public bool IsNewable;
        public string FullName;
        public string DisplayName;
    }
}