using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

internal sealed class TestLogger(string categoryName, TestLogSink logSink) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        logSink.LogEntries.Add((categoryName, logLevel, formatter(state, exception)));
        Console.WriteLine($"{logLevel} [{categoryName}]: {formatter(state, exception)}");
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
