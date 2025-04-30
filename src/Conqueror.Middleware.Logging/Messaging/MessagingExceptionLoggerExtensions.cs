using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Messaging;

[SuppressMessage(
    "LoggingGenerator",
    "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
    Justification = "we have many logging methods for the same phase of a message execution that should have the same event name")]
internal static partial class MessagingExceptionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-message-exception",
        Message = "An exception occurred while handling message of type '{MessageTypeName}' after {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageExceptionOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-exception",
        Message = "An exception occurred while sending in-process message of type '{MessageTypeName}' after {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageExceptionOnSender(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-exception",
        Message = "An exception occurred while handling {TransportTypeName:l} message of type '{MessageTypeName}' after {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageExceptionForTransportOnReceiver(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        string transportTypeName,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);

    [LoggerMessage(
        EventName = "conqueror-message-exception",
        Message = "An exception occurred while sending {TransportTypeName:l} message of type '{MessageTypeName}' after {ResponseLatency:0.0000}ms (Message ID: {MessageId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogMessageExceptionForTransportOnSender(
        this ILogger logger,
        LogLevel logLevel,
        Exception exception,
        string transportTypeName,
        string messageTypeName,
        double responseLatency,
        string messageId,
        string traceId);
}
