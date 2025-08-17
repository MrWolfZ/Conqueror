namespace Conqueror.Transport.FileSystem.Tests;

file sealed class FileSystemTransportTestLogger(
    string categoryName,
    FileSystemTransportTestLogSink logSink,
    ConsoleFormatter consoleFormatter
) : ILogger
{
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        var logEntry = new LogEntry<TState>(logLevel, categoryName, eventId, state, exception, formatter);
        using var textWriter = new StringWriter();
        consoleFormatter.Write(in logEntry, scopeProvider: null, textWriter);
        logSink.LogEntries.Add(textWriter.ToString());
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel is not LogLevel.None;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => new NoopDisposable();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

file sealed class FileSystemTransportTestLoggerProvider(
    FileSystemTransportTestLogSink logSink,
    IEnumerable<ConsoleFormatter> consoleFormatters,
    IOptions<ConsoleLoggerOptions> consoleOptions
) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new FileSystemTransportTestLogger(
            categoryName,
            logSink,
            consoleFormatters.Single(f =>
                string.Equals(
                    f.Name,
                    consoleOptions.Value.FormatterName ?? ConsoleFormatterNames.Simple,
                    StringComparison.Ordinal
                )
            )
        );

    public void Dispose() { }
}

file sealed class FileSystemTransportTestLogSink
{
    public FileSystemTransportTestLogSink() => FileSystemTransportTestLogPrinter.AddLogSink(this);

    public List<string> LogEntries { get; } = [];
}

internal static class FileSystemTransportTestLoggingBuilderExtensions
{
    public static ILoggingBuilder AddTestLogger(this ILoggingBuilder builder)
    {
        _ = builder.AddSimpleConsole(o => o.TimestampFormat = "[HH:mm:ss.fff]");

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, FileSystemTransportTestLoggerProvider>()
        );
        builder.Services.TryAddSingleton<FileSystemTransportTestLogSink>();

        // remove the console logger provider so that we can control the logging
        // ourselves (we want to only log stuff if a test fails to prevent disk
        // churn with superfluous log data, and also to improve test performance)
        _ = builder.Services.Remove(
            builder.Services.Single(s =>
                s.ServiceType == typeof(ILoggerProvider) && s.ImplementationType == typeof(ConsoleLoggerProvider)
            )
        );

        return builder;
    }
}

file static class FileSystemTransportTestLogPrinter
{
    private static readonly ConcurrentDictionary<string, FileSystemTransportTestLogSink> LogSinkByTestId = [];

    public static void AddLogSink(FileSystemTransportTestLogSink logSink)
    {
        var testId = TestContext.CurrentContext.Test.ID;
        LogSinkByTestId[testId] = logSink;
    }

    public static void PrintLogsIfNecessary()
    {
        var testContext = TestContext.CurrentContext;
        if (
            testContext.Result.Outcome.Status is TestStatus.Failed
            && LogSinkByTestId.TryRemove(testContext.Test.ID, out var logSink)
        )
        {
            Console.Write(string.Concat(logSink.LogEntries));
        }
    }
}

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class FileSystemTransportTestLoggingHookAttribute : Attribute, ITestAction
{
    public ActionTargets Targets => ActionTargets.Test;

    public void BeforeTest(ITest test)
    {
        // nothing to do here
    }

    public void AfterTest(ITest test)
    {
        if (test.IsSuite)
        {
            return;
        }

        FileSystemTransportTestLogPrinter.PrintLogsIfNecessary();
    }
}
