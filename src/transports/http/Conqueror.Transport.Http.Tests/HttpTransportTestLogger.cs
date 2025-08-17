namespace Conqueror.Transport.Http.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using NUnit.Framework.Interfaces;

file sealed class HttpTransportTestLogger(
    string categoryName,
    HttpTransportTestLogSink logSink,
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

file sealed class HttpTransportTestLoggerProvider(
    HttpTransportTestLogSink logSink,
    IEnumerable<ConsoleFormatter> consoleFormatters,
    IOptions<ConsoleLoggerOptions> consoleOptions
) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new HttpTransportTestLogger(
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

internal sealed class HttpTransportTestLogSink : IDisposable
{
    public List<string> LogEntries { get; } = [];

    public void Dispose()
    {
        var testContext = TestContext.CurrentContext;

        if (testContext.Result.Outcome.Status is TestStatus.Failed)
        {
            Console.Write(string.Concat(LogEntries));
        }
    }
}

internal static class HttpTransportTestLoggingBuilderExtensions
{
    public static ILoggingBuilder AddTestLogger(this ILoggingBuilder builder)
    {
        _ = builder.AddSimpleConsole(o => o.TimestampFormat = "[HH:mm:ss.fff]");

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, HttpTransportTestLoggerProvider>()
        );
        builder.Services.TryAddSingleton<HttpTransportTestLogSink>();

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
