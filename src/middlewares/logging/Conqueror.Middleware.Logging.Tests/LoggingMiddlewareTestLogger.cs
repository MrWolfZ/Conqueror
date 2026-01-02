namespace Conqueror.Middleware.Logging.Tests;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using NUnit.Framework.Interfaces;

file sealed class LoggingMiddlewareTestLogger(
    string categoryName,
    LoggingMiddlewareTestLogSink logSink,
    ConsoleFormatter consoleFormatter,
    LoggingMiddlewareTestLoggerOptions options
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
        var ex = options.ShouldTruncate
            ? exception is null
                ? null
                : new TruncatedLoggingException(exception)
            : exception;
        var logEntry = new LogEntry<TState>(logLevel, categoryName, eventId, state, ex, formatter);
        using var textWriter = new StringWriter();
        consoleFormatter.Write(in logEntry, scopeProvider: null, textWriter);
        var message = textWriter.ToString();
        logSink.LogEntries.Add((categoryName, logLevel, message));
        logSink.FormattedLogEntries.Add(message);
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel is not LogLevel.None;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => new NoopDisposable();

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}

file sealed class LoggingMiddlewareTestLoggerProvider(
    LoggingMiddlewareTestLogSink logSink,
    IEnumerable<ConsoleFormatter> consoleFormatters,
    IOptions<ConsoleLoggerOptions> consoleOptions,
    LoggingMiddlewareTestLoggerOptions options
) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        new LoggingMiddlewareTestLogger(
            categoryName,
            logSink,
            consoleFormatters.Single(f =>
                string.Equals(f.Name, consoleOptions.Value.FormatterName, StringComparison.Ordinal)
            ),
            options
        );

    public void Dispose() { }
}

internal sealed class LoggingMiddlewareTestLogSink : IDisposable
{
    public List<(string CategoryName, LogLevel LogLevel, string Message)> LogEntries { get; } = [];

    public List<string> FormattedLogEntries { get; } = [];

    public void Dispose()
    {
        var testContext = TestContext.CurrentContext;

        if (testContext.Result.Outcome.Status is TestStatus.Failed)
        {
            Console.Write(string.Concat(FormattedLogEntries));
        }
    }
}

internal static class LoggingMiddlewareTestLoggingBuilderExtensions
{
    public static ILoggingBuilder AddTestLogger(this ILoggingBuilder builder, bool shouldTruncate = true)
    {
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, LoggingMiddlewareTestLoggerProvider>()
        );
        builder.Services.TryAddSingleton<LoggingMiddlewareTestLogSink>();
        builder.Services.TryAddSingleton(new LoggingMiddlewareTestLoggerOptions(shouldTruncate));

        var consoleLoggerDescriptor = builder.Services.SingleOrDefault(s =>
            s.ServiceType == typeof(ILoggerProvider) && s.ImplementationType == typeof(ConsoleLoggerProvider)
        );

        if (consoleLoggerDescriptor is not null)
        {
            _ = builder.Services.Remove(consoleLoggerDescriptor);
        }

        return builder;
    }
}

file sealed record LoggingMiddlewareTestLoggerOptions(bool ShouldTruncate);
