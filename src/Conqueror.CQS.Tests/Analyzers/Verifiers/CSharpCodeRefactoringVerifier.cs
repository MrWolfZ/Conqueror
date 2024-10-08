using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Conqueror.CQS.Tests.Analyzers.Verifiers;

[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "this is based on analyzer template")]
public static class CSharpCodeRefactoringVerifier<TCodeRefactoring>
    where TCodeRefactoring : CodeRefactoringProvider, new()
{
    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring,TTest,TVerifier}.VerifyRefactoringAsync(string, string)" />
    public static async Task VerifyRefactoringAsync(string source, string fixedSource)
    {
        await VerifyRefactoringAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult, string)" />
    public static async Task VerifyRefactoringAsync(string source, DiagnosticResult expected, string fixedSource)
    {
        await VerifyRefactoringAsync(source, [expected], fixedSource);
    }

    /// <inheritdoc cref="CodeRefactoringVerifier{TCodeRefactoring, TTest, TVerifier}.VerifyRefactoringAsync(string, DiagnosticResult[], string)" />
    public static async Task VerifyRefactoringAsync(string source, DiagnosticResult[] expected, string fixedSource)
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
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = CSharpVerifierHelper.Net80ReferenceAssemblies,
        };

        test.ExpectedDiagnostics.AddRange(expected);
        await test.RunAsync(CancellationToken.None);
    }

    private sealed class Test : CSharpCodeRefactoringTest<TCodeRefactoring, MSTestVerifier>
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
