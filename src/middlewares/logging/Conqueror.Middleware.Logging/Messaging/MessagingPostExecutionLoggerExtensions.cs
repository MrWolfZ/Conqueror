using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

[SuppressMessage(
    "LoggingGenerator",
    "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
    Justification = "we have many logging methods for the same phase of a message execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'ResponsePayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class MessagingPostExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message of type '{MessageTypeName}' and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent in-process message of type '{MessageTypeName}' and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled {TransportTypeName:l} message of type '{MessageTypeName}' and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent {TransportTypeName:l} message of type '{MessageTypeName}' and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message of type '{MessageTypeName}' in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayloadOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent in-process message of type '{MessageTypeName}' in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayloadOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled {TransportTypeName:l} message of type '{MessageTypeName}' in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayloadForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent {TransportTypeName:l} message of type '{MessageTypeName}' in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayloadForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    public static void LogMessageResponseWithPayloadAsIndentedJsonOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonUnixOnReceiver(
                logLevel,
                messageTypeName,
                ResponsePayload,
                responseLatency,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonNonUnixOnReceiver(
            logLevel,
            messageTypeName,
            ResponsePayload,
            responseLatency,
            messageId,
            traceId);
    }

    public static void LogMessageResponseWithPayloadAsIndentedJsonOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonUnixOnSender(
                logLevel,
                messageTypeName,
                ResponsePayload,
                responseLatency,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonNonUnixOnSender(
            logLevel,
            messageTypeName,
            ResponsePayload,
            responseLatency,
            messageId,
            traceId);
    }

    public static void LogMessageResponseWithPayloadAsIndentedJsonForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
                logLevel,
                transportTypeName,
                messageTypeName,
                ResponsePayload,
                responseLatency,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
            logLevel,
            transportTypeName,
            messageTypeName,
            ResponsePayload,
            responseLatency,
            messageId,
            traceId);
    }

    public static void LogMessageResponseWithPayloadAsIndentedJsonForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportUnixOnSender(
                logLevel,
                transportTypeName,
                messageTypeName,
                ResponsePayload,
                responseLatency,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnixOnSender(
            logLevel,
            transportTypeName,
            messageTypeName,
            ResponsePayload,
            responseLatency,
            messageId,
            traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message of type '{MessageTypeName}' and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent in-process message of type '{MessageTypeName}' and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message of type '{MessageTypeName}' and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent in-process message of type '{MessageTypeName}' and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonNonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled {TransportTypeName:l} message of type '{MessageTypeName}' and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent {TransportTypeName:l} message of type '{MessageTypeName}' and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled {TransportTypeName:l} message of type '{MessageTypeName}' and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Sent {TransportTypeName:l} message of type '{MessageTypeName}' and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? ResponsePayload,
        double responseLatency,
        string messageId,
        string traceId);
}
