using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Conqueror.SourceGenerators.Util;

public static class AttributeGeneratorUtil
{
    public static void InitializeGeneratorForAttribute<TDescriptor>(
        this IncrementalGeneratorInitializationContext context,
        string simpleAttributeNameSuffix, // e.g. "Message", "Signal", etc.
        Func<GeneratorSyntaxContext, CancellationToken, TDescriptor?> descriptorFactory,
        Func<TDescriptor, (string FileName, string Content)> sourceFactory)
        where TDescriptor : struct
    {
        var descriptors = context.SyntaxProvider
                                 .CreateSyntaxProvider((n, _) => HasAttribute(n), descriptorFactory)
                                 .WithTrackingName(DefaultTrackingNames.InitialExtraction)
                                 .Where(static m => m is not null) // Filter out errors that we don't care about
                                 .Select(static (m, _) => m!.Value)
                                 .WithTrackingName(DefaultTrackingNames.RemovingNulls);

        context.RegisterSourceOutput(descriptors, (spc, descriptor) =>
        {
            var (result, filename) = sourceFactory(descriptor);
            spc.AddSource(filename, SourceText.From(result, Encoding.UTF8));
        });

        [SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions", Justification = "performance reasons")]
        bool HasAttribute(SyntaxNode node)
        {
            var attributeLists = (node as ClassDeclarationSyntax)?.AttributeLists ?? (node as RecordDeclarationSyntax)?.AttributeLists;

            if (attributeLists is null)
            {
                return false;
            }

            foreach (var attributeList in attributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name is SimpleNameSyntax ns && ns.Identifier.Text.EndsWith(simpleAttributeNameSuffix))
                    {
                        return true;
                    }

                    if (attribute.Name.ToString().EndsWith(simpleAttributeNameSuffix))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
