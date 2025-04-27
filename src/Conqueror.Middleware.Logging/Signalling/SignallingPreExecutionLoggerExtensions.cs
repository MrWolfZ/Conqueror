using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a signal execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'SignalPayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class SignallingPreExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignal(this ILogger logger,
                                         LogLevel logLevel,
                                         object? SignalPayload,
                                         string signalId,
                                         string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal on {TransportTypeName:l} {TransportRole:l} with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalForTransport(this ILogger logger,
                                                     LogLevel logLevel,
                                                     string transportTypeName,
                                                     string transportRole,
                                                     object? SignalPayload,
                                                     string signalId,
                                                     string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayload(this ILogger logger,
                                                       LogLevel logLevel,
                                                       string signalId,
                                                       string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal on {TransportTypeName:l} {TransportRole:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayloadForTransport(this ILogger logger,
                                                                   LogLevel logLevel,
                                                                   string transportTypeName,
                                                                   string transportRole,
                                                                   string signalId,
                                                                   string traceId);

    public static void LogSignalWithPayloadAsIndentedJson(this ILogger logger,
                                                          LogLevel logLevel,
                                                          object? SignalPayload,
                                                          string signalId,
                                                          string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonUnix(logLevel, SignalPayload, signalId, traceId);
            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonNonUnix(logLevel, SignalPayload, signalId, traceId);
    }

    public static void LogSignalWithPayloadAsIndentedJsonForTransport(this ILogger logger,
                                                                      LogLevel logLevel,
                                                                      string transportTypeName,
                                                                      string transportRole,
                                                                      object? SignalPayload,
                                                                      string signalId,
                                                                      string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonForTransportUnix(logLevel,
                                                                      transportTypeName,
                                                                      transportRole,
                                                                      SignalPayload,
                                                                      signalId,
                                                                      traceId);
            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonForTransportNonUnix(logLevel,
                                                                     transportTypeName,
                                                                     transportRole,
                                                                     SignalPayload,
                                                                     signalId,
                                                                     traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonUnix(this ILogger logger,
                                                                       LogLevel logLevel,
                                                                       object? SignalPayload,
                                                                       string signalId,
                                                                       string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonNonUnix(this ILogger logger,
                                                                          LogLevel logLevel,
                                                                          object? SignalPayload,
                                                                          string signalId,
                                                                          string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal on {TransportTypeName:l} {TransportRole:l} with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportUnix(this ILogger logger,
                                                                                   LogLevel logLevel,
                                                                                   string transportTypeName,
                                                                                   string transportRole,
                                                                                   object? SignalPayload,
                                                                                   string signalId,
                                                                                   string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal on {TransportTypeName:l} {TransportRole:l} with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportNonUnix(this ILogger logger,
                                                                                      LogLevel logLevel,
                                                                                      string transportTypeName,
                                                                                      string transportRole,
                                                                                      object? SignalPayload,
                                                                                      string signalId,
                                                                                      string traceId);
}
