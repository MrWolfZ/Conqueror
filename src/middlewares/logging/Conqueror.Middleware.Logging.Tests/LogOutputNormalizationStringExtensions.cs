namespace Conqueror.Middleware.Logging.Tests;

using System.Text.RegularExpressions;

internal static partial class LogOutputNormalizationStringExtensions
{
    public static string NormalizeLogOutput(this string logOutput) =>
        NormalizeLineNumbersInExceptions(NormalizeLineEndingsInJsonOutput(NormalizePathSeparators(logOutput)));

    private static string NormalizeLineEndingsInJsonOutput(string logOutput)
    {
        return logOutput
            .Replace(@"\r\n", @"\n", StringComparison.Ordinal)
            .Replace(@"\r", @"\n", StringComparison.Ordinal)
            // on non-unix systems the stack trace ends with a newline inside JSON output
            // so we strip that for consistency
            .Replace(@"\n""", @"""", StringComparison.Ordinal);
    }

    private static string NormalizePathSeparators(string logOutput)
    {
        // in JSON output, backslashes are escaped, so we first unescape them
        // before running further replacement logic
        logOutput = logOutput.Replace(@"\\", @"\", StringComparison.Ordinal);

        // exception stack traces contain file paths that may be absolute paths, and
        // to ensure that the log output is stable regardless of the execution env
        // we strip any reference to the solution directory so that any paths are
        // relative
        var solutionDir = FindSolutionDir(Directory.GetCurrentDirectory());

        if (solutionDir is not null)
        {
            logOutput = logOutput.Replace(
                solutionDir + Path.DirectorySeparatorChar,
                "{SolutionDirectory}",
                StringComparison.Ordinal
            );
        }

        return logOutput
            .Replace(@"src\", "src/", StringComparison.Ordinal)
            .Replace(@"Conqueror\", "Conqueror/", StringComparison.Ordinal)
            .Replace(@"Messaging\", "Messaging/", StringComparison.Ordinal)
            .Replace(@"MessageTypeGenerator\", "MessageTypeGenerator/", StringComparison.Ordinal)
            .Replace(@"Signalling\", "Signalling/", StringComparison.Ordinal)
            .Replace(@"SignalTypeGenerator\", "SignalTypeGenerator/", StringComparison.Ordinal)
            .Replace(@"Iterator\", "Iterator/", StringComparison.Ordinal)
            .Replace(@"IteratorTypeGenerator\", "IteratorTypeGenerator/", StringComparison.Ordinal)
            .Replace(@"middlewares\", "middlewares/", StringComparison.Ordinal)
            .Replace(@"Logging\", "Logging/", StringComparison.Ordinal)
            .Replace(@"logging\", "logging/", StringComparison.Ordinal)
            .Replace(@"Tests\", "Tests/", StringComparison.Ordinal)
            // on unix platforms the stack trace contains references to {ProjectDirectory} instead
            // of the solution dir for this test project, so we have to normalize those as well when
            // running on non-unix systems
            .Replace(
                "{SolutionDirectory}src/middlewares/logging/Conqueror.Middleware.Logging.Tests/",
                "{ProjectDirectory}",
                StringComparison.Ordinal
            )
            .Replace(
                @"obj\Debug\net8.0\Conqueror.SourceGenerators\",
                "obj/Debug/net8.0/Conqueror.SourceGenerators/",
                StringComparison.Ordinal
            );
    }

    private static string NormalizeLineNumbersInExceptions(string logOutput) =>
        LineNumberRegex().Replace(logOutput, ":line FIXED");

    private static string? FindSolutionDir(string? directory)
    {
        while (!string.IsNullOrEmpty(directory))
        {
            var solutionFiles = Directory.GetFiles(directory, "Conqueror.sln");
            if (solutionFiles.Length > 0)
            {
                return Path.GetDirectoryName(solutionFiles[0]);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }

    [GeneratedRegex(":line [0-9]+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex LineNumberRegex();
}
