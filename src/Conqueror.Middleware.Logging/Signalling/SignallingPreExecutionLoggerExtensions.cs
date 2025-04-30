using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Signalling;

[SuppressMessage(
    "LoggingGenerator",
    "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
    Justification = "we have many logging methods for the same phase of a signal execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'SignalPayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class SignallingPreExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal of type '{SignalTypeName}' with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing in-process signal of type '{SignalTypeName}' with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling {TransportTypeName:l} signal of type '{SignalTypeName}' with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing {TransportTypeName:l} signal of type '{SignalTypeName}' with payload {@SignalPayload:l} (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalForTransportOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal of type '{SignalTypeName}' (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayloadOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing in-process signal of type '{SignalTypeName}' (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayloadOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling {TransportTypeName:l} signal of type '{SignalTypeName}' (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayloadForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing {TransportTypeName:l} signal of type '{SignalTypeName}' (Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogSignalWithoutPayloadForTransportOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        string signalId,
        string traceId);

    public static void LogSignalWithPayloadAsIndentedJsonOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonUnixOnReceiver(
                logLevel,
                signalTypeName,
                SignalPayload,
                signalId,
                traceId);

            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonNonUnixOnReceiver(
            logLevel,
            signalTypeName,
            SignalPayload,
            signalId,
            traceId);
    }

    public static void LogSignalWithPayloadAsIndentedJsonOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonUnixOnPublisher(
                logLevel,
                signalTypeName,
                SignalPayload,
                signalId,
                traceId);

            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonNonUnixOnPublisher(
            logLevel,
            signalTypeName,
            SignalPayload,
            signalId,
            traceId);
    }

    public static void LogSignalWithPayloadAsIndentedJsonForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
                logLevel,
                transportTypeName,
                signalTypeName,
                SignalPayload,
                signalId,
                traceId);

            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
            logLevel,
            transportTypeName,
            signalTypeName,
            SignalPayload,
            signalId,
            traceId);
    }

    public static void LogSignalWithPayloadAsIndentedJsonForTransportOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogSignalWithPayloadAsIndentedJsonForTransportUnixOnPublisher(
                logLevel,
                transportTypeName,
                signalTypeName,
                SignalPayload,
                signalId,
                traceId);

            return;
        }

        logger.LogSignalWithPayloadAsIndentedJsonForTransportNonUnixOnPublisher(
            logLevel,
            transportTypeName,
            signalTypeName,
            SignalPayload,
            signalId,
            traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal of type '{SignalTypeName}' with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing in-process signal of type '{SignalTypeName}' with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonUnixOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling signal of type '{SignalTypeName}' with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing in-process signal of type '{SignalTypeName}' with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonNonUnixOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling {TransportTypeName:l} signal of type '{SignalTypeName}' with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing {TransportTypeName:l} signal of type '{SignalTypeName}' with payload\n{@SignalPayload:l}\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportUnixOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Handling {TransportTypeName:l} signal of type '{SignalTypeName}' with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-signal",
        Message = "Publishing {TransportTypeName:l} signal of type '{SignalTypeName}' with payload\r\n{@SignalPayload:l}\r\n(Signal ID: {SignalId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogSignalWithPayloadAsIndentedJsonForTransportNonUnixOnPublisher(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string signalTypeName,
        object? SignalPayload,
        string signalId,
        string traceId);
}
