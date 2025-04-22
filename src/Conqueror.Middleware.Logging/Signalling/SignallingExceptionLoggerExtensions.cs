using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a signal execution that should have the same event name")]
internal static partial class SignallingExceptionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-signal-exception",
        Message = "An exception occurred while handling signal after {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalException(this ILogger logger,
                                                  LogLevel logLevel,
                                                  Exception exception,
                                                  double responseLatency,
                                                  string signalId,
                                                  string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal-exception",
        Message = "An exception occurred while handling {TransportTypeName:l} signal on {TransportRole:l} after {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalExceptionForTransport(this ILogger logger,
                                                              LogLevel logLevel,
                                                              Exception exception,
                                                              string transportTypeName,
                                                              string transportRole,
                                                              double responseLatency,
                                                              string signalId,
                                                              string traceId);
}
