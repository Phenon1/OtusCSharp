using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace GenerateBinarySerializer
{
    internal static class GenerateBinarySerializerSyntaxProvider
    {
        private const string nameAttribute1 = "GenerateBinarySerializer";
        private const string nameAttribute2 = "GenerateBinarySerializerAttribute";

        internal static IncrementalValuesProvider<INamedTypeSymbol> GetSyntaxProvider(IncrementalGeneratorInitializationContext context)
        {
            return context.SyntaxProvider
           .CreateSyntaxProvider(
               predicate: (node, _) => IsTargetSyntax(node),
               transform: (ctx, _) => GetSemanticTargetForGeneration(ctx))
           .Where(target => target != null);

        }

        static bool HasGenerateSerializerAttribute(INamedTypeSymbol typeSymbol)
        {
            return typeSymbol?.GetAttributes()
                .Any(ad =>
                    ad.AttributeClass?.Name == nameAttribute2 ||
                    ad.AttributeClass?.ToDisplayString() == "OtusCSharpModels.GenerateBinarySerializer.GenerateBinarySerializerAttribute")
                ?? false;
        }

        static INamedTypeSymbol GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;

            if (HasGenerateSerializerAttribute(classSymbol))
            {
                return classSymbol;
            }
            return null;
        }

        static bool IsTargetSyntax(SyntaxNode node)
        {
            if (!(node is ClassDeclarationSyntax classDecl))
                return false;

            if (classDecl.AttributeLists.Count == 0)
                return false;

            foreach (var attributeList in classDecl.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    string name = attribute.Name.ToString();

                    if (name == nameAttribute1 || name == nameAttribute2)
                        return true;
                }
            }

            return false;

        }

    }
}
