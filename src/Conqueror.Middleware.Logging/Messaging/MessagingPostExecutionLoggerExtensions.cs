using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a message execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'ResponsePayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class MessagingPostExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponse(this ILogger logger,
                                                  LogLevel logLevel,
                                                  object? ResponsePayload,
                                                  double responseLatency,
                                                  string messageId,
                                                  string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message on {TransportTypeName:l} {TransportRole:l} and got response {@ResponsePayload:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseForTransport(this ILogger logger,
                                                              LogLevel logLevel,
                                                              string transportTypeName,
                                                              string transportRole,
                                                              object? ResponsePayload,
                                                              double responseLatency,
                                                              string messageId,
                                                              string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayload(this ILogger logger,
                                                                LogLevel logLevel,
                                                                double responseLatency,
                                                                string messageId,
                                                                string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message on {TransportTypeName:l} {TransportRole:l} in {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageResponseWithoutPayloadForTransport(this ILogger logger,
                                                                            LogLevel logLevel,
                                                                            string transportTypeName,
                                                                            string transportRole,
                                                                            double responseLatency,
                                                                            string messageId,
                                                                            string traceId);

    public static void LogMessageResponseWithPayloadAsIndentedJson(this ILogger logger,
                                                                   LogLevel logLevel,
                                                                   object? ResponsePayload,
                                                                   double responseLatency,
                                                                   string messageId,
                                                                   string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonUnix(logLevel, ResponsePayload, responseLatency, messageId, traceId);
            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonNonUnix(logLevel, ResponsePayload, responseLatency, messageId, traceId);
    }

    public static void LogMessageResponseWithPayloadAsIndentedJsonForTransport(this ILogger logger,
                                                                               LogLevel logLevel,
                                                                               string transportTypeName,
                                                                               string transportRole,
                                                                               object? ResponsePayload,
                                                                               double responseLatency,
                                                                               string messageId,
                                                                               string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportUnix(logLevel,
                                                                               transportTypeName,
                                                                               transportRole,
                                                                               ResponsePayload,
                                                                               responseLatency,
                                                                               messageId,
                                                                               traceId);
            return;
        }

        logger.LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnix(logLevel,
                                                                              transportTypeName,
                                                                              transportRole,
                                                                              ResponsePayload,
                                                                              responseLatency,
                                                                              messageId,
                                                                              traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonUnix(this ILogger logger,
                                                                                LogLevel logLevel,
                                                                                object? ResponsePayload,
                                                                                double responseLatency,
                                                                                string messageId,
                                                                                string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonNonUnix(this ILogger logger,
                                                                                   LogLevel logLevel,
                                                                                   object? ResponsePayload,
                                                                                   double responseLatency,
                                                                                   string messageId,
                                                                                   string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message on {TransportTypeName:l} {TransportRole:l} and got response\n{@ResponsePayload:l}\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportUnix(this ILogger logger,
                                                                                            LogLevel logLevel,
                                                                                            string transportTypeName,
                                                                                            string transportRole,
                                                                                            object? ResponsePayload,
                                                                                            double responseLatency,
                                                                                            string messageId,
                                                                                            string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-response",
        Message = "Handled message on {TransportTypeName:l} {TransportRole:l} and got response\r\n{@ResponsePayload:l}\r\nin {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogMessageResponseWithPayloadAsIndentedJsonForTransportNonUnix(this ILogger logger,
                                                                                               LogLevel logLevel,
                                                                                               string transportTypeName,
                                                                                               string transportRole,
                                                                                               object? ResponsePayload,
                                                                                               double responseLatency,
                                                                                               string messageId,
                                                                                               string traceId);
}
