using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging.Transport.Http;

[Generator]
public sealed class HttpMessageAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var messageTypesToGenerate = context.SyntaxProvider
                                            .ForAttributeWithMetadataName("Conqueror.HttpMessageAttribute",
                                                                          static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                                                                          GetTypeToGenerate)
                                            .WithTrackingName(TrackingNames.InitialExtraction)
                                            .Where(static m => m is not null) // Filter out errors that we don't care about
                                            .Select(static (m, _) => m!.Value)
                                            .WithTrackingName(TrackingNames.RemovingNulls);

        context.RegisterSourceOutput(messageTypesToGenerate,
                                     static (spc, messageTypeToGenerate) => Execute(in messageTypeToGenerate, spc));
    }

    private static void Execute(in HttpMessageTypeToGenerate messageTypeToGenerate, SourceProductionContext context)
    {
        var (result, filename) = HttpMessageAbstractionsGenerationHelper.GenerateMessageTypes(in messageTypeToGenerate);
        context.AddSource(filename, SourceText.From(result, Encoding.UTF8));
    }

    private static HttpMessageTypeToGenerate? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol messageTypeSymbol)
        {
            // weird, we couldn't get the symbol, ignore it
            return null;
        }

        ct.ThrowIfCancellationRequested();

        // TODO: improve by allowing combining custom interface with properties from attribute
        // skip message types that already declare an interface
        if (messageTypeSymbol.Interfaces.Any(i => i.Name == "IHttpMessage"
                                                  && i.ContainingAssembly.Name == "Conqueror.Transport.Http.Abstractions"))
        {
            return null;
        }

        INamedTypeSymbol? responseTypeSymbol = null;

        foreach (var attributeData in messageTypeSymbol.GetAttributes())
        {
            // TODO: extract into helper and reuse across generators
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

        return GenerateTypeDescriptor(messageTypeSymbol, responseTypeSymbol);
    }

    private static HttpMessageTypeToGenerate GenerateTypeDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                    INamedTypeSymbol? responseTypeSymbol)
    {
        string? httpMethod = null;
        string? pathPrefix = null;
        string? path = null;
        string? fullPath = null;
        string? version = null;
        int? successStatusCode = null;
        string? name = null;
        string? apiGroupName = null;

        foreach (var attributeData in messageTypeSymbol.GetAttributes())
        {
            if (attributeData.AttributeClass?.Name != "HttpMessageAttribute" ||
                attributeData.AttributeClass.ToDisplayString() != "Conqueror.HttpMessageAttribute")
            {
                continue;
            }

            // explicit loop instead of dictionary for performance
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "HttpMethod" && namedArgument.Value.Value?.ToString() is { } m)
                {
                    httpMethod = m;
                    continue;
                }

                if (namedArgument.Key == "PathPrefix" && namedArgument.Value.Value?.ToString() is { } prefix)
                {
                    pathPrefix = prefix;
                    continue;
                }

                if (namedArgument.Key == "Path" && namedArgument.Value.Value?.ToString() is { } p)
                {
                    path = p;
                    continue;
                }

                if (namedArgument.Key == "FullPath" && namedArgument.Value.Value?.ToString() is { } fp)
                {
                    fullPath = fp;
                    continue;
                }

                if (namedArgument.Key == "Version" && namedArgument.Value.Value?.ToString() is { } v)
                {
                    version = v;
                    continue;
                }

                if (namedArgument is { Key: "SuccessStatusCode", Value.Value: int code })
                {
                    successStatusCode = code;
                    continue;
                }

                if (namedArgument.Key == "Name" && namedArgument.Value.Value?.ToString() is { } n)
                {
                    name = n;
                    continue;
                }

                if (namedArgument.Key == "ApiGroupName" && namedArgument.Value.Value?.ToString() is { } groupName)
                {
                    apiGroupName = groupName;
                }
            }
        }

        return new(MessageAbstractionsGeneratorHelper.GenerateMessageTypeToGenerate(messageTypeSymbol, responseTypeSymbol),
                   httpMethod,
                   pathPrefix,
                   path,
                   fullPath,
                   version,
                   successStatusCode,
                   name,
                   apiGroupName);
    }
}
