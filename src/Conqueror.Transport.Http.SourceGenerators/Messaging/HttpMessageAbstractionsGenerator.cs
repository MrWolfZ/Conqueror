using System.Linq;
using System.Threading;
using Conqueror.SourceGenerators.Util;
using Conqueror.SourceGenerators.Util.Messaging;
using Microsoft.CodeAnalysis;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.Transport.Http.SourceGenerators.Messaging;

[Generator]
public sealed class HttpMessageAbstractionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.InitializeCoreMessageAbstractionsGenerator("Conqueror.HttpMessageAttribute");

        context.InitializeGeneratorForAttribute("Conqueror.HttpMessageAttribute",
                                                CreateTypeDescriptor,
                                                HttpMessageAbstractionsSources.GenerateHttpMessageTypes);

        context.InitializeGeneratorForAttribute("Conqueror.HttpMessageAttribute`1",
                                                CreateTypeDescriptor,
                                                HttpMessageAbstractionsSources.GenerateHttpMessageTypes);
    }

    private static HttpMessageTypesDescriptor? CreateTypeDescriptor(GeneratorAttributeSyntaxContext context, CancellationToken ct)
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

        ITypeSymbol? responseTypeSymbol = null;

        var attributeData = context.Attributes.Single();
        var attributeClass = attributeData.AttributeClass;

        if (attributeClass is null)
        {
            // weird, this should never happen, but better be safe and ignore it
            return null;
        }

        if (attributeClass.TypeArguments.Length > 0)
        {
            responseTypeSymbol = attributeClass.TypeArguments[0];
        }

        return GenerateTypeDescriptor(messageTypeSymbol, responseTypeSymbol, attributeData, context.SemanticModel);
    }

    private static HttpMessageTypesDescriptor GenerateTypeDescriptor(INamedTypeSymbol messageTypeSymbol,
                                                                     ITypeSymbol? responseTypeSymbol,
                                                                     AttributeData attributeData,
                                                                     SemanticModel semanticModel)
    {
        string? httpMethod = null;
        string? pathPrefix = null;
        string? path = null;
        string? fullPath = null;
        string? version = null;
        int? successStatusCode = null;
        string? name = null;
        string? apiGroupName = null;

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

        return new(MessageAbstractionsGeneratorHelper.GenerateMessageTypesDescriptor(messageTypeSymbol, responseTypeSymbol, semanticModel),
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
