using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Eventing;

[SuppressMessage("LoggingGenerator", "SYSLIB1025:Multiple logging methods should not use the same event name within a class",
                 Justification = "we have many logging methods for the same phase of an event notification execution that should have the same event name")]
internal static partial class EventingPostExecutionLoggerExtensions
{
    [LoggerMessage(
        EventName = "conqueror-event-notification-handled",
        Message = "Handled event notification in {ResponseLatency:0.0000}ms (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationHandled(this ILogger logger,
                                                           LogLevel logLevel,
                                                           double responseLatency,
                                                           string eventNotificationId,
                                                           string traceId);

    [LoggerMessage(
        EventName = "conqueror-event-notification-handled",
        Message = "Handled {TransportTypeName:l} event notification on {TransportRole:l} in {ResponseLatency:0.0000}ms (Event Notification ID: {EventNotificationId:l}, Trace ID: {TraceId:l})")]
    public static partial void LogEventNotificationHandledForTransport(this ILogger logger,
                                                                       LogLevel logLevel,
                                                                       string transportTypeName,
                                                                       string transportRole,
                                                                       double responseLatency,
                                                                       string eventNotificationId,
                                                                       string traceId);
}
