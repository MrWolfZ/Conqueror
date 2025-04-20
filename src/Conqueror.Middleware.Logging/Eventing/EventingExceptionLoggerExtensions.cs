using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Eventing;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of a notification execution that should have the same event name")]
internal static partial class EventingExceptionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-event-notification-exception",
        Message = "An exception occurred while handling event notification after {ResponseLatency:0.0000}ms (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationException(this ILogger logger,
                                                             LogLevel logLevel,
                                                             Exception exception,
                                                             double responseLatency,
                                                             string eventNotificationId,
                                                             string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification-exception",
        Message = "An exception occurred while handling {TransportTypeName:l} event notification on {TransportRole:l} after {ResponseLatency:0.0000}ms (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationExceptionForTransport(this ILogger logger,
                                                                         LogLevel logLevel,
                                                                         Exception exception,
                                                                         string transportTypeName,
                                                                         string transportRole,
                                                                         double responseLatency,
                                                                         string eventNotificationId,
                                                                         string traceId);
}
