using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging.Tests
{
    internal sealed class TestLogger : ILogger
    {
        private readonly string categoryName;
        private readonly TestLogSink logSink;

        public TestLogger(string categoryName, TestLogSink logSink)
        {
            this.categoryName = categoryName;
            this.logSink = logSink;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            logSink.LogEntries.Add((categoryName, logLevel, formatter(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public IDisposable BeginScope<TState>(TState state)
#if NET7_0_OR_GREATER
            where TState : notnull
#endif
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
}
