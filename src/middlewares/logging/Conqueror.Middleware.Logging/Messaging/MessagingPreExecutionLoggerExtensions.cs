using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

[SuppressMessage(
    "LoggingGenerator",
    "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
    Justification = "we have many logging methods for the same phase of a message execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'MessagePayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class MessagingPreExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message of type '{MessageTypeName}' with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending in-process message of type '{MessageTypeName}' with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling {TransportTypeName:l} message of type '{MessageTypeName}' with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending {TransportTypeName:l} message of type '{MessageTypeName}' with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message of type '{MessageTypeName}' (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayloadOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending in-process message of type '{MessageTypeName}' (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayloadOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling {TransportTypeName:l} message of type '{MessageTypeName}' (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayloadForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending {TransportTypeName:l} message of type '{MessageTypeName}' (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayloadForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        string messageId,
        string traceId);

    public static void LogMessageWithPayloadAsIndentedJsonOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonUnixOnReceiver(
                logLevel,
                messageTypeName,
                MessagePayload,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonNonUnixOnReceiver(
            logLevel,
            messageTypeName,
            MessagePayload,
            messageId,
            traceId);
    }

    public static void LogMessageWithPayloadAsIndentedJsonOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonUnixOnSender(
                logLevel,
                messageTypeName,
                MessagePayload,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonNonUnixOnSender(
            logLevel,
            messageTypeName,
            MessagePayload,
            messageId,
            traceId);
    }

    public static void LogMessageWithPayloadAsIndentedJsonForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
                logLevel,
                transportTypeName,
                messageTypeName,
                MessagePayload,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
            logLevel,
            transportTypeName,
            messageTypeName,
            MessagePayload,
            messageId,
            traceId);
    }

    public static void LogMessageWithPayloadAsIndentedJsonForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonForTransportUnixOnSender(
                logLevel,
                transportTypeName,
                messageTypeName,
                MessagePayload,
                messageId,
                traceId);

            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonForTransportNonUnixOnSender(
            logLevel,
            transportTypeName,
            messageTypeName,
            MessagePayload,
            messageId,
            traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message of type '{MessageTypeName}' with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending in-process message of type '{MessageTypeName}' with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message of type '{MessageTypeName}' with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending in-process message of type '{MessageTypeName}' with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonNonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling {TransportTypeName:l} message of type '{MessageTypeName}' with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending {TransportTypeName:l} message of type '{MessageTypeName}' with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling {TransportTypeName:l} message of type '{MessageTypeName}' with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportNonUnixOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Sending {TransportTypeName:l} message of type '{MessageTypeName}' with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportNonUnixOnSender(
        this ILogger logger,
        LogLevel logLevel,
        string transportTypeName,
        string messageTypeName,
        object? MessagePayload,
        string messageId,
        string traceId);
}
