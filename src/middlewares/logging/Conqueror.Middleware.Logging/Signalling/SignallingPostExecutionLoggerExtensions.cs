using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

[SuppressMessage(
    "LoggingGenerator",
    "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
    Justification = "we have many logging methods for the same phase of a signal execution that should have the same event name")]
internal static partial class SignallingPostExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Handled signal of type '{SignalTypeName}' in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandledOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        double responseLatency,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Published in-process signal of type '{SignalTypeName}' in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandledOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        double responseLatency,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Handled {TransportTypeName:l} signal of type '{SignalTypeName}' in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandledForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        double responseLatency,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal-handled",
        Message = "Published {TransportTypeName:l} signal of type '{SignalTypeName}' in {ResponseLatency:0.0000}ms (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalHandledForTransportOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        double responseLatency,
        string signalId,
        string traceId);
}
