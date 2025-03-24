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
                                            .ForAttributeWithMetadataName("Conqueror.MessageAttribute",
                                                                          static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                                                                          (ctx, ct) => GetTypeToGenerate(ctx, processedTypes, ct))
                                            .WithTrackingName(TrackingNames.InitialExtraction)
                                            .Where(static m => m is not null) // Filter out errors that we don't care about
                                            .Select(static (m, _) => m!.Value)
                                            .WithTrackingName(TrackingNames.RemovingNulls);

        var messageTypesToGenerateFromGeneric = context.SyntaxProvider
                                                       .ForAttributeWithMetadataName("Conqueror.MessageAttribute`1",
                                                                                     static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                                                                                     (ctx, ct) => GetTypeToGenerate(ctx, processedTypes, ct))
                                                       .WithTrackingName(TrackingNames.InitialExtraction)
                                                       .Where(static m => m is not null) // Filter out errors that we don't care about
                                                       .Select(static (m, _) => m!.Value)
                                                       .WithTrackingName(TrackingNames.RemovingNulls);

        context.RegisterSourceOutput(messageTypesToGenerate,
                                     static (spc, messageTypeToGenerate) => Execute(in messageTypeToGenerate, spc));

        context.RegisterSourceOutput(messageTypesToGenerateFromGeneric,
                                     static (spc, messageTypeToGenerate) => Execute(in messageTypeToGenerate, spc));
    }

    private static void Execute(in MessageTypeToGenerate messageTypeToGenerate, SourceProductionContext context)
    {
        var (result, filename) = MessageAbstractionsGenerationHelper.GenerateMessageTypes(in messageTypeToGenerate);
        context.AddSource(filename, SourceText.From(result, Encoding.UTF8));
    }

    private static MessageTypeToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context,
                                                            HashSet<INamedTypeSymbol> processedTypes,
                                                            CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol messageTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        // skip message types that already declare a handler member
        // TODO: improve with adding explicit diagnostic and also detecting pipeline
        if (messageTypeSymbol.MemberNames.Contains("IHandler"))
        {
            return null;
        }

        // ensure that we process a type only once, even if it has multiple partial declarations
        if (!processedTypes.Add(messageTypeSymbol))
        {
            return null;
        }

        INamedTypeSymbol? responseTypeSymbol = null;

        foreach (var attributeData in messageTypeSymbol.GetAttributes())
        {
            // TODO: allow for transport attributes as well
            // TODO: error if multiple attributes with conflicting response types are found
            if (attributeData.AttributeClass is { Name: "MessageAttribute" } c
                && c.ContainingAssembly.Name == "Conqueror.Abstractions")
            {
                if (c.TypeArguments.Length > 0)
                {
                    responseTypeSymbol = c.TypeArguments[0] as INamedTypeSymbol;
                }

                continue;
            }

            if (attributeData.AttributeClass is { Name: "HttpMessageAttribute" } c2
                && c2.ContainingAssembly.Name == "Conqueror.Transport.Http.Abstractions")
            {
                continue;
            }

            // if no attribute matches, we skip this type
            return null;
        }

        ct.ThrowIfCancellationRequested();

        return MessageAbstractionsGeneratorHelper.GenerateMessageTypeToGenerate(messageTypeSymbol, responseTypeSymbol);
    }
}
