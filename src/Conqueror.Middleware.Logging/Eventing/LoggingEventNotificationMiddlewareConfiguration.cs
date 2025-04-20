using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Conqueror.Middleware.Logging.Eventing;

/// <summary>
///     The configuration options for <see cref="LoggingEventNotificationMiddleware{TEventNotification}" />.
/// </summary>
public sealed class LoggingEventNotificationMiddlewareConfiguration<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    /// <summary>
    ///     The level at which the pre-execution log message is logged.
    ///     Defaults to <see cref="LogLevel.Information" />.
    /// </summary>
    public LogLevel PreExecutionLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     The level at which the post-execution log message is logged.
    ///     Defaults to <see cref="LogLevel.Information" />.
    /// </summary>
    public LogLevel PostExecutionLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    ///     The level at which the exception log message is logged.
    ///     Defaults to <see cref="LogLevel.Error" />.
    /// </summary>
    public LogLevel ExceptionLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    ///     The strategy to use for logging the event notification payload.
    ///     Defaults to <see cref="PayloadLoggingStrategy.MinimalJson" />.
    /// </summary>
    public PayloadLoggingStrategy PayloadLoggingStrategy { get; set; } = PayloadLoggingStrategy.MinimalJson;

    /// <summary>
    ///     Customize the logger category. The factory is passed the event notification.
    ///     By default, the logger will use the fully-qualified type name of
    ///     the event notification type.
    /// </summary>
    public Func<TEventNotification, string>? LoggerCategoryFactory { get; set; }

    /// <summary>
    ///     A hook that is called just before the pre-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingEventNotificationPreExecutionContext{TEventNotification}.Logger" />.
    /// </summary>
    public Func<LoggingEventNotificationPreExecutionContext<TEventNotification>, bool>? PreExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the post-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingEventNotificationPostExecutionContext{TEventNotification}.Logger" />.
    /// </summary>
    public Func<LoggingEventNotificationPostExecutionContext<TEventNotification>, bool>? PostExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the exception log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and using the <see cref="LoggingEventNotificationExceptionContext{TEventNotification}.Logger" />.<br />
    ///     <br />
    ///     Note that this hook does not influence what happens with the exception
    ///     itself. It will always be re-thrown by the middleware.
    /// </summary>
    public Func<LoggingEventNotificationExceptionContext<TEventNotification>, bool>? ExceptionHook { get; set; }
}

/// <summary>
///     The context passed to a <see cref="LoggingEventNotificationMiddleware{TEventNotification}" />'s
///     <see cref="LoggingEventNotificationMiddlewareConfiguration{TEventNotification}.PreExecutionHook" />.
/// </summary>
public sealed record LoggingEventNotificationPreExecutionContext<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the event notification
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the event notification that is being logged.
    /// </summary>
    public required string EventNotificationId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The event notification that is being logged.
    /// </summary>
    public required TEventNotification EventNotification { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingEventNotificationMiddleware{TEventNotification}" />'s
///     <see cref="LoggingEventNotificationMiddlewareConfiguration{TEventNotification}.PostExecutionHook" />.
/// </summary>
public sealed record LoggingEventNotificationPostExecutionContext<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log a message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the event notification that is being logged.
    /// </summary>
    public required string EventNotificationId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The event notification that is being logged.
    /// </summary>
    public required TEventNotification EventNotification { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the event notification.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}

/// <summary>
///     The context passed to a <see cref="LoggingEventNotificationMiddleware{TEventNotification}" />'s
///     <see cref="LoggingEventNotificationMiddlewareConfiguration{TEventNotification}.ExceptionHook" />.
/// </summary>
public sealed record LoggingEventNotificationExceptionContext<TEventNotification>
    where TEventNotification : class, IEventNotification<TEventNotification>
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log a message
    ///     yourself.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    ///     The level at which the log message will be logged.
    /// </summary>
    public required LogLevel LogLevel { get; init; }

    /// <summary>
    ///     The ID of the event notification that is being logged.
    /// </summary>
    public required string EventNotificationId { get; init; }

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    ///     The event notification that is being logged.
    /// </summary>
    public required TEventNotification EventNotification { get; init; }

    /// <summary>
    ///     The exception which occurred.
    /// </summary>
    public required Exception Exception { get; init; }

    /// <summary>
    ///     The stack trace of the current middleware execution. This property
    ///     can be combined with <see cref="Exception.StackTrace" /> to get the
    ///     full stack trace of the exception, since the exception's stack trace
    ///     only contains the stack frames from the middleware execution to the
    ///     handler.
    /// </summary>
    public required StackTrace ExecutionStackTrace { get; init; }

    /// <summary>
    ///     The time which has elapsed while executing the event notification.
    /// </summary>
    public required TimeSpan ElapsedTime { get; init; }
}
