using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging;

[Generator]
public sealed class MessageAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var processedTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var messageTypesToGenerate = context.SyntaxProvider
                                            .CreateSyntaxProvider(
                                                //// in the first quick filter pass, select classes and records with base types
                                                static (s, _) => s is ClassDeclarationSyntax { BaseList.Types.Count: > 0 } or RecordDeclarationSyntax { BaseList.Types.Count: > 0 },
                                                (ctx, ct) => GetTypeToGenerate(ctx, processedTypes, ct)) // select classes with one of our message interfaces and extract details
                                            .WithTrackingName(TrackingNames.InitialExtraction)
                                            .Where(static m => m is not null) // Filter out errors that we don't care about
                                            .Select(static (m, _) => m!.Value)
                                            .WithTrackingName(TrackingNames.RemovingNulls);

        context.RegisterSourceOutput(messageTypesToGenerate,
                                     static (spc, messageTypeToGenerate) => Execute(in messageTypeToGenerate, spc));
    }

    private static void Execute(in MessageTypeToGenerate messageTypeToGenerate, SourceProductionContext context)
    {
        var (result, filename) = MessageAbstractionsGenerationHelper.GenerateMessageTypes(in messageTypeToGenerate);
        context.AddSource(filename, SourceText.From(result, Encoding.UTF8));
    }

    private static MessageTypeToGenerate? GetTypeToGenerate(GeneratorSyntaxContext context, HashSet<INamedTypeSymbol> processedTypes, CancellationToken ct)
    {
        var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node);

        if (symbol is not INamedTypeSymbol namedSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types that already declare a handler member
        // TODO: improve with adding explicit diagnostic and also detecting pipeline
        if (namedSymbol.MemberNames.Contains("IHandler"))
        {
            return null;
        }

        // ensure that we process a type only once, even if it has multiple partial declarations
        if (!processedTypes.Add(namedSymbol))
        {
            return null;
        }

        var baseListSyntax = (context.Node as TypeDeclarationSyntax)?.BaseList;
        INamedTypeSymbol? interfaceSymbol = null;

        foreach (var baseTypeSyntax in baseListSyntax?.Types ?? [])
        {
            if (baseTypeSyntax.Type is GenericNameSyntax { Identifier.Text: "IMessage" } or IdentifierNameSyntax { Identifier.Text: "IMessage" }
                && context.SemanticModel.GetSymbolInfo(baseTypeSyntax.Type).Symbol is INamedTypeSymbol s
                && s.ContainingAssembly.Name == "Conqueror.Abstractions")
            {
                interfaceSymbol = s;
            }
        }

        if (interfaceSymbol is null)
        {
            // no base type was one of our IMessage interfaces, so we skip this type
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return MessageAbstractionsGeneratorHelper.GenerateMessageTypeToGenerate(namedSymbol, interfaceSymbol);
    }
}
