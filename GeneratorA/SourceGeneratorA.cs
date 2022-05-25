using GeneratorB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Text;

namespace GeneratorA;

[Generator]
public class SourceGeneratorA : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiverA());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var sources = new List<SourceText>();

        if (context.SyntaxContextReceiver is SyntaxReceiverA receiver)
        {
            foreach (var candidate in receiver.Candidates)
            {
                var source = Generate(candidate);
                sources.Add(source);

                context.AddSource($"{candidate.Name}.a.g.cs", source);
            }
        }

        var compilation = context.Compilation.RemoveAllSyntaxTrees();

        foreach (var source in sources)
        {
            var tree = CSharpSyntaxTree.ParseText(source);
            compilation = compilation.AddSyntaxTrees(tree);
        }

        var driver = CSharpGeneratorDriver.Create(new SourceGeneratorB());
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var result = driver.GetRunResult();

        foreach (var item in result.Results)
        {
            foreach (var source in item.GeneratedSources)
            {
                context.AddSource(source.HintName, source.SourceText);
            }
        }
    }

    private SourceText Generate(INamedTypeSymbol symbol)
    {
        var code = $@"
            using GeneratorB;

            namespace {symbol.ContainingNamespace.ToDisplayString()};

            [GenerateB]
            internal class {symbol.Name}A
            {{
                public static int A => 1;
            }}
        ";

        return SourceText.From(code, Encoding.UTF8);
    }
}

internal class SyntaxReceiverA : ISyntaxContextReceiver
{
    private INamedTypeSymbol _attributeSymbol;

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        _attributeSymbol ??= context.SemanticModel.Compilation.GetTypeByMetadataName(typeof(GenerateAAttribute).FullName);

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