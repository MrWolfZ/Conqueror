using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a signal execution that should have the same event name")]
internal static partial class SignallingPostExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Handled signal in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandled(this ILogger logger,
                                                LogLevel logLevel,
                                                double responseLatency,
                                                string signalId,
                                                string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Handled signal on {TransportTypeName:l} {TransportRole:l} in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandledForTransport(this ILogger logger,
                                                            LogLevel logLevel,
                                                            string transportTypeName,
                                                            string transportRole,
                                                            double responseLatency,
                                                            string signalId,
                                                            string traceId);
}
