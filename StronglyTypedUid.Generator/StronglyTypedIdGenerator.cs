using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using StronglyTypedUid.Generator;

namespace StronglyTypedUid.Generator
{
    [Generator]
    public class StronglyTypedUidGenerator : IIncrementalGenerator
    {
        private static readonly string StronglyTypedUidAttribute = "StronglyTypedUid.StronglyTypedUidAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
//#if DEBUG
//            if (!Debugger.IsAttached) Debugger.Launch();
//#endif
            IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s.IsSyntaxTargetForGeneration(),
                    transform: static (ctx, _) => ctx.GetSemanticTargetForGeneration(StronglyTypedUidAttribute))
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax>)> compilationAndEnums
                = context.CompilationProvider.Combine(typeDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndEnums,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<TypeDeclarationSyntax> type, SourceProductionContext context)
        {
            if (type.IsDefaultOrEmpty) return;

            var stronglyTypes = GetStronglyTypedUids(compilation, type.Distinct(), context);

            if (stronglyTypes.Any())
            {
                foreach (var stronglyTyped in stronglyTypes)
                {
                    var generator = new StronglyTypedUidWriter(stronglyTyped);
                    context.AddSource(stronglyTyped.GetFileNameGenerated(),
                                      SourceText.From(generator.GetCode(), Encoding.UTF8));
                }
            }
        }

        protected static List<Metadata> GetStronglyTypedUids(Compilation compilation,
            IEnumerable<TypeDeclarationSyntax> types, SourceProductionContext context)
        {
            var stronglyTypeds = new List<Metadata>();
            foreach (var type in types)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                SemanticModel semanticModel = compilation.GetSemanticModel(type.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(type) is not INamedTypeSymbol typeSymbol)
                {
                    // report diagnostic, something went wrong
                    continue;
                }

                var typelist = new List<string>();
                bool allowNulls = false;
                bool asUlid = false;
                string modifiers = type.GetModifiers();

                if (!modifiers.Contains("partial") || !modifiers.Contains("readonly") || !type.IsKind(SyntaxKind.RecordStructDeclaration))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(DiagnosticDescriptors.StructNotPartial, null, typeSymbol.ToString())
                    );
                    continue;
                }

                foreach (var attribute in typeSymbol.GetAttributes())
                {
                    if (attribute.AttributeClass!.ToDisplayString().Equals(StronglyTypedUidAttribute, StringComparison.OrdinalIgnoreCase))
                    {
                        if (attribute.ConstructorArguments.Any())
                        {
                            var argument = attribute.ConstructorArguments.First();
                            if (bool.TryParse(argument.Value!.ToString(), out bool ulid)) asUlid = ulid;
                        }
                    }
                }
                stronglyTypeds.Add(
                    new Metadata(type.GetNamespace(),
                                        type.GetUsings(),
                                        allowNulls,
                                        typeSymbol.Name,
                                        typeSymbol.GetNameTyped(),
                                        typeSymbol.ToString(),
                                        modifiers,
                                        asUlid));
            }
            return stronglyTypeds;
        }

    }
}
