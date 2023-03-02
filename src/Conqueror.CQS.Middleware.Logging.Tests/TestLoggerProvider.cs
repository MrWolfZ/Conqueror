using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests;

internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TestLogger> loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly TestLogSink logSink;

    public TestLoggerProvider(TestLogSink logSink)
    {
        this.logSink = logSink;
    }

    public ILogger CreateLogger(string categoryName) =>
        loggers.GetOrAdd(categoryName, _ => new(categoryName, logSink));

    public ILogger? GetLogger(string categoryName) =>
        loggers.TryGetValue(categoryName, out var l) ? l : null;

    public void Dispose()
    {
        loggers.Clear();
    }
}
