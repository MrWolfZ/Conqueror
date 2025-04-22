namespace Conqueror.Middleware.Logging.Tests;

internal static class LogOutputNormalizationStringExtensions
{
    public static string NormalizeLogOutput(this string logOutput)
        => NormalizeLineEndingsInJsonOutput(NormalizePathSeparators(logOutput));

    private static string NormalizeLineEndingsInJsonOutput(string logOutput)
    {
        return logOutput.Replace(@"\r\n", @"\n")
                        .Replace(@"\r", @"\n")

                        // on non-unix systems the stack trace ends with a newline inside JSON output
                        // so we strip that for consistency
                        .Replace(@"\n""", @"""");
    }

    private static string NormalizePathSeparators(string logOutput)
    {
        // in JSON output, backslashes are escaped, so we first unescape them
        // before running further replacement logic
        logOutput = logOutput.Replace(@"\\", @"\");

        // exception stack traces contain file paths that may be absolute paths, and
        // to ensure that the log output is stable regardless of the execution env
        // we strip any reference to the solution directory so that any paths are
        // relative
        var solutionDir = FindSolutionDir(Directory.GetCurrentDirectory());

        if (solutionDir is not null)
        {
            logOutput = logOutput.Replace(solutionDir + Path.DirectorySeparatorChar, "{SolutionDirectory}");
        }

        return logOutput.Replace(@"src\", "src/")
                        .Replace(@"Conqueror\", "Conqueror/")
                        .Replace(@"Messaging\", "Messaging/")
                        .Replace(@"Signalling\", "Signalling/")
                        .Replace(@"Streaming\", "Streaming/")
                        .Replace(@"Logging\", "Logging/")
                        .Replace(@"Tests\", "Tests/")

                        // on unix platforms the stack trace contains references to {ProjectDirectory} instead
                        // of the solution dir for this test project, so we have to normalize those as well when
                        // running on non-unix systems
                        .Replace("{SolutionDirectory}src/Conqueror.Middleware.Logging.Tests/", "{ProjectDirectory}");
    }

    private static string? FindSolutionDir(string? directory)
    {
        while (!string.IsNullOrEmpty(directory))
        {
            var solutionFiles = Directory.GetFiles(directory, "*.sln");
            if (solutionFiles.Length > 0)
            {
                return Path.GetDirectoryName(solutionFiles[0]);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return null;
    }
}
