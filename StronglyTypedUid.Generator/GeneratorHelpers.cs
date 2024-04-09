using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace StronglyTypedUid.Generator
{
    public static class GeneratorHelpers
    {
        public static string GetNamespace(this BaseTypeDeclarationSyntax syntax)
        {
            var result = string.Empty;
            SyntaxNode? potentialNamespaceParent = syntax.Parent;

            while (potentialNamespaceParent != null &&
                    potentialNamespaceParent is not NamespaceDeclarationSyntax
                    && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                result = namespaceParent.Name.ToString();

                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    result = $"{namespaceParent.Name}.{result}";
                    namespaceParent = parent;
                }
            }

            return result;
        }

        public static string GetModifiers(this BaseTypeDeclarationSyntax syntax)
        {
            return syntax.Modifiers.ToString();
        }

        public static string GetNameTyped(this INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Any())
            {
                return $"{symbol.Name}<" + string.Join(", ", symbol.TypeArguments.ToList().ConvertAll(x => x.ToString())) + ">";
            }
            else
            {
                return symbol.Name;
            }
        }

        public static IReadOnlyList<string> GetUsings(this BaseTypeDeclarationSyntax syntax)
        {
            SyntaxNode? parent = syntax.Parent;
            while (parent != null)
            {
                if (parent is CompilationUnitSyntax compilationUnit)
                {
                    return compilationUnit.Usings.ToList().ConvertAll(x => x.ToFullString());
                }
                parent = parent.Parent;
            }
            return [];
        }

        public static bool IsSyntaxTargetForGeneration(this SyntaxNode node)
            => node is TypeDeclarationSyntax m && m.AttributeLists.Count > 0;

        public static TypeDeclarationSyntax? GetSemanticTargetForGeneration(this GeneratorSyntaxContext context, string attribute)
        {
            var typeDeclarationSyntax = (TypeDeclarationSyntax)context.Node;

            foreach (AttributeListSyntax attributeListSyntax in typeDeclarationSyntax.AttributeLists)
            {
                foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    INamedTypeSymbol attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    string fullName = attributeContainingTypeSymbol.ToDisplayString();

                    if (fullName == attribute)
                    {
                        return typeDeclarationSyntax;
                    }
                }
            }
            return null;
        }

        public static string GetFileNameGenerated(this Metadata metadata)
            => $"{metadata.FullName.Replace('<', '_').Replace('>', '_')}.g.cs";
    }
}
