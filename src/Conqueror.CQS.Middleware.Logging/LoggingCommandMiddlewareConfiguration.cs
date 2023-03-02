using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Conqueror.CQS.Middleware.Logging;

/// <summary>
///     The configuration options for <see cref="LoggingCommandMiddleware" />.
/// </summary>
public sealed class LoggingCommandMiddlewareConfiguration
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
    ///     Set to <c>true</c> to omit the JSON-serialized command payload
    ///     from the log message. It will also be omitted from the structured
    ///     metadata.
    /// </summary>
    public bool OmitJsonSerializedCommandPayload { get; set; }

    /// <summary>
    ///     Set to <c>true</c> to omit the JSON-serialized response payload
    ///     (if the command has a response) from the log message. It will also
    ///     be omitted from the structured metadata.
    /// </summary>
    public bool OmitJsonSerializedResponsePayload { get; set; }

    /// <summary>
    ///     Customize the logger name. The factory is passed the command
    ///     object. By default the logger will be generic to the command
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
    ///     and then using either the <see cref="LoggingCommandPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingCommandPreExecutionContext.ServiceProvider" />.
    /// </summary>
    public Func<LoggingCommandPreExecutionContext, bool>? PreExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the post-execution log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and then using either the <see cref="LoggingCommandPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingCommandPreExecutionContext.ServiceProvider" />.
    /// </summary>
    public Func<LoggingCommandPostExecutionContext, bool>? PostExecutionHook { get; set; }

    /// <summary>
    ///     A hook that is called just before the exception log message
    ///     is written. Return <c>true</c> to allow the log message to be
    ///     written or return <c>false</c> for the log message to be skipped.
    ///     This hook can be used to customize the logging by returning <c>false</c>
    ///     and then using either the <see cref="LoggingCommandPreExecutionContext.Logger" />
    ///     directly or by resolving a different logger from the
    ///     <see cref="LoggingCommandPreExecutionContext.ServiceProvider" />.<br />
    ///     <br />
    ///     Note that this hook does not influence what happens with the exception
    ///     itself. It will always be re-thrown by the middleware.
    /// </summary>
    public Func<LoggingCommandExceptionContext, bool>? ExceptionHook { get; set; }

    /// <summary>
    ///     Specify the options to use when JSON-serializing command and
    ///     response objects.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}

/// <summary>
///     The context passed to a <see cref="LoggingCommandMiddleware" />'s <see cref="LoggingCommandMiddlewareConfiguration.PreExecutionHook" />.
/// </summary>
public sealed record LoggingCommandPreExecutionContext(ILogger Logger, LogLevel LogLevel, string CommandId, string TraceId, object Command, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; } = LogLevel;

    /// <summary>
    ///     The ID of the command that is being logged.
    /// </summary>
    public string CommandId { get; } = CommandId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The command that is being logged.
    /// </summary>
    public object Command { get; } = Command;

    /// <summary>
    ///     The service provider for the scope in which the command is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = ServiceProvider;
}

/// <summary>
///     The context passed to a <see cref="LoggingCommandMiddleware" />'s <see cref="LoggingCommandMiddlewareConfiguration.PostExecutionHook" />.
/// </summary>
public sealed record LoggingCommandPostExecutionContext(ILogger Logger, LogLevel LogLevel, string CommandId, string TraceId, object Command, object? Response, TimeSpan ElapsedTime, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; } = LogLevel;

    /// <summary>
    ///     The ID of the command that is being logged.
    /// </summary>
    public string CommandId { get; } = CommandId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The command that is being logged.
    /// </summary>
    public object Command { get; } = Command;

    /// <summary>
    ///     The response that is being logged (is <c>null</c> if there is no response).
    /// </summary>
    public object? Response { get; } = Response;

    /// <summary>
    ///     The time which has elapsed while executing the command.
    /// </summary>
    public TimeSpan ElapsedTime { get; } = ElapsedTime;

    /// <summary>
    ///     The service provider for the scope in which the command is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = ServiceProvider;
}

/// <summary>
///     The context passed to a <see cref="LoggingCommandMiddleware" />'s <see cref="LoggingCommandMiddlewareConfiguration.ExceptionHook" />.
/// </summary>
public sealed record LoggingCommandExceptionContext(ILogger Logger, LogLevel LogLevel, string CommandId, string TraceId, object Command, Exception Exception, TimeSpan ElapsedTime, IServiceProvider ServiceProvider)
{
    /// <summary>
    ///     The logger used in the middleware. Can be used to log the message
    ///     yourself.
    /// </summary>
    public ILogger Logger { get; } = Logger;

    /// <summary>
    ///     The level at which the message will be logged.
    /// </summary>
    public LogLevel LogLevel { get; } = LogLevel;

    /// <summary>
    ///     The ID of the command that is being logged.
    /// </summary>
    public string CommandId { get; } = CommandId;

    /// <summary>
    ///     The trace ID of the Conqueror operation context.
    /// </summary>
    public string TraceId { get; } = TraceId;

    /// <summary>
    ///     The command that is being logged.
    /// </summary>
    public object Command { get; } = Command;

    /// <summary>
    ///     The exception which occurred.
    /// </summary>
    public Exception Exception { get; } = Exception;

    /// <summary>
    ///     The time which has elapsed while executing the command.
    /// </summary>
    public TimeSpan ElapsedTime { get; } = ElapsedTime;

    /// <summary>
    ///     The service provider for the scope in which the command is being executed.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = ServiceProvider;
}
