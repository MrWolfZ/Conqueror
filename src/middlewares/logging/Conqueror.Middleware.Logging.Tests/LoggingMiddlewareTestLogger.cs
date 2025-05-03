using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Conqueror.Middleware.Logging.Tests;

file sealed class LoggingMiddlewareTestLogger(
    string categoryName,
    LoggingMiddlewareTestLogSink logSink,
    ConsoleFormatter consoleFormatter,
    LoggingMiddlewareTestLoggerOptions options) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var ex = options.ShouldTruncate ? exception is null ? null : new TruncatedLoggingException(exception) : exception;
        var logEntry = new LogEntry<TState>(logLevel, categoryName, eventId, state, ex, formatter);
        using var textWriter = new StringWriter();
        consoleFormatter.Write(in logEntry, null, textWriter);
        var message = textWriter.ToString();
        logSink.LogEntries.Add((categoryName, logLevel, message));
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

file sealed class LoggingMiddlewareTestLoggerProvider(
    LoggingMiddlewareTestLogSink logSink,
    IEnumerable<ConsoleFormatter> consoleFormatters,
    IOptions<ConsoleLoggerOptions> consoleOptions,
    LoggingMiddlewareTestLoggerOptions options) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
        => new LoggingMiddlewareTestLogger(categoryName,
                                           logSink,
                                           consoleFormatters.Single(f => f.Name == consoleOptions.Value.FormatterName),
                                           options);

    public void Dispose()
    {
    }
}

internal sealed class LoggingMiddlewareTestLogSink
{
    public List<(string CategoryName, LogLevel LogLevel, string Message)> LogEntries { get; } = [];
}

internal static class LoggingMiddlewareTestLoggingBuilderExtensions
{
    public static ILoggingBuilder AddTestLogger(this ILoggingBuilder builder, bool shouldTruncate = true)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggingMiddlewareTestLoggerProvider>());
        builder.Services.TryAddSingleton<LoggingMiddlewareTestLogSink>();
        builder.Services.TryAddSingleton(new LoggingMiddlewareTestLoggerOptions(shouldTruncate));

        return builder;
    }
}

file sealed record LoggingMiddlewareTestLoggerOptions(bool ShouldTruncate);
