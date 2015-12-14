using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System;
using System.Linq;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseSharedMutableStateInTests : DiagnosticAnalyzer
    {
        internal const string RuleId = "RS0026";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotUseSharedMutableStateInTestsTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotUseSharedMutableStateInTestsMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             DiagnosticCategory.Reliability,
                                                                             DiagnosticSeverity.Error,
                                                                             isEnabledByDefault: true,
                                                                             helpLinkUri: null,     // TODO: add MSDN url
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(c =>
            {
                if (c.Compilation.AssemblyName.StartsWith("Roslyn.Services"))
                {
                    c.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.Field);
                }
            });
        }

        private void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            if (context.Symbol.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First(), context.Symbol.ToDisplayString()));
            }
        }
    }
}
