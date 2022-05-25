using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Text;

namespace GeneratorB;

[Generator]
public class SourceGeneratorB : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiverB());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is SyntaxReceiverB receiver)
        {
            foreach (var candidate in receiver.Candidates)
            {
                var source = Generate(candidate);

                context.AddSource($"{candidate.Name}.b.g.cs", source);
            }
        }
    }

    private SourceText Generate(INamedTypeSymbol symbol)
    {
        var code = $@"
            namespace {symbol.ContainingNamespace.ToDisplayString()};

            internal class {symbol.Name}B
            {{
                public static int B => 2;
            }}
        ";

        return SourceText.From(code, Encoding.UTF8);
    }
}

internal class SyntaxReceiverB : ISyntaxContextReceiver
{
    private INamedTypeSymbol _attributeSymbol;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        _attributeSymbol ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(GenerateBAttribute).FullName);

        if (context.Node is ClassDeclarationSyntax &&
            context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol symbol)
        {
            foreach (var attribute in symbol.GetAttributes())
            {
                if (attribute.AttributeClass.Equals(_attributeSymbol, SymbolEqualityComparer.Default))
                {
                    Candidates.Add(symbol);
                }
            }
        }
    }

    public IList<INamedTypeSymbol> Candidates { get; } = new List<INamedTypeSymbol>();
}