using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VerifyTests;

namespace Conqueror.SourceGenerators.Tests;

public static class TestSources
{
    public static IEnumerable<TestCaseData> GenerateTestCases(string module)
    {
        foreach (var testCaseName in GetTestCaseNames(module))
        {
            var sourceCode = LoadTestFile(module, testCaseName);
            var cutOffIndex = sourceCode.IndexOf("// make the compiler happy during design time", StringComparison.Ordinal);

            if (cutOffIndex > 0)
            {
                sourceCode = sourceCode[..cutOffIndex];
            }

            yield return new(new SourceGenerationTestCase(testCaseName,
                                                          sourceCode,
                                                          sourceCode.Contains("ExpectedDiagnostics", StringComparison.Ordinal)))
            {
                TestName = testCaseName,
            };
        }
    }

    public static VerifySettings CreateVerifySettings(string testCaseName)
    {
        var settings = new VerifySettings();
        settings.ScrubLinesWithReplace(line => line.ReplaceGeneratorVersion());
        settings.UseDirectory($"TestCases/{testCaseName}");
        settings.UseFileName("snapshot");
        settings.DisableRequireUniquePrefix();
        settings.DisableDiff();

        return settings;
    }

    private static string LoadTestFile(string module, string testCaseName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var fullPrefix = $"{assembly.GetName().Name}.{module}.TestCases";
        var resourceName = $"{fullPrefix}.{testCaseName}.source.cs";

        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> GetTestCaseNames(string module)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = $"{assembly.GetName().Name}.{module}.TestCases.";
        var extension = ".cs";

        return assembly.GetManifestResourceNames()
                       .Where(name => name.StartsWith(prefix) && name.EndsWith(extension))
                       .Select(name => name.Replace(prefix, string.Empty).Replace(".source.cs", string.Empty));
    }

    public sealed record SourceGenerationTestCase(
        string Name,
        string SourceCode,
        bool HasExpectedDiagnostics);
}
