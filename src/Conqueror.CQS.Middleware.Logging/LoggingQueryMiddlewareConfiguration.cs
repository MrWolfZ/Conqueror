using System;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging;

/// <summary>
///     The configuration options for <see cref="LoggingQueryMiddleware" />.
/// </summary>
public sealed class LoggingQueryMiddlewareConfiguration
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
    ///     Set to <c>true</c> to omit the JSON-serialized query payload
    ///     from the log message. It will also be omitted from the structured
    ///     metadata.
    /// </summary>
    public bool OmitJsonSerializedQueryPayload { get; set; }

    /// <summary>
    ///     Set to <c>true</c> to omit the JSON-serialized response payload
    ///     (if the query has a response) from the log message. It will also
    ///     be omitted from the structured metadata.
    /// </summary>
    public bool OmitJsonSerializedResponsePayload { get; set; }

    /// <summary>
    ///     Customize the logger name. The factory is passed the query
    ///     object. By default the logger will be generic to the query
    ///     type (i.e. using <see cref="LoggerFactoryExtensions.CreateLogger{T}" />).
    ///     Return <c>null</c> to have the middleware fall back to the
    ///     default name selectively.
    /// </summary>
    public Func<object, string?>? LoggerNameFactory { get; set; }

    /// <summary>
    ///     A hook that is called just before the pre-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and then using either the <see cref="LoggingQueryPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingQueryPreExecutionContext.ServiceProvider" />.
    /// </summary>
    public Func<LoggingQueryPreExecutionContext, bool>? PreExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the post-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and then using either the <see cref="LoggingQueryPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingQueryPreExecutionContext.ServiceProvider" />.
    /// </summary>
    public Func<LoggingQueryPostExecutionContext, bool>? PostExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the exception log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and then using either the <see cref="LoggingQueryPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingQueryPreExecutionContext.ServiceProvider" />.<br />
    ///     <br />
    ///     Note that this hook does not influence what happens with the exception
    ///     itself. It will always be re-thrown by the middleware.
    /// </summary>
    public Func<LoggingQueryExceptionContext, bool>? ExceptionHook { get; set; }

    /// <summary>
    ///     Specify the options to use when JSON-serializing query and
    ///     response objects.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}

/// <summary>
///     The context passed to a <see cref="LoggingQueryMiddleware" />'s <see cref="LoggingQueryMiddlewareConfiguration.PreExecutionHook" />.
/// </summary>
public sealed record LoggingQueryPreExecutionContext(ILogger Logger, LogLevel LogLevel, string QueryId, string TraceId, object Query, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; init; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; init; } = LogLevel;

    /// <summary>
    ///     The ID of the query that is being logged.
    /// </summary>
    public string QueryId { get; } = QueryId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The query that is being logged.
    /// </summary>
    public object Query { get; init; } = Query;

    /// <summary>
    ///     The service provider for the scope in which the query is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; } = ServiceProvider;
}

/// <summary>
///     The context passed to a <see cref="LoggingQueryMiddleware" />'s <see cref="LoggingQueryMiddlewareConfiguration.PostExecutionHook" />.
/// </summary>
public sealed record LoggingQueryPostExecutionContext(ILogger Logger, LogLevel LogLevel, string QueryId, string TraceId, object Query, object? Response, TimeSpan ElapsedTime, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; init; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; init; } = LogLevel;

    /// <summary>
    ///     The ID of the query that is being logged.
    /// </summary>
    public string QueryId { get; } = QueryId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The query that is being logged.
    /// </summary>
    public object Query { get; init; } = Query;

    /// <summary>
    ///     The response that is being logged (is <c>null</c> if there is no response).
    /// </summary>
    public object? Response { get; init; } = Response;

    /// <summary>
    ///     The time which has elapsed while executing the query.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; } = ElapsedTime;

    /// <summary>
    ///     The service provider for the scope in which the query is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; } = ServiceProvider;
}

/// <summary>
///     The context passed to a <see cref="LoggingQueryMiddleware" />'s <see cref="LoggingQueryMiddlewareConfiguration.ExceptionHook" />.
/// </summary>
public sealed record LoggingQueryExceptionContext(ILogger Logger, LogLevel LogLevel, string QueryId, string TraceId, object Query, Exception Exception, StackTrace ExecutionStackTrace, TimeSpan ElapsedTime, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; init; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; init; } = LogLevel;

    /// <summary>
    ///     The ID of the query that is being logged.
    /// </summary>
    public string QueryId { get; } = QueryId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The query that is being logged.
    /// </summary>
    public object Query { get; init; } = Query;

    /// <summary>
    ///     The exception which occurred.
    /// </summary>
    public Exception Exception { get; init; } = Exception;

    /// <summary>
    ///     The stack trace of the current middleware execution. This property
    ///     can be combined with <see cref="Exception.StackTrace" /> to get the
    ///     full stack trace of the exception, since the exception's stack trace
    ///     only contains the stack frames from the middleware execution to the
    ///     handler.
    /// </summary>
    public StackTrace ExecutionStackTrace { get; init; } = ExecutionStackTrace;

    /// <summary>
    ///     The time which has elapsed while executing the query.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; } = ElapsedTime;

    /// <summary>
    ///     The service provider for the scope in which the query is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; } = ServiceProvider;
}
