using System.Linq;
using System.Threading;
using Conqueror.SourceGenerators.Util;
using Conqueror.SourceGenerators.Util.Messaging;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Messaging.Transport.Http;

[Generator]
public sealed class HttpMessageAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeCoreMessageAbstractionsGenerator("Conqueror.HttpMessageAttribute");

        context.InitializeGeneratorForAttribute("Conqueror.HttpMessageAttribute",
                                                GetTypeToGenerate,
                                                HttpMessageAbstractionsGenerationHelper.GenerateMessageTypes);

        context.InitializeGeneratorForAttribute("Conqueror.HttpMessageAttribute`1",
                                                GetTypeToGenerate,
                                                HttpMessageAbstractionsGenerationHelper.GenerateMessageTypes);
    }

    private static HttpMessageTypesDescriptor? GetTypeToGenerate(GeneratorAttributeSyntaxContext context, CancellationToken ct)
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

    private static HttpMessageTypesDescriptor GenerateTypeDescriptor(INamedTypeSymbol messageTypeSymbol,
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
