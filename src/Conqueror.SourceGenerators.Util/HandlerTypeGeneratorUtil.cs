using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Conqueror.SourceGenerators.Util;

public static class HandlerTypeGeneratorUtil
{
    public static void InitializeGeneratorForHandlerTypes<TDescriptor>(
        this IncrementalGeneratorInitializationContext context,
        Func<GeneratorSyntaxContext, CancellationToken, TDescriptor?> descriptorFactory,
        Func<TDescriptor, (string FileName, string Content)> sourceFactory)
        where TDescriptor : struct
    {
        var descriptors = context.SyntaxProvider
                                 .CreateSyntaxProvider(static (n, _) =>
                                                       {
                                                           var baseList = (n as ClassDeclarationSyntax)?.BaseList ?? (n as RecordDeclarationSyntax)?.BaseList;
                                                           return baseList?.Types.Count > 0 && baseList.ToString().Contains(".IHandler");
                                                       },
                                                       descriptorFactory)
                                 .WithTrackingName(DefaultTrackingNames.InitialExtraction)
                                 .Where(static m => m is not null) // Filter out errors that we don't care about
                                 .Select(static (m, _) => m!.Value)
                                 .WithTrackingName(DefaultTrackingNames.RemovingNulls);

        context.RegisterSourceOutput(descriptors, (spc, descriptor) =>
        {
            var (result, filename) = sourceFactory(descriptor);
            spc.AddSource(filename, SourceText.From(result, Encoding.UTF8));
        });
    }
}
