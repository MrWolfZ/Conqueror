using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NUnit.Framework.Interfaces;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Conqueror.Transport.FileSystem.Tests;

file sealed class FileSystemTransportTestLogger(
    string categoryName,
    FileSystemTransportTestLogSink logSink,
    ConsoleFormatter consoleFormatter) : ILogger
{
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var logEntry = new LogEntry<TState>(
            logLevel,
            categoryName,
            eventId,
            state,
            exception,
            formatter);
        using var textWriter = new StringWriter();
        consoleFormatter.Write(in logEntry, null, textWriter);
        logSink.LogEntries.Add(textWriter.ToString());
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return new NoopDisposable();
    }

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}

file sealed class FileSystemTransportTestLoggerProvider(
    FileSystemTransportTestLogSink logSink,
    IEnumerable<ConsoleFormatter> consoleFormatters,
    IOptions<ConsoleLoggerOptions> consoleOptions) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new FileSystemTransportTestLogger(
            categoryName,
            logSink,
            consoleFormatters.Single(f => f.Name == (consoleOptions.Value.FormatterName ?? ConsoleFormatterNames.Simple)));

    public void Dispose()
    {
    }
}

file sealed class FileSystemTransportTestLogSink
{
    public List<string> LogEntries { get; } = [];

    public FileSystemTransportTestLogSink() => FileSystemTransportTestLogPrinter.AddLogSink(this);
}

internal static class FileSystemTransportTestLoggingBuilderExtensions
{
    public static ILoggingBuilder AddTestLogger(this ILoggingBuilder builder)
    {
        _ = builder.AddSimpleConsole(o => o.TimestampFormat = "[HH:mm:ss.fff]");

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileSystemTransportTestLoggerProvider>());
        builder.Services.TryAddSingleton<FileSystemTransportTestLogSink>();

        // remove the console logger provider so that we can control the logging
        // ourselves (we want to only log stuff if a test fails to prevent disk
        // churn with superfluous log data, and also to improve test performance)
        _ = builder.Services.Remove(
            builder.Services.Single(s => s.ServiceType == typeof(ILoggerProvider)
                                         && s.ImplementationType == typeof(ConsoleLoggerProvider)));

        return builder;
    }
}

file static class FileSystemTransportTestLogPrinter
{
    private static readonly ConcurrentDictionary<string, FileSystemTransportTestLogSink> LogSinkByTestId = new();

    public static void AddLogSink(FileSystemTransportTestLogSink logSink)
    {
        var testId = TestContext.CurrentContext.Test.ID;
        LogSinkByTestId[testId] = logSink;
    }

    public static void PrintLogsIfNecessary()
    {
        var testContext = TestContext.CurrentContext;
        if (testContext.Result.Outcome.Status is TestStatus.Failed
            && LogSinkByTestId.TryRemove(testContext.Test.ID, out var logSink))
        {
            Console.Write(string.Concat(logSink.LogEntries));
        }
    }
}

[AttributeUsage(AttributeTargets.Assembly)]
internal sealed class FileSystemTransportTestLoggingHookAttribute : Attribute, ITestAction
{
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

    public ActionTargets Targets => ActionTargets.Test;
}
