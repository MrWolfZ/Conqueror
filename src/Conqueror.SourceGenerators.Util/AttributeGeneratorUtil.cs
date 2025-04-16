using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable S3267 // for performance reasons we do not want to use LINQ

namespace Conqueror.SourceGenerators.Util;

public static class AttributeGeneratorUtil
{
    public static void InitializeGeneratorForAttribute<TDescriptor>(
        this IncrementalGeneratorInitializationContext context,
        string fullyQualifiedMetadataName,
        Func<GeneratorAttributeSyntaxContext, CancellationToken, TDescriptor?> descriptorFactory,
        Func<TDescriptor, (string FileName, string Content)> sourceFactory)
        where TDescriptor : struct
    {
        var messageTypesToGenerate = context.SyntaxProvider
                                            .ForAttributeWithMetadataName(fullyQualifiedMetadataName,
                                                                          static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax,
                                                                          descriptorFactory)
                                            .WithTrackingName(DefaultTrackingNames.InitialExtraction)
                                            .Where(static m => m is not null) // Filter out errors that we don't care about
                                            .Select(static (m, _) => m!.Value)
                                            .WithTrackingName(DefaultTrackingNames.RemovingNulls);

        context.RegisterSourceOutput(messageTypesToGenerate, (spc, descriptor) =>
        {
            var (result, filename) = sourceFactory(descriptor);
            spc.AddSource(filename, SourceText.From(result, Encoding.UTF8));
        });
    }
}
