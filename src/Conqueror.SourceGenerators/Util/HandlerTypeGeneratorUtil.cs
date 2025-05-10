using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
                                 .CreateSyntaxProvider(
                                     static (n, _) =>
                                     {
                                         var baseList = (n as ClassDeclarationSyntax)?.BaseList ?? (n as RecordDeclarationSyntax)?.BaseList;

                                         return baseList?.Types.Count > 0 && baseList.ToString().Contains(".IHandler");
                                     },
                                     Transform)
                                 .WithTrackingName(DefaultTrackingNames.InitialExtraction)
                                 .Where(static d => d.Descriptor is not null || d.Diagnostics.Count > 0) // Filter out errors that we don't care about
                                 .WithTrackingName(DefaultTrackingNames.RemovingNulls);

        context.RegisterSourceOutput(
            descriptors,
            (spc, descriptor) =>
            {
                foreach (var diag in descriptor.Diagnostics)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(diag.Diagnostic, diag.Location?.ToLocation()));
                }

                if (descriptor.Descriptor is IHasDiagnostics d)
                {
                    foreach (var diag in d.Diagnostics)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(diag.Diagnostic, diag.Location?.ToLocation()));
                    }
                }

                if (descriptor.Descriptor is null)
                {
                    return;
                }

                var (result, filename) = sourceFactory(descriptor.Descriptor.Value);
                spc.AddSource(filename, SourceText.From(result, Encoding.UTF8));
            });

        [SuppressMessage(
            "MicrosoftCodeAnalysisCorrectness",
            "RS1035:Do not use APIs banned for analyzers",
            Justification = "safe to use environment for this")]
        DescriptorWithDiagnostics<TDescriptor> Transform(GeneratorSyntaxContext ctx, CancellationToken ct)
        {
            try
            {
                return new(descriptorFactory(ctx, ct), new([]));
            }
            catch (Exception ex)
            {
                var diagnostics = new List<DiagnosticWithLocationDescriptor>();

                var diag = new DiagnosticDescriptor(
                    id: "CONQSG0001",
                    title: "Exception in source generator",
                    messageFormat: $"Exception: {ex.Message}{ex.StackTrace.Replace(Environment.NewLine, string.Empty)}",
                    category: "SourceGenerator",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                diagnostics.Add(new(diag, LocationDescriptor.CreateFrom(ctx.Node.GetLocation())));

                return new(null, new([..diagnostics]));
            }
        }
    }
}
