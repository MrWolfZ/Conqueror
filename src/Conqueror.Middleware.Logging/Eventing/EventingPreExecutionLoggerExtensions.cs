using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Eventing;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of an event notification execution that should have the same event name")]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "the upper case for 'EventNotificationPayload' is required since otherwise the source gen emits it lowercase due to the '@' prefix")]
internal static partial class EventingPreExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling event notification with payload {@EventNotificationPayload:l} (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotification(this ILogger logger,
                                                    LogLevel logLevel,
                                                    object? EventNotificationPayload,
                                                    string eventNotificationId,
                                                    string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling {TransportTypeName:l} event notification on {TransportRole:l} with payload {@EventNotificationPayload:l} (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationForTransport(this ILogger logger,
                                                                LogLevel logLevel,
                                                                string transportTypeName,
                                                                string transportRole,
                                                                object? EventNotificationPayload,
                                                                string eventNotificationId,
                                                                string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling event notification (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationWithoutPayload(this ILogger logger,
                                                                  LogLevel logLevel,
                                                                  string eventNotificationId,
                                                                  string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling {TransportTypeName:l} event notification on {TransportRole:l} (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationWithoutPayloadForTransport(this ILogger logger,
                                                                              LogLevel logLevel,
                                                                              string transportTypeName,
                                                                              string transportRole,
                                                                              string eventNotificationId,
                                                                              string traceId);

    public static void LogEventNotificationWithPayloadAsIndentedJson(this ILogger logger,
                                                                     LogLevel logLevel,
                                                                     object? EventNotificationPayload,
                                                                     string eventNotificationId,
                                                                     string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogEventNotificationWithPayloadAsIndentedJsonUnix(logLevel, EventNotificationPayload, eventNotificationId, traceId);
            return;
        }

        logger.LogEventNotificationWithPayloadAsIndentedJsonNonUnix(logLevel, EventNotificationPayload, eventNotificationId, traceId);
    }

    public static void LogEventNotificationWithPayloadAsIndentedJsonForTransport(this ILogger logger,
                                                                                 LogLevel logLevel,
                                                                                 string transportTypeName,
                                                                                 string transportRole,
                                                                                 object? EventNotificationPayload,
                                                                                 string eventNotificationId,
                                                                                 string traceId)
    {
        if (Environment.NewLine == "\n")
        {
            logger.LogEventNotificationWithPayloadAsIndentedJsonForTransportUnix(logLevel,
                                                                                 transportTypeName,
                                                                                 transportRole,
                                                                                 EventNotificationPayload,
                                                                                 eventNotificationId,
                                                                                 traceId);
            return;
        }

        logger.LogEventNotificationWithPayloadAsIndentedJsonForTransportNonUnix(logLevel,
                                                                                transportTypeName,
                                                                                transportRole,
                                                                                EventNotificationPayload,
                                                                                eventNotificationId,
                                                                                traceId);
    }

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling event notification with payload\n{@EventNotificationPayload:l}\n(Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogEventNotificationWithPayloadAsIndentedJsonUnix(this ILogger logger,
                                                                                  LogLevel logLevel,
                                                                                  object? EventNotificationPayload,
                                                                                  string eventNotificationId,
                                                                                  string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling event notification with payload\r\n{@EventNotificationPayload:l}\r\n(Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogEventNotificationWithPayloadAsIndentedJsonNonUnix(this ILogger logger,
                                                                                     LogLevel logLevel,
                                                                                     object? EventNotificationPayload,
                                                                                     string eventNotificationId,
                                                                                     string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling {TransportTypeName:l} event notification on {TransportRole:l} with payload\n{@EventNotificationPayload:l}\n(Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogEventNotificationWithPayloadAsIndentedJsonForTransportUnix(this ILogger logger,
                                                                                              LogLevel logLevel,
                                                                                              string transportTypeName,
                                                                                              string transportRole,
                                                                                              object? EventNotificationPayload,
                                                                                              string eventNotificationId,
                                                                                              string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification",
        Message = "Handling {TransportTypeName:l} event notification on {TransportRole:l} with payload\r\n{@EventNotificationPayload:l}\r\n(Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    private static partial void LogEventNotificationWithPayloadAsIndentedJsonForTransportNonUnix(this ILogger logger,
                                                                                                 LogLevel logLevel,
                                                                                                 string transportTypeName,
                                                                                                 string transportRole,
                                                                                                 object? EventNotificationPayload,
                                                                                                 string eventNotificationId,
                                                                                                 string traceId);
}
