using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Conqueror.CQS.Tests.Analyzers.Verifiers;

public static class CSharpVerifierHelper
{
    private static readonly Lazy<ReferenceAssemblies> LazyNet80 = new(() =>
                                                                          new(
                                                                              "net8.0",
                                                                              new(
                                                                                  "Microsoft.NETCore.App.Ref",
                                                                                  "8.0.4"),
                                                                              Path.Combine("ref", "net8.0")));

    private static readonly Regex LeadingAndTrailingNewlinesRegex = new(@"(^\r?\n|\r?\n\s*$)");
    private static readonly Regex LeadingWhitespaceRegex = new(@"^(\s*)");

    /// <summary>
    ///     By default, the compiler reports diagnostics for nullable reference types at
    ///     <see cref="DiagnosticSeverity.Warning" />, and the analyzer test framework defaults to only validating
    ///     diagnostics at <see cref="DiagnosticSeverity.Error" />. This map contains all compiler diagnostic IDs
    ///     related to nullability mapped to <see cref="ReportDiagnostic.Error" />, which is then used to enable all
    ///     of these warnings for default validation during analyzer and code fix tests.
    /// </summary>
    public static ImmutableDictionary<string, ReportDiagnostic> NullableWarnings { get; } = GetNullableWarningsFromCompiler();

    public static ReferenceAssemblies Net80ReferenceAssemblies => LazyNet80.Value;

    public static string Dedent(this string s)
    {
        var withoutLeadingOrTrailingNewlines = LeadingAndTrailingNewlinesRegex.Replace(s, string.Empty);
        var indent = LeadingWhitespaceRegex.Match(withoutLeadingOrTrailingNewlines).Groups[0].Value.Length;
        return Regex.Replace(withoutLeadingOrTrailingNewlines, $"^[^\\S\\r\\n]{{{indent}}}", string.Empty, RegexOptions.Multiline);
    }

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        string[] args = ["/warnaserror:nullable"];
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, Environment.CurrentDirectory, Environment.CurrentDirectory);
        var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

        // Workaround for https://github.com/dotnet/roslyn/issues/41610
        nullableWarnings = nullableWarnings
                           .SetItem("CS8632", ReportDiagnostic.Error)
                           .SetItem("CS8669", ReportDiagnostic.Error);

        return nullableWarnings;
    }
}
