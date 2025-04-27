using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a message execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'MessagePayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class MessagingPreExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessage(this ILogger logger,
                                          LogLevel logLevel,
                                          object? MessagePayload,
                                          string messageId,
                                          string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message on {TransportTypeName:l} {TransportRole:l} with payload {@MessagePayload:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageForTransport(this ILogger logger,
                                                      LogLevel logLevel,
                                                      string transportTypeName,
                                                      string transportRole,
                                                      object? MessagePayload,
                                                      string messageId,
                                                      string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayload(this ILogger logger,
                                                        LogLevel logLevel,
                                                        string messageId,
                                                        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message on {TransportTypeName:l} {TransportRole:l} (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageWithoutPayloadForTransport(this ILogger logger,
                                                                    LogLevel logLevel,
                                                                    string transportTypeName,
                                                                    string transportRole,
                                                                    string messageId,
                                                                    string traceId);

    public static void LogMessageWithPayloadAsIndentedJson(this ILogger logger,
                                                           LogLevel logLevel,
                                                           object? MessagePayload,
                                                           string messageId,
                                                           string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonUnix(logLevel, MessagePayload, messageId, traceId);
            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonNonUnix(logLevel, MessagePayload, messageId, traceId);
    }

    public static void LogMessageWithPayloadAsIndentedJsonForTransport(this ILogger logger,
                                                                       LogLevel logLevel,
                                                                       string transportTypeName,
                                                                       string transportRole,
                                                                       object? MessagePayload,
                                                                       string messageId,
                                                                       string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageWithPayloadAsIndentedJsonForTransportUnix(logLevel,
                                                                       transportTypeName,
                                                                       transportRole,
                                                                       MessagePayload,
                                                                       messageId,
                                                                       traceId);
            return;
        }

        logger.LogMessageWithPayloadAsIndentedJsonForTransportNonUnix(logLevel,
                                                                      transportTypeName,
                                                                      transportRole,
                                                                      MessagePayload,
                                                                      messageId,
                                                                      traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonUnix(this ILogger logger,
                                                                        LogLevel logLevel,
                                                                        object? MessagePayload,
                                                                        string messageId,
                                                                        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonNonUnix(this ILogger logger,
                                                                           LogLevel logLevel,
                                                                           object? MessagePayload,
                                                                           string messageId,
                                                                           string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message on {TransportTypeName:l} {TransportRole:l} with payload\n{@MessagePayload:l}\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportUnix(this ILogger logger,
                                                                                    LogLevel logLevel,
                                                                                    string transportTypeName,
                                                                                    string transportRole,
                                                                                    object? MessagePayload,
                                                                                    string messageId,
                                                                                    string traceId);

    [LoggerMessage(
        EventName = "conqueror-message",
        Message = "Handling message on {TransportTypeName:l} {TransportRole:l} with payload\r\n{@MessagePayload:l}\r\n(Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageWithPayloadAsIndentedJsonForTransportNonUnix(this ILogger logger,
                                                                                       LogLevel logLevel,
                                                                                       string transportTypeName,
                                                                                       string transportRole,
                                                                                       object? MessagePayload,
                                                                                       string messageId,
                                                                                       string traceId);
}
