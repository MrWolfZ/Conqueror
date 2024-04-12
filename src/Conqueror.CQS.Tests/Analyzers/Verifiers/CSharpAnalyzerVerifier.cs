using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Conqueror.CQS.Tests.Analyzers.Verifiers;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "this is based on analyzer template")]
public static class CSharpAnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic()" />
    public static DiagnosticResult Diagnostic()
        => CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>.Diagnostic();

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(string)" />
    public static DiagnosticResult Diagnostic(string diagnosticId)
        => CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>.Diagnostic(diagnosticId);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.Diagnostic(DiagnosticDescriptor)" />
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
        => CSharpAnalyzerVerifier<TAnalyzer, MSTestVerifier>.Diagnostic(descriptor);

    /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, TVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])" />
    public static async Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            TestState =
            {
                AdditionalReferences =
                {
                    MetadataReference.CreateFromFile(typeof(ICommandHandler).Assembly.Location),
                },
            },
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = source,
            ReferenceAssemblies = CSharpVerifierHelper.Net80ReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    public static async Task VerifyAnalyzerWithoutConquerorReferenceAsync(string source, params DiagnosticResult[] expected)
    {
        var test = new Test
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = source,
            ReferenceAssemblies = CSharpVerifierHelper.Net80ReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    private sealed class Test : CSharpAnalyzerTest<TAnalyzer, MSTestVerifier>
    {
        public Test()
        {
            SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId)!;

                var compilationOptions = project.CompilationOptions!.WithSpecificDiagnosticOptions(
                    project.CompilationOptions!.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));

                return solution.WithProjectCompilationOptions(projectId, compilationOptions);
            });
        }
    }
}
