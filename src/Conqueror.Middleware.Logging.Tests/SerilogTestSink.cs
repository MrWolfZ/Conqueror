using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Conqueror.Middleware.Logging.Tests;

file sealed class SerilogTestSink(ILogEventSink wrapped, DateTimeOffset timestamp) : ILogEventSink, IDisposable
{
    public void Emit(LogEvent logEvent)
    {
        var newLogEvent = new LogEvent(
            timestamp,
            logEvent.Level,
            logEvent.Exception is null ? null : new TruncatedLoggingException(logEvent.Exception),
            logEvent.MessageTemplate,
            logEvent.Properties
                    .Select(kv => new LogEventProperty(kv.Key, kv.Value)));

        wrapped.Emit(newLogEvent);
    }

    public void Dispose() => (wrapped as IDisposable)?.Dispose();
}

public static class LoggerSinkConfigurationStableTimestampExtensions
{
    public static LoggerConfiguration TestSink(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        DateTimeOffset timestamp,
        TextWriter textWriter)
    {
        var sink = LoggerSinkConfiguration.Wrap(sink => new SerilogTestSink(sink, timestamp), c => c.TextWriter(textWriter));
        return loggerSinkConfiguration.Sink(sink);
    }

    public static LoggerConfiguration TestSink(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        DateTimeOffset timestamp,
        ITextFormatter textFormatter,
        TextWriter textWriter)
    {
        var sink = LoggerSinkConfiguration.Wrap(sink => new SerilogTestSink(sink, timestamp), c => c.TextWriter(textFormatter, textWriter));
        return loggerSinkConfiguration.Sink(sink);
    }
}
