using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System;
using System.Linq;
using System.Collections.Generic;

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
                                                                             DiagnosticSeverity.Warning,
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
            var fieldSymbol = (IFieldSymbol)context.Symbol;

            if (fieldSymbol.IsStatic && !fieldSymbol.IsConst)
            {
                if (fieldSymbol.IsReadOnly)
                {
                    if (!TypeIsImmutableWhenReadOnly(fieldSymbol.Type))
                    {
                        // The field is read only but the underlying type isn't, so fail
                        context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First(), context.Symbol.ToDisplayString()));
                    }
                }
                else
                {
                    // The field isn't read only, so most definitely not
                    context.ReportDiagnostic(Diagnostic.Create(Rule, context.Symbol.Locations.First(), context.Symbol.ToDisplayString()));
                }
            }
        }

        private static readonly ImmutableHashSet<string> TypesThatAreImmutableWhenReadOnly;

        static DoNotUseSharedMutableStateInTests()
        {
            var builder = ImmutableHashSet<string>.Empty.ToBuilder();

            builder.Add("System.Int32");
            builder.Add("System.Lazy`1");
            builder.Add("System.TimeSpan");
            builder.Add("System.String");
            builder.Add("System.Collections.Immutable.ImmutableArray");
            builder.Add("System.Text.RegularExpressions.Regex");

            // A ComposableCatalog is just the metadata of discovered types. It is immutable.
            // This is distinct from the ExportProvider which actually holds onto instances of the
            // created parts.
            builder.Add("Microsoft.VisualStudio.Composition.ComposableCatalog");

            TypesThatAreImmutableWhenReadOnly = builder.ToImmutableHashSet();
        }

        private static bool TypeIsImmutableWhenReadOnly(ITypeSymbol type)
        {
            if (!TypesThatAreImmutableWhenReadOnly.Contains(type.ContainingNamespace.MetadataName + "." + type.MetadataName))
            {
                return false;
            }

            var namedTypeSymbol = type as INamedTypeSymbol;

            if (!namedTypeSymbol.ConstructedFrom.Equals(namedTypeSymbol))
            {
                // The type is constructed, so we must check that it's constructed with immutable things too
                return namedTypeSymbol.TypeParameters.All(TypeIsImmutableWhenReadOnly);
            }

            return true;
        }
    }
}
